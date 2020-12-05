#ifndef UNITY_LIGHT_LOOP_DEF_INCLUDED
#define UNITY_LIGHT_LOOP_DEF_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

#define DWORD_PER_TILE 16 // See dwordsPerTile in LightLoop.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

// Some file may not required HD shadow context at all. In this case provide an empty one
// Note: if a double defintion error occur it is likely have include HDShadow.hlsl (and so HDShadowContext.hlsl) after lightloopdef.hlsl
#ifndef HAVE_HD_SHADOW_CONTEXT
struct HDShadowContext { float unused; };
#endif

// LightLoopContext is not visible from Material (user should not use these properties in Material file)
// It allow the lightloop to have transmit sampling information (do we use atlas, or texture array etc...)
struct LightLoopContext
{
    int sampleReflection;

    HDShadowContext shadowContext;

    uint contactShadow;         // a bit mask of 24 bits that tell if the pixel is in a contact shadow or not
    real contactShadowFade;     // combined fade factor of all contact shadows
    SHADOW_TYPE shadowValue;    // Stores the value of the cascade shadow map
};

// LightLoopOutput is the output of the LightLoop fuction call.
// It allow to retrieve the data output by the LightLoop
struct LightLoopOutput
{
    float3 diffuseLighting;
    float3 specularLighting;
};

//-----------------------------------------------------------------------------
// Reflection probe / Sky sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

#define PLANAR_ATLAS_SIZE _PlanarAtlasData.x
#define PLANAR_ATLAS_RCP_MIP_PADDING _PlanarAtlasData.y

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    ZERO_INITIALIZE(EnvLightData, output);
    output.lightLayers = 0xFFFFFFFF; // Enable sky for all layers
    output.influenceShapeType = ENVSHAPETYPE_SKY;
    // 31 bit index, 1 bit cache type
    output.envIndex = envIndex;

    output.influenceForward = float3(0.0, 0.0, 1.0);
    output.influenceUp = float3(0.0, 1.0, 0.0);
    output.influenceRight = float3(1.0, 0.0, 0.0);
    output.influencePositionRWS = float3(0.0, 0.0, 0.0);

    output.weight = 1.0;
    output.multiplier = _EnableSkyReflection.x != 0 ? 1.0 : 0.0;
    output.roughReflections = 1.0;
    output.distanceBasedRoughness = 0.0;

    // proxy
    output.proxyForward = float3(0.0, 0.0, 1.0);
    output.proxyUp = float3(0.0, 1.0, 0.0);
    output.proxyRight = float3(1.0, 0.0, 0.0);
    output.minProjectionDistance = 65504.0f;

    return output;
}

bool IsEnvIndexCubemap(int index)   { return index >= 0; }
bool IsEnvIndexTexture2D(int index) { return index < 0; }

