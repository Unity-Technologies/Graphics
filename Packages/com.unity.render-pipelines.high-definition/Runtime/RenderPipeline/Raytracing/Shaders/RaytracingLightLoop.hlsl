#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/RayTracingFallbackHierarchy.cs.hlsl"

#define USE_LIGHT_CLUSTER

struct RayContext
{
    // Signal that came from a bounced ray
    float3 reflection;
    // Weight for the bounced ray
    float reflectionWeight;

    // Signal that came from a transmitted ray
    float3 transmission;
    // Weight for the transmitted ray
    float transmissionWeight;

    // Should the APV be used for the lightloop ? (in case of multibounce GI)
    int useAPV;
};

void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, RayContext rayContext,
                out LightLoopOutput lightLoopOutput)
{
    // Init LightLoop output structure
    ZERO_INITIALIZE(LightLoopOutput, lightLoopOutput);
    ApplyCameraRelativeXR(posInput.positionWS);
    
    LightLoopContext context;
    context.contactShadow    = 1.0;
    context.shadowContext    = InitShadowContext();
    context.shadowValue      = 1.0;
    context.sampleReflection = 0;
#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
    context.positionWS       = posInput.positionWS;
#endif

    // Initialize the contactShadow and contactShadowFade fields
    InvalidateConctactShadow(posInput, context);

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
            int shadowSplitIndex;
            context.shadowValue = EvalShadow_CascadedDepth_Dither_SplitIndex(context.shadowContext, _ShadowmapCascadeAtlas, s_linear_clamp_compare_sampler, posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData), light.shadowIndex, L, shadowSplitIndex);
            if (shadowSplitIndex < 0.0)
            {
                 context.shadowValue = _DirectionalShadowFallbackIntensity;
            }
        }
    }

    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the structure

    // Indices of the subranges to process
    uint lightStart = 0, lightEnd = 0;

    // The light cluster is in actual world space coordinates,
    #ifdef USE_LIGHT_CLUSTER
    // Get the actual world space position
    float3 actualWSPos = posInput.positionWS;
    #endif

    #ifdef USE_LIGHT_CLUSTER
    // Get the punctual light count
    uint cellIndex;
    GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_PUNCTUAL, lightStart, lightEnd, cellIndex);
    #else
    lightStart = 0;
    lightEnd = _WorldPunctualLightCount;
    #endif

    uint i = 0;
    for (i = lightStart; i < lightEnd; i++)
    {
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(cellIndex, i);
        #else
        LightData lightData = _WorldLightDatas[i];
        #endif
        if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
        {
            DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
            AccumulateDirectLighting(lighting, aggregateLighting);
        }
    }

    // Initalize the reflection and refraction hierarchy weights
    float reflectionHierarchyWeight = 0.0;
    float refractionHierarchyWeight = 0.0;

    // Add the traced reflection (if any)
    if (rayContext.reflectionWeight == 1.0)
    {
        IndirectLighting lighting = EvaluateBSDF_RaytracedReflection(context, bsdfData, preLightData, rayContext.reflection.xyz);
        AccumulateIndirectLighting(lighting, aggregateLighting);
        reflectionHierarchyWeight = 1.0;
    }

#if HAS_REFRACTION
    // Add the traced transmission (if any)
    if (rayContext.transmissionWeight == 1.0)
    {
        IndirectLighting indirect;
        ZERO_INITIALIZE(IndirectLighting, indirect);
        IndirectLighting lighting = EvaluateBSDF_RaytracedRefraction(context, preLightData, rayContext.transmission.xyz);
        AccumulateIndirectLighting(lighting, aggregateLighting);
        refractionHierarchyWeight = 1.0;
    }
#endif

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    if (rayContext.useAPV == 1)
    {
        // At this point bakeDiffuseLighting only contain emissives when using APV
        real3 emissive = builtinData.bakeDiffuseLighting;

        if (_EnableProbeVolumes && rayContext.useAPV == 1)
        {
            // Reflect normal to get lighting for reflection probe tinting
            float3 R = reflect(-V, bsdfData.normalWS);

            // This variable is used with APV for reflection probe normalization - see code for LIGHTFEATUREFLAGS_ENV
            float3 lightInReflDir = float3(-1, -1, -1);

            EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(posInput.positionWS),
                bsdfData.normalWS,
                -bsdfData.normalWS,
                R,
                V,
                posInput.positionSS,
                _RaytracingAPVLayerMask,
                builtinData.bakeDiffuseLighting,
                builtinData.backBakeDiffuseLighting,
                lightInReflDir);
        }
        else // If probe volume is disabled we fallback on the ambient probes
        {
            builtinData.bakeDiffuseLighting = EvaluateAmbientProbe(bsdfData.normalWS) * _RayTracingAmbientProbeDimmer;
            builtinData.backBakeDiffuseLighting = EvaluateAmbientProbe(-bsdfData.normalWS) * _RayTracingAmbientProbeDimmer;
        }

    #ifdef  MODIFY_BAKED_DIFFUSE_LIGHTING
        // Make sure the baked diffuse lighting is tinted with the diffuse color
        ModifyBakedDiffuseLighting(V, posInput, preLightData, bsdfData, builtinData);
    #endif

        // Add emissiveon top of diffuse
        builtinData.bakeDiffuseLighting += emissive;
    }
#endif

    // This is applied only on bakeDiffuseLighting as ModifyBakedDiffuseLighting combine both bakeDiffuseLighting and backBakeDiffuseLighting
    builtinData.bakeDiffuseLighting *= GetIndirectDiffuseMultiplier(builtinData.renderingLayers);

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
    lightEnd = _WorldEnvLightCount;
    #endif

    context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
    if (RAYTRACINGFALLBACKHIERACHY_REFLECTION_PROBES & _RayTracingLastBounceFallbackHierarchy)
    {
        // Scalarized loop, same rationale of the punctual light version
        uint envLightIdx = lightStart;
        while (envLightIdx < lightEnd)
        {
            #ifdef USE_LIGHT_CLUSTER
            EnvLightData envLightData = FetchClusterEnvLightIndex(cellIndex, envLightIdx);
            #else
            EnvLightData envLightData = _WorldEnvLightDatas[envLightIdx];
            #endif

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
    }

    // Only apply the sky IBL if the sky texture is available
    if (_EnvLightSkyEnabled && (RAYTRACINGFALLBACKHIERACHY_SKY & _RayTracingLastBounceFallbackHierarchy))
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
    lightStart = _WorldPunctualLightCount;
    lightEnd = _WorldPunctualLightCount + _WorldAreaLightCount;
    #endif

    if (lightEnd != lightStart)
    {
        i = lightStart;
        uint last = lightEnd;
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(cellIndex, i);
        #else
        LightData lightData = _WorldLightDatas[i];
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
            lightData = _WorldLightDatas[i];
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
            lightData = _WorldLightDatas[i];
            #endif
        }
    }

    PostEvaluateBSDF(context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting, lightLoopOutput);
}
