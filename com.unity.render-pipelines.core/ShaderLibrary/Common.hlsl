#ifndef UNITY_COMMON_INCLUDED
#define UNITY_COMMON_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3
#pragma warning (disable : 3205) // conversion of larger type to smaller
#endif

// Convention:

// Unity is Y up and left handed in world space
// Caution: When going from world space to view space, unity is right handed in view space and the determinant of the matrix is negative
// For cubemap capture (reflection probe) view space is still left handed (cubemap convention) and the determinant is positive.

// The lighting code assume that 1 Unity unit (1uu) == 1 meters.  This is very important regarding physically based light unit and inverse square attenuation

// space at the end of the variable name
// WS: world space
// RWS: Camera-Relative world space. A space where the translation of the camera have already been substract in order to improve precision
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

// Do not use "in", only "out" or "inout" as califier, no "inline" keyword either, useless.
// When declaring "out" argument of function, they are always last

// headers from ShaderLibrary do not include "common.hlsl", this should be included in the .shader using it (or Material.hlsl)

// All uniforms should be in contant buffer (nothing in the global namespace).
// The reason is that for compute shader we need to guarantee that the layout of CBs is consistent across kernels. Something that we can't control with the global namespace (uniforms get optimized out if not used, modifying the global CBuffer layout per kernel)

// Structure definition that are share between C# and hlsl.
// These structures need to be align on float4 to respect various packing rules from shader language. This mean that these structure need to be padded.
// Rules: When doing an array for constant buffer variables, we always use float4 to avoid any packing issue, particularly between compute shader and pixel shaders
// i.e don't use SetGlobalFloatArray or SetComputeFloatParams
// The array can be alias in hlsl. Exemple:
// uniform float4 packedArray[3];
// static float unpackedArray[12] = (float[12])packedArray;

// The function of the shader library are stateless, no uniform declare in it.
// Any function that require an explicit precision, use float or half qualifier, when the function can support both, it use real (see below)
// If a function require to have both a half and a float version, then both need to be explicitly define
#ifndef real

// The including shader should define whether half
// precision is suitable for its needs.  The shader
// API (for now) can indicate whether half is possible.
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
#define HAS_HALF 1
#else
#define HAS_HALF 0
#endif

#ifndef PREFER_HALF
#define PREFER_HALF 1
#endif

#if HAS_HALF && PREFER_HALF
#define REAL_IS_HALF 1
#else
#define REAL_IS_HALF 0
#endif // Do we have half?

#if REAL_IS_HALF || (defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL) && (defined(UNITY_COMPILER_HLSL) || defined(UNITY_COMPILER_DXC)))
#define half min16float
#define half2 min16float2
#define half3 min16float3
#define half4 min16float4
#define half2x2 min16float2x2
#define half2x3 min16float2x3
#define half3x2 min16float3x2
#define half3x3 min16float3x3
#define half3x4 min16float3x4
#define half4x3 min16float4x3
#define half4x4 min16float4x4
#endif

#if REAL_IS_HALF
#define real half
#define real2 half2
#define real3 half3
#define real4 half4

#define real2x2 half2x2
#define real2x3 half2x3
#define real2x4 half2x4
#define real3x2 half3x2
#define real3x3 half3x3
#define real3x4 half3x4
#define real4x3 half4x3
#define real4x4 half4x4

#define REAL_MIN HALF_MIN
#define REAL_MAX HALF_MAX
#define REAL_EPS HALF_EPS
#define TEMPLATE_1_REAL TEMPLATE_1_HALF
#define TEMPLATE_2_REAL TEMPLATE_2_HALF
#define TEMPLATE_3_REAL TEMPLATE_3_HALF

#else

#define real float
#define real2 float2
#define real3 float3
#define real4 float4

#define real2x2 float2x2
#define real2x3 float2x3
#define real2x4 float2x4
#define real3x2 float3x2
#define real3x3 float3x3
#define real3x4 float3x4
#define real4x3 float4x3
#define real4x4 float4x4

#define REAL_MIN FLT_MIN
#define REAL_MAX FLT_MAX
#define REAL_EPS FLT_EPS
#define TEMPLATE_1_REAL TEMPLATE_1_FLT
#define TEMPLATE_2_REAL TEMPLATE_2_FLT
#define TEMPLATE_3_REAL TEMPLATE_3_FLT

#endif // REAL_IS_HALF

#endif // #ifndef real

// Target in compute shader are supported in 2018.2, for now define ours
// (Note only 45 and above support compute shader)
#ifdef  SHADER_STAGE_COMPUTE
#   ifndef SHADER_TARGET
#       if defined(SHADER_API_METAL)
#       define SHADER_TARGET 45
#       else
#       define SHADER_TARGET 50
#       endif
#   endif
#endif

// This is the default keyword combination and needs to be overriden by the platforms that need specific behaviors
// when enabling conservative depth overrides
#define SV_POSITION_QUALIFIERS
#define DEPTH_OFFSET_SEMANTIC SV_Depth

// Include language header
#if defined (SHADER_API_GAMECORE)
#include "Packages/com.unity.render-pipelines.gamecore/ShaderLibrary/API/GameCore.hlsl"
#elif defined(SHADER_API_XBOXONE)
#include "Packages/com.unity.render-pipelines.xboxone/ShaderLibrary/API/XBoxOne.hlsl"
#elif defined(SHADER_API_PS4)
#include "Packages/com.unity.render-pipelines.ps4/ShaderLibrary/API/PSSL.hlsl"
#elif defined(SHADER_API_PS5)
#include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/PSSL.hlsl"
#elif defined(SHADER_API_D3D11)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
#elif defined(SHADER_API_METAL)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Metal.hlsl"
#elif defined(SHADER_API_VULKAN)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Vulkan.hlsl"
#elif defined(SHADER_API_SWITCH)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Switch.hlsl"
#elif defined(SHADER_API_GLCORE)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLCore.hlsl"
#elif defined(SHADER_API_GLES3)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES3.hlsl"
#elif defined(SHADER_API_GLES)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES2.hlsl"
#else
#error unsupported shader api
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Validate.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

#ifdef SHADER_API_XBOXONE // TODO: to move in .nda package in 21.1
#define PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER
#endif

