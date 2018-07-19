#include "LightLoop.cs.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"

#define DWORD_PER_TILE 16 // See dwordsPerTile in LightLoop.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

// LightLoopContext is not visible from Material (user should not use these properties in Material file)
// It allow the lightloop to have transmit sampling information (do we use atlas, or texture array etc...)
struct LightLoopContext
{
    int sampleReflection;
    ShadowContext shadowContext;
    float contactShadow; // Currently we support only one contact shadow per view
    float3 positionRWSDdx;
    float3 positionRWSDdy;
};

//-----------------------------------------------------------------------------
// Cookie sampling functions
// ----------------------------------------------------------------------------

// Adjust UVs using the LOD padding size
float2 RemapUV(float2 coord, float2 size)
{
    float2 scale = rcp(size + _CookieAtlasData.y) * size;
    float2 offset = 0.5 * (1.0 - scale);
    
    return coord * scale + offset;
}

// Used by directional and spot lights.
float3 SampleCookie(LightLoopContext lightLoopContext, float2 coord, float4 scaleBias, float2 sampleDdx, float2 sampleDdy)
{
    float2 offset       = scaleBias.zw;
    float2 scale        = scaleBias.xy;
    float lod           = ComputeTextureLOD(sampleDdx, sampleDdy, scale * _CookieAtlasSize.xy);

    // Clamp lod to the maximum level of the texture we are sampling
    lod = min(_CookieAtlasData.x, lod);

    // Manage cookie in repeat mode
    coord = frac(coord);

    // Remap the uv to take in account the padding
    coord = RemapUV(coord, scale);

    // Apply atlas scale and offset
    float2 atlasCoords = coord * scale + offset;

#if 0
    // Anisotripic filtering test
    float2 p = sqrt(pow(sampleDdx, 2) + pow(sampleDdy, 2));
    float pMax = max(p.x, p.y);
    float pMin = min(p.x, p.y);

    uint N = min(ceil(pMax / pMin), 8);
    float lambda = log2(pMax / N);

    float3 tauAniso = 0;
    if (p.x > p.y)
    {
        for (uint i = 1; i <= N; i++)
        {
            float2 anisoCoord = float2(-0.5 + float(i) / (N + 1), 0) * clampBorder.x * 2;
            tauAniso += SAMPLE_TEXTURE2D_LOD(_CookieAtlas, sampler_CookieAtlas, atlasCoords + anisoCoord, lambda).rgb;
        }
    }
    else
    {
        for (uint i = 1; i <= N; i++)
        {
            float2 anisoCoord = float2(0, -0.5 + float(i) / (N + 1)) * clampBorder.y * 2;
            tauAniso += SAMPLE_TEXTURE2D_LOD(_CookieAtlas, sampler_CookieAtlas, atlasCoords + anisoCoord, lambda).rgb;
        }
    }
    tauAniso /= N;

    float3 color = tauAniso;
#else
    float3 color = SAMPLE_TEXTURE2D_LOD(_CookieAtlas, sampler_CookieAtlas, atlasCoords, lod).rgb;
#endif
    
    // Mip visualization (0 -> red, 10 -> blue)
    // color *= saturate(1 - abs(3 * lod / 10 - float4(0, 1, 2, 3))).rgb;

    return color;
}

//-----------------------------------------------------------------------------
// Reflection probe / Sky sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

// The EnvLightData of the sky light contains a bunch of compile-time constants.
// This function sets them directly to allow the compiler to propagate them and optimize the code.
EnvLightData InitSkyEnvLightData(int envIndex)
{
    EnvLightData output;
    ZERO_INITIALIZE(EnvLightData, output);
    output.influenceShapeType = ENVSHAPETYPE_SKY;
    // 31 bit index, 1 bit cache type
    output.envIndex = ENVCACHETYPE_CUBEMAP | (envIndex << 1);

    output.influenceForward = float3(0.0, 0.0, 1.0);
    output.influenceUp = float3(0.0, 1.0, 0.0);
    output.influenceRight = float3(1.0, 0.0, 0.0);
    output.influencePositionRWS = float3(0.0, 0.0, 0.0);

    output.weight = 1.0;
    output.multiplier = 1.0;

    // proxy
    output.proxyForward = float3(0.0, 0.0, 1.0);
    output.proxyUp = float3(0.0, 1.0, 0.0);
    output.proxyRight = float3(1.0, 0.0, 0.0);
    output.minProjectionDistance = 65504.0f;

    return output;
}

