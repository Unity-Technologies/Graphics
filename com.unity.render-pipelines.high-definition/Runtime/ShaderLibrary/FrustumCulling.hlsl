#ifndef FRUSTUM_CULLING_H
#define FRUSTUM_CULLING_H

float3 GetForward(OrientedBBox value)
{
    return cross(value.up, value.right);
}

struct FrustumPlane
{
    float3 normal;
    float dist;
};

struct Frustum
{
    FrustumPlane planes[6];
    // Needs to be aligned on a float4, a bit of waste here
    float4 corners[8];
};

bool CheckOverlap(OrientedBBox obb, float3 planeNormal, float planeDistance)
{
    // Max projection of the half-diagonal onto the normal (always positive).
    float maxHalfDiagProj = obb.extentX * abs(dot(planeNormal, obb.right))
        + obb.extentY * abs(dot(planeNormal, obb.up))
        + obb.extentZ * abs(dot(planeNormal, GetForward(obb)));

    // Positive distance -> center in front of the plane.
    // Negative distance -> center behind the plane (outside).
    float centerToPlaneDist = dot(planeNormal, obb.center) + planeDistance;

    // outside = maxHalfDiagProj < -centerToPlaneDist
    // outside = maxHalfDiagProj + centerToPlaneDist < 0
    // overlap = overlap && !outside
    return (maxHalfDiagProj + centerToPlaneDist >= 0);
}

int FrustumOBBIntersection(OrientedBBox obb, FrustumGPU frustum)
{
    // Test the OBB against frustum planes. Frustum planes are inward-facing.
    // The OBB is outside if it's entirely behind one of the frustum planes.
    // See "Real-Time Rendering", 3rd Edition, 16.10.2.
    bool overlap = CheckOverlap(obb, frustum.normal0, frustum.dist0);
    overlap = overlap && CheckOverlap(obb, frustum.normal1, frustum.dist1);
    overlap = overlap && CheckOverlap(obb, frustum.normal2, frustum.dist2);
    overlap = overlap && CheckOverlap(obb, frustum.normal3, frustum.dist3);
    overlap = overlap && CheckOverlap(obb, frustum.normal4, frustum.dist4);
    overlap = overlap && CheckOverlap(obb, frustum.normal5, frustum.dist5);

    // Test the frustum corners against OBB planes. The OBB planes are outward-facing.
    // The frustum is outside if all of its corners are entirely in front of one of the OBB planes.
    // See "Correct Frustum Culling" by Inigo Quilez.
    // We can exploit the symmetry of the box by only testing against 3 planes rather than 6.
    FrustumPlane planes[3];
    planes[0].normal = obb.right;
    planes[0].dist = obb.extentX;
    planes[1].normal = obb.up;
    planes[1].dist = obb.extentY;
    planes[2].normal = GetForward(obb);
    planes[2].dist = obb.extentZ;

    for (int i = 0; overlap && i < 3; i++)
    {
        // We need a separate counter for the "box fully inside frustum" case.
        bool outsidePos = true; // Positive normal
        bool outsideNeg = true; // Reversed normal
        float proj = 0.0;

        // Merge 2 loops. Continue as long as all points are outside either plane.
        // Corner 0
        proj = dot(planes[i].normal, frustum.corner0.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 1
        proj = dot(planes[i].normal, frustum.corner1.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 2
        proj = dot(planes[i].normal, frustum.corner2.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 3
        proj = dot(planes[i].normal, frustum.corner3.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 4
        proj = dot(planes[i].normal, frustum.corner4.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 5
        proj = dot(planes[i].normal, frustum.corner5.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 6
        proj = dot(planes[i].normal, frustum.corner6.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Corner 7
        proj = dot(planes[i].normal, frustum.corner7.xyz - obb.center);
        outsidePos = outsidePos && (proj > planes[i].dist);
        outsideNeg = outsideNeg && (-proj > planes[i].dist);

        // Combine data of the previous plane
        overlap = overlap && !(outsidePos || outsideNeg);
    }

    return overlap ? 1 : 0;
}
#endif // FRUSTUM_CULLING_H
