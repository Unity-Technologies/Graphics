Shader "Hidden/Lightweight Render Pipeline/Bloom"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
    }

    HLSLINCLUDE

        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_MainTex);
        TEXTURE2D(_MainTexLowMip);

        SAMPLER(sampler_LinearClamp);

        float4 _MainTex_TexelSize;
        float4 _MainTexLowMip_TexelSize;

        float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

        #define Scatter             _Params.x
        #define ClampMax            _Params.y
        #define Threshold           _Params.z
        #define ThresholdKnee       _Params.w

        half4 FragPrefilter(Varyings input) : SV_Target
        {
            half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv).xyz;

            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            // User controlled clamp to limit crazy high broken spec
            color = min(ClampMax, color);

            // Thresholding
            half brightness = Max3(color.r, color.g, color.b);
            half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
            softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
            half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
            color *= multiplier;

            return half4(color, 1.0);
        }

        half4 FragBlurH(Varyings input) : SV_Target
        {
            float texelSize = _MainTex_TexelSize.x * 2.0;

            // 9-tap gaussian blur on the downsampled source
            half3 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(texelSize * 4.0, 0.0)).xyz;
            half3 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(texelSize * 3.0, 0.0)).xyz;
            half3 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(texelSize * 2.0, 0.0)).xyz;
            half3 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(texelSize * 1.0, 0.0)).xyz;
            half3 c4 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv                               ).xyz;
            half3 c5 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(texelSize * 1.0, 0.0)).xyz;
            half3 c6 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(texelSize * 2.0, 0.0)).xyz;
            half3 c7 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(texelSize * 3.0, 0.0)).xyz;
            half3 c8 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(texelSize * 4.0, 0.0)).xyz;

            half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
                        + c4 * 0.22702703
                        + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

            return half4(color, 1.0);
        }

        half4 FragBlurV(Varyings input) : SV_Target
        {
            float texelSize = _MainTex_TexelSize.y;

            // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
            half3 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(0.0, texelSize * 3.23076923)).xyz;
            half3 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv - float2(0.0, texelSize * 1.38461538)).xyz;
            half3 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv                                      ).xyz;
            half3 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(0.0, texelSize * 1.38461538)).xyz;
            half3 c4 = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv + float2(0.0, texelSize * 3.23076923)).xyz;

            half3 color = c0 * 0.07027027 + c1 * 0.31621622
                        + c2 * 0.22702703
                        + c3 * 0.31621622 + c4 * 0.07027027;

            return half4(color, 1.0);
        }

        half4 FragUpsample(Varyings input) : SV_Target
        {
            half3 highMip = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv).xyz;

            #if FILTERING_HQ
            half3 lowMip = SampleTexture2DBicubic(TEXTURE2D_ARGS(_MainTexLowMip, sampler_LinearClamp), input.uv, _MainTexLowMip_TexelSize.zwxy, (1.0).xx, 0).xyz;
            #else
            half3 lowMip = SAMPLE_TEXTURE2D(_MainTexLowMip, sampler_LinearClamp, input.uv).xyz;
            #endif

            half3 color = lerp(highMip, lowMip, Scatter);
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
            Name "Bloom Prefilter"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPrefilter
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurH
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Vertical"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurV
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Upsample"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragUpsample
                #pragma multi_compile_local _ FILTERING_HQ
            ENDHLSL
        }
    }
}
