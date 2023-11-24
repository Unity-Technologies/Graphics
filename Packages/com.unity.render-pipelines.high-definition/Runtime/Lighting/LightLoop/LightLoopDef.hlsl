#ifndef UNITY_LIGHT_LOOP_DEF_INCLUDED
#define UNITY_LIGHT_LOOP_DEF_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

#define DWORD_PER_TILE 16 // See dwordsPerTile in LightLoop.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

// Depending if we are in ray tracing or not we need to use a different set of environement data
#if defined(WORLD_ENVIRONMENT_DATA) || defined(SHADER_STAGE_RAY_TRACING)
#define PLANAR_CAPTURE_VP _PlanarCaptureVPWL
#define PLANAR_SCALE_OFFSET _PlanarScaleOffsetWL
#define CUBE_SCALE_OFFSET _CubeScaleOffsetWL
#else
#define PLANAR_CAPTURE_VP _PlanarCaptureVP
#define PLANAR_SCALE_OFFSET _PlanarScaleOffset
#define CUBE_SCALE_OFFSET _CubeScaleOffset
#endif

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
#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
    float3 positionWS; // For sky reflection, we need position to evalute height base fog
#endif

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

#define REFL_ATLAS_CUBE_PADDING _ReflectionAtlasCubeData.xy
#define REFL_ATLAS_CUBE_MIP_PADDING _ReflectionAtlasCubeData.z
#define REFL_ATLAS_PLANAR_PADDING _ReflectionAtlasPlanarData.xy
#define REFL_ATLAS_TEXEL_SIZE _ReflectionAtlasPlanarData.zw

float2 GetReflectionAtlasCoordsCube(float4 scaleOffset, float3 dir, float lod)
{
    float2 uv = saturate(PackNormalOctQuadEncode(dir) * 0.5 + 0.5);
    float2 padding = REFL_ATLAS_CUBE_PADDING;
    // Every octahedral mip has at least one texel padding. This improves octahedral mapping as it always should have border for every mip.
    padding *= pow(2.0, max(lod - REFL_ATLAS_CUBE_MIP_PADDING, 0.0));
    float2 size = scaleOffset.xy - padding;
    float2 offset = scaleOffset.zw + 0.5 * padding;
    return uv * size + offset;
}

float2 GetReflectionAtlasCoordsPlanar(float4 scaleOffset, float2 uv, float lod)
{
    float2 padding = REFL_ATLAS_PLANAR_PADDING;
    float2 size = scaleOffset.xy - padding;
    float2 offset = scaleOffset.zw + 0.5 * padding;
    float2 paddingClamp = REFL_ATLAS_TEXEL_SIZE * pow(2.0, ceil(lod)) * 0.5;
    float2 minClamp = scaleOffset.zw + paddingClamp;
    float2 maxClamp = scaleOffset.zw + scaleOffset.xy - paddingClamp;
    return clamp(uv * size + offset, minClamp, maxClamp);
}

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

// Variant environment data that provides a better default behavior for screen space refraction, when no proxy volume is available.
EnvLightData InitDefaultRefractionEnvLightData(int envIndex)
{
    EnvLightData output = InitSkyEnvLightData(envIndex);

    // For screen space refraction, instead of an infinite projection, utilize the renderer's extents.
    output.proxyExtents = GetRendererExtents();

    // Revert the infinite projection.
    output.minProjectionDistance = 0;

    return output;
}

bool IsEnvIndexCubemap(int index)   { return index >= 0; }
bool IsEnvIndexTexture2D(int index) { return index < 0; }

float EdgeSampleFade(float2 positionNDC, float2 samplePositionNDC, float amplitude = 100.0f)
{
    float2 ndc = samplePositionNDC;
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
    return weights.x * weights.y;
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
            //_ReflAtlasPlanarCaptureVP is in capture space
            float4 samplePosCS = ComputeClipSpacePosition(texCoord, PLANAR_CAPTURE_VP[index]);
#if UNITY_UV_STARTS_AT_TOP
            samplePosCS.y = -samplePosCS.y;
#endif
            float2 ndc = samplePosCS.xy * rcp(samplePosCS.w);
            ndc.xy = ndc.xy * 0.5 + 0.5;
            float2 atlasCoords = GetReflectionAtlasCoordsPlanar(PLANAR_SCALE_OFFSET[index], ndc.xy, lod);

            color.a = all(abs(samplePosCS.xyz) < samplePosCS.w) ? 1.0 : 0.0;
            if (color.a > 0.0)
            {
                color.rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, atlasCoords, sliceIdx, lod).rgb;
                color.a *= EdgeSampleFade(positionNDC, ndc.xy);
            }
        }
        else if (cacheType == ENVCACHETYPE_CUBEMAP)
        {
            float2 atlasCoords = GetReflectionAtlasCoordsCube(CUBE_SCALE_OFFSET[index], texCoord, lod);

            color.rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, atlasCoords, sliceIdx, lod).rgb;
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

