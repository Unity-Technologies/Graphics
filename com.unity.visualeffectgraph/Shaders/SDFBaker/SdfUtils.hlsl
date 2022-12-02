#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define BARY_EPS 1e-5
#define CONSERVATIVE_RASTER_EPS 1e-6
#define INTERSECT_EPS 0

float dot2(float3 v)
{
    return dot(v, v);
}

//Triangle is a reserved word in PSSL, use "Tri" instead
struct Tri
{
    float3 a, b, c;
};

float3 computeNormalUnnormalized(float3 a, float3 b, float3 c)
{
    float3 e0 = b - a;
    float3 e1 = c - b;
    return cross(e0, e1);
}

//compiler shows warning when using intermediate returns, disable this.
#pragma warning(push)
#pragma warning(disable : 4000)
float IntersectSegmentTriangle(float3 p, float3 q, Tri tri, out float t_out)
{
    float3 ab = tri.b - tri.a;
    float3 ac = tri.c - tri.a;
    float3 qp = p - q;
    // Compute triangle normal.
    float3 n = cross(ab, ac);

    float d = dot(qp, n);
    float ood = 1.0f / d;

    if (d <= 0)
    {
        t_out = 1e10;
        return 0;
    }
    // Compute intersection t value of pq with plane of triangle. A ray
    // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
    // dividing by d until intersection has been found to pierce triangle

    float3 ap = p - tri.a;
    float t = dot(ap, n) * ood;

    if (t < -INTERSECT_EPS)
    {
        t_out = 1e10;
        return 0;
    }
    if (t > 1 + INTERSECT_EPS)
    {
        t_out = 1e10;
        return 0;
    }
    // Compute barycentric coordinate components and test if within bounds
    float3 e = cross(qp, ap);
    float v = dot(ac, e) * ood;
    float edgeCoeff = 1.0f; // is 0.5f if the intersection in on an edge
    if (v < -BARY_EPS || v > 1 + BARY_EPS)
    {
        t_out = 1e10;
        return 0;

    }
    float w = -dot(ab, e) * ood;
    if (w < -BARY_EPS || v + w > 1 + BARY_EPS)
    {
        t_out = 1e10;
        return 0;
    }
    float u = 1 - v - w;
    if (abs(u) < BARY_EPS || abs(v) < BARY_EPS || abs(w) < BARY_EPS)
    {
        edgeCoeff = 0.5f;
    }

    t_out = t; //Writes t_out only if all the other tests passed
    return 1.0f * edgeCoeff;
}
#pragma warning(pop)

int3 GenerateNeighborOffset(int iNeighbour, float maxSize, float distToSurface)
{
    float u = 2.0f * GenerateHashedRandomFloat(iNeighbour) - 1;
    float phi = 2.0f * PI * GenerateHashedRandomFloat(iNeighbour + 1);
    float r = pow(GenerateHashedRandomFloat(iNeighbour + 2), 1.0f / 3.0f) * max(1.0f , distToSurface * float(maxSize));

    float C = sqrt(1 - u * u);
    float s, c;
    sincos(phi, s, c);

    float x = r * c * C;
    float y = r * s * C;
    float z = r * u;

    return int3(x, y, z);
}

float ComputeDistancePointTri(float3 p, Tri tri)
{
    // prepare data
    float3 v21 = tri.b - tri.a; float3 p1 = p - tri.a;
    float3 v32 = tri.c - tri.b; float3 p2 = p - tri.b;
    float3 v13 = tri.a - tri.c; float3 p3 = p - tri.c;
    float3 nor = cross(v21, v13);

    return sqrt( // inside/outside test
        (sign(dot(cross(v21, nor), p1)) +
            sign(dot(cross(v32, nor), p2)) +
            sign(dot(cross(v13, nor), p3)) < 2.0f)
        ?
        // 3 edges
        min(min(
            dot2(v21 * clamp(dot(v21, p1) / dot2(v21), 0.0, 1.0) - p1),
            dot2(v32 * clamp(dot(v32, p2) / dot2(v32), 0.0, 1.0) - p2)),
            dot2(v13 * clamp(dot(v13, p3) / dot2(v13), 0.0, 1.0) - p3))
        :
        // 1 face
        dot(nor, p1) * dot(nor, p1) / dot2(nor));
}
