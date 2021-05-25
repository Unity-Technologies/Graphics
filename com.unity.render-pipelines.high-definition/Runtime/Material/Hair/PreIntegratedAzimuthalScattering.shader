Shader "Hidden/HDRP/PreIntegratedAzimuthalScattering"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #define PREFER_HALF 0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.cs.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            // TODO: Merge/Re-use

            // Ref: Light Scattering from Human Hair Fibers
            float AzimuthalDirection(uint p, float etaPrime, float h)
            {
                float gammaI = asin(h);
                float gammaT = asin(h / etaPrime);

                return (2 * p * gammaT) - (2 * gammaI) + (p * PI);
            }

            float ModifiedRefractionIndex(float cosThetaD)
            {
                // Original derivation of modified refraction index for arbitrary IOR.
                // float sinThetaD = sqrt(1 - Sq(cosThetaD));
                // return sqrt(Sq(eta) - Sq(sinThetaD)) / cosThetaD;

                // Approximate the modified refraction index for human hair (1.55)
                return 1.19 / cosThetaD + (0.36 * cosThetaD);
            }

            float Gaussian(float beta, float phi)
            {
                return exp(-0.5 * (phi * phi) / (beta * beta)) * rcp(sqrt(TWO_PI) * beta);
            }

            // Ref: [An Energy-Conserving Hair Reflectance Model]
            float GaussianDetector(float beta, float phi)
            {
                float D = 0;

                // Higher order detection is negligible for (beta < 80º).
                int order = 4;

                for (int k = -order; k <= order; k++)
                {
                    D += Gaussian(beta, phi - (TWO_PI * k));
                }

                return D;
            }


            float4 Frag(Varyings input) : SV_Target
            {
                // We want the LUT to contain the entire [0, 1] range, without losing half a texel at each side.
                float2 coordLUT = RemapHalfTexelCoordTo01(input.texCoord, AZIMUTHALSCATTERINGTEXTURE_RESOLUTION);

                float beta      = 0.35;
                float cosThetaD = coordLUT.x;
                float phi       = coordLUT.y * FOUR_PI - TWO_PI;

                // Fixed at 1.55 (human hair).
                float refractionIndex = ModifiedRefractionIndex(cosThetaD);

                const uint sampleCountDistribution = 1024;

                float D = 0;

                // Evaluate the distribution for this slice of phi.
                for (uint k = 0; k < sampleCountDistribution; k++)
                {
                    float h = 2 * ((float)k / sampleCountDistribution) - 1;
                    float omega = AzimuthalDirection(1, refractionIndex, h);
                    D += GaussianDetector(beta, phi - omega) * rcp(sampleCountDistribution);
                }

                D *= 0.5;

                return float4(D, D, D, 1);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
