#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// We perform scalarization only for forward rendering as for deferred loads will already be scalar since tiles will match waves and therefore all threads will read from the same tile.
// More info on scalarization: https://flashypixels.wordpress.com/2018/11/10/intro-to-gpu-scalarization-part-2-scalarize-all-the-lights/
#define SCALARIZE_LIGHT_LOOP (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER) && SHADERPASS == SHADERPASS_FORWARD)

//-----------------------------------------------------------------------------
// LightLoop
// ----------------------------------------------------------------------------

// Copied from VolumeVoxelization.compute
float ProbeVolumeComputeFadeFactor(
    float3 samplePositionBoxNDC,
    float depthWS,
    float3 rcpPosFaceFade,
    float3 rcpNegFaceFade,
    float rcpDistFadeLen,
    float endTimesRcpDistFadeLen)
{
    float3 posF = Remap10(samplePositionBoxNDC, rcpPosFaceFade, rcpPosFaceFade);
    float3 negF = Remap01(samplePositionBoxNDC, rcpNegFaceFade, 0);
    float  dstF = Remap10(depthWS, rcpDistFadeLen, endTimesRcpDistFadeLen);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    return dstF * fade;
}

void ApplyDebug(LightLoopContext context, PositionInputs posInput, BSDFData bsdfData, inout float3 diffuseLighting, inout float3 specularLighting)
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
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
        // Take the luminance
        diffuseLighting = Luminance(diffuseLighting).xxx;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_VISUALIZE_CASCADE)
    {
        specularLighting = float3(0.0, 0.0, 0.0);

        const float3 s_CascadeColors[] = {
            float3(0.5, 0.5, 0.7),
            float3(0.5, 0.7, 0.5),
            float3(0.7, 0.7, 0.5),
            float3(0.7, 0.5, 0.5),
            float3(1.0, 1.0, 1.0)
        };

        diffuseLighting = Luminance(diffuseLighting);
        if (_DirectionalShadowIndex >= 0)
        {
            real alpha;
            int cascadeCount;

            int shadowSplitIndex = EvalShadow_GetSplitIndex(context.shadowContext, _DirectionalShadowIndex, posInput.positionWS, alpha, cascadeCount);
            if (shadowSplitIndex >= 0)
            {
                float shadow = 1.0;
                if (_DirectionalShadowIndex >= 0)
                {
                    DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

#if defined(SCREEN_SPACE_SHADOWS) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    if(light.screenSpaceShadowIndex >= 0)
                    {
                        shadow = GetScreenSpaceShadow(posInput, light.screenSpaceShadowIndex);
                    }
                    else
#endif
                    {
                        float3 L = -light.forward;
                        shadow = GetDirectionalShadowAttenuation(context.shadowContext,
                                                             posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData),
                                                             light.shadowIndex, L);
                    }
                }

                float3 cascadeShadowColor = lerp(s_CascadeColors[shadowSplitIndex], s_CascadeColors[shadowSplitIndex + 1], alpha);
                // We can't mix with the lighting as it can be HDR and it is hard to find a good lerp operation for this case that is still compliant with
                // exposure. So disable exposure instead and replace color.
                diffuseLighting = cascadeShadowColor * Luminance(diffuseLighting) * shadow;
            }

        }
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_MATCAP_VIEW)
    {
        specularLighting = 0.0f;
        float3 normalVS = mul((float3x3)UNITY_MATRIX_V, bsdfData.normalWS).xyz;

        float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float3 R = reflect(V, bsdfData.normalWS);

        float2 UV = saturate(normalVS.xy * 0.5f + 0.5f);

        float4 defaultColor = GetDiffuseOrDefaultColor(bsdfData, 1.0);

        if (defaultColor.a == 1.0)
        {
            UV = saturate(R.xy * 0.5f + 0.5f);
        }

        diffuseLighting = SAMPLE_TEXTURE2D_LOD(_DebugMatCapTexture, s_linear_repeat_sampler, UV, 0).rgb * (_MatcapMixAlbedo > 0  ? defaultColor.rgb * _MatcapViewScale : 1.0f);
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_PROBE_VOLUME)
    {
        // Debug info is written to diffuseColor inside of light loop.
        specularLighting = float3(0.0, 0.0, 0.0);
    }

    // We always apply exposure when in debug mode. The exposure value will be at a neutral 0.0 when not needed.
    diffuseLighting *= exp2(_DebugExposure);
    specularLighting *= exp2(_DebugExposure);
