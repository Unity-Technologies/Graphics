//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

// Calculate the offset in global light index light for current light category
int GetTileOffset(Coordinate coord, uint lightCategory)
{
    uint2 tileIndex = coord.unPositionSS / TILE_SIZE;
    return (tileIndex.y + lightCategory * _NumTileY) * _NumTileX + tileIndex.x;
}

void GetCountAndStartOpaque(Coordinate coord, uint lightCategory, float linearDepth, out uint start, out uint lightCount)
{
    const int tileOffset = GetTileOffset(coord, lightCategory);

    // The first entry inside a tile is the number of light for lightCategory (thus the +0)
    lightCount = g_vLightListGlobal[DWORD_PER_TILE * tileOffset + 0] & 0xffff;
    start = tileOffset;
}

uint FetchIndexOpaque(uint tileOffset, uint lightIndex)
{
    const uint lightIndexPlusOne = lightIndex + 1; // Add +1 as first slot is reserved to store number of light
    // Light index are store on 16bit
    return (g_vLightListGlobal[DWORD_PER_TILE * tileOffset + (lightIndexPlusOne >> 1)] >> ((lightIndexPlusOne & 1) * DWORD_PER_TILE)) & 0xffff;
}

#ifdef USE_FPTL_LIGHTLIST

void GetCountAndStart(Coordinate coord, uint lightCategory, float linearDepth, out uint start, out uint lightCount)
{
    GetCountAndStartOpaque(coord, lightCategory, linearDepth, start, lightCount);
}

uint FetchIndex(uint tileOffset, uint lightIndex)
{
    return FetchIndexOpaque(tileOffset, lightIndex);
}

#else

#include "ClusteredUtils.hlsl"

void GetCountAndStart(Coordinate coord, uint lightCategory, float linearDepth, out uint start, out uint lightCount)
{
    if(g_isOpaquesOnlyEnabled)
    {
        GetCountAndStartOpaque(coord, lightCategory, linearDepth, start, lightCount);
    }
    else
    {
        uint2 tileIndex = coord.unPositionSS / TILE_SIZE;

        float logBase = g_fClustBase;
        if (g_isLogBaseBufferEnabled)
        {
            logBase = g_logBaseBuffer[tileIndex.y * _NumTileX + tileIndex.x];
        }

        int clustIdx = SnapToClusterIdxFlex(linearDepth, logBase, g_isLogBaseBufferEnabled != 0);

        int nrClusters = (1 << g_iLog2NumClusters);
        const int idx = ((model * nrClusters + clustIdx) * _NumTileY + tileIndex.y) * _NumTileX + tileIndex.x;
        uint dataPair = g_vLayeredOffsetsBuffer[idx];
        start = dataPair & 0x7ffffff;
        lightCount = (dataPair >> 27) & 31;
    }
}

uint FetchIndex(uint tileOffset, uint lightIndex)
{
    if(g_isOpaquesOnlyEnabled)
        return FetchIndexOpaque(tileOffset, lightIndex);
    else
        return g_vLightListGlobal[tileOffset + lightIndex];
}

float GetLinearDepth(float zDptBufSpace)    // 0 is near 1 is far
{
    // todo (simplify): m22 is zero and m23 is +1/-1 (depends on left/right hand proj)
    float m22 = g_mInvScrProjection[2].z, m23 = g_mInvScrProjection[2].w;
    float m32 = g_mInvScrProjection[3].z, m33 = g_mInvScrProjection[3].w;

    return (m22 * zDptBufSpace + m23) / (m32 * zDptBufSpace + m33);

    //float3 vP = float3(0.0f,0.0f,zDptBufSpace);
    //float4 v4Pres = mul(g_mInvScrProjection, float4(vP,1.0));
    //return v4Pres.z / v4Pres.w;
}

#endif

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop( float3 V, float3 positionWS, Coordinate coord, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
#if USE_CLUSTERED_LIGHTLIST
    // TODO: Think more about the design, it is ok to do that ? hope the compiler could optimize it out as we do it before LightLoop call, else need to pass it as argument...
    float depth = LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).x;
    float linearDepth = GetLinearDepth(depth); // View space linear depth
#else
    float linearDepth = 0.0; // unsued
#endif

    LightLoopContext context;
    ZERO_INITIALIZE(LightLoopContext, context);

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

#ifdef PROCESS_DIRECTIONAL_LIGHT
    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Directional(   context, V, positionWS, prelightData, _DirectionalLightList[i], bsdfData,
                                    localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }
#endif

#ifdef PROCESS_PUNCTUAL_LIGHT
    uint punctualLightStart;
    uint punctualLightCount;
    GetCountAndStart(coord, DIRECT_LIGHT, linearDepth, punctualLightStart, punctualLightCount);
    for (i = 0; i < punctualLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Punctual(  context, V, positionWS, prelightData, _PunctualLightList[FetchIndex(punctualLightStart, i)], bsdfData,
                                localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }
#endif

#ifdef PROCESS_AREA_LIGHT
    /*
    // TODO: Area lights are where the sorting is important (Morten approach with while loop)
    uint areaLightStart;
    uint areaLightCount;
    GetCountAndStart(coord, LightCatergory.AreaLight, linearDepth, areaLightStart, areaLightCount);
    for (i = 0; i < areaLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Area(  context, V, positionWS, prelightData, _AreaLightList[FetchIndex(areaLightStart, i)], bsdfData,
                            localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }
    */
#endif

#ifdef PROCESS_ENV_LIGHT
    uint envLightStart;
    uint envLightCount;
    GetCountAndStart(coord, REFLECTION_LIGHT, linearDepth, envLightStart, envLightCount);

    float3 iblDiffuseLighting = float3(0.0, 0.0, 0.0);
    float3 iblSpecularLighting = float3(0.0, 0.0, 0.0);

    for (i = 0; i < envLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
        EvaluateBSDF_Env(context, V, positionWS, prelightData, _EnvLightList[FetchIndex(envLightStart, i)], bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
    }

    diffuseLighting += iblDiffuseLighting;
    specularLighting += iblSpecularLighting;
#endif

    // Currently do lightmap with indirect specula
    // TODO: test what is the most appropriate here...
#ifdef PROCESS_ENV_LIGHT
    // Add indirect diffuse + emissive (if any)
    diffuseLighting += bakeDiffuseLighting;
#endif
}
