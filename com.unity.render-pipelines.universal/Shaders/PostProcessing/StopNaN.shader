Shader "Hidden/Universal Render Pipeline/Stop NaN"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL
        #pragma exclude_renderers gles
        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        #define NAN_COLOR half3(0.0, 0.0, 0.0)

        TEXTURE2D_X(_SourceTex);

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_PointClamp, UnityStereoTransformScreenSpaceTex(input.uv)).xyz;

            if (AnyIsNaN(color) || AnyIsInf(color))
                color = NAN_COLOR;

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Stop NaN"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
