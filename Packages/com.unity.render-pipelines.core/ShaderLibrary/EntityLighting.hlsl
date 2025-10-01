#ifndef UNITY_ENTITY_LIGHTING_INCLUDED
#define UNITY_ENTITY_LIGHTING_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_SWITCH  || SHADER_API_SWITCH2|| defined(UNITY_UNIFIED_SHADER_PRECISION_MODEL)
#pragma warning (disable : 3205) // conversion of larger type to smaller
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"

#define LIGHTMAP_RGBM_MAX_GAMMA     real(5.0)       // NB: Must match value in RGBMRanges.h
#define LIGHTMAP_RGBM_MAX_LINEAR    real(34.493242) // LIGHTMAP_RGBM_MAX_GAMMA ^ 2.2

#ifdef UNITY_LIGHTMAP_RGBM_ENCODING
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_GAMMA
        #define LIGHTMAP_HDR_EXPONENT   real(1.0)   // Not used in gamma color space
    #else
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_LINEAR
        #define LIGHTMAP_HDR_EXPONENT   real(2.2)
    #endif
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER real(2.0)
    #else
        #define LIGHTMAP_HDR_MULTIPLIER real(4.59) // 2.0 ^ 2.2
    #endif
    #define LIGHTMAP_HDR_EXPONENT real(0.0)
#else // (UNITY_LIGHTMAP_FULL_HDR)
    #define LIGHTMAP_HDR_MULTIPLIER real(1.0)
    #define LIGHTMAP_HDR_EXPONENT real(1.0)
#endif

// This sample a 3D volume storing SH
// Volume is store as 3D texture with 4 R, G, B, Occ set of 4 coefficient store atlas in same 3D texture. Occ is use for occlusion.
// TODO: the packing here is inefficient as we will fetch values far away from each other and they may not fit into the cache - Suggest we pack RGB continuously
// TODO: The calcul of texcoord could be perform with a single matrix multicplication calcualted on C++ side that will fold probeVolumeMin and probeVolumeSizeInv into it and handle the identity case, no reasons to do it in C++ (ask Ionut about it)
// It should also handle the camera relative path (if the render pipeline use it)
// bakeDiffuseLighting and backBakeDiffuseLighting must be initialize outside the function
void SampleProbeVolumeSH4(TEXTURE3D_PARAM(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float3 normalWS, float3 backNormalWS, float4x4 WorldToTexture,
                            float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv,
                            inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
    float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
    float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;
    // Each component is store in the same texture 3D. Each use one quater on the x axis
    // Here we get R component then increase by step size (0.25) to get other component. This assume 4 component
    // but last one is not used.
    // Clamp to edge of the "internal" texture, as R is from half texel to size of R texture minus half texel.
    // This avoid leaking
    texCoord.x = clamp(texCoord.x * 0.25, 0.5 * texelSizeX, 0.25 - 0.5 * texelSizeX);

    float4 shAr = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
    texCoord.x += 0.25;
    float4 shAg = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
    texCoord.x += 0.25;
    float4 shAb = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);

    bakeDiffuseLighting += SHEvalLinearL0L1(normalWS, shAr, shAg, shAb);
    backBakeDiffuseLighting += SHEvalLinearL0L1(backNormalWS, shAr, shAg, shAb);
}

// Just a shortcut that call function above
float3 SampleProbeVolumeSH4(TEXTURE3D_PARAM(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float3 normalWS, float4x4 WorldToTexture,
                                float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv)
{
    float3 backNormalWSUnused = 0.0;
    float3 bakeDiffuseLighting = 0.0;
    float3 backBakeDiffuseLightingUnused = 0.0;
    SampleProbeVolumeSH4(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), positionWS, normalWS, backNormalWSUnused, WorldToTexture,
                            transformToLocal, texelSizeX, probeVolumeMin, probeVolumeSizeInv,
                            bakeDiffuseLighting, backBakeDiffuseLightingUnused);
    return bakeDiffuseLighting;
}

