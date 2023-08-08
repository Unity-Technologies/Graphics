Shader "Hidden/HDRP/GPUInlineDebugDrawer"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // WS
        Pass
        {
            Name "LineWSNoDepthTest"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawer.hlsl"

            #define GPU_INLINE_DEBUG_DRAWER_WS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawerCommon.hlsl"

            ENDHLSL
        }

        // CS
        Pass
        {
            Name "LineCSNoDepthTest"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawer.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawerCommon.hlsl"

            ENDHLSL
        }

        // CS
        Pass
        {
            Name "PlotRingBufferPass"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma target 5.0
            #pragma vertex vertPlotRingBuffer
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawer.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawerCommon.hlsl"

            ENDHLSL
        }
    }
}
