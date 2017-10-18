#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "../../Core/ShaderLibrary/Color.hlsl"
#include "../../Core/ShaderLibrary/Packing.hlsl"
#include "../../Core/ShaderLibrary/BSDF.hlsl"
#include "../../Core/ShaderLibrary/Debug.hlsl"
#include "../../Core/ShaderLibrary/GeometricTools.hlsl"
#include "../../Core/ShaderLibrary/CommonMaterial.hlsl"
#include "../../Core/ShaderLibrary/EntityLighting.hlsl"
#include "../../Core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "../Sky/AtmosphericScattering/AtmosphericScattering.hlsl"

// Guidelines for Material Keyword.
// There is a set of Material Keyword that a HD shaders must define (or not define). We call them system KeyWord.
// .Shader need to define:
// - _SURFACE_TYPE_TRANSPARENT if they use a transparent material
// - _BLENDMODE_ALPHA, _BLENDMODE_ADD, _BLENDMODE_MULTIPLY, _BLENDMODE_PRE_MULTIPLY for blend mode
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
    #else // defined(_BLENDMODE_MULTIPLY) || defined(_BLENDMODE_PRE_MULTIPLY)
    return float4(diffuseLighting + specularLighting, opacity);
    #endif
#else
    #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
    return float4((diffuseLighting + specularLighting) * opacity, opacity);
    #else // defined(_BLENDMODE_MULTIPLY) || defined(_BLENDMODE_PRE_MULTIPLY)
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
    #elif defined(_BLENDMODE_MULTIPLY)
    // For multiplicative, we just need to fade to white with fog density (white * background == background color == fog color)
    result.rgb = lerp(result.rgb, float3(1.0, 1.0, 1.0), fog.a);
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

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));

#define FETCH_GBUFFER(NAME, TEX, unCoord2)                                        \
        GBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2); \
        GBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2);

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))
#define DECODE_FROM_GBUFFER(NAME, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING) DecodeFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(NAME) MaterialFeatureFlagsFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))

#if SHADEROPTIONS_VELOCITY_IN_GBUFFER
#define OUTPUT_GBUFFER_VELOCITY(NAME) ,out float4 NAME : SV_Target2
#endif

#elif GBUFFERMATERIAL_COUNT == 3

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));

#define FETCH_GBUFFER(NAME, TEX, unCoord2)                                        \
        GBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2); \
        GBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2); \
        GBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), unCoord2);

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))
#define DECODE_FROM_GBUFFER(NAME, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING) DecodeFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2), FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(NAME) MaterialFeatureFlagsFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))


#if SHADEROPTIONS_VELOCITY_IN_GBUFFER
#define OUTPUT_GBUFFER_VELOCITY(NAME) ,out float4 NAME : SV_Target3
#endif

#elif GBUFFERMATERIAL_COUNT == 4

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));  \
        TEXTURE2D(MERGE_NAME(NAME, 3));

#define FETCH_GBUFFER(NAME, TEX, unCoord2)                                        \
        GBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2); \
        GBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2); \
        GBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), unCoord2); \
        GBufferType3 MERGE_NAME(NAME, 3) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 3), unCoord2);

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))
#define DECODE_FROM_GBUFFER(NAME, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING) DecodeFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(NAME) MaterialFeatureFlagsFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#if SHADEROPTIONS_VELOCITY_IN_GBUFFER
#define OUTPUT_GBUFFER_VELOCITY(NAME) ,out float4 NAME : SV_Target4
#endif

#elif GBUFFERMATERIAL_COUNT == 5

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));  \
        TEXTURE2D(MERGE_NAME(NAME, 3));  \
        TEXTURE2D(MERGE_NAME(NAME, 4));

#define FETCH_GBUFFER(NAME, TEX, unCoord2)                                        \
        GBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2); \
        GBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2); \
        GBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), unCoord2); \
        GBufferType3 MERGE_NAME(NAME, 3) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 3), unCoord2); \
        GBufferType4 MERGE_NAME(NAME, 4) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 4), unCoord2);

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, NAME) EncodeIntoGBuffer(SURFACE_DATA, BAKE_DIFFUSE_LIGHTING, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))
#define DECODE_FROM_GBUFFER(NAME, FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING) DecodeFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), FEATURE_FLAGS, BSDF_DATA, BAKE_DIFFUSE_LIGHTING)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(NAME) MaterialFeatureFlagsFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))

#if SHADEROPTIONS_VELOCITY_IN_GBUFFER
#define OUTPUT_GBUFFER_VELOCITY(NAME) ,out float4 NAME : SV_Target5
#endif

#endif

#if SHADEROPTIONS_VELOCITY_IN_GBUFFER
#define ENCODE_VELOCITY_INTO_GBUFFER(VELOCITY, NAME) EncodeVelocity(VELOCITY, NAME)
#else
#define OUTPUT_GBUFFER_VELOCITY(NAME)
#define ENCODE_VELOCITY_INTO_GBUFFER(VELOCITY, NAME)
#endif

#endif // #ifdef GBUFFERMATERIAL_COUNT

#endif // UNITY_MATERIAL_INCLUDED
