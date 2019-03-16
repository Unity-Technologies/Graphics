// This allows us to either use the light cluster to pick which lights should be used, or use all the lights available
#define USE_LIGHT_CLUSTER 

uint GetTotalLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 3) + 0];   
}

uint GetPunctualLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 3) + 1];   
}

uint GetAreaLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 3) + 2];   
}

uint GetLightClusterCellLightByIndex(int cellIndex, int lightIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 3) + 3 + lightIndex];   
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
    lightStart = lightCategory == 0 ? 0 : GetPunctualLightClusterCellCount(cellIndex);
    lightEnd = lightCategory == 0 ? GetPunctualLightClusterCellCount(cellIndex) : GetAreaLightClusterCellCount(cellIndex);
}

LightData FetchClusterLightIndex(int cellIndex, uint lightIndex)
{
    int absoluteLightIndex = GetLightClusterCellLightByIndex(cellIndex, lightIndex);
    return _LightDatasRT[absoluteLightIndex];
}

void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, float3 reflection, float3 transmission,
			out float3 diffuseLighting,
            out float3 specularLighting)
{
    LightLoopContext context;
    context.contactShadow    = 1.0f;
    context.shadowContext    = InitShadowContext();
    context.shadowValue      = 1.0f;
    context.sampleReflection = 0;

    // Evaluate sun shadows.
    if (_DirectionalShadowIndex >= 0)
    {
        DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

        // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
        // Also, the light direction is not consistent with the sun disk highlight hack, which modifies the light vector.
        float  NdotL            = dot(bsdfData.normalWS, -light.forward);
        float3 shadowBiasNormal = GetNormalForShadowBias(bsdfData);
        bool   evaluateShadows  = (NdotL > 0);

        if (evaluateShadows)
        {
            context.shadowValue = EvaluateRuntimeSunShadow(context, posInput, light, shadowBiasNormal);
        }
    }

    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the structure

    // We loop over all the directional lights given that there is no culling for them
    int i = 0;
    for (i = 0; i < _DirectionalLightCount; ++i)
    {
		if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, builtinData.renderingLayers))
		{
			DirectLighting lighting = EvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData, builtinData);
			AccumulateDirectLighting(lighting, aggregateLighting);
		}
    }

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

    #ifdef USE_LIGHT_CLUSTER
    // Let's loop through all the 
    GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_AREA, lightStart, lightEnd, cellIndex);
    #else
    lightStart = _PunctualLightCountRT;
    lightEnd = _PunctualLightCountRT + _AreaLightCountRT;
    #endif

    diffuseLighting = i;

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

#if !defined(_DISABLE_SSR)
    // Add the traced reflection
    {
        IndirectLighting indirect;
        ZERO_INITIALIZE(IndirectLighting, indirect);
        indirect.specularReflected = reflection.rgb * preLightData.specularFGD;
        AccumulateIndirectLighting(indirect, aggregateLighting);
    }
#endif

#if HAS_REFRACTION
    // Add the traced transmission
    {
        IndirectLighting indirect;
        ZERO_INITIALIZE(IndirectLighting, indirect);
        IndirectLighting lighting = EvaluateBSDF_RaytracedRefraction(context, preLightData, transmission);
        AccumulateIndirectLighting(lighting, aggregateLighting);
    }
#endif
    // TODO: Support properly the sky env lights
   	EnvLightData envLightSky = InitSkyEnvLightData(0);
    // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
    context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;
    float val = 0.0f;
    IndirectLighting lighting = EvaluateBSDF_Env(context, V, posInput, preLightData, envLightSky, bsdfData, envLightSky.influenceShapeType, 0, val);
    AccumulateIndirectLighting(lighting, aggregateLighting);
    
    PostEvaluateBSDF(context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting, diffuseLighting, specularLighting);
}