#if defined(PLATFORM_SUPPORTS_NATIVE_RENDERPASS)

    #if defined(UNITY_COMPILER_DXC)

        //Subpass inputs are disallowed in non-fragment shader stages with DXC so we need some dummy value to use in the fragment function while it's not being compiled
        #if defined(SHADER_STAGE_FRAGMENT)
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(type, idx) [[vk::input_attachment_index(idx)]] SubpassInput<type##4> hlslcc_fbinput_##idx
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(type, idx) [[vk::input_attachment_index(idx)]] SubpassInputMS<type##4> hlslcc_fbinput_##idx
        #else
            //declaring dummy resources here so that non-fragment shader stage automatic bindings wouldn't diverge from the fragment shader (important for vulkan)
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(type, idx) Texture2D dxc_dummy_fbinput_resource##idx; static type DXC_DummySubpassVariable##idx = type(0).xxxx;
            #define UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(type, idx) Texture2D dxc_dummy_fbinput_resource##idx; static type DXC_DummySubpassVariable##idx = type(0).xxxx
        #endif
        // Renderpass inputs: Vulkan/Metal subpass input
        #define FRAMEBUFFER_INPUT_FLOAT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(float, idx)
        #define FRAMEBUFFER_INPUT_FLOAT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(float, idx)
        // For halfs
        #define FRAMEBUFFER_INPUT_HALF(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(half, idx)
        #define FRAMEBUFFER_INPUT_HALF_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(half, idx)
        // For ints
        #define FRAMEBUFFER_INPUT_INT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(int, idx)
        #define FRAMEBUFFER_INPUT_INT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(int, idx)
        // For uints
        #define FRAMEBUFFER_INPUT_UINT(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX(uint, idx)
        #define FRAMEBUFFER_INPUT_UINT_MS(idx) UNITY_DXC_SUBPASS_INPUT_TYPE_INDEX_MS(uint, idx)

        #if defined(SHADER_STAGE_FRAGMENT)
            #define LOAD_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx.SubpassLoad()
            #define LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) hlslcc_fbinput_##idx.SubpassLoad(sampleIdx)
        #else
            #define LOAD_FRAMEBUFFER_INPUT(idx, v2fname) DXC_DummySubpassVariable##idx
            #define LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) DXC_DummySubpassVariable##idx
        #endif
    #else
        // For floats
        #define FRAMEBUFFER_INPUT_FLOAT(idx) cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; }
        #define FRAMEBUFFER_INPUT_FLOAT_MS(idx) cbuffer hlslcc_SubpassInput_F_##idx { float4 hlslcc_fbinput_##idx[8]; }
        // For halfs
        #define FRAMEBUFFER_INPUT_HALF(idx) cbuffer hlslcc_SubpassInput_h_##idx { half4 hlslcc_fbinput_##idx; }
        #define FRAMEBUFFER_INPUT_HALF_MS(idx) cbuffer hlslcc_SubpassInput_H_##idx { half4 hlslcc_fbinput_##idx[8]; }
        // For ints
        #define FRAMEBUFFER_INPUT_INT(idx) cbuffer hlslcc_SubpassInput_i_##idx { int4 hlslcc_fbinput_##idx; }
        #define FRAMEBUFFER_INPUT_INT_MS(idx) cbuffer hlslcc_SubpassInput_I_##idx { int4 hlslcc_fbinput_##idx[8]; }
        // For uints
        #define FRAMEBUFFER_INPUT_UINT(idx) cbuffer hlslcc_SubpassInput_u_##idx { uint4 hlslcc_fbinput_##idx; }
        #define FRAMEBUFFER_INPUT_UINT_MS(idx) cbuffer hlslcc_SubpassInput_U_##idx { uint4 hlslcc_fbinput_##idx[8]; }

        #define LOAD_FRAMEBUFFER_INPUT(idx, v2fname) hlslcc_fbinput_##idx
        #define LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fname) hlslcc_fbinput_##idx[sampleIdx]
    #endif

#else

// Renderpass inputs: General fallback paths
#define FRAMEBUFFER_INPUT_FLOAT(idx) TEXTURE2D_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_HALF(idx) TEXTURE2D_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_INT(idx) TEXTURE2D_INT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_UINT(idx) TEXTURE2D_UINT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize

#define LOAD_FRAMEBUFFER_INPUT(idx, v2fvertexname) _UnityFBInput##idx.Load(uint3(v2fvertexname.xy, 0))

#define FRAMEBUFFER_INPUT_FLOAT_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_HALF_MS(idx) Texture2DMS<float4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_INT_MS(idx) Texture2DMS<int4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize
#define FRAMEBUFFER_INPUT_UINT_MS(idx) Texture2DMS<uint4> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize

#define LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx, v2fvertexname) _UnityFBInput##idx.Load(uint2(v2fvertexname.xy), sampleIdx)


#endif

// ----------------------------------------------------------------------------
// Global Constant buffers API definitions
// ----------------------------------------------------------------------------
#if (SHADER_STAGE_RAY_TRACING && UNITY_RAY_TRACING_GLOBAL_RESOURCES)
    #define GLOBAL_RESOURCE(type, name, reg) type name : register(reg, space1);
    #define GLOBAL_CBUFFER_START(name, reg) cbuffer name : register(reg, space1) {
#else
    #define GLOBAL_RESOURCE(type, name, reg) type name;
    #define GLOBAL_CBUFFER_START(name, reg) CBUFFER_START(name)
#endif

// ----------------------------------------------------------------------------
// Common intrinsic (general implementation of intrinsic available on some platform)
// ----------------------------------------------------------------------------

// Error on GLES2 undefined functions
#ifdef SHADER_API_GLES
#define BitFieldExtract ERROR_ON_UNSUPPORTED_FUNCTION(BitFieldExtract)
#define IsBitSet ERROR_ON_UNSUPPORTED_FUNCTION(IsBitSet)
#define SetBit ERROR_ON_UNSUPPORTED_FUNCTION(SetBit)
#define ClearBit ERROR_ON_UNSUPPORTED_FUNCTION(ClearBit)
#define ToggleBit ERROR_ON_UNSUPPORTED_FUNCTION(ToggleBit)
#define FastMulBySignOfNegZero ERROR_ON_UNSUPPORTED_FUNCTION(FastMulBySignOfNegZero)
#define LODDitheringTransition ERROR_ON_UNSUPPORTED_FUNCTION(LODDitheringTransition)
#endif

