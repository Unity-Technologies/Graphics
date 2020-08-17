using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        public static List<Vector2> GetFalloffShape(Vector3[] shapePath)
        {
            var extrusionDir = new List<Vector2>();
            for (var i = 0; i < shapePath.Length; ++i)
            {
                var h = (i == 0) ? (shapePath.Length - 1) : (i - 1);
                var j = (i + 1) % shapePath.Length;

                var pp = shapePath[h];
                var cp = shapePath[i];
                var np = shapePath[j];

                var cpd = cp - pp;
                var npd = np - cp;
                if (cpd.magnitude < 0.001f || npd.magnitude < 0.001f)
                    continue;

                var vl = cpd.normalized;
                var vr = npd.normalized;

                vl = new Vector2(-vl.y, vl.x);
                vr = new Vector2(-vr.y, vr.x);

                var va = vl.normalized + vr.normalized;
                var vn = -va.normalized;

                if (va.magnitude > 0 && vn.magnitude > 0)
                {
                    var dir = new Vector2(vn.x, vn.y);
                    extrusionDir.Add(dir);
                }
            }
            return extrusionDir;
        }

        static void Tessellate(ContourVertex[] inputs, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, ref int vcount, ref int icount, Color icolor, Color ecolor)
        {
            var tess = new Tess();
            tess.AddContour(inputs, ContourOrientation.Original);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            var iout = tess.Elements.Select(i => i);
            var vout = tess.Vertices.Select(v => new float3(v.Position.X, v.Position.Y, 0));

            foreach(var v in vout)
            {
                vertices[vcount++] = new ParametricLightMeshVertex
                {
                    position = v,
                    color = v.z > 0 ? icolor : ecolor
                };
            }

            foreach (var i in iout)
            {
                indices[icount++] = (ushort)i;
            }
        }

        static Vector3[] Subdivide(Vector3[] shapePath, int subdivision)
        {
            if (subdivision <= 1)
                return shapePath;
            int sdsCount = 0;
            int sdcount = shapePath.Length * subdivision;
            var sdShapePath = new Vector3[sdcount];
            for (int i = 0; i < shapePath.Length; ++i)
            {
                Vector3 dst = (i == shapePath.Length - 1) ? shapePath[0] : shapePath[i + 1];
                for (int j = 0; j < subdivision; ++j)
                    sdShapePath[sdsCount++] = Vector3.Lerp(shapePath[i], dst, j / (float)subdivision);
            }
            return sdShapePath;
        }

        public static Bounds GenerateShapeMesh(Mesh mesh, Vector3[] shapePath, float falloffDistance)
        {
            var meshInteriorColor = new Color(1.0f,0,0,1.0f);
            var meshExteriorColor = new Color(1.0f,0,0,1.0f);
            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);

            var vcount = 0;
            var icount = 0;
            var ocount = 0;
            var vertices = new NativeArray<ParametricLightMeshVertex>(shapePath.Length * 64, Allocator.Temp);
            var indices = new NativeArray<ushort>(shapePath.Length * 64, Allocator.Temp);
            var pointCount = shapePath.Length;

            // Create interior geometry
            var inputs = new ContourVertex[pointCount];
            for (var i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y, Z = 1.0f }};
            Tessellate(inputs, indices, vertices, ref vcount, ref icount, meshInteriorColor, meshExteriorColor);
            ocount = icount;

            // Create falloff geometry
            var subdiv = 8;
            inputs = new ContourVertex[(pointCount * (subdiv + 1)) + 2];
            for (var i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y, Z = 0 }};
            inputs[pointCount] = inputs[0];

            // Subdivide
            var extrusionDirs = GetFalloffShape(shapePath);
            shapePath = Subdivide(shapePath, subdiv);
            var exPointCount = shapePath.Length;
            for (var i = 0; i < exPointCount; i++)
            {
                var idx = (int)(math.floor((float) i / (float) subdiv));
                var p = new Vec3()
                {
                    X = shapePath[i].x + (falloffDistance * extrusionDirs[idx].x),
                    Y = shapePath[i].y + (falloffDistance * extrusionDirs[idx].y),
                    Z = 0
                };
                inputs[1 + pointCount + i] = new ContourVertex() { Position = p };
            }
            inputs[inputs.Length - 1] = inputs[pointCount + 1];
            Tessellate(inputs, indices, vertices, ref vcount, ref icount, meshInteriorColor, meshExteriorColor);

            var fvertices = new NativeArray<ParametricLightMeshVertex>(vcount, Allocator.Temp);
            var findices = new NativeArray<ushort>(icount, Allocator.Temp);
            unsafe
            {
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafePtr(fvertices), NativeArrayUnsafeUtility.GetUnsafePtr(vertices), vcount * UnsafeUtility.SizeOf<ParametricLightMeshVertex>());
            }
            for (int i = 0; i < ocount; ++i)
                findices[i] = indices[i];
            for (int i = ocount; i < icount; ++i)
                findices[i] = (ushort)(indices[i] + (ushort)pointCount);

            mesh.SetVertexBufferParams(vcount, ParametricLightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(fvertices, 0, 0, vcount);
            mesh.SetIndices(findices, MeshTopology.Triangles, 0, false);

            return new Bounds
            {
                min = min,
                max = max
            };
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

