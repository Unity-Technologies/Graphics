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

#endif // UNITY_COMMON_INCLUDED