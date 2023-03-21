///
/// Scalable Temporal Post-Processing (STP) Common Shader Code
///
/// This file provides shader helper functions which are intended to be used with the C# helper functions in
/// STPUtils.cs. The main purpose of this file is to reduce the amount of redundant shader code that would need to be
/// added to an SRP in order to integrate STP.
///
/// Usage:
/// - Call SetXConstants function on a command buffer in C#
/// - Launch shader (via draw or dispatch)
/// - Call associated ApplyX function provided by this file
///
/// By default, no shader functions are available until they are specifically requested via define.
///
/// The following defines can be used to enable shader functionality:
/// - STP_IN
///     - Enables the "Inline" pass
/// - STP_TAA
///     - Enables the "TAA" pass
/// - STP_CLN
///     - Enables the "Cleaner" pass
///
/// For portability reasons, the caller must provide a few samplers via defines:
/// - STP_POINT_CLAMP_SAMPLER
///     - Specifies a sampler with point filtering and clamp wrapping
/// - STP_LINEAR_CLAMP_SAMPLER
///     - Specifies a sampler with linear filtering and clamp wrapping
///
/// Each shader pass has its own specific required defines:
///
/// Inline (ApplyStpInline)
///
/// - STP_PRIOR_LUMA_TEXTURE
///     - Specifies a texture that contains STP’s luma data for the previous frame
/// - STP_PRIOR_DEPTH_MOTION_TEXTURE
///     - Specifies a texture that contains STP’s combined depth+motion data for the previous frame
/// - STP_PRIOR_FEEDBACK_TEXTURE
///     - Specifies a texture that contains STP’s feedback data for the previous frame
///
/// - STP_INPUT_COLOR_TEXTURE
///     - Specifies a texture that contains the current frame’s color
/// - STP_INPUT_DEPTH_TEXTURE
///     - Specifies a texture that contains the current frame’s depth
/// - STP_INPUT_MOTION_TEXTURE
///     - Specifies a texture that contains the current frame’s motion vectors
///
/// - STP_INPUT_STENCIL_TEXTURE [Optional]
///     - Specifies a texture that contains the current frame's stencil data which is used to drive the responsive feature
///
/// TAA (ApplyStpTaa)
///
/// - STP_INTERMEDIATE_COLOR_TEXTURE
///     - Specifies a texture that contains intermediate STP color data
/// - STP_LUMA_TEXTURE
///     - Specifies a texture that contains STP’s luma data for the current frame
/// - STP_DEPTH_MOTION_TEXTURE
///     - Specifies a texture that contains STP’s combined depth+motion data for the current frame
/// - STP_PRIOR_FEEDBACK_TEXTURE
///     - Specifies a texture that contains STP’s feedback data for the previous frame
///
/// Cleaner (ApplyStpCleaner) [Optional]
///
/// - STP_FEEDBACK_TEXTURE
///     - Specifies a texture that contains STP’s feedback data for the current frame
///

// Ensure that the shader including this file is targeting an adequate shader level
#if SHADER_TARGET < 45
#error Scalable Temporal Post-Processing requires a shader target of 4.5 or greater
#endif

// Indicate that we'll be using the HLSL implementation of STP
#define STP_HLSL 1
#define STP_GPU 1

// Disable grain since we don't currently have a way to integrate this with the rest of Unity's post processing
#define STP_GRAIN 0

// Enable the minimum precision path when supported by the shader environment
#if REAL_IS_HALF
    #define STP_MEDIUM 1
#endif

#if defined(UNITY_DEVICE_SUPPORTS_NATIVE_16BIT)
    #define STP_16BIT 1

    // Disable denormals on mobile and PlayStation (PlayStation doesn't seem to enable denorm support with fp16?)
    // NOTE: We will likely need more robust logic for this since it's possible for desktop HW to lack denormal support too
    #if (defined(SHADER_API_MOBILE) || defined(SHADER_API_PSSL))
        #define STP_BUG_DENORMAL 1
    #endif

    // We currently enable this to work around a saturate() related bug on AMD GPUs
    #define STP_BUG_SAT 1
#else
    #define STP_32BIT 1
#endif

// Default to low quality if the user didn't specify
#if !defined(STP_CONFIG_1)
    #define STP_CONFIG_0 1
#endif

// Include the STP HLSL file
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/Stp.hlsl"

//
// Common
//

float _StpCommonConstant;

#define STP_COMMON_CONSTANT asuint(_StpCommonConstant)

