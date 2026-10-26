Shader "Hidden/Universal Render Pipeline/UIBackdropFilterComposite"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Composite"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_BlitTexture_Before);

            // Per-channel difference threshold for considering a pixel "touched by UI". Small enough
            // to catch any meaningful UI contribution; large enough to avoid float-precision noise.
            #define BACKDROP_DIFF_EPSILON 0.001

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 cur = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord).rgb;
                half3 before = SAMPLE_TEXTURE2D_X(_BlitTexture_Before, sampler_PointClamp, input.texcoord).rgb;
                half3 diff = abs(cur - before);
                half maxDiff = max(diff.x, max(diff.y, diff.z));
                clip(maxDiff - BACKDROP_DIFF_EPSILON);
                return half4(cur, 1.0);
            }
            ENDHLSL
        }
    }
}
