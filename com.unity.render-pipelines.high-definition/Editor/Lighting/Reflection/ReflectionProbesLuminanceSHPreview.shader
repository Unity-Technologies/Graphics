Shader "Hidden/Debug/ReflectionProbeLuminanceSHPreview"
{
    Properties
    {
        _LuminanceSHEnabled("_LuminanceSHEnabled", Int) = 0
        _L0L1("_L0L1", Vector) = (0,0,0,0)
        _L2_1("_L2_1", Vector) = (0,0,0,0)
        _L2_2("_L2_2", Float) = 0.0
        _Exposure("_Exposure", Range(-10.0,10.0)) = 0.0

    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Transparent" }
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward" }

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD0;
            };

            int _LuminanceSHEnabled;
            float4 _L0L1;
            float4 _L2_1;
            float _L2_2;
            float _Exposure;

            v2f vert(appdata v)
            {
                v2f o;
                // Transform local to world before custom vertex code
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                return o;
            }

            // Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
            float SHEvalLinearL0L1Luminance(float3 N, float4 shA)
            {
                // Linear (L1) + constant (L0) polynomial terms
                return dot(shA.xyz, N) + shA.w;
            }

            float SHEvalLinearL2Luminance(float3 N, float4 shB, float shC)
            {
                // 4 of the quadratic (L2) polynomials
                float4 vB = N.xyzz * N.yzzx;
                float x2 = dot(shB, vB);

                // Final (5th) quadratic (L2) polynomial
                float vC = N.x * N.x - N.y * N.y;
                float x3 = shC * vC;

                return x2 + x3;
            }

            float SampleSH9Luminance(float3 N, float4 shA, float4 shB, float shC)
            {
                // Linear + constant polynomial terms
                float res = SHEvalLinearL0L1Luminance(N, shA);

                // Quadratic polynomials
                res += SHEvalLinearL2Luminance(N, shB, shC);

                return res;
            }

            float GetReflectionProbeNormalizationFactor(float3 sampleDirectionWS, float4 reflProbeSHL0L1, float4 reflProbeSHL2_1, float reflProbeSHL2_2)
            {
                // SHEvalLinearL0L1() expects coefficients in float4 shAr, float4 shAg, float4 shAb vectors whos channels are laid out {x, y, z, DC}
                float4 shALuminance = float4(reflProbeSHL0L1.w, reflProbeSHL0L1.y, reflProbeSHL0L1.z, reflProbeSHL0L1.x);

                float4 shBLuminance = reflProbeSHL2_1;
                float shCLuminance = reflProbeSHL2_2;

                // // Normalize DC term:
                shALuminance.w -= shBLuminance.z;

                // // Normalize Quadratic term:
                shBLuminance.z *= 3.0f;

                return  SampleSH9Luminance(sampleDirectionWS, shALuminance, shBLuminance, shCLuminance);
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normalWS);

                float luminanceSH = GetReflectionProbeNormalizationFactor(N, _L0L1, _L2_1, _L2_2);
                float4 colorLuminanceSH = (_LuminanceSHEnabled > 0)
                    ? (luminanceSH < 0.0f)
                        ? float4(1.0f, 0.0f, 0.0f, 1.0f)
                        : float4(luminanceSH, luminanceSH, luminanceSH, 1.0f)
                    : float4(1.0f, 0.0f, 1.0f, 1.0f);

                float4 color = colorLuminanceSH;

                color.rgb = color.rgb * exp2(_Exposure) * GetCurrentExposureMultiplier();

                return float4(color);
            }
            ENDHLSL
        }
    }
}
