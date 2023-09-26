Shader "Hidden/Universal Render Pipeline/XR/XRMirrorView"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
            #pragma exclude_renderers gles
        ENDHLSL

        // 0: TEXTURE2D
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
                #pragma multi_compile_fragment _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER
                #pragma multi_compile_local_fragment _ HDR_COLORSPACE_CONVERSION_AND_ENCODING
                // Foveated rendering currently not supported in dxc on metal
                #pragma never_use_dxc metal

                #define SRC_TEXTURE2D_X_ARRAY 0
                #include "Packages/com.unity.render-pipelines.universal/Shaders/XR/XRMirrorView.hlsl"
            ENDHLSL
        }

        // 1: TEXTURE2D_ARRAY
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
                #pragma multi_compile_fragment _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER
                #pragma multi_compile_local_fragment _ HDR_COLORSPACE_CONVERSION_AND_ENCODING
                // Foveated rendering currently not supported in dxc on metal
                #pragma never_use_dxc metal

                #define SRC_TEXTURE2D_X_ARRAY 1
                #include "Packages/com.unity.render-pipelines.universal/Shaders/XR/XRMirrorView.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
