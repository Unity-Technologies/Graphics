#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

#ifndef SCALARIZE_LIGHT_LOOP
// We perform scalarization only for forward rendering as for deferred loads will already be scalar since tiles will match waves and therefore all threads will read from the same tile.
// More info on scalarization: https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/ .
// Note that it is currently disabled on gamecore platforms for issues with wave intrinsics and the new compiler, it will be soon investigated, but we disable it in the meantime.
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && !defined(SHADER_API_GAMECORE) && SHADERPASS == SHADERPASS_FORWARD)
#endif


//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

void LightLoopSurfaceCache( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, uint featureFlags,
                out LightLoopOutput lightLoopOutput)
{
    // Init LightLoop output structure
    ZERO_INITIALIZE(LightLoopOutput, lightLoopOutput);

    LightLoopContext context;

    context.shadowContext    = InitShadowContext();
    context.shadowValue      = 1;
    context.sampleReflection = 0;
    context.splineVisibility = -1;

      // With XR single-pass and camera-relative: offset position to do lighting computations from the combined center view (original camera matrix).
    // This is required because there is only one list of lights generated on the CPU. Shadows are also generated once and shared between the instanced views.
    ApplyCameraRelativeXR(posInput.positionWS);

    // Initialize the contactShadow and contactShadowFade fields
    InitContactShadow(posInput, context);

    // First of all we compute the shadow value of the directional light to reduce the VGPR pressure
    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        // Evaluate sun shadows.
        if (_DirectionalShadowIndex >= 0)
        {
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

#if defined(SCREEN_SPACE_SHADOWS_ON) && !defined(_SURFACE_TYPE_TRANSPARENT)
            if (UseScreenSpaceShadow(light, bsdfData.normalWS))
            {
                context.shadowValue = GetScreenSpaceColorShadow(posInput, light.screenSpaceShadowIndex).SHADOW_TYPE_SWIZZLE;
            }
            else
#endif
            {
                // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
                float3 L = -light.forward;

                // Is it worth sampling the shadow map?
                if ((light.lightDimmer > 0) && (light.shadowDimmer > 0) && // Note: Volumetric can have different dimmer, thus why we test it here
                    IsNonZeroBSDF(V, L, preLightData, bsdfData) &&
                    !ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
                {
                    float3 positionWS = posInput.positionWS;

#ifdef LIGHT_EVALUATION_SPLINE_SHADOW_BIAS
                    positionWS += L * GetSplineOffsetForShadowBias(bsdfData);
#endif
                    context.shadowValue = GetDirectionalShadowAttenuation(context.shadowContext,
                                                                          posInput.positionSS, positionWS, GetNormalForShadowBias(bsdfData),
                                                                          light.shadowIndex, L);

#ifdef LIGHT_EVALUATION_SPLINE_SHADOW_VISIBILITY_SAMPLE
                    // Tap the shadow a second time for strand visibility term.
                    context.splineVisibility = GetDirectionalShadowAttenuation(context.shadowContext,
                                                                               posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData),
                                                                               light.shadowIndex, L);
#endif
                }
            }
        }
    }

    // This struct is define in the material. the Lightloop must not access it
    // PostEvaluateBSDF call at the end will convert Lighting to diffuse and specular lighting
    AggregateLighting aggregateLighting;
    ZERO_INITIALIZE(AggregateLighting, aggregateLighting); // LightLoop is in charge of initializing the struct

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        uint lightCount, lightStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        lightCount = _PunctualLightCount;
        lightStart = 0;
