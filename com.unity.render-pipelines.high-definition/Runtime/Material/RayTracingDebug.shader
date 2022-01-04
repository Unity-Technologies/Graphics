Shader "HDRP/RayTracingDebug"
{
    Properties
    {
    }

    HLSLINCLUDE

    #pragma target 4.5

    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline"="HDRenderPipeline" }
        Pass
        {
            Name "DebugDXR"
            Tags{ "LightMode" = "DebugDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps5
            #pragma raytracing surface_shader

            #define SHADERPASS SHADERPASS_RAYTRACING_DEBUG

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingDebug.hlsl"

            ENDHLSL
        }
    }
}
