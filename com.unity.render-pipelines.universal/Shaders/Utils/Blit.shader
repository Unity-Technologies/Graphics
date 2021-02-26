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
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile _ _DEBUG_SHADER

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float _RangeMinimum;
            float _RangeMaximum;
            int _HighlightOutOfRangeAlpha;

            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);

             	#ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
             	#endif

                #if defined(_DEBUG_SHADER)
                if(_DebugValidationMode == DEBUGVALIDATIONMODE_HIGHLIGHT_NAN_INF_NEGATIVE)
                {
                    if (isnan(col.r) || isnan(col.g) || isnan(col.b) || isnan(col.a))
                    {
                        return half4(1, 0, 0, 1);
                    }
                    else if (isinf(col.r) || isinf(col.g) || isinf(col.b) || isinf(col.a))
                    {
                        return half4(0, 1, 0, 1);
                    }
                    else if (col.r < 0 || col.g < 0 || col.b < 0 || col.a < 0)
                    {
                        return half4(0, 0, 1, 1);
                    }
                    else
                    {
                        return half4(LinearRgbToLuminance(col.rgb).rrr, 1);
                    }
                }
                else if(_DebugValidationMode == DEBUGVALIDATIONMODE_HIGHLIGHT_OUTSIDE_OF_RANGE)
                {
                    if(col.r < _RangeMinimum || col.g < _RangeMinimum || col.b < _RangeMinimum ||
                        (_HighlightOutOfRangeAlpha && (col.a < _RangeMinimum)))
                    {
                        return _DebugValidateBelowMinThresholdColor;
                    }
                    else if(col.r > _RangeMaximum || col.g > _RangeMaximum || col.b > _RangeMaximum ||
                            (_HighlightOutOfRangeAlpha && (col.a > _RangeMaximum)))
                    {
                        return _DebugValidateAboveMaxThresholdColor;
                    }
                    else
                    {
                        return half4(LinearRgbToLuminance(col.rgb).rrr, 1);
                    }
                }
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
