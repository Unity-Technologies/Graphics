#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "CoreRP/ShaderLibrary/Color.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/BSDF.hlsl"
#include "CoreRP/ShaderLibrary/Debug.hlsl"
#include "CoreRP/ShaderLibrary/GeometricTools.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"
#include "CoreRP/ShaderLibrary/EntityLighting.hlsl"
#include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
#include "../Sky/AtmosphericScattering/AtmosphericScattering.hlsl"

// Guidelines for Material Keyword.
// There is a set of Material Keyword that a HD shaders must define (or not define). We call them system KeyWord.
// .Shader need to define:
// - _SURFACE_TYPE_TRANSPARENT if they use a transparent material
// - _BLENDMODE_ALPHA, _BLENDMODE_ADD, _BLENDMODE_PRE_MULTIPLY for blend mode
// - _BLENDMODE_PRESERVE_SPECULAR_LIGHTING for correct lighting when blend mode are use with a Lit material
// - _ENABLE_FOG_ON_TRANSPARENT if fog is enable on transparent surface

//-----------------------------------------------------------------------------
// ApplyBlendMode function
//-----------------------------------------------------------------------------

float4 ApplyBlendMode(float3 diffuseLighting, float3 specularLighting, float opacity)
{
    // ref: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
    // Lit transparent object should have reflection and tramission.
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
float4 EvaluateAtmosphericScattering(PositionInputs posInput, float4 inputColor)
{
    float4 result = inputColor;

#ifdef _ENABLE_FOG_ON_TRANSPARENT
    float4 fog = EvaluateAtmosphericScattering(posInput);

    #if defined(_BLENDMODE_ALPHA)
    // Regular alpha blend need to multiply fog color by opacity (as we do src * src_a inside the shader)
    result.rgb = lerp(result.rgb, fog.rgb * result.a, fog.a);
    #elif defined(_BLENDMODE_ADD)
    // For additive, we just need to fade to black with fog density (black + background == background color == fog color)
    result.rgb = result.rgb * (1.0 - fog.a);
    #elif defined(_BLENDMODE_PRE_MULTIPLY)
    // For Pre-Multiplied Alpha Blend, we need to multiply fog color by src alpha to match regular alpha blending formula.
    result.rgb = lerp(result.rgb, fog.rgb * result.a, fog.a);
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

// This function must be use instead of clip instruction. It allow to manage in which case the clip is perform
void DoAlphaTest(float alpha, float alphaCutoff)
{
    // For Deferred:
    // If we have a prepass, we need to remove the clip from the GBuffer pass (otherwise HiZ does not work on PS4) - SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
    // For Forward (Lit or Unlit)
    // Opaque geometry always has a depth pre-pass so we never want to do the clip here. For transparent we perform the clip as usual.
    // Also no alpha test for light transport
#if !(SHADERPASS == SHADERPASS_FORWARD && !defined(_SURFACE_TYPE_TRANSPARENT)) && !(SHADERPASS == SHADERPASS_FORWARD_UNLIT && !defined(_SURFACE_TYPE_TRANSPARENT)) && !defined(SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST) && !(SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
    clip(alpha - alphaCutoff);
#endif
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

#include "Builtin/BuiltinData.hlsl"

//-----------------------------------------------------------------------------
// Material definition
//-----------------------------------------------------------------------------

// Here we include all the different lighting model supported by the renderloop based on define done in .shader
// Only one deferred layout is allowed for a HDRenderPipeline, this will be detect by the redefinition of GBUFFERMATERIAL_COUNT
// If GBUFFERMATERIAL_COUNT is define two time, the shaders will not compile
#ifdef UNITY_MATERIAL_LIT
#include "Lit/Lit.hlsl"
#elif defined(UNITY_MATERIAL_UNLIT)
#include "Unlit/Unlit.hlsl"
#elif defined(UNITY_MATERIAL_IRIDESCENCE)
//#include "Iridescence/Iridescence.hlsl"
#endif

//-----------------------------------------------------------------------------
// Define for GBuffer management
//-----------------------------------------------------------------------------

#ifdef GBUFFERMATERIAL_COUNT

#if GBUFFERMATERIAL_COUNT == 2

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))

#ifdef SHADOWS_SHADOWMASK
    #define OUTPUT_GBUFFER_SHADOWMASK(NAME) ,out float4 NAME : SV_Target2
#endif

#elif GBUFFERMATERIAL_COUNT == 3

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))

#ifdef SHADOWS_SHADOWMASK
    #define OUTPUT_GBUFFER_SHADOWMASK(NAME) ,out float4 NAME : SV_Target3
#endif

#elif GBUFFERMATERIAL_COUNT == 4

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#ifdef SHADOWS_SHADOWMASK
    #define OUTPUT_GBUFFER_SHADOWMASK(NAME) ,out float4 NAME : SV_Target4
#endif

#elif GBUFFERMATERIAL_COUNT == 5

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))

#ifdef SHADOWS_SHADOWMASK
    #define OUTPUT_GBUFFER_SHADOWMASK(NAME) ,out float4 NAME : SV_Target5
#endif

#elif GBUFFERMATERIAL_COUNT == 6

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5))

#ifdef SHADOWS_SHADOWMASK
    #define OUTPUT_GBUFFER_SHADOWMASK(NAME) ,out float4 NAME : SV_Target6
#endif

#endif

#define DECODE_FROM_GBUFFER(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING) DecodeFromGBuffer(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(UNPOSITIONSS) MaterialFeatureFlagsFromGBuffer(UNPOSITIONSS)

#ifdef SHADOWS_SHADOWMASK
#define ENCODE_SHADOWMASK_INTO_GBUFFER(SHADOWMASK, NAME) EncodeShadowMask(SHADOWMASK, NAME)
#else
#define OUTPUT_GBUFFER_SHADOWMASK(NAME)
#define ENCODE_SHADOWMASK_INTO_GBUFFER(SHADOWMASK, NAME)
#endif

#endif // #ifdef GBUFFERMATERIAL_COUNT


#endif // UNITY_MATERIAL_INCLUDED