bool IsEnvIndexCubemap(int index)   { return (index & 1) == ENVCACHETYPE_CUBEMAP; }
bool IsEnvIndexTexture2D(int index) { return (index & 1) == ENVCACHETYPE_TEXTURE2D; }

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
// Cubemap      : texCoord = direction vector
// Texture2D    : texCoord = projectedPositionWS - lightData.capturePosition
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod)
{
    // 31 bit index, 1 bit cache type
    uint cacheType = index & 1;
    index = index >> 1;

    float4 color = float4(0.0, 0.0, 0.0, 1.0);

    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        if (cacheType == ENVCACHETYPE_TEXTURE2D)
        {
            //_Env2DCaptureVP is in capture space
            float3 ndc = ComputeNormalizedDeviceCoordinatesWithZ(texCoord, _Env2DCaptureVP[index]);

            color.rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_Env2DTextures, s_trilinear_clamp_sampler, ndc.xy, index, lod).rgb;
            color.a = any(ndc.xyz < 0) || any(ndc.xyz > 1) ? 0.0 : 1.0;
        }
        else if (cacheType == ENVCACHETYPE_CUBEMAP)
        {
            color.rgb = SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_EnvCubemapTextures, s_trilinear_clamp_sampler, texCoord, index, lod).rgb;
        }
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        color.rgb = SampleSkyTexture(texCoord, lod).rgb;
    }

    return color;
}

//-----------------------------------------------------------------------------
// Single Pass and Tile Pass
// ----------------------------------------------------------------------------

#ifdef LIGHTLOOP_TILE_PASS

// Calculate the offset in global light index light for current light category
int GetTileOffset(PositionInputs posInput, uint lightCategory)
{
    uint2 tileIndex = posInput.tileCoord;
    return (tileIndex.y + lightCategory * _NumTileFtplY) * _NumTileFtplX + tileIndex.x;
}

void GetCountAndStartTile(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    const int tileOffset = GetTileOffset(posInput, lightCategory);

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

uint FetchIndex(uint tileOffset, uint lightIndex)
{
    const uint lightIndexPlusOne = lightIndex + 1; // Add +1 as first slot is reserved to store number of light
    // Light index are store on 16bit
    return (g_vLightListGlobal[DWORD_PER_TILE * tileOffset + (lightIndexPlusOne >> 1)] >> ((lightIndexPlusOne & 1) * DWORD_PER_TILE)) & 0xffff;
}

#elif defined(USE_CLUSTERED_LIGHTLIST)

#include "ClusteredUtils.hlsl"

uint GetTileSize()
{
    return TILE_SIZE_CLUSTERED;
}

float GetLightClusterMinLinearDepth(uint2 tileIndex, uint clusterIndex)
{
    float logBase = g_fClustBase;
    if (g_isLogBaseBufferEnabled)
    {
        // XRTODO: Stereo-ize access to g_logBaseBuffer
        logBase = g_logBaseBuffer[tileIndex.y * _NumTileClusteredX + tileIndex.x];
    }

    return ClusterIdxToZFlex(clusterIndex, logBase, g_isLogBaseBufferEnabled != 0);
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

uint FetchIndex(uint tileOffset, uint lightIndex)
{
    return g_vLightListGlobal[tileOffset + lightIndex];
}

#endif // USE_FPTL_LIGHTLIST

#else

uint GetTileSize()
{
    return 1;
}

uint FetchIndex(uint globalOffset, uint lightIndex)
{
    return globalOffset + lightIndex;
}

#endif // LIGHTLOOP_TILE_PASS

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
    int j = FetchIndex(start, i);

    return _LightDatas[j];
}

EnvLightData FetchEnvLight(uint start, uint i)
{
    int j = FetchIndex(start, i);

    return _EnvLightDatas[j];
}

// We always fetch the screen space shadow texture to reduce the number of shader variant, overhead is negligible,
// it is a 1x1 white texture if deferred directional shadow and/or contact shadow are disabled
// We perform a single featch a the beginning of the lightloop
float InitContactShadow(PositionInputs posInput)
{
    // For now we only support one contact shadow
    // Contactshadow is store in Green Channel of _DeferredShadowTexture
    return LOAD_TEXTURE2D(_DeferredShadowTexture, posInput.positionSS).y;
}

float GetContactShadow(LightLoopContext lightLoopContext, int contactShadowIndex)
{
    return contactShadowIndex >= 0 ? lightLoopContext.contactShadow : 1.0;
}
