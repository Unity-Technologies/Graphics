#ifndef HD_SHADOW_CONTEXT_HLSL
#define HD_SHADOW_CONTEXT_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowManager.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"

// Say to LightloopDefs.hlsl that we have a sahdow context struct define
#define HAVE_HD_SHADOW_CONTEXT

struct HDShadowContext
{
    StructuredBuffer<HDShadowData>  shadowDatas;
    HDDirectionalShadowData         directionalShadowData;
#ifdef SHADOWS_SHADOWMASK
    int shadowSplitIndex;
    float fade;
#endif 
};

// HD shadow sampling bindings
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAlgorithms.hlsl"

GLOBAL_TEXTURE2D(_ShadowmapAtlas, RAY_TRACING_SHADOWMAP_ATLAS_REGISTER);
GLOBAL_TEXTURE2D(_CachedShadowmapAtlas, RAY_TRACING_CACHED_SHADOWMAP_ATLAS_REGISTER);
GLOBAL_TEXTURE2D(_ShadowmapCascadeAtlas, RAY_TRACING_SHADOWMAP_CASCADE_ATLAS_REGISTER);
GLOBAL_TEXTURE2D(_ShadowmapAreaAtlas, RAY_TRACING_SHADOWMAP_AREA_ATLAS_REGISTER);
GLOBAL_TEXTURE2D(_CachedAreaLightShadowmapAtlas, RAY_TRACING_CACHED_AREA_LIGHT_SHADOWMAP_ATLAS_REGISTER);

GLOBAL_RESOURCE(StructuredBuffer<HDShadowData>, _HDShadowDatas, RAY_TRACING_HD_SHADOW_DATAS_REGISTER);

// Only the first element is used since we only support one directional light
GLOBAL_RESOURCE(StructuredBuffer<HDDirectionalShadowData>, _HDDirectionalShadowData, RAY_TRACING_HD_DIRECTIONAL_SHADOW_DATA_REGISTER);

HDShadowContext InitShadowContext()
{
    HDShadowContext         sc;

    sc.shadowDatas = _HDShadowDatas;
    sc.directionalShadowData = _HDDirectionalShadowData[0];
#ifdef SHADOWS_SHADOWMASK
    sc.shadowSplitIndex = -1;
    sc.fade = 0.0;
#endif

    return sc;
}

#endif // HD_SHADOW_CONTEXT_HLSL
