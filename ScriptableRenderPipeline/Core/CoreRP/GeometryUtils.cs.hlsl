//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef GEOMETRYUTILS_CS_HLSL
#define GEOMETRYUTILS_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.OrientedBBox
// PackingRules = Exact
struct OrientedBBox
{
    float3 center;
    float extentX;
    float3 right;
    float extentY;
    float3 up;
    float extentZ;
};

//
// Accessors for UnityEngine.Experimental.Rendering.OrientedBBox
//
float3 GetCenter(OrientedBBox value)
{
	return value.center;
}
float GetExtentX(OrientedBBox value)
{
	return value.extentX;
}
float3 GetRight(OrientedBBox value)
{
	return value.right;
}
float GetExtentY(OrientedBBox value)
{
	return value.extentY;
}
float3 GetUp(OrientedBBox value)
{
	return value.up;
}
float GetExtentZ(OrientedBBox value)
{
	return value.extentZ;
}


#endif
