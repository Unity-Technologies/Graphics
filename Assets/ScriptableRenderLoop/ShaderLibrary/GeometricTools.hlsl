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

// TODO: Describe difference with above
// AND compare with intersections.y
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

// Sphere is at the origin
bool SphereRayIntersect(out float2 intersections, float3 start, float3 dir, float radius)
{
    float a = dot(dir, dir);
    float b = dot(dir, start) * 2.0;
    float c = dot(start, start) - radius * radius;
    float discriminant = b * b - 4.0 * a * c;

    bool intersect = false;
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

    return intersect;
}

float3 RayPlaneIntersect(in float3 rayOrigin, in float3 rayDirection, in float3 planeOrigin, in float3 planeNormal)
{
    float dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

//-----------------------------------------------------------------------------
// Distance functions
//-----------------------------------------------------------------------------

/*
// Ref: real time detection collision page 131
float DistancePointBox(float3 pos, float3 boxMin, float3 boxMax)
{
    // Clamp to find closest point then calc distance
    float3 distanceToMin = pos < boxMin ? (boxMin - pos) * (boxMin - pos) : 0.0;
    float3 distancesToMax = pos > boxMax ? (pos - boxMax) * (pos - boxMax) : 0.0;

    float distanceSquare = dot(distanceToMin, float3(1.0, 1.0, 1.0)) + dot(distancesToMax, float3(1.0, 1.0, 1.0));
    return sqrt(distanceSquare);
}
*/

// TODO: check that this code is effectively equivalent to code above (it should)
// Box is AABB
float DistancePointBox(float3 pos, float3 boxMin, float3 boxMax)
{
    return length(max(max(pos - boxMax, boxMin - pos), float3(0.0, 0.0, 0.0)));
}

#endif // UNITY_GEOMETRICTOOLS_INCLUDED
