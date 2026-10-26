Shader "Hidden/Test/283_DisplayAlpha"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "DisplayAlpha"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Visualizes the alpha channel of the camera color as grayscale, so the graphics
            // test compares the upscaled alpha instead of the composited color.
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half alpha = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord.xy).a;
                return half4(alpha.xxx, 1);
            }
            ENDHLSL
        }
    }
}
