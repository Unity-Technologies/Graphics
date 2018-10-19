//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDLIGHTFLAG_CS_HLSL
#define HDLIGHTFLAG_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDLightFlag+LightClipPlaneData
// PackingRules = Exact
struct LightClipPlaneData
{
    float4 plane;
    float feather;
    float3 unused;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDLightFlag+LightClipPlaneData
//
float4 GetPlane(LightClipPlaneData value)
{
    return value.plane;
}
float GetFeather(LightClipPlaneData value)
{
    return value.feather;
}
float3 GetUnused(LightClipPlaneData value)
{
    return value.unused;
}


#endif
