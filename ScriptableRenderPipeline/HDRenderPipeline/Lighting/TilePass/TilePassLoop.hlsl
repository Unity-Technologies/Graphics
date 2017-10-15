//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

void ApplyDebug(LightLoopContext lightLoopContext, float3 positionWS, inout float3 diffuseLighting, inout float3 specularLighting)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        diffuseLighting = float3(0.0, 0.0, 0.0); // Disable diffuse lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_VISUALIZE_CASCADE)
    {
        specularLighting = float3(0.0, 0.0, 0.0);

        const float3 s_CascadeColors[] = {
            float3(1.0, 0.0, 0.0),
            float3(0.0, 1.0, 0.0),
            float3(0.0, 0.0, 1.0),
            float3(1.0, 1.0, 0.0)
        };

        float shadow = GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, positionWS, float3(0.0, 1.0, 0.0 ), 0, float3(0.0, 0.0, 0.0), float2(0.0, 0.0));
        float4 dirShadowSplitSpheres[4];
        uint payloadOffset = EvalShadow_LoadSplitSpheres(lightLoopContext.shadowContext, 0, dirShadowSplitSpheres);
        int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows(positionWS, dirShadowSplitSpheres);

        if (shadowSplitIndex == -1)
        {
            diffuseLighting = float3(0.0, 0.0, 0.0);
        }
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
    if (_UseTileLightList)
        return TILE_SIZE_FPTL;
    else
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

#endif // USE_FPTL_LIGHTLIST

#else

uint GetTileSize()
{
    return 1;
}

#endif // LIGHTLOOP_TILE_PASS

void applyWeigthedIblLighting(float3 localDiffuseLighting, float3 localSpecularLighting, float2 weight, inout float3 iblDiffuseLighting, inout float3 iblSpecularLighting, inout float totalIblWeight)
{
    // IBL weights should not exceed 1.
    float accumulatedWeight = totalIblWeight + weight.y;
    totalIblWeight = saturate(accumulatedWeight);
    weight.y -= saturate(accumulatedWeight - totalIblWeight);

    iblDiffuseLighting = lerp(iblDiffuseLighting, localDiffuseLighting, weight.x);
    iblSpecularLighting = lerp(iblSpecularLighting, localSpecularLighting, weight.y);
}

