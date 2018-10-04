#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "LightLoop.hlsl"

//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

void SimpleLightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, uint featureFlags,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;
    context.sampleReflection = 0;
    context.shadowContext = InitShadowContext();
    context.contactShadow = 1;
    context.shadowValue = 1;
    
    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        UNITY_BRANCH if(_DirectionalShadowIndex != -1)
        {
            context.shadowValue = GetDirectionalShadowAttenuation(
                context.shadowContext, posInput.positionWS, GetShadowNormalBias(bsdfData),
                _DirectionalLightDatas[_DirectionalShadowIndex].shadowIndex,
                -_DirectionalLightDatas[_DirectionalShadowIndex].forward
            );
        }
    }

    // This struct is define in the material. the Lightloop must not access it
    // PostEvaluateBSDF call at the end will convert Lighting to diffuse and specular lighting
    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the struct

    uint i = 0; // Declare once to avoid the D3D11 compiler warning.

    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        for (i = 0; i < _DirectionalLightCount; ++i)
        {
            if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = SimpleEvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
        }
    }

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        uint lightCount, lightStart;

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
    #else
        lightCount = _PunctualLightCount;
        lightStart = 0;
    #endif

        for (i = 0; i < lightCount; i++)
        {
            LightData lightData = FetchLight(lightStart, i);

            if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = SimpleEvaluateBSDF_Punctual(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
        }
    }
    
    // We use punctual BSDF for area lights (cheaper)
    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        uint lightCount, lightStart;

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, lightStart, lightCount);
    #else
        lightCount = _AreaLightCount;
        lightStart = _PunctualLightCount;
    #endif

        for (i = 0; i < lightCount; i++)
        {
            LightData lightData = FetchLight(lightStart, i);

            if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = SimpleEvaluateBSDF_Punctual(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
        }
    }
    
    // Define macro for a better understanding of the loop
    // TODO: this code is now much harder to understand...
#define EVALUATE_BSDF_ENV_SKY(envLightData, TYPE, type) \
        IndirectLighting lighting = EvaluateBSDF_Env(context, V, posInput, preLightData, envLightData, bsdfData, envLightData.influenceShapeType, MERGE_NAME(GPUIMAGEBASEDLIGHTINGTYPE_, TYPE), MERGE_NAME(type, HierarchyWeight)); \
        AccumulateIndirectLighting(lighting, aggregateLighting);

// Environment cubemap test lightlayers, sky don't test it
#define EVALUATE_BSDF_ENV(envLightData, TYPE, type) if (IsMatchingLightLayer(envLightData.lightLayers, builtinData.renderingLayers)) { EVALUATE_BSDF_ENV_SKY(envLightData, TYPE, type) }

#if HDRP_ENABLE_ENV_LIGHT
    // First loop iteration
    if (featureFlags & (LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SKY))
    {
        float reflectionHierarchyWeight = 0.0; // Max: 1.0
        float refractionHierarchyWeight = 0.0; // Max: 1.0

        uint envLightStart, envLightCount;

        // Fetch first env light to provide the scene proxy for screen space computation
    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_ENV, envLightStart, envLightCount);
    #else
        envLightCount = _EnvLightCount;
        envLightStart = 0;
    #endif

        // Reflection / Refraction hierarchy is
        //  1. Screen Space Refraction / Reflection
        //  2. Environment Reflection / Refraction
        //  3. Sky Reflection / Refraction

        // Reflection probes are sorted by volume (in the increasing order).
        if (featureFlags & LIGHTFEATUREFLAGS_ENV)
        {
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;

            // Note: In case of IBL we are sorted from smaller to bigger projected solid angle bounds. We are not sorted by type so we can't do a 'while' approach like for area light.
            for (i = 0; i < envLightCount && reflectionHierarchyWeight < 1.0; ++i)
            {
                EnvLightData envLightData = FetchEnvLight(envLightStart, i);
                if (IsMatchingLightLayer(envLightData.lightLayers, builtinData.renderingLayers))
                {
                    IndirectLighting lighting = SimpleEvaluateBSDF_Env(context, V, posInput, preLightData, envLightData, bsdfData, envLightData.influenceShapeType, GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION, reflectionHierarchyWeight);
                    AccumulateIndirectLighting(lighting, aggregateLighting);
                }
            }
        }

        // Only apply the sky IBL if the sky texture is available
        if ((featureFlags & LIGHTFEATUREFLAGS_SKY) && _EnvLightSkyEnabled)
        {
            // The sky is a single cubemap texture separate from the reflection probe texture array (different resolution and compression)
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_SKY;

            // The sky data are generated on the fly so the compiler can optimize the code
            EnvLightData envLightSky = InitSkyEnvLightData(0);

            // Only apply the sky if we haven't yet accumulated enough IBL lighting.
            if (reflectionHierarchyWeight < 1.0)
            {
                IndirectLighting lighting = SimpleEvaluateBSDF_Env(context, V, posInput, preLightData, envLightSky, bsdfData, envLightSky.influenceShapeType, GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION, reflectionHierarchyWeight);
                AccumulateIndirectLighting(lighting, aggregateLighting);
            }
        }
    }
#endif // HDRP_ENABLE_ENV_LIGHT
#undef EVALUATE_BSDF_ENV
#undef EVALUATE_BSDF_ENV_SKY    

    // Also Apply indiret diffuse (GI)
    // SimplePostEvaluateBSDF will perform any operation wanted by the material and sum everything into diffuseLighting and specularLighting
    SimplePostEvaluateBSDF(   context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting,
                        diffuseLighting, specularLighting);

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}
