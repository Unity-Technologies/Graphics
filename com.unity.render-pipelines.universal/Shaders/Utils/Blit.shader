Shader "Hidden/Universal Render Pipeline/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile _ _DEBUG_HIGHLIGHT_NAN_INF_NEGATIVE_PIXELS
            #pragma multi_compile _ _DEBUG_HIGHLIGHT_PIXELS_OUTSIDE_RANGE
            #pragma multi_compile _ _DEBUG_HIGHLIGHT_ALPHA_OUTSIDE_RANGE

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float _RangeMinimum;
            float _RangeMaximum;

            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);

            half4 Fragment(FullscreenVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);

             	#ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
             	#endif

                #ifdef _DEBUG_HIGHLIGHT_NAN_INF_NEGATIVE_PIXELS
                {
                    if (isnan(col.r) || isnan(col.g) || isnan(col.b) || isnan(col.a))
                        return half4(1, 0, 0, 1);

                    if (isinf(col.r) || isinf(col.g) || isinf(col.b) || isinf(col.a))
                        return half4(0, 1, 0, 1);

                    if (col.r < 0 || col.g < 0 || col.b < 0 || col.a < 0)
                        return half4(0, 0, 1, 1);

                    col = half4(dot(col.xyz, half3(0.2126, 0.7152, 0.0722)).xxx, 1);
                }
                #endif

                #ifdef _DEBUG_HIGHLIGHT_PIXELS_OUTSIDE_RANGE
                {
                    if (col.r < _RangeMinimum || col.g < _RangeMinimum || col.b < _RangeMinimum
                        #ifdef _DEBUG_HIGHLIGHT_ALPHA_OUTSIDE_RANGE
                        || col.a < _RangeMinimum
                        #endif
                        )
                        return half4(1, 0, 0, 1);

                    if (col.r > _RangeMaximum || col.g > _RangeMaximum || col.b > _RangeMaximum
                        #ifdef _DEBUG_HIGHLIGHT_ALPHA_OUTSIDE_RANGE
                        || col.a > _RangeMaximum
                        #endif
                        )
                        return half4(0, 1, 0, 1);

                    col = half4(dot(col.xyz, half3(0.2126, 0.7152, 0.0722)).xxx, 1);
                }
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
