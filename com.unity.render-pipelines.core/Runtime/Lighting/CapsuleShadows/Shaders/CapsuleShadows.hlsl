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

/*
    Description of flag bits for capsule direct shadows.  These bits
    control how the capsule-vs-light-cone occlusion problem is transformed
    into a cone-vs-cone overlap test, which is then approximated used
    ApproximateSphericalCapIntersectionCosTheta() above.

    The main flag is ELLIPSOID.
    * If specified, then the capsule is replaced with an interior
    ellipsoid, and space is then scaled along the long axis to transform
    this into sphere.
    * If not specified, then the closest interior sphere to the light
    axis is chosen as the occluder.

    Other flags are:
    * FLATTEN: only applies if ELLIPSOID is *not* used.  Before choosing
    an interior sphere, scale (down) space along the light z axis as the
    capsule axis and light axis become aligned.  This avoids the closest
    sphere from moving quickly in between the two ends of the capsule.

    * CLIP_TO_PLANE: only applies if ELLIPSOID is *not* used.  Clips the
    capsule to the plane above the surface as a first step.  Can help to
    avoid missing shadowing in some cases.

    * FADE_SELF_SHADOW: fades out the occlusion effect if the surface
    is likely to be approximated by *this* capsule.  Uses a heuristic
    based on how close the surface is to the capsule surface and
    capsule surface normal.
*/
#define CAPSULE_SHADOW_FLAG_ELLIPSOID           0x1
#define CAPSULE_SHADOW_FLAG_FLATTEN             0x2
#define CAPSULE_SHADOW_FLAG_CLIP_TO_PLANE       0x4
#define CAPSULE_SHADOW_FLAG_FADE_SELF_SHADOW    0x8

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
        if (flags & CAPSULE_SHADOW_FLAG_FADE_SELF_SHADOW) {
            float interiorTerm = sphereDistance/capsuleRadius;                      // 0 in interior, 1 on surface
            float facingTerm = dot(normalWS, surfaceToSphereVec)/sphereDistance;    // -1 facing out of capsule, +1 facing into capsule
            occlusion *= smoothstep(0.6f, 0.8f, interiorTerm + 0.5f*facingTerm);
        }
    }
    // early out before more intersection tests
    if (occlusion == 0.f)
        return 0.f;

    // test the occluder shape vs the light
    if (flags & CAPSULE_SHADOW_FLAG_ELLIPSOID) {
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
        if (flags & CAPSULE_SHADOW_FLAG_CLIP_TO_PLANE) {
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
        if (flags & CAPSULE_SHADOW_FLAG_FLATTEN) {
            // shear the capsule along the light direction, to flatten when shadowing along length
            float3 zAxisDir = surfaceToLightDir;
            float capsuleOffsetZ = lightDotAxis*clippedOffset;
            float radiusOffsetZ = (lightDotAxis < 0.f) ? (-capsuleRadius) : capsuleRadius;
            float edgeOffsetZ = radiusOffsetZ + capsuleOffsetZ;
            float shearAmount = lightDotAxis*lightDotAxis; // could also use abs(lightDotAxis)
            float zOffsetFactor = shearAmount*capsuleOffsetZ/edgeOffsetZ;
            surfaceToCapsuleVec -= zAxisDir*(dot(surfaceToCapsuleVec, zAxisDir)*zOffsetFactor);
            capOffsetVec -= zAxisDir*(edgeOffsetZ*zOffsetFactor);
            if (lightIsPunctual)
                maxDistance *= (1.f - zOffsetFactor);

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

#define CAPSULE_AMBIENT_OCCLUSION_FLAG_WITH_LINE    0x1

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

        if (flags & CAPSULE_AMBIENT_OCCLUSION_FLAG_WITH_LINE)
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
