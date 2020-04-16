//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTFLAG_CS_HLSL
#define LIGHTFLAG_CS_HLSL
// Generated from UnityEngine.Rendering.LightFlag+LightFlagData
// PackingRules = Exact
struct LightFlagData
{
    float4 plane;
    float feather;
    float3 unused;
};

//
// Accessors for UnityEngine.Rendering.LightFlag+LightFlagData
//
float4 GetPlane(LightFlagData value)
{
    return value.plane;
}
float GetFeather(LightFlagData value)
{
    return value.feather;
}
float3 GetUnused(LightFlagData value)
{
    return value.unused;
}

#endif