#endif

        bool fastPath = false;
    #if SCALARIZE_LIGHT_LOOP
        uint lightStartLane0;
        fastPath = IsFastPath(lightStart, lightStartLane0);

        if (fastPath)
        {
            lightStart = lightStartLane0;
        }
    #endif

        // Scalarized loop. All lights that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
        // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
        // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
        // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
        // Note that the above is valid only if wave intriniscs are supported.
        uint v_lightListOffset = 0;
        uint v_lightIdx = lightStart;

        while (v_lightListOffset < lightCount)
        {
            v_lightIdx = FetchIndex(lightStart, v_lightListOffset);
#if SCALARIZE_LIGHT_LOOP
            uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
#else
            uint s_lightIdx = v_lightIdx;
#endif
            if (s_lightIdx == -1)
                break;

            LightData s_lightData = FetchLight(s_lightIdx);

            // If current scalar and vector light index match, we process the light. The v_lightListOffset for current thread is increased.
            // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
            // end up with a unique v_lightIdx value that is smaller than s_lightIdx hence being stuck in a loop. All the active lanes will not have this problem.
            if (s_lightIdx >= v_lightIdx)
            {
                v_lightListOffset++;
                if (IsMatchingLightLayer(s_lightData.lightLayers, builtinData.renderingLayers))
                {
                    DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, s_lightData, bsdfData, builtinData);
                    AccumulateDirectLighting(lighting, aggregateLighting);
                }
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

    // First loop iteration
    if (featureFlags & (LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_SSREFRACTION | LIGHTFEATUREFLAGS_SSREFLECTION))
    {
        float reflectionHierarchyWeight = 0.0; // Max: 1.0
        float refractionHierarchyWeight = _EnableSSRefraction ? 0.0 : 1.0; // Max: 1.0

        uint envLightStart, envLightCount;

        // Fetch first env light to provide the scene proxy for screen space computation
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_ENV, envLightStart, envLightCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        envLightCount = _EnvLightCount;
        envLightStart = 0;
#endif

        bool fastPath = false;
    #if SCALARIZE_LIGHT_LOOP
        uint envStartFirstLane;
        fastPath = IsFastPath(envLightStart, envStartFirstLane);
    #endif

        //---------------------------------------
        // Sum up of operations on indirect diffuse lighting
        // Let's define SSGI as SSGI/RTGI/Mixed or APV without lightmaps
        // Let's define GI as Lightmaps/Lightprobe/APV with lightmaps

        // By default we do those operations in deferred
        // GBuffer pass : GI * AO + Emissive -> lightingbuffer
        // Lightloop : indirectDiffuse = lightingbuffer; indirectDiffuse * SSAO
        // Note that SSAO is apply on emissive in this case and we have double occlusion between AO and SSAO on indirectDiffuse

        // By default we do those operation in forward
        // Lightloop : indirectDiffuse = GI; indirectDiffuse * min(AO, SSAO) + Emissive

        // With any SSGI effect we are performing those operations in deferred
        // GBuffer pass : Emissive == 0 ? AmbientOcclusion -> EncodeIn(lightingbuffer) : Emissive -> lightingbuffer
        // Lightloop : indirectDiffuse = SSGI; Emissive = lightingbuffer; AmbientOcclusion = Extract(lightingbuffer) or 1.0;
        // indirectDiffuse * min(AO, SSAO) + Emissive
        // Note that mean that we have the same behavior than forward path if Emissive is 0

        // With any SSGI effect we are performing those operations in Forward
        // Lightloop : indirectDiffuse = SSGI; indirectDiffuse * min(AO, SSAO) + Emissive
        //---------------------------------------

        // Explanation about APV and SSGI/RTGI/Mixed effects steps in the rendering pipeline.
        // All effects will output only Emissive inside the lighting buffer (gbuffer3) in case of deferred (For APV this is done only if we are not a lightmap).
        // The Lightmaps/Lightprobes contribution is 0 for those cases. Code enforce it in SampleBakedGI(). The remaining code of Material pass (and the debug code)
        // is exactly the same with or without effects on, including the EncodeToGbuffer.
        // builtinData.isLightmap is used by APV to know if we have lightmap or not and is harcoded based on preprocessor in InitBuiltinData()
        // AO is also store with a hack in Gbuffer3 if possible. Otherwise it is set to 1.
        // In case of regular deferred path (when effects aren't enable) AO is already apply on lightmap and emissive is added on to of it. All is inside bakeDiffuseLighting and emissiveColor is 0. AO must be 1
        // When effects are enabled and for APV we don't have lightmaps, bakeDiffuseLighting is 0 and emissiveColor contain emissive (Either in deferred or forward) and AO should be the real AO value.
        // Then in the lightloop in below code we will evalaute APV or read the indirectDiffuseTexture to fill bakeDiffuseLighting.
        // We will then just do all the regular step we do with bakeDiffuseLighting in PostInitBuiltinData()
        // No code change is required to handle AO, it is the same for all path.
        // Note: Decals Emissive and Transparent Emissve aren't taken into account by RTGI/Mixed.
        // Forward opaque emissive work in all cases. The current code flow with Emissive store in GBuffer3 is only to manage the case of Opaque Lit Material with Emissive in case of deferred
        // Only APV can handle backFace lighting, all other effects are front face only.

        // If we use SSGI/RTGI/Mixed effect, we are fully replacing the value of builtinData.bakeDiffuseLighting which is 0 at this step.
        // If we are APV we only replace the non lightmaps part.
        bool replaceBakeDiffuseLighting = false;
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        float3 lightInReflDir = float3(-1, -1, -1); // This variable is used with APV for reflection probe normalization - see code for LIGHTFEATUREFLAGS_ENV
#endif

#if !defined(_SURFACE_TYPE_TRANSPARENT) // No SSGI/RTGI/Mixed effect on transparent
        if (_IndirectDiffuseMode != INDIRECTDIFFUSEMODE_OFF)
            replaceBakeDiffuseLighting = true;
#endif
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        if (!builtinData.isLightmap)
            replaceBakeDiffuseLighting = true;
#endif

        if (replaceBakeDiffuseLighting)
        {
            BuiltinData tempBuiltinData;
            ZERO_INITIALIZE(BuiltinData, tempBuiltinData);

#if !defined(_SURFACE_TYPE_TRANSPARENT)
            if (_IndirectDiffuseMode != INDIRECTDIFFUSEMODE_OFF)
            {
                tempBuiltinData.bakeDiffuseLighting = LOAD_TEXTURE2D_X(_IndirectDiffuseTexture, posInput.positionSS).xyz * GetInverseCurrentExposureMultiplier();
            }
            else
#endif
            {
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
                if (_EnableProbeVolumes)
                {
                    // Reflect normal to get lighting for reflection probe tinting
                    float3 R = reflect(-V, bsdfData.normalWS);

                    EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(posInput.positionWS),
                        bsdfData.normalWS,
                        -bsdfData.normalWS,
                        R,
                        V,
                        posInput.positionSS,
                        tempBuiltinData.bakeDiffuseLighting,
                        tempBuiltinData.backBakeDiffuseLighting,
                        lightInReflDir);
                }
                else // If probe volume is disabled we fallback on the ambient probes
                {
                    tempBuiltinData.bakeDiffuseLighting = EvaluateAmbientProbe(bsdfData.normalWS);
                    tempBuiltinData.backBakeDiffuseLighting = EvaluateAmbientProbe(-bsdfData.normalWS);
                }
#endif
            }

#ifdef MODIFY_BAKED_DIFFUSE_LIGHTING
                ModifyBakedDiffuseLighting(V, posInput, preLightData, bsdfData, tempBuiltinData);
#endif
            // This is applied only on bakeDiffuseLighting as ModifyBakedDiffuseLighting combine both bakeDiffuseLighting and backBakeDiffuseLighting
            tempBuiltinData.bakeDiffuseLighting *= GetIndirectDiffuseMultiplier(builtinData.renderingLayers);

            ApplyDebugToBuiltinData(tempBuiltinData); // This will not affect emissive as we don't use it

            // Replace original data
            builtinData.bakeDiffuseLighting = tempBuiltinData.bakeDiffuseLighting;

        } // if (replaceBakeDiffuseLighting)

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
                EVALUATE_BSDF_ENV_SKY(envLightSky, REFLECTION, reflection);
            }

            if ((featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION) && (refractionHierarchyWeight < 1.0))
            {
                EVALUATE_BSDF_ENV_SKY(envLightSky, REFRACTION, refraction);
            }
        }
    }
