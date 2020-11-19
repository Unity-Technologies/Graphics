#ifndef UNITY_HD_SHADOW_LOOP_HLSL
#define UNITY_HD_SHADOW_LOOP_HLSL

//#define SHADOW_LOOP_MULTIPLY
//#define SHADOW_LOOP_AVERAGE

void ShadowLoopMin(HDShadowContext shadowContext, PositionInputs posInput, float3 normalWS, uint featureFlags, uint renderLayer,
                        out float3 shadow)
{
    float weight      = 0.0f;
    float shadowCount = 0.0f;

#ifdef SHADOW_LOOP_MULTIPLY
    shadow = float3(1, 1, 1);
#elif defined(SHADOW_LOOP_AVERAGE)
    shadow = float3(0, 0, 0);
#else
    shadow = float3(1, 1, 1);
#endif

    // TODO: may want to make 'tile' wave-uniform.
    uint tile = ComputeTileIndex(posInput.positionSS);
    uint zBin = ComputeZBinIndex(posInput.linearDepth);

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
            DirectionalLightData light = _DirectionalLightData[_DirectionalShadowIndex];

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
                shadowCount += 1.0f;
                weight      += 1.0f - shadowD;
            }
        }
    }

    uint i; // Declare once to avoid the D3D11 compiler warning.

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        i = 0;

        LightData lightData;
        while (TryLoadPunctualLightData(i, tile, zBin, lightData))
        {
            if (IsMatchingLightLayer(lightData.lightLayers, renderLayer) &&
                lightData.shadowIndex >= 0 &&
                lightData.shadowDimmer > 0)
            {
                float shadowP;
                float3 L;
                float4 distances; // {d, d^2, 1/d, d_proj}
                GetPunctualLightVectors(posInput.positionWS, lightData, L, distances);
                float distToLight = (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
                float lightRadSqr = lightData.size.x;
                if (distances.x < lightData.range &&
                    PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias,
                                                        lightData.angleScale,            lightData.angleOffset) > 0.0 &&
                    L.y > 0.0)
                {
                    shadowP = GetPunctualShadowAttenuation(shadowContext, posInput.positionSS, posInput.positionWS, normalWS, lightData.shadowIndex, L, distances.x, lightData.lightType == GPULIGHTTYPE_POINT, lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
                    shadowP = lightData.nonLightMappedOnly ? min(1.0f, shadowP) : shadowP;
                    shadowP = lerp(1.0f, shadowP, lightData.shadowDimmer);

#ifdef SHADOW_LOOP_MULTIPLY
                    shadow *= lerp(lightData.shadowTint, float3(1, 1, 1), shadowP);
#elif defined(SHADOW_LOOP_AVERAGE)
                    shadow += lerp(lightData.shadowTint, float3(1, 1, 1), shadowP);
#else
                    shadow = min(shadow, shadowP.xxx);
#endif
                    shadowCount += 1.0f;
                    weight      += 1.0f - shadowP;
                }
            }

            i++;
        }
    }

    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        i = 0;

        LightData lightData;
        while (TryLoadAreaLightData(i, tile, zBin, lightData))
        {
            if (lightData.lightType == GPULIGHTTYPE_RECTANGLE)
            {
                if (IsMatchingLightLayer(lightData.lightLayers, renderLayer))
                {
                    float3 L;
                    float4 distances; // {d, d^2, 1/d, d_proj}
                    GetPunctualLightVectors(posInput.positionWS, lightData, L, distances);
                    float distToLight = (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
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
                        shadowCount += 1.0f;
                        weight      += 1.0f - shadowA;
                    }
                }
            }
            else // GPULIGHTTYPE_TUBE
            {
                /* ??? */
            }

            i++;
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
#else
    //shadow = (1.0f - saturate(shadowCount)).xxx;
    //shadow = (1.0f - saturate(weight)).xxx;
#endif
}

#endif
