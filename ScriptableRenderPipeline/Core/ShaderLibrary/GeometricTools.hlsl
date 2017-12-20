#ifndef UNITY_GEOMETRICTOOLS_INCLUDED
#define UNITY_GEOMETRICTOOLS_INCLUDED

//-----------------------------------------------------------------------------
// Intersection functions
//-----------------------------------------------------------------------------

// return furthest near intersection in x and closest far intersection in y
// if (intersections.y > intersections.x) the ray hit the box, else it miss it
// Assume dir is normalize
REAL2 BoxRayIntersect(REAL3 start, REAL3 dir, REAL3 boxMin, REAL3 boxMax)
{
    REAL3 invDir = 1.0 / dir;

    // Find the ray intersection with box plane
    REAL3 firstPlaneIntersect = (boxMin - start) * invDir;
    REAL3 secondPlaneIntersect = (boxMax - start) * invDir;

    // Get the closest/furthest of these intersections along the ray (Ok because x/0 give +inf and -x/0 give �inf )
    REAL3 closestPlane = min(firstPlaneIntersect, secondPlaneIntersect);
    REAL3 furthestPlane = max(firstPlaneIntersect, secondPlaneIntersect);

    REAL2 intersections;
    // Find the furthest near intersection
    intersections.x = max(closestPlane.x, max(closestPlane.y, closestPlane.z));
    // Find the closest far intersection
    intersections.y = min(min(furthestPlane.x, furthestPlane.y), furthestPlane.z);

    return intersections;
}

// This simplified version assume that we care about the result only when we are inside the box
// Assume dir is normalize
REAL BoxRayIntersectSimple(REAL3 start, REAL3 dir, REAL3 boxMin, REAL3 boxMax)
{
    REAL3 invDir = 1.0 / dir;

    // Find the ray intersection with box plane
    REAL3 rbmin = (boxMin - start) * invDir;
    REAL3 rbmax = (boxMax - start) * invDir;

    REAL3 rbminmax = (dir > 0.0) ? rbmax : rbmin;

    return min(min(rbminmax.x, rbminmax.y), rbminmax.z);
}

// Assume Sphere is at the origin (i.e start = position - spherePosition)
REAL2 SphereRayIntersect(REAL3 start, REAL3 dir, REAL radius, out bool intersect)
{
    REAL a = dot(dir, dir);
    REAL b = dot(dir, start) * 2.0;
    REAL c = dot(start, start) - radius * radius;
    REAL discriminant = b * b - 4.0 * a * c;

    REAL2 intersections = REAL2(0.0, 0.0);
    intersect = false;
    if (discriminant < 0.0 || a == 0.0)
    {
        intersections.x = 0.0;
        intersections.y = 0.0;
    }
    else
    {
        REAL sqrtDiscriminant = sqrt(discriminant);
        intersections.x = (-b - sqrtDiscriminant) / (2.0 * a);
        intersections.y = (-b + sqrtDiscriminant) / (2.0 * a);
        intersect = true;
    }

    return intersections;
}

// This simplified version assume that we care about the result only when we are inside the sphere
// Assume Sphere is at the origin (i.e start = position - spherePosition) and dir is normalized
// Ref: http://http.developer.nvidia.com/GPUGems/gpugems_ch19.html
REAL SphereRayIntersectSimple(REAL3 start, REAL3 dir, REAL radius)
{
    REAL b = dot(dir, start) * 2.0;
    REAL c = dot(start, start) - radius * radius;
    REAL discriminant = b * b - 4.0 * c;

    return abs(sqrt(discriminant) - b) * 0.5;
}

