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
// capital letter mean the vector is normalize, unless we put 'un' in front of it.
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

// All uniforms should be in contant buffer (nothing in the global namespace).
// The reason is that for compute shader we need to guarantee that the layout of CBs is consistent across kernels. Something that we can't control with the global namespace (uniforms get optimized out if not used, modifying the global CBuffer layout per kernel)

// Structure definition that are share between C# and hlsl.
// These structures need to be align on float4 to respect various packing rules from sahder language.
// This mean that these structure need to be padded.

// Do not use "in", only "out" or "inout" as califier, not "inline" keyword either, useless.

// The lighting code assume that 1 Unity unit (1uu) == 1 meters.  This is very important regarding physically based light unit and inverse square attenuation

// When declaring "out" argument of function, they are always last

// headers from ShaderLibrary do not include "common.hlsl", this should be included in the .shader using it (or Material.hlsl)

// Rules: When doing an array for constant buffer variables, we always use float4 to avoid any packing issue, particularly between compute shader and pixel shaders
// i.e don't use SetGlobalFloatArray or SetComputeFloatParams
// The array can be alias in hlsl. Exemple:
// uniform float4 packedArray[3];
// static float unpackedArray[12] = (float[12]packedArray;


// Include language header
#if defined(SHADER_API_D3D11)
#include "API/D3D11.hlsl"
#elif defined(SHADER_API_PSSL)
#include "API/PSSL.hlsl"
#elif defined(SHADER_API_XBOXONE)
#include "API/D3D11.hlsl"
#include "API/D3D11_1.hlsl"
#elif defined(SHADER_API_METAL)
#include "API/Metal.hlsl"
#else
#error unsupported shader api
#endif
#include "API/Validate.hlsl"

#include "Random.hlsl"

// Some shader compiler don't support to do multiple ## for concatenation inside the same macro, it require an indirection.
// This is the purpose of this macro
#define MERGE_NAME(X, Y) X##Y

// These define are use to abstract the way we sample into a cubemap array.
// Some platform don't support cubemap array so we fallback on 2D latlong
#ifdef UNITY_NO_CUBEMAP_ARRAY
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURE2D_ARRAY
#define SAMPLERCUBE_ABSTRACT SAMPLER2D
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURE2D_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURE2D_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, DirectionToLatLongCoordinate(coord3), index, lod)
#else
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURECUBE_ARRAY
#define SAMPLERCUBE_ABSTRACT SAMPLERCUBE
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURECUBE_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURECUBE_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)
#endif

// ----------------------------------------------------------------------------
// Common intrinsic (general implementation of intrinsic available on some platform)
// ----------------------------------------------------------------------------

#ifndef INTRINSIC_BITFIELD_EXTRACT
// unsigned integer bit field extract implementation
uint BitFieldExtract(uint data, uint numBits, uint offset)
{
    uint mask = 0xFFFFFFFFu >> (32u - numBits);
    return (data >> offset) & mask;
}
#endif // INTRINSIC_BITFIELD_EXTRACT

bool IsBitSet(uint data, uint bitPos)
{
    return BitFieldExtract(data, 1u, bitPos) != 0;
}

#ifndef INTRINSIC_WAVEREADFIRSTLANE
// Warning: for correctness, the value you pass to the function must be constant across the wave!
uint WaveReadFirstLane(uint scalarValue)
{
    return scalarValue;
}
#endif

#ifndef INTRINSIC_MUL24
int Mul24(int a, int b)
{
    return a * b;
}

uint Mul24(uint a, uint b)
{
    return a * b;
}
#endif // INTRINSIC_MUL24

#ifndef INTRINSIC_MAD24
int Mad24(int a, int b, int c)
{
    return a * b + c;
}

uint Mad24(uint a, uint b, uint c)
{
    return a * b + c;
}
#endif // INTRINSIC_MAD24

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

void Swap(inout float a, inout float b)
{
    float  t = a; a = b; b = t;
}

void Swap(inout float2 a, inout float2 b)
{
    float2 t = a; a = b; b = t;
}

void Swap(inout float3 a, inout float3 b)
{
    float3 t = a; a = b; b = t;
}

