Shader "Hidden/HDRenderPipeline/SubsurfaceScattering"
{
    Properties
    {
        [HideInInspector] _DstBlend("", Float) = 1 // Can be set to 1 for blending with specular
    }

    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref  1 // StencilLightingUsage.SplitLighting
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  One [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SSS_FILTER_HORIZONTAL_AND_COMBINE

            // Do not modify these.
            #define SSS_PASS              1
            #define MILLIMETERS_PER_METER 1000
            #define CENTIMETERS_PER_METER 100

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "../../../../ShaderLibrary/Common.hlsl"
            #include "../../../ShaderVariables.hlsl"
            #define UNITY_MATERIAL_LIT // Needs to be defined before including Material.hlsl
            #include "../../../Material/Material.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _WorldScales[SSS_N_PROFILES];                             // Size of the world unit in meters (only the X component is used)
            float4 _FilterKernelsBasic[SSS_N_PROFILES][SSS_BASIC_N_SAMPLES]; // RGB = weights, A = radial distance
            float4 _HalfRcpWeightedVariances[SSS_BASIC_N_SAMPLES];           // RGB for chromatic, A for achromatic

            TEXTURE2D(_IrradianceSource);             // Includes transmitted light
            DECLARE_GBUFFER_TEXTURE(_GBufferTexture); // Contains the albedo and SSS parameters

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);

                float3 unused;

                // Note: When we are in this SubsurfaceScattering shader we know that we are a SSS material. This shader is strongly coupled with the deferred Lit.shader.
                // We can use the material classification facility to help the compiler to know we use SSS material and optimize the code (and don't require to read gbuffer with materialId).
                uint featureFlags = MATERIALFEATUREFLAGS_LIT_SSS;

                BSDFData bsdfData;
                FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS);
                DECODE_FROM_GBUFFER(gbuffer, featureFlags, bsdfData, unused);

                int    profileID   = bsdfData.subsurfaceProfile;
                float  distScale   = bsdfData.subsurfaceRadius;
                float  maxDistance = _FilterKernelsBasic[profileID][SSS_BASIC_N_SAMPLES - 1].a;

                // Take the first (central) sample.
                // TODO: copy its neighborhood into LDS.
                float2 centerPosition   = posInput.unPositionSS;
                float3 centerIrradiance = LOAD_TEXTURE2D(_IrradianceSource, centerPosition).rgb;

                // Reconstruct the view-space position.
                float2 centerPosSS = posInput.positionSS;
                float2 cornerPosSS = centerPosSS + 0.5 * _ScreenSize.zw;
                float  centerDepth = LOAD_TEXTURE2D(_MainDepthTexture, centerPosition).r;
                float3 centerPosVS = ComputeViewSpacePosition(centerPosSS, centerDepth, _InvProjMatrix);
                float3 cornerPosVS = ComputeViewSpacePosition(cornerPosSS, centerDepth, _InvProjMatrix);

                // Rescaling the filter is equivalent to inversely scaling the world.
                float  metersPerUnit = _WorldScales[profileID].x / distScale * SSS_BASIC_DISTANCE_SCALE;
                float  centimPerUnit = CENTIMETERS_PER_METER * metersPerUnit;
                // Compute the view-space dimensions of the pixel as a quad projected onto geometry.
                float2 unitsPerPixel = 2 * abs(cornerPosVS.xy - centerPosVS.xy);
                float2 pixelsPerCm   = rcp(centimPerUnit * unitsPerPixel);

                // Compute the filtering direction.
            #ifdef SSS_FILTER_HORIZONTAL_AND_COMBINE
                float2 unitDirection = float2(1, 0);
            #else
                float2 unitDirection = float2(0, 1);
            #endif

                float2   scaledDirection  = pixelsPerCm * unitDirection;
                float    phi              = 0; // Random rotation; unused for now
                float2x2 rotationMatrix   = float2x2(cos(phi), -sin(phi), sin(phi), cos(phi));
                float2   rotatedDirection = mul(rotationMatrix, scaledDirection);

                // Load (1 / (2 * WeightedVariance)) for bilateral weighting.
            #if RBG_BILATERAL_WEIGHTS
                float3 halfRcpVariance = _HalfRcpWeightedVariances[profileID].rgb;
            #else
                float  halfRcpVariance = _HalfRcpWeightedVariances[profileID].a;
            #endif

            #ifndef SSS_FILTER_HORIZONTAL_AND_COMBINE
                bsdfData.diffuseColor = float3(1, 1, 1);
            #endif

                // Take the first (central) sample.
                float2 samplePosition   = posInput.unPositionSS;
                float3 sampleWeight     = _FilterKernelsBasic[profileID][0].rgb;
                float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                // We perform point sampling. Therefore, we can avoid the cost
                // of filtering if we stay within the bounds of the current pixel.
                // We use the value of 1 instead of 0.5 as an optimization.
                float maxDistInPixels = maxDistance * max(pixelsPerCm.x, pixelsPerCm.y);

                [branch]
                if (distScale == 0 || maxDistInPixels < 1)
                {
                    #if SSS_DEBUG_LOD
                        return float4(0, 0, 1, 1);
                    #else
                        return float4(bsdfData.diffuseColor * sampleIrradiance, 1);
                    #endif
                }
                
                #if SSS_DEBUG_LOD
                    return float4(0.5, 0.5, 0, 1);
                #endif

                // Accumulate filtered irradiance and bilateral weights (for renormalization).
                float3 totalIrradiance = sampleWeight * sampleIrradiance;
                float3 totalWeight     = sampleWeight;

                [unroll]
                for (int i = 1; i < SSS_BASIC_N_SAMPLES; i++)
                {
                    samplePosition   = posInput.unPositionSS + rotatedDirection * _FilterKernelsBasic[profileID][i].a;
                    sampleWeight     = _FilterKernelsBasic[profileID][i].rgb;
                    sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                    [flatten]
                    if (any(sampleIrradiance))
                    {
                        // Apply bilateral weighting.
                        // Ref #1: Skin Rendering by Pseudo–Separable Cross Bilateral Filtering.
                        // Ref #2: Separable SSS, Supplementary Materials, Section E.
                        float rawDepth    = LOAD_TEXTURE2D(_MainDepthTexture, samplePosition).r;
                        float sampleDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                        float zDistance   = centimPerUnit * sampleDepth - (centimPerUnit * centerPosVS.z);
                        sampleWeight     *= exp(-zDistance * zDistance * halfRcpVariance);

                        totalIrradiance += sampleWeight * sampleIrradiance;
                        totalWeight     += sampleWeight;
                    }
                    else
                    {
                        // The irradiance is 0. This could happen for 3 reasons.
                        // Most likely, the surface fragment does not have an SSS material.
                        // Alternatively, our sample comes from a region without any geometry.
                        // Finally, the surface fragment could be completely shadowed.
                        // Our blur is energy-preserving, so 'centerWeight' should be set to 0.
                        // We do not terminate the loop since we want to gather the contribution
                        // of the remaining samples (e.g. in case of hair covering skin).
                    }
                }

                return float4(bsdfData.diffuseColor * totalIrradiance / totalWeight, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