// On everything but GCN consoles or DXC compiled shaders we error on cross-lane operations
#if !defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(UNITY_COMPILER_DXC)
#define WaveActiveAllTrue ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveAllTrue)
#define WaveActiveAnyTrue ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveAnyTrue)
#define WaveGetLaneIndex ERROR_ON_UNSUPPORTED_FUNCTION(WaveGetLaneIndex)
#define WaveIsFirstLane ERROR_ON_UNSUPPORTED_FUNCTION(WaveIsFirstLane)
#define GetWaveID ERROR_ON_UNSUPPORTED_FUNCTION(GetWaveID)
#define WaveActiveMin ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveMin)
#define WaveActiveMax ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveMax)
#define WaveActiveBallot ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveBallot)
#define WaveActiveSum ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveSum)
#define WaveActiveBitAnd ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveBitAnd)
#define WaveActiveBitOr ERROR_ON_UNSUPPORTED_FUNCTION(WaveActiveBitOr)
#define WaveGetLaneCount ERROR_ON_UNSUPPORTED_FUNCTION(WaveGetLaneCount)
#endif

#if defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS)
// Helper macro to compute lane swizzle offset starting from andMask, orMask and xorMask.
// IMPORTANT, to guarantee compatibility with all platforms, the masks need to be constant literals (constants at compile time)
#define LANE_SWIZZLE_OFFSET(andMask, orMask, xorMask)  (andMask | (orMask << 5) | (xorMask << 10))
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonDeprecated.hlsl"

#if !defined(SHADER_API_GLES)

#ifndef INTRINSIC_BITFIELD_EXTRACT
// Unsigned integer bit field extraction.
// Note that the intrinsic itself generates a vector instruction.
// Wrap this function with WaveReadLaneFirst() to get scalar output.
uint BitFieldExtract(uint data, uint offset, uint numBits)
{
    uint mask = (1u << numBits) - 1u;
    return (data >> offset) & mask;
}
#endif // INTRINSIC_BITFIELD_EXTRACT

#ifndef INTRINSIC_BITFIELD_EXTRACT_SIGN_EXTEND
// Integer bit field extraction with sign extension.
// Note that the intrinsic itself generates a vector instruction.
// Wrap this function with WaveReadLaneFirst() to get scalar output.
int BitFieldExtractSignExtend(int data, uint offset, uint numBits)
{
    int  shifted = data >> offset;      // Sign-extending (arithmetic) shift
    int  signBit = shifted & (1u << (numBits - 1u));
    uint mask    = (1u << numBits) - 1u;

    return -signBit | (shifted & mask); // Use 2-complement for negation to replicate the sign bit
}
#endif // INTRINSIC_BITFIELD_EXTRACT_SIGN_EXTEND

#ifndef INTRINSIC_BITFIELD_INSERT
// Inserts the bits indicated by 'mask' from 'src' into 'dst'.
uint BitFieldInsert(uint mask, uint src, uint dst)
{
    return (src & mask) | (dst & ~mask);
}
#endif // INTRINSIC_BITFIELD_INSERT

