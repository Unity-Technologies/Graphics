Shader "Hidden/Universal Render Pipeline/DBufferClear"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DBufferClear"
            ZTest Always
            ZWrite Off
            Cull Off

            /*ColorMask 0 0
            ColorMask RGBA 1
            ColorMask 0 2*/

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment DECALS_1RT DECALS_2RT DECALS_3RT

            #define _USE_DRAW_PROCEDURAL 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/Decal.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/Decal.hlsl"

            void Fragment(
                Varyings input,
                OUTPUT_DBUFFER(outDBuffer))
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                outDBuffer0 = half4(0, 0, 0, 1);
#if defined(DECALS_3RT) || defined(DECALS_2RT)
                outDBuffer1 = half4(0.5f, 0.5f, 0.5f, 1);
#endif
#if defined(DECALS_3RT)
                outDBuffer2 = half4(0, 0, 0, 1);
#endif
            }
            ENDHLSL
        }
    }
}
