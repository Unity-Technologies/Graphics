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
#include "API/XBoneOne.hlsl"
#elif defined(SHADER_API_METAL)
#include "API/Metal.hlsl"
#elif defined(SHADER_API_VULKAN)
#include "API/Vulkan.hlsl"
#else
#error unsupported shader api
#endif
#include "API/Validate.hlsl"

#include "Macros.hlsl"
#include "Random.hlsl"

// ----------------------------------------------------------------------------
// Common intrinsic (general implementation of intrinsic available on some platform)
// ----------------------------------------------------------------------------

#ifndef INTRINSIC_BITFIELD_EXTRACT
// Unsigned integer bit field extraction.
// Note that the intrinsic itself generates a vector instruction.
// Wrap this function with WaveReadFirstLane() to get scalar output.
uint BitFieldExtract(uint data, uint numBits, uint offset)
{
    uint mask = UINT_MAX >> (32u - numBits);
    return (data >> offset) & mask;
}
#endif // INTRINSIC_BITFIELD_EXTRACT

bool IsBitSet(uint data, uint offset)
{
    return BitFieldExtract(data, 1u, offset) != 0;
}

void SetBit(inout uint data, uint offset)
{
    data |= 1u << offset;
}

void ClearBit(inout uint data, uint offset)
{
    data &= ~(1u << offset);
}

void ToggleBit(inout uint data, uint offset)
{
    data ^= 1u << offset;
}

#ifndef INTRINSIC_WAVEREADFIRSTLANE
    // Warning: for correctness, the argument must have the same value across the wave!
    TEMPLATE_1_FLT(WaveReadFirstLane, scalarValue, return scalarValue)
    TEMPLATE_1_INT(WaveReadFirstLane, scalarValue, return scalarValue)
#endif

#ifndef INTRINSIC_MUL24
    TEMPLATE_2_INT(Mul24, a, b, return a * b)
#endif // INTRINSIC_MUL24

#ifndef INTRINSIC_MAD24
    TEMPLATE_3_INT(Mad24, a, b, c, return a * b + c)
#endif // INTRINSIC_MAD24

#ifndef INTRINSIC_MINMAX3
    TEMPLATE_3_FLT(Min3, a, b, c, return min(min(a, b), c))
    TEMPLATE_3_INT(Min3, a, b, c, return min(min(a, b), c))
    TEMPLATE_3_FLT(Max3, a, b, c, return max(max(a, b), c))
    TEMPLATE_3_INT(Max3, a, b, c, return max(max(a, b), c))
#endif // INTRINSIC_MINMAX3

TEMPLATE_SWAP(Swap) // Define a Swap(a, b) function for all types

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
// Common math functions
// ----------------------------------------------------------------------------

float DegToRad(float deg)
{
    return deg * (PI / 180.0);
}

float RadToDeg(float rad)
{
    return rad * (180.0 / PI);
}

// Square functions for cleaner code
TEMPLATE_1_FLT(Sq, x, return x * x)
TEMPLATE_1_INT(Sq, x, return x * x)

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
TEMPLATE_2_FLT(PositivePow, base, power, return pow(max(abs(base), FLT_EPS), power))

// Computes (FastSign(s) * x) using 2x VALU.
// See the comment about FastSign() below.
float FastMulBySignOf(float s, float x, bool ignoreNegZero = true)
{
    if (ignoreNegZero)
    {
        return (s >= 0) ? x : -x;
    }
    else
    {
        uint negZero = 0x80000000u;
        uint signBit = negZero & asuint(s);
        return asfloat(signBit ^ asuint(x));
    }
}

// Returns -1 for negative numbers and 1 for positive numbers.
// 0 can be handled in 2 different ways.
// The IEEE floating point standard defines 0 as signed: +0 and -0.
// However, mathematics typically treats 0 as unsigned.
// Therefore, we treat -0 as +0 by default: FastSign(+0) = FastSign(-0) = 1.
// If (ignoreNegZero = false), FastSign(-0, false) = -1.
// Note that the sign() function in HLSL implements signum, which returns 0 for 0.
float FastSign(float s, bool ignoreNegZero = true)
{
    return FastMulBySignOf(s, 1.0, ignoreNegZero);
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
// Depth encoding/decoding
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
float LinearEyeDepth(float2 positionNDC, float deviceDepth, float4 invProjParam)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
    float  viewSpaceZ = rcp(dot(positionCS, invProjParam));
    // The view space uses a right-handed coordinate system.
    return -viewSpaceZ;
}

// Z buffer to linear depth.
// Correctly handles oblique view frustums.
// Typically, this is the cheapest variant, provided you've already computed 'positionWS'.
float LinearEyeDepth(float3 positionWS, float4x4 viewProjMatrix)
{
    return mul(viewProjMatrix, float4(positionWS, 1.0)).w;
}

// ----------------------------------------------------------------------------
// Space transformations
// ----------------------------------------------------------------------------