bool IsBitSet(uint data, uint offset)
{
    return BitFieldExtract(data, offset, 1u) != 0;
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
#endif

#ifndef INTRINSIC_WAVEREADFIRSTLANE
    // Warning: for correctness, the argument's value must be the same across all lanes of the wave.
    TEMPLATE_1_REAL(WaveReadLaneFirst, scalarValue, return scalarValue)
    TEMPLATE_1_INT(WaveReadLaneFirst, scalarValue, return scalarValue)
#endif

#ifndef INTRINSIC_MUL24
    TEMPLATE_2_INT(Mul24, a, b, return a * b)
#endif // INTRINSIC_MUL24

#ifndef INTRINSIC_MAD24
    TEMPLATE_3_INT(Mad24, a, b, c, return a * b + c)
#endif // INTRINSIC_MAD24

#ifndef INTRINSIC_MINMAX3
    TEMPLATE_3_REAL(Min3, a, b, c, return min(min(a, b), c))
    TEMPLATE_3_INT(Min3, a, b, c, return min(min(a, b), c))
    TEMPLATE_3_REAL(Max3, a, b, c, return max(max(a, b), c))
    TEMPLATE_3_INT(Max3, a, b, c, return max(max(a, b), c))
#endif // INTRINSIC_MINMAX3

TEMPLATE_3_REAL(Avg3, a, b, c, return (a + b + c) * 0.33333333)

// Important! Quad functions only valid in pixel shaders!
    float2 GetQuadOffset(int2 screenPos)
    {
        return float2(float(screenPos.x & 1) * 2.0 - 1.0, float(screenPos.y & 1) * 2.0 - 1.0);
    }

#ifndef INTRINSIC_QUAD_SHUFFLE
    float QuadReadAcrossX(float value, int2 screenPos)
    {
        return value - (ddx_fine(value) * (float(screenPos.x & 1) * 2.0 - 1.0));
    }

    float QuadReadAcrossY(float value, int2 screenPos)
    {
        return value - (ddy_fine(value) * (float(screenPos.y & 1) * 2.0 - 1.0));
    }

    float QuadReadAcrossDiagonal(float value, int2 screenPos)
    {
        float dX = ddx_fine(value);
        float dY = ddy_fine(value);
        float2 quadDir = GetQuadOffset(screenPos);
        float X = value - (dX * quadDir.x);
        return X - (ddy_fine(value) * quadDir.y);
    }
#endif

    float3 QuadReadFloat3AcrossX(float3 val, int2 positionSS)
    {
        return float3(QuadReadAcrossX(val.x, positionSS), QuadReadAcrossX(val.y, positionSS), QuadReadAcrossX(val.z, positionSS));
    }

    float4 QuadReadFloat4AcrossX(float4 val, int2 positionSS)
    {
        return float4(QuadReadAcrossX(val.x, positionSS), QuadReadAcrossX(val.y, positionSS), QuadReadAcrossX(val.z, positionSS), QuadReadAcrossX(val.w, positionSS));
    }

    float3 QuadReadFloat3AcrossY(float3 val, int2 positionSS)
    {
        return float3(QuadReadAcrossY(val.x, positionSS), QuadReadAcrossY(val.y, positionSS), QuadReadAcrossY(val.z, positionSS));
    }

    float4 QuadReadFloat4AcrossY(float4 val, int2 positionSS)
    {
        return float4(QuadReadAcrossY(val.x, positionSS), QuadReadAcrossY(val.y, positionSS), QuadReadAcrossY(val.z, positionSS), QuadReadAcrossY(val.w, positionSS));
    }

    float3 QuadReadFloat3AcrossDiagonal(float3 val, int2 positionSS)
    {
        return float3(QuadReadAcrossDiagonal(val.x, positionSS), QuadReadAcrossDiagonal(val.y, positionSS), QuadReadAcrossDiagonal(val.z, positionSS));
    }

    float4 QuadReadFloat4AcrossDiagonal(float4 val, int2 positionSS)
    {
        return float4(QuadReadAcrossDiagonal(val.x, positionSS), QuadReadAcrossDiagonal(val.y, positionSS), QuadReadAcrossDiagonal(val.z, positionSS), QuadReadAcrossDiagonal(val.w, positionSS));
    }

TEMPLATE_SWAP(Swap) // Define a Swap(a, b) function for all types

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

#ifndef INTRINSIC_CUBEMAP_FACE_ID
float CubeMapFaceID(float3 dir)
{
    float faceID;

    if (abs(dir.z) >= abs(dir.x) && abs(dir.z) >= abs(dir.y))
    {
        faceID = (dir.z < 0.0) ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;
    }
    else if (abs(dir.y) >= abs(dir.x))
    {
        faceID = (dir.y < 0.0) ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
    }
    else
    {
        faceID = (dir.x < 0.0) ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
    }

    return faceID;
}
#endif // INTRINSIC_CUBEMAP_FACE_ID

#if !defined(SHADER_API_GLES)
// Intrinsic isnan can't be used because it require /Gic to be enabled on fxc that we can't do. So use AnyIsNan instead
bool IsNaN(float x)
{
    return (asuint(x) & 0x7FFFFFFF) > 0x7F800000;
}

bool AnyIsNaN(float2 v)
{
    return (IsNaN(v.x) || IsNaN(v.y));
}

bool AnyIsNaN(float3 v)
{
    return (IsNaN(v.x) || IsNaN(v.y) || IsNaN(v.z));
}

bool AnyIsNaN(float4 v)
{
    return (IsNaN(v.x) || IsNaN(v.y) || IsNaN(v.z) || IsNaN(v.w));
}

bool IsInf(float x)
{
    return (asuint(x) & 0x7FFFFFFF) == 0x7F800000;
}

bool AnyIsInf(float2 v)
{
    return (IsInf(v.x) || IsInf(v.y));
}

bool AnyIsInf(float3 v)
{
    return (IsInf(v.x) || IsInf(v.y) || IsInf(v.z));
}

bool AnyIsInf(float4 v)
{
    return (IsInf(v.x) || IsInf(v.y) || IsInf(v.z) || IsInf(v.w));
}

bool IsFinite(float x)
{
    return (asuint(x) & 0x7F800000) != 0x7F800000;
}

float SanitizeFinite(float x)
{
    return IsFinite(x) ? x : 0;
}

bool IsPositiveFinite(float x)
{
    return asuint(x) < 0x7F800000;
}

float SanitizePositiveFinite(float x)
{
    return IsPositiveFinite(x) ? x : 0;
}

#endif

// ----------------------------------------------------------------------------
// Common math functions
// ----------------------------------------------------------------------------

real DegToRad(real deg)
{
    return deg * (PI / 180.0);
}

real RadToDeg(real rad)
{
    return rad * (180.0 / PI);
}

// Square functions for cleaner code
TEMPLATE_1_REAL(Sq, x, return (x) * (x))
TEMPLATE_1_INT(Sq, x, return (x) * (x))

bool IsPower2(uint x)
{
    return (x & (x - 1)) == 0;
}

// Input [0, 1] and output [0, PI/2]
// 9 VALU
real FastACosPos(real inX)
{
    real x = abs(inX);
    real res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
    res *= sqrt(1.0 - x);

    return res;
}

// Ref: https://seblagarde.wordpress.com/2014/12/01/inverse-trigonometric-functions-gpu-optimization-for-amd-gcn-architecture/
// Input [-1, 1] and output [0, PI]
// 12 VALU
real FastACos(real inX)
{
    real res = FastACosPos(inX);

    return (inX >= 0) ? res : PI - res; // Undo range reduction
}

// Same cost as Acos + 1 FR
// Same error
// input [-1, 1] and output [-PI/2, PI/2]
real FastASin(real x)
{
    return HALF_PI - FastACos(x);
}

// max absolute error 1.3x10^-3
// Eberly's odd polynomial degree 5 - respect bounds
// 4 VGPR, 14 FR (10 FR, 1 QR), 2 scalar
// input [0, infinity] and output [0, PI/2]
real FastATanPos(real x)
{
    real t0 = (x < 1.0) ? x : 1.0 / x;
    real t1 = t0 * t0;
    real poly = 0.0872929;
    poly = -0.301895 + poly * t1;
    poly = 1.0 + poly * t1;
    poly = poly * t0;
    return (x < 1.0) ? poly : HALF_PI - poly;
}

// 4 VGPR, 16 FR (12 FR, 1 QR), 2 scalar
// input [-infinity, infinity] and output [-PI/2, PI/2]
real FastATan(real x)
{
    real t0 = FastATanPos(abs(x));
    return (x < 0.0) ? -t0 : t0;
}

real FastAtan2(real y, real x)
{
    return FastATan(y / x) + (y >= 0.0 ? PI : -PI) * (x < 0.0);
}

#if (SHADER_TARGET >= 45)
uint FastLog2(uint x)
{
    return firstbithigh(x);
}
#endif

// Using pow often result to a warning like this
// "pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them"
// PositivePow remove this warning when you know the value is positive or 0 and avoid inf/NAN.
// Note: https://msdn.microsoft.com/en-us/library/windows/desktop/bb509636(v=vs.85).aspx pow(0, >0) == 0
TEMPLATE_2_REAL(PositivePow, base, power, return pow(abs(base), power))

// SafePositivePow: Same as pow(x,y) but considers x always positive and never exactly 0 such that
// SafePositivePow(0,y) will numerically converge to 1 as y -> 0, including SafePositivePow(0,0) returning 1.
//
// First, like PositivePow, SafePositivePow removes this warning for when you know the x value is positive or 0 and you know
// you avoid a NaN:
// ie you know that x == 0 and y > 0, such that pow(x,y) == pow(0, >0) == 0
// SafePositivePow(0, y) will however return close to 1 as y -> 0, see below.
//
// Also, pow(x,y) is most probably approximated as exp2(log2(x) * y), so pow(0,0) will give exp2(-inf * 0) == exp2(NaN) == NaN.
//
// SafePositivePow avoids NaN in allowing SafePositivePow(x,y) where (x,y) == (0,y) for any y including 0 by clamping x to a
// minimum of FLT_EPS. The consequences are:
//
// -As a replacement for pow(0,y) where y >= 1, the result of SafePositivePow(x,y) should be close enough to 0.
// -For cases where we substitute for pow(0,y) where 0 < y < 1, SafePositivePow(x,y) will quickly reach 1 as y -> 0, while
// normally pow(0,y) would give 0 instead of 1 for all 0 < y.
// eg: if we #define FLT_EPS  5.960464478e-8 (for fp32),
// SafePositivePow(0, 0.1)   = 0.1894646
// SafePositivePow(0, 0.01)  = 0.8467453
// SafePositivePow(0, 0.001) = 0.9835021
//
// Depending on the intended usage of pow(), this difference in behavior might be a moot point since:
// 1) by leaving "y" free to get to 0, we get a NaNs
// 2) the behavior of SafePositivePow() has more continuity when both x and y get closer together to 0, since
// when x is assured to be positive non-zero, pow(x,x) -> 1 as x -> 0.
//
// TL;DR: SafePositivePow(x,y) avoids NaN and is safe for positive (x,y) including (x,y) == (0,0),
//        but SafePositivePow(0, y) will return close to 1 as y -> 0, instead of 0, so watch out
//        for behavior depending on pow(0, y) giving always 0, especially for 0 < y < 1.
//
// Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb509636(v=vs.85).aspx
TEMPLATE_2_REAL(SafePositivePow, base, power, return pow(max(abs(base), real(REAL_EPS)), power))

// Helpers for making shadergraph functions consider precision spec through the same $precision token used for variable types
TEMPLATE_2_FLT(SafePositivePow_float, base, power, return pow(max(abs(base), float(FLT_EPS)), power))
TEMPLATE_2_HALF(SafePositivePow_half, base, power, return pow(max(abs(base), half(HALF_EPS)), power))

float Eps_float() { return FLT_EPS; }
float Min_float() { return FLT_MIN; }
float Max_float() { return FLT_MAX; }
half Eps_half() { return HALF_EPS; }
half Min_half() { return HALF_MIN; }
half Max_half() { return HALF_MAX; }


// Composes a floating point value with the magnitude of 'x' and the sign of 's'.
// See the comment about FastSign() below.
float CopySign(float x, float s, bool ignoreNegZero = true)
{
#if !defined(SHADER_API_GLES)
    if (ignoreNegZero)
    {
        return (s >= 0) ? abs(x) : -abs(x);
    }
    else
    {
        uint negZero = 0x80000000u;
        uint signBit = negZero & asuint(s);
        return asfloat(BitFieldInsert(negZero, signBit, asuint(x)));
    }
#else
    return (s >= 0) ? abs(x) : -abs(x);
#endif
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
    return CopySign(1.0, s, ignoreNegZero);
}

// Orthonormalizes the tangent frame using the Gram-Schmidt process.
// We assume that the normal is normalized and that the two vectors
// aren't collinear.
// Returns the new tangent (the normal is unaffected).
real3 Orthonormalize(real3 tangent, real3 normal)
{
    // TODO: use SafeNormalize()?
    return normalize(tangent - dot(tangent, normal) * normal);
}

// [start, end] -> [0, 1] : (x - start) / (end - start) = x * rcpLength - (start * rcpLength)
TEMPLATE_3_REAL(Remap01, x, rcpLength, startTimesRcpLength, return saturate(x * rcpLength - startTimesRcpLength))

// [start, end] -> [1, 0] : (end - x) / (end - start) = (end * rcpLength) - x * rcpLength
TEMPLATE_3_REAL(Remap10, x, rcpLength, endTimesRcpLength, return saturate(endTimesRcpLength - x * rcpLength))

// Remap: [0.5 / size, 1 - 0.5 / size] -> [0, 1]
real2 RemapHalfTexelCoordTo01(real2 coord, real2 size)
{
    const real2 rcpLen              = size * rcp(size - 1);
    const real2 startTimesRcpLength = 0.5 * rcp(size - 1);

    return Remap01(coord, rcpLen, startTimesRcpLength);
}

// Remap: [0, 1] -> [0.5 / size, 1 - 0.5 / size]
real2 Remap01ToHalfTexelCoord(real2 coord, real2 size)
{
    const real2 start = 0.5 * rcp(size);
    const real2 len   = 1 - rcp(size);

    return coord * len + start;
}

// smoothstep that assumes that 'x' lies within the [0, 1] interval.
real Smoothstep01(real x)
{
    return x * x * (3 - (2 * x));
}

real Smootherstep01(real x)
{
  return x * x * x * (x * (x * 6 - 15) + 10);
}

real Smootherstep(real a, real b, real t)
{
    real r = rcp(b - a);
    real x = Remap01(t, r, a * r);
    return Smootherstep01(x);
}

float3 NLerp(float3 A, float3 B, float t)
{
    return normalize(lerp(A, B, t));
}

float Length2(float3 v)
{
    return dot(v, v);
}

#ifndef BUILTIN_TARGET_API
real Pow4(real x)
{
    return (x * x) * (x * x);
}
#endif

TEMPLATE_3_FLT(RangeRemap, min, max, t, return saturate((t - min) / (max - min)))

float4x4 Inverse(float4x4 m)
{
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

// ----------------------------------------------------------------------------
// Texture utilities
// ----------------------------------------------------------------------------

float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias = 0.0)
{
    float2 ddx_ = scale * uvdx;
    float2 ddy_ = scale * uvdy;
    float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));

    return max(0.5 * log2(d) - bias, 0.0);
}

