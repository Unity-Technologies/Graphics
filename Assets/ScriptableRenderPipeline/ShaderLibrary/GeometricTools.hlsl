#ifndef UNITY_GEOMETRICTOOLS_INCLUDED
#define UNITY_GEOMETRICTOOLS_INCLUDED

//-----------------------------------------------------------------------------
// Intersection functions
//-----------------------------------------------------------------------------

// return furthest near intersection in x and closest far intersection in y
// if (intersections.y > intersections.x) the ray hit the box, else it miss it
// Assume dir is normalize
float2 BoxRayIntersect(float3 start, float3 dir, float3 boxMin, float3 boxMax)
{
    float3 invDir = 1.0 / dir;

    // Find the ray intersection with box plane
    float3 firstPlaneIntersect = (boxMin - start) * invDir;
    float3 secondPlaneIntersect = (boxMax - start) * invDir;

    // Get the closest/furthest of these intersections along the ray (Ok because x/0 give +inf and -x/0 give �inf )
    float3 closestPlane = min(firstPlaneIntersect, secondPlaneIntersect);
    float3 furthestPlane = max(firstPlaneIntersect, secondPlaneIntersect);

    float2 intersections;
    // Find the furthest near intersection
    intersections.x = max(closestPlane.x, max(closestPlane.y, closestPlane.z));
    // Find the closest far intersection
    intersections.y = min(min(furthestPlane.x, furthestPlane.y), furthestPlane.z);

    return intersections;
}

// This simplified version assume that we care about the result only when we are inside the box
// Assume dir is normalize
float BoxRayIntersectSimple(float3 start, float3 dir, float3 boxMin, float3 boxMax)
{
    float3 invDir = 1.0 / dir;

    // Find the ray intersection with box plane
    float3 rbmin = (boxMin - start) * invDir;
    float3 rbmax = (boxMax - start) * invDir;

    float3 rbminmax = (dir > 0.0) ? rbmax : rbmin;

    return min(min(rbminmax.x, rbminmax.y), rbminmax.z);
}

// Assume Sphere is at the origin (i.e start = position - spherePosition)
float2 SphereRayIntersect(float3 start, float3 dir, float radius, out bool intersect)
{
    float a = dot(dir, dir);
    float b = dot(dir, start) * 2.0;
    float c = dot(start, start) - radius * radius;
    float discriminant = b * b - 4.0 * a * c;

    float2 intersections = float2(0.0, 0.0);
    intersect = false;
    if (discriminant < 0.0 || a == 0.0)
    {
        intersections.x = 0.0;
        intersections.y = 0.0;
    }
    else
    {
        float sqrtDiscriminant = sqrt(discriminant);
        intersections.x = (-b - sqrtDiscriminant) / (2.0 * a);
        intersections.y = (-b + sqrtDiscriminant) / (2.0 * a);
        intersect = true;
    }

    return intersections;
}

// This simplified version assume that we care about the result only when we are inside the sphere
// Assume Sphere is at the origin (i.e start = position - spherePosition) and dir is normalized
// Ref: http://http.developer.nvidia.com/GPUGems/gpugems_ch19.html
float SphereRayIntersectSimple(float3 start, float3 dir, float radius)
{
    float b = dot(dir, start) * 2.0;
    float c = dot(start, start) - radius * radius;
    float discriminant = b * b - 4.0 * c;

    return abs(sqrt(discriminant) - b) * 0.5;
}

float3 RayPlaneIntersect(in float3 rayOrigin, in float3 rayDirection, in float3 planeOrigin, in float3 planeNormal)
{
    float dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

//-----------------------------------------------------------------------------
// Distance functions
//-----------------------------------------------------------------------------

// Box is AABB
float DistancePointBox(float3 position, float3 boxMin, float3 boxMax)
{
    return length(max(max(position - boxMax, boxMin - position), float3(0.0, 0.0, 0.0)));
}

#endif // UNITY_GEOMETRICTOOLS_INCLUDED
