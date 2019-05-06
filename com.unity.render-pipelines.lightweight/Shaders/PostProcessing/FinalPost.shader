Shader "Hidden/Lightweight Render Pipeline/FinalPost"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
    }

    HLSLINCLUDE

        #pragma multi_compile_local _ _FXAA
        #pragma multi_compile_local _ _DITHERING
        
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_MainTex);
        TEXTURE2D(_BlueNoise_Texture);

        float4 _MainTex_TexelSize;
        float4 _Dithering_Params;

        #define DitheringScale          _Dithering_Params.xy
        #define DitheringOffset         _Dithering_Params.zw

        #define FXAA_SPAN_MAX           (8.0)
        #define FXAA_REDUCE_MUL         (1.0 / 8.0)
        #define FXAA_REDUCE_MIN         (1.0 / 128.0)

        half3 Fetch(float2 coords, float2 offset)
        {
            float2 uv = coords + offset;
            return SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv).xyz;
        }

        half3 Load(int2 icoords, int idx, int idy)
        {
            #if SHADER_API_GLES
            float2 uv = (icoords + int2(idx, idy)) * _MainTex_TexelSize.xy;
            return SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv).xyz;
            #else
            return LOAD_TEXTURE2D(_MainTex, clamp(icoords + int2(idx, idy), 0, _MainTex_TexelSize.zw - 1.0)).xyz;
            #endif
        }

        half4 Frag(Varyings input) : SV_Target
        {
            float2 positionNDC = input.uv;
            int2   positionSS  = input.uv * _MainTex_TexelSize.zw;

            half3 color = Load(positionSS, 0, 0).xyz;

            #if _FXAA
            {
                // Edge detection
                half3 rgbNW = Load(positionSS, -1, -1);
                half3 rgbNE = Load(positionSS,  1, -1);
                half3 rgbSW = Load(positionSS, -1,  1);
                half3 rgbSE = Load(positionSS,  1,  1);

                rgbNW = saturate(rgbNW);
                rgbNE = saturate(rgbNE);
                rgbSW = saturate(rgbSW);
                rgbSE = saturate(rgbSE);
                color = saturate(color);

                half lumaNW = Luminance(rgbNW);
                half lumaNE = Luminance(rgbNE);
                half lumaSW = Luminance(rgbSW);
                half lumaSE = Luminance(rgbSE);
                half lumaM = Luminance(color);

                float2 dir;
                dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
                dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));

                half lumaSum = lumaNW + lumaNE + lumaSW + lumaSE;
                float dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
                float rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);

                dir = min((FXAA_SPAN_MAX).xx, max((-FXAA_SPAN_MAX).xx, dir * rcpDirMin)) * _MainTex_TexelSize.xy;

                // Blur
                half3 rgb03 = Fetch(positionNDC, dir * (0.0 / 3.0 - 0.5));
                half3 rgb13 = Fetch(positionNDC, dir * (1.0 / 3.0 - 0.5));
                half3 rgb23 = Fetch(positionNDC, dir * (2.0 / 3.0 - 0.5));
                half3 rgb33 = Fetch(positionNDC, dir * (3.0 / 3.0 - 0.5));

                rgb03 = saturate(rgb03);
                rgb13 = saturate(rgb13);
                rgb23 = saturate(rgb23);
                rgb33 = saturate(rgb33);

                half3 rgbA = 0.5 * (rgb13 + rgb23);
                half3 rgbB = rgbA * 0.5 + 0.25 * (rgb03 + rgb33);

                half lumaB = Luminance(rgbB);

                half lumaMin = Min3(lumaM, lumaNW, Min3(lumaNE, lumaSW, lumaSE));
                half lumaMax = Max3(lumaM, lumaNW, Max3(lumaNE, lumaSW, lumaSE));

                color = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;
            }
            #endif

            #if _DITHERING
            {
                color = ApplyDithering(color, positionNDC, TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset);
            }
            #endif

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FinalPost"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
