///
/// FidelityFX Contrast Adaptive Sharpening (FFX CAS) Common Shader Code
///
/// This file provides shader helper functions which are intended to be used with the C# helper functions in
/// FSRUtils.cs. The main purpose of this file is to simplify the usage of the external FSR shader functions within
/// the Unity shader environment.
///
/// Usage:
/// - Call SetXConstants function on a command buffer in C#
/// - Launch shader (via draw or dispatch)
/// - Call associated ApplyCAS function provided by this file
///
/// The following preprocessor parameters MUST be defined before including this file:
/// - FFX_CAS_INPUT_TEXTURE
///     - The texture to use as input for the FSR passes
///

// Indicate that we'll be using the HLSL implementation of FSR
#define A_GPU 1
#define A_HLSL 1

// Include the external FSR HLSL files
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_a.hlsl"

AF3 CasLoad(ASU2 p)
{
    return LOAD_TEXTURE2D_X(FFX_CAS_INPUT_TEXTURE, p);
}

void CasInput(inout AF1 r, inout AF1 g, inout AF1 b)
{
    // No need to transform to linear
}

#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/ffx/ffx_cas.hlsl"

float4 _CasConstants0;
float4 _CasConstants1;
float4 _CasOptions;

half3 ApplyCAS(AF2 uv)
{
    bool sharpenOnly = _CasOptions.x > 0.0001;
    bool lowQuality = _CasOptions.y > 0.0001;
    AU2 pixelCoord = _CasOptions.zw * uv;

    // Filter.
    AF3 c;
    CasFilter(c.r, c.g, c.b, pixelCoord, asuint(_CasConstants0), asuint(_CasConstants1), sharpenOnly, lowQuality);
    return c;
}