#undef EVALUATE_BSDF_ENV
#undef EVALUATE_BSDF_ENV_SKY


    uint i = 0; // Declare once to avoid the D3D11 compiler warning.
    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        for (i = 0; i < _DirectionalLightCount; ++i)
        {
            if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, builtinData.renderingLayers))
            {
                DirectLighting lighting = EvaluateBSDF_Directional(context, V, posInput, preLightData, _DirectionalLightDatas[i], bsdfData, builtinData);
                AccumulateDirectLighting(lighting, aggregateLighting);
            }
        }
    }
    #if SHADEROPTIONS_AREA_LIGHTS
    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        uint lightCount, lightStart;

    #ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
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

            while (i <= last && lightData.lightType == GPULIGHTTYPE_TUBE)
            {
                lightData.lightType = GPULIGHTTYPE_TUBE; // Enforce constant propagation
                lightData.cookieMode = COOKIEMODE_NONE;  // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
                {
                    DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                    AccumulateDirectLighting(lighting, aggregateLighting);
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }

            while (i <= last) // GPULIGHTTYPE_RECTANGLE
            {
                lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
                {
                    DirectLighting lighting = EvaluateBSDF_Area(context, V, posInput, preLightData, lightData, bsdfData, builtinData);
                    AccumulateDirectLighting(lighting, aggregateLighting);
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }
#endif

    lightLoopOutput.diffuseLighting = bsdfData.diffuseColor * aggregateLighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor /*+ bsdfData.diffuseColor * aggregateLighting.indirect.specularReflected*/;

}
