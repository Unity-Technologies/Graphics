Shader "Hidden/SDFRP/DepthOfField"
{
 HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.sdf/Shaders/PostProcessing/FullScreen.hlsl"
        #include "Packages/com.unity.render-pipelines.sdf/Shaders/PostProcessing/DepthOfField.hlsl"
ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "SDFRP" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "SDF Depth Of Field"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment DepthOfField
            ENDHLSL
        }

    }
}
