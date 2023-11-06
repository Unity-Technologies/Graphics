#ifndef __CLOUDUTILS_H__
#define __CLOUDUTILS_H__

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyCommon.hlsl"

// Returns true if the ray intersects the cloud volume
// Outputs the entry and exit distance from the volume
bool IntersectCloudVolume(float3 originPS, float3 dir, float lowerBoundPS, float higherBoundPS, out float tEntry, out float tExit)
{
    bool intersect;
    float radialDistance = length(originPS);
    float rcpRadialDistance = rcp(radialDistance);
    float cosChi = dot(originPS, dir) * rcpRadialDistance;
    float2 tInner = IntersectSphere(lowerBoundPS, cosChi, radialDistance, rcpRadialDistance);
    float2 tOuter = IntersectSphere(higherBoundPS, cosChi, radialDistance, rcpRadialDistance);

    if (tInner.x < 0.0 && tInner.y >= 0.0) // Below the lower bound
    {
        // The ray starts at the intersection with the lower bound and ends at the intersection with the outer bound
        tEntry = tInner.y;
        tExit = tOuter.y;
        // We don't see the clouds if they are behind Earth
        intersect = cosChi >= ComputeCosineOfHorizonAngle(radialDistance);
    }
    else // Inside or above the cloud volume
    {
        // The ray starts at the intersection with the outer bound, or at 0 if we are inside
        // The ray ends at the lower bound if we hit it, at the outer bound otherwise
        tEntry = max(tOuter.x, 0.0f);
        tExit = tInner.x >= 0.0 ? tInner.x : tOuter.y;
        // We don't see the clouds if we don't hit the outer bound
        intersect = tOuter.y >= 0.0f;
    }

    return intersect;
}

// Returns true if the ray exits the cloud volume (doesn't intersect earth)
// The ray is supposed to start inside the volume
bool ExitCloudVolume(float3 originPS, float3 dir, float higherBoundPS, out float tExit)
{
    // Given that we are inside the volume, we are guaranteed to exit at the outer bound
    float radialDistance = length(originPS);
    float cosChi = dot(originPS, dir) * rcp(radialDistance);
    tExit = IntersectSphere(higherBoundPS, cosChi, radialDistance, rcp(radialDistance)).y;

    // If the ray intersects the earth, then the sun is occluded by the earth
    return cosChi >= ComputeCosineOfHorizonAngle(radialDistance);
}

#endif // __CLOUDUTILS_H__
