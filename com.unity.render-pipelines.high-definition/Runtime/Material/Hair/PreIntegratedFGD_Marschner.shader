Shader "Hidden/HDRP/PreIntegratedFGD_Marschner"
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
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #define PREFER_HALF 0
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "HairMarschner.hlsl"

            // ----------------------------------------------------------------------------
            // Importance Sampling
            // ----------------------------------------------------------------------------

            // The general idea here is that we will sample only longitudinal roughness and assume that azimuthal
            // roughness is really large and we can modulate the result with another narrower azimuthal roughness
            // window function, thereby faking having some support for more range of azimuthal roughness.
            void SampleMarschnerDir( real2 u, real3 V, real3x3 localToWorld, real longitudinalRoughness, 
                                     out real3 L, out real thetaH, out real phiH, out real LdotT, out real VdotH)
            {
                real thetaI = FastACos(V.y);
                real tHMin = (thetaI - HALF_PI) * 0.5;
                real tHMax = (thetaI + HALF_PI) * 0.5;
                // This is slightly different from the proper CDF in that it is normalized
                // to the 0..1 range so that it gives us UV coords to plug into the inverse CDF
                tHMin = 1.0 / (1.0 + exp(tHMin / -longitudinalRoughness));    // Longitudinal angles
                tHMax = 1.0 / (1.0 + exp(tHMax / -longitudinalRoughness));
                real pHMin = 1.0 / (1.0 + exp(-HALF_PI / -0.7071));            // Azimuthal angles
                real pHMax = 1.0 / (1.0 + exp(HALF_PI / -0.7071));
                thetaH = inverseLogisticCDF(lerp(tHMin, tHMax, u.x), longitudinalRoughness);
                phiH = inverseLogisticCDF(lerp(pHMin, pHMax, u.y), 0.7071);

                real3 localH = real3(sin(thetaH) * cos(phiH), cos(thetaH), sin(thetaH) * sin(phiH));
 
                real3 localV;

                localV = mul(V, transpose(localToWorld));
                VdotH  = saturate(dot(localV, localH));

                // Compute { localL = reflect(-localV, localH) }
                real3   localL = -localV + 2.0 * VdotH * localH;

                L = mul(localL, localToWorld);
                LdotT = localL.y;
            }

            void ImportanceSampleMarschner( real2   u,
                                            real3   V,
                                            real3x3 localToWorld,
                                            real    longRoughness,
                                            out real3   L, out real LdotT, out real phiD, out real weightOverPdf )
            {
                real thetaH, phiH, VdotH;
                SampleMarschnerDir(u, V, localToWorld, longRoughness, L, thetaH, phiH, LdotT, VdotH);

                real thetaL = FastACos(LdotT);
                real thetaI = thetaL - 2.0 * (thetaL - thetaH);

                phiD = phiH * 2.0;
                weightOverPdf = evalHairCosTerm(thetaL, thetaI);
            }

            float4  IntegrateMarschnerFGD(float3 V, float3 T, float roughness, uint sampleCount = 1024)
            {
                float4    acc = float4(0.0, 0.0, 0.0, 0.0);
                float3x3  localToWorld = k_identity3x3;

                for (uint i = 0; i < sampleCount; ++i)
                {
                    float2  u = Hammersley2d(i, sampleCount);
                    float3 L;
                    float LdotT;
                    float phiD;
                    float weightOverPdf;

                    ImportanceSampleMarschner(u, V, localToWorld, roughness, L, LdotT, phiD, weightOverPdf);
                    float3 Fres = HairFresnelAllLobes(1.55, phiD);
                    acc.x += weightOverPdf * dot(Fres,1);
                    acc.y += weightOverPdf;  
                }
                acc /= sampleCount;

                return float4(acc.xy, 1.0, 0.0);
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
                float   TdotV               = input.texCoord.x;
                float   perceptualRoughness = input.texCoord.y;
                float3  V                   = float3(0.0, TdotV, sqrt(1 - TdotV * TdotV));
                float3  T                   = float3(0.0, 1.0, 0.0);

                float4 preFGD = IntegrateMarschnerFGD(V, T, PerceptualRoughnessToRoughness(perceptualRoughness));

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}