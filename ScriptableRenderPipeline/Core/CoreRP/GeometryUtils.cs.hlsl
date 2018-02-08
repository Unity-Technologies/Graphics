//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef GEOMETRYUTILS_CS_HLSL
#define GEOMETRYUTILS_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.OrientedBBox
// PackingRules = Exact
struct OrientedBBox
{
    float4 center;
    float4 right;
    float4 up;
    float4 forward;
};

//
// Accessors for UnityEngine.Experimental.Rendering.OrientedBBox
//
float4 GetCenter(OrientedBBox value)
{
	return value.center;
}
float4 GetRight(OrientedBBox value)
{
	return value.right;
}
float4 GetUp(OrientedBBox value)
{
	return value.up;
}
float4 GetForward(OrientedBBox value)
{
	return value.forward;
}


#endif