#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
        if (_FogEnabled)
        {
            float4 fogAttenuation = EvaluateFogForSkyReflections(lightLoopContext.positionWS, texCoord);
            color.rgb = color.rgb * fogAttenuation.a + fogAttenuation.rgb;
        }
#endif
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
    lightCount = g_vLightListTile[LIGHT_DWORD_PER_FPTL_TILE * tileOffset + 0] & 0xffff;
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
    return (g_vLightListTile[LIGHT_DWORD_PER_FPTL_TILE * tileOffset + (lightOffsetPlusOne >> 1)] >> ((lightOffsetPlusOne & 1) * 16)) & 0xffff;
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

void UnpackClusterLayeredOffset(uint packedValue, out uint offset, out uint count)
{
    offset = packedValue & LIGHT_CLUSTER_PACKING_OFFSET_MASK;
    count = packedValue >> LIGHT_CLUSTER_PACKING_OFFSET_BITS;
}

void GetCountAndStartCluster(uint2 tileIndex, uint clusterIndex, uint lightCategory, out uint start, out uint lightCount)
{
    int nrClusters = (1 << g_iLog2NumClusters);

    const int idx = GenerateLayeredOffsetBufferIndex(lightCategory, tileIndex, clusterIndex, _NumTileClusteredX, _NumTileClusteredY, nrClusters, unity_StereoEyeIndex);

    uint dataPair = g_vLayeredOffsetsBuffer[idx];
    UnpackClusterLayeredOffset(dataPair, start, lightCount);
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
    return g_vLightListCluster[lightStart + lightOffset];
}

#elif defined(USE_BIG_TILE_LIGHTLIST)

// The "big tile" list contains the number of objects contained within the tile followed by the
// list of object indices. Note that while objects are already sorted by type, we don't know the
// number of each type of objects (e.g. lights), so we should remember to break out of the loop.
// Each light indices are encoded into 16 bits.
void GetBigTileLightCountAndStart(uint tileIndex, out uint lightCount, out uint start)
{
    start = tileIndex;
    lightCount = g_vBigTileLightList[MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE * tileIndex / 2] & 0xFFFF;
    // On Metal for unknow reasons it seems we have bad value sometimes causing freeze / crash. This min here prevent it.
    lightCount = min(lightCount, MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE);
}

uint FetchIndex(uint lightStart, uint lightOffset)
{
    const uint lightOffsetPlusOne = lightOffset + 1; // Add +1 as first slot is reserved to store number of light
    // Light index are store on 16bit
    return (g_vBigTileLightList[MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE * lightStart / 2 + (lightOffsetPlusOne >> 1)] >> ((lightOffsetPlusOne & 1) * 16)) & 0xffff;
}

#else
// Fallback case (mainly for raytracing right or for shader stages that don't define the keywords)
uint FetchIndex(uint lightStart, uint lightOffset)
{
    return 0;
}

uint GetTileSize()
{
    return 1;
}

void GetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    start = 0;
    lightCount = 0;
    return;
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

// In the first bits of the target we store the max fade of the contact shadows as a byte.
//By default its 8 bits for the fade and 24 for the mask, please check the LightLoop.cs definitions.
void UnpackContactShadowData(uint contactShadowData, out float fade, out uint mask)
{
    fade = float(contactShadowData >> CONTACT_SHADOW_MASK_BITS) / ((float)CONTACT_SHADOW_FADE_MASK);
    mask = contactShadowData & CONTACT_SHADOW_MASK_MASK; // store only the first 24 bits which represent
}

uint PackContactShadowData(float fade, uint mask)
{
    uint fadeAsByte = (uint(saturate(fade) * CONTACT_SHADOW_FADE_MASK) << CONTACT_SHADOW_MASK_BITS);

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
