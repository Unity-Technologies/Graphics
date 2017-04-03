//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

void ApplyDebug(LightLoopContext lightLoopContext, float3 positionWS, inout float3 diffuseLighting, inout float3 specularLighting)
{
#ifdef LIGHTING_DEBUG
    int lightDebugMode = (int)_DebugLightModeAndAlbedo.x;

    if (lightDebugMode == LIGHTINGDEBUGMODE_DIFFUSE_LIGHTING)
    {
        specularLighting = float3(0.0, 0.0, 0.0);
    }
    else if (lightDebugMode == LIGHTINGDEBUGMODE_SPECULAR_LIGHTING)
    {
        diffuseLighting = float3(0.0, 0.0, 0.0);
    }
    else if (lightDebugMode == LIGHTINGDEBUGMODE_VISUALIZE_CASCADE)
    {
        specularLighting = float3(0.0, 0.0, 0.0);

        const float3 s_CascadeColors[] = {
            float3(1.0, 0.0, 0.0),
            float3(0.0, 1.0, 0.0),
            float3(0.0, 0.0, 1.0),
            float3(1.0, 1.0, 0.0)
        };

#ifdef SHADOWS_USE_SHADOWCTXT
        float shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, 0, float3(0.0, 0.0, 0.0), float2(0.0, 0.0));
#else
        float shadow = GetDirectionalShadowAttenuation(lightLoopContext, positionWS, 0, float3(0.0, 0.0, 0.0), float2(0.0, 0.0));
#endif

        int shadowSplitIndex = GetSplitSphereIndexForDirshadows(positionWS, _DirShadowSplitSpheres);
        if (shadowSplitIndex == -1)
            diffuseLighting = float3(0.0, 0.0, 0.0);
        else
        {
            diffuseLighting = s_CascadeColors[shadowSplitIndex] * shadow;
        }

    }
#endif
}

#ifdef LIGHTLOOP_TILE_PASS

// Calculate the offset in global light index light for current light category
int GetTileOffset(PositionInputs posInput, uint lightCategory)
{
    uint2 tileIndex = posInput.unTileCoord;
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

void GetCountAndStartCluster(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    uint2 tileIndex = posInput.unTileCoord;

    float logBase = g_fClustBase;
    if (g_isLogBaseBufferEnabled)
    {
        logBase = g_logBaseBuffer[tileIndex.y * _NumTileClusteredX + tileIndex.x];
    }

    int clustIdx = SnapToClusterIdxFlex(posInput.depthVS, logBase, g_isLogBaseBufferEnabled != 0);

    int nrClusters = (1 << g_iLog2NumClusters);
    const int idx = ((lightCategory * nrClusters + clustIdx) * _NumTileClusteredY + tileIndex.y) * _NumTileClusteredX + tileIndex.x;
    uint dataPair = g_vLayeredOffsetsBuffer[idx];
    start = dataPair & 0x7ffffff;
    lightCount = (dataPair >> 27) & 31;
}

void GetCountAndStart(PositionInputs posInput, uint lightCategory, out uint start, out uint lightCount)
{
    if (_UseTileLightList)
        GetCountAndStartTile(posInput, lightCategory, start, lightCount);
    else
        GetCountAndStartCluster(posInput, lightCategory, start, lightCount);
}

uint FetchIndex(uint tileOffset, uint lightIndex)
{
    uint offset = tileOffset + lightIndex;
    const uint lightIndexPlusOne = lightIndex + 1; // Add +1 as first slot is reserved to store number of light

    if (_UseTileLightList)
        offset = DWORD_PER_TILE * tileOffset + (lightIndexPlusOne >> 1);

    // Avoid generated HLSL bytecode to always access g_vLightListGlobal with
    // two different offsets, fixes out of bounds issue
    uint value = g_vLightListGlobal[offset];

    // Light index are store on 16bit
    return (_UseTileLightList ? ((value >> ((lightIndexPlusOne & 1) * DWORD_PER_TILE)) & 0xffff) : value);
}

#endif

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop( float3 V, PositionInputs posInput, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting, uint featureFlags,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
#ifndef SHADOWS_USE_SHADOWCTXT
    ZERO_INITIALIZE(LightLoopContext, context);
#else
    context.sampleShadow = 0;
    context.sampleReflection = 0;
    context.shadowContext = InitShadowContext();
#endif

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

#ifdef PROCESS_DIRECTIONAL_LIGHT
    if(featureFlags & FEATURE_FLAG_DIRECTIONAL_LIGHT)
    {
        for(i = 0; i < _DirectionalLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            EvaluateBSDF_Directional(context, V, posInput, prelightData, _DirectionalLightDatas[i], bsdfData,
                localDiffuseLighting, localSpecularLighting);

            diffuseLighting += localDiffuseLighting;
            specularLighting += localSpecularLighting;
        }
    }
#endif

#ifdef PROCESS_PUNCTUAL_LIGHT
    if(featureFlags & FEATURE_FLAG_PUNCTUAL_LIGHT)
    {
        // TODO: Convert the for loop below to a while on each type as we know we are sorted!
        uint punctualLightStart;
        uint punctualLightCount;
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, punctualLightStart, punctualLightCount);

        for(i = 0; i < punctualLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            EvaluateBSDF_Punctual(context, V, posInput, prelightData, _LightDatas[FetchIndex(punctualLightStart, i)], bsdfData,
                localDiffuseLighting, localSpecularLighting);

            diffuseLighting += localDiffuseLighting;
            specularLighting += localSpecularLighting;
        }
    }
#endif

#ifdef PROCESS_AREA_LIGHT
    if(featureFlags & FEATURE_FLAG_AREA_LIGHT)
    {
        // TODO: Convert the for loop below to a while on each type as we know we are sorted!
        uint areaLightStart;
        uint areaLightCount;
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, areaLightStart, areaLightCount);
        for(i = 0; i < areaLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            uint areaIndex = FetchIndex(areaLightStart, i);

            if(_LightDatas[areaIndex].lightType == GPULIGHTTYPE_LINE)
            {
                EvaluateBSDF_Line(context, V, posInput, prelightData, _LightDatas[areaIndex], bsdfData,
                    localDiffuseLighting, localSpecularLighting);
            }
            else
            {
                EvaluateBSDF_Area(context, V, posInput, prelightData, _LightDatas[areaIndex], bsdfData,
                    localDiffuseLighting, localSpecularLighting);
            }


            diffuseLighting += localDiffuseLighting;
            specularLighting += localSpecularLighting;
        }
    }
