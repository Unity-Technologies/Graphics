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
            #define MILLIMETERS_PER_METER 1000

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

            float _WorldScales[SSS_N_PROFILES];                                         // Size of the world unit in meters
            float _FilterKernelsNearField[SSS_N_PROFILES][SSS_N_SAMPLES_NEAR_FIELD][2]; // 0 = radius, 1 = reciprocal of the PDF
            float _FilterKernelsFarField[SSS_N_PROFILES][SSS_N_SAMPLES_FAR_FIELD][2];   // 0 = radius, 1 = reciprocal of the PDF

            TEXTURE2D(_IrradianceSource);             // Includes transmitted light
            DECLARE_GBUFFER_TEXTURE(_GBufferTexture); // Contains the albedo and SSS parameters

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            // Computes the value of the integrand over a disk: (2 * PI * r) * KernelVal().
            // N.b.: the returned value is multiplied by 4. It is irrelevant due to weight renormalization.
            float3 KernelValCircle(float r, float3 S)
            {
                float3 expOneThird = exp(((-1.0 / 3.0) * r) * S);
                return /* 0.25 * */ S * (expOneThird + expOneThird * expOneThird * expOneThird);
            }

            // Computes F(x)/P(x), s.t. x = sqrt(r^2 + z^2).
            float3 ComputeBilateralWeight(float3 S, float r, float z, float rcpDistScale, float rcpPdf)
            {
                // Reducing the integration distance is equivalent to stretching the integration axis.
                float3 valX = KernelValCircle(sqrt(r * r + z * z) * rcpDistScale, S);

                // The reciprocal of the PDF could be reinterpreted as a 'dx' term in Int{F(x)dx}.
                // As we shift the location of the value on the curve during integration,
                // the length of the segment 'dx' under the curve changes approximately linearly.
                float rcpPdfX = rcpPdf * (1 + abs(z) / r);

                return valX * rcpPdfX;
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

                int    profileID   = bsdfData.subsurfaceProfile;
                float  distScale   = bsdfData.subsurfaceRadius;
                float3 shapeParam  = _ShapeParameters[profileID].rgb;
                float  maxDistance = _ShapeParameters[profileID].a;

                // Reconstruct the view-space position.
                float2 cornerPosSS = posInput.positionSS + 0.5 * _ScreenSize.zw;
                float  rawDepth    = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).r;
                float3 centerPosVS = ComputeViewSpacePosition(posInput.positionSS, rawDepth, _InvProjMatrix);
                float3 cornerPosVS = ComputeViewSpacePosition(cornerPosSS,         rawDepth, _InvProjMatrix);

                // Compute the view-space dimensions of the pixel as a quad projected onto geometry.
                float2 unitsPerPixel  = 2 * (cornerPosVS.xy - centerPosVS.xy);
                float  metersPerUnit  = _WorldScales[profileID];
                float  millimPerUnit  = MILLIMETERS_PER_METER * metersPerUnit;
                float2 scaledPixPerMm = distScale * rcp(millimPerUnit * unitsPerPixel);

                // Take the first (central) sample.
                float2 samplePosition   = posInput.unPositionSS;
                float3 sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                // We perform point sampling. Therefore, we can avoid the cost
                // of filtering if we stay within the bounds of the current pixel.
                [branch]
                if (maxDistance * max(scaledPixPerMm.x, scaledPixPerMm.y) < 0.5)
                {
                    return float4(bsdfData.diffuseColor * sampleIrradiance, 1);
                }

                bool useNearFieldKernel = true; // TODO

                if (useNearFieldKernel)
                {
                    float  sampleRcpPdf = _FilterKernelsNearField[profileID][0][1];
                    float3 sampleWeight = KernelValCircle(0, shapeParam) * sampleRcpPdf;

                    // Accumulate filtered irradiance and bilateral weights (for renormalization).
                    float3 totalIrradiance = sampleWeight * sampleIrradiance;
                    float3 totalWeight     = sampleWeight;

                    // Perform integration over the screen-aligned plane in the view space.
                    // TODO: it would be more accurate to use the tangent plane in the world space.

                    [unroll]
                    for (uint i = 1; i < SSS_N_SAMPLES_NEAR_FIELD; i++)
                    {
                        // Everything except for the radius is a compile-time constant.
                        float  r   = _FilterKernelsNearField[profileID][i][0];
                        float  phi = TWO_PI * Fibonacci2d(i, SSS_N_SAMPLES_NEAR_FIELD).y;
                        float2 pos = r * float2(cos(phi), sin(phi));

                        samplePosition = posInput.unPositionSS + pos * scaledPixPerMm;
                        sampleRcpPdf   = _FilterKernelsNearField[profileID][i][1];

                        rawDepth         = LOAD_TEXTURE2D(_MainDepthTexture, samplePosition).r;
                        sampleIrradiance = LOAD_TEXTURE2D(_IrradianceSource, samplePosition).rgb;

                        [flatten]
                        if (any(sampleIrradiance) == false)
                        {
                            // The irradiance is 0. This could happen for 3 reasons.
                            // Most likely, the surface fragment does not have an SSS material.
                            // Alternatively, our sample comes from a region without any geometry.
                            // Finally, the surface fragment could be completely shadowed.
                            // Our blur is energy-preserving, so 'sampleWeight' should be set to 0.
                            // We do not terminate the loop since we want to gather the contribution
                            // of the remaining samples (e.g. in case of hair covering skin).
                            continue;
                        }

                        // Apply bilateral weighting.
                        float sampleZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                        float z       = millimPerUnit * sampleZ - (millimPerUnit * centerPosVS.z);
                        sampleWeight  = ComputeBilateralWeight(shapeParam, r, z, rcp(distScale), sampleRcpPdf);

                        totalIrradiance += sampleWeight * sampleIrradiance;
                        totalWeight     += sampleWeight;
                    }

                    return float4(bsdfData.diffuseColor * totalIrradiance / totalWeight, 1);
                }
                else
                {
                    return float4(0, 0, 0, 1); // TODO
                }
            }
            ENDHLSL
        }
    }
    Fallback Off
}
