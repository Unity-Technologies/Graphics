#ifndef UNITY_ENTITY_LIGHTING_INCLUDED
#define UNITY_ENTITY_LIGHTING_INCLUDED

#include "common.hlsl"

// TODO: Check if PI is correctly handled!

// Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
REAL3 SHEvalLinearL0L1(REAL3 N, REAL4 shAr, REAL4 shAg, REAL4 shAb)
{
    REAL4 vA = REAL4(N, 1.0);

    REAL3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(shAr, vA);
    x1.g = dot(shAg, vA);
    x1.b = dot(shAb, vA);

    return x1;
}

REAL3 SHEvalLinearL2(REAL3 N, REAL4 shBr, REAL4 shBg, REAL4 shBb, REAL4 shC)
{
    REAL3 x2;
    // 4 of the quadratic (L2) polynomials
    REAL4 vB = N.xyzz * N.yzzx;
    x2.r = dot(shBr, vB);
    x2.g = dot(shBg, vB);
    x2.b = dot(shBb, vB);

    // Final (5th) quadratic (L2) polynomial
    REAL vC = N.x * N.x - N.y * N.y;
    REAL3 x3 = shC.rgb * vC;

    return x2 + x3;
}

REAL3 SampleSH9(REAL4 SHCoefficients[7], REAL3 N)
{
    REAL4 shAr = SHCoefficients[0];
    REAL4 shAg = SHCoefficients[1];
    REAL4 shAb = SHCoefficients[2];
    REAL4 shBr = SHCoefficients[3];
    REAL4 shBg = SHCoefficients[4];
    REAL4 shBb = SHCoefficients[5];
    REAL4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    REAL3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

    return res;
}

// This sample a 3D volume storing SH
// Volume is store as 3D texture with 4 R, G, B, Occ set of 4 coefficient store atlas in same 3D texture. Occ is use for occlusion.
// TODO: the packing here is inefficient as we will fetch values far away from each other and they may not fit into the cache - Suggest we pack RGB continuously
// TODO: The calcul of texcoord could be perform with a single matrix multicplication calcualted on C++ side that will fold probeVolumeMin and probeVolumeSizeInv into it and handle the identity case, no reasons to do it in C++ (ask Ionut about it)
// It should also handle the camera relative path (if the render pipeline use it)
REAL3 SampleProbeVolumeSH4(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), REAL3 positionWS, REAL3 normalWS, REAL4x4 WorldToTexture,
                            REAL transformToLocal, REAL texelSizeX, REAL3 probeVolumeMin, REAL3 probeVolumeSizeInv)
{
    REAL3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, REAL4(positionWS, 1.0)).xyz : positionWS;
    REAL3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;
    // Each component is store in the same texture 3D. Each use one quater on the x axis
    // Here we get R component then increase by step size (0.25) to get other component. This assume 4 component
    // but last one is not used.
    // Clamp to edge of the "internal" texture, as R is from REAL texel to size of R texture minus REAL texel.
    // This avoid leaking
    texCoord.x = clamp(texCoord.x * 0.25, 0.5 * texelSizeX, 0.25 - 0.5 * texelSizeX);

    REAL4 shAr = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
    texCoord.x += 0.25;
    REAL4 shAg = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
    texCoord.x += 0.25;
    REAL4 shAb = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);

    return SHEvalLinearL0L1(normalWS, shAr, shAg, shAb);
}

REAL4 SampleProbeOcclusion(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), REAL3 positionWS, REAL4x4 WorldToTexture,
                            REAL transformToLocal, REAL texelSizeX, REAL3 probeVolumeMin, REAL3 probeVolumeSizeInv)
{
    REAL3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, REAL4(positionWS, 1.0)).xyz : positionWS;
    REAL3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;

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

// RGBM lightmaps are currently always gamma encoded, so we use a constant of range^2.2 = 5^2.2
#define LIGHTMAP_RGBM_RANGE 34.493242f

