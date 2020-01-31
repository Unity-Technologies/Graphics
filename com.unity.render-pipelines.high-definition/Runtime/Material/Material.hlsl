#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

// Guidelines for Material Keyword.
// There is a set of Material Keyword that a HD shaders must define (or not define). We call them system KeyWord.
// .Shader need to define:
// - _SURFACE_TYPE_TRANSPARENT if they use a transparent material
// - _BLENDMODE_ALPHA, _BLENDMODE_ADD, _BLENDMODE_PRE_MULTIPLY for blend mode
// - _BLENDMODE_PRESERVE_SPECULAR_LIGHTING for correct lighting when blend mode are use with a Lit material
// - _ENABLE_FOG_ON_TRANSPARENT if fog is enable on transparent surface
// - _DISABLE_DECALS if the material don't support decals

#define HAVE_DECALS ( (defined(DECALS_3RT) || defined(DECALS_4RT)) && !defined(_DISABLE_DECALS) )

//-----------------------------------------------------------------------------
// ApplyBlendMode function
//-----------------------------------------------------------------------------

float4 ApplyBlendMode(float3 diffuseLighting, float3 specularLighting, float opacity)
{

#ifndef _SURFACE_TYPE_TRANSPARENT
    // When doing off screen rendering (low-res transparency or after post process pass) and rendering opaque objects, opacity needs to be set to zero for proper compositing.
    if (_OffScreenRendering != 0)
        opacity = 0;
#endif

    // ref: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
    // Lit transparent object should have reflection and transmission.
    // Transmission when not using "rough refraction mode" (with fetch in preblured background) is handled with blend mode.
    // However reflection should not be affected by blend mode. For example a glass should still display reflection and not lose the highlight when blend
    // This is the purpose of following function, "Cancel" the blend mode effect on the specular lighting but not on the diffuse lighting
#ifdef _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    // In the case of _BLENDMODE_ALPHA the code should be float4(diffuseLighting + (specularLighting / max(opacity, 0.01)), opacity)
    // However this have precision issue when reaching 0, so we change the blend mode and apply src * src_a inside the shader instead
    #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
    return float4(diffuseLighting * opacity + specularLighting, opacity);
    #else // defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting + specularLighting, opacity);
    #endif
#else
    #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
    return float4((diffuseLighting + specularLighting) * opacity, opacity);
    #else // defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting + specularLighting, opacity);
    #endif
#endif
}

float4 ApplyBlendMode(float3 color, float opacity)
{
    return ApplyBlendMode(color, float3(0.0, 0.0, 0.0), opacity);
}

//-----------------------------------------------------------------------------
// Fog sampling function for materials
//-----------------------------------------------------------------------------

// Used for transparent object. input color is color + alpha of the original transparent pixel.
// This must be call after ApplyBlendMode to work correctly
float4 EvaluateAtmosphericScattering(PositionInputs posInput, float3 V, float4 inputColor)
{
    float4 result = inputColor;

#ifdef _ENABLE_FOG_ON_TRANSPARENT
    float3 volColor, volOpacity;
    EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity); // Premultiplied alpha

    #if defined(_BLENDMODE_ALPHA)
        // Regular alpha blend need to multiply fog color by opacity (as we do src * src_a inside the shader)
        // result.rgb = lerp(result.rgb, unpremul_volColor * result.a, volOpacity);
        // result.rgb = result.rgb + volOpacity * (unpremul_volColor * result.a - result.rgb);
        // result.rgb = result.rgb + volColor * result.a - result.rgb * volOpacity;
        result.rgb = result.rgb * (1 - volOpacity) + volColor * result.a;
    #elif defined(_BLENDMODE_ADD)
        // For additive, we just need to fade to black with fog density (black + background == background color == fog color)
        result.rgb = result.rgb * (1.0 - volOpacity);
    #elif defined(_BLENDMODE_PRE_MULTIPLY)
        // For Pre-Multiplied Alpha Blend, we need to multiply fog color by src alpha to match regular alpha blending formula.
        // result.rgb = lerp(result.rgb, unpremul_volColor * result.a, volOpacity);
        result.rgb = result.rgb * (1 - volOpacity) + volColor * result.a;
        // Note: this formula for color is correct, assuming we apply the Over operator afterwards
        // (see the appendix in the Deep Compositing paper). But do we?
        // Additionally, we do not modify the alpha here, which is most certainly WRONG.
    #endif
