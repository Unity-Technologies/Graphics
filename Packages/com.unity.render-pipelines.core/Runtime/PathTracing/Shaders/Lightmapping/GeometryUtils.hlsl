// Given a triangle edge defined by 2 verts, calculate 2 new verts that define the same edge,
// pushed outwards enough to cover centroids of any pixels which may intersect the triangle.
void OffsetEdge(float2 v1, float2 v2, float2 pixelSize, out float2 v1Offset, out float2 v2Offset)
{
    // Find the normal of the edge
    float2 edge = v2 - v1;
    float2 normal = normalize(float2(-edge.y, edge.x));

    // Find the amount to offset by. This is the semidiagonal of the pixel box in the same quadrant as the normal.
    float2 semidiagonal = pixelSize / sqrt(2.0);
    semidiagonal *= float2(normal.x > 0 ? 1 : -1, normal.y > 0 ? 1 : -1);

    // Offset the edge
    v1Offset = v1 + semidiagonal;
    v2Offset = v2 + semidiagonal;
}

// Given 2 lines defined by 2 points each, find the intersection point.
float2 LineIntersect(float2 p1, float2 p2, float2 p3, float2 p4)
{
    // Line p1p2 represented as a1x + b1y = c1
    float a1 = p2.y - p1.y;
    float b1 = p1.x - p2.x;
    float c1 = a1 * p1.x + b1 * p1.y;

    // Line p3p4 represented as a2x + b2y = c2
    float a2 = p4.y - p3.y;
    float b2 = p3.x - p4.x;
    float c2 = a2 * p3.x + b2 * p3.y;

    float determinant = a1 * b2 - a2 * b1;
    if (determinant == 0) // Parallel lines - return any valid point.
        return p1;

    float x = b2 * c1 - b1 * c2;
    float y = a1 * c2 - a2 * c1;
    return float2(x, y) / determinant;
}

// Check which side the point p is on, relative to the line cp1-cp2.
// Optionally with an epsilon value within which points are considered outside.
bool IsInside(float2 p, float2 cp1, float2 cp2, float eps = 0.0f)
{
    return (cp2.x - cp1.x) * (p.y - cp1.y) - (cp2.y - cp1.y) * (p.x - cp1.x) >= eps;
}

// Clip a polygon with a line defined by two points, in place.
// https://en.wikipedia.org/wiki/Sutherland-Hodgman_algorithm
void ClipPolygonWithLine(inout float2 polygon[6], inout uint polygonLength, float2 cp1, float2 cp2)
{
    float2 result[6];
    uint resultLength = 0;

    for (uint i = 0; i < polygonLength; i++)
    {
        float2 s = polygon[i];
        float2 e = polygon[(i + 1) % polygonLength];

        if (IsInside(e, cp1, cp2)) // At least one endpoint is inside
        {
            if (!IsInside(s, cp1, cp2)) // Only the end point is inside, add the intersection
            {
                result[resultLength] = LineIntersect(cp1, cp2, s, e);
                resultLength++;
            }
            result[resultLength] = e;
            resultLength++;
        }
        else if (IsInside(s, cp1, cp2)) // Only the start point is inside, add the intersection
        {
            result[resultLength] = LineIntersect(cp1, cp2, s, e);
            resultLength++;
        }
    }

    polygonLength = resultLength;
    for (uint p = 0; p < polygonLength; p++)
        polygon[p] = result[p];
}

// Clip a polygon to the bounding box of a texel at the given position
bool ClipPolygonWithTexel(float2 texelPosition, inout float2 polygon[6], inout uint polygonLength)
{
    // Get the bounding box of the texel to clip with.
    float2 clipPolygon[4] =
    {
        texelPosition + float2(-0.5, -0.5),
        texelPosition + float2(0.5, -0.5),
        texelPosition + float2(0.5, 0.5),
        texelPosition + float2(-0.5, 0.5),
    };

    // Clip to each edge of the quad
    for (uint edge = 0; edge < 4; edge++)
    {
        ClipPolygonWithLine(polygon, polygonLength, clipPolygon[edge], clipPolygon[(edge + 1) % 4]);
    }

    return polygonLength > 0;
}

// Read the parent triangle of a vertex from a vertex buffer, and transform it to the desired space.
void ReadParentTriangle(StructuredBuffer<float2> vertexBuffer, uint vertexId, float4 scaleAndOffset, out float2 tri[3])
{
    uint triId = vertexId / 3;
    uint baseVertexId = triId * 3;
    tri[0] = vertexBuffer[baseVertexId + 0].xy * scaleAndOffset.xy + scaleAndOffset.zw;
    tri[1] = vertexBuffer[baseVertexId + 1].xy * scaleAndOffset.xy + scaleAndOffset.zw;
    tri[2] = vertexBuffer[baseVertexId + 2].xy * scaleAndOffset.xy + scaleAndOffset.zw;

    // Flip the winding order if it doesn't follow Unity's convention
    float2 edgeAB = tri[1] - tri[0];
    float2 edgeAC = tri[2] - tri[0];
    bool flipWinding = ((edgeAB.x * edgeAC.y) - (edgeAB.y * edgeAC.x)) > 0;

    if (flipWinding)
    {
        float2 temp = tri[1];
        tri[1] = tri[2];
        tri[2] = temp;
    }
}

// Given an effective resolution, a triangle, and a specific vertex [0; 2], calculate a new position for the
// vertex, which will result in conservative rasterization when fed to a non-conservative rasterizer.
void ExpandTriangleForConservativeRasterization(
    uint2 resolution,
    float2 tri[3],
    uint vertexId,
    out float4 triangleAABB,
    out float4 vertex)
{
    float2 pixelSize = (1.0 / resolution);

    // Calculate the AABB to clip off the overly conservative edges
    float2 aabbMin = min(min(tri[0], tri[1]), tri[2]);
    float2 aabbMax = max(max(tri[0], tri[1]), tri[2]);
    triangleAABB = (float4(aabbMin - (pixelSize / 2), aabbMax + (pixelSize / 2))) * resolution.xyxy;

    // Get offset lines
    float2 v1Off1, v2Off1;
    OffsetEdge(tri[0], tri[1], pixelSize, v1Off1, v2Off1);
    float2 v2Off2, v3Off2;
    OffsetEdge(tri[1], tri[2], pixelSize, v2Off2, v3Off2);
    float2 v3Off3, v1Off3;
    OffsetEdge(tri[2], tri[0], pixelSize, v3Off3, v1Off3);

    // Find their intersections. This is the new triangle
    tri[0] = LineIntersect(v1Off1, v2Off1, v3Off3, v1Off3);
    tri[1] = LineIntersect(v2Off2, v3Off2, v1Off1, v2Off1);
    tri[2] = LineIntersect(v3Off3, v1Off3, v2Off2, v3Off2);
    vertex = float4(tri[vertexId % 3]*2-1, 0, 1);
    #if UNITY_UV_STARTS_AT_TOP
    vertex.y = -vertex.y;
    #endif
}
