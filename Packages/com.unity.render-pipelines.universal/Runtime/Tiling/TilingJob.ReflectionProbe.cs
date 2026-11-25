using System;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Reflection probe tiling: box edges clipped to the depth range, projected to a 2D convex hull.
    partial struct TilingJob
    {
        const int k_CubePointCount = 8;

        // Cube corners between the near and far planes, plus a near and a far clip point for each of the 12 edges.
        const int k_MaxClippedPointCount = k_CubePointCount + 4 * 3 * 2;

        static readonly float3[] k_CubePoints =
        {
            new(-1, -1, -1),
            new(-1, -1, +1),
            new(-1, +1, -1),
            new(-1, +1, +1),
            new(+1, -1, -1),
            new(+1, -1, +1),
            new(+1, +1, -1),
            new(+1, +1, +1),
        };

        // Each item represents 3 lines, with x being the start index and yzw the end indices.
        static readonly int4[] k_CubeLineIndices =
        {
            // (-1, -1, -1) -> {(+1, -1, -1), (-1, +1, -1), (-1, -1, +1)}
            new(0, 4, 2, 1),

            // (-1, +1, +1) -> {(+1, +1, +1), (-1, -1, +1), (-1, +1, -1)}
            new(3, 7, 1, 2),

            // (+1, -1, +1) -> {(-1, -1, +1), (+1, +1, +1), (+1, -1, -1)}
            new(5, 1, 7, 4),

            // (+1, +1, -1) -> {(-1, +1, -1), (+1, -1, -1), (+1, +1, +1)}
            new(6, 2, 4, 7),
        };

        void TileReflectionProbe(int index)
        {
            // The algorithm used here works by clipping all the lines of the cube against the near-plane, and then
            // projects the resulting points to the view plane. These points are then used to construct a 2D convex
            // hull, which we can iterate linearly to get the lines on screen making up the cube.

            var reflectionProbe = reflectionProbes[index - lights.Length];
            if (!HasNonZeroVolume(reflectionProbe))
                return;

            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;
            quaternion rotation;
            if (reflectionProbeRotation)
                rotation = (quaternion)reflectionProbe.localToWorldMatrix.rotation;
            else
                rotation = quaternion.identity;

            // The vertices of the cube in view space.
            Span<float3> points = stackalloc float3[k_CubePointCount];
            // This is initially filled with just the cube vertices that lie between the near and far planes. Each edge can add
            // two more points where it crosses them.
            Span<float2> clippedPoints = stackalloc float2[k_MaxClippedPointCount];
            var clippedPointsCount = 0;
            var leftmostIndex = 0;
            for (var i = 0; i < k_CubePoints.Length; i++)
            {
                var rotatedPointsWS = centerWS + math.rotate(rotation, extentsWS * k_CubePoints[i]);
                var point = math.mul(worldToViews[m_ViewIndex], math.float4(rotatedPointsWS, 1)).xyz;
                point.z *= -1;
                points[i] = point;
                if (IsInDepthRange(point))
                {
                    var clippedPoint = isOrthographic ? point.xy : point.xy/point.z;
                    var clippedIndex = clippedPointsCount++;
                    clippedPoints[clippedIndex] = clippedPoint;
                    if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                }
            }

            // Clip the cube's line segments with the near and far planes, and add the new vertices to clippedPoints. Only
            // lines that cross a plane generate new vertices.
            for (var i = 0; i < k_CubeLineIndices.Length; i++)
            {
                var indices = k_CubeLineIndices[i];
                var p0 = points[indices.x];
                for (var j = 0; j < 3; j++)
                {
                    var p1 = points[indices[j+1]];
                    for (var k = 0; k < 2; k++)
                    {
                        var depth = k == 0 ? near : far;
                        if ((p0.z < depth) == (p1.z < depth)) continue;
                        var d = (depth - p0.z) / (p1.z - p0.z);
                        var p = math.lerp(p0, p1, d);
                        var clippedPoint = isOrthographic ? p.xy : p.xy/p.z;
                        var clippedIndex = clippedPointsCount++;
                        clippedPoints[clippedIndex] = clippedPoint;
                        if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                    }
                }
            }

            // Construct the convex hull. It is formed by the line loop consisting of the points in the array.
            Span<float2> hullPoints = stackalloc float2[k_MaxClippedPointCount];
            var hullPointsCount = 0;

            if (clippedPointsCount > 0)
            {
                // Start with the leftmost point, as that is guaranteed to be on the hull.
                var hullPointIndex = leftmostIndex;

                // Find the remaining hull points until we end up back at the leftmost point.
                do
                {
                    var hullPoint = clippedPoints[hullPointIndex];
                    ExpandY(math.float3(hullPoint, 1));
                    hullPoints[hullPointsCount++] = hullPoint;

                    // Find the endpoint resulting in the leftmost turning line. This line will be a part of the hull.
                    var endpointIndex = 0;
                    var endpointLine = clippedPoints[endpointIndex] - hullPoint;
                    for (var i = 0; i < clippedPointsCount; i++)
                    {
                        var candidateLine = clippedPoints[i] - hullPoint;
                        var det = math.determinant(math.float2x2(endpointLine, candidateLine));

                        // Check if point i lies on the left side of the line to the current endpoint, or if it lies
                        // collinear to the current endpoint but farther away.
                        if (endpointIndex == hullPointIndex || det > 0 || (det == 0.0f && math.lengthsq(candidateLine) > math.lengthsq(endpointLine)))
                        {
                            endpointIndex = i;
                            endpointLine = candidateLine;
                        }
                    }

                    hullPointIndex = endpointIndex;
                } while (hullPointIndex != leftmostIndex && hullPointsCount < clippedPointsCount);

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for sphere.
                for (var planeIndex = m_TileYRange.start; planeIndex <= m_TileYRange.end + 1; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    var planeY = math.lerp(viewPlaneBottoms[m_ViewIndex], viewPlaneTops[m_ViewIndex], planeIndex * tileScaleInv.y);

                    for (var i = 0; i < hullPointsCount; i++)
                    {
                        var hp0 = hullPoints[i];
                        var hp1 = hullPoints[(i + 1) % hullPointsCount];

                        // planeY = hp0 + t * (hp1 - hp0) => planeY - hp0 = t * (hp1 - hp0) => (planeY - hp0) / (hp1 - hp0) = t
                        // A horizontal edge's endpoints are also endpoints of the neighboring edges, which cover them.
                        if (hp1.y == hp0.y) continue;
                        var t = (planeY - hp0.y) / (hp1.y - hp0.y);
                        if (t < 0 || t > 1) continue;
                        var x = math.lerp(hp0.x, hp1.x, t);

                        var p = math.float3(x, planeY, 1);
                        var pTS = isOrthographic ? ViewToTileSpaceOrthographic(p) : ViewToTileSpace(p);
                        ExpandTileX(ref planeRange, pTS.x);
                    }

                    MergeTileRangeRows(planeIndex, planeRange);
                }

                tileRanges[m_Offset] = m_TileYRange;
            }
        }
    }
}