float ComputeTextureLOD(float2 uv, float bias = 0.0)
{
    float2 ddx_ = ddx(uv);
    float2 ddy_ = ddy(uv);

    return ComputeTextureLOD(ddx_, ddy_, 1.0, bias);
}

// x contains width, w contains height
float ComputeTextureLOD(float2 uv, float2 texelSize, float bias = 0.0)
{
    uv *= texelSize;

    return ComputeTextureLOD(uv, bias);
}

// LOD clamp is optional and happens outside the function.
float ComputeTextureLOD(float3 duvw_dx, float3 duvw_dy, float3 duvw_dz, float scale, float bias = 0.0)
{
    float d = Max3(dot(duvw_dx, duvw_dx), dot(duvw_dy, duvw_dy), dot(duvw_dz, duvw_dz));

    return max(0.5f * log2(d * (scale * scale)) - bias, 0.0);
}

#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
    #define MIP_COUNT_SUPPORTED 1
#endif
    // TODO: Bug workaround, switch defines GLCORE when it shouldn't
#if ((defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_VULKAN)) && !defined(SHADER_STAGE_COMPUTE)
    // OpenGL only supports textureSize for width, height, depth
    // textureQueryLevels (GL_ARB_texture_query_levels) needs OpenGL 4.3 or above and doesn't compile in compute shaders
    // tex.GetDimensions converted to textureQueryLevels
    #define MIP_COUNT_SUPPORTED 1
