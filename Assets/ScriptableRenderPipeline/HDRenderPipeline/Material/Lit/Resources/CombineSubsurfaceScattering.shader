Shader "Hidden/HDRenderPipeline/CombineSubsurfaceScattering"
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
                Ref  1 // StencilBits.SSS
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  One One

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev
            #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #define SSS_PASS
            #define METERS_TO_MILLIMETERS 1000

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "../../../../ShaderLibrary/Common.hlsl"
            #include "../../../ShaderConfig.cs.hlsl"
            #include "../../../ShaderVariables.hlsl"
            #define UNITY_MATERIAL_LIT // Needs to be defined before including Material.hlsl
            #include "../../../Material/Material.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _FilterKernelsNearField[SSS_N_PROFILES][SSS_N_SAMPLES_NEAR_FIELD]; // RGB = weights, A = radial distance (in millimeters)
            float4 _FilterKernelsFarField[SSS_N_PROFILES][SSS_N_SAMPLES_FAR_FIELD];   // RGB = weights, A = radial distance (in millimeters)

            TEXTURE2D(_IrradianceSource);             // RGB = irradiance on the back side of the object
            DECLARE_GBUFFER_TEXTURE(_GBufferTexture); // Contains the albedo and SSS parameters

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            // Computes A * B, s.t.:
            // A = (exp(-S * sqrt(r*r + z*z)) + exp(-S * sqrt(r*r + z*z) / 3)) / sqrt(r*r + z*z)
            // B = sqrt(r*r) / (exp(-S * sqrt(r*r)) + exp(-S * sqrt(r*r) / 3))
            float3 ComputeBilateralWeight(float3 S, float r, float z, float distScale)
            {
                float3 S3 = S * (1.0 / 3.0); // Same for all samples
                float  iF = rcp(distScale);  // Same for all samples

                r *= iF; z *= iF;

                float  rz2 = r * r + z * z;
                float  sR  = abs(r);
                float  sRZ = sqrt(rz2);
                float  iRZ = rsqrt(rz2);
                float3 eR  = exp(-S3 * sR);
                float3 eRZ = exp(-S3 * sRZ);

                eR  += eR  * eR  * eR;

                float3 A = iRZ * (eRZ * eRZ * eRZ + eRZ);
                float3 B = sR  / (eR  * eR  * eR  + eR ); // TODO: precompute

                return A * B;
            }

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
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, uint2(0, 0));

                float3 unused;

                BSDFData bsdfData;
                FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS);
                DECODE_FROM_GBUFFER(gbuffer, 0xFFFFFFFF, bsdfData, unused);

                int    profileID  = bsdfData.subsurfaceProfile;
                float  distScale  = bsdfData.subsurfaceRadius;
                float3 shapeParam = _ShapeParameters[profileID].rgb;

                // Reconstruct the view-space position.
                float  rawDepth    = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).r;
                float3 centerPosVS = ComputeViewSpacePosition(posInput.positionSS, rawDepth, _InvProjMatrix);

                // Compute the dimensions of the surface fragment viewed as a quad facing the camera.
                // TODO: this could be done more accurately using a matrix precomputed on the CPU.
                float2 metersPerPixel      = float2(ddx_fine(centerPosVS.x), ddy_fine(centerPosVS.y));
                float2 pixelsPerMillimeter = distScale * rcp(METERS_TO_MILLIMETERS * metersPerPixel);

                bool useNearFieldKernel = true; // TODO

                if (useNearFieldKernel)
                {
                    // Take the first (central) sample.
                    float2 samplePosition   = posInput.unPositionSS;
                    float3 sampleWeight     = _FilterKernelsNearField[profileID][0].rgb;
                    float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                    // We perform point sampling. Therefore, we can avoid the cost
                    // of filtering if we stay within the bounds of the current pixel.
                    float maxDistance = _FilterKernelsNearField[profileID][0].a;

                    [branch]
                    if (maxDistance * max(pixelsPerMillimeter.x, pixelsPerMillimeter.y) < 0.5)
                    {
                        return float4(bsdfData.diffuseColor * sampleIrradiance, 1);
                    }

                    // Accumulate filtered irradiance and bilateral weights (for renormalization).
                    float3 totalIrradiance = sampleWeight * sampleIrradiance;
                    float3 totalWeight     = sampleWeight;

                    // Perform integration over the screen-aligned plane in the view space.
                    // TODO: it would be more accurate to use the tangent plane in the world space.
                    [unroll]
                    for (uint i = 1; i < SSS_N_SAMPLES_NEAR_FIELD; i++)
                    {
                        // Everything except for the radius is a compile-time constant.
                        float  r   = _FilterKernelsNearField[profileID][i].a;
                        float  phi = TWO_PI * VanDerCorputBase2(i);
                        float2 pos = r * float2(cos(phi), sin(phi));

                        samplePosition = posInput.unPositionSS + pos * pixelsPerMillimeter;
                        sampleWeight   = _FilterKernelsNearField[profileID][i].rgb;

                        rawDepth         = LOAD_TEXTURE2D(_MainDepthTexture, samplePosition).r;
                        sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                        // Apply bilateral weighting.
                        // We adjust the precomputed weight W(r) by W(d)/W(r), where
                        // r = sqrt(x^2 + y^2), d = sqrt(x^2 + y^2 + z^2) = sqrt(r^2 + z^2).
                        float sampleZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                        float z       = METERS_TO_MILLIMETERS * sampleZ - (METERS_TO_MILLIMETERS * centerPosVS.z);
                        sampleWeight *= ComputeBilateralWeight(shapeParam, r, z, distScale);

                        [flatten]
                        if (any(sampleIrradiance) == false)
                        {
                            // The irradiance is 0. This could happen for 2 reasons.
                            // Most likely, the surface fragment does not have an SSS material.
                            // Alternatively, the surface fragment could be completely shadowed.
                            // Our blur is energy-preserving, so 'sampleWeight' should be set to 0.
                            // We do not terminate the loop since we want to gather the contribution
                            // of the remaining samples (e.g. in case of hair covering skin).
                            continue;
                        }

                        totalIrradiance += sampleWeight * sampleIrradiance;
                        totalWeight     += sampleWeight;
                    }

                    return float4(bsdfData.diffuseColor * totalIrradiance / totalWeight, 1);
                }
                else
                {
                    return float4(0, 0, 0, 0); // TODO
                }
            }
            ENDHLSL
        }
    }
    Fallback Off
}
