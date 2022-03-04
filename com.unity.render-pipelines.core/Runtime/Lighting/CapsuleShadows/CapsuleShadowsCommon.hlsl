#ifndef CORE_CAPSULE_SHADOWS_COMMON_DEF
#define CORE_CAPSULE_SHADOWS_COMMON_DEF

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleOccluderFlags.cs.hlsl"

// ref: https://developer.amd.com/wordpress/media/2012/10/Oat-AmbientApetureLighting.pdf
float ApproximateSphericalCapIntersectionCosTheta(
    float cosThetaA,
    float cosThetaB,
    float cosBetweenAxes,
    bool softPartialOcclusion)
{
    float thetaA = FastACos(cosThetaA);
    float thetaB = FastACos(cosThetaB);
    float angleBetweenAxes = FastACos(cosBetweenAxes);

    float angleDiff = softPartialOcclusion ? max(thetaB - thetaA, 0.f) : abs(thetaB - thetaA);
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

float MatchingSinCos(float sinOrCosTheta)
{
    return sqrt(max(0.f, 1.f - Sq(sinOrCosTheta)));
}

float RayClosestPoint(float3 ro, float3 rd, float3 p)
{
    return dot(rd, p - ro)/dot(rd, rd);
}

bool IntersectRayCapsule(
    float3 capsuleCenter,
    float3 capsuleAxisDir,
    float capsuleOffset,
    float capsuleRadius,
    float3 rayOrigin,
    float3 rayDir,
    float rayMaxT)
{
    float3 p = rayOrigin - capsuleCenter;
    float3 d = rayDir;
    float3 n = capsuleAxisDir;
    float r = capsuleRadius;

    float pn = dot(p, n);
    float dn = dot(d, n);

    float qa = dot(d, d) - dn*dn;
    float qb = dot(d, p) - dn*pn;
    float qc = dot(p, p) - pn*pn - r*r;

    // check if we hit the infinite cylinder
    float qs = qb*qb - qa*qc;
    if (qs < 0.f)
        return false;

    // check if we hit the cylinder between the caps
    float t = (-qb - sqrt(qs))/qa;
    float k = pn + t*dn;
    if (abs(k) > capsuleOffset)
    {
        // check the spherical cap nearest the cylinder hit
        p -= capsuleAxisDir*CopySign(capsuleOffset, k, false);

        pn = dot(p, n);
        qb = dot(d, p);
        qc = dot(p, p) - r*r;

        qs = qb*qb - qc;
        if (qs < 0.f)
            return false;
        t = -qb - sqrt(qs);
    }

    // check hit distance
    return (0.f < t && t < rayMaxT);
}

/*
    Solving:

        ray:    v = p + d*t
        cone:   |v - n(v.n)| <= a(v.n)    where     a = tan(theta)

    Squaring

        v.v - (v.n)^2 <= a^2 (v.n)^2

    So:
            
        v.v - (1 + a^2)(v.n)^2 <= 0

    Let s = (1 + a^2) = (1 + tan^2(theta)) = 1/cos^2(theta)

        v.v - s(v.n)^2 <= 0

    Substitute in v = p + d*t, gather terms of t:

        (d.d - s(d.n)^2)t^2 + 2(d.p - s(d.n)(p.n))t + (p.p - s(p.n^2)) <= 0

    To solve for cone surface hits, use equality and solve quadratic.

    Returns the line segment within the cone. The line segment can be (half-)infinite,
    in which case either result.x=(-FLT_MAX) or result.y=FLT_MAX.  In all cases,
    result.x <= result.y for a hit, result.x > result.y for a miss.
 */
float2 IntersectCylinderCone(
    float3 coneApex,
    float3 coneAxisDir,
    float coneCosTheta,
    float coneSinTheta,
    float3 cylinderOrigin,
    float3 cylinderAxisDir,
    float cylinderRadius)
{
    float2 result = float2(1.f, 0.f); // miss

    float coneCosThetaRsq = 1.f/Sq(coneCosTheta);
    float3 cylinderOffsetForRadius = (cylinderRadius/coneSinTheta)*coneAxisDir;

    float3 p = cylinderOrigin + cylinderOffsetForRadius - coneApex;
    float3 d = cylinderAxisDir;
    float3 n = coneAxisDir;
    float s = coneCosThetaRsq;

    float pn = dot(p, n);
    float dn = dot(d, n);

    float qa = 1.f - s*dn*dn;
    float qb = dot(d, p) - s*dn*pn;
    float qc = dot(p, p) - s*pn*pn;

    float qs = qb*qb - qa*qc;
    if (qs > 0.f)
    {
        float tmp0 = -qb/qa;
        float tmp1 = sqrt(qs)/qa;
        float ta = tmp0 - tmp1;
        float tb = tmp0 + tmp1;

        if (tmp1 > 0.f)
        {
            if (dot(p, n) > 0.f)
                result = float2(ta, tb);
        }
        else
        {
            if (dot(d, n) > 0.f)
                result = float2(ta, FLT_MAX);
            else
                result = float2(-FLT_MAX, tb);
        }
    }
    return result;
}

float CapsuleSignedDistance(
    float3 capsuleCenter,
    float capsuleOffset,
    float3 capsuleAxisDir,
    float capsuleRadius)
{
    float closestAxisT = RayClosestPoint(capsuleCenter, capsuleAxisDir, 0.f);
    float3 closestPointOnAxis = capsuleCenter + clamp(closestAxisT, -capsuleOffset, capsuleOffset)*capsuleAxisDir;
    return length(closestPointOnAxis) - capsuleRadius;
}

float ApproximateSphereOcclusion(
    float3 coneAxis,
    float coneCosTheta,
    float maxDistance,
    float3 sphereCenter,
    float sphereRadius,
    bool softPartialOcclusion)
{
    float sphereDistance = length(sphereCenter);

    // do not occlude when the sphere moves farther away than the light
    // TODO: make smooth
    float intersectionCosTheta = 1.f;
    if (sphereDistance < maxDistance) {
        float sphereSinTheta = saturate(sphereRadius/sphereDistance);
        float sphereCosTheta = MatchingSinCos(sphereSinTheta);

        float cosBetweenAxes = min(dot(coneAxis, sphereCenter)/sphereDistance, 1.f);

        intersectionCosTheta = ApproximateSphericalCapIntersectionCosTheta(coneCosTheta, sphereCosTheta, cosBetweenAxes, softPartialOcclusion);
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
    float capsuleRadius,
    bool softPartialOcclusion)
{
    // find the point on the capsule axis that is closest to the ray
    float t = saturate(RayVsRayClosestPoints(0.f, coneAxis, capsuleStart, capsuleVec).y);

    // occlude using a sphere at this point
    float3 sphereCenter = capsuleStart + t*capsuleVec;
    return ApproximateSphereOcclusion(coneAxis, coneCosTheta, maxDistance, sphereCenter, capsuleRadius, softPartialOcclusion);
}

float EvaluateCapsuleOcclusion(
    uint flags,
    float3 surfaceToLightVec,
    bool lightIsPunctual,
    float lightCosTheta,
    float3 surfaceToCapsuleVec,
    float3 capsuleAxisDirWS,
    float capsuleOffset,
    float capsuleRadius,
    float shadowRange,
    float3 normalWS)
{
    float occlusion = 1.f;
    {
        // apply falloff based on the max range of the shadow effect
        float closestT = RayClosestPoint(surfaceToCapsuleVec, capsuleAxisDirWS, float3(0.f, 0.f, 0.f));
        float clampedClosestT = clamp(closestT, -capsuleOffset, capsuleOffset);
        float3 surfaceToSphereVec = surfaceToCapsuleVec + clampedClosestT*capsuleAxisDirWS;
        float sphereDistance = length(surfaceToSphereVec);
        float capsuleBoundaryDistance = sphereDistance - capsuleRadius;
        occlusion = smoothstep(1.0f, 0.75f, capsuleBoundaryDistance/shadowRange);

        // apply falloff to avoid self-shadowing
        // (adjusts where in the interior to fade in the shadow based on the local normal)
        if ((flags & CAPSULEOCCLUSIONFLAGS_FADE_SELF_SHADOW) != 0) {
            float interiorTerm = sphereDistance/capsuleRadius;                      // 0 in interior, 1 on surface
            float facingTerm = dot(normalWS, surfaceToSphereVec)/sphereDistance;    // -1 facing out of capsule, +1 facing into capsule
            occlusion *= smoothstep(0.6f, 0.8f, interiorTerm + 0.5f*facingTerm);
        }

        // apply falloff under the horizong
        if ((flags & CAPSULEOCCLUSIONFLAGS_FADE_AT_HORIZON) != 0) {
            float heightAboveSurface = dot(surfaceToSphereVec, normalWS) + capsuleOffset*abs(dot(capsuleAxisDirWS, normalWS)) + capsuleRadius;
            occlusion *= smoothstep(0.f, 0.25f*capsuleRadius, heightAboveSurface);
        }
    }

    // early out before more intersection tests
    if (occlusion == 0.f)
        return 0.f;

    // handle clipping
    if ((flags & (CAPSULEOCCLUSIONFLAGS_CLIP_TO_CONE | CAPSULEOCCLUSIONFLAGS_CLIP_TO_PLANE)) != 0)
    {
        float3 surfaceToLightDir = lightIsPunctual ? normalize(surfaceToLightVec) : surfaceToLightVec;
        float minT = -capsuleOffset;
        float maxT = capsuleOffset;
        if ((flags & CAPSULEOCCLUSIONFLAGS_CLIP_TO_CONE) != 0)
        {
            float lightSinTheta = MatchingSinCos(lightCosTheta);
            float2 segmentT = IntersectCylinderCone(0.f, surfaceToLightDir, lightCosTheta, lightSinTheta, surfaceToCapsuleVec, capsuleAxisDirWS, capsuleRadius);
            if (segmentT.x > segmentT.y)
                return 0.f;

            minT = clamp(segmentT.x, -capsuleOffset, capsuleOffset);
            maxT = clamp(segmentT.y, -capsuleOffset, capsuleOffset);
        }
        else // if ((flags & CAPSULEOCCLUSIONFLAGS_CLIP_TO_PLANE) != 0)
        {
            // clip capsule to be towards the light from the surface point
            float lightDotAxis = dot(surfaceToLightDir, capsuleAxisDirWS);
            float intersectT = clamp(-dot(surfaceToCapsuleVec, surfaceToLightDir)/lightDotAxis, minT, maxT);
            if (lightDotAxis < 0.f) {
                maxT = intersectT;
            } else {
                minT = intersectT;
            }
        }
        surfaceToCapsuleVec += capsuleAxisDirWS * .5f*(maxT + minT);
        capsuleOffset = .5f*(maxT - minT);
    }

    // test the occluder shape vs the light
    bool softPartialOcclusion = ((flags & CAPSULEOCCLUSIONFLAGS_SOFT_PARTIAL_OCCLUSION) != 0);
    if ((flags & CAPSULEOCCLUSIONFLAGS_RAY_TRACED_REFERENCE) != 0)
    {
        // brute force ray traced for reference
        int sampleCount = 64;
        uint hitCount = 0;
        float3x3 basis = GetLocalFrame(normalize(surfaceToLightVec));
        float goldenAngle = PI*(3.f - sqrt(5.f));
        for (int i = 0; i < sampleCount; ++i)
        {
            float t = (float)(i + .5f)/(float)sampleCount;
            float cosTheta = lerp(1.f, lightCosTheta, t);
            float sinTheta = MatchingSinCos(cosTheta);
            float phi = (float)i * goldenAngle;
            float3 dir = float3(sinTheta*cos(phi), sinTheta*sin(phi), cosTheta);
            if (IntersectRayCapsule(
                surfaceToCapsuleVec,
                capsuleAxisDirWS,
                capsuleOffset,
                capsuleRadius,
                0.f,
                normalize(mul(dir, basis)),
                shadowRange))
            {
                hitCount++;
            }
        }
        occlusion *= (float)hitCount/(float)sampleCount;
    }
    else if ((flags & CAPSULEOCCLUSIONFLAGS_CAPSULE_AXIS_SCALE) != 0)
    {
        // scale down along the capsule axis to approximate the capsule with a sphere
        float3 zAxisDir = capsuleAxisDirWS;
        float zOffsetFactor = capsuleOffset/(capsuleRadius + capsuleOffset);
        surfaceToLightVec -= zAxisDir*(dot(surfaceToLightVec, zAxisDir)*zOffsetFactor);
        surfaceToCapsuleVec -= zAxisDir*(dot(surfaceToCapsuleVec, zAxisDir)*zOffsetFactor);

        // normalize after adjustment
        float3 surfaceToLightDir = normalize(surfaceToLightVec);
        float maxDistance = lightIsPunctual ? length(surfaceToLightVec) : FLT_MAX;

        // adjust cone angle after scaling
        if ((flags & CAPSULEOCCLUSIONFLAGS_ADJUST_LIGHT_CONE_DURING_CAPSULE_AXIS_SCALE) != 0)
        {
            float absDotAxes = abs(dot(surfaceToLightDir, zAxisDir));
            float lightSin2Theta = max(1.f - Sq(lightCosTheta), 0.f);
            lightCosTheta -= lightCosTheta*absDotAxes*zOffsetFactor;
            lightCosTheta /= sqrt(lightSin2Theta + Sq(lightCosTheta));
        }

        // consider sphere occlusion of the light cone
        occlusion *= ApproximateSphereOcclusion(
            surfaceToLightDir,
            lightCosTheta,
            maxDistance,
            surfaceToCapsuleVec,
            capsuleRadius,
            softPartialOcclusion);
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

        float3 capOffsetVec = capsuleAxisDirWS * capsuleOffset;
        float shearCosTheta = lightCosTheta;
        if ((flags & CAPSULEOCCLUSIONFLAGS_LIGHT_AXIS_SCALE) != 0)
        {
            float lightDotAxis = dot(capsuleAxisDirWS, surfaceToLightDir);

            // shear the capsule along the light direction, to flatten when shadowing along length
            float3 zAxisDir = surfaceToLightDir;
            float capsuleOffsetZ = lightDotAxis*capsuleOffset;
            float radiusOffsetZ = (lightDotAxis < 0.f) ? (-capsuleRadius) : capsuleRadius;
            float edgeOffsetZ = radiusOffsetZ + capsuleOffsetZ;
            float shearAmount = lightDotAxis*lightDotAxis; // could also use abs(lightDotAxis)
            float zOffsetFactor = shearAmount*capsuleOffsetZ/edgeOffsetZ;
            surfaceToCapsuleVec -= zAxisDir*(dot(surfaceToCapsuleVec, zAxisDir)*zOffsetFactor);
            capOffsetVec -= zAxisDir*(edgeOffsetZ*zOffsetFactor);
            if (lightIsPunctual)
                maxDistance *= (1.f - zOffsetFactor);

            // shear the light cone an equivalent amount
            float lightSinTheta2 = 1.f - Sq(lightCosTheta);
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
            capsuleRadius,
            softPartialOcclusion);
    }

    return occlusion;
}

float LineDiffuseOcclusion(float3 p0, float3 wt, float t1, float t2, float3 n)
{
    /*
        Computes the amount of cosine-weighted occlusion for a surface at the
        origin with normal n, for a "thin" line.  The return value can be
        multiplied by the actual thickness for an approximation of the ambient
        occlusion from that thick line.

        parameters:
            p0: closet point to origin on infinite line
            wt: direction of the line
            t1, t2: the line endpoints, t1 <= t2
            n: the normal of the surface at the origin

        reference: Linear-Light Shading with Linearly Transformed Cosines

        optimisations applied to the approach from the paper:
            * project the out line so that p0 is at distance 1
                (if we scale the thickness and distance at the same time, the
                occlusion remains the same, so apply the same sacle to the
                return value)
            * combine the two arctan into a single one using:
                tan(a - b) = (tan(a) - tan(b))/(1 + tan(a)*tan(b))
    */

    // check horizon
    float p0DotN = dot(p0, n);
    float wtDotN = dot(wt, n);
    float h1 = p0DotN + t1*wtDotN;
    float h2 = p0DotN + t2*wtDotN;
    if (h1 <= 0.f && h2 <= 0.f)
        return 0.f;

    // clamp to horizon
    if (h1 < 0.f)
        t1 -= h1/wtDotN;
    if (h2 < 0.f)
        t2 -= h2/wtDotN;

    // project to distance 1
    float s = 1.f/length(p0);
    p0DotN *= s;
    t1 *= s;
    t2 *= s;

    // combine the two arctan terms into a single one
    float tanAngle = max(t2 - t1, 0.f)/(1.f + t2*t1);
    float absAngle = FastATanPos(abs(tanAngle));
    float angle = (tanAngle < 0.f) ? (PI - absAngle) : absAngle;

    // occlusion term with d=1
    float m1 = t1/(1.f + t1*t1);
    float m2 = t2/(1.f + t2*t2);
    float I = (m2 - m1 + angle)*p0DotN + (t2*m2 - t1*m1)*wtDotN;

    // account for the projection in the output
    return I*s/PI;
}

float EvaluateCapsuleAmbientOcclusion(
    uint flags,
    float3 surfaceToCapsuleVec,
    float3 capsuleAxisDirWS,
    float capsuleOffset,
    float capsuleRadius,
    float shadowRange,
    float3 normalWS)
{
    // get the closest position on the (infinite) capsule axis
    float closestT = RayClosestPoint(surfaceToCapsuleVec, capsuleAxisDirWS, float3(0.f, 0.f, 0.f));

    // get the closest interior sphere to the surface
    float clampedClosestT = clamp(closestT, -capsuleOffset, capsuleOffset);
    float3 surfaceToSphereVec = surfaceToCapsuleVec + clampedClosestT*capsuleAxisDirWS;
    float sphereDistance = length(surfaceToSphereVec);
    float capsuleBoundaryDistance = sphereDistance - capsuleRadius;

    // apply range-based falloff
    float occlusion = smoothstep(1.0f, 0.75f, capsuleBoundaryDistance/shadowRange);
    if (occlusion > 0.f)
    {
        // compute AO from this closest interior sphere
        // ref: https://iquilezles.org/www/articles/sphereao/sphereao.htm
        float3 surfaceToSphereDir = surfaceToSphereVec/sphereDistance;
        float cosAlpha = dot(normalWS, surfaceToSphereDir);
        float sphereAO = saturate(cosAlpha*Sq(capsuleRadius/sphereDistance));

        if ((flags & CAPSULEAMBIENTOCCLUSIONFLAGS_INCLUDE_AXIS) != 0)
        {
            // cosine-weighted occlusion from a thick line along the capsule axis
            float lineIntegral = LineDiffuseOcclusion(
                surfaceToCapsuleVec + closestT*capsuleAxisDirWS,
                capsuleAxisDirWS,
                -capsuleOffset - closestT,
                capsuleOffset - closestT,
                normalWS);
            float thickLineAO = capsuleRadius*lineIntegral;

            // assume that 50% of the sphere occlusion is independent of the line (for long capsules with hemispherical caps)
            // but ensure that the result is always at least at much as only using the sphere (for short capsules)
            occlusion *= clamp(thickLineAO + 0.5f*sphereAO, sphereAO, 1.f);
        }
        else
        {
            occlusion *= sphereAO;
        }
    }
    return occlusion;
}

#endif
