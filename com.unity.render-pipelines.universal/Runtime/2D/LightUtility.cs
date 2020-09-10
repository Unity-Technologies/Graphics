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

        private struct ParametricLightMeshVertex
        {
            public float3 position;
            public Color color;

            public static readonly VertexAttributeDescriptor[] VertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            };
        }

        private struct SpriteLightMeshVertex
        {
            public Vector3 position;
            public Color color;
            public Vector2 uv;

            public static readonly VertexAttributeDescriptor[] VertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
        }

        static Bounds Tessellate(Tess tess, ElementType boundaryType, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, Color c, ref int vcount, ref int icount)
        {
            tess.Tessellate(WindingRule.NonZero, boundaryType, 3);

            var iout = tess.Elements.Select(i => i);
            var vout = tess.Vertices.Select(v =>
                new ParametricLightMeshVertex() { position =  new float3(v.Position.X, v.Position.Y, 0), color = c });

            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);
            var bounds = new Bounds {min = min, max = max};
            var oldvcount = vcount;
            foreach(var v in vout)
            {
                min = math.min(min, v.position);
                max = math.max(max, v.position);
                vertices[vcount++] = v;
            }

            foreach (var i in iout)
            {
                indices[icount++] = (ushort)(i + oldvcount);
            }

            return new Bounds {min = min, max = max};
        }

        private static bool TestPivot(List<IntPoint> path, int activePoint, long lastPoint)
        {
            for (int i = activePoint; i < path.Count; ++i)
            {
                if (path[i].N > lastPoint)
                    return true;
            }

            return (path[activePoint].N == -1);
        }

        private static void FixPivots(List<IntPoint> path, int pathLength)
        {
            long pivotPoint = 0;

            for (int i = 1; i < path.Count; ++i)
            {
                var prev = path[i - 1];
                var curr = path[i];

                if (prev.N > curr.N)
                {
                    var incr = TestPivot(path, i, pivotPoint);
                    if (incr)
                    {
                        var test = path[i];
                        test.N = (pivotPoint + 1) < pathLength ? (pivotPoint + 1) : 0;
                        path[i] = test;
                    }
                }
                pivotPoint = path[i].N;
            }

            for (int i = 1; i < path.Count;)
            {
                var prev = path[i - 1];
                var curr = path[i];

                if (curr.N - prev.N > 1)
                {
                    IntPoint ins = prev;
                    ins.X = (prev.X + curr.X) / 2;
                    ins.Y = (prev.Y + curr.Y) / 2;
                    ins.N++;
                    path.Insert(i, ins);
                }
                else
                {
                    i++;
                }
            }
        }

        public static Bounds GenerateShapeMesh(Mesh mesh, Vector3[] shapePath, float falloffDistance)
        {

            var ix = 0;
            var vcount = 0;
            var icount = 0;
            const float kClipperScale = 10000.0f;
            var meshInteriorColor = new Color(1.0f,0,0,1.0f);
            var meshExteriorColor = new Color(0.0f,0,0,0.0f);
            var convexPath = shapePath;
            var vertices = new NativeArray<ParametricLightMeshVertex>(convexPath.Length * 256, Allocator.Temp);
            var indices = new NativeArray<ushort>(convexPath.Length * 256, Allocator.Temp);

            // Create shape geometry
            var innerShapeVertexCount = convexPath.Length;
            var inner = new ContourVertex[innerShapeVertexCount + 1];
            for (var i = 0; i < innerShapeVertexCount; ++i)
                inner[ix++] = new ContourVertex() { Position = new Vec3() { X = convexPath[i].x, Y = convexPath[i].y, Z = 0 } };
            inner[ix++] = inner[0];

            var tess = new Tess();
            tess.AddContour(inner, ContourOrientation.CounterClockwise);
            var bounds = Tessellate(tess, ElementType.Polygons, indices, vertices, meshInteriorColor, ref vcount, ref icount);

            // Create falloff geometry
            List<IntPoint> path = new List<IntPoint>();
            var scramblePntRand = new System.Random();
            for (var i = 0; i < innerShapeVertexCount; ++i)
            {

                var newPoint = new Vector2(inner[i].Position.X, inner[i].Position.Y) * kClipperScale;
                var addPoint = new IntPoint((System.Int64) (newPoint.x + scramblePntRand.Next(10)),
                    (System.Int64) (newPoint.y +  + scramblePntRand.Next(10)));
                addPoint.N = i;
                path.Add(addPoint);
            }

            // Generate Bevels.
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset clipOffset = new ClipperOffset(144.0f);
            clipOffset.AddPath(path, JoinType.jtRound, EndType.etClosedPolygon);
            clipOffset.Execute(ref solution, kClipperScale * falloffDistance, path.Count);

            if (solution.Count > 0)
            {
                path = solution[0];
                path.Add(path[0]);

                // Fix path for Pivots.
                FixPivots(path, convexPath.Length);

                for (int i = 1; i < path.Count; ++i)
                {
                    var prev = path[i - 1];
                    var curr = path[i];

                    var prevPoint = new float2(prev.X / kClipperScale, prev.Y / kClipperScale);
                    var currPoint = new float2(curr.X / kClipperScale, curr.Y / kClipperScale);

                    var prevIndex = prev.N == -1 ? 0 : prev.N;
                    var currIndex = curr.N == -1 ? 0 : curr.N;

                    if (prevIndex != currIndex)
                    {
                        vertices[vcount++] = new ParametricLightMeshVertex()
                        {
                            position =
                                new float3(inner[prevIndex].Position.X, inner[prevIndex].Position.Y, 0),
                            color = meshInteriorColor
                        };
                        vertices[vcount++] = new ParametricLightMeshVertex()
                        {
                            position =
                                new float3(inner[currIndex].Position.X, inner[currIndex].Position.Y, 0),
                            color = meshInteriorColor
                        };
                        vertices[vcount++] = new ParametricLightMeshVertex()
                        {
                            position =
                                new float3(currPoint.x, currPoint.y, 0),
                            color = meshExteriorColor
                        };
                        indices[icount++] = (ushort)(vcount - 3);
                        indices[icount++] = (ushort)(vcount - 2);
                        indices[icount++] = (ushort)(vcount - 1);
                    }

                    vertices[vcount++] = new ParametricLightMeshVertex()
                    {
                        position =
                            new float3(inner[prevIndex].Position.X, inner[prevIndex].Position.Y, 0),
                        color = meshInteriorColor
                    };
                    vertices[vcount++] = new ParametricLightMeshVertex()
                    {
                        position =
                            new float3(prevPoint.x, prevPoint.y, 0),
                        color = meshExteriorColor
                    };
                    vertices[vcount++] = new ParametricLightMeshVertex()
                    {
                        position =
                            new float3(currPoint.x, currPoint.y, 0),
                        color = meshExteriorColor
                    };
                    indices[icount++] = (ushort)(vcount - 3);
                    indices[icount++] = (ushort)(vcount - 2);
                    indices[icount++] = (ushort)(vcount - 1);
                }
            }

            mesh.SetVertexBufferParams(vcount, ParametricLightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vcount);
            mesh.SetIndices(indices, 0, icount, MeshTopology.Triangles, 0, false);

            return bounds;
        }


        public static Bounds GenerateParametricMesh(Mesh mesh, float radius, float falloffDistance, float angle, int sides)
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
            var vertices = new NativeArray<ParametricLightMeshVertex>(vertexCount, Allocator.Temp);
            var triangles = new NativeArray<ushort>(indexCount, Allocator.Temp);
            var centerIndex = (ushort)(2 * sides);

            // Color will contain r,g = x,y extrusion direction, a = alpha. b is unused at the moment. The inner shape should not be extruded
            var color = new Color(0, 0, 0, 1);
            vertices[centerIndex] = new ParametricLightMeshVertex
            {
                position = float3.zero,
                color = color
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
                vertices[vertexIndex] = new ParametricLightMeshVertex
                {
                    position = endPoint,
                    color = new Color(extrudeDir.x, extrudeDir.y, 0, 0)
                };
                vertices[vertexIndex + 1] = new ParametricLightMeshVertex
                {
                    position = endPoint,
                    color = color
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

            mesh.SetVertexBufferParams(vertexCount, ParametricLightMeshVertex.VertexLayout);
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
            var vertices = new NativeArray<SpriteLightMeshVertex>(srcIndices.Length, Allocator.Temp);
            var color = new Color(0,0,0, 1);

            for (var i = 0; i < srcVertices.Length; i++)
            {
                vertices[i] = new SpriteLightMeshVertex
                {
                    position = new Vector3(srcVertices[i].x, srcVertices[i].y, 0) - center,
                    color = color,
                    uv = srcUVs[i]
                };
            }
            mesh.SetVertexBufferParams(vertices.Length, SpriteLightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            mesh.SetIndices(srcIndices, MeshTopology.Triangles, 0, true);
            return mesh.bounds;
        }


#if UNITY_EDITOR
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
#endif

    }
}

