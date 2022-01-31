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
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_local_fragment _ _OVERRIDE_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);

            #if _OVERRIDE_ALPHA
            TEXTURE2D_X(_AlphaOverrideTexture);
            SAMPLER(sampler_AlphaOverrideTexture);
            #endif

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;

                half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv);

                #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
                #endif

                #if defined(DEBUG_DISPLAY)
                half4 debugColor = 0;

                if(CanDebugOverrideOutputColor(col, uv, debugColor))
                {
                    return debugColor;
                }
                #endif

                #if _OVERRIDE_ALPHA
                col.a = SAMPLE_TEXTURE2D_X(_AlphaOverrideTexture, sampler_AlphaOverrideTexture, uv).x;
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
