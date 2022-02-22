#ifndef CAPSULE_SHADOWS_LIGHT_LOOP_DEF
#define CAPSULE_SHADOWS_LIGHT_LOOP_DEF

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleShadows.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleShadowsGlobals.hlsl"

StructuredBuffer<CapsuleOccluderData> _CapsuleOccluders;
TEXTURE2D_ARRAY(_CapsuleShadowsTexture);

float EvaluateCapsuleDirectShadowLightLoop(
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

    uint flags = GetCapsuleDirectOcclusionFlags();

    uint capsuleCount, capsuleStart;
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_DIRECT_SHADOW, capsuleStart, capsuleCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    capsuleCount = _CapsuleDirectShadowCount; 
    capsuleStart = 0;
#endif

    bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
    uint capsuleStartLane0;
    fastPath = IsFastPath(capsuleStart, capsuleStartLane0);
    if (fastPath)
        capsuleStart = capsuleStartLane0;
#endif

    // Scalarized loop. All capsules that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_capsuleListOffset = 0;
    uint v_capsuleIdx = capsuleStart;

    float visibility = 1.f;
    while (v_capsuleListOffset < capsuleCount)
    {
        v_capsuleIdx = FetchIndex(capsuleStart, v_capsuleListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_capsuleIdx = ScalarizeElementIndex(v_capsuleIdx, fastPath);
#else
        uint s_capsuleIdx = v_capsuleIdx;
#endif
        if (s_capsuleIdx == -1)
            break;

        CapsuleOccluderData s_capsuleData = _CapsuleOccluders[s_capsuleIdx];

        // If current scalar and vector capsule index match, we process the capsule. The v_capsuleListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_capsuleIdx value that is smaller than s_capsuleIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_capsuleIdx >= v_capsuleIdx)
        {
            v_capsuleListOffset++;

            if (IsMatchingLightLayer(GetCapsuleLayerMask(s_capsuleData), renderLayer))
            {
                float occlusion = EvaluateCapsuleOcclusion(
                    flags,
                    surfaceToLightVec,
                    lightIsPunctual,
                    lightCosTheta,
                    s_capsuleData.centerRWS - posInput.positionWS,
                    s_capsuleData.axisDirWS,
                    s_capsuleData.offset,
                    s_capsuleData.radius,
                    shadowRange,
                    normalWS);

                // combine visibility by multiplying term from each capsule
                visibility *= max(1.f - occlusion, 0.f);
            }
        }
    }
    return visibility;
}

float EvaluateCapsuleAmbientOcclusionLightLoop(
    PositionInputs posInput,
    float3 normalWS)
{
    uint flags = GetCapsuleAmbientOcclusionFlags();

    uint capsuleCount, capsuleStart;
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_INDIRECT_SHADOW, capsuleStart, capsuleCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    capsuleCount = _CapsuleIndirectShadowCount; 
    capsuleStart = 0;
#endif

    bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
    uint capsuleStartLane0;
    fastPath = IsFastPath(capsuleStart, capsuleStartLane0);
    if (fastPath)
        capsuleStart = capsuleStartLane0;
#endif

    // Scalarized loop. All capsules that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_capsuleListOffset = 0;
    uint v_capsuleIdx = capsuleStart;

    float visibility = 1.f;
    while (v_capsuleListOffset < capsuleCount)
    {
        v_capsuleIdx = FetchIndex(capsuleStart, v_capsuleListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_capsuleIdx = ScalarizeElementIndex(v_capsuleIdx, fastPath);
#else
        uint s_capsuleIdx = v_capsuleIdx;
#endif
        if (s_capsuleIdx == -1)
            break;

        CapsuleOccluderData s_capsuleData = _CapsuleOccluders[_CapsuleDirectShadowCount + s_capsuleIdx];

        // If current scalar and vector capsule index match, we process the capsule. The v_capsuleListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_capsuleIdx value that is smaller than s_capsuleIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_capsuleIdx >= v_capsuleIdx)
        {
            v_capsuleListOffset++;

            float occlusion = EvaluateCapsuleAmbientOcclusion(
                flags,
                s_capsuleData.centerRWS - posInput.positionWS,
                s_capsuleData.axisDirWS,
                s_capsuleData.offset,
                s_capsuleData.radius,
                s_capsuleData.radius*_CapsuleIndirectRangeFactor,
                normalWS);

            // combine visibility by multiplying term from each capsule
            visibility *= max(1.f - occlusion, 0.f);
        }
    }
    return visibility;
}

float EvaluateCapsuleIndirectShadowLightLoop(
    float3 overrideLightDir,
    bool useOverride,
    float lightCosTheta,
    PositionInputs posInput,
    float3 normalWS)
{
    uint flags = GetCapsuleIndirectOcclusionFlags();

    uint capsuleCount, capsuleStart;
#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_INDIRECT_SHADOW, capsuleStart, capsuleCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    capsuleCount = _CapsuleIndirectShadowCount; 
    capsuleStart = 0;
#endif

    bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
    uint capsuleStartLane0;
    fastPath = IsFastPath(capsuleStart, capsuleStartLane0);
    if (fastPath)
        capsuleStart = capsuleStartLane0;
#endif

    // Scalarized loop. All capsules that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
    // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
    // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
    // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
    // Note that the above is valid only if wave intriniscs are supported.
    uint v_capsuleListOffset = 0;
    uint v_capsuleIdx = capsuleStart;

    float visibility = 1.f;
    while (v_capsuleListOffset < capsuleCount)
    {
        v_capsuleIdx = FetchIndex(capsuleStart, v_capsuleListOffset);
#if SCALARIZE_LIGHT_LOOP
        uint s_capsuleIdx = ScalarizeElementIndex(v_capsuleIdx, fastPath);
#else
        uint s_capsuleIdx = v_capsuleIdx;
#endif
        if (s_capsuleIdx == -1)
            break;

        CapsuleOccluderData s_capsuleData = _CapsuleOccluders[_CapsuleDirectShadowCount + s_capsuleIdx];

        // If current scalar and vector capsule index match, we process the capsule. The v_capsuleListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_capsuleIdx value that is smaller than s_capsuleIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_capsuleIdx >= v_capsuleIdx)
        {
            v_capsuleListOffset++;

            float3 lightDir = useOverride ? overrideLightDir : s_capsuleData.indirectDirWS;
            float occlusion = EvaluateCapsuleOcclusion(
                flags,
                lightDir,
                false,
                lightCosTheta,
                s_capsuleData.centerRWS - posInput.positionWS,
                s_capsuleData.axisDirWS,
                s_capsuleData.offset,
                s_capsuleData.radius,
                s_capsuleData.radius*_CapsuleIndirectRangeFactor,
                normalWS);

            // combine visibility by multiplying term from each capsule
            visibility *= max(1.f - occlusion, 0.f);
        }
    }
    return visibility;
}

#endif
