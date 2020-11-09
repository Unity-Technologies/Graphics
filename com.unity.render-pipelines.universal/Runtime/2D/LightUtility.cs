using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class LightUtility
    {

        public static bool CheckForChange(Light2D.LightType a, ref Light2D.LightType b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }
        public static bool CheckForChange(int a, ref int b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(float a, ref float b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(bool a, ref bool b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(Color a, ref Color b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        private enum PivotType
        {
            PivotBase,
            PivotCurve,
            PivotIntersect,
            PivotSkip,
            PivotClip
        };

        [Serializable]
        internal struct LightMeshVertex
        {
            public float3 position;
            public float3 nor;
            public Color color;
            public float2 uv;
            

            public static readonly VertexAttributeDescriptor[] VertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
        }

        static void Tessellate(Tess tess, ElementType boundaryType, NativeArray<ushort> indices,
            NativeArray<LightMeshVertex> vertices, Color c, float2 uvData, ref int VCount, ref int ICount)
        {
            tess.Tessellate(WindingRule.NonZero, boundaryType, 3);

            var prevCount = VCount;
            var tessIndices = tess.Elements.Select(i => i);
            var tessVertices = tess.Vertices.Select(v =>
                new LightMeshVertex() { position =  new float3(v.Position.X, v.Position.Y, 0), color = c, uv = uvData, nor = float3.zero });

            foreach(var v in tessVertices)
                vertices[VCount++] = v;
            foreach (var i in tessIndices)
                indices[ICount++] = (ushort)(i + prevCount);
        }

        static bool TestPivot(List<IntPoint> path, int activePoint, long lastPoint)
        {
            for (int i = activePoint; i < path.Count; ++i)
            {
                if (path[i].N > lastPoint)
                    return true;
            }

            return (path[activePoint].N == -1);
        }

        // Degenerate Pivots at the End Points.
        static List<IntPoint> DegeneratePivots(List<IntPoint> path, List<IntPoint> inPath)
        {
            List<IntPoint> degenerate = new List<IntPoint>();
            var minN = path[0].N;
            var maxN = path[0].N;
            for (int i = 1; i < path.Count; ++i)
            {
                if (path[i].N != -1)
                {
                    minN = Math.Min(minN, path[i].N);
                    maxN = Math.Max(maxN, path[i].N);
                }
            }

            for (long i = 0; i < minN; ++i)
            {
                IntPoint ins = path[(int)minN];
                ins.N = i;
                degenerate.Add(ins);
            }
            degenerate.AddRange(path.GetRange(0, path.Count));
            for (long i = maxN + 1; i < inPath.Count; ++i)
            {
                IntPoint ins = inPath[(int)i];
                ins.N = i;
                degenerate.Add(ins);
            }
            return degenerate;
        }

        // Ensure that we get a valid path from 0.
        static List<IntPoint> SortPivots(List<IntPoint> outPath, List<IntPoint> inPath)
        {
            List<IntPoint> sorted = new List<IntPoint>();
            var min = outPath[0].N;
            var max = outPath[0].N;
            var minIndex = 0;
            bool newMin = true;
            for (int i = 1; i < outPath.Count; ++i)
            {
                if (max > outPath[i].N && newMin && outPath[i].N != -1)
                {
                    min = max = outPath[i].N;
                    minIndex = i;
                    newMin = false;
                }
                else if (outPath[i].N >= max)
                {
                    max = outPath[i].N;
                    newMin = true;
                }
            }
            sorted.AddRange(outPath.GetRange(minIndex, (outPath.Count - minIndex)));
            sorted.AddRange(outPath.GetRange(0, minIndex));
            return sorted;
        }

        // Ensure that all points eliminated due to overlaps and intersections are accounted for Tessellation.
        static List<IntPoint> FixPivots(List<IntPoint> outPath, List<IntPoint> inPath)
        {
            var path = SortPivots(outPath, inPath);
            long pivotPoint = path[0].N;

            // Connect Points for Overlaps.
            for (int i = 1; i < path.Count; ++i)
            {
                var j = (i == path.Count - 1) ? 0 : (i + 1);
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[j];

                if (prev.N > curr.N)
                {
                    var incr = TestPivot(path, i, pivotPoint);
                    if (incr)
                    {
                        if (prev.N == next.N)
                            curr.N = prev.N;
                        else
                            curr.N = (pivotPoint + 1) < inPath.Count ? (pivotPoint + 1) : 0;
                        curr.D = 3;
                        path[i] = curr;
                    }
                }
                pivotPoint = path[i].N;
            }

            // Insert Skipped Points.
            for (int i = 1; i < path.Count - 1;)
            {
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[i + 1];

                if (curr.N - prev.N > 1)
                {
                    if (curr.N == next.N)
                    {
                        IntPoint ins = curr;
                        ins.N = (ins.N - 1);
                        path[i] = ins;
                    }
                    else
                    {
                        IntPoint ins = curr;
                        ins.N = (ins.N - 1);
                        path.Insert(i, ins);
                    }
                }
                else
                {
                    i++;
                }
            }

            path = DegeneratePivots(path, inPath);
            return path;
        }

        // Rough shape only used in Inspector for quick preview.
        internal static List<Vector2> GetOutlinePath(Vector3[] shapePath, float offsetDistance)
        {
            const float kClipperScale = 10000.0f;
            List<IntPoint> path = new List<IntPoint>();
            List<Vector2> output = new List<Vector2>();
            for (var i = 0; i < shapePath.Length; ++i)
            {
                var newPoint = new Vector2(shapePath[i].x, shapePath[i].y) * kClipperScale;
                path.Add(new IntPoint((System.Int64) (newPoint.x), (System.Int64) (newPoint.y)));
            }
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset clipOffset = new ClipperOffset(2048.0f);
            clipOffset.AddPath(path, JoinType.jtRound, EndType.etClosedPolygon);
            clipOffset.Execute(ref solution, kClipperScale * offsetDistance, path.Count);
            if (solution.Count > 0)
            {
                for (int i = 0; i < solution[0].Count; ++i)
                    output.Add(new Vector2(solution[0][i].X / kClipperScale, solution[0][i].Y / kClipperScale));
            }
            return output;
        }

        public static Bounds GenerateShapeMesh(Mesh mesh, Color lightColor, Vector3[] shapePath, float falloffDistance, float fallOffIntensity, float volumeOpacity)
        {

            var ix = 0;
            var vcount = 0;
            var icount = 0;
            const float kClipperScale = 10000.0f;

            // todo Revisit this while we do Batching.
            var meshInteriorColor = new Color(lightColor.r, lightColor.g, lightColor.b,1.0f);
            var meshExteriorColor = new Color(lightColor.r, lightColor.g, lightColor.b,0.0f);
            var vertices = new NativeArray<LightMeshVertex>(shapePath.Length * 256, Allocator.Temp);
            var indices = new NativeArray<ushort>(shapePath.Length * 256, Allocator.Temp);
            var uvData = new float2(fallOffIntensity, volumeOpacity);

            // Create shape geometry
            var inputPointCount = shapePath.Length;
            var inner = new ContourVertex[inputPointCount + 1];
            for (var i = 0; i < inputPointCount; ++i)
                inner[ix++] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y, Z = 0 } };
            inner[ix++] = inner[0];

            var tess = new Tess();
            tess.AddContour(inner, ContourOrientation.CounterClockwise);
            Tessellate(tess, ElementType.Polygons, indices, vertices, meshInteriorColor, uvData, ref vcount, ref icount);

            // Create falloff geometry
            List<IntPoint> path = new List<IntPoint>();
            for (var i = 0; i < inputPointCount; ++i)
            {
                var newPoint = new Vector2(inner[i].Position.X, inner[i].Position.Y) * kClipperScale;
                var addPoint = new IntPoint((System.Int64) (newPoint.x),(System.Int64) (newPoint.y));
                addPoint.N = i; addPoint.D = -1;
                path.Add(addPoint);
            }
            var lastPointIndex = inputPointCount - 1;

            // Generate Bevels.
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset clipOffset = new ClipperOffset(24.0f);
            clipOffset.AddPath(path, JoinType.jtRound, EndType.etClosedPolygon);
            clipOffset.Execute(ref solution, kClipperScale * falloffDistance, path.Count);

            if (solution.Count > 0)
            {
                // Fix path for Pivots.
                var outPath = solution[0];
                var minPath = (long)inputPointCount;
                for (int i = 0; i < outPath.Count; ++i)
                    minPath = (outPath[i].N != -1 ) ? Math.Min(minPath, outPath[i].N) : minPath;
                var containsStart = minPath == 0;
                outPath = FixPivots(outPath, path);

                // Tessellate.
                var innerIndices = new ushort[inputPointCount];

                // Inner Vertices. (These may or may not be part of the created path. Beware!!)
                for (int i = 0; i < inputPointCount; ++i)
                {
                    vertices[vcount++] = new LightMeshVertex()
                    {
                        position = new float3(inner[i].Position.X, inner[i].Position.Y, 0),
                        color = meshInteriorColor,
                        uv = uvData,
                        nor = float3.zero
                    };
                    innerIndices[i] = (ushort)(vcount - 1);
                }

                var saveIndex = (ushort)vcount;
                var pathStart = saveIndex;
                var prevIndex = outPath[0].N == -1 ? 0 : outPath[0].N;

                for (int i = 0; i < outPath.Count; ++i)
                {
                    var curr = outPath[i];
                    var currPoint = new float2(curr.X / kClipperScale, curr.Y / kClipperScale);
                    var currIndex = curr.N == -1 ? 0 : curr.N;

                    vertices[vcount++] = new LightMeshVertex()
                    {
                        position = new float3(currPoint.x, currPoint.y, 0),
                        color = meshExteriorColor,
                        uv = uvData,
                        nor = float3.zero
                    };

                    if (prevIndex != currIndex)
                    {
                        indices[icount++] = innerIndices[prevIndex];
                        indices[icount++] = innerIndices[currIndex];
                        indices[icount++] = (ushort)(vcount - 1);
                    }

                    indices[icount++] = innerIndices[prevIndex];
                    indices[icount++] = saveIndex;
                    indices[icount++] = saveIndex = (ushort)(vcount - 1);
                    prevIndex = currIndex;
                }

                // Close the Loop.
                {
                    indices[icount++] = pathStart;
                    indices[icount++] = innerIndices[minPath];
                    indices[icount++] = containsStart ? innerIndices[lastPointIndex] : saveIndex;

                    indices[icount++] = containsStart ? pathStart : saveIndex;
                    indices[icount++] = containsStart ? saveIndex : innerIndices[minPath];
                    indices[icount++] = containsStart ? innerIndices[lastPointIndex] : innerIndices[minPath - 1];
                }
            }

            mesh.SetVertexBufferParams(vcount, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vcount);
            mesh.SetIndices(indices, 0, icount, MeshTopology.Triangles, 0, true);
            return mesh.GetSubMesh(0).bounds;
        }

        public static Bounds GenerateParametricMesh(Mesh mesh, Color lightColor, float radius, float falloffDistance, float angle, int sides, float fallOffIntensity, float volumeOpacity)
        {
            var angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                sides = 4;
            }

            if(sides == 4)
            {
                angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
            }

            var vertexCount = 1 + 2 * sides;
            var indexCount = 3 * 3 * sides;
            var vertices = new NativeArray<LightMeshVertex>(vertexCount, Allocator.Temp);
            var triangles = new NativeArray<ushort>(indexCount, Allocator.Temp);
            var centerIndex = (ushort)(2 * sides);
            var uvData = new float2(fallOffIntensity, volumeOpacity);

            // Only Alpha value in Color channel is ever used. May remove it or keep it for batching params in the future.
            var color = new Color( lightColor.r, lightColor.g, lightColor.b, 1);
            vertices[centerIndex] = new LightMeshVertex
            {
                position = float3.zero,
                color = color,
                uv = uvData,
                nor = new float3(0, 0, falloffDistance)
            };

            var radiansPerSide = 2 * Mathf.PI / sides;
            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);

            for (var i = 0; i < sides; i++)
            {
                var endAngle = (i + 1) * radiansPerSide;
                var extrudeDir = new float3(math.cos(endAngle + angleOffset), math.sin(endAngle + angleOffset), 0);
                var endPoint = radius * extrudeDir;

                var vertexIndex = (2 * i + 2) % (2 * sides);
                vertices[vertexIndex] = new LightMeshVertex
                {
                    position = endPoint,
                    color = new Color(lightColor.r, lightColor.g, lightColor.b, 0),
                    uv = uvData,
                    nor = new float3(extrudeDir.x, extrudeDir.y, falloffDistance)
                };
                vertices[vertexIndex + 1] = new LightMeshVertex
                {
                    position = endPoint,
                    color = color,
                    uv = uvData,
                    nor = new float3(extrudeDir.x, extrudeDir.y, falloffDistance)
                };

                // Triangle 1 (Tip)
                var triangleIndex = 9 * i;
                triangles[triangleIndex] = (ushort)(vertexIndex + 1);
                triangles[triangleIndex + 1] = (ushort)(2 * i + 1);
                triangles[triangleIndex + 2] = centerIndex;

                // Triangle 2 (Upper Top Left)
                triangles[triangleIndex + 3] = (ushort)(vertexIndex);
                triangles[triangleIndex + 4] = (ushort)(2 * i);
                triangles[triangleIndex + 5] = (ushort)(2 * i + 1);

                // Triangle 2 (Bottom Top Left)
                triangles[triangleIndex + 6] = (ushort)(vertexIndex + 1);
                triangles[triangleIndex + 7] = (ushort)(vertexIndex);
                triangles[triangleIndex + 8] = (ushort)(2 * i + 1);

                min = math.min(min, endPoint + extrudeDir * falloffDistance);
                max = math.max(max, endPoint + extrudeDir * falloffDistance);
            }

            mesh.SetVertexBufferParams(vertexCount, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0, false);

            return new Bounds
            {
                min = min,
                max = max
            };
        }

        public static Bounds GenerateSpriteMesh(Mesh mesh, Sprite sprite)
        {
            if(sprite == null)
            {
                mesh.Clear();
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            // this needs to be called before getting UV at the line below.
            // Venky fixed it, enroute to trunk
            var uvs = sprite.uv;

            var srcVertices = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
            var srcUVs = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
            var srcIndices = sprite.GetIndices();

            var center = 0.5f * (sprite.bounds.min + sprite.bounds.max);
            var vertices = new NativeArray<LightMeshVertex>(srcIndices.Length, Allocator.Temp);
            var color = new Color(0,0,0, 1);

            for (var i = 0; i < srcVertices.Length; i++)
            {
                vertices[i] = new LightMeshVertex
                {
                    position = new Vector3(srcVertices[i].x, srcVertices[i].y, 0) - center,
                    color = color,
                    uv = srcUVs[i],
                    nor = float3.zero
                };
            }
            mesh.SetVertexBufferParams(vertices.Length, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            mesh.SetIndices(srcIndices, MeshTopology.Triangles, 0, true);
            return mesh.GetSubMesh(0).bounds;
        }


        public static int GetShapePathHash(Vector3[] path)
        {
            unchecked
            {
                int hashCode = (int)2166136261;

                if (path != null)
                {
                    foreach (var point in path)
                        hashCode = hashCode * 16777619 ^ point.GetHashCode();
                }
                else
                {
                    hashCode = 0;
                }

                return hashCode;
            }
        }

    }
}

