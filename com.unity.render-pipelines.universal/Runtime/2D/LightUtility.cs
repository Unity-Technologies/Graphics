using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.UIElements;

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
        
        static void CalculateTangents(Vector3 point, Vector3 prevPoint, Vector3 nextPoint, Vector3 forward, float scale, out Vector3 rightTangent, out Vector3 leftTangent)
        {
            Vector3 v1 = (prevPoint - point).normalized;
            Vector3 v2 = (nextPoint - point).normalized;
            Vector3 v3 = v1 + v2;
            Vector3 cross = forward;

            if (prevPoint != nextPoint)
            {
                bool colinear = Mathf.Abs(v1.x * v2.y - v1.y * v2.x + v1.x * v2.z - v1.z * v2.x + v1.y * v2.z - v1.z * v2.y) < 0.01f;

                if (colinear)
                {
                    rightTangent = v2 * scale;
                    leftTangent = v1 * scale;
                    return;
                }

                cross = Vector3.Cross(v1, v2);
            }

            rightTangent = Vector3.Cross(cross, v3).normalized * scale;
            leftTangent = -rightTangent;
        }
        
        static Vector3 BezierPoint(Vector3 startPosition, Vector3 startTangent, Vector3 endTangent, Vector3 endPosition, float t)
        {
            float s = 1.0f - t;
            return s * s * s * startPosition + 3.0f * s * s * t * startTangent + 3.0f * s * t * t * endTangent + t * t * t * endPosition;
        }          

        static List<ContourVertex> GetFalloff(Vector3[] shapePath, float fallOff, Color meshInterior, ref List<Vec3> bevelInputs)
        {
            List<ContourVertex> vertices = new List<ContourVertex>();
            var extrusionDir = new List<Vector2>();
            var shapePathLength = shapePath.Length;
            
            for (var i = 0; i < shapePathLength; ++i)
            {
                var j = (i + 1) % shapePath.Length;
                
                var cp = shapePath[i];
                var np = shapePath[j];
                
                var npd = np - cp;
                if (npd.magnitude < 0.001f)
                    continue;
                
                var vr = npd.normalized;
                vr = new Vector2(-vr.y, vr.x);

                var va = -vr.normalized;
                var dir = new Vector2(va.x, va.y);
                extrusionDir.Add(dir);
            }
            
            for (var i = 0; i < shapePathLength; i++)
            {
                var a = new Vec3()
                {
                    X = shapePath[i].x, Y = shapePath[i].y, Z = 0
                };
                vertices.Add( new ContourVertex() { Position = a, Data = meshInterior } );
                var b = new Vec3() 
                {
                    X = shapePath[i].x + (fallOff * extrusionDir[i].x),
                    Y = shapePath[i].y + (fallOff * extrusionDir[i].y),
                    Z = 0 
                };
                vertices.Add( new ContourVertex() { Position = b, Data = new Color(extrusionDir[i].x, extrusionDir[i].y, 0, 1) } );
                var j = (i == shapePath.Length - 1) ? 0 : (i + 1);
                var c = new Vec3() 
                {
                    X = shapePath[j].x + (fallOff * extrusionDir[i].x),
                    Y = shapePath[j].y + (fallOff * extrusionDir[i].y),
                    Z = 0 
                };
                vertices.Add( new ContourVertex() { Position = c, Data = new Color(extrusionDir[i].x, extrusionDir[i].y, 0, 1) } );
                var d = new Vec3()
                {
                    X = shapePath[j].x, Y = shapePath[j].y, Z = 0
                };
                vertices.Add( new ContourVertex() { Position = d, Data = meshInterior } );
                
                // For Bevels.
                if (i != 0)
                    bevelInputs.Add(b);                
                bevelInputs.Add(c);
                bevelInputs.Add(d);
            }
            bevelInputs.Add(vertices[1].Position);
            return vertices;
        }
        
        static float SlopeAngle(Vector3 dirNormalized)
        {
            Vector3 dvup = new Vector3(0, 1f);
            Vector3 dvrt = new Vector3(1f, 0);

            float dr = math.dot(dirNormalized, dvrt);
            float du = math.dot(dirNormalized, dvup);
            float cu = math.acos(du);
            float sn = dr >= 0 ? 1.0f : -1.0f;
            float an = cu * Mathf.Rad2Deg * sn;

            // Adjust angles when direction is parallel to Up Axis.
            an = (du != 1f) ? an : 0;
            an = (du != -1f) ? an : -180f;
            return an;
        }
        
        static List<Vector3> GetArc(Vector3 from, Vector3 to, Vector3 center, float radius, float detail)
        {
            var arc = new List<Vector3>();
            var fq = SlopeAngle(from.normalized);
            var tq = SlopeAngle(to.normalized);
            
            var arcLength = tq - fq;
            for (int i = 0; i <= detail; i++)
            {
                var x = Mathf.Sin(Mathf.Deg2Rad * fq) * radius;
                var y = Mathf.Cos(Mathf.Deg2Rad * fq) * radius;
                var pt = new Vector3(x, y, 0);
                arc.Add((pt.normalized * radius) + center);
                fq += (arcLength / detail);
            }
            return arc;
        }

        static void GenerateBevels(List<Vec3> bevelInputs, float fallOff, Color meshInteriorColor, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, ref int vcount, ref int icount)
        {
            var vertexCount = bevelInputs.Count;
            var bezierQuality = 4;

            for (var i = 0; i < vertexCount; i = i + 3)
            {
                // Pivot Pos
                List<ContourVertex> bevel = new List<ContourVertex>();
                var sp = new Vector3(bevelInputs[i + 0].X, bevelInputs[i + 0].Y, 0);
                var ip = new Vector3(bevelInputs[i + 1].X, bevelInputs[i + 1].Y, 0);
                var ep = new Vector3(bevelInputs[i + 2].X, bevelInputs[i + 2].Y, 0);
                
                var scale = Mathf.Min((ep - ip).magnitude, (sp - ip).magnitude) * 0.33f;
                var lT = (sp - ip).normalized * scale;
                var rT = (ep - ip).normalized * scale;

                var isn = (sp - ip).normalized;
                var ien = (ep - ip).normalized;
                var sen = (isn + ien).normalized;
                var np = ip + (sen * fallOff);

                var pivotPoint = new ParametricLightMeshVertex() {position = ip, color = meshInteriorColor};
                var startPoint = new ParametricLightMeshVertex() {position = sp, color = new Color(isn.x, isn.y, 0, 1)};
                var arc = GetArc(isn, ien, ip, fallOff, bezierQuality);
                
                for (int n = 0; n <= bezierQuality; ++n)
                {
                    Vector3 r = arc[n];
                    Vector3 d = (np - r).normalized;

                    indices[icount++] = (ushort)vcount;
                    indices[icount++] = (ushort)(vcount + 1);
                    indices[icount++] = (ushort)(vcount + 2); 
                    vertices[vcount++] = pivotPoint;
                    vertices[vcount++] = startPoint;
                    startPoint = new ParametricLightMeshVertex() { position = r, color = new Color(d.x, d.y, 0, 1) };
                    vertices[vcount++] = startPoint;
                }
            }
        }

        static void Tessellate(Tess tess, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, ref int vcount, ref int icount)
        {
            tess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

            var iout = tess.Elements.Select(i => i);
            var vout = tess.Vertices.Select(v => 
                new ParametricLightMeshVertex() { position = 
                    new float3(v.Position.X, v.Position.Y, 0), color = v.Data != null ? (Color)v.Data : Color.white });

            foreach(var v in vout)
            {
                vertices[vcount++] = new ParametricLightMeshVertex { position = v.position, color = v.color };
            }

            foreach (var i in iout)
            {
                indices[icount++] = (ushort)i;
            }
        }

        public static Bounds GenerateShapeMesh(Mesh mesh, Vector3[] shapePath, float falloffDistance)
        {
            var meshInteriorColor = new Color(1.0f,0,0,1.0f);
            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);

            var vcount = 0;
            var icount = 0;
            var ix = 0;
            var vertices = new NativeArray<ParametricLightMeshVertex>(shapePath.Length * 64, Allocator.Temp);
            var indices = new NativeArray<ushort>(shapePath.Length * 64, Allocator.Temp);
            var tess = new Tess();
            
            // Create shape geometry
            var innerShapeVertexCount = shapePath.Length;
            var inner = new ContourVertex[innerShapeVertexCount + 1];
            for (var i = 0; i < innerShapeVertexCount; ++i)
                inner[ix++] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y, Z = 0 }, Data = meshInteriorColor };
            inner[ix++] = inner[0];
            tess.AddContour(inner, ContourOrientation.CounterClockwise);

            // Create falloff geometry
            var bevelInputs = new List<Vec3>(); 
            var outer = GetFalloff(shapePath, falloffDistance, meshInteriorColor, ref bevelInputs);
            tess.AddContour(outer.ToArray(), ContourOrientation.CounterClockwise);
            Tessellate(tess, indices, vertices, ref vcount, ref icount);

            // Generate Bevels.
            GenerateBevels(bevelInputs, falloffDistance, meshInteriorColor, indices, vertices, ref vcount, ref icount);
            mesh.SetVertexBufferParams(vcount, ParametricLightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vcount);
            mesh.SetIndices(indices, 0, icount, MeshTopology.Triangles, 0, false);

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