#else
    // Evaluation of fog for opaque objects is currently done in a full screen pass independent from any material parameters.
    // but this funtction is called in generic forward shader code so we need it to be neutral in this case.
#endif

    return result;
}

//-----------------------------------------------------------------------------
// Alpha test replacement
//-----------------------------------------------------------------------------

// This function must be use instead of clip instruction. It allow to manage in which case the clip is perform for optimization purpose
void DoAlphaTest(float alpha, float alphaCutoff)
{
    // For Deferred:
    // If we have a prepass, we  may want to remove the clip from the GBuffer pass (otherwise HiZ does not work on PS4) - SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
    // For Forward Opaque:
    // If we have a prepass, we may want to remove the clip from the forward pass (otherwise HiZ does not work on PS4) - SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
    // For Forward Transparent
    // Also no alpha test for light transport
    // Note: If SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST or SHADERPASS_FORWARD_BYPASS_ALPHA_TEST are used, it mean that we must use ZTest depth equal for the pass (Need to use _ZTestDepthEqualForOpaque property).
#if !defined(SHADERPASS_FORWARD_BYPASS_ALPHA_TEST) && !defined(SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST) && !(SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
    clip(alpha - alphaCutoff);
#endif
}

// This function is the alternative version used for ray tracing
void DoAlphaTest(float alpha, float alphaCutoff, out bool alphaTestResult)
{
    alphaTestResult = alpha > alphaCutoff;
}
//-----------------------------------------------------------------------------
// LoD Fade
//-----------------------------------------------------------------------------

// Helper for LODDitheringTransition.
uint2 ComputeFadeMaskSeed(float3 V, uint2 positionSS)
{
    uint2 fadeMaskSeed;

    if (IsPerspectiveProjection())
    {
        // Start with the world-space direction V. It is independent from the orientation of the camera,
        // and only depends on the position of the camera and the position of the fragment.
        // Now, project and transform it into [-1, 1].
        float2 pv = PackNormalOctQuadEncode(V);
        // Rescale it to account for the resolution of the screen.
        pv *= _ScreenSize.xy;
        // The camera only sees a small portion of the sphere, limited by hFoV and vFoV.
        // Therefore, we must rescale again (before quantization), roughly, by 1/tan(FoV/2).
        pv *= UNITY_MATRIX_P._m00_m11;
        // Truncate and quantize.
        fadeMaskSeed = asuint((int2)pv);
    }
    else
    {
        // Can't use the view direction, it is the same across the entire screen.
        fadeMaskSeed = positionSS;
    }

    return fadeMaskSeed;
}

//-----------------------------------------------------------------------------
// Reflection / Refraction hierarchy handling
//-----------------------------------------------------------------------------

// This function is use with reflection and refraction hierarchy of LightLoop
// It will add weight to hierarchyWeight but ensure that hierarchyWeight is not more than one
// by updating the weight value. Returned weight value must be apply on current lighting
// Example: Total hierarchyWeight is 0.8 and weight is 0.4. Function return hierarchyWeight of 1.0 and weight of 0.2
// hierarchyWeight and weight must be positive and between 0 and 1
void UpdateLightingHierarchyWeights(inout float hierarchyWeight, inout float weight)
{
    float accumulatedWeight = hierarchyWeight + weight;
    hierarchyWeight = saturate(accumulatedWeight);
    weight -= saturate(accumulatedWeight - hierarchyWeight);
}

//-----------------------------------------------------------------------------
// BuiltinData
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

#endif // UNITY_MATERIAL_INCLUDED
