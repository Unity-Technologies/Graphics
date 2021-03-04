#ifndef UNITY_EDITOR_UIE_INCLUDED
#define UNITY_EDITOR_UIE_INCLUDED

// We need to use a uniform for the project color space because editor shaders are always compiled with
// UNITY_COLORSPACE_GAMMA defined. Editor shaders are not recompiled when the project color space is modified.
fixed _EditorColorSpace; // 1 for Linear, 0 for Gamma
#define UIE_CUSTOM_SHADER
// Editor ALWAYS renders in nonlinear sRGB color space
#define UIE_COLORSPACE_GAMMA 1
// This is to prevent banding in linear
#define UIE_FRAG_T half4
#include "UnityUIE.cginc"

// The editor shader must ALWAYS return sRGB-encoded colors.
fixed4 uie_editor_frag(v2f IN)
{
    // Postpone the application of the tint after the following linear-to-gamma conversion. We assume that the tint is
    // already in the sRGB color space. The editor shader must force UIE_COLORSPACE_GAMMA to 1 to disable
    // gamma-to-linear conversion of the tint performed by the vertex shader.
    fixed4 tint = IN.color;

    // Override the color with opaque white so that it doesn't tint the output of uie_std_frag.
    IN.color = (fixed4)1;
    half4 stdColor = uie_std_frag(IN);

    // Convert the color and use the converted result if we need to.
    fixed4 gammaColor = fixed4(LinearToGammaSpace(stdColor.rgb), stdColor.a);
    // In linear, Atlas and Custom textures fetches will return linear colors that must be converted to sRGB.
    fixed isTextured = IN.typeTexSettings.x == 3 ? 1 : 0;
    fixed convertToGamma = _EditorColorSpace * isTextured;
    fixed4 result = lerp(stdColor, gammaColor, convertToGamma);

    // Apply the tint.
    return result * tint;
}

#endif // UNITY_EDITOR_UIE_INCLUDED