void Swap(inout float4 a, inout float4 b)
{
    float4 t = a; a = b; b = t;
}

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

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

void GetCubeFaceID(float3 dir, out int faceIndex)
{
    // TODO: Use faceID intrinsic on console
    float3 adir = abs(dir);

    // +Z -Z
    faceIndex = dir.z > 0.0 ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;

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

#endif // INTRINSIC_CUBEMAP_FACE_ID

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
#define INFINITY    asfloat(0x7F800000)
#define FLT_SMALL   0.0001
#define LOG2_E      1.44269504089

#define FLT_EPSILON 1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#define FLT_MIN     1.175494351e-38 // Minimum representable positive floating-point number
#define FLT_MAX     3.402823466e+38 // Maximum representable floating-point number

#define HFLT_MIN    0.00006103515625 // 2^14  it is the same for 10, 11 and 16bit float. ref: https://www.khronos.org/opengl/wiki/Small_Float_Formats

float DegToRad(float deg)
{
    return deg * PI / 180.0;
}

float RadToDeg(float rad)
{
    return rad * 180.0 / PI;
}

// Square functions for cleaner code
float Sqr(float x)
{
    return x * x;
}

float3 Sqr(float3 x)
{
    return x * x;
}

// Input [0, 1] and output [0, PI/2]
// 9 VALU
float FastACosPos(float inX)
{
    float x = abs(inX);
    float res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
    res *= sqrt(1.0 - x);

    return res;
}

// Ref: https://seblagarde.wordpress.com/2014/12/01/inverse-trigonometric-functions-gpu-optimization-for-amd-gcn-architecture/
// Input [-1, 1] and output [0, PI]
// 12 VALU
float FastACos(float inX)
{
    float res = FastACosPos(inX);

    return (inX >= 0) ? res : PI - res; // Undo range reduction
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

// Same as smoothstep except it assume 0, 1 interval for x
float Smoothstep01(float x)
{
    return x * x * (3.0 - (2.0 * x));
}

static const float3x3 k_identity3x3 = {1.0, 0.0, 0.0,
                                       0.0, 1.0, 0.0,
                                       0.0, 0.0, 1.0};

static const float4x4 k_identity4x4 = {1.0, 0.0, 0.0, 0.0,
                                       0.0, 1.0, 0.0, 0.0,
                                       0.0, 0.0, 1.0, 0.0,
                                       0.0, 0.0, 0.0, 1.0};

// Using pow often result to a warning like this
// "pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them"
// PositivePow remove this warning when you know the value is positive and avoid inf/NAN.
float PositivePow(float base, float power)
{
    return pow(max(abs(base), float(FLT_EPSILON)), power);
}

float2 PositivePow(float2 base, float2 power)
{
    return pow(max(abs(base), float2(FLT_EPSILON, FLT_EPSILON)), power);
}

float3 PositivePow(float3 base, float3 power)
{
    return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

float4 PositivePow(float4 base, float4 power)
{
    return pow(max(abs(base), float4(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

// Ref: https://twitter.com/SebAaltonen/status/878250919879639040
// 2 mads (mad_sat and mad), faster than regular sign
float3 FastSign(float x)
{
    return  saturate(x * FLT_MAX) * 2.0 - 1.0;
}

// Orthonormalizes the tangent frame using the Gram-Schmidt process.
// We assume that both the tangent and the normal are normalized.
// Returns the new tangent (the normal is unaffected).
float3 Orthonormalize(float3 tangent, float3 normal)
{
    return normalize(tangent - dot(tangent, normal) * normal);
}

// ----------------------------------------------------------------------------
// Texture utilities
// ----------------------------------------------------------------------------

float ComputeTextureLOD(float2 uv)
{
    float2 ddx_ = ddx(uv);
    float2 ddy_ = ddy(uv);
    float d = max(dot(ddx_, ddx_), dot(ddy_, ddy_));

    return max(0.5 * log2(d), 0.0);
}

// texelSize is Unity XXX_TexelSize feature parameters
// x contains 1.0/width, y contains 1.0 / height, z contains width, w contains height
float ComputeTextureLOD(float2 uv, float4 texelSize)
{
    uv *= texelSize.zw;

    return ComputeTextureLOD(uv);
}

// ----------------------------------------------------------------------------
// Texture format sampling
// ----------------------------------------------------------------------------

float2 DirectionToLatLongCoordinate(float3 unDir)
{
    float3 dir = normalize(unDir);
    // coordinate frame is (-Z, X) meaning negative Z is primary axis and X is secondary axis.
    return float2(1.0 - 0.5 * INV_PI * atan2(dir.x, -dir.z), asin(dir.y) * INV_PI + 0.5);
}

float3 LatlongToDirectionCoordinate(float2 coord)
{
    float theta = coord.y * PI;
    float phi = (coord.x * 2.f * PI - PI*0.5f);

    float cosTheta = cos(theta);
    float sinTheta = sqrt(1.0f - min(1.0f, cosTheta*cosTheta));
    float cosPhi = cos(phi);
    float sinPhi = sin(phi);

    float3 direction = float3(sinTheta*cosPhi, cosTheta, sinTheta*sinPhi);
    direction.xy *= -1.0;
    return direction;
}

// ----------------------------------------------------------------------------
// World position reconstruction / transformation
// ----------------------------------------------------------------------------

// Z buffer to linear 0..1 depth (0 at near plane, 1 at far plane).
// Does not correctly handle oblique view frustums.
float Linear01DepthFromNear(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.x + zBufferParam.y / depth);
}

// Z buffer to linear 0..1 depth (0 at camera position, 1 at far plane).
// Does not correctly handle oblique view frustums.
float Linear01Depth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.x * depth + zBufferParam.y);
}

// Z buffer to linear depth.
// Does not correctly handle oblique view frustums.
float LinearEyeDepth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
}

// Z buffer to linear depth.
// Correctly handles oblique view frustums. Only valid for projection matrices!
// Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
float LinearEyeDepth(float2 positionSS, float depthRaw, float4 invProjParam)
{
    float4 positionCS = float4(positionSS * 2.0 - 1.0, depthRaw, 1.0);
    float  viewSpaceZ = rcp(dot(positionCS, invProjParam));
    // The view space uses a right-handed coordinate system.
    return -viewSpaceZ;
}

struct PositionInputs
{
    // Normalize screen position (offset by 0.5)
    float2 positionSS;
    // Unormalize screen position (offset by 0.5)
    uint2 unPositionSS;
    uint2 unTileCoord;

    float depthRaw; // raw depth from depth buffer
    float depthVS;

    float3 positionWS;
};

// This function is use to provide an easy way to sample into a screen texture, either from a pixel or a compute shaders.
// This allow to easily share code.
// If a compute shader call this function unPositionSS is an integer usually calculate like: uint2 unPositionSS = groupId.xy * BLOCK_SIZE + groupThreadId.xy
// else it is current unormalized screen coordinate like return by SV_Position
PositionInputs GetPositionInput(float2 unPositionSS, float2 invScreenSize, uint2 unTileCoord)   // Specify explicit tile coordinates so that we can easily make it lane invariant for compute evaluation.
{
    PositionInputs posInput;
    ZERO_INITIALIZE(PositionInputs, posInput);

    posInput.positionSS = unPositionSS;
#if SHADER_STAGE_COMPUTE
    // In case of compute shader an extra half offset is added to the screenPos to shift the integer position to pixel center.
    posInput.positionSS.xy += float2(0.5, 0.5);
#endif
    posInput.positionSS *= invScreenSize;

    posInput.unPositionSS = uint2(unPositionSS);
    posInput.unTileCoord = unTileCoord;

    return posInput;
}

PositionInputs GetPositionInput(float2 unPositionSS, float2 invScreenSize)
{
    return GetPositionInput(unPositionSS, invScreenSize, uint2(0, 0));
}

// From forward
// depthRaw and depthVS come directly form .zw of SV_Position
void UpdatePositionInput(float depthRaw, float depthVS, float3 positionWS, inout PositionInputs posInput)
{
    posInput.depthRaw   = depthRaw;
    posInput.depthVS    = depthVS;
    posInput.positionWS = positionWS;
}

float4 ComputeClipSpacePosition(float2 positionSS, float depthRaw)
{
#if UNITY_UV_STARTS_AT_TOP
    positionSS.y = 1.0 - positionSS.y;
#endif
    return float4(positionSS * 2.0 - 1.0, depthRaw, 1.0);
}

float2 ComputeScreenSpacePosition(float4 positionCS)
{
    float2 positionSS = positionCS.xy * (rcp(positionCS.w) * 0.5) + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    positionSS.y = 1.0 - positionSS.y;
#endif
    return positionSS;
}

float2 ComputeScreenSpacePosition(float3 positionWS, float4x4 viewProjectionMatrix)
{
    float4 positionCS = mul(viewProjectionMatrix, float4(positionWS, 1.0));
    return ComputeScreenSpacePosition(positionCS);
}

float3 ComputeViewSpacePosition(float2 positionSS, float depthRaw, float4x4 invProjMatrix)
{
    float4 positionCS = ComputeClipSpacePosition(positionSS, depthRaw);
    float4 positionVS = mul(invProjMatrix, positionCS);
    // The view space uses a right-handed coordinate system.
    positionVS.z = -positionVS.z;
    return positionVS.xyz / positionVS.w;
}

float3 ComputeWorldSpacePosition(float2 positionSS, float depthRaw, float4x4 invViewProjMatrix)
{
    float4 positionCS  = ComputeClipSpacePosition(positionSS, depthRaw);
    float4 hpositionWS = mul(invViewProjMatrix, positionCS);
    return hpositionWS.xyz / hpositionWS.w;
}

// From deferred or compute shader
// depth must be the depth from the raw depth buffer. This allow to handle all kind of depth automatically with the inverse view projection matrix.
// For information. In Unity Depth is always in range 0..1 (even on OpenGL) but can be reversed.
void UpdatePositionInput(float depthRaw, float4x4 invViewProjMatrix, float4x4 viewProjMatrix, inout PositionInputs posInput)
{
    posInput.depthRaw = depthRaw;

    posInput.positionWS = ComputeWorldSpacePosition(posInput.positionSS, depthRaw, invViewProjMatrix);

    // The compiler should optimize this (less expensive than reconstruct depth VS from depth buffer)
    posInput.depthVS = mul(viewProjMatrix, float4(posInput.positionWS, 1.0)).w;
}

// The view direction 'V' points towards the camera.
// 'depthOffsetVS' is always applied in the opposite direction (-V).
void ApplyDepthOffsetPositionInput(float3 V, float depthOffsetVS, float4x4 viewProjMatrix, inout PositionInputs posInput)
{
    posInput.positionWS += depthOffsetVS * (-V);

    float4 positionCS = mul(viewProjMatrix, float4(posInput.positionWS, 1.0));
    posInput.depthVS  = positionCS.w;
    posInput.depthRaw = positionCS.z / positionCS.w;
}

// ----------------------------------------------------------------------------
// Misc utilities
// ----------------------------------------------------------------------------

// Generates a triangle in homogeneous clip space, s.t.
// v0 = (-1, -1, 1), v1 = (3, -1, 1), v2 = (-1, 3, 1).
float2 GetFullScreenTriangleTexCoord(uint vertexID)
{
#if UNITY_UV_STARTS_AT_TOP
    return float2((vertexID << 1) & 2, 1.0 - (vertexID & 2));
#else
    return float2((vertexID << 1) & 2, vertexID & 2);
#endif
}

float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
{
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    return float4(uv * 2.0 - 1.0, z, 1.0);
}

// LOD dithering transition helper
// LOD0 must use this function with ditherFactor 1..0
// LOD1 must use this function with ditherFactor 0..1
void LODDitheringTransition(uint2 unPositionSS, float ditherFactor)
{
    // Generate a spatially varying pattern.
    // Unfortunately, varying the pattern with time confuses the TAA, increasing the amount of noise.
    float p = GenerateHashedRandomFloat(unPositionSS);

    // We want to have a symmetry between 0..0.5 ditherFactor and 0.5..1 so no pixels are transparent during the transition
    // this is handled by this test which reverse the pattern
    p = (ditherFactor >= 0.5) ? p : 1 - p;
    clip(ditherFactor - p);
}

#endif // UNITY_COMMON_INCLUDED
