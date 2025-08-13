// This is necessary to prevent Unity from deciding that our default config logic is actually an include guard declaration
#ifndef STP_COMMON_UNITY_INCLUDE_GUARD
#define STP_COMMON_UNITY_INCLUDE_GUARD

///
/// Spatial-Temporal Post-Processing (STP) Common Shader Code
///
/// This file provides configuration defines and other common utilities associated with STP
/// See STP.cs for more details on how this shader code interacts with C#
///
/// Usage:
/// - Add control defines
/// - Include this file in a shader pass associated with STP
/// - Call relevant STP function
///
/// By default, no shader functions are available until they are specifically requested via define.
///
/// The following defines can be used to enable shader functionality:
/// - STP_PAT
///     - Enables the "Pattern" pass
/// - STP_DIL
///     - Enables the "Dilation" pass
/// - STP_SAA
///     - Enables the "Spatial Anti-Aliasing" pass
/// - STP_TAA
///     - Enables the "TAA" pass

// Indicate that we'll be using the HLSL implementation of STP
#define STP_HLSL 1
#define STP_GPU 1

// Disable grain since we don't currently have a way to integrate this with the rest of Unity's post processing
#define STP_GRAIN 0

// Enable the minimum precision path when supported by the shader environment.
#if REAL_IS_HALF || defined(UNITY_DEVICE_SUPPORTS_NATIVE_16BIT)
    #define STP_MEDIUM 1
#endif

// Mobile platforms use a simplified version of STP to reduce runtime overhead.
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH) || defined(SHADER_API_SWITCH2)
    #define STP_TAA_Q 0
#endif

#if defined(SHADER_API_SWITCH) || defined(SHADER_API_SWITCH2)
    #define STP_BUG_SAT_INF 1
#endif

// Enable workarounds that help us avoid issues on Metal
#if defined(SHADER_API_METAL)
    // Relying on infinity behavior causes issues in the on-screen inline pass calculations
    // We expect this option to be required on Metal because the shading language spec states that the fast-math
    // option is on by default which disables support for proper INF handling.
    #define STP_BUG_SAT_INF 1
#endif

#if defined(SHADER_API_PSSL)
    #define STP_BUG_SAT_INF 1
#endif

#if defined(UNITY_DEVICE_SUPPORTS_NATIVE_16BIT)
    #define STP_16BIT 1

    #if defined(SHADER_API_PSSL)
        #define STP_BUG_PRX 1
    #endif
#else
    #define STP_32BIT 1
#endif

#if defined(ENABLE_DEBUG_MODE)
    #define STP_BUG 1
#endif

// Include the STP HLSL files
#include "Packages/com.unity.render-pipelines.core/Runtime/STP/Stp.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/STP/STP.cs.hlsl"

// Include TextureXR.hlsl for XR macros
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"

//
// Common
//

#if defined(ENABLE_LARGE_KERNEL)
#define STP_GROUP_SIZE 128
#define STP_GROUP_SIZE_SHIFT_X 3
#define STP_GROUP_SIZE_SHIFT_Y 4
#else
#define STP_GROUP_SIZE 64
#define STP_GROUP_SIZE_SHIFT_X 3
#define STP_GROUP_SIZE_SHIFT_Y 3
#endif

#if defined(STP_16BIT)
StpW2 ComputeGroupPos(StpW2 groupId)
{
    return StpW2(
        groupId.x << StpW1_(STP_GROUP_SIZE_SHIFT_X),
        groupId.y << StpW1_(STP_GROUP_SIZE_SHIFT_Y)
    );
}
#else
StpMU2 ComputeGroupPos(StpMU2 groupId)
{
    return StpMU2(
        groupId.x << StpMU1_(STP_GROUP_SIZE_SHIFT_X),
        groupId.y << StpMU1_(STP_GROUP_SIZE_SHIFT_Y)
    );
}
#endif

#define STP_COMMON_CONSTANT asuint(_StpCommonConstant.x)
#define STP_ZBUFFER_PARAMS_Z _StpCommonConstant.y
#define STP_ZBUFFER_PARAMS_W _StpCommonConstant.z