// The SphericalHarmonicsL2 coefficients are packed into 7 coefficients per color channel instead of 9.
// The packing from 9 to 7 is done from engine code and will use the alpha component of the pixel to store an additional SH coefficient.
// The 3D atlas texture will contain 7 SH coefficient parts.
// bakeDiffuseLighting and backBakeDiffuseLighting must be initialize outside the function
void SampleProbeVolumeSH9(TEXTURE3D_PARAM(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float3 normalWS, float3 backNormalWS, float4x4 WorldToTexture,
                                           float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv,
                                           inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
    float3 position = (transformToLocal == 1.0f) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
    float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv;

    const uint shCoeffCount = 7;
    const float invShCoeffCount = 1.0f / float(shCoeffCount);

    // We need to compute proper X coordinate to sample into the atlas.
    texCoord.x = texCoord.x / shCoeffCount;

    // Clamp the x coordinate otherwise we'll have leaking between RGB coefficients.
    float texCoordX = clamp(texCoord.x, 0.5f * texelSizeX, invShCoeffCount - 0.5f * texelSizeX);

    float4 SHCoefficients[7];

    for (uint i = 0; i < shCoeffCount; i++)
    {
        texCoord.x = texCoordX + i * invShCoeffCount;
        SHCoefficients[i] = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
    }

    bakeDiffuseLighting += SampleSH9(SHCoefficients, normalize(normalWS));
    backBakeDiffuseLighting += SampleSH9(SHCoefficients, normalize(backNormalWS));
}

// Just a shortcut that call function above
float3 SampleProbeVolumeSH9(TEXTURE3D_PARAM(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float3 normalWS, float4x4 WorldToTexture,
                                float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv)
{
    float3 backNormalWSUnused = 0.0;
    float3 bakeDiffuseLighting = 0.0;
    float3 backBakeDiffuseLightingUnused = 0.0;
    SampleProbeVolumeSH9(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), positionWS, normalWS, backNormalWSUnused, WorldToTexture,
                            transformToLocal, texelSizeX, probeVolumeMin, probeVolumeSizeInv,
                            bakeDiffuseLighting, backBakeDiffuseLightingUnused);
    return bakeDiffuseLighting;
}

float4 SampleProbeOcclusion(TEXTURE3D_PARAM(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float4x4 WorldToTexture,
                            float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv)
{
    float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
    float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;

    // Sample fourth texture in the atlas
    // We need to compute proper U coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between ShB coefficients and Probe Occlusion(Occ) info
    texCoord.x = max(texCoord.x * 0.25 + 0.75, 0.75 + 0.5 * texelSizeX);

    return SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
}

// Following functions are to sample enlighten lightmaps (or lightmaps encoded the same way as our
// enlighten implementation). They assume use of RGB9E5 for dynamic illuminance map and RGBM for baked ones.
// It is required for other platform that aren't supporting this format to implement variant of these functions
// (But these kind of platform should use regular render loop and not news shaders).

// TODO: This is the max value allowed for emissive (bad name - but keep for now to retrieve it) (It is 8^2.2 (gamma) and 8 is the limit of punctual light slider...), comme from UnityCg.cginc. Fix it!
// Ask Jesper if this can be change for HDRenderPipeline
#define EMISSIVE_RGBM_SCALE 97.0

// RGBM stuff is temporary. For now baked lightmap are in RGBM and the RGBM range for lightmaps is specific so we can't use the generic method.
// In the end baked lightmaps are going to be BC6H so the code will be the same as dynamic lightmaps.
// Same goes for emissive packed as an input for Enlighten with another hard coded multiplier.

// TODO: This function is used with the LightTransport pass to encode lightmap or emissive
real4 PackEmissiveRGBM(real3 rgb)
{
    real kOneOverRGBMMaxRange = 1.0 / EMISSIVE_RGBM_SCALE;
    const real kMinMultiplier = 2.0 * 1e-2;

    real4 rgbm = real4(rgb * kOneOverRGBMMaxRange, 1.0);
        rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    rgbm.a = max(rgbm.a, kMinMultiplier);

    rgbm.rgb /= rgbm.a;
    return rgbm;
}

real3 UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return rgbmInput.rgb * (rgbmInput.a * decodeInstructions.x);
#else
    return rgbmInput.rgb * (PositivePow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x);
#endif
}

real3 UnpackLightmapDoubleLDR(real4 encodedColor, real4 decodeInstructions)
{
    return encodedColor.rgb * decodeInstructions.x;
}

#ifndef BUILTIN_TARGET_API
real3 DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)
{
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    return UnpackLightmapRGBM(encodedIlluminance, decodeInstructions);
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    return UnpackLightmapDoubleLDR(encodedIlluminance, decodeInstructions);
#else // (UNITY_LIGHTMAP_FULL_HDR)
    return encodedIlluminance.rgb;
#endif
}
#endif

real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

#if defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(USE_LEGACY_LIGHTMAPS)
// ^ GPU-driven rendering is enabled, and we haven't opted-out from lightmap
// texture arrays. This minimizes batch breakages, but texture arrays aren't
// supported in a performant way on all GPUs.
#define TEXTURE2D_LIGHTMAP_PARAM TEXTURE2D_ARRAY_PARAM
#define TEXTURE2D_LIGHTMAP_ARGS TEXTURE2D_ARRAY_ARGS
#define SAMPLE_TEXTURE2D_LIGHTMAP SAMPLE_TEXTURE2D_ARRAY
#define LIGHTMAP_EXTRA_ARGS float2 uv, float slice
#define LIGHTMAP_EXTRA_ARGS_USE uv, slice
#else
// ^ Lightmaps are not bound as texture arrays, but as individual textures. The
// batch is broken every time lightmaps are changed, but this is well-supported
// on all GPUs.
#define TEXTURE2D_LIGHTMAP_PARAM TEXTURE2D_PARAM
#define TEXTURE2D_LIGHTMAP_ARGS TEXTURE2D_ARGS
#define SAMPLE_TEXTURE2D_LIGHTMAP SAMPLE_TEXTURE2D
#define LIGHTMAP_EXTRA_ARGS float2 uv
#define LIGHTMAP_EXTRA_ARGS_USE uv
#endif

