#ifndef UNITY_COMMON_INCLUDED
#define UNITY_COMMON_INCLUDED

// Convention:
// space at the end of the variable name
// WS: world space
// VS: view space
// OS: object space
// HS: Homogenous clip space
// CS: clips space
// Example: NormalWS

// normalized / unormalized vector
// normalized direction are almost everywhere, we tag unormalized vector with un.
// Example: unL for unormalized light vector

// use capital letter for regular vector, vector are always pointing outward the current pixel position (ready for lighting equation)
// capital letter mean the vector is normalize, unless we put un in front of it.
// V: View vector  (no eye vector)
// L: Light vector
// N: Normal vector
// H: Half vector

// Input/Outputs structs in PascalCase and prefixed by entry type
// struct AttributesDefault
// struct VaryingsDefault
// use input/output as variable name when using these structures

// Entry program name
// VertDefault
// FragDefault / FragForward / FragDeferred

// constant floating number written as 1.0  (not 1, not 1.0f, not 1.0h)

// uniform have g_ as prefix (still lowercaseThenCamelCase)

// Structure definition that are share between C# and hlsl.
// These structures need to be align on float4 to respectect various packing rules from sahder language.
// This mean that these structure need to be padded.

// Do not use "in", only "out" or "inout" as califier, not "inline" keyword either, useless.

// The lighting code assume that 1 Unity unit (1uu) == 1 meters.  This is very important regarding physically based light unit and inverse square attenuation

// When declaring "out" argument of function, they are always last


// Include language header
#if defined(SHADER_API_D3D11)
#include "API/D3D11.hlsl"
#elif defined(SHADER_API_XBOXONE)
#include "API/D3D11_1.hlsl"
#else
#error unsupported shader api
#endif
#include "API/Validate.hlsl"


// ----------------------------------------------------------------------------
// Common define allowing to include shared file between C# and hlsl
// ----------------------------------------------------------------------------

#define __HLSL		1
#define public
#define Vec2		float2
#define Vec3		float3
#define Vec4		float4
#define Mat44		float4x4

// ----------------------------------------------------------------------------
// Common math definition and fastmath function
// ----------------------------------------------------------------------------

#define PI			3.14159265359f
#define TWO_PI		6.28318530718f
#define FOUR_PI		12.56637061436f
#define INV_PI		0.31830988618f
#define INV_TWO_PI	0.15915494309f							
#define INV_FOUR_PI	0.07957747155f
#define HALF_PI		1.57079632679f
#define INV_HALF_PI	0.636619772367f

#define MERGE_NAME(X, Y) X##Y

// Ref: https://seblagarde.wordpress.com/2014/12/01/inverse-trigonometric-functions-gpu-optimization-for-amd-gcn-architecture/
float FastACos(float inX) 
{ 
    float x = abs(inX); 
    float res = -0.156583f * x + HALF_PI; 
    res *= sqrt(1.0f - x); 
    return (inX >= 0) ? res : PI - res; 
}

// Same cost as Acos + 1 FR
// Same error
// input [-1, 1] and output [-PI/2, PI/2]
float FastASin(float x)
{
    return HALF_PI - FastACos(x);
}

// max absolute error 1.3x10^-3
// Eberly's odd polynomial degree 5 - respect bounds
// 4 VGPR, 14 FR (10 FR, 1 QR), 2 scalar
// input [0, infinity] and output [0, PI/2]
float FastATanPos(float x) 
{ 
    float t0 = (x < 1.0f) ? x : 1.0f / x;
    float t1 = t0 * t0;
    float poly = 0.0872929f;
    poly = -0.301895f + poly * t1;
    poly = 1.0f + poly * t1;
    poly = poly * t0;
    return (x < 1.0f) ? poly : HALF_PI - poly;
}

// 4 VGPR, 16 FR (12 FR, 1 QR), 2 scalar
// input [-infinity, infinity] and output [-PI/2, PI/2]
float FastATan(float x) 
{     
    float t0 = FastATanPos(abs(x));     
    return (x < 0.0f) ? -t0: t0; 
}

// ----------------------------------------------------------------------------
// World position reconstruction / transformation
// ----------------------------------------------------------------------------

struct Coordinate
{
	// Normalize coordinates
	float2	positionSS;
	// Unormalize coordinates
	int2	unPositionSS;
};

// This function is use to provide an easy way to sample into a screen texture, either from a pixel or a compute shaders.
// This allow to easily share code.
// If a compute shader call this function inPositionSS is an interger usually calculate like: uint2 inPositionSS = groupId.xy * BLOCK_SIZE + groupThreadId.xy
// else it is current unormalized screen coordinate like return by VPOS
Coordinate GetCoordinate(float2 inPositionSS, float2 invScreenSize)
{
	Coordinate coord;
	coord.positionSS = inPositionSS;
	// TODO: How to detect automatically that we are a compute shader ?
 #if 0
	// In case of compute shader an extra half offset is added to the screenPos to shift the integer position to pixel center.
	coord.positionSS.xy += float2(0.5f, 0.5f);
#endif
	coord.positionSS *= invScreenSize;

	coord.unPositionSS = int2(inPositionSS);

	return coord;
}

// screenPos is screen coordinate in [0..1] (return by Coordinate.positionSS)
// depth must be the depth from the raw depth buffer. This allow to handle all kind of depth automatically with the inverse view projection matrix.
// For information. In Unity Depth is always in range 0..1 (even on OpenGL) but can be reversed.
float3 UnprojectToWorld(float depth, float2 screenPos, float4x4 invViewProjectionMatrix)
{
	float4 positionHS	= float4(screenPos.xy * 2.0 - 1.0, depth, 1.0);
	float4 hpositionWS	= mul(invViewProjectionMatrix, positionHS);

	return hpositionWS.xyz / hpositionWS.w;
}

// Z buffer to linear 0..1 depth
float Linear01Depth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.x * depth + zBufferParam.y);
}
// Z buffer to linear depth
float LinearEyeDepth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
}

#endif // UNITY_COMMON_INCLUDED