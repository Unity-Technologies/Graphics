Shader "Hidden/HDRP/XRMirrorView"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        HLSLINCLUDE
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            #pragma multi_compile_local_fragment _ HDR_COLORSPACE_CONVERSION_AND_ENCODING
            #pragma multi_compile_fragment _ DISABLE_TEXTURE2D_X_ARRAY
        ENDHLSL

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
