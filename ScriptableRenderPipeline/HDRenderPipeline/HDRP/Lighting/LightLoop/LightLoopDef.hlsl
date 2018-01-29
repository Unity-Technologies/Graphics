#include "LightLoop.cs.hlsl"
#include "../../Sky/SkyVariables.hlsl"

StructuredBuffer<uint> g_vLightListGlobal;      // don't support Buffer yet in unity

#define DWORD_PER_TILE 16 // See dwordsPerTile in LightLoop.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)
#define MAX_ENV2D_LIGHT 32

CBUFFER_START(UnityTilePass)
uint _NumTileFtplX;
uint _NumTileFtplY;

// these uniforms are only needed for when OPAQUES_ONLY is NOT defined
// but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
//#ifdef USE_CLUSTERED_LIGHTLIST
float4x4 g_mInvScrProjection;

float g_fClustScale;
float g_fClustBase;
float g_fNearPlane;
float g_fFarPlane;
int g_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible

uint g_isLogBaseBufferEnabled;
//#endif

//#ifdef USE_CLUSTERED_LIGHTLIST
uint _NumTileClusteredX;
uint _NumTileClusteredY;
CBUFFER_END

StructuredBuffer<uint> g_vLayeredOffsetsBuffer;     // don't support Buffer yet in unity
StructuredBuffer<float> g_logBaseBuffer;            // don't support Buffer yet in unity
//#endif

#ifdef USE_INDIRECT
    StructuredBuffer<uint> g_TileFeatureFlags;
#endif

StructuredBuffer<DirectionalLightData> _DirectionalLightDatas;
StructuredBuffer<LightData>            _LightDatas;
StructuredBuffer<EnvLightData>         _EnvLightDatas;
StructuredBuffer<ShadowData>           _ShadowDatas;

// Used by directional and spot lights
TEXTURE2D_ARRAY(_CookieTextures);

// Used by point lights
TEXTURECUBE_ARRAY_ABSTRACT(_CookieCubeTextures);

// Use texture array for reflection (or LatLong 2D array for mobile)
TEXTURECUBE_ARRAY_ABSTRACT(_EnvCubemapTextures);
TEXTURE2D_ARRAY(_Env2DTextures);
float4x4 _Env2DCaptureVP[MAX_ENV2D_LIGHT];

TEXTURE2D(_DeferredShadowTexture);

CBUFFER_START(UnityPerLightLoop)
uint _DirectionalLightCount;
uint _PunctualLightCount;
uint _AreaLightCount;
uint _EnvLightCount;
uint _EnvProxyCount;
int  _EnvLightSkyEnabled;         // TODO: make it a bool
CBUFFER_END

// LightLoopContext is not visible from Material (user should not use these properties in Material file)
// It allow the lightloop to have transmit sampling information (do we use atlas, or texture array etc...)
struct LightLoopContext
{
    int sampleReflection;
    ShadowContext shadowContext;
};

//-----------------------------------------------------------------------------
// Cookie sampling functions
// ----------------------------------------------------------------------------

// Used by directional and spot lights.
float3 SampleCookie2D(LightLoopContext lightLoopContext, float2 coord, int index, bool repeat)
{
    if (repeat)
    {
        // TODO: add MIP maps to combat aliasing?
        return SAMPLE_TEXTURE2D_ARRAY_LOD(_CookieTextures, s_linear_repeat_sampler, coord, index, 0).rgb;
    }
    else // clamp
    {
        // TODO: add MIP maps to combat aliasing?
        return SAMPLE_TEXTURE2D_ARRAY_LOD(_CookieTextures, s_linear_clamp_sampler, coord, index, 0).rgb;
    }
}

// Used by point lights.
float3 SampleCookieCube(LightLoopContext lightLoopContext, float3 coord, int index)
{
    // TODO: add MIP maps to combat aliasing?
    return SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_CookieCubeTextures, s_linear_clamp_sampler, coord, index, 0).rgb;
}

//-----------------------------------------------------------------------------
// Reflection probe / Sky sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

#ifdef DEBUG_DISPLAY
float4 ApplyDebugProjectionVolume(float4 color, float3 radiusToProxy, float scale)
{
    float l = length(radiusToProxy);
    l = pow(l / (1 + l), scale);
    return float4(l.xxx * 0.7 + color.rgb * 0.3, color.a);
}
#endif

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
// Cubemap      : texCoord = direction vector
// Texture2D    : texCoord = projectedPositionWS - lightData.capturePosition
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod)
{
    // 31 bit index, 1 bit cache type
    uint cacheType = index & 1;
    index = index >> 1;

    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        if (cacheType == ENVCACHETYPE_TEXTURE2D)
        {
            //_Env2DCaptureVP is in capture space
            float4 ndc = ComputeClipSpaceCoordinates(texCoord, _Env2DCaptureVP[index]);
            ndc *= rcp(ndc.w);
            ndc.xy = ndc.xy * 0.5 + 0.5;

            float4 color = SAMPLE_TEXTURE2D_ARRAY_LOD(_Env2DTextures, s_trilinear_clamp_sampler, ndc.xy, index, 0);
            color.a = any(ndc.xyz < 0) || any(ndc.xyz > 1) ? 0 : 1;
            
            return color;
        }
        else if (cacheType == ENVCACHETYPE_CUBEMAP)
        {
            float4 color = SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_EnvCubemapTextures, s_trilinear_clamp_sampler, texCoord, index, lod);
            color.a = 1;
            return color;
        }
        return float4(0, 0, 0, 0);
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        return SampleSkyTexture(texCoord, lod);
    }
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
        logBase = g_logBaseBuffer[tileIndex.y * _NumTileClusteredX + tileIndex.x];
    }

    return ClusterIdxToZFlex(clusterIndex, logBase, g_isLogBaseBufferEnabled != 0);
}

uint GetLightClusterIndex(uint2 tileIndex, float linearDepth)
{
    float logBase = g_fClustBase;
    if (g_isLogBaseBufferEnabled)
    {
        logBase = g_logBaseBuffer[tileIndex.y * _NumTileClusteredX + tileIndex.x];
    }

    return SnapToClusterIdxFlex(linearDepth, logBase, g_isLogBaseBufferEnabled != 0);
}

void GetCountAndStartCluster(uint2 tileIndex, uint clusterIndex, uint lightCategory, out uint start, out uint lightCount)
{
    int nrClusters = (1 << g_iLog2NumClusters);
    const int idx = ((lightCategory * nrClusters + clusterIndex) * _NumTileClusteredY + tileIndex.y) * _NumTileClusteredX + tileIndex.x;
    uint dataPair = g_vLayeredOffsetsBuffer[idx];
    start = dataPair & 0x7ffffff;
    lightCount = (dataPair >> 27) & 31;
}

void GetCountAndStartCluster(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
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

#endif // LIGHTLOOP_TILE_PASS

LightData FetchLight(uint start, uint i)
{
#ifdef LIGHTLOOP_TILE_PASS
    int j = FetchIndex(start, i);
#else
    int j = start + i;
#endif
    return _LightDatas[j];
}
