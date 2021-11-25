#ifndef CAPSULE_SHADOWS_DEF
#define CAPSULE_SHADOWS_DEF

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluder.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

StructuredBuffer<CapsuleOccluderData> _CapsuleOccluderDatas;

// ref: https://www.iquilezles.org/www/articles/intersectors/intersectors.htm
float RayIntersectCapsule(float3 ro, float3 rd, float3 pa, float3 pb, float r)
{
    float3 ba = pb - pa;
    float3 oa = ro - pa;
    float baba = dot(ba,ba);
    float bard = dot(ba,rd);
    float baoa = dot(ba,oa);
    float rdoa = dot(rd,oa);
    float oaoa = dot(oa,oa);
    float a = baba      - bard*bard;
    float b = baba*rdoa - baoa*bard;
    float c = baba*oaoa - baoa*baoa - r*r*baba;
    float h = b*b - a*c;
    if( h>=0.0 )
    {
        float t = (-b-sqrt(h))/a;
        float y = baoa + t*bard;
        // body
        if( y>0.0 && y<baba ) return t;
        // caps
        float3 oc = (y<=0.0) ? oa : ro - pb;
        b = dot(rd,oc);
        c = dot(oc,oc) - r*r;
        h = b*b - c;
        if( h>0.0 ) return -b - sqrt(h);
    }
    return -1.0;
}

// ref: https://developer.amd.com/wordpress/media/2012/10/Oat-AmbientApetureLighting.pdf
float ApproximateSphericalCapIntersectionCosTheta(
    float cosThetaA,
    float cosThetaB,
    float cosBetweenAxes)
{
    float thetaA = FastACos(cosThetaA);
    float thetaB = FastACos(cosThetaB);
    float angleBetweenAxes = FastACos(cosBetweenAxes);

    float angleDiff = abs(thetaA - thetaB);
    float angleSum = thetaA + thetaB;

    float t = smoothstep(angleSum, angleDiff, angleBetweenAxes);
    return lerp(1.f, max(cosThetaA, cosThetaB), t);
}

float ApproximateConeVsSphereIntersectionCosTheta(
    float3 coneAxis,
    float coneCosTheta,
    float3 sphereCenter,
    float sphereRadius)
{
    float sphereCenterDistance = length(sphereCenter);
    float sphereSinTheta = saturate(sphereRadius/sphereCenterDistance);
    float sphereCosTheta = sqrt(max(1.f - sphereSinTheta*sphereSinTheta, 0.f));

    float cosBetweenAxes = min(dot(coneAxis, sphereCenter)/sphereCenterDistance, 1.f);

    return ApproximateSphericalCapIntersectionCosTheta(coneCosTheta, sphereCosTheta, cosBetweenAxes);
}

float2 RayVsRayClosestPoints(float3 p1, float3 d1, float3 p2, float3 d2)
{
    // p1 + t1*d1 closest point to p2 + t2*d2
    float3 n = cross(d1, d2);
    float3 n1 = cross(d1, n);
    float3 n2 = cross(d2, n);
    float t1 = dot(p2 - p1, n2)/dot(d1, n2);
    float t2 = dot(p1 - p2, n1)/dot(d2, n1);
    return float2(t1, t2);
}

float RayClosestPoint(float3 ro, float3 rd, float3 p)
{
    return dot(rd, p - ro)/dot(rd, rd);
}

float ApproximateCapsuleOcclusion(
    float3 coneOrigin,
    float3 coneAxis,
    float coneCosTheta,
    float maxDistance,
    float3 capsuleA,
    float3 capsuleB,
    float capsuleRadius)
{
    float intersectionCosTheta = 1.f;

    // find the point on the capsule axis that is closest to the ray
    float3 capsuleFromCone = capsuleA - coneOrigin;
    float3 capsuleVec = capsuleB - capsuleA;
    float t = saturate(RayVsRayClosestPoints(0.f, coneAxis, capsuleFromCone, capsuleVec).y);

    // occlude using a sphere at this point
    float3 sphereCenter = capsuleFromCone + t*capsuleVec;
    if (length(sphereCenter) < maxDistance) {
        intersectionCosTheta =
            ApproximateConeVsSphereIntersectionCosTheta(coneAxis, coneCosTheta, sphereCenter, capsuleRadius);
    }

    // return the amount the intersection occludes the cone
    return saturate((1.f - intersectionCosTheta)/(1.f - coneCosTheta));
}

float EvaluateCapsuleShadow(float3 lightPosOrAxis, bool lightIsPunctual, float lightCosTheta, PositionInputs posInput)
{
    uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_OCCLUDER, sphereStart, sphereCount);
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

    float capsuleShadow = 1.f;

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

            float3 centerRWS = s_capsuleData.centerRWS_radius.xyz;
            float radius = s_capsuleData.centerRWS_radius.w;
            float3 directionWS = s_capsuleData.directionWS_range.xyz;
            float range = s_capsuleData.directionWS_range.w;

            float3 surfaceToLightDir = lightPosOrAxis;
            float surfaceToLightDistance = FLT_MAX;
            if (lightIsPunctual) {
                float3 surfaceToLightVec = lightPosOrAxis - posInput.positionWS;
                surfaceToLightDistance = length(surfaceToLightVec);
                surfaceToLightDir = surfaceToLightVec/surfaceToLightDistance;
            }

#if 0
            float occlusion = (RayIntersectCapsule(
                posInput.positionWS,
                surfaceToLightDir,
                centerRWS - directionWS,
                centerRWS + directionWS,
                radius) > 0.f) ? 1.f : 0.f;
#else
            float occlusion = ApproximateCapsuleOcclusion(
                posInput.positionWS,
                surfaceToLightDir,
                lightCosTheta,
                surfaceToLightDistance,
                centerRWS - directionWS,
                centerRWS + directionWS,
                radius);
#endif

            float falloff = smoothstep(1.0f, 0.75f, length(posInput.positionWS - centerRWS)/range);
            capsuleShadow *= max(1.f - occlusion*falloff, 0.f);
        }
    }

    return capsuleShadow;
}

#endif