// Clamp the UVs to avoid edge beelding caused by bilinear filtering on the edge of the atlas.
float2 RemapUVForPlanarAtlas(float2 coord, float2 size, float lod)
{
    float2 scale = rcp(size + PLANAR_ATLAS_RCP_MIP_PADDING) * size;
    float2 offset = 0.5 * (1.0 - scale);

    // Avoid edge bleeding for texture when sampling with lod by clamping uvs:
    float2 mipClamp = pow(2, lod) / (size * PLANAR_ATLAS_SIZE * 2);
    return clamp(coord * scale + offset, mipClamp, 1 - mipClamp);
}

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
// Cubemap      : texCoord = direction vector
// Texture2D    : texCoord = projectedPositionWS - lightData.capturePosition
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod, float rangeCompressionFactorCompensation, float2 positionNDC, int sliceIdx = 0)
{
    // 31 bit index, 1 bit cache type
    uint cacheType = IsEnvIndexCubemap(index) ? ENVCACHETYPE_CUBEMAP : ENVCACHETYPE_TEXTURE2D;
    // Index start at 1, because -0 == 0, so we can't known which cache to sample for that index. Thus it is invalid.
    index = abs(index) - 1;

    float4 color = float4(0.0, 0.0, 0.0, 1.0);

    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        if (cacheType == ENVCACHETYPE_TEXTURE2D)
        {
            //_Env2DCaptureVP is in capture space
            float3 ndc = ComputeNormalizedDeviceCoordinatesWithZ(texCoord, _Env2DCaptureVP[index]);

            // Apply atlas scale and offset
            float2 scale = _Env2DAtlasScaleOffset[index].xy;
            float2 offset = _Env2DAtlasScaleOffset[index].zw;
            float2 atlasCoords = RemapUVForPlanarAtlas(ndc.xy, scale, lod);
            atlasCoords = atlasCoords * scale + offset;

            color.rgb = SAMPLE_TEXTURE2D_LOD(_Env2DTextures, s_trilinear_clamp_sampler, atlasCoords, lod).rgb;
#if UNITY_REVERSED_Z
            // We check that the sample was capture by the probe according to its frustum planes, except the far plane.
            //   When using oblique projection, the far plane is so distorded that it is not reliable for this check.
            //   and most of the time, what we want, is the clipping from the oblique near plane.
            color.a = any(ndc.xy < 0) || any(ndc.xyz > 1) ? 0.0 : 1.0;
#else
            color.a = any(ndc.xyz < 0) || any(ndc.xy > 1) ? 0.0 : 1.0;
#endif
            float3 capturedForwardWS = _Env2DCaptureForward[index].xyz;
            if (dot(capturedForwardWS, texCoord) < 0.0)
                color.a = 0.0;
            else
            {
                // Controls the blending on the edges of the screen
                const float amplitude = 100.0;

                float2 rcoords = abs(saturate(ndc.xy) * 2.0 - 1.0);

                // When the object normal is not aligned with the reflection plane, the reflected ray might deviate too much and go out
                // of the reflection frustum. So we apply blending when the reflection sample coords are on the edges of the texture
                // These "edges" depend on the screen space coordinates of the pixel, because it is expected that a pixel on the
                // edge of the screen will sample on the edge of the texture

                // Blending factors taking the above into account
                bool2 blend = (positionNDC < ndc.xy) ^ (ndc.xy < 0.5);
                float2 alphas = saturate(amplitude * abs(ndc.xy - positionNDC));
                alphas = float2(Smoothstep01(alphas.x), Smoothstep01(alphas.y));

                float2 weights = lerp(1.0, saturate(2.0 - 2.0 * rcoords), blend * alphas);
                color.a *= weights.x * weights.y;
            }
        }
        else if (cacheType == ENVCACHETYPE_CUBEMAP)
        {
            color.rgb = SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_EnvCubemapTextures, s_trilinear_clamp_sampler, texCoord, _EnvSliceSize * index + sliceIdx, lod).rgb;
        }

        color.rgb *= rangeCompressionFactorCompensation;
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        color.rgb = SampleSkyTexture(texCoord, lod, sliceIdx).rgb;
    }

    // Planar, Reflection Probes and Sky aren't pre-expose, so best to clamp to max16 here in case of inf
    color.rgb = ClampToFloat16Max(color.rgb);

    return color;
}

//-----------------------------------------------------------------------------
// Single Pass and Tile Pass
// ----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/TilingAndBinningUtilities.hlsl"

#if (defined(COARSE_BINNING) || defined(FINE_BINNING))

// Internal. Do not call directly.
uint TryFindEntityIndex(inout uint i, uint tile, uint2 zBinRange, uint category, out uint entityIndex)
{
    entityIndex = UINT16_MAX;

    bool b = false;
    uint n = TILE_ENTRY_LIMIT;

    // This part (1) is loop-invariant. These values do not depend on the value of 'i'.
    // They will only be computed once per category, not once per function call.
    const uint tileBufferHeaderIndex = ComputeTileBufferHeaderIndex(tile, category, unity_StereoEyeIndex);
    const uint tileRangeData         = TILE_BUFFER[tileBufferHeaderIndex]; // {last << 16 | first}
    const bool isTileEmpty           = tileRangeData == UINT16_MAX;

    if (!isTileEmpty) // Avoid wasted work
    {
        const uint zBinBufferIndex0 = ComputeZBinBufferIndex(zBinRange[0], category, unity_StereoEyeIndex);
        const uint zBinBufferIndex1 = ComputeZBinBufferIndex(zBinRange[1], category, unity_StereoEyeIndex);
        const uint zBinRangeData0   = _zBinBuffer[zBinBufferIndex0]; // {last << 16 | first}
        const uint zBinRangeData1   = _zBinBuffer[zBinBufferIndex1]; // {last << 16 | first}

        // Recall that entities are sorted by the z-coordinate.
        // So we can take the smallest index from the first bin and the largest index from the last bin.
        // TODO: while it appears to work, for a large range, this code can actually fail. :-(
        // Imagine that we are given the first and the last zBins, and that none of the lights overlap them.
        // All the lights are in between. So the code below would give us an empty range, which is wrong.
        // Should we loop over the entire zBinRange? Seems impractical... Maybe change the way we sort?
        const uint2 tileEntityIndexRange = uint2(tileRangeData  & UINT16_MAX, tileRangeData  >> 16);
        const uint2 zBinEntityIndexRange = uint2(zBinRangeData0 & UINT16_MAX, zBinRangeData1 >> 16);

        if (IntervalsOverlap(tileEntityIndexRange, zBinEntityIndexRange)) // Avoid wasted work
        {
            const uint tileBufferBodyIndex = ComputeTileBufferBodyIndex(tile, category, unity_StereoEyeIndex);

            // The part (2) below will be actually executed during every function call.
            while (i < n)
            {
                // Walk the list of entity indices of the tile.
                uint tileEntityPair  = TILE_BUFFER[tileBufferBodyIndex + (i / 2)];        // 16-bit indices
                uint tileEntityIndex = BitFieldExtract(tileEntityPair, 16 * (i & 1), 16); // First Lo, then Hi bits

                // Entity indices are stored in the ascending order.
                // We can distinguish 3 cases:
                if (tileEntityIndex < zBinEntityIndexRange.x)
                {
                    i++; // Skip this entity; continue the (linear) search
                    // Fine point: entities are currently sorted by Centroid.z.
                    // So it could be that bin #0 starts with entity #1 (which is large)
                    // while bin #1 contains both entity #0 (which is small) and entity #1.
                    // So, for volumetrics, we cannot create a skipping scheme where we cache (and
                    // start from) min_valid(i) per slice, as that may make us skip valid lights.
                }
                else if (tileEntityIndex <= zBinEntityIndexRange.y)
                {
                    entityIndex = tileEntityIndex;

                    b = true; // Found a valid index
                    break;    // Avoid incrementing 'i' further
                }
                else // if (zBinEntityIndexRange.y < tileEntityIndex)
                {
                    // (tileEntityIndex == UINT16_MAX) signifies a terminator.
                    break;    // Avoid incrementing 'i' further
                }
            }
        }
    }

    return b;
}

