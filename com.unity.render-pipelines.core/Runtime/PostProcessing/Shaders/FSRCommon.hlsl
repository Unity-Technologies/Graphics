// Ensure that the shader including this file is targeting an adequate shader level
#if SHADER_TARGET < 45
#error FidelityFX Super Resolution requires a shader target of 4.5 or greater
#endif

// Indicate that we'll be using the HLSL implementation of FSR
#define A_GPU 1
#define A_HLSL 1

// Enable either the 16-bit or the 32-bit implementation of FSR depending on platform support
#if HAS_FLOAT
    #define A_HALF
    #define FSR_EASU_H 1
    #define FSR_RCAS_H 1
#else
    #define FSR_EASU_F 1
    #define FSR_RCAS_F 1
#endif

// Include the external FSR HLSL files
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_a.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl"

/// Bindings for FSR constants provided by the CPU
///
/// Helper functions that set these constants can be found in FSRUtils
float4 _FsrConstants0;
float4 _FsrConstants1;
float4 _FsrConstants2;
float4 _FsrConstants3;

// Unity doesn't currently have a way to bind uint4 types so we reinterpret float4 as uint4 using macros below
#define FSR_CONSTANTS_0 asuint(_FsrConstants0)
#define FSR_CONSTANTS_1 asuint(_FsrConstants1)
#define FSR_CONSTANTS_2 asuint(_FsrConstants2)
#define FSR_CONSTANTS_3 asuint(_FsrConstants3)

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
#endif

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

/// Applies FidelityFX Super Resolution Edge Adaptive Spatial Upsampling at the provided pixel position
///
/// The source texture must be provided before this file is included via the FSR_INPUT_TEXTURE preprocessor symbol
/// Ex: #define FSR_INPUT_TEXTURE _SourceTex
///
/// A valid sampler must also be provided via the FSR_INPUT_SAMPLER preprocessor symbol
/// Ex: #define FSR_INPUT_SAMPLER sampler_LinearClamp
///
/// The color data stored in the source texture should be in gamma 2.0 color space
half3 ApplyEASU(uint2 positionSS)
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
        color, positionSS, FSR_CONSTANTS_0, FSR_CONSTANTS_1, FSR_CONSTANTS_2, FSR_CONSTANTS_3
    );

    return color;
}

/// Applies FidelityFX Super Resolution Robust Contrast Adaptive Sharpening at the provided pixel position
///
/// The source texture must be provided before this file is included via the FSR_INPUT_TEXTURE preprocessor symbol
/// Ex: #define FSR_INPUT_TEXTURE _SourceTex
///
/// A valid sampler must also be provided via the FSR_INPUT_SAMPLER preprocessor symbol
/// Ex: #define FSR_INPUT_SAMPLER sampler_LinearClamp
///
/// The color data stored in the source texture should be in linear color space
half3 ApplyRCAS(uint2 positionSS)
{
    #if FSR_RCAS_H
    // Execute 16-bit RCAS
    AH3 color;
    FsrRcasH(
    #else
    // Execute 32-bit RCAS
    AF3 color;
    FsrRcasF(
    #endif
        color.r, color.g, color.b, positionSS, FSR_CONSTANTS_0
    );

    return color;
}