// DLRD lightmaps are currently always gamma encoded, so we use a constant of 2.0^2.2 = 4.59
#define LIGHTMAP_DLDR_RANGE 4.59f

// TODO: This is the max value allowed for emissive (bad name - but keep for now to retrieve it) (It is 8^2.2 (gamma) and 8 is the limit of punctual light slider...), comme from UnityCg.cginc. Fix it!
// Ask Jesper if this can be change for HDRenderPipeline
#define EMISSIVE_RGBM_SCALE 97.0

// RGBM stuff is temporary. For now baked lightmap are in RGBM and the RGBM range for lightmaps is specific so we can't use the generic method.
// In the end baked lightmaps are going to be BC6H so the code will be the same as dynamic lightmaps.
// Same goes for emissive packed as an input for Enlighten with another hard coded multiplier.

// TODO: This function is used with the LightTransport pass to encode lightmap or emissive
REAL4 PackEmissiveRGBM(REAL3 rgb)
{
    REAL kOneOverRGBMMaxRange = 1.0 / EMISSIVE_RGBM_SCALE;
    const REAL kMinMultiplier = 2.0 * 1e-2;

    REAL4 rgbm = REAL4(rgb * kOneOverRGBMMaxRange, 1.0);
        rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    rgbm.a = max(rgbm.a, kMinMultiplier);

    rgbm.rgb /= rgbm.a;
    return rgbm;
}

REAL3 UnpackLightmapRGBM(REAL4 rgbmInput)
{
    // RGBM lightmaps are always gamma encoded for now, so decode with that in mind:
    return rgbmInput.rgb * pow(rgbmInput.a, 2.2f) * LIGHTMAP_RGBM_RANGE;
}

REAL3 UnpackLightmapDoubleLDR(REAL4 encodedColor)
{
    return encodedColor.rgb * LIGHTMAP_DLDR_RANGE;
}

REAL3 DecodeLightmap(REAL4 encodedIlluminance)
{
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    return UnpackLightmapRGBM(encodedIlluminance);
#else // DLDR encoding on mobile platforms
    return UnpackLightmapDoubleLDR(encodedIlluminance);
#endif
}

REAL3 DecodeHDREnvironment(REAL4 encodedIrradiance, REAL4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    REAL alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

REAL3 SampleSingleLightmap(TEXTURE2D_ARGS(lightmapTex, lightmapSampler), REAL2 uv, REAL4 transform, bool encodedLightmap)
{
    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;
    REAL3 illuminance = REAL3(0.0, 0.0, 0.0);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    if (encodedLightmap)
    {
        REAL4 encodedIlluminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgba;
        illuminance = DecodeLightmap(encodedIlluminance);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgb;
    }
    return illuminance;
}

REAL3 SampleDirectionalLightmap(TEXTURE2D_ARGS(lightmapTex, lightmapSampler), TEXTURE2D_ARGS(lightmapDirTex, lightmapDirSampler), REAL2 uv, REAL4 transform, REAL3 normalWS, bool encodedLightmap)
{
    // In directional mode Enlighten bakes dominant light direction
    // in a way, that using it for REAL Lambert and then dividing by a "rebalancing coefficient"
    // gives a result close to plain diffuse response lightmaps, but normalmapped.

    // Note that dir is not unit length on purpose. Its length is "directionality", like
    // for the directional specular lightmaps.

    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;

    REAL4 direction = SAMPLE_TEXTURE2D(lightmapDirTex, lightmapDirSampler, uv);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    REAL3 illuminance = REAL3(0.0, 0.0, 0.0);
    if (encodedLightmap)
    {
        REAL4 encodedIlluminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgba;
        illuminance = DecodeLightmap(encodedIlluminance);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgb;
    }
    REAL halfLambert = dot(normalWS, direction.xyz - 0.5) + 0.5;
    return illuminance * halfLambert / max(1e-4, direction.w);
}

#endif // UNITY_ENTITY_LIGHTING_INCLUDED