#endif
}

void LightLoop( float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, uint featureFlags,
                out float3 diffuseLighting,
                out float3 specularLighting)
{
    LightLoopContext context;

    context.shadowContext    = InitShadowContext();
    context.shadowValue      = 1;
    context.sampleReflection = 0;

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

#if defined(SCREEN_SPACE_SHADOWS) && !defined(_SURFACE_TYPE_TRANSPARENT)
            if(light.screenSpaceShadowIndex >= 0)
            {
                context.shadowValue = GetScreenSpaceShadow(posInput, light.screenSpaceShadowIndex);
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
                    context.shadowValue = GetDirectionalShadowAttenuation(context.shadowContext,
                                                                          posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData),
                                                                          light.shadowIndex, L);
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
            uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
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

        // Reflection / Refraction hierarchy is
        //  1. Screen Space Refraction / Reflection
        //  2. Environment Reflection / Refraction
        //  3. Sky Reflection / Refraction

        // Apply SSR.
    #if !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(_DISABLE_SSR)
        {
            IndirectLighting indirect = EvaluateBSDF_ScreenSpaceReflection(posInput, preLightData, bsdfData,
                                                                           reflectionHierarchyWeight);
            AccumulateIndirectLighting(indirect, aggregateLighting);
        }
    #endif

        EnvLightData envLightData;
        if (envLightCount > 0)
        {
            envLightData = FetchEnvLight(envLightStart, 0);
        }
        else
        {
            envLightData = InitSkyEnvLightData(0);
        }

        if ((featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION) && (_EnableSSRefraction > 0))
        {
            IndirectLighting lighting = EvaluateBSDF_ScreenspaceRefraction(context, V, posInput, preLightData, bsdfData, envLightData, refractionHierarchyWeight);
            AccumulateIndirectLighting(lighting, aggregateLighting);
        }

        // Reflection probes are sorted by volume (in the increasing order).
        if (featureFlags & LIGHTFEATUREFLAGS_ENV)
        {
            context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;

        #if SCALARIZE_LIGHT_LOOP
            if (fastPath)
            {
                envLightStart = envStartFirstLane;
            }
        #endif

            // Scalarized loop, same rationale of the punctual light version
            uint v_envLightListOffset = 0;
            uint v_envLightIdx = envLightStart;
            while (v_envLightListOffset < envLightCount)
            {
                v_envLightIdx = FetchIndex(envLightStart, v_envLightListOffset);
                uint s_envLightIdx = ScalarizeElementIndex(v_envLightIdx, fastPath);
                if (s_envLightIdx == -1)
                    break;

                EnvLightData s_envLightData = FetchEnvLight(s_envLightIdx);    // Scalar load.

                // If current scalar and vector light index match, we process the light. The v_envLightListOffset for current thread is increased.
                // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
                // end up with a unique v_envLightIdx value that is smaller than s_envLightIdx hence being stuck in a loop. All the active lanes will not have this problem.
                if (s_envLightIdx >= v_envLightIdx)
                {
                    v_envLightListOffset++;
                    if (reflectionHierarchyWeight < 1.0)
                    {
                        EVALUATE_BSDF_ENV(s_envLightData, REFLECTION, reflection);
                    }
                    // Refraction probe and reflection probe will process exactly the same weight. It will be good for performance to be able to share this computation
                    // However it is hard to deal with the fact that reflectionHierarchyWeight and refractionHierarchyWeight have not the same values, they are independent
                    // The refraction probe is rarely used and happen only with sphere shape and high IOR. So we accept the slow path that use more simple code and
                    // doesn't affect the performance of the reflection which is more important.
                    // We reuse LIGHTFEATUREFLAGS_SSREFRACTION flag as refraction is mainly base on the screen. Would be a waste to not use screen and only cubemap.
                    if ((featureFlags & LIGHTFEATUREFLAGS_SSREFRACTION) && (refractionHierarchyWeight < 1.0))
                    {
                        EVALUATE_BSDF_ENV(s_envLightData, REFRACTION, refraction);
                    }
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
                lightData.cookieIndex = -1;              // Enforce constant propagation

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

    // TODO(Nicholas): verify this copy-pasted code against the source.
    if (featureFlags & LIGHTFEATUREFLAGS_PROBE_VOLUME)
    {
        float probeVolumeHierarchyWeight = 0.0; // Max: 1.0
        float3 probeVolumeDiffuseLighting = 0.0;

        uint probeVolumeStart, probeVolumeCount;

        bool fastPath = false;
        // Fetch first probe volume to provide the scene proxy for screen space computation
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_PROBE_VOLUME, probeVolumeStart, probeVolumeCount);

    #if SCALARIZE_LIGHT_LOOP
        // Fast path is when we all pixels in a wave is accessing same tile or cluster.
        uint probeVolumeStartFirstLane = WaveReadLaneFirst(probeVolumeStart);
        fastPath = WaveActiveAllTrue(probeVolumeStart == probeVolumeStartFirstLane);
    #endif

#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        probeVolumeCount = _ProbeVolumeCount;
        probeVolumeStart = 0;
#endif

        // Reflection probes are sorted by volume (in the increasing order).

        // context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
    #if SCALARIZE_LIGHT_LOOP
        if (fastPath)
        {
            probeVolumeStart = probeVolumeStartFirstLane;
        }
    #endif

        // Scalarized loop, same rationale of the punctual light version
        uint v_probeVolumeListOffset = 0;
        uint v_probeVolumeIdx = probeVolumeStart;
        while (v_probeVolumeListOffset < probeVolumeCount)
        {
            v_probeVolumeIdx = FetchIndex(probeVolumeStart, v_probeVolumeListOffset);
            uint s_probeVolumeIdx = v_probeVolumeIdx;

        #if SCALARIZE_LIGHT_LOOP
            if (!fastPath)
            {
                s_probeVolumeIdx = WaveActiveMin(v_probeVolumeIdx);
                // If we are not in fast path, s_probeVolumeIdx is not scalar
               // If WaveActiveMin returns 0xffffffff it means that all lanes are actually dead, so we can safely ignore the loop and move forward.
               // This could happen as an helper lane could reach this point, hence having a valid v_lightIdx, but their values will be ignored by the WaveActiveMin
                if (s_probeVolumeIdx == -1)
                {
                    break;
                }
            }
            // Note that the WaveReadLaneFirst should not be needed, but the compiler might insist in putting the result in VGPR.
            // However, we are certain at this point that the index is scalar.
            s_probeVolumeIdx = WaveReadLaneFirst(s_probeVolumeIdx);

        #endif

            // Scalar load.
            ProbeVolumeEngineData s_probeVolumeData = _ProbeVolumeDatas[s_probeVolumeIdx];
            OrientedBBox s_probeVolumeBounds = _ProbeVolumeBounds[s_probeVolumeIdx];

            // If current scalar and vector light index match, we process the light. The v_envLightListOffset for current thread is increased.
            // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
            // end up with a unique v_envLightIdx value that is smaller than s_envLightIdx hence being stuck in a loop. All the active lanes will not have this problem.
            if (s_probeVolumeIdx >= v_probeVolumeIdx)
            {
                v_probeVolumeListOffset++;
                if (probeVolumeHierarchyWeight < 1.0)
                {
                    // TODO: Implement light layer support for probe volumes.
                    // if (IsMatchingLightLayer(s_probeVolumeData.lightLayers, builtinData.renderingLayers)) { EVALUATE_BSDF_ENV_SKY(s_probeVolumeData, TYPE, type) }

                    // TODO: Implement per-probe user defined max weight.
                    float weight = 0.0;
                    float4 sampleShAr = 0.0;
                    float4 sampleShAg = 0.0;
                    float4 sampleShAb = 0.0;
                    {
                        float3x3 obbFrame = float3x3(s_probeVolumeBounds.right, s_probeVolumeBounds.up, cross(s_probeVolumeBounds.right, s_probeVolumeBounds.up));
                        float3 obbExtents = float3(s_probeVolumeBounds.extentX, s_probeVolumeBounds.extentY, s_probeVolumeBounds.extentZ);

                        // TODO: Need to adjust tile / cluster culling code to handle this bias off the surface position.
                        // One option is to conservatively dilate the volumes in the tile / cluster culling + assignment phase based on the normal bias.
                        float3 samplePositionWS = bsdfData.normalWS * _ProbeVolumeNormalBiasWS + posInput.positionWS;
                        float3 samplePositionBS = mul(obbFrame, samplePositionWS - s_probeVolumeBounds.center);
                        float3 samplePositionBCS = samplePositionBS * rcp(obbExtents);

                        // TODO: Verify if this early out is actually improving performance.
                        bool isInsideProbeVolume = max(abs(samplePositionBCS.x), max(abs(samplePositionBCS.y), abs(samplePositionBCS.z))) < 1.0;
                        if (!isInsideProbeVolume) { continue; }

                        float3 samplePositionBNDC = samplePositionBCS * 0.5 + 0.5;

                        float fadeFactor = ProbeVolumeComputeFadeFactor(
                            samplePositionBNDC,
                            posInput.linearDepth,
                            s_probeVolumeData.rcpPosFaceFade,
                            s_probeVolumeData.rcpNegFaceFade,
                            s_probeVolumeData.rcpDistFadeLen,
                            s_probeVolumeData.endTimesRcpDistFadeLen
                        );

                        // Alpha composite: weight = (1.0f - probeVolumeHierarchyWeight) * fadeFactor;
                        weight = probeVolumeHierarchyWeight * -fadeFactor + fadeFactor;
                        if (weight > 0.0)
                        {
                            // TODO: Cleanup / optimize this math.
                            float3 probeVolumeUVW = clamp(samplePositionBNDC.xyz, 0.5 * s_probeVolumeData.resolutionInverse, 1.0 - s_probeVolumeData.resolutionInverse * 0.5);
                            float3 probeVolumeTexel3D = probeVolumeUVW * s_probeVolumeData.resolution;
                            float2 probeVolumeTexel2DBack = float2(
                                max(0.0, floor(probeVolumeTexel3D.z - 0.5)) * s_probeVolumeData.resolution.x + probeVolumeTexel3D.x,
                                probeVolumeTexel3D.y
                            );

                            float2 probeVolumeTexel2DFront = float2(
                                max(0.0, floor(probeVolumeTexel3D.z + 0.5)) * s_probeVolumeData.resolution.x + probeVolumeTexel3D.x,
                                probeVolumeTexel3D.y
                            );

                            float2 probeVolumeAtlasUV2DBack = probeVolumeTexel2DBack * _ProbeVolumeAtlasResolutionAndInverse.zw + s_probeVolumeData.scaleBias.zw;
                            float2 probeVolumeAtlasUV2DFront = probeVolumeTexel2DFront * _ProbeVolumeAtlasResolutionAndInverse.zw + s_probeVolumeData.scaleBias.zw;
                            float lerpZ = frac(probeVolumeTexel3D.z - 0.5);
                            sampleShAr = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack,  0, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 0, 0),
                                lerpZ
                            );
                            sampleShAg = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack,  1, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 1, 0),
                                lerpZ
                            );
                            sampleShAb = lerp(
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DBack,  2, 0),
                                SAMPLE_TEXTURE2D_ARRAY_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, probeVolumeAtlasUV2DFront, 2, 0),
                                lerpZ
                            );
                        }
                    }

                    float3 sampleOutgoingRadiance = SHEvalLinearL0L1(bsdfData.normalWS, sampleShAr, sampleShAg, sampleShAb);

                    // TODO: Sample irradiance data from atlas and integrate against diffuse BRDF.
                    // probeVolumeDiffuseLighting += s_probeVolumeData.debugColor * sample * weight;
                    probeVolumeDiffuseLighting += sampleOutgoingRadiance * weight * bsdfData.diffuseColor;
                    probeVolumeHierarchyWeight = probeVolumeHierarchyWeight + weight;

                }
            }
        }

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_PROBE_VOLUME)
        {
            builtinData.bakeDiffuseLighting = 0.0;
        }
    #endif

        // Lerp down any baked diffuse lighting where probe volume lighting data is present.
        // This allows us to fallback to lightmaps and / or legacy probes for distant features (such as terrain).
        // TODO: We may want to elect to fully disable the code paths + memory for baked diffuse lighting and simply anticipate a low resolution
        // global volume will always be present. Needs discussion. For now, this lerp between baked and probe volumes is the least invasive approach.
        builtinData.bakeDiffuseLighting = builtinData.bakeDiffuseLighting * (1.0 - probeVolumeHierarchyWeight) + probeVolumeDiffuseLighting;
    }

    // Also Apply indiret diffuse (GI)
    // PostEvaluateBSDF will perform any operation wanted by the material and sum everything into diffuseLighting and specularLighting
    PostEvaluateBSDF(   context, V, posInput, preLightData, bsdfData, builtinData, aggregateLighting,
                        diffuseLighting, specularLighting);

    ApplyDebug(context, posInput, bsdfData, diffuseLighting, specularLighting);
}
