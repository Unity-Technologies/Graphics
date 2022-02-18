Shader "Hidden/HDRP/preIntegratedFGD_CharlieFabricLambert"
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            // ----------------------------------------------------------------------------
            // Importance Sampling
            // ----------------------------------------------------------------------------
            float4 IntegrateCharlieAndFabricLambertFGD(float3 V, float3 N, float roughness, uint sampleCount = 4096)
            {
                // Ref: "Production Friendly Microfacet Sheen BRDF": http://www.aconty.com/pdf/s2017_pbs_imageworks_sheen.pdf
                float NdotV = ClampNdotV(dot(N, V));
                float4 acc = float4(0.0, 0.0, 0.0, 0.0);
                float3x3 localToWorld = GetLocalFrame(N);
                float rcpSampleCount = rcp(sampleCount);
                for (uint i = 0; i < sampleCount; ++i)
                {
                    // uniformly sample the hemisphere (recommended by the paper)
                    float3 localL = SampleConeStrata(i, rcpSampleCount, 0.0f);
                    float NdotL = localL.z;
                    float3 L = mul(localL, localToWorld);

                    // evaluate cos-weighted "Charlie" BRDF without the Fresnel term
                    float3 H = normalize(V + L);
                    float NdotH = dot(N, H);
                    float weight = D_Charlie(NdotH, roughness) * V_Charlie(NdotL, NdotV, roughness) * NdotL;

                    // Integral{BSDF * <N,L> dw} =
                    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                    float VdotH = dot(V, H);
                    acc.x += weight * pow(1 - VdotH, 5);
                    acc.y += weight;

                    // for Fabric Lambert we still use a Cosine importance sampling
                    float weightOverPdf;
                    float2 u = Hammersley2d(i, sampleCount);
                    ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);
                    float fabricLambert = FabricLambertNoPI(roughness);
                    acc.z += fabricLambert * weightOverPdf;
                }

                // Normalize the accumulated value
                acc *= 1.0f / sampleCount; // should be multiplied by 2pi, but to keep the values in [0, 1] range for texture storage we defer the multiplication to sampling in GetPreIntegratedFGDCharlieAndFabricLambert
                return acc;
            }

            // ----------------------------------------------------------------------------
            // Pre-Integration
            // ----------------------------------------------------------------------------

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

            float4 Frag(Varyings input) : SV_Target
            {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float NdotV                 = input.texCoord.x;
                float perceptualRoughness   = input.texCoord.y;
                float3 V                    = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3 N                    = float3(0.0, 0.0, 1.0);

                // Pre integrate GGX with smithJoint visibility as well as DisneyDiffuse
                float4 preFGD = IntegrateCharlieAndFabricLambertFGD(V, N, PerceptualRoughnessToRoughness(perceptualRoughness));

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
