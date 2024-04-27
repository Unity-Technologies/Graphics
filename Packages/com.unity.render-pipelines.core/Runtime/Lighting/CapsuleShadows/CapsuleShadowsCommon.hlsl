#ifndef CORE_CAPSULE_SHADOWS_COMMON_DEF
#define CORE_CAPSULE_SHADOWS_COMMON_DEF

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleShadowsFlags.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleShadowsLUT.hlsl"

#ifdef ENABLE_CAPSULE_RAY_TRACED_REFERENCE
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#endif

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
    // dot(rd, p - ro)/dot(rd, rd), but assume dot(rd, rd) == 1
    return dot(rd, p - ro);
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

float UnitDiscOcclusion(float distance, float radius)
{
    // transform the problem into overlap between a disc of radius 1 and a larger radius
    float areaScale = 1.f;
    float curvature = 1.f/radius;
    if (radius < 1.f)
    {
        areaScale = radius*radius;
        distance *= curvature;

        float tmp = curvature;
        curvature = radius;
        radius = tmp;
    }

    // sum = radius + 1.f
    // diff = radius - 1.f
    // t = saturate((distance - sum)/(diff - sum))
    float t = saturate(.5f*(1.f + radius - distance));

    float a = lerp(-1.11551f, -0.418303f, curvature);
    float b = lerp( 1.67327f,  1.0493f,   curvature);
    float c = lerp( 0.454782f, 0.373335f, curvature);
    float d = -0.0063f;

    float t2 = t*t;
    float intersectionAmount = a*t2*t + b*t2 + c*t + d;
    
    return intersectionAmount*areaScale;
}

float linearstep(float a, float b, float x)
{
    return saturate((x - b)/(a - b));
}