REAL3 RayPlaneIntersect(in REAL3 rayOrigin, in REAL3 rayDirection, in REAL3 planeOrigin, in REAL3 planeNormal)
{
    REAL dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

//-----------------------------------------------------------------------------
// Miscellaneous functions
//-----------------------------------------------------------------------------

// Box is AABB
REAL DistancePointBox(REAL3 position, REAL3 boxMin, REAL3 boxMax)
{
    return length(max(max(position - boxMax, boxMin - position), REAL3(0.0, 0.0, 0.0)));
}

REAL3 ProjectPointOnPlane(REAL3 position, REAL3 planePosition, REAL3 planeNormal)
{
    return position - (dot(position - planePosition, planeNormal) * planeNormal);
}

// Plane equation: {(a, b, c) = N, d = -dot(N, P)}.
// Returns the distance from the plane to the point 'p' along the normal.
// Positive -> in front (above), negative -> behind (below).
REAL DistanceFromPlane(REAL3 p, REAL4 plane)
{
    return dot(REAL4(p, 1.0), plane);
}

// Returns 'true' if the triangle is outside of the frustum.
// 'epsilon' is the (negative) distance to (outside of) the frustum below which we cull the triangle.
bool CullTriangleFrustum(REAL3 p0, REAL3 p1, REAL3 p2, REAL epsilon, REAL4 frustumPlanes[6], int numPlanes)
{
    bool outside = false;

    for (int i = 0; i < numPlanes; i++)
    {
        // If all 3 points are behind any of the planes, we cull.
        outside = outside || Max3(DistanceFromPlane(p0, frustumPlanes[i]),
                                  DistanceFromPlane(p1, frustumPlanes[i]),
                                  DistanceFromPlane(p2, frustumPlanes[i])) < epsilon;
    }

    return outside;
}

// Returns 'true' if the edge of the triangle is outside of the frustum.
// The edges are defined s.t. they are on the opposite side of the point with the given index.
// 'epsilon' is the (negative) distance to (outside of) the frustum below which we cull the triangle.
bool3 CullTriangleEdgesFrustum(REAL3 p0, REAL3 p1, REAL3 p2, REAL epsilon, REAL4 frustumPlanes[6], int numPlanes)
{
    bool3 edgesOutside = false;

    for (int i = 0; i < numPlanes; i++)
    {
        bool3 pointsOutside = bool3(DistanceFromPlane(p0, frustumPlanes[i]) < epsilon,
                                    DistanceFromPlane(p1, frustumPlanes[i]) < epsilon,
                                    DistanceFromPlane(p2, frustumPlanes[i]) < epsilon);

        // If both points of the edge are behind any of the planes, we cull.
        edgesOutside.x = edgesOutside.x || (pointsOutside.y && pointsOutside.z);
        edgesOutside.y = edgesOutside.y || (pointsOutside.x && pointsOutside.z);
        edgesOutside.z = edgesOutside.z || (pointsOutside.x && pointsOutside.y);
    }

    return edgesOutside;
}

// Returns 'true' if a triangle defined by 3 vertices is back-facing.
// 'epsilon' is the (negative) value of dot(N, V) below which we cull the triangle.
// 'winding' can be used to change the order: pass 1 for (p0 -> p1 -> p2), or -1 for (p0 -> p2 -> p1).
bool CullTriangleBackFace(REAL3 p0, REAL3 p1, REAL3 p2, REAL epsilon, REAL3 viewPos, REAL winding)
{
    REAL3 edge1 = p1 - p0;
    REAL3 edge2 = p2 - p0;

    REAL3 N     = cross(edge1, edge2);
    REAL3 V     = viewPos - p0;
    REAL  NdotV = dot(N, V) * winding;

    // Optimize:
    // NdotV / (length(N) * length(V)) < Epsilon
    // NdotV < Epsilon * length(N) * length(V)
    // NdotV < Epsilon * sqrt(dot(N, N)) * sqrt(dot(V, V))
    // NdotV < Epsilon * sqrt(dot(N, N) * dot(V, V))
    return NdotV < epsilon * sqrt(dot(N, N) * dot(V, V));
}

#endif // UNITY_GEOMETRICTOOLS_INCLUDED
