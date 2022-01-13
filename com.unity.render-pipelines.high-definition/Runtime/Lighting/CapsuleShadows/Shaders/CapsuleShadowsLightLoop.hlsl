#ifndef CAPSULE_SHADOWS_LIGHT_LOOP_DEF
#define CAPSULE_SHADOWS_LIGHT_LOOP_DEF

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleShadows.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluderData.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

StructuredBuffer<CapsuleOccluderData> _CapsuleOccluderDatas;

uint GetDefaultCapsuleShadowFeatureBits()
{
#ifdef DEBUG_DISPLAY
    uint featureBits = 0;
    switch (_DebugCapsuleShadowMethod) {
    case CAPSULESHADOWMETHOD_ELLIPSOID:
        featureBits |= CAPSULE_SHADOW_FEATURE_ELLIPSOID;
        break;
    case CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE:
        featureBits |= CAPSULE_SHADOW_FEATURE_FLATTEN;
        break;
    }
    if (_DebugCapsuleFadeSelfShadow) {
        featureBits |= CAPSULE_SHADOW_FEATURE_FADE_SELF_SHADOW;
    }
    return featureBits;
#else
    return CAPSULE_SHADOW_FEATURE_FLATTEN | CAPSULE_SHADOW_FEATURE_FADE_SELF_SHADOW;
#endif
}

float EvaluateCapsuleShadow(
    float3 lightPosOrAxis,
    bool lightIsPunctual,
    float lightCosTheta,
    float shadowRange,
    PositionInputs posInput,
    float3 normalWS,
    uint renderLayer)
{
    float3 surfaceToLightVec = lightPosOrAxis;
    if (lightIsPunctual)
        surfaceToLightVec -= posInput.positionWS;

    uint featureBits = GetDefaultCapsuleShadowFeatureBits();

    uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_OCCLUDER, sphereStart, sphereCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    sphereCount = _CapsuleOccluderCount; 
    sphereStart = 0;
#endif

    bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
    uint sphereStartLane0;
    fastPath = IsFastPath(sphereStart, sphereStartLane0);

    if (fastPath)
    {
        sphereStart = sphereStartLane0;
    }
#endif

    // Scalarized loop. All spheres that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_sphereListOffset = 0;
    uint v_sphereIdx = sphereStart;

    float visibility = 1.f;
    while (v_sphereListOffset < sphereCount)
    {
        v_sphereIdx = FetchIndex(sphereStart, v_sphereListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_sphereIdx = ScalarizeElementIndex(v_sphereIdx, fastPath);
#else
        uint s_sphereIdx = v_sphereIdx;
#endif
        if (s_sphereIdx == -1)
            break;

        CapsuleOccluderData s_capsuleData = _CapsuleOccluderDatas[s_sphereIdx];

        // If current scalar and vector sphere index match, we process the sphere. The v_sphereListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_sphereIdx value that is smaller than s_sphereIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_sphereIdx >= v_sphereIdx)
        {
            v_sphereListOffset++;

            if (IsMatchingLightLayer(s_capsuleData.lightLayers, renderLayer))
            {
                float3 surfaceToCapsuleVec = s_capsuleData.centerRWS - posInput.positionWS;

                // apply range-based falloff
                float occlusion = smoothstep(1.0f, 0.75f, length(surfaceToCapsuleVec)/shadowRange);

                // amount the light source is occluded by the capsule shape
                if (occlusion > 0.f) {
                    occlusion *= EvaluateCapsuleOcclusion(
                        featureBits,
                        surfaceToLightVec,
                        lightIsPunctual,
                        lightCosTheta,
                        surfaceToCapsuleVec,
                        s_capsuleData.axisDirWS,
                        s_capsuleData.offset,
                        s_capsuleData.radius,
                        normalWS);
                }

                // combine visibility by multiplying term from each capsule
                visibility *= max(1.f - occlusion, 0.f);
            }
        }
    }
    return visibility;
}

#endif
