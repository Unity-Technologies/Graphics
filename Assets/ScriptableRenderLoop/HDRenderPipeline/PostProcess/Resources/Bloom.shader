Shader "Hidden/HDRenderPipeline/Bloom"
{
    Properties
    {
        _MainTex ("", 2D) = "" {}
        _BaseTex ("", 2D) = "" {}
        _AutoExposure ("", 2D) = "" {}
    }

    HLSLINCLUDE

        #pragma target 4.5
        #include "Common.hlsl"
        #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"
        #include "Bloom.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER2D(sampler_MainTex);
        float4 _MainTex_TexelSize;

        TEXTURE2D(_BaseTex);
        SAMPLER2D(sampler_BaseTex);
        float4 _BaseTex_TexelSize;

        TEXTURE2D(_AutoExposure);
        SAMPLER2D(sampler_AutoExposure);

        float _Threshold;
        float3 _Curve;
        float _SampleScale;

        struct Attributes
        {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct VaryingsMultitex
        {
            float4 vertex : SV_POSITION;
            float2 texcoordMain : TEXCOORD0;
            float2 texcoordBase : TEXCOORD1;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = TransformWorldToHClip(input.vertex);
            output.texcoord = input.texcoord.xy;
            return output;
        }

        VaryingsMultitex VertMultitex(Attributes input)
        {
            VaryingsMultitex o;
            o.vertex = TransformWorldToHClip(input.vertex);
            o.texcoordMain = input.texcoord.xy;
            o.texcoordBase = o.texcoordMain;

        #if UNITY_UV_STARTS_AT_TOP
            if (_BaseTex_TexelSize.y < 0.0)
                o.texcoordMain.y = 1.0 - o.texcoordMain.y;
        #endif

            return o;
        }

        float4 FetchAutoExposed(TEXTURE2D_ARGS(tex, texSampler), float2 uv)
        {
            float autoExposure = 1.0;

        #if EYE_ADAPTATION
            autoExposure = SAMPLE_TEXTURE2D(_AutoExposure, sampler_AutoExposure, uv).r;
        #endif

            return SAMPLE_TEXTURE2D(tex, texSampler, uv) * autoExposure;
        }

        float4 FragPrefilter(Varyings i) : SV_Target
        {
            float2 uv = i.texcoord;

            float4 s0 = min(65504.0, FetchAutoExposed(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), uv));
            float3 m = s0.rgb;

            // Pixel brightness
            float br = max(m.r, max(m.g, m.r));

            // Under-threshold part: quadratic curve
            float rq = clamp(br - _Curve.x, 0.0, _Curve.y);
            rq = _Curve.z * rq * rq;

            // Combine and apply the brightness response curve.
            m *= max(rq, br - _Threshold) / max(br, 1e-5);

            return float4(m, 0.0);
        }

        float4 FragDownsample1(Varyings i) : SV_Target
        {
            return float4(DownsampleFilter(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy), 0.0);
        }

        float4 FragDownsample2(Varyings i) : SV_Target
        {
            return float4(DownsampleFilter(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy), 0.0);
        }

        float4 FragUpsample(VaryingsMultitex i) : SV_Target
        {
            float3 base = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, i.texcoordBase).rgb;
            float3 blur = UpsampleFilter(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoordMain, _MainTex_TexelSize.xy, _SampleScale);
            return float4(base + blur, 0.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma multi_compile __ EYE_ADAPTATION
                #pragma vertex Vert
                #pragma fragment FragPrefilter

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment FragDownsample1

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment FragDownsample2

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertMultitex
                #pragma fragment FragUpsample

            ENDHLSL
        }
    }

    Fallback Off
}
