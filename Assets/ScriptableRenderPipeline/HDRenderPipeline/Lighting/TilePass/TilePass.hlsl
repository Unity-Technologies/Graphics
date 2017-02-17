#if defined (LIGHTLOOP_TILE_DIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_DIRECTIONAL_LIGHT
#define PROCESS_PUNCTUAL_LIGHT
#define PROCESS_AREA_LIGHT
#endif

#if defined (LIGHTLOOP_TILE_INDIRECT) || defined(LIGHTLOOP_TILE_ALL)
#define PROCESS_ENV_LIGHT
#endif

#include "TilePass.cs.hlsl"

uint _NumTileX;
uint _NumTileY;

StructuredBuffer<uint> g_vLightListGlobal;		// don't support Buffer yet in unity

#define TILE_SIZE 16 // This is fixed
#define DWORD_PER_TILE 16 // See dwordsPerTile in TilePass.cs, we have roomm for 31 lights and a number of light value all store on 16 bit (ushort)

// these uniforms are only needed for when OPAQUES_ONLY is NOT defined
// but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
//#ifdef USE_CLUSTERED_LIGHTLIST
float4x4 g_mInvScrProjection;

float g_fClustScale;
float g_fClustBase;
float g_fNearPlane;
float g_fFarPlane;
int g_iLog2NumClusters;	// We need to always define these to keep constant buffer layouts compatible

uint g_isLogBaseBufferEnabled;
uint _UseTileLightList;
//#endif

//#ifdef USE_CLUSTERED_LIGHTLIST
StructuredBuffer<uint> g_vLayeredOffsetsBuffer;		// don't support Buffer yet in unity
StructuredBuffer<float> g_logBaseBuffer;			// don't support Buffer yet in unity
//#endif

StructuredBuffer<DirectionalLightData>  _DirectionalLightDatas;
StructuredBuffer<LightData>             _LightDatas;
StructuredBuffer<EnvLightData>          _EnvLightDatas;
StructuredBuffer<ShadowData>            _ShadowDatas;

// Use texture atlas for shadow map
//TEXTURE2D(_ShadowAtlas);
//SAMPLER2D_SHADOW(sampler_ShadowAtlas);
//SAMPLER2D(sampler_ManualShadowAtlas); // TODO: settings sampler individually is not supported in shader yet...
TEXTURE2D(g_tShadowBuffer); // TODO: No choice, the name is hardcoded in ShadowrenderPass.cs for now. Need to change this!
SAMPLER2D_SHADOW(samplerg_tShadowBuffer);

// Use texture array for IES
//TEXTURE2D_ARRAY(_IESArray);
//SAMPLER2D(sampler_IESArray);

// Used by directional and spot lights
TEXTURE2D_ARRAY(_CookieTextures);
SAMPLER2D(sampler_CookieTextures);

// Used by point lights
TEXTURECUBE_ARRAY(_CookieCubeTextures);
SAMPLERCUBE(sampler_CookieCubeTextures);

// Use texture array for reflection (or LatLong 2D array for mobile)
#ifdef CUBE_ARRAY_NOT_SUPPORTED
TEXTURE2D_ARRAY(_EnvTextures);
SAMPLER2D(sampler_EnvTextures);
#else
TEXTURECUBE_ARRAY(_EnvTextures);
SAMPLERCUBE(sampler_EnvTextures);
#endif

TEXTURECUBE(_SkyTexture);
SAMPLERCUBE(sampler_SkyTexture); // NOTE: Sampler could be share here with _EnvTextures. Don't know if the shader compiler will complain...

CBUFFER_START(UnityPerLightLoop)
uint _DirectionalLightCount;
uint _PunctualLightCount;
uint _AreaLightCount;
uint _EnvLightCount;
float4 _DirShadowSplitSpheres[4]; // TODO: share this max between C# and hlsl

int  _EnvLightSkyEnabled;         // TODO: make it a bool	
CBUFFER_END

struct LightLoopContext
{
    int sampleShadow;
    int sampleReflection;
#ifdef SHADOWS_USE_SHADOWCTXT
	ShadowContext shadowContext;
#endif
};

#ifndef SHADOWS_USE_SHADOWCTXT

//-----------------------------------------------------------------------------
// Shadow sampling function
// ----------------------------------------------------------------------------

float GetPunctualShadowAttenuation(LightLoopContext lightLoopContext, uint lightType, float3 positionWS, int index, float3 L, float2 unPositionSS)
{
    int faceIndex = 0;
    if (lightType == GPULIGHTTYPE_POINT)
    {
        GetCubeFaceID(L, faceIndex);
    }

    ShadowData shadowData = _ShadowDatas[index + faceIndex];

    // Note: scale and bias of shadow atlas are included in ShadowTransform but could be apply here.
    float4 positionTXS = mul(float4(positionWS, 1.0), shadowData.worldToShadow);
    positionTXS.xyz /= positionTXS.w;
    // positionTXS.z -=  shadowData.bias;
    positionTXS.z -= 0.001; // Apply a linear bias

#if UNITY_REVERSED_Z
    positionTXS.z = 1.0 - positionTXS.z;
#endif

    // float3 shadowPosDX = ddx_fine(positionTXS);
    // float3 shadowPosDY = ddy_fine(positionTXS);

    return SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, positionTXS);
}