TEXTURE2D(_StpBlueNoiseIn);

uint DecodeNoiseWidthMinusOne(uint param)
{
    return param & 0xFF;
}

bool DecodeHasValidHistory(uint param)
{
    return (param >> 8) & 1;
}

uint DecodeStencilMask(uint param)
{
    return (param >> 16) & 0xFF;
}

#if STP_32BIT
StpMF1 StpDitF1(StpU2 o)
{
    uint noiseWidthMinusOne = DecodeNoiseWidthMinusOne(STP_COMMON_CONSTANT);
    return (StpMF1)LOAD_TEXTURE2D_LOD(_StpBlueNoiseIn, o & noiseWidthMinusOne, 0).a;
}
StpMF3 StpDitF3(StpU2 o) { return (StpMF3)StpDitF1(o); }
#endif

#if STP_16BIT
StpH1 StpDitH1(StpU2 o)
{
    uint noiseWidthMinusOne = DecodeNoiseWidthMinusOne(STP_COMMON_CONSTANT);
    return (StpH1)LOAD_TEXTURE2D_LOD(_StpBlueNoiseIn, o & noiseWidthMinusOne, 0).a;
}
StpH3 StpDitH3(StpU2 o) { return (StpH3)StpDitH1(o); }
#endif

///
/// Inline
///

#if defined(STP_IN)
float4 _StpInlineConstants0;
float4 _StpInlineConstants1;
float4 _StpInlineConstants2;
float4 _StpInlineConstants3;
float4 _StpInlineConstants4;
float4 _StpInlineConstants5;
float4 _StpInlineConstants6;
float4 _StpInlineConstants7;
float4 _StpInlineConstants8;
float4 _StpInlineConstants9;
float4 _StpInlineConstantsA;
float4 _StpInlineConstantsB;
float4 _StpInlineConstantsC;
float4 _StpInlineConstantsD;

#define STP_INLINE_CONSTANTS_0 asuint(_StpInlineConstants0)
#define STP_INLINE_CONSTANTS_1 asuint(_StpInlineConstants1)
#define STP_INLINE_CONSTANTS_2 asuint(_StpInlineConstants2)
#define STP_INLINE_CONSTANTS_3 asuint(_StpInlineConstants3)
#define STP_INLINE_CONSTANTS_4 asuint(_StpInlineConstants4)
#define STP_INLINE_CONSTANTS_5 asuint(_StpInlineConstants5)
#define STP_INLINE_CONSTANTS_6 asuint(_StpInlineConstants6)
#define STP_INLINE_CONSTANTS_7 asuint(_StpInlineConstants7)
#define STP_INLINE_CONSTANTS_8 asuint(_StpInlineConstants8)
#define STP_INLINE_CONSTANTS_9 asuint(_StpInlineConstants9)
#define STP_INLINE_CONSTANTS_A asuint(_StpInlineConstantsA)
#define STP_INLINE_CONSTANTS_B asuint(_StpInlineConstantsB)
#define STP_INLINE_CONSTANTS_C asuint(_StpInlineConstantsC)
#define STP_INLINE_CONSTANTS_D asuint(_StpInlineConstantsD)

