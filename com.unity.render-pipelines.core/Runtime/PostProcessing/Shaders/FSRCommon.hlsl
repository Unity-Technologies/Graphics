///
/// FidelityFX Super Resolution (FSR) Common Shader Code
///
/// This file provides shader helper functions which are intended to be used with the C# helper functions in
/// FSRUtils.cs. The main purpose of this file is to simplify the usage of the external FSR shader functions within
/// the Unity shader environment.
///
/// Usage:
/// - Call SetXConstants function on a command buffer in C#
/// - Launch shader (via draw or dispatch)
/// - Call associated ApplyX function provided by this file
///
/// The following preprocessor parameters MUST be defined before including this file:
/// - FSR_INPUT_TEXTURE
///     - The texture to use as input for the FSR passes
/// - FSR_INPUT_SAMPLER
///     - The sample to use for FSR_INPUT_TEXTURE
///
/// The following preprocessor parameters are optional and MAY be defined before including this file:
/// - FSR_ENABLE_ALPHA
///     - Enables alpha pass-through functionality for the RCAS pass
///

// Ensure that the shader including this file is targeting an adequate shader level
#if SHADER_TARGET < 45
#error FidelityFX Super Resolution requires a shader target of 4.5 or greater
#endif

// Indicate that we'll be using the HLSL implementation of FSR
#define A_GPU 1
#define A_HLSL 1

// Enable either the 16-bit or the 32-bit implementation of FSR depending on platform support
// Note: There are known issues relating to the math approximation functions on some DX11 drivers when FP16 is used.
//       Due to this issue, we currently prevent the 16-bit implementation from being used when DX11 is detected.
#if REAL_IS_HALF && !defined(SHADER_API_D3D11)
    #define A_HALF
    #define FSR_EASU_H 1
    #define FSR_RCAS_H 1
#else
    #define FSR_EASU_F 1
    #define FSR_RCAS_F 1
#endif

// Enable the RCAS passthrough alpha feature when the alpha channel is in use
#if FSR_ENABLE_ALPHA
    #define FSR_RCAS_PASSTHROUGH_ALPHA 1
#endif

// Include the external FSR HLSL files
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_a.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl"

/// Bindings for FSR EASU constants provided by the CPU
///
/// Helper functions that set these constants can be found in FSRUtils
float4 _FsrEasuConstants0;
float4 _FsrEasuConstants1;
float4 _FsrEasuConstants2;
float4 _FsrEasuConstants3;

// Unity doesn't currently have a way to bind uint4 types so we reinterpret float4 as uint4 using macros below
#define FSR_EASU_CONSTANTS_0 asuint(_FsrEasuConstants0)
#define FSR_EASU_CONSTANTS_1 asuint(_FsrEasuConstants1)
#define FSR_EASU_CONSTANTS_2 asuint(_FsrEasuConstants2)
#define FSR_EASU_CONSTANTS_3 asuint(_FsrEasuConstants3)

/// EASU glue functions
///
/// These are used by the EASU implementation to access texture data
#if FSR_EASU_H
AH4 FsrEasuRH(AF2 p)
{
    return (AH4)GATHER_RED_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}
AH4 FsrEasuGH(AF2 p)
{
    return (AH4)GATHER_GREEN_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}
AH4 FsrEasuBH(AF2 p)
{
    return (AH4)GATHER_BLUE_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}

void FsrEasuProcessInput(inout AH4 r, inout AH4 g, inout AH4 b)
{
#ifdef FUTURE_HDR_OUTPUT
    AH3 s0 = InvertibleTonemap(AH3(r.x, g.x, b.x));
    AH3 s1 = InvertibleTonemap(AH3(r.y, g.y, b.y));
    AH3 s2 = InvertibleTonemap(AH3(r.z, g.z, b.z));
    AH3 s3 = InvertibleTonemap(AH3(r.w, g.w, b.w));

    r = AH4(s0.r, s1.r, s2.r, s3.r);
    g = AH4(s0.g, s1.g, s2.g, s3.g);
    b = AH4(s0.b, s1.b, s2.b, s3.b);
#endif
}
#else
AF4 FsrEasuRF(AF2 p)
{
    return GATHER_RED_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}
AF4 FsrEasuGF(AF2 p)
{
    return GATHER_GREEN_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}
AF4 FsrEasuBF(AF2 p)
{
    return GATHER_BLUE_TEXTURE2D_X(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, p);
}