// For the built-in target, lightmaps are defined with half precision.
// Unfortunately, TEXTURE2D_Half_PARAM is not defined.
#ifdef BUILTIN_TARGET_API
#undef TEXTURE2D_LIGHTMAP_PARAM
#undef TEXTURE2D_LIGHTMAP_ARGS
#undef SAMPLE_TEXTURE2D_LIGHTMAP

#ifdef SHADER_API_GLES
#define TEXTURE2D_LIGHTMAP_PARAM(textureName, samplerName) TEXTURE2D_HALF(textureName)
#else
#define TEXTURE2D_LIGHTMAP_PARAM(textureName, samplerName) TEXTURE2D_HALF(textureName), SAMPLER(samplerName)
#endif

#define TEXTURE2D_LIGHTMAP_ARGS TEXTURE2D_ARGS
#define SAMPLE_TEXTURE2D_LIGHTMAP SAMPLE_TEXTURE2D
#endif

// isStaticLightmap mean it is not an Enlighten map
real3 SampleSingleLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), LIGHTMAP_EXTRA_ARGS, float4 transform, bool isStaticLightmap)
{
    real4 decodeInstructions = real4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0);

    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;
    real4 encodedIlluminance = SAMPLE_TEXTURE2D_LIGHTMAP(lightmapTex, lightmapSampler, LIGHTMAP_EXTRA_ARGS_USE).rgba;
    // Remark: static lightmap is RGBM for now, dynamic lightmap is RGB9E5
    real3 illuminance = isStaticLightmap ? DecodeLightmap(encodedIlluminance, decodeInstructions) : encodedIlluminance.rgb;

    return illuminance;
}

// deprecated
real3 SampleSingleLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), LIGHTMAP_EXTRA_ARGS, float4 transform, bool isStaticLightmap, real4 ignore)
{
    return SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(lightmapTex, lightmapSampler), LIGHTMAP_EXTRA_ARGS_USE, transform, isStaticLightmap);
}

void SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_PARAM(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS, float4 transform,
    float3 normalWS, float3 backNormalWS, bool isStaticLightmap, inout real3 bakeDiffuseLighting, inout real3 backBakeDiffuseLighting)
{
     // In directional mode Enlighten bakes dominant light direction
    // in a way, that using it for half Lambert and then dividing by a "rebalancing coefficient"
    // gives a result close to plain diffuse response lightmaps, but normalmapped.

    // Note that dir is not unit length on purpose. Its length is "directionality", like
    // for the directional specular lightmaps.

    real3 illuminance = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(lightmapTex, lightmapSampler), LIGHTMAP_EXTRA_ARGS_USE, transform, isStaticLightmap);

    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;

    real4 direction = SAMPLE_TEXTURE2D_LIGHTMAP(lightmapDirTex, lightmapDirSampler, LIGHTMAP_EXTRA_ARGS_USE);

    real halfLambert = dot(normalWS, direction.xyz - 0.5) + 0.5;
    bakeDiffuseLighting += illuminance * halfLambert / max(1e-4, direction.w);

    real backHalfLambert = dot(backNormalWS, direction.xyz - 0.5) + 0.5;
    backBakeDiffuseLighting += illuminance * backHalfLambert / max(1e-4, direction.w);
}

// deprecated
void SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_PARAM(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS, float4 transform,
    float3 normalWS, float3 backNormalWS, bool isStaticLightmap, real4 ignore, inout real3 bakeDiffuseLighting, inout real3 backBakeDiffuseLighting)
{
    SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_ARGS(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS_USE,
        transform, normalWS, backNormalWS, isStaticLightmap, bakeDiffuseLighting, backBakeDiffuseLighting);
}

// Just a shortcut that call function above
real3 SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_PARAM(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS, float4 transform, float3 normalWS, bool isStaticLightmap)
{
    float3 backNormalWSUnused = 0.0;
    real3 bakeDiffuseLighting = 0.0;
    real3 backBakeDiffuseLightingUnused = 0.0;
    SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_ARGS(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS_USE, transform,
                                normalWS, backNormalWSUnused, isStaticLightmap, bakeDiffuseLighting, backBakeDiffuseLightingUnused);

    return bakeDiffuseLighting;
}

// deprecated
real3 SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_PARAM(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS, float4 transform, float3 normalWS,
    bool isStaticLightmap, real4 ignore)
{
    return SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(lightmapTex, lightmapSampler), TEXTURE2D_LIGHTMAP_ARGS(lightmapDirTex, lightmapDirSampler), LIGHTMAP_EXTRA_ARGS_USE, transform, normalWS, isStaticLightmap);
}

#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_SWITCH || SHADER_API_SWITCH2
#pragma warning (enable : 3205) // conversion of larger type to smaller
#endif

#endif // UNITY_ENTITY_LIGHTING_INCLUDED
