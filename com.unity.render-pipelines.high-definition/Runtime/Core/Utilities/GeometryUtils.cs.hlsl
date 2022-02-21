//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef GEOMETRYUTILS_CS_HLSL
#define GEOMETRYUTILS_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.FrustumGPU
// PackingRules = Exact
struct FrustumGPU
{
    float3 normal0;
    float dist0;
    float3 normal1;
    float dist1;
    float3 normal2;
    float dist2;
    float3 normal3;
    float dist3;
    float3 normal4;
    float dist4;
    float3 normal5;
    float dist5;
    float4 corner0;
    float4 corner1;
    float4 corner2;
    float4 corner3;
    float4 corner4;
    float4 corner5;
    float4 corner6;
    float4 corner7;
};

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
