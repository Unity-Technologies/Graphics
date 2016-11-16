#ifndef UNITY_COMMON_INCLUDED
#define UNITY_COMMON_INCLUDED

// Convention:

// Unity is Y up - left handed

// space at the end of the variable name
// WS: world space
// VS: view space
// OS: object space
// CS: Homogenous clip spaces
// TS: tangent space
// TXS: texture space
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

// uniform have _ as prefix + uppercase _LowercaseThenCamelCase

// Structure definition that are share between C# and hlsl.
// These structures need to be align on float4 to respectect various packing rules from sahder language.
// This mean that these structure need to be padded.

// Do not use "in", only "out" or "inout" as califier, not "inline" keyword either, useless.

// The lighting code assume that 1 Unity unit (1uu) == 1 meters.  This is very important regarding physically based light unit and inverse square attenuation

// When declaring "out" argument of function, they are always last

// headers from ShaderLibrary do not include "common.hlsl", this should be included in the .shader using it (or Material.hlsl)


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
// Common intrinsic (general implementation of intrinsic available on some platform)
// ----------------------------------------------------------------------------

#ifndef INTRINSIC_BITFIELD_EXTRACT
// unsigned integer bit field extract implementation
uint BitFieldExtract(uint data, uint size, uint offset)
{
    return (data >> offset) & ((1u << size) - 1u);
}
#endif // INTRINSIC_BITFIELD_EXTRACT

#ifndef INTRINSIC_CLAMP
// TODO: should we force all clamp to be intrinsic by default ?
// Some platform have one instruction clamp
#define Clamp clamp
#endif // INTRINSIC_CLAMP

#ifndef INTRINSIC_MED3
float Med3(float a, float b, float c)
{
    return Clamp(a, b, c);
}
#endif // INTRINSIC_MED3

#ifndef INTRINSIC_MINMAX3
float Min3(float a, float b, float c)
{
    return min(min(a, b), c);
}

float2 Min3(float2 a, float2 b, float2 c)
{
    return min(min(a, b), c);
}

float3 Min3(float3 a, float3 b, float3 c)
{
    return min(min(a, b), c);
}

float4 Min3(float4 a, float4 b, float4 c)
{
    return min(min(a, b), c);
}

float Max3(float a, float b, float c)
{
    return max(max(a, b), c);
}

float2 Max3(float2 a, float2 b, float2 c)
{
    return max(max(a, b), c);
}

float3 Max3(float3 a, float3 b, float3 c)
{
    return max(max(a, b), c);
}

float4 Max3(float4 a, float4 b, float4 c)
{
    return max(max(a, b), c);
}
#endif // INTRINSIC_MINMAX3

#ifndef INTRINSIC_CUBEMAP_FACE_ID
// TODO: implement this. Is the reference implementation of cubemapID provide by AMD the reverse of our ? 
/*
float CubemapFaceID(float3 dir)
{
    float faceID;
    if (abs(dir.z) >= abs(dir.x) && abs(dir.z) >= abs(dir.y))
    {
        faceID = (dir.z < 0.0) ? 5.0 : 4.0;
    }
    else if (abs(dir.y) >= abs(dir.x))
    {
        faceID = (dir.y < 0.0) ? 3.0 : 2.0;
    }
    else
    {
        faceID = (dir.x < 0.0) ? 1.0 : 0.0;
    }
    return faceID;
}
*/
#endif // INTRINSIC_CUBEMAP_FACE_ID

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

void GetCubeFaceID(float3 dir, out int faceIndex)
{
    // TODO: Use faceID intrinsic on console
    float3 adir = abs(dir);

    // +Z -Z
    faceIndex = dir.z > 0.0f ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;

    // +X -X
    if (adir.x > adir.y && adir.x > adir.z)
    {
        faceIndex = dir.x > 0.0 ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
    }
    // +Y -Y
    else if (adir.y > adir.x && adir.y > adir.z)
    {
        faceIndex = dir.y > 0.0 ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
    }
}

// ----------------------------------------------------------------------------
// Common math definition and fastmath function
// ----------------------------------------------------------------------------

#define PI          3.14159265359
#define TWO_PI      6.28318530718
#define FOUR_PI     12.56637061436
#define INV_PI      0.31830988618
#define INV_TWO_PI  0.15915494309
#define INV_FOUR_PI 0.07957747155
#define HALF_PI     1.57079632679
#define INV_HALF_PI 0.636619772367

#define FLT_EPSILON     1.192092896e-07f // smallest such that 1.0 + FLT_EPSILON != 1.0

#define MERGE_NAME(X, Y) X##Y

// Ref: https://seblagarde.wordpress.com/2014/12/01/inverse-trigonometric-functions-gpu-optimization-for-amd-gcn-architecture/
float FastACos(float inX)
{
    float x = abs(inX);
    float res = -0.156583 * x + HALF_PI;
    res *= sqrt(1.0 - x);
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
    float t0 = (x < 1.0) ? x : 1.0 / x;
    float t1 = t0 * t0;
    float poly = 0.0872929;
    poly = -0.301895 + poly * t1;
    poly = 1.0 + poly * t1;
    poly = poly * t0;
    return (x < 1.0) ? poly : HALF_PI - poly;
}

// 4 VGPR, 16 FR (12 FR, 1 QR), 2 scalar
// input [-infinity, infinity] and output [-PI/2, PI/2]
float FastATan(float x) 
{
    float t0 = FastATanPos(abs(x));
    return (x < 0.0) ? -t0 : t0;
}

// Same smoothstep except it assume 0, 1 interval for x
float smoothstep01(float x)
{
    return x * x * (3.0 - (2.0 * x));
}

// ----------------------------------------------------------------------------
// World position reconstruction / transformation
// ----------------------------------------------------------------------------

struct Coordinate
{
    // Normalize coordinates
    float2  positionSS;
    // Unormalize coordinates
    int2    unPositionSS;
};

// This function is use to provide an easy way to sample into a screen texture, either from a pixel or a compute shaders.
// This allow to easily share code.
// If a compute shader call this function unPositionSS is an integer usually calculate like: uint2 unPositionSS = groupId.xy * BLOCK_SIZE + groupThreadId.xy
// else it is current unormalized screen coordinate like return by VPOS
Coordinate GetCoordinate(float2 unPositionSS, float2 invScreenSize)
{
    Coordinate coord;
    coord.positionSS = unPositionSS;
#if SHADER_STAGE_COMPUTE
    // In case of compute shader an extra half offset is added to the screenPos to shift the integer position to pixel center.
    coord.positionSS.xy += float2(0.5, 0.5);
#endif
    coord.positionSS *= invScreenSize;

    coord.unPositionSS = int2(unPositionSS);

    return coord;
}

// screenPos is screen coordinate in [0..1] (return by Coordinate.positionSS)
// depth must be the depth from the raw depth buffer. This allow to handle all kind of depth automatically with the inverse view projection matrix.
// For information. In Unity Depth is always in range 0..1 (even on OpenGL) but can be reversed.
float3 UnprojectToWorld(float depth, float2 screenPos, float4x4 invViewProjectionMatrix)
{
    float4 positionCS   = float4(screenPos.xy * 2.0 - 1.0, depth, 1.0);
    float4 hpositionWS  = mul(invViewProjectionMatrix, positionCS);

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
