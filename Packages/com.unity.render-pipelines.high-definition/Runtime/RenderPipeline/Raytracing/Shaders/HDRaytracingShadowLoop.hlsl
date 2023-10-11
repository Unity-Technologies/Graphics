#ifndef UNITY_HD_RAYTRACING_SHADOW_LOOP_HLSL
#define UNITY_HD_RAYTRACING_SHADOW_LOOP_HLSL

#define USE_LIGHT_CLUSTER

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingLightCluster.hlsl"

//#define SHADOW_LOOP_MULTIPLY
//#define SHADOW_LOOP_AVERAGE

#if defined(SHADOW_LOOP_MULTIPLY) || defined(SHADOW_LOOP_AVERAGE)
#define SHADOW_LOOP_WEIGHT
#endif

void ShadowLoopMin(HDShadowContext shadowContext, PositionInputs posInput, float3 normalWS, uint featureFlags, uint renderLayer, out float3 shadow)
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
                // If we are in the ray tracing case, we want to be able to have the directional shadow fallback.
                int shadowSplitIndex;
                SHADOW_TYPE shadowD = EvalShadow_CascadedDepth_Dither_SplitIndex(shadowContext, _ShadowmapCascadeAtlas, s_linear_clamp_compare_sampler, posInput.positionSS, posInput.positionWS, normalWS, light.shadowIndex, wi, shadowSplitIndex);
                if (shadowSplitIndex < 0.0)
                    shadowD = _DirectionalShadowFallbackIntensity;

#ifdef SHADOW_LOOP_MULTIPLY
                shadow *= lerp(light.shadowTint, float3(1, 1, 1), shadowD);
#elif defined(SHADOW_LOOP_AVERAGE)
                shadow += lerp(light.shadowTint, float3(1, 1, 1), shadowD);
#else
                shadow = min(shadow, shadowD.SHADOW_TYPE_SWIZZLE);
#endif
#ifdef SHADOW_LOOP_WEIGHT
                shadowCount += 1.0f;
#endif
            }
        }
    }

    // Indices of the subranges to process
    uint lightStart = 0, lightEnd = 0;
    uint cellIndex;

    // Index used to loop over the lights
    uint i = 0;

    // The light cluster is in actual world space coordinates,
    #ifdef USE_LIGHT_CLUSTER
    // Get the actual world space position
    float3 actualWSPos = posInput.positionWS;
    #endif

    if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
    {
        #ifdef USE_LIGHT_CLUSTER
        // Get the punctual light count
        GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_PUNCTUAL, lightStart, lightEnd, cellIndex);
        #else
        lightStart = 0;
        lightEnd = _WorldPunctualLightCount;
        #endif

        for (i = lightStart; i < lightEnd; i++)
        {
            #ifdef USE_LIGHT_CLUSTER
            LightData lightData = FetchClusterLightIndex(cellIndex, i);
            #else
            LightData lightData = _WorldLightDatas[i];
            #endif
            if (IsMatchingLightLayer(lightData.lightLayers, renderLayer) &&
                        lightData.shadowIndex >= 0 &&
                        lightData.shadowDimmer > 0)
            {
                float shadowP;
                float3 L;
                float4 distances; // {d, d^2, 1/d, d_proj}
                GetPunctualLightVectors(posInput.positionWS, lightData, L, distances);

                // Projector lights (box, pyramid) always have cookies, so we can perform clipping inside the if().
                float lightinBounds = 1.0;
                if (lightData.lightType == GPULIGHTTYPE_PROJECTOR_PYRAMID || lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
                {
                    float3 lightToSample = posInput.positionWS - lightData.positionRWS;
                    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, lightData.forward);
                    float3 positionLS   = mul(lightToSample, transpose(lightToWorld));

                    // Perform orthographic or perspective projection.
                    float  perspectiveZ = (lightData.lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1.0;
                    float2 positionCS   = positionLS.xy / perspectiveZ;

                    float z = positionLS.z;
                    float r = lightData.range;

                    // Box lights have no range attenuation, so we must clip manually.
                    lightinBounds = Max3(abs(positionCS.x), abs(positionCS.y), abs(z - 0.5 * r) - 0.5 * r + 1) <= lightData.boxLightSafeExtent ?  1 : 0;
                }

                if (distances.x < lightData.range
                    && PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias, lightData.angleScale, lightData.angleOffset) > 0.0
                    && lightinBounds > 0.0
                    && L.y > 0.0)
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
    #ifdef SHADOW_LOOP_WEIGHT
                    shadowCount += 1.0f;
    #endif
                }
            }
        }
}

    if (featureFlags & LIGHTFEATUREFLAGS_AREA)
    {
        #ifdef USE_LIGHT_CLUSTER
        // Let's loop through all the
        GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_AREA, lightStart, lightEnd, cellIndex);
        #else
        lightStart = _WorldPunctualLightCount;
        lightEnd = _WorldPunctualLightCount + _WorldAreaLightCount;
        #endif

        for (i = lightStart; i < lightEnd; i++)
        {
            #ifdef USE_LIGHT_CLUSTER
            LightData lightData = FetchClusterLightIndex(cellIndex, i);
            #else
            LightData lightData = _WorldLightDatas[i];
            #endif
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

#endif // UNITY_HD_RAYTRACING_SHADOW_LOOP_HLSL