float EvaluateCapsuleOcclusion(
    uint flags,
    TEXTURE3D_PARAM(lutTexture, lutSampler),
    float3 lutCoordScale,
    float3 lutCoordOffset,
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
    // get normalized light direction
    float3 surfaceToLightDir;
    float lightDistance;
    if (lightIsPunctual)
    {
        lightDistance = length(surfaceToLightVec);
        surfaceToLightDir = surfaceToLightVec/lightDistance;
    }
    else
    {
        surfaceToLightDir = surfaceToLightVec;
        lightDistance = 0.f;
    }

    float occlusionStrength = 1.f;
    float interiorOcclusion = 1.f;
    {
        // apply falloff based on the max range of the shadow effect
        float closestT = RayClosestPoint(surfaceToCapsuleVec, capsuleAxisDirWS, float3(0.f, 0.f, 0.f));
        float clampedClosestT = clamp(closestT, -capsuleOffset, capsuleOffset);
        float3 surfaceToSphereVec = surfaceToCapsuleVec + clampedClosestT*capsuleAxisDirWS;
        float sphereDistance = length(surfaceToSphereVec);
        float capsuleSignedDistance = sphereDistance - capsuleRadius;
        occlusionStrength = smoothstep(shadowRange, 0.75f*shadowRange, capsuleSignedDistance);

        // compute the amount of shadowing in the interior of the capsule
        if ((flags & CAPSULE_OCCLUSION_FLAG_FADE_SELF_SHADOW) != 0) {
            float3 sdfGrad = -surfaceToSphereVec/sphereDistance;
            float nDotG = dot(normalWS, sdfGrad);
            float gDotL = dot(sdfGrad, surfaceToLightDir);
            float interiorT = sphereDistance/capsuleRadius;

            // fade out shadowing if the SDF points towards the light source (shadow from "back faces")
            interiorOcclusion *= smoothstep(0.f, -0.1f, gDotL);

            // fade out shadowing in the interior, and fade more quickly if the normal is aligned with the field
            float blendDistance = lerp(0.f, .8f, linearstep(.5f, 0.f, nDotG));
            interiorOcclusion *= smoothstep(blendDistance, .99f, interiorT);
        }
    }

    // early out before more intersection tests
    if (occlusionStrength == 0.f)
        return 0.f;

    // brute force ray traced for reference
#ifdef ENABLE_CAPSULE_RAY_TRACED_REFERENCE
    if ((flags & CAPSULE_OCCLUSION_FLAG_RAY_TRACED_REFERENCE) != 0)
    {
        if (CapsuleSignedDistance(surfaceToCapsuleVec, capsuleOffset, capsuleAxisDirWS, capsuleRadius) <= 0.f)
            return interiorOcclusion;

        uint rngState = 0xc0dec0de;
        int gridSize = 16;
        int hitCount = 0;
        float3x3 basis = GetLocalFrame(surfaceToLightDir);
        for (int y = 0; y < gridSize; ++y)
        {
            for (int x = 0; x < gridSize; ++x)
            {
    			// stratified jittered sampling of unit square
			    float2 jitter = float2(
				    UnitFloatFromHighBits(rngState & 0xffff0000),
				    UnitFloatFromHighBits(rngState << 16));
    			float2 u = (float2(x, y) + jitter)/(float)gridSize;

                // remap to light cone
                float3 dir = SampleConeUniform(u.x, u.y, lightCosTheta);

                // check ray for capsule hit
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

			    // advance RNG
			    rngState = XorShift32(rngState);
            }
        }
        return occlusionStrength*(float)hitCount/(float)(gridSize*gridSize);
    }
#endif

    // change of basis
    float3 bitangentDir;
    {
        bitangentDir = cross(surfaceToLightDir, capsuleAxisDirWS);
        float lenSq = dot(bitangentDir, bitangentDir);
        if (lenSq < .0001f)
        {
            float3x3 basis = GetLocalFrame(surfaceToLightDir);
            bitangentDir = basis[0];
        }
        else
            bitangentDir = bitangentDir*rsqrt(lenSq);
    }
    float3 tangentDir = cross(bitangentDir, surfaceToLightDir);

    surfaceToCapsuleVec = float3(
        dot(surfaceToCapsuleVec, tangentDir),
        dot(surfaceToCapsuleVec, bitangentDir),
        dot(surfaceToCapsuleVec, surfaceToLightDir));
    capsuleAxisDirWS = float3(
        dot(capsuleAxisDirWS, tangentDir),
        0.f,
        dot(capsuleAxisDirWS, surfaceToLightDir));
    surfaceToLightDir = float3(0.f, 0.f, 1.f);

    // clip the capsule to the light positive z plane
    float minT = -capsuleOffset;
    float maxT = capsuleOffset;
    {
        float lightDotAxis = capsuleAxisDirWS.z;
        float planeZ = -capsuleRadius; // clip when the capsule surface no longer crosses the light z plane
        float intersectT = clamp((planeZ - surfaceToCapsuleVec.z)/lightDotAxis, minT, maxT);
        if (lightDotAxis < 0.f) {
            maxT = intersectT;
        } else {
            minT = intersectT;
        }
    }
        
    // construct endpoints
    float3 capCenterA = surfaceToCapsuleVec + minT*capsuleAxisDirWS;
    float3 capCenterB = surfaceToCapsuleVec + maxT*capsuleAxisDirWS;

    // scale in z, adjust endpoints to preserve extent in z
    float lightConeZ = lightCosTheta;
    float lightConeXY = MatchingSinCos(lightCosTheta);
    {
        float lightDotAxis = capsuleAxisDirWS.z;
        float adjustAmount = abs(lightDotAxis*lightDotAxis*lightDotAxis);

        // want to scale: r + e/2 -> r
        float zExtent = capCenterB.z - capCenterA.z;
        float zScale = capsuleRadius/(capsuleRadius + adjustAmount*.5f*abs(zExtent));

        float radiusAdjust = (zExtent >= 0.f) ? capsuleRadius : (-capsuleRadius);
        radiusAdjust = radiusAdjust - radiusAdjust*zScale;

        capCenterA.z = zScale*capCenterA.z + radiusAdjust;
        capCenterB.z = zScale*capCenterB.z - radiusAdjust;

        // scale max distance for punctual
        if (lightIsPunctual)
            lightDistance *= zScale;

        // scale the light cone
        lightConeZ *= zScale;
    }

    // limit the light disc size
    float lightCotTheta = max(lightConeZ/lightConeXY, .25f);

    // consider the sphere at the closest point between the two axes
    float3 capVec = float3(capCenterB.x - capCenterA.x, 0.f, capCenterB.z - capCenterA.z); // capCenterA.y == capCenterB.y
    float2 closestT = RayVsRayClosestPoints(0.f, surfaceToLightDir, capCenterA, capVec);
    float3 sphereCenter = capCenterA + saturate(closestT.y)*capVec;
    float sphereDistance = length(sphereCenter);
    float sphereRadius = capsuleRadius;

    // apply falloff as the capsule goes behind the light source
    if (lightIsPunctual)
        occlusionStrength *= linearstep(0.f, sphereRadius, sphereCenter.z - lightDistance);

    // project the nearest edge of this sphere onto the light disc
    float2 sphereCenterPolar = float2(length(sphereCenter.xy), sphereCenter.z);
    float sphereConeSinTheta = min(sphereRadius/sphereDistance, 1.f);
    float sphereConeCosTheta = MatchingSinCos(sphereConeSinTheta);
    float2 sphereEdgePolar = float2(
        dot(sphereCenterPolar, float2(sphereConeCosTheta, -sphereConeSinTheta)),
        dot(sphereCenterPolar, float2(sphereConeSinTheta, sphereConeCosTheta)));

    // handle sampling from inside the closest sphere
    if (sphereConeSinTheta == 1.f)
        return interiorOcclusion;
        
    // project onto the unit disc
    if (sphereEdgePolar.y <= 0.f)
        return 0.f;
    float discEdgeCoord = sphereEdgePolar.x*lightCotTheta/sphereEdgePolar.y;
    float discRadius = 100.f;
    if (sphereCenterPolar.y > 0.f)
    {
        float discCenterCoord = sphereCenterPolar.x*lightCotTheta/sphereCenterPolar.y;
        discRadius = clamp(discCenterCoord - discEdgeCoord, 0.f, discRadius);
    }

    // account for the sphere occlusion using this disc
    float capsuleOcclusion = UnitDiscOcclusion(discEdgeCoord + discRadius, discRadius);

    // account for additional occlusion from the rest of the capsule
    if ((flags & CAPSULE_OCCLUSION_FLAG_INCLUDE_AXIS) != 0)
    {
        float d = abs(capCenterA.y);
        float h1 = abs(capVec.x)*(1.f - closestT.y);
        float h2 = abs(capVec.x)*(0.f - closestT.y);
        float r = capsuleRadius;

        float projScale = lightCotTheta/sphereCenter.z;
        d *= projScale;
        h1 *= projScale;
        h2 *= projScale;
        r *= projScale;

    #if 0
        float extraOcclusion = ExtraCapsuleShadowReference(d, h1, h2, r);
    #else
        float extraOcclusion = ExtraCapsuleShadowFromLUT(
            TEXTURE3D_ARGS(lutTexture, lutSampler),
            lutCoordScale, lutCoordOffset,
            d, h1, h2, r);
    #endif
        capsuleOcclusion += extraOcclusion;
    }

    // use the combined result
    return occlusionStrength*saturate(capsuleOcclusion);
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

float SphereAmbientOcclusion(float3 sphereCenterDir, float sphereDistance, float sphereRadius, float3 surfaceNormal)
{
    // ref: https://iquilezles.org/www/articles/sphereao/sphereao.htm
    float cosAlpha = dot(surfaceNormal, sphereCenterDir);
    return saturate(cosAlpha*Sq(sphereRadius/sphereDistance));
}

float SphereAmbientOcclusion(float3 sphereCenter, float sphereRadius, float3 surfaceNormal)
{
    float sphereDistance = length(sphereCenter);
    return SphereAmbientOcclusion(sphereCenter/sphereDistance, sphereDistance, sphereRadius, surfaceNormal);
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
#ifdef ENABLE_CAPSULE_RAY_TRACED_REFERENCE
        if ((flags & CAPSULE_AMBIENT_OCCLUSION_RAY_TRACED_REFERENCE) != 0)
        {
            uint rngState = 0xc0dec0de;
            int gridSize = 16;
            int hitCount = 0;
            float3x3 basis = GetLocalFrame(normalWS);
            for (int y = 0; y < gridSize; ++y)
            {
                for (int x = 0; x < gridSize; ++x)
                {
    			    // stratified jittered sampling of unit square
			        float2 jitter = float2(
				        UnitFloatFromHighBits(rngState & 0xffff0000),
				        UnitFloatFromHighBits(rngState << 16));
    			    float2 u = (float2(x, y) + jitter)/(float)gridSize;

                    // remap to hemisphere (cosine-weighted)
                    float3 dir = SampleHemisphereCosine(u.x, u.y);

                    // check ray for capsule hit
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

			        // advance RNG
			        rngState = XorShift32(rngState);
                }
            }
            occlusion *= (float)hitCount/(float)(gridSize*gridSize);
        }
        else
#endif
        if ((flags & CAPSULE_AMBIENT_OCCLUSION_FLAG_INCLUDE_AXIS) != 0)
        {
            // compute AO from the capsule endpoints
            float capAO0 = SphereAmbientOcclusion(surfaceToCapsuleVec - capsuleOffset*capsuleAxisDirWS, capsuleRadius, normalWS);
            float capAO1 = SphereAmbientOcclusion(surfaceToCapsuleVec + capsuleOffset*capsuleAxisDirWS, capsuleRadius, normalWS);
                
            // cosine-weighted occlusion from a thick line along the capsule axis
            float lineIntegral = LineDiffuseOcclusion(
                surfaceToCapsuleVec + closestT*capsuleAxisDirWS,
                capsuleAxisDirWS,
                -capsuleOffset - closestT,
                capsuleOffset - closestT,
                normalWS);
            float thickLineAO = capsuleRadius*lineIntegral;

            // combine the terms
            occlusion *= saturate(thickLineAO + .5f*(capAO0 + capAO1));
        }
        else
        {
            // compute AO from this closest interior sphere
            occlusion *= SphereAmbientOcclusion(surfaceToSphereVec/sphereDistance, sphereDistance, capsuleRadius, normalWS);
        }
    }
    return occlusion;
}

#endif
