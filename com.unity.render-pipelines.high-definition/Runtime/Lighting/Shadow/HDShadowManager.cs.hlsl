//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDSHADOWMANAGER_CS_HLSL
#define HDSHADOWMANAGER_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
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
    float4 zBufferParam;
    float4 shadowMapSize;
    float normalBias;
    float constantBias;
    float _pad1;
    float _pad2;
    float4 shadowFilterParams0;
    float4 cacheTranslationDelta;
    float4x4 shadowToWorld;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
// PackingRules = Exact
struct HDDirectionalShadowData
{
    float4 sphereCascades[4];
    float4 cascadeDirection;
    float cascadeBorders[4];
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
//
float3 GetRot0(HDShadowData value)
{
    return value.rot0;
}
float3 GetRot1(HDShadowData value)
{
    return value.rot1;
}
float3 GetRot2(HDShadowData value)
{
    return value.rot2;
}
float3 GetPos(HDShadowData value)
{
    return value.pos;
}
float4 GetProj(HDShadowData value)
{
    return value.proj;
}
float2 GetAtlasOffset(HDShadowData value)
{
    return value.atlasOffset;
}
float GetWorldTexelSize(HDShadowData value)
{
    return value.worldTexelSize;
}
int Get_pad0(HDShadowData value)
{
    return value._pad0;
}
float4 GetZBufferParam(HDShadowData value)
{
    return value.zBufferParam;
}
float4 GetShadowMapSize(HDShadowData value)
{
    return value.shadowMapSize;
}
float GetNormalBias(HDShadowData value)
{
    return value.normalBias;
}
float GetConstantBias(HDShadowData value)
{
    return value.constantBias;
}
float Get_pad1(HDShadowData value)
{
    return value._pad1;
}
float Get_pad2(HDShadowData value)
{
    return value._pad2;
}
float4 GetShadowFilterParams0(HDShadowData value)
{
    return value.shadowFilterParams0;
}
float4 GetCacheTranslationDelta(HDShadowData value)
{
    return value.cacheTranslationDelta;
}
float4x4 GetShadowToWorld(HDShadowData value)
{
    return value.shadowToWorld;
}
//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
//
float4 GetSphereCascades(HDDirectionalShadowData value, int index)
{
    return value.sphereCascades[index];
}
float4 GetCascadeDirection(HDDirectionalShadowData value)
{
    return value.cascadeDirection;
}
float GetCascadeBorders(HDDirectionalShadowData value, int index)
{
    return value.cascadeBorders[index];
}

#endif