#else // !(defined(COARSE_BINNING) || defined(FINE_BINNING))

// Internal. Do not call directly.
uint TryFindEntityIndex(inout uint i, uint tile, uint2 zBinRange, uint category, out uint entityIndex)
{
    entityIndex = UINT16_MAX;

    bool b = false;
    uint n;

    switch (category)
    {
        case BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT:
            n = _PunctualLightCount;
            break;
        case BOUNDEDENTITYCATEGORY_AREA_LIGHT:
            n = _AreaLightCount;
            break;
        case BOUNDEDENTITYCATEGORY_REFLECTION_PROBE:
            n = _ReflectionProbeCount;
            break;
        case BOUNDEDENTITYCATEGORY_DECAL:
            n = _DecalCount;
            break;
        case BOUNDEDENTITYCATEGORY_DENSITY_VOLUME:
            n = _DensityVolumeCount;
            break;
        default:
            n = 0;
            break;
    }

    if (i < n)
    {
        entityIndex = i;
        b           = true;
    }

    return b;
}

#endif // !(defined(COARSE_BINNING) || defined(FINE_BINNING))

// Internal. Do not call directly.
uint TryFindEntityIndex(inout uint i, uint tile, uint zBin, uint category, out uint entityIndex)
{
    uint2 zBinRange = uint2(zBin, zBin);

    return TryFindEntityIndex(i, tile, zBinRange, category, entityIndex);
}

bool TryLoadPunctualLightData(inout uint i, uint tile, uint zBin, out LightData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBin, BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT, entityIndex))
    {
        success = true;
        data    = _PunctualLightData[entityIndex];
    }

    return success;
}

bool TryLoadPunctualLightData(inout uint i, uint tile, uint2 zBinRange, out LightData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBinRange, BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT, entityIndex))
    {
        success = true;
        data    = _PunctualLightData[entityIndex];
    }

    return success;
}

bool TryLoadAreaLightData(inout uint i, uint tile, uint zBin, out LightData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBin, BOUNDEDENTITYCATEGORY_AREA_LIGHT, entityIndex))
    {
        success = true;
        data    = _AreaLightData[entityIndex];
    }

    return success;
}

bool TryLoadAreaLightData(inout uint i, uint tile, uint2 zBinRange, out LightData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBinRange, BOUNDEDENTITYCATEGORY_AREA_LIGHT, entityIndex))
    {
        success = true;
        data    = _AreaLightData[entityIndex];
    }

    return success;
}

bool TryLoadReflectionProbeData(inout uint i, uint tile, uint zBin, out EnvLightData data, out uint entityIndex)
{
    bool success = false;

    if (TryFindEntityIndex(i, tile, zBin, BOUNDEDENTITYCATEGORY_REFLECTION_PROBE, entityIndex))
    {
        success = true;
        data    = _ReflectionProbeData[entityIndex];
    }

    return success;
}

bool TryLoadReflectionProbeData(inout uint i, uint tile, uint2 zBinRange, out EnvLightData data, out uint entityIndex)
{
    bool success = false;

    if (TryFindEntityIndex(i, tile, zBinRange, BOUNDEDENTITYCATEGORY_REFLECTION_PROBE, entityIndex))
    {
        success = true;
        data    = _ReflectionProbeData[entityIndex];
    }

    return success;
}