// Gets the cascade weights based on the world position of the fragment and the positions of the split spheres for each cascade.
// Returns an invalid split index if past shadowDistance (ie 4 is invalid for cascade)
int GetSplitSphereIndexForDirshadows(float3 positionWS, float4 dirShadowSplitSpheres[4])
{
    float3 fromCenter0 = positionWS.xyz - dirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = positionWS.xyz - dirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = positionWS.xyz - dirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = positionWS.xyz - dirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 dirShadowSplitSphereSqRadii;
    dirShadowSplitSphereSqRadii.x = dirShadowSplitSpheres[0].w;
    dirShadowSplitSphereSqRadii.y = dirShadowSplitSpheres[1].w;
    dirShadowSplitSphereSqRadii.z = dirShadowSplitSpheres[2].w;
    dirShadowSplitSphereSqRadii.w = dirShadowSplitSpheres[3].w;

    if (distances2.w > dirShadowSplitSphereSqRadii.w)
        return -1;

    float4 weights = float4(distances2 < dirShadowSplitSphereSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return int(4.0 - dot(weights, float4(4.0, 3.0, 2.0, 1.0)));
}


float GetDirectionalShadowAttenuation(LightLoopContext lightLoopContext, float3 positionWS, int index, float3 L, float2 unPositionSS)
{
    // Note Index is 0 for now, but else we need to provide the correct index in _DirShadowSplitSpheres and _ShadowDatas
    int shadowSplitIndex = GetSplitSphereIndexForDirshadows(positionWS, _DirShadowSplitSpheres);
    if (shadowSplitIndex == -1)
        return 1.0;

    ShadowData shadowData = _ShadowDatas[shadowSplitIndex];

    // Note: scale and bias of shadow atlas are included in ShadowTransform but could be apply here.
    float4 positionTXS = mul(float4(positionWS, 1.0), shadowData.worldToShadow);
    positionTXS.xyz /= positionTXS.w;
    // positionTXS.z -=  shadowData.bias;
    positionTXS.z -= 0.003; // Apply a linear bias

#if UNITY_REVERSED_Z
    positionTXS.z = 1.0 - positionTXS.z;
#endif

    float4 vShadow3x3PCFTerms0;
    float4 vShadow3x3PCFTerms1;
    float4 vShadow3x3PCFTerms2;
    float4 vShadow3x3PCFTerms3;

    float flTexelEpsilonX = shadowData.invResolution.x;
    float flTexelEpsilonY = shadowData.invResolution.y;
    vShadow3x3PCFTerms0 = float4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
    vShadow3x3PCFTerms1 = float4(flTexelEpsilonX, flTexelEpsilonY, -flTexelEpsilonX, -flTexelEpsilonY);
    vShadow3x3PCFTerms2 = float4(flTexelEpsilonX, flTexelEpsilonY, 0.0f, 0.0f);
    vShadow3x3PCFTerms3 = float4(-flTexelEpsilonX, -flTexelEpsilonY, 0.0f, 0.0f);

    // float3 shadowPosDX = ddx_fine(positionTXS);
    // float3 shadowPosDY = ddy_fine(positionTXS);

    //return SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, positionTXS);

    float4 v20Taps;
    v20Taps.x = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms1.xy, positionTXS.z)).x; //  1  1
    v20Taps.y = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms1.zy, positionTXS.z)).x; // -1  1
    v20Taps.z = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms1.xw, positionTXS.z)).x; //  1 -1
    v20Taps.w = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms1.zw, positionTXS.z)).x; // -1 -1
    float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
    if ((flSum == 0.0) || (flSum == 1.0))
        return flSum;
    flSum *= vShadow3x3PCFTerms0.x * 4.0;

    float4 v33Taps;
    v33Taps.x = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms2.xz, positionTXS.z)).x; //  1  0
    v33Taps.y = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms3.xz, positionTXS.z)).x; // -1  0
    v33Taps.z = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms3.zy, positionTXS.z)).x; //  0 -1
    v33Taps.w = SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, float3(positionTXS.xy + vShadow3x3PCFTerms2.zy, positionTXS.z)).x; //  0  1
    flSum += dot(v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy);

    flSum += SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, positionTXS).x * vShadow3x3PCFTerms0.z;

    return flSum;

}
#endif

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
    return SAMPLE_TEXTURECUBE_ARRAY_LOD(_CookieCubeTextures, sampler_CookieCubeTextures, coord, index, 0);
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
        #ifdef CUBE_ARRAY_NOT_SUPPORTED
        return SAMPLE_TEXTURE2D_ARRAY_LOD(_EnvTextures, sampler_EnvTextures, DirectionToLatLongCoordinate(texCoord), index, lod);
        #else
        return SAMPLE_TEXTURECUBE_ARRAY_LOD(_EnvTextures, sampler_EnvTextures, texCoord, index, lod);
        #endif
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, sampler_SkyTexture, texCoord, lod);
    }
}

