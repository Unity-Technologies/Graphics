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
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

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
