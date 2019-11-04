//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDSHADOWMANAGER_CS_HLSL
#define HDSHADOWMANAGER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.HDShadowData
// PackingRules = Exact
struct HDShadowData
{
    float3 rot0;
    float3 rot1;
    float3 rot2;
    float3 pos;
    float4 proj;
    float2 atlasOffset;
    float worldTexelSize;
    int _pad0;
    real4 zBufferParam;
    float4 shadowMapSize;
    float normalBias;
    float constantBias;
    float _pad1;
    float _pad2;
    real4 shadowFilterParams0;
    float3 cacheTranslationDelta;
    float _padding1;
    float4x4 shadowToWorld;
};

// Generated from UnityEngine.Rendering.HighDefinition.HDDirectionalShadowData
// PackingRules = Exact
struct HDDirectionalShadowData
{
    float4 sphereCascades[4];
    real4 cascadeDirection;
    real cascadeBorders[4];
};


#endif
