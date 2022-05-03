Shader "Hidden/Universal Render Pipeline/DrawRenderingLayer"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DrawRenderingLayers"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float4 _RenderingLayersColors[32];

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                uint2 positionSS = uint2(input.positionCS.xy);
                uint sceneRenderingLayer = LoadSceneRenderingLayer(positionSS);
                uint renderingLayerColorIndex = clamp(log2(sceneRenderingLayer), 0, 31);

                float4 color = sceneRenderingLayer == 0 ? 0 : _RenderingLayersColors[renderingLayerColorIndex];
                color.a = 1;

                return color;
            }
            ENDHLSL
        }
    }
}