// bakeDiffuseLighting is part of the prototype so a user is able to implement a "base pass" with GI and multipass direct light (aka old unity rendering path)
void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, float3 bakeDiffuseLighting, uint featureFlags,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
    context.sampleReflection = 0;
    context.shadowContext = InitShadowContext();

    // This struct is use to store accumulated lighting, summation is done in PostEvaluateBSDF
    LightLoopAccumulatedLighting accLighting;
    ZERO_INITIALIZE(LightLoopAccumulatedLighting, accLighting);

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        for (i = 0; i < _DirectionalLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            EvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData,
                                     localDiffuseLighting, localSpecularLighting);

            accLighting.dirDiffuseLighting += localDiffuseLighting;
            accLighting.dirSpecularLighting += localSpecularLighting;
        }
    }

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        #ifdef LIGHTLOOP_TILE_PASS

        // TODO: Convert the for loop below to a while on each type as we know we are sorted and compare performance.
        uint punctualLightStart;
        uint punctualLightCount;
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, punctualLightStart, punctualLightCount);

        for (i = 0; i < punctualLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;
            int punctualIndex = FetchIndex(punctualLightStart, i);

            EvaluateBSDF_Punctual(  context, V, posInput, preLightData, _LightDatas[punctualIndex], bsdfData, _LightDatas[punctualIndex].lightType,
                                    localDiffuseLighting, localSpecularLighting);

            accLighting.punctualDiffuseLighting += localDiffuseLighting;
            accLighting.punctualSpecularLighting += localSpecularLighting;
        }

        #else

        for (i = 0; i < _PunctualLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            EvaluateBSDF_Punctual(  context, V, posInput, preLightData, _LightDatas[i], bsdfData, _LightDatas[i].lightType,
                                    localDiffuseLighting, localSpecularLighting);

            accLighting.punctualDiffuseLighting += localDiffuseLighting;
            accLighting.punctualSpecularLighting += localSpecularLighting;
        }

        #endif
    }

    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        #ifdef LIGHTLOOP_TILE_PASS

        uint areaLightStart;
        uint areaLightCount;
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, areaLightStart, areaLightCount);

        // COMPILER BEHAVIOR WARNING!
        // If rectangle lights are before line lights, the compiler will duplicate light matrices in VGPR because they are used differently between the two types of lights.
        // By keeping line lights first we avoid this behavior and save substantial register pressure.
        // TODO: This is based on the current Lit.shader and can be different for any other way of implementing area lights, how to be generic and ensure performance ?

        i = 0;
        if (areaLightCount > 0)
        {
            uint areaIndex = FetchIndex(areaLightStart, 0);
            uint lightType = _LightDatas[areaIndex].lightType;

            while (i < areaLightCount && lightType == GPULIGHTTYPE_LINE)
            {
                float3 localDiffuseLighting, localSpecularLighting;

                EvaluateBSDF_Area(  context, V, posInput, preLightData, _LightDatas[areaIndex], bsdfData, GPULIGHTTYPE_LINE,
                                    localDiffuseLighting, localSpecularLighting);

                accLighting.areaDiffuseLighting += localDiffuseLighting;
                accLighting.areaSpecularLighting += localSpecularLighting;

                i++;
                areaIndex = i < areaLightCount ? FetchIndex(areaLightStart, i) : 0;
                lightType = i < areaLightCount ? _LightDatas[areaIndex].lightType : 0xFF;
            }

            while (i < areaLightCount && lightType == GPULIGHTTYPE_RECTANGLE)
            {
                float3 localDiffuseLighting, localSpecularLighting;

                EvaluateBSDF_Area(  context, V, posInput, preLightData, _LightDatas[areaIndex], bsdfData, GPULIGHTTYPE_RECTANGLE,
                                    localDiffuseLighting, localSpecularLighting);

                accLighting.areaDiffuseLighting += localDiffuseLighting;
                accLighting.areaSpecularLighting += localSpecularLighting;

                i++;
                areaIndex = i < areaLightCount ? FetchIndex(areaLightStart, i) : 0;
                lightType = i < areaLightCount ? _LightDatas[areaIndex].lightType : 0xFF;
            }
        }

        #else
        for (i = _PunctualLightCount; i < _PunctualLightCount + _AreaLightCount; ++i)
        {
            float3 localDiffuseLighting, localSpecularLighting;

            EvaluateBSDF_Area(  context, V, posInput, preLightData, _LightDatas[i], bsdfData, _LightDatas[i].lightType,
                                localDiffuseLighting, localSpecularLighting);

            accLighting.areaDiffuseLighting += localDiffuseLighting;
            accLighting.areaSpecularLighting += localSpecularLighting;
        }

        #endif
    }

    float  totalIblWeight = 0.0; // Max: 1

    if (featureFlags & LIGHTFEATUREFLAGS_SSL)
    {
        // SSR and rough refraction
        float3 localDiffuseLighting, localSpecularLighting;
        float2 weight;
        EvaluateBSDF_SSL(V, posInput, bsdfData, localDiffuseLighting, localSpecularLighting, weight);
        applyWeigthedIblLighting(localDiffuseLighting, localSpecularLighting, weight, accLighting.envDiffuseLighting, accLighting.envSpecularLighting, totalIblWeight);
        accLighting.envDiffuseLightingWeight = weight.x;
    }

    if (featureFlags & LIGHTFEATUREFLAGS_ENV || featureFlags & LIGHTFEATUREFLAGS_SKY)
    {
        // Reflection probes are sorted by volume (in the increasing order).
        if (featureFlags & LIGHTFEATUREFLAGS_ENV)
        {
            float3 localDiffuseLighting, localSpecularLighting;
            float2 weight;
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;

        #ifdef LIGHTLOOP_TILE_PASS
            uint envLightStart;
            uint envLightCount;
            GetCountAndStart(posInput, LIGHTCATEGORY_ENV, envLightStart, envLightCount);
        #else
            uint envLightCount = _EnvLightCount;
        #endif

            // Note: In case of IBL we are sorted from smaller to bigger projected solid angle bounds. We are not sorted by type so we can't do a 'while' approach like for area light.
            for (i = 0; i < envLightCount && totalIblWeight < 1.0; ++i)
            {
            #ifdef LIGHTLOOP_TILE_PASS
                uint envLightIndex = FetchIndex(envLightStart, i);
            #else
                uint envLightIndex = i;
            #endif
                EvaluateBSDF_Env(context, V, posInput, preLightData, _EnvLightDatas[envLightIndex], bsdfData, _EnvLightDatas[envLightIndex].envShapeType, localDiffuseLighting, localSpecularLighting, weight);
                applyWeigthedIblLighting(localDiffuseLighting, localSpecularLighting, weight, accLighting.envDiffuseLighting, accLighting.envSpecularLighting, totalIblWeight);
            }
        }

        if (featureFlags & LIGHTFEATUREFLAGS_SKY)
        {
            // Only apply the sky IBL if the sky texture is available, and if we haven't yet accumulated enough IBL lighting.
            if (_EnvLightSkyEnabled && totalIblWeight < 1.0)
            {
                float3 localDiffuseLighting, localSpecularLighting;
                float2 weight;

                // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
                context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
                EnvLightData envLightSky = InitSkyEnvLightData(0); // The sky data are generated on the fly so the compiler can optimize the code
                EvaluateBSDF_Env(context, V, posInput, preLightData, envLightSky, bsdfData, ENVSHAPETYPE_SKY, localDiffuseLighting, localSpecularLighting, weight);
                applyWeigthedIblLighting(localDiffuseLighting, localSpecularLighting, weight, accLighting.envDiffuseLighting, accLighting.envSpecularLighting, totalIblWeight);
            }
        }
    }

    // Also Apply indiret diffuse (GI)
    // PostEvaluateBSDF will perform any operation wanted by the material and sum everything into diffuseLighting and specularLighting
    PostEvaluateBSDF(   context, V, posInput, preLightData, accLighting, bsdfData, bakeDiffuseLighting,
                        diffuseLighting, specularLighting);

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}