#endif
    // Metal doesn't support high enough OpenGL version

uint GetMipCount(TEXTURE2D_PARAM(tex, smp))
{
#if defined(MIP_COUNT_SUPPORTED)
    uint mipLevel, width, height, mipCount;
    mipLevel = width = height = mipCount = 0;
    tex.GetDimensions(mipLevel, width, height, mipCount);
    return mipCount;
#else
    return 0;
#endif
}

// ----------------------------------------------------------------------------
// Texture format sampling
// ----------------------------------------------------------------------------

// DXC no longer supports DX9-style HLSL syntax for sampler2D, tex2D and the like.
// These are emulated for backwards compatibilit using our own small structs and functions which manually combine samplers and textures.
#if defined(UNITY_COMPILER_DXC) && !defined(DXC_SAMPLER_COMPATIBILITY)
#define DXC_SAMPLER_COMPATIBILITY 1
struct sampler1D            { Texture1D t; SamplerState s; };
struct sampler2D            { Texture2D t; SamplerState s; };
struct sampler3D            { Texture3D t; SamplerState s; };
struct samplerCUBE          { TextureCube t; SamplerState s; };

float4 tex1D(sampler1D x, float v)              { return x.t.Sample(x.s, v); }
float4 tex2D(sampler2D x, float2 v)             { return x.t.Sample(x.s, v); }
float4 tex3D(sampler3D x, float3 v)             { return x.t.Sample(x.s, v); }
float4 texCUBE(samplerCUBE x, float3 v)         { return x.t.Sample(x.s, v); }

float4 tex1Dbias(sampler1D x, in float4 t)              { return x.t.SampleBias(x.s, t.x, t.w); }
float4 tex2Dbias(sampler2D x, in float4 t)              { return x.t.SampleBias(x.s, t.xy, t.w); }
float4 tex3Dbias(sampler3D x, in float4 t)              { return x.t.SampleBias(x.s, t.xyz, t.w); }
float4 texCUBEbias(samplerCUBE x, in float4 t)          { return x.t.SampleBias(x.s, t.xyz, t.w); }

float4 tex1Dlod(sampler1D x, in float4 t)           { return x.t.SampleLevel(x.s, t.x, t.w); }
float4 tex2Dlod(sampler2D x, in float4 t)           { return x.t.SampleLevel(x.s, t.xy, t.w); }
float4 tex3Dlod(sampler3D x, in float4 t)           { return x.t.SampleLevel(x.s, t.xyz, t.w); }
float4 texCUBElod(samplerCUBE x, in float4 t)       { return x.t.SampleLevel(x.s, t.xyz, t.w); }

