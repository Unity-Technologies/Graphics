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

    context.shadowContext    = InitShadowContext();
    context.contactShadow    = 1;
    context.shadowValue      = 1;
    context.sampleReflection = 0;

    // First of all we compute the shadow value of the directional light to reduce the VGPR pressure
    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        // Evaluate sun shadows.
        if (_DirectionalShadowIndex >= 0)
        {
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

            // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
            // Also, the light direction is not consistent with the sun disk highlight hack, which modifies the light vector.
            float  NdotL            = dot(bsdfData.normalWS, -light.forward);
            float3 shadowBiasNormal = GetNormalForShadowBias(bsdfData);
            bool   evaluateShadows  = (NdotL > 0);

        #ifdef MATERIAL_INCLUDE_TRANSMISSION
            if (MaterialSupportsTransmission(bsdfData))
            {
                // We support some kind of transmission.
                if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
                {
                    // We always evaluate shadows.
                    evaluateShadows = true;

                    // Care must be taken to bias in the direction of the light.
                    shadowBiasNormal *= FastSign(NdotL);
                }
            }
        #endif

            if (evaluateShadows)
            {
                context.shadowValue = EvaluateRuntimeSunShadow(context, posInput, light, shadowBiasNormal);
            }
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
//Note: We don't enable area lights yet because there are some issues with punctual light attenuation intensity for simple area lights
#if 0

    // We don't evaluate area lights in simple lit mode
    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        uint lightCount, lightStart;

    #ifdef LIGHTLOOP_TILE_PASS
        GetCountAndStart(posInput, LIGHTCATEGORY_AREA, lightStart, lightCount);
    #else
        lightCount = _AreaLightCount;
        lightStart = _PunctualLightCount;
    #endif

        // COMPILER BEHAVIOR WARNING!
        // If rectangle lights are before line lights, the compiler will duplicate light matrices in VGPR because they are used differently between the two types of lights.
        // By keeping line lights first we avoid this behavior and save substantial register pressure.
        // TODO: This is based on the current Lit.shader and can be different for any other way of implementing area lights, how to be generic and ensure performance ?

        if (lightCount > 0)
        {
            i = 0;

            uint      last      = lightCount - 1;
            LightData lightData = FetchLight(lightStart, i);

            while (i <= last && lightData.lightType == GPULIGHTTYPE_LINE)
            {
                lightData.lightType = GPULIGHTTYPE_LINE; // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
                {
                    DirectLighting lighting = SimpleEvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                    AccumulateDirectLighting(lighting, aggregateLighting);
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }

            while (i <= last) // GPULIGHTTYPE_RECTANGLE
            {
                lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
                {
                    DirectLighting lighting = SimpleEvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                    AccumulateDirectLighting(lighting, aggregateLighting);
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }

#endif // Area light disabled

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

    // Also Apply indiret diffuse (GI)
    // PostEvaluateBSDF will perform any operation wanted by the material and sum everything into diffuseLighting and specularLighting
    SimplePostEvaluateBSDF(   context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting,
                        diffuseLighting, specularLighting);

    ApplyDebug(context, posInput.positionWS, diffuseLighting, specularLighting);
}
