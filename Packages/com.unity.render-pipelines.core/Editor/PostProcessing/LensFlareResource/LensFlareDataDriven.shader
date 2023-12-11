Shader "Hidden/Core/LensFlareDataDrivenPreview2"
{
    SubShader
    {
        // Additive
        Pass
        {
            Name "LensFlareAdditive"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }
            LOD 100

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #define FLARE_ADDITIVE_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Screen
        Pass
        {
            Name "LensFlareScreen"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 100

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #define FLARE_SCREEN_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Premultiply
        Pass
        {
            Name "LensFlarePremultiply"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 100

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #define FLARE_PREMULTIPLIED_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Lerp
        Pass
        {
            Name "LensFlareLerp"
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 100

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #define FLARE_LERP_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
    }
}