#if defined(STP_32BIT)
StpMF4 StpInPriLumF(float2 uv) { return (StpMF4)GATHER_RED_TEXTURE2D_X(STP_PRIOR_LUMA_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpU4 StpInPriMot4F(float2 uv) { return GATHER_RED_TEXTURE2D_X(STP_PRIOR_DEPTH_MOTION_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpInPriFedF(float2 uv) { return (StpMF4)SAMPLE_TEXTURE2D_X_LOD(STP_PRIOR_FEEDBACK_TEXTURE, STP_LINEAR_CLAMP_SAMPLER, uv, 0); }
void StpInDatF(
    inout StpF1 r,
    inout StpMF3 c,
    inout StpF1 z,
    inout StpF2 m,
    StpU2 o)
{
    c = (StpMF3)LOAD_TEXTURE2D_X_LOD(STP_INPUT_COLOR_TEXTURE, o, 0).rgb;

    z = LinearEyeDepth(LOAD_TEXTURE2D_X_LOD(STP_INPUT_DEPTH_TEXTURE, o, 0).x, _ZBufferParams);

    float2 motion = LOAD_TEXTURE2D_X_LOD(STP_INPUT_MOTION_TEXTURE, o, 0).xy;
    m = motion > 1.0 ? 0.0 : motion;

    // Activate the "responsive" feature when we don't have valid history textures
    bool hasValidHistory = DecodeHasValidHistory(STP_COMMON_CONSTANT);
    bool excludeTaa = false;
#ifdef STP_INPUT_STENCIL_TEXTURE
    excludeTaa = (GetStencilValue(LOAD_TEXTURE2D_X_LOD(STP_INPUT_STENCIL_TEXTURE, o, 0).xy) & DecodeStencilMask(STP_COMMON_CONSTANT)) != 0;
#endif
    r = (hasValidHistory && !excludeTaa) ? 1.0 : 0.0;
}
StpMF1 StpInDitF(StpU2 o) { return StpDitF1(o); }
#endif

#if defined(STP_16BIT)
StpH4 StpInPriLumH(float2 uv) { return (StpH4)GATHER_RED_TEXTURE2D_X(STP_PRIOR_LUMA_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpU4 StpInPriMot4H(float2 uv) { return GATHER_RED_TEXTURE2D_X(STP_PRIOR_DEPTH_MOTION_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpInPriFedH(float2 uv) { return (StpH4)SAMPLE_TEXTURE2D_X_LOD(STP_PRIOR_FEEDBACK_TEXTURE, STP_LINEAR_CLAMP_SAMPLER, uv, 0); }
void StpInDatH(
    inout StpF1 r,
    inout StpH3 c,
    inout StpF1 z,
    inout StpF2 m,
    StpU2 o)
{
    c = (StpH3)LOAD_TEXTURE2D_X_LOD(STP_INPUT_COLOR_TEXTURE, o, 0).rgb;

    z = LinearEyeDepth(LOAD_TEXTURE2D_X_LOD(STP_INPUT_DEPTH_TEXTURE, o, 0).x, _ZBufferParams);

    float2 motion = LOAD_TEXTURE2D_X_LOD(STP_INPUT_MOTION_TEXTURE, o, 0).xy;
    m = motion > 1.0 ? 0.0 : motion;

    // Activate the "responsive" feature when we don't have valid history textures
    bool hasValidHistory = DecodeHasValidHistory(STP_COMMON_CONSTANT);
    bool excludeTaa = false;
#ifdef STP_INPUT_STENCIL_TEXTURE
    excludeTaa = (GetStencilValue(LOAD_TEXTURE2D_X_LOD(STP_INPUT_STENCIL_TEXTURE, o, 0).xy) & DecodeStencilMask(STP_COMMON_CONSTANT)) != 0;
#endif
    r = (hasValidHistory && !excludeTaa) ? 1.0 : 0.0;
}
StpH1 StpInDitH(StpU2 o) { return StpDitH1(o); }
#endif

void ApplyStpInline(
    out half4 outColor,
    out uint outDepthMotion,
    out half outLuma,
    uint2 pos
)
{
#if defined(STP_16BIT)
    StpInH(
#else
    StpInF(
#endif
        outColor,
        outDepthMotion,
        outLuma,
        pos,
        STP_INLINE_CONSTANTS_0,
        STP_INLINE_CONSTANTS_1,
        STP_INLINE_CONSTANTS_2,
        STP_INLINE_CONSTANTS_3,
        STP_INLINE_CONSTANTS_4,
        STP_INLINE_CONSTANTS_5,
        STP_INLINE_CONSTANTS_6,
        STP_INLINE_CONSTANTS_7,
        STP_INLINE_CONSTANTS_8,
        STP_INLINE_CONSTANTS_9,
        STP_INLINE_CONSTANTS_A,
        STP_INLINE_CONSTANTS_B,
        STP_INLINE_CONSTANTS_C,
        STP_INLINE_CONSTANTS_D
    );
}
#endif

///
/// Taa
///

#if defined(STP_TAA)
float4 _StpTaaConstants0;
float4 _StpTaaConstants1;
float4 _StpTaaConstants2;
float4 _StpTaaConstants3;
float4 _StpTaaConstants4;

#define STP_TAA_CONSTANTS_0 asuint(_StpTaaConstants0)
#define STP_TAA_CONSTANTS_1 asuint(_StpTaaConstants1)
#define STP_TAA_CONSTANTS_2 asuint(_StpTaaConstants2)
#define STP_TAA_CONSTANTS_3 asuint(_StpTaaConstants3)
#define STP_TAA_CONSTANTS_4 asuint(_StpTaaConstants4)

#if defined(STP_32BIT)
StpMF4 StpTaaCol4RF(float2 uv) { return (StpMF4)GATHER_RED_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpTaaCol4GF(float2 uv) { return (StpMF4)GATHER_GREEN_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpTaaCol4BF(float2 uv) { return (StpMF4)GATHER_BLUE_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpTaaCol4AF(float2 uv) { return (StpMF4)GATHER_ALPHA_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpTaaLum4F(float2 uv) { return (StpMF4)GATHER_RED_TEXTURE2D_X(STP_LUMA_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpU4 StpTaaMot4F(float2 uv) { return GATHER_RED_TEXTURE2D_X(STP_DEPTH_MOTION_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpMF4 StpTaaPriFedF(float2 uv) { return (StpMF4)SAMPLE_TEXTURE2D_X_LOD(STP_PRIOR_FEEDBACK_TEXTURE, STP_LINEAR_CLAMP_SAMPLER, uv, 0); }
StpMF1 StpTaaPriFedMaxAF(float2 uv)
{
    StpMF4 f = (StpMF4)GATHER_ALPHA_TEXTURE2D_X(STP_PRIOR_FEEDBACK_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv);
    return max(StpMax3MF1(f.x, f.y, f.z), f.w);
}
StpMF3 StpTaaDitF(StpU2 o) { return StpDitF3(o); }
#endif

#if defined(STP_16BIT)
StpH4 StpTaaCol4RH(float2 uv) { return (StpH4)GATHER_RED_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpTaaCol4GH(float2 uv) { return (StpH4)GATHER_GREEN_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpTaaCol4BH(float2 uv) { return (StpH4)GATHER_BLUE_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpTaaCol4AH(float2 uv) { return (StpH4)GATHER_ALPHA_TEXTURE2D_X(STP_INTERMEDIATE_COLOR_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpTaaLum4H(float2 uv) { return (StpH4)GATHER_RED_TEXTURE2D_X(STP_LUMA_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpU4 StpTaaMot4H(float2 uv) { return GATHER_RED_TEXTURE2D_X(STP_DEPTH_MOTION_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv); }
StpH4 StpTaaPriFedH(float2 uv) { return (StpH4)SAMPLE_TEXTURE2D_X_LOD(STP_PRIOR_FEEDBACK_TEXTURE, STP_LINEAR_CLAMP_SAMPLER, uv, 0); }
StpH1 StpTaaPriFedMaxAH(float2 uv)
{
    StpH4 f = (StpH4)GATHER_ALPHA_TEXTURE2D_X(STP_PRIOR_FEEDBACK_TEXTURE, STP_POINT_CLAMP_SAMPLER, uv);
    return max(StpMax3H1(f.x, f.y, f.z), f.w);
}
StpH3 StpTaaDitH(StpU2 o) { return StpDitH3(o); }
#endif

void ApplyStpTaa(
    out half4 outColor,
    out half4 outFeedback,
    uint2 pos
)
{
#if defined(STP_16BIT)
    StpTaaH(
#else
    StpTaaF(
#endif
        outColor,
        outFeedback,
        pos,
        STP_TAA_CONSTANTS_0,
        STP_TAA_CONSTANTS_1,
        STP_TAA_CONSTANTS_2,
        STP_TAA_CONSTANTS_3,
        STP_TAA_CONSTANTS_4
    );
}
#endif

///
/// Cleaner
///

#if defined(STP_CLN)
float4 _StpCleanerConstants0;
float4 _StpCleanerConstants1;

#define STP_CLEANER_CONSTANTS_0 asuint(_StpCleanerConstants0)
#define STP_CLEANER_CONSTANTS_1 asuint(_StpCleanerConstants1)

#if defined(STP_32BIT)
StpMF4 StpClnFedF(StpU2 o) { return (StpMF4)LOAD_TEXTURE2D_X(STP_FEEDBACK_TEXTURE, o); }
StpMF3 StpClnDitF(StpU2 o) { return StpDitF3(o); }
#endif

#if defined(STP_16BIT)
StpH4 StpClnFedH(StpW2 o) { return (StpH4)LOAD_TEXTURE2D_X(STP_FEEDBACK_TEXTURE, o); }
StpH3 StpClnDitH(StpW2 o) { return StpDitH3(o); }
#endif

void ApplyStpCleaner(
    out half4 outColor,
    uint2 pos
)
{
#if defined(STP_16BIT)
    StpClnH(
#else
    StpClnF(
#endif
        outColor,
        pos,
        STP_CLEANER_CONSTANTS_0,
        STP_CLEANER_CONSTANTS_1
    );
}
#endif

