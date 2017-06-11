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
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #define SSS_REFERENCE         1 // Enable reference mode for debugging
            #define SSS_PASS              1
            #define SSS_BILATERAL         1
            #define SSS_DEBUG             0
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

            float4 _SurfaceShapeParams[SSS_N_PROFILES];                                  // RGB = S = 1 / D, A = filter radius
            float  _WorldScales[SSS_N_PROFILES];                                         // Size of the world unit in meters
            float  _FilterKernelsNearField[SSS_N_PROFILES][SSS_N_SAMPLES_NEAR_FIELD][2]; // 0 = radius, 1 = reciprocal of the PDF
            float  _FilterKernelsFarField[SSS_N_PROFILES][SSS_N_SAMPLES_FAR_FIELD][2];   // 0 = radius, 1 = reciprocal of the PDF

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

            #if SSS_REFERENCE

            // In reference mode we compute importance sampling separately for (R, G, B)
            float KernelValCircle(float r, float s)
            {
                float expOneThird = exp(((-1.0 / 3.0) * r) * s);
                return  0.25 * s * (expOneThird + expOneThird * expOneThird * expOneThird);
            }

            // All funciton below are similar to C# version

            float KernelPdf(float r, float s)
            {
                float expOneThird = exp(((-1.0 / 3.0) * r) * s);
                return 0.25 * s * (expOneThird + expOneThird * expOneThird * expOneThird);
            }

            float KernelCdf(float r, float s)
            {
                return 1.0 - 0.25 * exp(-r * s) - 0.75f * exp(-r * s * (1.0 / 3.0));
            }

            float KernelCdfDerivative1(float r, float s)
            {
                return 0.25 * s * exp(-r * s) * (1.0f + exp(r * s * (2.0 / 3.0)));
            }

            float KernelCdfDerivative2(float r, float s)
            {
                return (-1.0 / 12.0) * s * s * exp(-r * s) * (3.0 + exp(r * s * (2.0 / 3.0)));
            }

            // The CDF is not analytically invertible, so we use Halley's Method of root finding.
            // { f(r, s, p) = CDF(r, s) - p = 0 } with the initial guess { r = (10^p - 1) / s }.
            float KernelCdfInverse(float p, float s)
            {
                // Supply the initial guess.
                float r = (pow(10.0, p) - 1.0) / s;
                float t = FLT_MAX;

                while (true)
                {
                    float f0 = KernelCdf(r, s) - p;
                    float f1 = KernelCdfDerivative1(r, s);
                    float f2 = KernelCdfDerivative2(r, s);
                    float dr = f0 / (f1 * (1.0 - f0 * f2 / (2.0 * f1 * f1)));

                    if (abs(dr) < t)
                    {
                        r = r - dr;
                        t = abs(dr);
                    }
                    else
                    {
                        // Converged to the best result.
                        break;
                    }
                }

                return r;
            }
            #endif // #if SSS_REFERENCE

            // Computes F(x)/P(x), s.t. x = sqrt(r^2 + t^2).
            float3 ComputeBilateralWeight(float3 S, float r, float t, float rcpDistScale, float rcpPdf)
            {
            #if (SSS_BILATERAL == 0)
                t = 0;
            #endif
                // Reducing the integration distance is equivalent to stretching the integration axis.
                float3 val = KernelValCircle(sqrt(r * r + t * t) * rcpDistScale, S);

                // Rescaling of the PDF is handled via 'totalWeight'.
                return val * rcpPdf;
            }

            #define SSS_ITER(i, n, kernel, profileID, shapeParam, centerPosUnSS, centerDepthVS, \
                    millimPerUnit, scaledPixPerMm, rcpDistScale, totalIrradiance, totalWeight)  \
            {                                                                                   \
                float  r   = kernel[profileID][i][0];                                           \
                /* The relative sample position is known at compile time. */                    \
                float  phi = TWO_PI * Fibonacci2d(i, n).y;                                      \
                float2 vec = r * float2(cos(phi), sin(phi));                                    \
                                                                                                \
                float2 position   = centerPosUnSS + vec * scaledPixPerMm;                       \
                float3 irradiance = LOAD_TEXTURE2D(_IrradianceSource, position).rgb;            \
                                                                                                \
                /* TODO: see if making this a [branch] improves performance. */                 \
                [flatten]                                                                       \
                if (any(irradiance))                                                            \
                {                                                                               \
                    /* Apply bilateral weighting. */                                            \
                    float  z = LOAD_TEXTURE2D(_MainDepthTexture, position).r;                   \
                    float  d = LinearEyeDepth(z, _ZBufferParams);                               \
                    float  t = millimPerUnit * d - (millimPerUnit * centerDepthVS);             \
                    float  p = kernel[profileID][i][1];                                         \
                    float3 w = ComputeBilateralWeight(shapeParam, r, t, rcpDistScale, p);       \
                                                                                                \
                    totalIrradiance += w * irradiance;                                          \
                    totalWeight     += w;                                                       \
                }                                                                               \
                else                                                                            \
                {                                                                               \
                    /*************************************************************************/ \
                    /* The irradiance is 0. This could happen for 3 reasons.                 */ \
                    /* Most likely, the surface fragment does not have an SSS material.      */ \
                    /* Alternatively, our sample comes from a region without any geometry.   */ \
                    /* Finally, the surface fragment could be completely shadowed.           */ \
                    /* Our blur is energy-preserving, so 'centerWeight' should be set to 0.  */ \
                    /* We do not terminate the loop since we want to gather the contribution */ \
                    /* of the remaining samples (e.g. in case of hair covering skin).        */ \
                    /*************************************************************************/ \
                }                                                                               \
            }

            #define SSS_LOOP(n, kernel, profileID, shapeParam, centerPosUnSS, centerDepthVS,    \
                    millimPerUnit, scaledPixPerMm, rcpDistScale, totalIrradiance, totalWeight)  \
            {                                                                                   \
                float  centerRcpPdf = kernel[profileID][0][1];                                  \
                float3 centerWeight = KernelValCircle(0, shapeParam) * centerRcpPdf;            \
                                                                                                \
                totalIrradiance = centerWeight * centerIrradiance;                              \
                totalWeight     = centerWeight;                                                 \
                                                                                                \
                /* Perform integration over the screen-aligned plane in the view space. */      \
                /* TODO: it would be more accurate to use the tangent plane instead.    */      \
                [unroll]                                                                        \
                for (uint i = 1; i < n; i++)                                                    \
                {                                                                               \
                    SSS_ITER(i, n, kernel, profileID, shapeParam, centerPosUnSS, centerDepthVS, \
                    millimPerUnit, scaledPixPerMm, rcpDistScale, totalIrradiance, totalWeight)  \
                }                                                                               \
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
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);

                float3 unused;

                BSDFData bsdfData;
                FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS);
                DECODE_FROM_GBUFFER(gbuffer, 0xFFFFFFFF, bsdfData, unused);

                int    profileID   = bsdfData.subsurfaceProfile;
                float  distScale   = bsdfData.subsurfaceRadius;
                float3 shapeParam  = _SurfaceShapeParams[profileID].rgb;
                float  maxDistance = _SurfaceShapeParams[profileID].a;

                // Calculate the size of a pixel in unity unit on the screen. This will allow to be take into account, perspective, FOV, aspect ratio and have a constant effects.
                // Reconstruct the view-space position.
                float2 centerPosSS = posInput.positionSS;
                float2 cornerPosSS = centerPosSS + 0.5 * _ScreenSize.zw;
                float  centerDepth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).r;
                float3 centerPosVS = ComputeViewSpacePosition(centerPosSS, centerDepth, _InvProjMatrix);
                float3 cornerPosVS = ComputeViewSpacePosition(cornerPosSS, centerDepth, _InvProjMatrix);

                // Compute the view-space dimensions of the pixel as a quad projected onto geometry.
                float2 unitsPerPixel  = 2 * (cornerPosVS.xy - centerPosVS.xy);
                float  metersPerUnit  = _WorldScales[profileID];
                float  millimPerUnit  = MILLIMETERS_PER_METER * metersPerUnit;
                float2 scaledPixPerMm = distScale * rcp(millimPerUnit * unitsPerPixel);

                float2 centerPosition = posInput.unPositionSS;

                #if SSS_REFERENCE

                float3 totalIrradiance = 0.0;
                float3 totalWeight     = 0.0;

                // Add some jittering on Hammersley2d
                float2 randNum = InitRandom(centerPosSS.xy * 0.5 + 0.5);

                /* Perform integration over the screen-aligned plane in the view space. */
                /* TODO: it would be more accurate to use the tangent plane instead.    */
                uint sampleCount = 1000;
                float rcpDistScale = rcp(distScale);

                float3 maxRadius;
                maxRadius.x = KernelCdfInverse(1, shapeParam.x);
                maxRadius.y = KernelCdfInverse(1, shapeParam.y);
                maxRadius.z = KernelCdfInverse(1, shapeParam.z);

                for (uint i = 0; i < sampleCount; ++i)
                {
                    float2 u = Hammersley2d(i, sampleCount);
                    u = frac(u + randNum);

                    float3 r = maxRadius * u.x;
                    float  phi = TWO_PI * u.y;

                    float irradiance[3];
                    float w[3];
                    for (int j = 0; j < 3; j++)
                    {
                        float2 vec = r[j] * float2(cos(phi), sin(phi));

                        float2 position = centerPosition + vec * scaledPixPerMm;
                        float3 value = LOAD_TEXTURE2D(_IrradianceSource, position).rgb;
                        irradiance[j] = value[j];

                        float  z = LOAD_TEXTURE2D(_MainDepthTexture, position).r;
                        float  d = LinearEyeDepth(z, _ZBufferParams);
                        float  t = millimPerUnit * d - (millimPerUnit * centerPosVS.z);
                        // Reducing the integration distance is equivalent to stretching the integration axis.
                        w[j] = KernelValCircle(sqrt(r[j] * r[j] + t * t) * rcpDistScale, shapeParam[j]);
                    }

                    totalIrradiance += float3(w[0], w[1], w[2]) * float3(irradiance[0], irradiance[1], irradiance[2]);
                    totalWeight += float3(w[0], w[1], w[2]);
                }

                return float4(bsdfData.diffuseColor * totalIrradiance / totalWeight, 1);

                #else

                // Take the first (central) sample.
                // TODO: copy its neighborhood into LDS.
                float3 centerIrradiance = LOAD_TEXTURE2D(_IrradianceSource, centerPosition).rgb;
                float  maxDistInPixels = maxDistance * max(scaledPixPerMm.x, scaledPixPerMm.y);

                // We perform point sampling. Therefore, we can avoid the cost
                // of filtering if we stay within the bounds of the current pixel.
                [branch]
                if (maxDistInPixels < 1)
                {
                    #if SSS_DEBUG
                        return float4(0, 0, 1, 1);
                    #else
                        return float4(bsdfData.diffuseColor * centerIrradiance, 1);
                    #endif
                }

                // Accumulate filtered irradiance and bilateral weights (for renormalization).
                float3 totalIrradiance, totalWeight;

                // Use fewer samples for SS regions smaller than 5x5 pixels (rotated by 45 degrees).
                [branch]
                if (maxDistInPixels < SSS_LOD_THRESHOLD)
                {
                    #if SSS_DEBUG
                        return float4(0.5, 0.5, 0, 1);
                    #else
                        SSS_LOOP(SSS_N_SAMPLES_FAR_FIELD, _FilterKernelsFarField,
                                 profileID, shapeParam, centerPosition, centerPosVS.z,
                                 millimPerUnit, scaledPixPerMm, rcp(distScale),
                                 totalIrradiance, totalWeight)
                    #endif
                }
                else
                {
                    #if SSS_DEBUG
                        return float4(1, 0, 0, 1);
                    #else
                        SSS_LOOP(SSS_N_SAMPLES_NEAR_FIELD, _FilterKernelsNearField,
                                 profileID, shapeParam, centerPosition, centerPosVS.z,
                                 millimPerUnit, scaledPixPerMm, rcp(distScale),
                                 totalIrradiance, totalWeight)
                    #endif
                }

                return float4(bsdfData.diffuseColor * totalIrradiance / totalWeight, 1);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
