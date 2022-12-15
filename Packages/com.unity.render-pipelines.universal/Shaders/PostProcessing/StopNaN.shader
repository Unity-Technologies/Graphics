Shader "Hidden/Universal Render Pipeline/Stop NaN"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma exclude_renderers gles
        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define NAN_COLOR half3(0.0, 0.0, 0.0)

        half4 FragStopNaN(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).xyz;

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
                #pragma vertex Vert
                #pragma fragment FragStopNaN
            ENDHLSL
        }
    }
}
