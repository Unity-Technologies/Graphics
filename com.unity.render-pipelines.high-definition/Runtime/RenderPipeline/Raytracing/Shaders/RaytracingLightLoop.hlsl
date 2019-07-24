// This allows us to either use the light cluster to pick which lights should be used, or use all the lights available
// #define USE_LIGHT_CLUSTER 

uint GetTotalLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 0];   
}

uint GetPunctualLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 1];   
}

uint GetAreaLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 2];   
}

uint GetEnvLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 3];   
}

uint GetLightClusterCellLightByIndex(int cellIndex, int lightIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 4 + lightIndex];   
}

bool PointInsideCluster(float3 positionWS)
{
    return !(positionWS.x < _MinClusterPos.x || positionWS.y < _MinClusterPos.y || positionWS.z < _MinClusterPos.z 
        || positionWS.x > _MaxClusterPos.x || positionWS.y > _MaxClusterPos.y || positionWS.z > _MaxClusterPos.z);
}

uint GetClusterCellIndex(float3 positionWS)
{
    // Compute the grid position
    uint3 gridPosition = (uint3)((positionWS - _MinClusterPos) / (_MaxClusterPos - _MinClusterPos) * float3(64.0, 64.0, 32.0));

    // Deduce the cell index
    return gridPosition.z + gridPosition.y * 32 + gridPosition.x * 2048;
}

void GetLightCountAndStartCluster(float3 positionWS, uint lightCategory, out uint lightStart, out uint lightEnd, out uint cellIndex)
{
    // If this point is outside the cluster, no lights
    if(!PointInsideCluster(positionWS))
    {
        lightStart = 0;
        lightEnd = 0;
        cellIndex = 0;
        return;
    }

    // Deduce the cell index
    cellIndex = GetClusterCellIndex(positionWS);

    // Grab the light count
    lightStart = lightCategory == 0 ? 0 : (lightCategory == 1 ? GetPunctualLightClusterCellCount(cellIndex) : GetAreaLightClusterCellCount(cellIndex));
    lightEnd = lightCategory == 0 ? GetPunctualLightClusterCellCount(cellIndex) : (lightCategory == 1 ? GetAreaLightClusterCellCount(cellIndex) : GetEnvLightClusterCellCount(cellIndex));
}

LightData FetchClusterLightIndex(int cellIndex, uint lightIndex)
{
    int absoluteLightIndex = GetLightClusterCellLightByIndex(cellIndex, lightIndex);
    return _LightDatasRT[absoluteLightIndex];
}

EnvLightData FetchClusterEnvLightIndex(int cellIndex, uint lightIndex)
{
    int absoluteLightIndex = GetLightClusterCellLightByIndex(cellIndex, lightIndex);
    return _EnvLightDatasRT[absoluteLightIndex];
}

void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, 
            float reflectionHierarchyWeight, float refractionHierarchyWeight, float3 reflection, float3 transmission,
			out float3 diffuseLighting,
            out float3 specularLighting)
{
    LightLoopContext context;
    context.contactShadow    = 1.0;
    context.shadowContext    = InitShadowContext();
    context.shadowValue      = 1.0;
    context.sampleReflection = 0;

    // Initialize the contactShadow and contactShadowFade fields
    InitContactShadow(posInput, context);
    
    // Evaluate sun shadows.
    if (_DirectionalShadowIndex >= 0)
    {
        DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

        // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
        float3 L = -light.forward;

        // Is it worth sampling the shadow map?
        if ((light.lightDimmer > 0) && (light.shadowDimmer > 0) && // Note: Volumetric can have different dimmer, thus why we test it here
            IsNonZeroBSDF(V, L, preLightData, bsdfData) &&
            !ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
        {
            context.shadowValue = GetDirectionalShadowAttenuation(context.shadowContext,
                                                                  posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData),
                                                                  light.shadowIndex, L);
        }
    }

    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the structure

    // Indices of the subranges to process
    uint lightStart = 0, lightEnd = 0;

    // The light cluster is in actual world space coordinates, 
    #ifdef USE_LIGHT_CLUSTER
    // Get the actual world space position
    float3 actualWSPos = GetAbsolutePositionWS(posInput.positionWS);
    #endif

    #ifdef USE_LIGHT_CLUSTER
    // Get the punctual light count
    uint cellIndex;
    GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_PUNCTUAL, lightStart, lightEnd, cellIndex);
    #else
    lightStart = 0;
    lightEnd = _PunctualLightCountRT;
    #endif

    uint i = 0;
    for (i = lightStart; i < lightEnd; i++)
    {
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(cellIndex, i);
        #else
        LightData lightData = _LightDatasRT[i];
        #endif
        if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
        {
            DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
            AccumulateDirectLighting(lighting, aggregateLighting);
        }
    }

