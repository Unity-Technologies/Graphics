#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

#if (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD) || (SHADERPASS == SHADERPASS_PATH_TRACING)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#endif

#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT

#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD) || (SHADERPASS == SHADERPASS_PATH_TRACING)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
    #define HAS_LIGHTLOOP
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

#if (SHADERPASS != SHADERPASS_PATH_TRACING)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitPathTracing.hlsl"
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitData.hlsl"
