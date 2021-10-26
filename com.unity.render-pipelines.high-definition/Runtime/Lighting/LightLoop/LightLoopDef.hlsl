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

        // Planar and Reflection Probes aren't pre-expose, so best to clamp to max16 here in case of inf
        color.rgb = ClampToFloat16Max(color.rgb);

        color.rgb *= rangeCompressionFactorCompensation;
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        color.rgb = SampleSkyTexture(texCoord, lod, sliceIdx).rgb;
        // Sky isn't pre-expose, so best to clamp to max16 here in case of inf
        color.rgb = ClampToFloat16Max(color.rgb);
    }


    return color;
}

//-----------------------------------------------------------------------------
// Single Pass and Tile Pass
// ----------------------------------------------------------------------------

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER

// Calculate the offset in global light index light for current light category
int GetTileOffset(PositionInputs posInput, uint lightCategory)
{
    uint2 tileIndex = posInput.tileCoord;
    return (tileIndex.y + lightCategory * _NumTileFtplY) * _NumTileFtplX + tileIndex.x;
}

void GetCountAndStartTile(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    int tileOffset = GetTileOffset(posInput, lightCategory);

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    // Eye base offset must match code in lightlistbuild.compute
    tileOffset += unity_StereoEyeIndex * _NumTileFtplX * _NumTileFtplY * LIGHTCATEGORY_COUNT;
#endif

    // The first entry inside a tile is the number of light for lightCategory (thus the +0)
    lightCount = g_vLightListGlobal[DWORD_PER_TILE * tileOffset + 0] & 0xffff;
    start = tileOffset;
}

#ifdef USE_FPTL_LIGHTLIST

uint GetTileSize()
{
    return TILE_SIZE_FPTL;
}

void GetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    GetCountAndStartTile(posInput, lightCategory, start, lightCount);
}

uint FetchIndex(uint tileOffset, uint lightOffset)
{
    const uint lightOffsetPlusOne = lightOffset + 1; // Add +1 as first slot is reserved to store number of light
    // Light index are store on 16bit
    return (g_vLightListGlobal[DWORD_PER_TILE * tileOffset + (lightOffsetPlusOne >> 1)] >> ((lightOffsetPlusOne & 1) * DWORD_PER_TILE)) & 0xffff;
}

#elif defined(USE_CLUSTERED_LIGHTLIST)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/ClusteredUtils.hlsl"

uint GetTileSize()
{
    return TILE_SIZE_CLUSTERED;
}

uint GetLightClusterIndex(uint2 tileIndex, float linearDepth)
{
    float logBase = g_fClustBase;
    if (g_isLogBaseBufferEnabled)
    {
        const uint logBaseIndex = GenerateLogBaseBufferIndex(tileIndex, _NumTileClusteredX, _NumTileClusteredY, unity_StereoEyeIndex);
        logBase = g_logBaseBuffer[logBaseIndex];
    }

    return SnapToClusterIdxFlex(linearDepth, logBase, g_isLogBaseBufferEnabled != 0);
}

void GetCountAndStartCluster(uint2 tileIndex, uint clusterIndex, uint lightCategory, out uint start, out uint lightCount)
{
    int nrClusters = (1 << g_iLog2NumClusters);

    const int idx = GenerateLayeredOffsetBufferIndex(lightCategory, tileIndex, clusterIndex, _NumTileClusteredX, _NumTileClusteredY, nrClusters, unity_StereoEyeIndex);

    uint dataPair = g_vLayeredOffsetsBuffer[idx];
    start = dataPair & 0x7ffffff;
    lightCount = (dataPair >> 27) & 31;
}

void GetCountAndStartCluster(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    // Note: XR depends on unity_StereoEyeIndex already being defined,
    // which means ShaderVariables.hlsl needs to be defined ahead of this!

    uint2 tileIndex    = posInput.tileCoord;
    uint  clusterIndex = GetLightClusterIndex(tileIndex, posInput.linearDepth);

    GetCountAndStartCluster(tileIndex, clusterIndex, lightCategory, start, lightCount);
}

void GetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    GetCountAndStartCluster(posInput, lightCategory, start, lightCount);
}

uint FetchIndex(uint lightStart, uint lightOffset)
{
    return g_vLightListGlobal[lightStart + lightOffset];
}

#elif defined(USE_BIG_TILE_LIGHTLIST)

uint FetchIndex(uint lightStart, uint lightOffset)
{
    return g_vBigTileLightList[lightStart + lightOffset];
}

#else
// Fallback case (mainly for raytracing right now)
uint FetchIndex(uint lightStart, uint lightOffset)
{
    return 0;
}
#endif // USE_FPTL_LIGHTLIST

#else

uint GetTileSize()
{
    return 1;
}

uint FetchIndex(uint lightStart, uint lightOffset)
{
    return lightStart + lightOffset;
}

#endif // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER

uint FetchIndexWithBoundsCheck(uint start, uint count, uint i)
{
    if (i < count)
    {
        return FetchIndex(start, i);
    }
    else
    {
        return UINT_MAX;
    }
}

LightData FetchLight(uint start, uint i)
{
    uint j = FetchIndex(start, i);

    return _LightDatas[j];
}

LightData FetchLight(uint index)
{
    return _LightDatas[index];
}

EnvLightData FetchEnvLight(uint start, uint i)
{
    int j = FetchIndex(start, i);

    return _EnvLightDatas[j];
}

EnvLightData FetchEnvLight(uint index)
{
    return _EnvLightDatas[index];
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