#endif

#ifdef PROCESS_ENV_LIGHT
    float3 iblDiffuseLighting = float3(0.0, 0.0, 0.0);
    float3 iblSpecularLighting = float3(0.0, 0.0, 0.0);

    // Only apply sky IBL if the sky texture is available.
    if(featureFlags & FEATURE_FLAG_SKY_LIGHT)
    {
        if(_EnvLightSkyEnabled)
        {
            float3 localDiffuseLighting, localSpecularLighting;
            float2 weight;
            // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
            EnvLightData envLightSky = InitSkyEnvLightData(0); // The sky data are generated on the fly so the compiler can optimize the code
            EvaluateBSDF_Env(context, V, posInput, prelightData, envLightSky, bsdfData, localDiffuseLighting, localSpecularLighting, weight);
            iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x); // Should be remove by the compiler if it is smart as all is constant 0
            iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
        }
    }


    if(featureFlags & FEATURE_FLAG_ENV_LIGHT)
    {
        uint envLightStart;
        uint envLightCount;
        GetCountAndStart(posInput, LIGHTCATEGORY_ENV, envLightStart, envLightCount);

        for(i = 0; i < envLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;
            float2 weight;
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
            EvaluateBSDF_Env(context, V, posInput, prelightData, _EnvLightDatas[FetchIndex(envLightStart, i)], bsdfData, localDiffuseLighting, localSpecularLighting, weight);
            iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x); // Should be remove by the compiler if it is smart as all is constant 0
            iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
        }
    }


    diffuseLighting += iblDiffuseLighting;
    specularLighting += iblSpecularLighting;
#endif

    // TODO: currently apply GI at the same time as reflection
#ifdef PROCESS_ENV_LIGHT
    // Add indirect diffuse + emissive (if any)
    diffuseLighting += bakeDiffuseLighting;
#endif

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}

#else // LIGHTLOOP_SINGLE_PASS

uint GetTileSize()
{
    return 1;
}


// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop( float3 V, PositionInputs posInput, PreLightData prelightData, BSDFData bsdfData, float3 bakeDiffuseLighting, uint featureFlag,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
#ifndef SHADOWS_USE_SHADOWCTXT
    ZERO_INITIALIZE(LightLoopContext, context);
#else
    context.sampleShadow = 0;
    context.sampleReflection = 0;
    context.shadowContext = InitShadowContext();
#endif

    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Directional(   context, V, posInput, prelightData, _DirectionalLightDatas[i], bsdfData,
                                    localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    for (i = 0; i < _PunctualLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        EvaluateBSDF_Punctual(  context, V, posInput, prelightData, _LightDatas[i], bsdfData,
                                localDiffuseLighting, localSpecularLighting);

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    // Area are store with punctual, just offset the index
    for (i = _PunctualLightCount; i < _AreaLightCount + _PunctualLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;

        if (_LightDatas[i].lightType == GPULIGHTTYPE_LINE)
        {
            EvaluateBSDF_Line(  context, V, posInput, prelightData, _LightDatas[i], bsdfData,
                                localDiffuseLighting, localSpecularLighting);
        }
        else
        {
            EvaluateBSDF_Area(  context, V, posInput, prelightData, _LightDatas[i], bsdfData,
                                localDiffuseLighting, localSpecularLighting);
        }

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    // TODO: Check the reflection hierarchy, for the current system (matching legacy unity) we must sort from bigger solid angle to lower (lower override bigger). So begging by sky
    // TODO: Change the way it is done by reversing the order, from smaller solid angle to bigger, so we can early out when the weight is 1.
    float3 iblDiffuseLighting = float3(0.0, 0.0, 0.0);
    float3 iblSpecularLighting = float3(0.0, 0.0, 0.0);

    // Only apply sky IBL if the sky texture is available.
    if (_EnvLightSkyEnabled)
    {
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
        EnvLightData envLightSky = InitSkyEnvLightData(0); // The sky data are generated on the fly so the compiler can optimize the code
        EvaluateBSDF_Env(context, V, posInput, prelightData, envLightSky, bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
    }

    for (i = 0; i < _EnvLightCount; ++i)
    {
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
        EvaluateBSDF_Env(context, V, posInput, prelightData, _EnvLightDatas[i], bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x); // Should be remove by the compiler if it is smart as all is constant 0
        iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
    }

    diffuseLighting += iblDiffuseLighting;
    specularLighting += iblSpecularLighting;

    // Add indirect diffuse + emissive (if any)
    diffuseLighting += bakeDiffuseLighting;

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}

#endif
