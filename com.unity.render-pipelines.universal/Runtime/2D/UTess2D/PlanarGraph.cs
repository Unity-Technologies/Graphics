using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal.UTess
{
    // Ensures that there are no duplicate Points, Overlapping Edges.
    struct PlanarGraph
    {
        private static readonly double kEpsilon = 0.00001;
        private static readonly int kMaxIntersectionTolerance = 4;  // Maximum Intersection Tolerance per Intersection Loop Check.

        internal static void RemoveDuplicateEdges(ref NativeArray<int2> edges, ref int edgeCount, NativeArray<int> duplicates, int duplicateCount)
        {
            if (duplicateCount == 0)
            {
                for (var i = 0; i < edgeCount; ++i)
                {
                    var e = edges[i];
                    e.x = math.min(edges[i].x, edges[i].y);
                    e.y = math.max(edges[i].x, edges[i].y);
                    edges[i] = e;
                }
            }
            else
            {
                for (var i = 0; i < edgeCount; ++i)
                {
                    var e = edges[i];
                    var a = duplicates[e.x];
                    var b = duplicates[e.y];
                    e.x = math.min(a, b);
                    e.y = math.max(a, b);
                    edges[i] = e;
                }
            }

            unsafe
            {
                ModuleHandle.InsertionSort<int2, TessEdgeCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(edges), 0, edgeCount - 1,
                    new TessEdgeCompare());
            }

            var n = 1;
            for (var i = 1; i < edgeCount; ++i)
            {
                var prev = edges[i - 1];
                var next = edges[i];
                if (next.x == prev.x && next.y == prev.y)
                    continue;
                if (next.x == next.y)
                    continue;
                edges[n++] = next;
            }
            edgeCount = n;
        }

        internal static bool CheckCollinear(double2 a0, double2 a1, double2 b0, double2 b1)
        {
            double2 a = a0;
            double2 b = a1;
            double2 c = b0;
            double2 d = b1;

            double x = (b.y - a.y) / (b.x - a.x);
            double y = (c.y - a.y) / (c.x - a.x);
            double z = (d.y - a.y) / (d.x - a.x);
            return ((!math.isinf(x) || !math.isinf(y) || !math.isinf(z)) && math.abs(x - y) > kEpsilon && math.abs(x - z) > kEpsilon);
        }

        internal static bool LineLineIntersection(double2 a0, double2 a1, double2 b0, double2 b1)
        {
            var x0 = ModuleHandle.OrientFastDouble(a0, b0, b1);
            var y0 = ModuleHandle.OrientFastDouble(a1, b0, b1);
            if ((x0 > kEpsilon && y0 > kEpsilon) || (x0 < -kEpsilon && y0 < -kEpsilon))
            {
                return false;
            }

            var x1 = ModuleHandle.OrientFastDouble(b0, a0, a1);
            var y1 = ModuleHandle.OrientFastDouble(b1, a0, a1);
            if ((x1 > kEpsilon && y1 > kEpsilon) || (x1 < -kEpsilon && y1 < -kEpsilon))
            {
                return false;
            }

            // Check for degenerate collinear case
            if (math.abs(x0) < kEpsilon && math.abs(y0) < kEpsilon && math.abs(x1) < kEpsilon && math.abs(y1) < kEpsilon)
            {
                return CheckCollinear(a0, a1, b0, b1);
            }

            return true;
        }

        internal static bool LineLineIntersection(double2 p1, double2 p2, double2 p3, double2 p4, ref double2 result)
        {
            double bx = p2.x - p1.x;
            double by = p2.y - p1.y;
            double dx = p4.x - p3.x;
            double dy = p4.y - p3.y;
            double bDotDPerp = bx * dy - by * dx;
            if (math.abs(bDotDPerp) < kEpsilon)
            {
                return false;
            }

            double cx = p3.x - p1.x;
            double cy = p3.y - p1.y;
            double t = (cx * dy - cy * dx) / bDotDPerp;
            if ((t >= -kEpsilon) && (t <= 1.0f + kEpsilon))
            {
                result.x = p1.x + t * bx;
                result.y = p1.y + t * by;
                return true;
            }
            return false;
        }

        internal static bool CalculateEdgeIntersections(NativeArray<int2> edges, int edgeCount, NativeArray<double2> points, int pointCount, ref NativeArray<int2> results, ref NativeArray<double2> intersects, ref int resultCount)
        {
            resultCount = 0;

            for (int i = 0; i < edgeCount; ++i)
            {
                for (int j = i + 1; j < edgeCount; ++j)
                {
                    var e = edges[i];
                    var f = edges[j];
                    if (e.x == f.x || e.x == f.y || e.y == f.x || e.y == f.y)
                        continue;

                    var a = points[e.x];
                    var b = points[e.y];
                    var c = points[f.x];
                    var d = points[f.y];
                    var g = double2.zero;
                    if (LineLineIntersection(a, b, c, d))
                    {
                        if (LineLineIntersection(a, b, c, d, ref g))
                        {
                            // Until we ensure Outline is generated properly, we need this useless Check every correction.
                            if (resultCount >= intersects.Length)
                                return false;

                            intersects[resultCount] = g;
                            results[resultCount++] = new int2(i, j);
                        }
                    }
                }
            }

            // Basically we have self intersections all over (eg. gobo_tree_04). Better don't generate any Mesh as even though this will eventually succeed, error correction will take long time.
            if (resultCount > (edgeCount * kMaxIntersectionTolerance))
            {
                return false;
            }

            unsafe
            {
                var tjc = new IntersectionCompare();
                tjc.edges = edges;
                tjc.points = points;
                ModuleHandle.InsertionSort<int2, IntersectionCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), 0, resultCount - 1, tjc);
            }

            return true;
        }

        internal static bool CalculateTJunctions(NativeArray<int2> edges, int edgeCount, NativeArray<double2> points, int pointCount, NativeArray<int2> results, ref int resultCount)
        {
            resultCount = 0;

            for (int i = 0; i < edgeCount; ++i)
            {
                for (int j = 0; j < pointCount; ++j)
                {
                    var e = edges[i];
                    if (e.x == j || e.y == j)
                        continue;

                    var a = points[e.x];
                    var b = points[e.y];
                    var c = points[j];
                    var d = points[j];
                    if (LineLineIntersection(a, b, c, d))
                    {
                        // Until we ensure Outline is generated properly, we need this useless Check every correction.
                        if (resultCount >= results.Length)
                            return false;
                        results[resultCount++] = new int2(i, j);
                    }
                }
            }

            return true;
        }

        internal static bool CutEdges(ref NativeArray<double2> points, ref int pointCount, ref NativeArray<int2> edges, ref int edgeCount, ref NativeArray<int2> tJunctions, ref int tJunctionCount, NativeArray<int2> intersections, NativeArray<double2> intersects, int intersectionCount)
        {
            for (int i = 0; i < intersectionCount; ++i)
            {
                var intr = intersections[i];
                var e = intr.x;
                var f = intr.y;

                int2 j1 = int2.zero;
                j1.x = e;
                j1.y = pointCount;
                tJunctions[tJunctionCount++] = j1;
                int2 j2 = int2.zero;
                j2.x = f;
                j2.y = pointCount;
                tJunctions[tJunctionCount++] = j2;

                // Until we ensure Outline is generated properly, we need this useless Check every correction.
                if (pointCount >= points.Length)
                    return false;

                points[pointCount++] = intersects[i];
            }

            unsafe
            {
                ModuleHandle.InsertionSort<int2, TessJunctionCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(tJunctions), 0, tJunctionCount - 1,
                    new TessJunctionCompare());
            }

            // Split edges along junctions
            for (int i = tJunctionCount - 1; i >= 0; --i)
            {
                var tJunction = tJunctions[i];
                var e = tJunction.x;
                var edge = edges[e];
                var s = edge.x;
                var t = edge.y;

                // Check if edge is not lexicographically sorted
                var a = points[s];
                var b = points[t];
                if (((a.x - b.x) < 0) || (a.x == b.x && (a.y - b.y) < 0))
                {
                    var tmp = s;
                    s = t;
                    t = tmp;
                }

                // Split leading edge
                edge.x = s;
                var last = edge.y = tJunction.y;
                edges[e] = edge;

                // If we are grouping edges by color, remember to track data
                // Split other edges
                while (i > 0 && tJunctions[i - 1].x == e)
                {
                    var next = tJunctions[--i].y;
                    int2 te = new int2();
                    te.x = last;
                    te.y = next;
                    edges[edgeCount++] = te;
                    last = next;
                }

                int2 le = new int2();
                le.x = last;
                le.y = t;
                edges[edgeCount++] = le;
            }

            return true;
        }

        internal static void RemoveDuplicatePoints(ref NativeArray<double2> points, ref int pointCount, ref NativeArray<int> duplicates, ref int duplicateCount, Allocator allocator)
        {
            TessLink link = TessLink.CreateLink(pointCount, allocator);

            for (int i = 0; i < pointCount; ++i)
            {
                for (int j = i + 1; j < pointCount; ++j)
                {
                    if (math.distance(points[i], points[j]) < kEpsilon)
                    {
                        link.Link(i, j);
                    }
                }
            }

            duplicateCount = 0;
            for (var i = 0; i < pointCount; ++i)
            {
                var j = link.Find(i);
                if (j != i)
                {
                    duplicateCount++;
                    points[j] = math.min(points[i], points[j]);
                }
            }

            // Find Duplicates.
            if (duplicateCount != 0)
            {
                var prevPointCount = pointCount;
                pointCount = 0;
                for (var i = 0; i < prevPointCount; ++i)
                {
                    var j = link.Find(i);
                    if (j == i)
                    {
                        duplicates[i] = pointCount;
                        points[pointCount++] = points[i];
                    }
                    else
                    {
                        duplicates[i] = -1;
                    }
                }

                // Update Duplicates.
                for (int i = 0; i < prevPointCount; ++i)
                {
                    if (duplicates[i] < 0)
                    {
                        duplicates[i] = duplicates[link.Find(i)];
                    }
                }
            }

            TessLink.DestroyLink(link);
        }

        // Validate the Input Points ane Edges.
        internal static bool Validate(Allocator allocator, NativeArray<float2> inputPoints, int pointCount, NativeArray<int2> inputEdges, int edgeCount, ref NativeArray<float2> outputPoints, ref int outputPointCount, ref NativeArray<int2> outputEdges, ref int outputEdgeCount)
        {
            // Outline generated inputs can have differences in the range of 0.00001f.. See TwoLayers.psb sample.
            // Since PlanarGraph operates on double, scaling up and down does not result in loss of data.
            var precisionFudge = 10000.0f;
            var protectLoop = edgeCount;
            var requiresFix = true;
            var validGraph = false;

            // Processing Arrays.
            NativeArray<int> duplicates = new NativeArray<int>(ModuleHandle.kMaxEdgeCount, allocator);
            NativeArray<int2> edges = new NativeArray<int2>(ModuleHandle.kMaxEdgeCount, allocator);
            NativeArray<int2> tJunctions = new NativeArray<int2>(ModuleHandle.kMaxEdgeCount, allocator);
            NativeArray<int2> edgeIntersections = new NativeArray<int2>(ModuleHandle.kMaxEdgeCount, allocator);
            NativeArray<double2> points = new NativeArray<double2>(pointCount * 8, allocator);
            NativeArray<double2> intersects = new NativeArray<double2>(pointCount * 8, allocator);

            // Initialize.
            for (int i = 0; i < pointCount; ++i)
                points[i] = inputPoints[i] * precisionFudge;
            ModuleHandle.Copy(inputEdges, edges, edgeCount);

            // Pre-clear duplicates, otherwise the following will simply fail.
            RemoveDuplicateEdges(ref edges, ref edgeCount, duplicates, 0);

            // While PSG is clean.
            while (requiresFix && --protectLoop > 0)
            {
                // Edge Edge Intersection.
                int intersectionCount = 0;
                validGraph = CalculateEdgeIntersections(edges, edgeCount, points, pointCount, ref edgeIntersections, ref intersects, ref intersectionCount);
                if (!validGraph)
                    break;

                // Edge Point Intersection. T-Junction.
                int tJunctionCount = 0;
                validGraph = CalculateTJunctions(edges, edgeCount, points, pointCount, tJunctions, ref tJunctionCount);
                if (!validGraph)
                    break;

                // Cut Overlapping Edges.
                validGraph = CutEdges(ref points, ref pointCount, ref edges, ref edgeCount, ref tJunctions, ref tJunctionCount, edgeIntersections, intersects, intersectionCount);
                if (!validGraph)
                    break;

                // Remove Duplicate Points.
                int duplicateCount = 0;
                RemoveDuplicatePoints(ref points, ref pointCount, ref duplicates, ref duplicateCount, allocator);
                RemoveDuplicateEdges(ref edges, ref edgeCount, duplicates, duplicateCount);

                requiresFix = intersectionCount != 0 || tJunctionCount != 0;
            }

            if (validGraph)
            {
                // Finalize Output.
                outputEdgeCount = edgeCount;
                outputPointCount = pointCount;
                ModuleHandle.Copy(edges, outputEdges, edgeCount);
                for (int i = 0; i < pointCount; ++i)
                    outputPoints[i] = new float2((float)(points[i].x / precisionFudge), (float)(points[i].y / precisionFudge));
            }

            edges.Dispose();
            points.Dispose();
            intersects.Dispose();
            duplicates.Dispose();
            tJunctions.Dispose();
            edgeIntersections.Dispose();

            return (validGraph && protectLoop > 0);
        }
    }
}
