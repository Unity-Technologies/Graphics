Shader "Hidden/HDRenderPipeline/SubsurfaceScattering"
{
    Properties
    {
        [HideInInspector] _DstBlend("", Float) = 1 // Can be set to 1 for blending with specular

        [HideInInspector] _StencilMask("_StencilMask", Int) = 7
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Stencil
            {
                ReadMask[_StencilMask]
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
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SSS_FILTER_HORIZONTAL_AND_COMBINE

            // Do not modify these.
            #include "../../ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_SUBSURFACE_SCATTERING

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "../../ShaderVariables.hlsl"
            #include "SubsurfaceScattering.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _FilterKernelsBasic[DIFFUSION_PROFILE_COUNT][SSS_BASIC_N_SAMPLES]; // RGB = weights, A = radial distance
            float4 _HalfRcpWeightedVariances[SSS_BASIC_N_SAMPLES];           // RGB for chromatic, A for achromatic

            TEXTURE2D(_IrradianceSource);             // Includes transmitted light

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

                // Note: When we are in this SubsurfaceScattering shader we know that we are a SSS material.
                SSSData sssData;
                DECODE_FROM_SSSBUFFER(posInput.positionSS, sssData);

                int    profileID   = sssData.diffusionProfile;
                float  distScale   = sssData.subsurfaceMask;
                float  maxDistance = _FilterKernelsBasic[profileID][SSS_BASIC_N_SAMPLES - 1].a;

                // Take the first (central) sample.
                // TODO: copy its neighborhood into LDS.
                float2 centerPosition   = posInput.positionSS;
                float3 centerIrradiance = LOAD_TEXTURE2D(_IrradianceSource, centerPosition).rgb;

                // Reconstruct the view-space position.
                float2 centerPosSS = posInput.positionNDC;
                float2 cornerPosSS = centerPosSS + 0.5 * _ScreenSize.zw;
                float  centerDepth = LOAD_TEXTURE2D(_MainDepthTexture, centerPosition).r;
                float3 centerPosVS = ComputeViewSpacePosition(centerPosSS, centerDepth, UNITY_MATRIX_I_P);
                float3 cornerPosVS = ComputeViewSpacePosition(cornerPosSS, centerDepth, UNITY_MATRIX_I_P);

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

            uint   texturingMode = GetSubsurfaceScatteringTexturingMode(profileID);
            float3 albedo        = ApplySubsurfaceScatteringTexturingMode(texturingMode, sssData.diffuseColor);

            #ifndef SSS_FILTER_HORIZONTAL_AND_COMBINE
                albedo = float3(1, 1, 1);
            #endif

                // Take the first (central) sample.
                float2 samplePosition   = posInput.positionSS;
                float3 sampleWeight     = _FilterKernelsBasic[profileID][0].rgb;
                float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                // We perform point sampling. Therefore, we can avoid the cost
                // of filtering if we stay within the bounds of the current pixel.
                // We use the value of 1 instead of 0.5 as an optimization.
                float maxDistInPixels = maxDistance * max(pixelsPerCm.x, pixelsPerCm.y);

                UNITY_BRANCH
                if (distScale == 0 || maxDistInPixels < 1)
                {
                    #if SSS_DEBUG_LOD
                        return float4(0, 0, 1, 1);
                    #else
                        return float4(albedo * sampleIrradiance, 1);
                    #endif
                }

                #if SSS_DEBUG_LOD
                    return float4(0.5, 0.5, 0, 1);
                #endif

                // Accumulate filtered irradiance and bilateral weights (for renormalization).
                float3 totalIrradiance = sampleWeight * sampleIrradiance;
                float3 totalWeight     = sampleWeight;

                UNITY_UNROLL
                for (int i = 1; i < SSS_BASIC_N_SAMPLES; i++)
                {
                    samplePosition   = posInput.positionSS + rotatedDirection * _FilterKernelsBasic[profileID][i].a;
                    sampleWeight     = _FilterKernelsBasic[profileID][i].rgb;
                    sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                    if (TestLightingForSSS(sampleIrradiance))
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
                        // The irradiance is 0. This could happen for 2 reasons.
                        // Most likely, the surface fragment does not have an SSS material.
                        // Alternatively, our sample comes from a region without any geometry.
                        // Our blur is energy-preserving, so 'centerWeight' should be set to 0.
                        // We do not terminate the loop since we want to gather the contribution
                        // of the remaining samples (e.g. in case of hair covering skin).
                    }
                }

                return float4(albedo * totalIrradiance / totalWeight, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
