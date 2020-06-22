#ifndef CAPSULE_OCCLUSION_COMMON_DEF
#define CAPSULE_OCCLUSION_COMMON_DEF

#if !defined(USE_FPTL_LIGHTLIST) && !defined(USE_CLUSTERED_LIGHTLIST)
    #define USE_FPTL_LIGHTLIST // Use light tiles for contact shadows
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOcclusionManager.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/EllipsoidOccluder.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

// --------------------------------------------
// Occluder data helpers
// --------------------------------------------
float3 GetOccluderPositionRWS(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.xyz;
}

float GetOccluderRadius(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.w;
}

float3 GetOccluderDirectionWS(EllipsoidOccluderData data)
{
    return data.directionWS_scaling.xyz;
}

float GetOccluderScaling(EllipsoidOccluderData data)
{
    return data.directionWS_scaling.w;
}

// --------------------------------------------
// Data preparation functions
// --------------------------------------------
float4 GetDataForSphereIntersection(EllipsoidOccluderData data)
{
    // TODO : Fill with transformations needed so the rest of the code deals with simple spheres.
    // xyz should be un-normalized direction, w should contain the length.
    float3 dir = 0;
    float len = 0;
    return float4(dir.x, dir.y, dir.z, len);
}

// --------------------------------------------
// Evaluation functions
// --------------------------------------------
// These functions should evaluate the occlusion types. Note that all of these functions take EllipsoidOccluderData containing the shape to evaluate against
// and a dirAndLength containing the data output by the function GetDataForSphereIntersection()

float EvaluateCapsuleAmbientOcclusion(EllipsoidOccluderData data, float3 positionWS, float3 N, float4 dirAndLength)
{
    // IMPORTANT: Remember to modify by intensity modifier here and not after.
    return 1.0f;
}

float EvaluateCapsuleSpecularOcclusion(EllipsoidOccluderData data, float3 positionWS, float3 N, float roughness, float4 dirAndLength)
{
    return 1.0f;
}

float EvaluateCapsuleShadow(EllipsoidOccluderData data, float3 positionWS, float3 N, float4 dirAndLength)
{
    return 1.0f;
}

// --------------------------------------------
// Accumulation functions
// --------------------------------------------
// Functions used to accumulate results coming from different capsules.
// Min should be a safe bet, but abstract away in case more complex accumulation is required.

float AccumulateCapsuleAmbientOcclusion(float prevAO, float capsuleAO)
{
    return min(prevAO, capsuleAO);
}

float AccumulateCapsuleSpecularOcclusion(float prevSpecOcc, float capsuleSpecOcc)
{
    return min(prevSpecOcc, capsuleSpecOcc);
}

float AccumulateCapsuleShadow(float prevShadow, float capsuleShadow)
{
    return min(prevShadow, capsuleShadow);
}

// --------------------------------------------
// Main evaluation function
// --------------------------------------------
// This is the main loop through the capsule data. To change the intersection behaviour just modify the functions above.
// Should be responsability of the caller to avoid calling this when evaluationFlags == CAPSULEOCCLUSIONTYPE_NONE

void EvaluateCapsuleOcclusion(uint evaluationFlags,
                              PositionInputs posInput,
                              float3 N,
                              float roughness,
                              out float ambientOcclusion,
                              out float specularOcclusion,
                              out float shadow)
{
    uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_SPHERE_OCCLUDER, sphereStart, sphereCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    sphereCount = /* TO ADD FIXED COUNT */ ; 
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

    while (v_sphereListOffset < sphereCount)
    {
        v_sphereIdx = FetchIndex(sphereStart, v_sphereListOffset);
        uint s_sphereIdx = ScalarizeElementIndex(v_sphereIdx, fastPath);
        if (s_sphereIdx == -1)
            break;

        // TODO! Sample from the right data structure here whenever we have it available. 
        EllipsoidOccluderData s_capsuleData = /* FetchEllipsoidOccluderData(s_sphereIdx); */ (EllipsoidOccluderData)0;

        // If current scalar and vector sphere index match, we process the sphere. The v_sphereListOffset for current thread is increased.
        // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
        // end up with a unique v_sphereIdx value that is smaller than s_sphereIdx hence being stuck in a loop. All the active lanes will not have this problem.
        if (s_sphereIdx >= v_sphereIdx)
        {
            v_sphereListOffset++;

            float4 dirAndLen = GetDataForSphereIntersection(s_capsuleData);

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_AMBIENT_OCCLUSION)
            {
                float capsuleAO = EvaluateCapsuleAmbientOcclusion(s_capsuleData, posInput.positionWS, N, dirAndLen);
                ambientOcclusion = AccumulateCapsuleAmbientOcclusion(ambientOcclusion, capsuleAO);
            }

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_SPECULAR_OCCLUSION)
            {
                float capsuleSpecOcc = EvaluateCapsuleSpecularOcclusion(s_capsuleData, posInput.positionWS, N, roughness, dirAndLen);
                specularOcclusion = AccumulateCapsuleSpecularOcclusion(specularOcclusion, capsuleSpecOcc);
            }

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_DIRECTIONAL_SHADOWS)
            {
                float capsuleShadow = EvaluateCapsuleShadow(s_capsuleData, posInput.positionWS, N, dirAndLen);
                shadow = AccumulateCapsuleShadow(shadow, capsuleShadow);
            }
        }
    }
}

#endif