void FsrEasuProcessInput(inout AF4 r, inout AF4 g, inout AF4 b)
{
#ifdef FUTURE_HDR_OUTPUT
    float3 s0 = InvertibleTonemap(float3(r.x, g.x, b.x));
    float3 s1 = InvertibleTonemap(float3(r.y, g.y, b.y));
    float3 s2 = InvertibleTonemap(float3(r.z, g.z, b.z));
    float3 s3 = InvertibleTonemap(float3(r.w, g.w, b.w));

    r = float4(s0.r, s1.r, s2.r, s3.r);
    g = float4(s0.g, s1.g, s2.g, s3.g);
    b = float4(s0.b, s1.b, s2.b, s3.b);
#endif
}
#endif

/// Applies FidelityFX Super Resolution Edge Adaptive Spatial Upsampling at the provided pixel position
///
/// The source texture must be provided before this file is included via the FSR_INPUT_TEXTURE preprocessor symbol
/// Ex: #define FSR_INPUT_TEXTURE _SourceTex
///
/// A valid sampler must also be provided via the FSR_INPUT_SAMPLER preprocessor symbol
/// Ex: #define FSR_INPUT_SAMPLER sampler_LinearClamp
///
/// The color data stored in the source texture should be in gamma 2.0 color space
real3 ApplyEASU(uint2 positionSS)
{
#if FSR_EASU_H
    // Execute 16-bit EASU
    AH3 color;
    FsrEasuH(
#else
    // Execute 32-bit EASU
    AF3 color;
    FsrEasuF(
#endif
        color, positionSS, FSR_EASU_CONSTANTS_0, FSR_EASU_CONSTANTS_1, FSR_EASU_CONSTANTS_2, FSR_EASU_CONSTANTS_3
    );
    return color;
}

/// Bindings for FSR RCAS constants provided by the CPU
///
/// Helper functions that set these constants can be found in FSRUtils
float4 _FsrRcasConstants;

// Unity doesn't currently have a way to bind uint4 types so we reinterpret float4 as uint4 using macros below
#define FSR_RCAS_CONSTANTS asuint(_FsrRcasConstants)

/// RCAS glue functions
///
/// These are used by the RCAS implementation to access texture data and perform color space conversion if necessary
#if FSR_RCAS_H
AH4 FsrRcasLoadH(ASW2 p)
{
    return (AH4)LOAD_TEXTURE2D_X(FSR_INPUT_TEXTURE, p);
}
void FsrRcasInputH(inout AH1 r, inout AH1 g, inout AH1 b)
{
    // No conversion to linear necessary since it's always performed during EASU output
}
#else
AF4 FsrRcasLoadF(ASU2 p)
{
    return LOAD_TEXTURE2D_X(FSR_INPUT_TEXTURE, p);
}
void FsrRcasInputF(inout AF1 r, inout AF1 g, inout AF1 b)
{
    // No conversion to linear necessary since it's always performed during EASU output
}
#endif

/// Applies FidelityFX Super Resolution Robust Contrast Adaptive Sharpening at the provided pixel position
///
/// The source texture must be provided before this file is included via the FSR_INPUT_TEXTURE preprocessor symbol
/// Ex: #define FSR_INPUT_TEXTURE _SourceTex
///
/// A valid sampler must also be provided via the FSR_INPUT_SAMPLER preprocessor symbol
/// Ex: #define FSR_INPUT_SAMPLER sampler_LinearClamp
///
/// RCAS supports an optional alpha passthrough that can be enabled via the FSR_ENABLE_ALPHA preprocessor symbol
/// When passthrough is enabled, this function will return the input texture's alpha channel unmodified
///
/// The color data stored in the source texture should be in linear color space
#if FSR_ENABLE_ALPHA
real4 ApplyRCAS(uint2 positionSS)
#else
real3 ApplyRCAS(uint2 positionSS)
#endif
{
#if FSR_RCAS_H
    // Execute 16-bit RCAS
#if FSR_ENABLE_ALPHA
    AH4 color;
#else
    AH3 color;
#endif
    FsrRcasH(
#else
    // Execute 32-bit RCAS
#if FSR_ENABLE_ALPHA
    AF4 color;
#else
    AF3 color;
#endif
    FsrRcasF(
#endif
        color.r,
        color.g,
        color.b,
#if FSR_ENABLE_ALPHA
        color.a,
#endif
        positionSS,
        FSR_RCAS_CONSTANTS
    );
    return color;
}