TEXTURE2D(_StpBlueNoiseIn);

RW_TEXTURE2D_X(float4, _StpDebugOut);

SAMPLER(s_point_clamp_sampler);
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_linear_repeat_sampler);

#if defined(STP_32BIT)
StpMU1 StpBfeF(StpMU1 data, StpMU1 offset, StpMU1 numBits)
{
    StpMU1 mask = (StpMU1(1) << numBits) - StpMU1(1);
    return (data >> offset) & mask;
}

StpMU1 StpBfiF(StpMU1 mask, StpMU1 src, StpMU1 dst)
{
    return (src & mask) | (dst & ~mask);
}

StpMU2 StpRemapLaneTo8x16F(StpMU1 i)
{
    return StpMU2(StpBfiF(StpMU1(1), i, StpBfeF(i, StpMU1(2), StpMU1(3))),
        StpBfiF(StpMU1(3), StpBfeF(i, StpMU1(1), StpMU1(2)), StpBfeF(i, StpMU1(3), StpMU1(4))));
}

StpMU1 DecodeNoiseWidthMinusOneF(StpMU1 param)
{
    return param & StpMU1(0xFF);
}
#endif

#if defined(STP_16BIT)
StpW1 StpBfeH(StpW1 data, StpW1 offset, StpW1 numBits)
{
    StpW1 mask = (StpW1(1) << numBits) - StpW1(1);
    return (data >> offset) & mask;
}

StpW1 StpBfiH(StpW1 mask, StpW1 src, StpW1 dst)
{
    return (src & mask) | (dst & ~mask);
}

StpW2 StpRemapLaneTo8x16H(StpW1 i)
{
    return StpW2(StpBfiH(StpW1(1), i, StpBfeH(i, StpW1(2), StpW1(3))),
        StpBfiH(StpW1(3), StpBfeH(i, StpW1(1), StpW1(2)), StpBfeH(i, StpW1(3), StpW1(4))));
}

StpW1 DecodeNoiseWidthMinusOneH(StpW1 param)
{
    return param & StpW1(0xFF);
}
#endif

bool DecodeHasValidHistory(uint param)
{
    return (param >> 8) & 1;
}

uint DecodeStencilMask(uint param)
{
    return (param >> 16) & 0xFF;
}

uint DecodeDebugViewIndex(uint param)
{
    return (param >> 24) & 0xFF;
}

#if defined(STP_32BIT)
StpMF1 StpDitF1(StpMU2 o)
{
    StpMU1 noiseWidthMinusOne = DecodeNoiseWidthMinusOneF(StpMU1(STP_COMMON_CONSTANT));
    return (StpMF1)LOAD_TEXTURE2D_LOD(_StpBlueNoiseIn, o & noiseWidthMinusOne, 0).a;
}
// TODO: Broadcast one value as all three outputs a bug that will effect 'STP_GRAIN=3' output.
StpMF3 StpDitF3(StpMU2 o) { return (StpMF3)StpDitF1(o); }
#endif

// NOTE: This function is used by both the 32-bit path, and the 16-bit path (when various workarounds are active)
void StpBugF(StpU3 p, StpF4 c)
{
    if (p.z == DecodeDebugViewIndex(STP_COMMON_CONSTANT))
        _StpDebugOut[COORD_TEXTURE2D_X(p.xy)] = c;
}

#if defined(STP_16BIT)
StpH1 StpDitH1(StpW2 o)
{
    StpW1 noiseWidthMinusOne = DecodeNoiseWidthMinusOneH(StpW1(STP_COMMON_CONSTANT));
    return (StpH1)LOAD_TEXTURE2D_LOD(_StpBlueNoiseIn, o & noiseWidthMinusOne, 0).a;
}
// TODO: Broadcast one value as all three outputs a bug that will effect 'STP_GRAIN=3' output.
StpH3 StpDitH3(StpW2 o) { return (StpH3)StpDitH1(o); }
#endif

#endif // STP_COMMON_UNITY_INCLUDE_GUARD
