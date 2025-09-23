Shader "Hidden/HDRP/LensFlareDataDriven"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // Additive
        Pass
        {
            Name "LensFlareAdditive"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #pragma multi_compile _ FLARE_HAS_OCCLUSION

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define HDRP_FLARE
            #define FLARE_ADDITIVE_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Screen
        Pass
        {
            Name "LensFlareScreen"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One OneMinusSrcColor
            BlendOp Max
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #pragma multi_compile _ FLARE_HAS_OCCLUSION

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define HDRP_FLARE
            #define FLARE_SCREEN_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Premultiply
        Pass
        {
            Name "LensFlarePremultiply"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #pragma multi_compile _ FLARE_HAS_OCCLUSION

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define HDRP_FLARE
            #define FLARE_PREMULTIPLIED_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // Lerp
        Pass
        {
            Name "LensFlareLerp"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ FLARE_INVERSE_SDF

            #pragma multi_compile _ FLARE_HAS_OCCLUSION

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define HDRP_FLARE
            #define FLARE_LERP_BLEND
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
        // OcclusionOnly
        Pass
        {
            Name "LensFlareOcclusion"

            Blend Off
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

            #pragma target 5.0
            #pragma vertex vertOcclusion
            #pragma fragment fragOcclusion

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #define HDRP_FLARE
            #define FLARE_COMPUTE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon.hlsl"

            ENDHLSL
        }
    }
}
