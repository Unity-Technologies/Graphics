#ifndef UNITY_HD_SHADOW_LOOP_HLSL
#define UNITY_HD_SHADOW_LOOP_HLSL

void ShadowLoopMin( HDShadowContext shadowContext, PositionInputs posInput, float3 normalWS, uint featureFlags, uint renderLayer,
                        out float shadow)
{
    shadow = 1.0f;
    float shadowCount = 0.0f;

    // With XR single-pass and camera-relative: offset position to do lighting computations from the combined center view (original camera matrix).
    // This is required because there is only one list of lights generated on the CPU. Shadows are also generated once and shared between the instanced views.
    ApplyCameraRelativeXR(posInput.positionWS);

    // Initialize the contactShadow and contactShadowFade fields
    //InitContactShadow(posInput, context);

    // First of all we compute the shadow value of the directional light to reduce the VGPR pressure
    if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
    {
        // Evaluate sun shadows.
        if (_DirectionalShadowIndex >= 0)
        {
            DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

            // TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
            float3 wi = -light.forward;

            // Is it worth sampling the shadow map?
            if ((light.lightDimmer > 0) && (light.shadowDimmer > 0)
                //&& // Note: Volumetric can have different dimmer, thus why we test it here
                //IsNonZeroBSDF(V, L, preLightData, bsdfData) &&
                //!ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex)
                )
            {
                float shadowD = GetDirectionalShadowAttenuation(shadowContext,
                                                                posInput.positionSS, posInput.positionWS, normalWS,
                                                                light.shadowIndex, wi);
                shadow = min(shadow, shadowD);
                shadowCount += 1.0f;
            }
        }
    }

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
                if (IsMatchingLightLayer(s_lightData.lightLayers, renderLayer))
                {
                    //DirectLighting lighting = EvaluateBSDF_Punctual(context, V, posInput, preLightData, s_lightData, bsdfData, builtinData);
                    //AccumulateDirectLighting(lighting, aggregateLighting);
                    float3 wi;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(posInput.positionWS, s_lightData, wi, distances);
                    float shadowP = GetPunctualShadowAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, s_lightData.shadowIndex, wi, distances.x, s_lightData.lightType == GPULIGHTTYPE_POINT, s_lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
                    shadow = min(shadow, shadowP);
                    shadowCount += 1.0f;
                }
            }
        }
    }

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

        uint i;
        if (lightCount > 0)
        {
            i = 0;

            uint      last      = lightCount - 1;
            LightData lightData = FetchLight(lightStart, i);

            while (i <= last && lightData.lightType == GPULIGHTTYPE_TUBE)
            {
                lightData = FetchLight(lightStart, min(++i, last));
            }

            while (i <= last) // GPULIGHTTYPE_RECTANGLE
            {
                lightData.lightType = GPULIGHTTYPE_RECTANGLE; // Enforce constant propagation

                if (IsMatchingLightLayer(lightData.lightLayers, renderLayer))
                {
                    float shadowA = GetAreaLightAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, lightData.shadowIndex, normalize(lightData.positionRWS), length(lightData.positionRWS));
                    shadow = min(shadow, shadowA);
                    shadowCount += 1.0f;
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }
}

#endif
