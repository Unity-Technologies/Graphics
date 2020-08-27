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

        static bool LineIntersection(float epsilon, float2 p1, float2 p2, float2 p3, float2 p4, ref float2 result)
        {
            float bx = p2.x - p1.x;
            float by = p2.y - p1.y;
            float dx = p4.x - p3.x;
            float dy = p4.y - p3.y;
            float bDotDPerp = bx * dy - by * dx;
            if (math.abs(bDotDPerp) < epsilon)
            {
                return false;
            }
            float cx = p3.x - p1.x;
            float cy = p3.y - p1.y;
            float t = (cx * dy - cy * dx) / bDotDPerp;
            if ((t >= -epsilon) && (t <= 1.0f + epsilon))
            {
                result.x = p1.x + t * bx;
                result.y = p1.y + t * by;
                return true;
            }
            return false;
        }        
        
        static NativeArray<float4> GetFalloff(Vector3[] shapePath, float fallOff, Color meshInterior, ref List<ContourVertex> vertices)
        {
            var shapePathLength = shapePath.Length;
            var extrusionDir = new NativeArray<float2>(shapePathLength, Allocator.Temp);
            var smoothInputs = new NativeArray<float4>(shapePathLength * 3, Allocator.Temp);
            var bi = 0;
            
            for (var i = 0; i < shapePathLength; ++i)
            {
                var j = (i + 1) % shapePath.Length;
                
                var cp = shapePath[i];
                var np = shapePath[j];
                
                float3 npd = np - cp;
                if (math.length(npd) < 0.001f)
                    continue;
                
                var vr = math.normalize(npd);
                var vn = new float2(-vr.y, vr.x);

                var va = -math.normalize(vn);
                var dir = new float2(va.x, va.y);
                extrusionDir[i] = dir;
            }
            
            for (var i = 0; i < shapePathLength; i++)
            {
                var j = (i == shapePath.Length - 1) ? 0 : (i + 1);
                var sPI = shapePath[i];
                var sPJ = shapePath[j];
                var eDI = extrusionDir[i];
                var eDJ = extrusionDir[i];

                // LibTess Inputs.
                var a = new Vec3() { X = sPI.x, Y = sPI.y, Z = 0 };
                var b = new Vec3() { X = sPI.x + (fallOff * eDI.x), Y = sPI.y + (fallOff * eDI.y), Z = 0 };
                var c = new Vec3() { X = sPJ.x + (fallOff * eDJ.x), Y = sPJ.y + (fallOff * eDJ.y), Z = 0 };
                var d = new Vec3() { X = sPJ.x, Y = sPJ.y, Z = 0 };

                // For Bevels. Check if they overlap in which generating offset polygon will result in Artifacts.
                var intersects = false;
                if (i != 0)
                {
                    var verticesCount = vertices.Count;
                    var pIL = verticesCount - 3;
                    var pIR = verticesCount - 2;
                    var pVL = new float2(vertices[pIL].Position.X, vertices[pIL].Position.Y);
                    var pVR = new float2(vertices[pIR].Position.X, vertices[pIR].Position.Y);
                    var cVL = new float2(b.X, b.Y);
                    var cVR = new float2(c.X, c.Y);
                    var iNP = new float2();
                    
                    intersects = LineIntersection(Mathf.Epsilon, pVL, pVR, cVR, cVL, ref iNP);
                    if (intersects)
                    {
                        b = new Vec3() {X = iNP.x, Y = iNP.y, Z = 0};
                        smoothInputs[bi - 1] = new float4(smoothInputs[bi - 1].x, smoothInputs[bi - 1].y, smoothInputs[bi - 1].z, 1.0f);
                        vertices[pIR] = new ContourVertex() { Position = new Vec3(){ X = iNP.x, Y = iNP.y, Z = 0}, Data = vertices[pIR].Data };
                    }
                    smoothInputs[bi++] = new float4(b.X, b.Y, b.Z, 0);
                }
                
                vertices.Add( new ContourVertex() { Position = a, Data = meshInterior } );
                vertices.Add( new ContourVertex() { Position = b, Data = new Color(eDI.x, eDI.y, 0, 0) } );
                vertices.Add( new ContourVertex() { Position = c, Data = new Color(eDJ.x, eDI.y, 0, 0) } );
                vertices.Add( new ContourVertex() { Position = d, Data = meshInterior } );                
                
                smoothInputs[bi++] = new float4(c.X, c.Y, c.Z, 0);
                smoothInputs[bi++] = new float4(d.X, d.Y, d.Z, 0);
            }
            smoothInputs[bi++] = new float4(vertices[1].Position.X, vertices[1].Position.Y, vertices[2].Position.Z, 0);
            return smoothInputs;
        }
        
        static float SlopeAngle(float3 dirNormalized)
        {
            float3 dvup = new float3(0, 1f, 0);
            float3 dvrt = new float3(1f, 0, 0);

            float dr = math.dot(dirNormalized, dvrt);
            float du = math.dot(dirNormalized, dvup);
            float cu = math.acos(du);
            float sn = dr >= 0 ? 1.0f : -1.0f;
            float an = cu * Mathf.Rad2Deg * sn;

            // Adjust angles when direction is parallel to Up Axis.
            an = (du != 1f) ? an : 0;
            an = (du != -1f) ? an : -180f;
            return (360.0f + an) % 360.0f;
        }
        
        static void GetArc(NativeArray<float3> arcPoints, float3 from, float3 to, float3 pivot, float3 center, float radius, float detail)
        {
            var fq = SlopeAngle(math.normalize(from));
            var tq = SlopeAngle(math.normalize(to));
            var sq = SlopeAngle(math.normalize(pivot));

            var fd = math.abs(sq - fq);
            var td = math.abs(tq - sq);
            var df = math.abs(fd - td);
            
            if (df > 1.0f)
                fq = (fd > td) ? (sq + td) : (fq + 360);

            var arcLength = tq - fq;
            var ix = 0;
            for (int i = 0; i <= detail; i++)
            {
                var x = Mathf.Sin(Mathf.Deg2Rad * fq) * radius;
                var y = Mathf.Cos(Mathf.Deg2Rad * fq) * radius;
                var pt = new float3(x, y, 0);
                arcPoints[ix++] = (math.normalize(pt) * radius) + center;
                fq += (arcLength / detail);
            }
        }

        static void GenerateBevels(NativeArray<float4> bevel, float fallOff, Color meshInteriorColor, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, ref int vcount, ref int icount)
        {
            var vertexCount = bevel.Length;
            var quality = 4;
            var arcPoints = new NativeArray<float3>(quality + 1, Allocator.Temp);
            for (var i = 0; i < vertexCount; i = i + 3)
            {
                if ( 0 != bevel[i + 1].w )
                    continue;
                
                // Gnerate Pivot Position
                var sp = new float3(bevel[i + 0].x, bevel[i + 0].y, bevel[i + 0].z);
                var ip = new float3(bevel[i + 1].x, bevel[i + 1].y, bevel[i + 1].z);
                var ep = new float3(bevel[i + 2].x, bevel[i + 2].y, bevel[i + 2].z);
                
                var isn = math.normalize(sp - ip);
                var ien = math.normalize(ep - ip);                
                var sen = math.normalize(isn + ien);
                var ptp = ip + (sen * fallOff);

                var pivotPoint = new ParametricLightMeshVertex() {position = ip, color = meshInteriorColor};
                var startPoint = new ParametricLightMeshVertex() {position = sp, color = new Color(isn.x, isn.y, 0, 0)};
                GetArc(arcPoints, isn, ien, sen, ip, fallOff, quality);
                
                for (int n = 0; n <= quality; ++n)
                {
                    float3 r = arcPoints[n];
                    float3 d = math.normalize(ptp - r);

                    indices[icount++] = (ushort)vcount;
                    indices[icount++] = (ushort)(vcount + 1);
                    indices[icount++] = (ushort)(vcount + 2); 
                    vertices[vcount++] = pivotPoint;
                    vertices[vcount++] = startPoint;
                    startPoint = new ParametricLightMeshVertex() { position = r, color = new Color(d.x, d.y, 0, 0) };
                    vertices[vcount++] = startPoint;
                }
            }
        }

        static Bounds Tessellate(Tess tess, NativeArray<ushort> indices,
            NativeArray<ParametricLightMeshVertex> vertices, ref int vcount, ref int icount)
        {
            tess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

            var iout = tess.Elements.Select(i => i);
            var vout = tess.Vertices.Select(v => 
                new ParametricLightMeshVertex() { position = 
                    new float3(v.Position.X, v.Position.Y, 0), color = v.Data != null ? (Color)v.Data : Color.white });

            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);
            var bounds = new Bounds {min = min, max = max};

            foreach(var v in vout)
            {
                min = math.min(min, v.position);
                max = math.max(max, v.position);
                vertices[vcount++] = new ParametricLightMeshVertex { position = v.position, color = v.color };
            }

            foreach (var i in iout)
            {
                indices[icount++] = (ushort)i;
            }

            return new Bounds {min = min, max = max};
        }

        public static Bounds GenerateShapeMesh(Mesh mesh, Vector3[] shapePath, float falloffDistance)
        {
            var meshInteriorColor = new Color(1.0f,0,0,1.0f);
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
            var outer = new List<ContourVertex>(); 
            var bevel = GetFalloff(shapePath, falloffDistance, meshInteriorColor, ref outer);
            tess.AddContour(outer.ToArray(), ContourOrientation.CounterClockwise);
            var bounds = Tessellate(tess, indices, vertices, ref vcount, ref icount);

            // Generate Bevels.
            GenerateBevels(bevel, falloffDistance, meshInteriorColor, indices, vertices, ref vcount, ref icount);
            mesh.SetVertexBufferParams(vcount, ParametricLightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vcount);
            mesh.SetIndices(indices, 0, icount, MeshTopology.Triangles, 0, false);

            return bounds;
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