#if !defined(_DISABLE_SSR)
    // Add the traced reflection
    if (reflectionHierarchyWeight == 1.0)
    {
        IndirectLighting indirect;
        ZERO_INITIALIZE(IndirectLighting, indirect);
        indirect.specularReflected = reflection.rgb * preLightData.specularFGD;
        AccumulateIndirectLighting(indirect, aggregateLighting);
    }
#endif

#if HAS_REFRACTION
    // Add the traced transmission
    if (refractionHierarchyWeight == 1.0)
    {
        IndirectLighting indirect;
        ZERO_INITIALIZE(IndirectLighting, indirect);
        IndirectLighting lighting = EvaluateBSDF_RaytracedRefraction(context, preLightData, transmission);
        AccumulateIndirectLighting(lighting, aggregateLighting);
    }
#endif

    // Define macro for a better understanding of the loop
    // TODO: this code is now much harder to understand...
#define EVALUATE_BSDF_ENV_SKY(envLightData, TYPE, type) \
    IndirectLighting lighting = EvaluateBSDF_Env(context, V, posInput, preLightData, envLightData, bsdfData, envLightData.influenceShapeType, MERGE_NAME(GPUIMAGEBASEDLIGHTINGTYPE_, TYPE), MERGE_NAME(type, HierarchyWeight)); \
    AccumulateIndirectLighting(lighting, aggregateLighting);

// Environment cubemap test lightlayers, sky don't test it
#define EVALUATE_BSDF_ENV(envLightData, TYPE, type) if (IsMatchingLightLayer(envLightData.lightLayers, builtinData.renderingLayers)) { EVALUATE_BSDF_ENV_SKY(envLightData, TYPE, type) }
    
    #ifdef USE_LIGHT_CLUSTER
    // Get the punctual light count
    GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_ENV, lightStart, lightEnd, cellIndex);
    #else
    lightStart = 0;
    lightEnd = _EnvLightCountRT;
    #endif

    context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;

    // Scalarized loop, same rationale of the punctual light version
    uint envLightIdx = lightStart;
    while (envLightIdx < lightEnd)
    {
        #ifdef USE_LIGHT_CLUSTER
        EnvLightData envLightData = FetchClusterEnvLightIndex(cellIndex, envLightIdx);
        #else
        EnvLightData envLightData = _EnvLightDatasRT[envLightIdx];
        #endif
        envLightData.multiplier = _EnvLightDatas[envLightIdx].multiplier;

        if (reflectionHierarchyWeight < 1.0)
        {
            EVALUATE_BSDF_ENV(envLightData, REFLECTION, reflection);
        }
        if (refractionHierarchyWeight < 1.0)
        {
            EVALUATE_BSDF_ENV(envLightData, REFRACTION, refraction);
        }
        envLightIdx++;
    }

    // Only apply the sky IBL if the sky texture is available
    if (_EnvLightSkyEnabled)
    {
        // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
        context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;

        // The sky data are generated on the fly so the compiler can optimize the code
        EnvLightData envLightSky = InitSkyEnvLightData(0);

        // Only apply the sky if we haven't yet accumulated enough IBL lighting.
        if (reflectionHierarchyWeight < 1.0)
        {
            EVALUATE_BSDF_ENV_SKY(envLightSky, REFLECTION, reflection);
        }

        if ((refractionHierarchyWeight < 1.0))
        {
            EVALUATE_BSDF_ENV_SKY(envLightSky, REFRACTION, refraction);
        }
    }
#undef EVALUATE_BSDF_ENV
#undef EVALUATE_BSDF_ENV_SKY

    // We loop over all the directional lights given that there is no culling for them
    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, builtinData.renderingLayers))
        {
            DirectLighting lighting = EvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData, builtinData);
            AccumulateDirectLighting(lighting, aggregateLighting);
        }
    }


    #ifdef USE_LIGHT_CLUSTER
    // Let's loop through all the 
    GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_AREA, lightStart, lightEnd, cellIndex);
    #else
    lightStart = _PunctualLightCountRT;
    lightEnd = _PunctualLightCountRT + _AreaLightCountRT;
    #endif

    if (lightEnd != lightStart)
    {
        i = lightStart;
        uint last = lightEnd;
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(cellIndex, i);
        #else
        LightData lightData = _LightDatasRT[i];
        #endif

        while (i < last && lightData.lightType == GPULIGHTTYPE_TUBE)
        {
            lightData.lightType = GPULIGHTTYPE_TUBE; // Enforce constant propagation

            if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
            i++;
            #ifdef USE_LIGHT_CLUSTER
            lightData = FetchClusterLightIndex(cellIndex, i);
            #else
            lightData = _LightDatasRT[i];
            #endif
        }

        while (i < last ) // GPULIGHTTYPE_RECTANGLE
        {
            lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

            if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
            i++;
            #ifdef USE_LIGHT_CLUSTER
            lightData = FetchClusterLightIndex(cellIndex, i);
            #else
            lightData = _LightDatasRT[i];
            #endif
        }
    }

    PostEvaluateBSDF(context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting, diffuseLighting, specularLighting);
}
