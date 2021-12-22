#ifndef CORE_CAPSULE_SHADOWS_DEF
#define CORE_CAPSULE_SHADOWS_DEF

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

float ApproximateSphereOcclusion(
    float3 coneAxis,
    float coneCosTheta,
    float maxDistance,
    float3 sphereCenter,
    float sphereRadius)
{
    float sphereDistance = length(sphereCenter);

    // do not occlude when the sphere moves farther away than the light
    // TODO: make smooth
    float intersectionCosTheta = 1.f;
    if (sphereDistance < maxDistance) {
        float sphereSinTheta = saturate(sphereRadius/sphereDistance);
        float sphereCosTheta = sqrt(max(1.f - sphereSinTheta*sphereSinTheta, 0.f));

        float cosBetweenAxes = min(dot(coneAxis, sphereCenter)/sphereDistance, 1.f);

        intersectionCosTheta = ApproximateSphericalCapIntersectionCosTheta(coneCosTheta, sphereCosTheta, cosBetweenAxes);
    }

    // return the amount the intersection occludes the cone
    return saturate((1.f - intersectionCosTheta)/(1.f - coneCosTheta));
}

float ApproximateCapsuleOcclusion(
    float3 coneAxis,
    float coneCosTheta,
    float maxDistance,
    float3 capsuleStart,
    float3 capsuleVec,
    float capsuleRadius)
{
    // find the point on the capsule axis that is closest to the ray
    float t = saturate(RayVsRayClosestPoints(0.f, coneAxis, capsuleStart, capsuleVec).y);

    // occlude using a sphere at this point
    float3 sphereCenter = capsuleStart + t*capsuleVec;
    return ApproximateSphereOcclusion(coneAxis, coneCosTheta, maxDistance, sphereCenter, capsuleRadius);
}

#define CAPSULE_SHADOW_FEATURE_ELLIPSOID            0x1
#define CAPSULE_SHADOW_FEATURE_FLATTEN              0x2
#define CAPSULE_SHADOW_FEATURE_CLIP_TO_PLANE        0x4
#define CAPSULE_SHADOW_FEATURE_FADE_SELF_SHADOW     0x8

float EvaluateCapsuleOcclusion(
    uint featureBits,
    float3 surfaceToLightVec,
    bool lightIsPunctual,
    float lightCosTheta,
    float3 surfaceToCapsuleVec,
    float3 capsuleAxisDirWS,
    float capsuleOffset,
    float capsuleRadius,
    float3 normalWS)
{
    float occlusion = 1.f;

    // apply falloff to avoid self-shadowing
    // (adjusts where in the interior to fade in the shadow based on the local normal)
    if (featureBits & CAPSULE_SHADOW_FEATURE_FADE_SELF_SHADOW) {
        float t = RayClosestPoint(surfaceToCapsuleVec, capsuleAxisDirWS, float3(0.f, 0.f, 0.f));
        float3 closestCenter = surfaceToCapsuleVec + clamp(t, -capsuleOffset, capsuleOffset)*capsuleAxisDirWS;
        float closestDistance = length(closestCenter);
        float3 closestDir = closestCenter/closestDistance;
        float fadeCoord
            = closestDistance/capsuleRadius     // 0 in interior, 1 on surface
            + 0.5f*dot(normalWS, closestDir);   // -1 facing out of capsule, +1 facing into capsule
        occlusion *= smoothstep(0.6f, 0.8f, fadeCoord);
    }

    // test the occluder shape vs the light
    if (featureBits & CAPSULE_SHADOW_FEATURE_ELLIPSOID) {
        // scale down along the capsule axis to approximate the capsule with a sphere
        float3 zAxisDir = capsuleAxisDirWS;
        float zOffsetFactor = capsuleOffset/(capsuleRadius + capsuleOffset);
        surfaceToLightVec -= zAxisDir*(dot(surfaceToLightVec, zAxisDir)*zOffsetFactor);
        surfaceToCapsuleVec -= zAxisDir*(dot(surfaceToCapsuleVec, zAxisDir)*zOffsetFactor);

        // consider sphere occlusion of the light cone
        float3 surfaceToLightDir;
        float maxDistance = FLT_MAX;
        if (lightIsPunctual)
        {
            maxDistance = length(surfaceToLightVec);
            surfaceToLightDir = surfaceToLightVec/maxDistance;
        }
        else
            surfaceToLightDir = normalize(surfaceToLightVec);

        // consider sphere occlusion of the light cone
        occlusion *= ApproximateSphereOcclusion(
            surfaceToLightDir,
            lightCosTheta,
            maxDistance,
            surfaceToCapsuleVec,
            capsuleRadius);
    }
    else
    {
        // make everything relative to the surface
        float3 surfaceToLightDir;
        float maxDistance;
        if (lightIsPunctual)
        {
            maxDistance = length(surfaceToLightVec);
            surfaceToLightDir = surfaceToLightVec/maxDistance;
        }
        else
        {
            surfaceToLightDir = surfaceToLightVec;
            maxDistance = FLT_MAX;
        }

        float lightDotAxis = dot(capsuleAxisDirWS, surfaceToLightDir);

        float clippedOffset = capsuleOffset;
        if (featureBits & CAPSULE_SHADOW_FEATURE_CLIP_TO_PLANE) {
            // clip capsule to be towards the light from the surface point
            float clipMaxT = capsuleOffset;
            float clipMinT = -clipMaxT;
            float clipIntersectT = clamp(-dot(surfaceToCapsuleVec, surfaceToLightDir)/lightDotAxis, clipMinT, clipMaxT);
            if (lightDotAxis < 0.f) {
                clipMaxT = clipIntersectT;
            } else {
                clipMinT = clipIntersectT;
            }
            float clippedBias = 0.5f*(clipMaxT + clipMinT);
            float clippedOffset = 0.5f*(clipMaxT - clipMinT);
            surfaceToCapsuleVec += capsuleAxisDirWS * clippedBias;
        }
        float3 capOffsetVec = capsuleAxisDirWS * clippedOffset;

        float shearCosTheta = lightCosTheta;
        if (featureBits & CAPSULE_SHADOW_FEATURE_FLATTEN) {
            // shear the capsule along the light direction, to flatten when shadowing along length
            float3 zAxisDir = surfaceToLightDir;
            float capsuleOffsetZ = lightDotAxis*clippedOffset;
            float radiusOffsetZ = (lightDotAxis < 0.f) ? (-capsuleRadius) : capsuleRadius;
            float edgeOffsetZ = radiusOffsetZ + capsuleOffsetZ;
            float shearAmount = lightDotAxis*lightDotAxis; // could also use abs(lightDotAxis)
            float zOffsetFactor = shearAmount*capsuleOffsetZ/edgeOffsetZ;
            surfaceToCapsuleVec -= zAxisDir*(dot(surfaceToCapsuleVec, zAxisDir)*zOffsetFactor);
            capOffsetVec -= zAxisDir*(edgeOffsetZ*zOffsetFactor);

            // shear the light cone an equivalent amount
            float lightSinTheta2 = 1.f - lightCosTheta*lightCosTheta;
            shearCosTheta = lightCosTheta*(1.f - zOffsetFactor);
            shearCosTheta /= sqrt(shearCosTheta*shearCosTheta + lightSinTheta2);
        }

        // occlude using closest sphere along the sheared capsule
        occlusion *= ApproximateCapsuleOcclusion(
            surfaceToLightDir,
            shearCosTheta,
            maxDistance,
            surfaceToCapsuleVec - capOffsetVec,
            2.f*capOffsetVec,
            capsuleRadius);
    }

    return occlusion;
}

#endif