// Use case examples:
// (position = positionCS) => (clipSpaceTransform = use default)
// (position = positionVS) => (clipSpaceTransform = UNITY_MATRIX_P)
// (position = positionWS) => (clipSpaceTransform = UNITY_MATRIX_VP)
float2 ComputeNormalizedDeviceCoordinates(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
{
    float4 positionCS = mul(clipSpaceTransform, float4(position, 1.0));
    float2 positionNDC = positionCS.xy * (rcp(positionCS.w) * 0.5) + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    positionNDC.y = 1.0 - positionNDC.y;
#endif
    return positionNDC;
}

float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
#if UNITY_UV_STARTS_AT_TOP
    positionNDC.y = 1.0 - positionNDC.y;
#endif
    return float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
}

float3 ComputeViewSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invProjMatrix)
{
    float4 positionCS = ComputeClipSpacePosition(positionNDC, deviceDepth);
    float4 positionVS = mul(invProjMatrix, positionCS);
    // The view space uses a right-handed coordinate system.
    positionVS.z = -positionVS.z;
    return positionVS.xyz / positionVS.w;
}

float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
{
    float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
    float4 hpositionWS = mul(invViewProjMatrix, positionCS);
    return hpositionWS.xyz / hpositionWS.w;
}

// ----------------------------------------------------------------------------
// PositionInputs
// ----------------------------------------------------------------------------

struct PositionInputs
{
    float3 positionWS;  // World space position (could be camera-relative)
    float2 positionNDC; // Normalized screen UVs          : [0, 1) (with the half-pixel offset)
    uint2  positionSS;  // Screen space pixel coordinates : [0, NumPixels)
    uint2  tileCoord;   // Screen tile coordinates        : [0, NumTiles)
    float  deviceDepth; // Depth from the depth buffer    : [0, 1] (typically reversed)
    float  linearDepth; // View space Z coordinate        : [Near, Far]
};

// This function is use to provide an easy way to sample into a screen texture, either from a pixel or a compute shaders.
// This allow to easily share code.
// If a compute shader call this function positionSS is an integer usually calculate like: uint2 positionSS = groupId.xy * BLOCK_SIZE + groupThreadId.xy
// else it is current unormalized screen coordinate like return by SV_Position
PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, uint2 tileCoord)   // Specify explicit tile coordinates so that we can easily make it lane invariant for compute evaluation.
{
    PositionInputs posInput;
    ZERO_INITIALIZE(PositionInputs, posInput);

    posInput.positionNDC = positionSS;
#if SHADER_STAGE_COMPUTE
    // In case of compute shader an extra half offset is added to the screenPos to shift the integer position to pixel center.
    posInput.positionNDC.xy += float2(0.5, 0.5);
#endif
    posInput.positionNDC *= invScreenSize;

    posInput.positionSS = uint2(positionSS);
    posInput.tileCoord = tileCoord;

    return posInput;
}

PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize)
{
    return GetPositionInput(positionSS, invScreenSize, uint2(0, 0));
}

// From forward
// deviceDepth and linearDepth come directly from .zw of SV_Position
void UpdatePositionInput(float deviceDepth, float linearDepth, float3 positionWS, inout PositionInputs posInput)
{
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = linearDepth;
    posInput.positionWS  = positionWS;
}

// From deferred or compute shader
// depth must be the depth from the raw depth buffer. This allow to handle all kind of depth automatically with the inverse view projection matrix.
// For information. In Unity Depth is always in range 0..1 (even on OpenGL) but can be reversed.
void UpdatePositionInput(float deviceDepth, float4x4 invViewProjMatrix, float4x4 viewProjMatrix, inout PositionInputs posInput)
{
    posInput.deviceDepth = deviceDepth;
    posInput.positionWS  = ComputeWorldSpacePosition(posInput.positionNDC, deviceDepth, invViewProjMatrix);

    // The compiler should optimize this (less expensive than reconstruct depth VS from depth buffer)
    posInput.linearDepth = mul(viewProjMatrix, float4(posInput.positionWS, 1.0)).w;
}

// The view direction 'V' points towards the camera.
// 'depthOffsetVS' is always applied in the opposite direction (-V).
void ApplyDepthOffsetPositionInput(float3 V, float depthOffsetVS, float4x4 viewProjMatrix, inout PositionInputs posInput)
{
    posInput.positionWS += depthOffsetVS * (-V);

    float4 positionCS    = mul(viewProjMatrix, float4(posInput.positionWS, 1.0));
    posInput.linearDepth = positionCS.w;
    posInput.deviceDepth = positionCS.z / positionCS.w;
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
void LODDitheringTransition(uint2 positionSS, float ditherFactor)
{
    // Generate a spatially varying pattern.
    // Unfortunately, varying the pattern with time confuses the TAA, increasing the amount of noise.
    float p = GenerateHashedRandomFloat(positionSS);

    // We want to have a symmetry between 0..0.5 ditherFactor and 0.5..1 so no pixels are transparent during the transition
    // this is handled by this test which reverse the pattern
    p = (ditherFactor >= 0.5) ? p : 1 - p;
    clip(ditherFactor - p);
}


#endif // UNITY_COMMON_INCLUDED
