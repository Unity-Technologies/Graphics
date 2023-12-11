Shader "Hidden/Core/LensFlareDataDrivenPreview"
{
    // Note: For UI as we don't have command buffer for UI we need to have one shader per usage
    // instead of permutation like for rendering

    // Keep the order as the same order of SRPLensFlareType

    SubShader
    {
        // Not Inverted
        Pass
        {
            Name "FlarePreviewNotInverted"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Inverse
        Pass
        {
            Name "FlarePreviewInverted"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define FLARE_PREVIEW
            #define FLARE_INVERSE_SDF
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
    }
}
