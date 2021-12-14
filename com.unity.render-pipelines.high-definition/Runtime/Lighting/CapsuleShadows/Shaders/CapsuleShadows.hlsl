#ifndef CAPSULE_SHADOWS_DEF
#define CAPSULE_SHADOWS_DEF

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleOccluder.cs.hlsl"
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
#define CAPSULE_SHADOW_FEATURE_AVOID_SELF_SHADOW    0x8

uint GetDefaultCapsuleShadowFeatureBits()
{
    uint featureBits = CAPSULE_SHADOW_FEATURE_AVOID_SELF_SHADOW;
    switch (_CapsuleOccluderShadowMethod) {
    case CAPSULESHADOWMETHOD_ELLIPSOID:
        featureBits |= CAPSULE_SHADOW_FEATURE_ELLIPSOID;
        break;
    case CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE:
        featureBits |= CAPSULE_SHADOW_FEATURE_FLATTEN;
        break;
    }
    return featureBits;
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

            if (IsMatchingLightLayer(s_capsuleData.lightLayers, renderLayer))
            {
                float3 surfaceToCapsuleVec = s_capsuleData.centerRWS - posInput.positionWS;

                // apply range-based falloff
                float occlusion = smoothstep(1.0f, 0.75f, length(surfaceToCapsuleVec)/shadowRange);

                // apply falloff to avoid self-shadowing
                // (adjusts where in the interior to fade in the shadow based on the local normal)
                if (featureBits & CAPSULE_SHADOW_FEATURE_AVOID_SELF_SHADOW) {
                    float t = RayClosestPoint(surfaceToCapsuleVec, s_capsuleData.axisDirWS, float3(0.f, 0.f, 0.f));
                    float3 closestCenter = surfaceToCapsuleVec + clamp(t, -s_capsuleData.offset, s_capsuleData.offset)*s_capsuleData.axisDirWS;
                    float closestDistance = length(closestCenter);
                    float3 closestDir = closestCenter/closestDistance;
                    float fadeCoord
                        = closestDistance/s_capsuleData.radius  // 0 in interior, 1 on surface
                        + 0.5f*dot(normalWS, closestDir);       // -1 facing out of capsule, +1 facing into capsule
                    occlusion *= smoothstep(0.6f, 0.8f, fadeCoord);
                }

                // test the occluder shape vs the light
                if (occlusion > 0.f) {
                    if (featureBits & CAPSULE_SHADOW_FEATURE_ELLIPSOID) {
                        // make everything relative to the surface
                        float3 surfaceToLightVec = lightPosOrAxis;
                        if (lightIsPunctual)
                            surfaceToLightVec -= posInput.positionWS;

                        // scale down along the capsule axis to approximate the capsule with a sphere
                        float3 zAxisDir = s_capsuleData.axisDirWS;
                        float zOffsetFactor = s_capsuleData.offset/(s_capsuleData.radius + s_capsuleData.offset);
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
                            s_capsuleData.radius);
                    }
                    else
                    {
                        // make everything relative to the surface
                        float3 surfaceToLightDir;
                        float maxDistance = FLT_MAX;
                        if (lightIsPunctual)
                        {
                            surfaceToLightDir = lightPosOrAxis - posInput.positionWS;
                            maxDistance = length(surfaceToLightDir);
                            surfaceToLightDir /= maxDistance;
                        }
                        else
                            surfaceToLightDir = lightPosOrAxis;

                        float lightDotAxis = dot(s_capsuleData.axisDirWS, surfaceToLightDir);

                        float clippedOffset = s_capsuleData.offset;
                        if (featureBits & CAPSULE_SHADOW_FEATURE_CLIP_TO_PLANE) {
                            // clip capsule to be towards the light from the surface point
                            float clipMaxT = s_capsuleData.offset;
                            float clipMinT = -clipMaxT;
                            float clipIntersectT = clamp(-dot(surfaceToCapsuleVec, surfaceToLightDir)/lightDotAxis, clipMinT, clipMaxT);
                            if (lightDotAxis < 0.f) {
                                clipMaxT = clipIntersectT;
                            } else {
                                clipMinT = clipIntersectT;
                            }
                            float clippedBias = 0.5f*(clipMaxT + clipMinT);
                            float clippedOffset = 0.5f*(clipMaxT - clipMinT);
                            surfaceToCapsuleVec += s_capsuleData.axisDirWS * clippedBias;
                        }
                        float3 capOffsetVec = s_capsuleData.axisDirWS * clippedOffset;

                        float shearCosTheta = lightCosTheta;
                        if (featureBits & CAPSULE_SHADOW_FEATURE_FLATTEN) {
                            // shear the capsule along the light direction, to flatten when shadowing along length
                            float3 zAxisDir = surfaceToLightDir;
                            float capsuleOffsetZ = lightDotAxis*clippedOffset;
                            float radiusOffsetZ = (lightDotAxis < 0.f) ? (-s_capsuleData.radius) : s_capsuleData.radius;
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
                            s_capsuleData.radius);
                    }
                }

                capsuleShadow *= max(1.f - occlusion, 0.f);
            }
        }
    }

    return capsuleShadow;
}

#endif
