#if defined (LIGHTLOOP_TILE_DIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_DIRECTIONAL_LIGHT
#define PROCESS_PUNCTUAL_LIGHT
#define PROCESS_AREA_LIGHT
#define PROCESS_PROJECTOR_LIGHT
#endif

#if defined (LIGHTLOOP_TILE_INDIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_ENV_LIGHT
#endif

#include "TilePass.cs.hlsl"

// For FPTL
uint _NumTileFtplX;
uint _NumTileFtplY;

StructuredBuffer<uint> g_vLightListGlobal;      // don't support Buffer yet in unity

#define DWORD_PER_TILE 16 // See dwordsPerTile in TilePass.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

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
uint _UseTileLightList;
//#endif

//#ifdef USE_CLUSTERED_LIGHTLIST
uint _NumTileClusteredX;
uint _NumTileClusteredY;
StructuredBuffer<uint> g_vLayeredOffsetsBuffer;     // don't support Buffer yet in unity
StructuredBuffer<float> g_logBaseBuffer;            // don't support Buffer yet in unity
//#endif

StructuredBuffer<DirectionalLightData> _DirectionalLightDatas;
StructuredBuffer<LightData>            _LightDatas;
StructuredBuffer<EnvLightData>         _EnvLightDatas;
StructuredBuffer<ShadowData>           _ShadowDatas;

// Use texture array for IES
//TEXTURE2D_ARRAY(_IESArray);
//SAMPLER2D(sampler_IESArray);

// Used by directional and spot lights
TEXTURE2D_ARRAY(_CookieTextures);
SAMPLER2D(sampler_CookieTextures);

// Used by point lights
TEXTURECUBE_ARRAY_ABSTRACT(_CookieCubeTextures);
SAMPLERCUBE_ABSTRACT(sampler_CookieCubeTextures);

// Use texture array for reflection (or LatLong 2D array for mobile)
TEXTURECUBE_ARRAY_ABSTRACT(_EnvTextures);
SAMPLERCUBE_ABSTRACT(sampler_EnvTextures);

TEXTURECUBE(_SkyTexture);
SAMPLERCUBE(sampler_SkyTexture); // NOTE: Sampler could be share here with _EnvTextures. Don't know if the shader compiler will complain...

CBUFFER_START(UnityPerLightLoop)
uint _DirectionalLightCount;
uint _PunctualLightCount;
uint _AreaLightCount;
uint _ProjectorLightCount;
uint _EnvLightCount;
float4 _DirShadowSplitSpheres[4]; // TODO: share this max between C# and hlsl

int  _EnvLightSkyEnabled;         // TODO: make it a bool
CBUFFER_END

struct LightLoopContext
{
    // Visible from Material
    float ambientOcclusion;

    // Not visible from Material (user should not use these properties in Material)
    int sampleShadow;
    int sampleReflection;
    ShadowContext shadowContext;
};

//-----------------------------------------------------------------------------
// Cookie sampling functions
// ----------------------------------------------------------------------------

// Used by directional and spot lights.
// Returns the color in the RGB components, and the transparency (lack of occlusion) in A.
float4 SampleCookie2D(LightLoopContext lightLoopContext, float2 coord, int index)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_CookieTextures, sampler_CookieTextures, coord, index, 0);
}

// Used by point lights.
// Returns the color in the RGB components, and the transparency (lack of occlusion) in A.
float4 SampleCookieCube(LightLoopContext lightLoopContext, float3 coord, int index)
{
    return SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_CookieCubeTextures, sampler_CookieCubeTextures, coord, index, 0);
}

//-----------------------------------------------------------------------------
// IES sampling function
// ----------------------------------------------------------------------------

// sphericalTexCoord is theta and phi spherical coordinate
//float4 SampleIES(LightLoopContext lightLoopContext, int index, float2 sphericalTexCoord, float lod)
//{
//    return SAMPLE_TEXTURE2D_ARRAY_LOD(_IESArray, sampler_IESArray, sphericalTexCoord, index, 0);
//}

//-----------------------------------------------------------------------------
// Reflection proble / Sky sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod)
{
    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        return SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(_EnvTextures, sampler_EnvTextures, texCoord, index, lod);
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, sampler_SkyTexture, texCoord, lod);
    }
}

//-----------------------------------------------------------------------------
// AmbientOcclusion
// ----------------------------------------------------------------------------

TEXTURE2D(_AmbientOcclusionTexture);