float4 tex1Dgrad(sampler1D x, float t, float dx, float dy)              { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2Dgrad(sampler2D x, float2 t, float2 dx, float2 dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3Dgrad(sampler3D x, float3 t, float3 dx, float3 dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBEgrad(samplerCUBE x, float3 t, float3 dx, float3 dy)       { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1D(sampler1D x, float t, float dx, float dy)              { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex2D(sampler2D x, float2 t, float2 dx, float2 dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 tex3D(sampler3D x, float3 t, float3 dx, float3 dy)           { return x.t.SampleGrad(x.s, t, dx, dy); }
float4 texCUBE(samplerCUBE x, float3 t, float3 dx, float3 dy)       { return x.t.SampleGrad(x.s, t, dx, dy); }

float4 tex1Dproj(sampler1D s, in float2 t)              { return tex1D(s, t.x / t.y); }
float4 tex1Dproj(sampler1D s, in float4 t)              { return tex1D(s, t.x / t.w); }
float4 tex2Dproj(sampler2D s, in float3 t)              { return tex2D(s, t.xy / t.z); }
float4 tex2Dproj(sampler2D s, in float4 t)              { return tex2D(s, t.xy / t.w); }
float4 tex3Dproj(sampler3D s, in float4 t)              { return tex3D(s, t.xyz / t.w); }
float4 texCUBEproj(samplerCUBE s, in float4 t)          { return texCUBE(s, t.xyz / t.w); }
#endif

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
    float sinTheta = sqrt(1.0 - min(1.0, cosTheta*cosTheta));
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
// Does NOT correctly handle oblique view frustums.
// Does NOT work with orthographic projection.
// zBufferParam = { (f-n)/n, 1, (f-n)/n*f, 1/f }
float Linear01DepthFromNear(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.x + zBufferParam.y / depth);
}

// Z buffer to linear 0..1 depth (0 at camera position, 1 at far plane).
// Does NOT work with orthographic projections.
// Does NOT correctly handle oblique view frustums.
// zBufferParam = { (f-n)/n, 1, (f-n)/n*f, 1/f }
float Linear01Depth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.x * depth + zBufferParam.y);
}

// Z buffer to linear depth.
// Does NOT correctly handle oblique view frustums.
// Does NOT work with orthographic projection.
// zBufferParam = { (f-n)/n, 1, (f-n)/n*f, 1/f }
float LinearEyeDepth(float depth, float4 zBufferParam)
{
    return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
}

// Z buffer to linear depth.
// Correctly handles oblique view frustums.
// Does NOT work with orthographic projection.
// Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
float LinearEyeDepth(float2 positionNDC, float deviceDepth, float4 invProjParam)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
    float  viewSpaceZ = rcp(dot(positionCS, invProjParam));

    // If the matrix is right-handed, we have to flip the Z axis to get a positive value.
    return abs(viewSpaceZ);
}

// Z buffer to linear depth.
// Works in all cases.
// Typically, this is the cheapest variant, provided you've already computed 'positionWS'.
// Assumes that the 'positionWS' is in front of the camera.
float LinearEyeDepth(float3 positionWS, float4x4 viewMatrix)
{
    float viewSpaceZ = mul(viewMatrix, float4(positionWS, 1.0)).z;

    // If the matrix is right-handed, we have to flip the Z axis to get a positive value.
    return abs(viewSpaceZ);
}

// 'z' is the view space Z position (linear depth).
// saturate(z) the output of the function to clamp them to the [0, 1] range.
// d = log2(c * (z - n) + 1) / log2(c * (f - n) + 1)
//   = log2(c * (z - n + 1/c)) / log2(c * (f - n) + 1)
//   = log2(c) / log2(c * (f - n) + 1) + log2(z - (n - 1/c)) / log2(c * (f - n) + 1)
//   = E + F * log2(z - G)
// encodingParams = { E, F, G, 0 }
float EncodeLogarithmicDepthGeneralized(float z, float4 encodingParams)
{
    // Use max() to avoid NaNs.
    return encodingParams.x + encodingParams.y * log2(max(0, z - encodingParams.z));
}

// 'd' is the logarithmically encoded depth value.
// saturate(d) to clamp the output of the function to the [n, f] range.
// z = 1/c * (pow(c * (f - n) + 1, d) - 1) + n
//   = 1/c * pow(c * (f - n) + 1, d) + n - 1/c
//   = 1/c * exp2(d * log2(c * (f - n) + 1)) + (n - 1/c)
//   = L * exp2(d * M) + N
// decodingParams = { L, M, N, 0 }
// Graph: https://www.desmos.com/calculator/qrtatrlrba
float DecodeLogarithmicDepthGeneralized(float d, float4 decodingParams)
{
    return decodingParams.x * exp2(d * decodingParams.y) + decodingParams.z;
}

// 'z' is the view-space Z position (linear depth).
// saturate(z) the output of the function to clamp them to the [0, 1] range.
// encodingParams = { n, log2(f/n), 1/n, 1/log2(f/n) }
// This is an optimized version of EncodeLogarithmicDepthGeneralized() for (c = 2).
float EncodeLogarithmicDepth(float z, float4 encodingParams)
{
    // Use max() to avoid NaNs.
    // TODO: optimize to (log2(z) - log2(n)) / (log2(f) - log2(n)).
    return log2(max(0, z * encodingParams.z)) * encodingParams.w;
}

// 'd' is the logarithmically encoded depth value.
// saturate(d) to clamp the output of the function to the [n, f] range.
// encodingParams = { n, log2(f/n), 1/n, 1/log2(f/n) }
// This is an optimized version of DecodeLogarithmicDepthGeneralized() for (c = 2).
// Graph: https://www.desmos.com/calculator/qrtatrlrba
float DecodeLogarithmicDepth(float d, float4 encodingParams)
{
    // TODO: optimize to exp2(d * y + log2(x)).
    return encodingParams.x * exp2(d * encodingParams.y);
}

real4 CompositeOver(real4 front, real4 back)
{
    return front + (1 - front.a) * back;
}

void CompositeOver(real3 colorFront, real3 alphaFront,
                   real3 colorBack,  real3 alphaBack,
                   out real3 color,  out real3 alpha)
{
    color = colorFront + (1 - alphaFront) * colorBack;
    alpha = alphaFront + (1 - alphaFront) * alphaBack;
}

// ----------------------------------------------------------------------------
// Space transformations
// ----------------------------------------------------------------------------

static const float3x3 k_identity3x3 = {1, 0, 0,
                                       0, 1, 0,
                                       0, 0, 1};

static const float4x4 k_identity4x4 = {1, 0, 0, 0,
                                       0, 1, 0, 0,
                                       0, 0, 1, 0,
                                       0, 0, 0, 1};

float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

#if UNITY_UV_STARTS_AT_TOP
    // Our world space, view space, screen space and NDC space are Y-up.
    // Our clip space is flipped upside-down due to poor legacy Unity design.
    // The flip is baked into the projection matrix, so we only have to flip
    // manually when going from CS to NDC and back.
    positionCS.y = -positionCS.y;
#endif

    return positionCS;
}

// Use case examples:
// (position = positionCS) => (clipSpaceTransform = use default)
// (position = positionVS) => (clipSpaceTransform = UNITY_MATRIX_P)
// (position = positionWS) => (clipSpaceTransform = UNITY_MATRIX_VP)
float4 ComputeClipSpacePosition(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
{
    return mul(clipSpaceTransform, float4(position, 1.0));
}

// The returned Z value is the depth buffer value (and NOT linear view space Z value).
// Use case examples:
// (position = positionCS) => (clipSpaceTransform = use default)
// (position = positionVS) => (clipSpaceTransform = UNITY_MATRIX_P)
// (position = positionWS) => (clipSpaceTransform = UNITY_MATRIX_VP)
float3 ComputeNormalizedDeviceCoordinatesWithZ(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
{
    float4 positionCS = ComputeClipSpacePosition(position, clipSpaceTransform);

#if UNITY_UV_STARTS_AT_TOP
    // Our world space, view space, screen space and NDC space are Y-up.
    // Our clip space is flipped upside-down due to poor legacy Unity design.
    // The flip is baked into the projection matrix, so we only have to flip
    // manually when going from CS to NDC and back.
    positionCS.y = -positionCS.y;
#endif

    positionCS *= rcp(positionCS.w);
    positionCS.xy = positionCS.xy * 0.5 + 0.5;

    return positionCS.xyz;
}

// Use case examples:
// (position = positionCS) => (clipSpaceTransform = use default)
// (position = positionVS) => (clipSpaceTransform = UNITY_MATRIX_P)
// (position = positionWS) => (clipSpaceTransform = UNITY_MATRIX_VP)
float2 ComputeNormalizedDeviceCoordinates(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
{
    return ComputeNormalizedDeviceCoordinatesWithZ(position, clipSpaceTransform).xy;
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

float3 ComputeWorldSpacePosition(float4 positionCS, float4x4 invViewProjMatrix)
{
    float4 hpositionWS = mul(invViewProjMatrix, positionCS);
    return hpositionWS.xyz / hpositionWS.w;
}

// ----------------------------------------------------------------------------
// PositionInputs
// ----------------------------------------------------------------------------

// Note: if you modify this struct, be sure to update the CustomPassFullscreenShader.template
struct PositionInputs
{
    float3 positionWS;  // World space position (could be camera-relative)
    float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    float  linearDepth; // View space Z coordinate                              : [Near, Far]
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
#if defined(SHADER_STAGE_COMPUTE) || defined(SHADER_STAGE_RAY_TRACING)
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

// For Raytracing only
// This function does not initialize deviceDepth and linearDepth
PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, float3 positionWS)
{
    PositionInputs posInput = GetPositionInput(positionSS, invScreenSize, uint2(0, 0));
    posInput.positionWS = positionWS;

    return posInput;
}

// From forward
// deviceDepth and linearDepth come directly from .zw of SV_Position
PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS, uint2 tileCoord)
{
    PositionInputs posInput = GetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = positionWS;
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = linearDepth;

    return posInput;
}

PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS)
{
    return GetPositionInput(positionSS, invScreenSize, deviceDepth, linearDepth, positionWS, uint2(0, 0));
}

// From deferred or compute shader
// depth must be the depth from the raw depth buffer. This allow to handle all kind of depth automatically with the inverse view projection matrix.
// For information. In Unity Depth is always in range 0..1 (even on OpenGL) but can be reversed.
PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
    float4x4 invViewProjMatrix, float4x4 viewMatrix,
    uint2 tileCoord)
{
    PositionInputs posInput = GetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = ComputeWorldSpacePosition(posInput.positionNDC, deviceDepth, invViewProjMatrix);
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = LinearEyeDepth(posInput.positionWS, viewMatrix);

    return posInput;
}

PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
                                float4x4 invViewProjMatrix, float4x4 viewMatrix)
{
    return GetPositionInput(positionSS, invScreenSize, deviceDepth, invViewProjMatrix, viewMatrix, uint2(0, 0));
}

// The view direction 'V' points towards the camera.
// 'depthOffsetVS' is always applied in the opposite direction (-V).
void ApplyDepthOffsetPositionInput(float3 V, float depthOffsetVS, float3 viewForwardDir, float4x4 viewProjMatrix, inout PositionInputs posInput)
{
    posInput.positionWS += depthOffsetVS * (-V);
    posInput.deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(posInput.positionWS, viewProjMatrix).z;

    // Transform the displacement along the view vector to the displacement along the forward vector.
    // Use abs() to make sure we get the sign right.
    // 'depthOffsetVS' applies in the direction away from the camera.
    posInput.linearDepth += depthOffsetVS * abs(dot(V, viewForwardDir));
}

// ----------------------------------------------------------------------------
// Terrain/Brush heightmap encoding/decoding
// ----------------------------------------------------------------------------

#if defined(SHADER_API_VULKAN) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)

// For the built-in target this is already a defined symbol
#ifndef BUILTIN_TARGET_API
real4 PackHeightmap(real height)
{
    uint a = (uint)(65535.0 * height);
    return real4((a >> 0) & 0xFF, (a >> 8) & 0xFF, 0, 0) / 255.0;
}

real UnpackHeightmap(real4 height)
{
    return (height.r + height.g * 256.0) / 257.0; // (255.0 * height.r + 255.0 * 256.0 * height.g) / 65535.0
}
#endif

#else

// For the built-in target this is already a defined symbol
#ifndef BUILTIN_TARGET_API
real4 PackHeightmap(real height)
{
    return real4(height, 0, 0, 0);
}

real UnpackHeightmap(real4 height)
{
    return height.r;
}
#endif

#endif

// ----------------------------------------------------------------------------
// Misc utilities
// ----------------------------------------------------------------------------

// Simple function to test a bitfield
bool HasFlag(uint bitfield, uint flag)
{
    return (bitfield & flag) != 0;
}

// Normalize that account for vectors with zero length
real3 SafeNormalize(float3 inVec)
{
    real dp3 = max(REAL_MIN, dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

// Checks if a vector is normalized
bool IsNormalized(float3 inVec)
{
    real l = length(inVec);
    return length(l) < 1.0001 && length(l) > 0.9999;
}

// Division which returns 1 for (inf/inf) and (0/0).
// If any of the input parameters are NaNs, the result is a NaN.
real SafeDiv(real numer, real denom)
{
    return (numer != denom) ? numer / denom : 1;
}

// Perform a square root safe of imaginary number.
real SafeSqrt(real x)
{
    return sqrt(max(0, x));
}

// Assumes that (0 <= x <= Pi).
real SinFromCos(real cosX)
{
    return sqrt(saturate(1 - cosX * cosX));
}

// Dot product in spherical coordinates.
real SphericalDot(real cosTheta1, real phi1, real cosTheta2, real phi2)
{
    return SinFromCos(cosTheta1) * SinFromCos(cosTheta2) * cos(phi1 - phi2) + cosTheta1 * cosTheta2;
}

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
    // note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    return float4(uv * 2.0 - 1.0, z, 1.0);
}


// draw procedural with 2 triangles has index order (0,1,2)  (0,2,3)

// 0 - 0,0
// 1 - 0,1
// 2 - 1,1
// 3 - 1,0

float2 GetQuadTexCoord(uint vertexID)
{
    uint topBit = vertexID >> 1;
    uint botBit = (vertexID & 1);
    float u = topBit;
    float v = (topBit + botBit) & 1; // produces 0 for indices 0,3 and 1 for 1,2
#if UNITY_UV_STARTS_AT_TOP
    v = 1.0 - v;
#endif
    return float2(u, v);
}

// 0 - 0,1
// 1 - 0,0
// 2 - 1,0
// 3 - 1,1
float4 GetQuadVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
{
    uint topBit = vertexID >> 1;
    uint botBit = (vertexID & 1);
    float x = topBit;
    float y = 1 - (topBit + botBit) & 1; // produces 1 for indices 0,3 and 0 for 1,2
    return float4(x, y, z, 1.0);
}

#if !defined(SHADER_API_GLES) && !defined(SHADER_STAGE_RAY_TRACING)

// LOD dithering transition helper
// LOD0 must use this function with ditherFactor 1..0
// LOD1 must use this function with ditherFactor -1..0
// This is what is provided by unity_LODFade
void LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)
{
    // Generate a spatially varying pattern.
    // Unfortunately, varying the pattern with time confuses the TAA, increasing the amount of noise.
    float p = GenerateHashedRandomFloat(fadeMaskSeed);

    // This preserves the symmetry s.t. if LOD 0 has f = x, LOD 1 has f = -x.
    float f = ditherFactor - CopySign(p, ditherFactor);
    clip(f);
}

#endif

// The resource that is bound when binding a stencil buffer from the depth buffer is two channel. On D3D11 the stencil value is in the green channel,
// while on other APIs is in the red channel. Note that on some platform, always using the green channel might work, but is not guaranteed.
uint GetStencilValue(uint2 stencilBufferVal)
{
#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_GAMECORE)
    return stencilBufferVal.y;
#else
    return stencilBufferVal.x;
#endif
}

// Sharpens the alpha of a texture to the width of a single pixel
// Used for alpha to coverage
// source: https://medium.com/@bgolus/anti-aliased-alpha-test-the-esoteric-alpha-to-coverage-8b177335ae4f
float SharpenAlpha(float alpha, float alphaClipTreshold)
{
    return saturate((alpha - alphaClipTreshold) / max(fwidth(alpha), 0.0001) + 0.5);
}

// These clamping function to max of floating point 16 bit are use to prevent INF in code in case of extreme value
TEMPLATE_1_REAL(ClampToFloat16Max, value, return min(value, HALF_MAX))

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3
#pragma warning (enable : 3205) // conversion of larger type to smaller
#endif

#endif // UNITY_COMMON_INCLUDED
