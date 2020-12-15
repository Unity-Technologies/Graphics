//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef GEOMETRYUTILS_CS_HLSL
#define GEOMETRYUTILS_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.OrientedBBox
// PackingRules = Exact
struct OrientedBBox
{
    float3 right;
    float extentX;
    float3 up;
    float extentY;
    float3 center;
    float extentZ;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.OrientedBBox
//
float3 GetRight(OrientedBBox value)
{
    return value.right;
}
float GetExtentX(OrientedBBox value)
{
    return value.extentX;
}
float3 GetUp(OrientedBBox value)
{
    return value.up;
}
float GetExtentY(OrientedBBox value)
{
    return value.extentY;
}
float3 GetCenter(OrientedBBox value)
{
    return value.center;
}
float GetExtentZ(OrientedBBox value)
{
    return value.extentZ;
}

#endif
