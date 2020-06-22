//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef ELLIPSOIDOCCLUDER_CS_HLSL
#define ELLIPSOIDOCCLUDER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.EllipsoidOccluderData
// PackingRules = Exact
struct EllipsoidOccluderData
{
    float3 position;
    float radius;
    float3 direction;
    float scaling;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.EllipsoidOccluderData
//
float3 GetPosition(EllipsoidOccluderData value)
{
    return value.position;
}
float GetRadius(EllipsoidOccluderData value)
{
    return value.radius;
}
float3 GetDirection(EllipsoidOccluderData value)
{
    return value.direction;
}
float GetScaling(EllipsoidOccluderData value)
{
    return value.scaling;
}

#endif
