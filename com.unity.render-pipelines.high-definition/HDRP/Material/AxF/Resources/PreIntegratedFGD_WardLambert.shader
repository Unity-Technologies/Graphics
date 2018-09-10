Shader "Hidden/HDRenderPipeline/PreIntegratedFGD_WardLambert"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "../../../ShaderVariables.hlsl"

            // ==============================================================================================
            // Pre-Integration Code
            //

            // ----------------------------------------------------------------------------
            // Importance sampling BSDF functions
            // ----------------------------------------------------------------------------
            // Formulas come from -> Walter, B. 2005 "Notes on the Ward BRDF" (https://pdfs.semanticscholar.org/330e/59117d7da6c794750730a15f9a178391b9fe.pdf)
            // The BRDF though, is the one most proeminently used by the AxF materials and is based on the Geisler-Moroder variation of Ward (http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.169.9908&rep=rep1&type=pdf)
            //
            void SampleWardDir( real2   u,
                                real3   V,
                                real3x3 localToWorld,
                                real    roughness,
                            out real3   L,
                            out real    NdotL,
                            out real    NdotH,
                            out real    VdotH )
            {
                // Ward NDF sampling (eqs. 6 & 7 from above paper)
                real    tanTheta = roughness * sqrt( -log( max( 1e-6, u.x ) ) );
                real    phi      = TWO_PI * u.y;

                real    cosTheta = rsqrt( 1 + Sq( tanTheta ) );
                real3   localH = SphericalToCartesian( phi, cosTheta );

                NdotH = cosTheta;

                real3   localV = mul( V, transpose(localToWorld) );
                VdotH  = saturate( dot( localV, localH ) );

                // Compute { localL = reflect(-localV, localH) }
                real3   localL = -localV + 2.0 * VdotH * localH;
                NdotL = localL.z;

                L = mul( localL, localToWorld );
            }

            // weightOverPdf returns the weight (without the Fresnel term) over pdf. Fresnel term must be applied by the caller.
            void ImportanceSampleWard(  real2   u,
                                        real3   V,
                                        real3x3 localToWorld,
                                        real    roughness,
                                        real    NdotV,
                                    out real3   L,
                                    out real    VdotH,
                                    out real    NdotL,
                                    out real    weightOverPdf)
            {
                real    NdotH;
                SampleWardDir( u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH );

                // Importance sampling weight for each sample (eq. 9 from Walter, 2005)
                // pdf = 1 / (4PI * a² * (L.H) * (H.N)^3) * exp( ((N.H)² - 1) / (a² * (N.H)²) )                 <= From Walter, eq. 24 pdf(H) = D(H) . (N.H)
                // fr = (F(N.H) * s) / (4PI * a² * (L.H)² * (H.N)^4) * exp( ((N.H)² - 1) / (a² * (N.H)²) )      <= Moroder-Geisler version
                // weight over pdf is:
                // weightOverPdf = fr * (N.V) / pdf = s * F(N.H) * (N.V) / ((L.H) * (N.H))
                // s * F(N.H) is applied outside the function
                //
                weightOverPdf = NdotV / (VdotH * NdotH);
            }

            float4  IntegrateWardAndLambertDiffuseFGD( float3 V, float3 N, float roughness, uint sampleCount = 8192 ) {
                float   NdotV    = ClampNdotV( dot(N, V) );
                float4  acc      = float4(0.0, 0.0, 0.0, 0.0);
                float2  randNum  = InitRandom( V.xy * 0.5 + 0.5 );  // Add some jittering on Hammersley2d

                float3x3    localToWorld = GetLocalFrame( N );

                for ( uint i = 0; i < sampleCount; ++i ) {
                    float2  u = frac( randNum + Hammersley2d( i, sampleCount ) );

                    float   VdotH;
                    float   NdotL;
                    float   weightOverPdf;

                    float3  L; // Unused
                    ImportanceSampleWard(   u, V, localToWorld, roughness, NdotV,
                                            L, VdotH, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        // Integral{BSDF * <N,L> dw} =
                        // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                        // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                        // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                        acc.x += weightOverPdf * pow( 1 - VdotH, 5 );
                        acc.y += weightOverPdf;
                    }

                    // Regular Lambert
                    ImportanceSampleLambert( u, localToWorld, L, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        acc.z += LambertNoPI() * weightOverPdf;
                    }
                }

                acc /= sampleCount;

                return acc;
            }

            // ==============================================================================================
            //
            struct Attributes {
                uint vertexID : SV_VertexID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float   NdotV               = input.texCoord.x;
                float   perceptualRoughness = input.texCoord.y;
                float3  V                   = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3  N                   = float3(0.0, 0.0, 1.0);

                float4 preFGD = IntegrateWardAndLambertDiffuseFGD( V, N, PerceptualRoughnessToRoughness(perceptualRoughness) );

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
