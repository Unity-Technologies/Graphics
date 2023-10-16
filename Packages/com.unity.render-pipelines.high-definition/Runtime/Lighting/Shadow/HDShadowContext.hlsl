#ifndef HD_SHADOW_CONTEXT_HLSL
#define HD_SHADOW_CONTEXT_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowManager.cs.hlsl"

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

TEXTURE2D(_ShadowmapAtlas);
TEXTURE2D(_CachedShadowmapAtlas);
TEXTURE2D(_ShadowmapCascadeAtlas);
TEXTURE2D(_ShadowmapAreaAtlas);
TEXTURE2D(_CachedAreaLightShadowmapAtlas);

StructuredBuffer<HDShadowData>              _HDShadowDatas;
// Only the first element is used since we only support one directional light
StructuredBuffer<HDDirectionalShadowData>   _HDDirectionalShadowData;

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