bool TryLoadDecalData(inout uint i, uint tile, uint zBin, out DecalData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBin, BOUNDEDENTITYCATEGORY_DECAL, entityIndex))
    {
        success = true;
        data    = _DecalData[entityIndex];
    }

    return success;
}

bool TryLoadDecalData(inout uint i, uint tile, uint2 zBinRange, out DecalData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBinRange, BOUNDEDENTITYCATEGORY_DECAL, entityIndex))
    {
        success = true;
        data    = _DecalData[entityIndex];
    }

    return success;
}

bool TryLoadDensityVolumeData(inout uint i, uint tile, uint zBin, out DensityVolumeData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBin, BOUNDEDENTITYCATEGORY_DENSITY_VOLUME, entityIndex))
    {
        success = true;
        data    = _DensityVolumeData[entityIndex];
    }

    return success;
}

bool TryLoadDensityVolumeData(inout uint i, uint tile, uint2 zBinRange, out DensityVolumeData data)
{
    bool success = false;

    uint entityIndex;
    if (TryFindEntityIndex(i, tile, zBinRange, BOUNDEDENTITYCATEGORY_DENSITY_VOLUME, entityIndex))
    {
        success = true;
        data    = _DensityVolumeData[entityIndex];
    }

    return success;
}

// This call performs no bounds-checking.
// Only call this using the 'entityIndex' returned by TryLoadReflectionProbeData, and
// only if the call to TryLoadReflectionProbeData succeeded.
EnvLightData LoadReflectionProbeDataUnsafe(uint entityIndex)
{
    return _ReflectionProbeData[entityIndex];
}

// In the first 8 bits of the target we store the max fade of the contact shadows as a byte
void UnpackContactShadowData(uint contactShadowData, out float fade, out uint mask)
{
    fade = float(contactShadowData >> 24) / 255.0;
    mask = contactShadowData & 0xFFFFFF; // store only the first 24 bits which represent
}

uint PackContactShadowData(float fade, uint mask)
{
    uint fadeAsByte = (uint(saturate(fade) * 255) << 24);

    return fadeAsByte | mask;
}

// We always fetch the screen space shadow texture to reduce the number of shader variant, overhead is negligible,
// it is a 1x1 white texture if deferred directional shadow and/or contact shadow are disabled
// We perform a single featch a the beginning of the lightloop
void InitContactShadow(PositionInputs posInput, inout LightLoopContext context)
{
    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for contact shadows that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse contact shadow so neutral is white. So either we sample inside or outside the texture it return 1 in case of neutral
    uint packedContactShadow = LOAD_TEXTURE2D_X(_ContactShadowTexture, posInput.positionSS).x;
    UnpackContactShadowData(packedContactShadow, context.contactShadowFade, context.contactShadow);
}

void InvalidateConctactShadow(PositionInputs posInput, inout LightLoopContext context)
{
    context.contactShadowFade = 0.0;
    context.contactShadow = 0;
}

float GetContactShadow(LightLoopContext lightLoopContext, int contactShadowMask, float rayTracedShadow)
{
    bool occluded = (lightLoopContext.contactShadow & contactShadowMask) != 0;
    return 1.0 - occluded * lerp(lightLoopContext.contactShadowFade, 1.0, rayTracedShadow) * _ContactShadowOpacity;
}

float GetScreenSpaceShadow(PositionInputs posInput, uint shadowIndex)
{
    uint slot = shadowIndex / 4;
    uint channel = shadowIndex & 0x3;
    return LOAD_TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture, posInput.positionSS, INDEX_TEXTURE2D_ARRAY_X(slot))[channel];
}

float2 GetScreenSpaceShadowArea(PositionInputs posInput, uint shadowIndex)
{
    uint slot = shadowIndex / 4;
    uint channel = shadowIndex & 0x3;
    return float2(LOAD_TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture, posInput.positionSS, INDEX_TEXTURE2D_ARRAY_X(slot))[channel], LOAD_TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture, posInput.positionSS, INDEX_TEXTURE2D_ARRAY_X(slot))[channel + 1]);
}

float3 GetScreenSpaceColorShadow(PositionInputs posInput, int shadowIndex)
{
    float4 res = LOAD_TEXTURE2D_ARRAY(_ScreenSpaceShadowsTexture, posInput.positionSS, INDEX_TEXTURE2D_ARRAY_X(shadowIndex & SCREEN_SPACE_SHADOW_INDEX_MASK));
    return (SCREEN_SPACE_COLOR_SHADOW_FLAG & shadowIndex) ? res.xyz : res.xxx;
}

#endif // UNITY_LIGHT_LOOP_DEF_INCLUDED
