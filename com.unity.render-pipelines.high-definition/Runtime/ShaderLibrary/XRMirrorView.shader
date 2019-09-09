Shader "Hidden/HDRP/XRMirrorView"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        HLSLINCLUDE
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        ENDHLSL

        // 0: TEXTURE2D
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear

                #define DISABLE_TEXTURE2D_X_ARRAY 1
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/XRMirrorView.hlsl"
            ENDHLSL
        }

        // 1: TEXTURE2D_ARRAY
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear

                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/XRMirrorView.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
