#ifndef UNITY_HD_SHADOW_LOOP_HLSL
#define UNITY_HD_SHADOW_LOOP_HLSL

//#define SHADOW_LOOP_MULTIPLY
//#define SHADOW_LOOP_AVERAGE

#if defined(SHADOW_LOOP_MULTIPLY) || defined(SHADOW_LOOP_AVERAGE)
#define SHADOW_LOOP_WEIGHT
#endif

void ShadowLoopMin(HDShadowContext shadowContext, PositionInputs posInput, float3 normalWS, uint featureFlags, uint renderLayer,
                        out float3 shadow)
{
#ifdef SHADOW_LOOP_WEIGHT
    float shadowCount = 0.0f;
#endif

#ifdef SHADOW_LOOP_MULTIPLY
    shadow = float3(1, 1, 1);
#elif defined(SHADOW_LOOP_AVERAGE)
    shadow = float3(0, 0, 0);
#else
    shadow = float3(1, 1, 1);
#endif

    // With XR single-pass and camera-relative: offset position to do lighting computations from the combined center view (original camera matrix).
    // This is required because there is only one list of lights generated on the CPU. Shadows are also generated once and shared between the instanced views.
    ApplyCameraRelativeXR(posInput.positionWS);

    // Initialize the contactShadow and contactShadowFade fields

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
            if (light.lightDimmer > 0 && light.shadowDimmer > 0)
            {
                float shadowD = GetDirectionalShadowAttenuation(shadowContext,
                                                                posInput.positionSS, posInput.positionWS, normalWS,
                                                                light.shadowIndex, wi);
#ifdef SHADOW_LOOP_MULTIPLY
                shadow *= lerp(light.shadowTint, float3(1, 1, 1), shadowD);
#elif defined(SHADOW_LOOP_AVERAGE)
                shadow += lerp(light.shadowTint, float3(1, 1, 1), shadowD);
#else
                shadow = min(shadow, shadowD.xxx);
#endif
#ifdef SHADOW_LOOP_WEIGHT
                shadowCount += 1.0f;
#endif
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
        uint lightStartLane0;
        fastPath = IsFastPath(lightStart, lightStartLane0);

        if (fastPath)
        {
            lightStart = lightStartLane0;
        }

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
                if (IsMatchingLightLayer(s_lightData.lightLayers, renderLayer) &&
                    s_lightData.shadowIndex >= 0 &&
                    s_lightData.shadowDimmer > 0)
                {
                    float shadowP;
                    float3 L;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(posInput.positionWS, s_lightData, L, distances);
                    float lightRadSqr = s_lightData.size.x;
                    if (distances.x < s_lightData.range &&
                        PunctualLightAttenuation(distances, s_lightData.rangeAttenuationScale, s_lightData.rangeAttenuationBias,
                                                            s_lightData.angleScale,            s_lightData.angleOffset) > 0.0 &&
                        L.y > 0.0)
                    {
                        shadowP = GetPunctualShadowAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, s_lightData.shadowIndex, L, distances.x, s_lightData.lightType == GPULIGHTTYPE_POINT, s_lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
                        shadowP = s_lightData.nonLightMappedOnly ? min(1.0f, shadowP) : shadowP;
                        shadowP = lerp(1.0f, shadowP, s_lightData.shadowDimmer);

#ifdef SHADOW_LOOP_MULTIPLY
                        shadow *= lerp(s_lightData.shadowTint, float3(1, 1, 1), shadowP);
#elif defined(SHADOW_LOOP_AVERAGE)
                        shadow += lerp(s_lightData.shadowTint, float3(1, 1, 1), shadowP);
#else
                        shadow = min(shadow, shadowP.xxx);
#endif
#ifdef SHADOW_LOOP_WEIGHT
                        shadowCount += 1.0f;
#endif
                    }
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
                    float3 L;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(posInput.positionWS, lightData, L, distances);
                    float lightRadSqr = lightData.size.x;
                    float shadowP;

                    float coef = 0.0f;
                    float3 unL = lightData.positionRWS - posInput.positionWS;
                    if (dot(lightData.forward, unL) < FLT_EPS)
                    {
                        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
                        unL = mul(unL, transpose(lightToWorld));

                        float halfWidth   = lightData.size.x*0.5;
                        float halfHeight  = lightData.size.y*0.5;

                        float  range      = lightData.range;
                        float3 invHalfDim = rcp(float3(range + halfWidth,
                                                       range + halfHeight,
                                                       range));

                        coef = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                                   lightData.rangeAttenuationScale,
                                                                   lightData.rangeAttenuationBias);
                    }

                    if (distances.x < lightData.range && coef > 0.0)
                    {
                        float shadowA = GetRectAreaShadowAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, lightData.shadowIndex, normalize(lightData.positionRWS), length(lightData.positionRWS));

#ifdef SHADOW_LOOP_MULTIPLY
                        shadow *= lerp(lightData.shadowTint, float3(1, 1, 1), shadowA);
#elif defined(SHADOW_LOOP_AVERAGE)
                        shadow += lerp(lightData.shadowTint, float3(1, 1, 1), shadowA);
#else
                        shadow = min(shadow, shadowA.xxx);
#endif
#ifdef SHADOW_LOOP_WEIGHT
                        shadowCount += 1.0f;
#endif
                    }
                }

                lightData = FetchLight(lightStart, min(++i, last));
            }
        }
    }
#ifdef SHADOW_LOOP_MULTIPLY
    if (shadowCount == 0.0f)
    {
        shadow = float3(1, 1, 1);
    }
#elif defined(SHADOW_LOOP_AVERAGE)
    if (shadowCount > 0.0f)
    {
        shadow /= shadowCount;
    }
    else
    {
        shadow = float3(1, 1, 1);
    }
#endif
}

#endif
