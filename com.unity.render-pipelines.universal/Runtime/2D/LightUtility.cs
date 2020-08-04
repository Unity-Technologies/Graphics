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
        // public static bool CheckForChange<T>(T a, ref T b) where T : struct
        // {
        //     var changed = !EqualityComparer<T>.Default.Equals(a, b);
        //     b = a;
        //     return changed;
        // }

        public static bool CheckForChange(int a, ref int b)
        {
            bool changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(float a, ref float b)
        {
            bool changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(bool a, ref bool b)
        {
            bool changed = a != b;
            b = a;
            return changed;
        }

        // public static bool CheckForChange(Vector2 a, ref Vector2 b)
        // {
        //     bool changed = a != b;
        //     b = a;
        //     return changed;
        // }
        //
        // public static bool CheckForChange(Sprite a, ref Sprite b)
        // {
        //     bool changed = !Equals(a, b);
        //     b = a;
        //     return changed;
        // }

        public static Bounds CalculateBoundingSphere(ref Vector3[] vertices, ref Color[] colors, float falloffDistance)
        {
            Bounds localBounds = new Bounds();

            Vector3 minimum = new Vector3(float.MaxValue, float.MaxValue);
            Vector3 maximum = new Vector3(float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                vertex.x += falloffDistance * colors[i].r;
                vertex.y += falloffDistance * colors[i].g;

                minimum.x = vertex.x < minimum.x ? vertex.x : minimum.x;
                minimum.y = vertex.y < minimum.y ? vertex.y : minimum.y;
                maximum.x = vertex.x > maximum.x ? vertex.x : maximum.x;
                maximum.y = vertex.y > maximum.y ? vertex.y : maximum.y;
            }

            localBounds.max = maximum;
            localBounds.min = minimum;

            return localBounds;
        }

        private struct ParametricLightMeshVertex
        {
            public float3 position;
            public Color color;
        }

        private struct SpriteLightMeshVertex
        {
            public Vector3 position;
            public Color color;
            public Vector2 uv;
        }

        // Takes in a mesh that
        public static Bounds GenerateParametricMesh(ref Mesh mesh, float radius, float falloffDistance, float angle, int sides)
        {
            if (mesh == null)
                mesh = new Mesh();

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

            for (var i = 0; i < sides; i++)
            {
                var endAngle = (i + 1) * radiansPerSide;

                var extrudeDir = new float3(math.cos(endAngle + angleOffset), math.sin(endAngle + angleOffset), 0);
                var endPoint = radius * extrudeDir;

                var vertexIndex = (ushort)((2 * i + 2) % (2 * sides));

                vertices[vertexIndex] = new ParametricLightMeshVertex
                {
                    position = endPoint, // This is the extruded endpoint
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
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = (ushort)(2 * i);
                triangles[triangleIndex + 5] = (ushort)(2 * i + 1);

                // Triangle 2 (Bottom Top Left)
                triangles[triangleIndex + 6] = (ushort)(vertexIndex + 1);
                triangles[triangleIndex + 7] = vertexIndex;
                triangles[triangleIndex + 8] = (ushort)(2 * i + 1);
            }

            mesh.Clear(); // not sure we need this
            mesh.SetVertexBufferParams(vertexCount, new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            });
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0, true);

            vertices.Dispose();
            triangles.Dispose();

            // as parametric, this should be calculatable
            return mesh.bounds;
        }

        public static Bounds GenerateSpriteMesh(ref Mesh mesh, Sprite sprite)
        {
            if (mesh == null)
                mesh = new Mesh();

            if (sprite != null)
            {
                // this needs to be called before getting UV at the line below
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
                mesh.SetVertexBufferParams(vertices.Length, new []
                {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                });
                mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
                mesh.SetIndices(srcIndices, MeshTopology.Triangles, 0, true);
                return mesh.bounds;
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        static void GetFalloffExtrusion(ContourVertex[] contourPoints, int contourPointCount, ref List<Vector2> extrusionDir)
        {
            for (int i = 0; i < contourPointCount; ++i)
            {
                int h = (i == 0) ? (contourPointCount - 1) : (i - 1);
                int j = (i + 1) % contourPointCount;

                Vector2 pp = new Vector2(contourPoints[h].Position.X, contourPoints[h].Position.Y);
                Vector2 cp = new Vector2(contourPoints[i].Position.X, contourPoints[i].Position.Y);
                Vector2 np = new Vector2(contourPoints[j].Position.X, contourPoints[j].Position.Y);

                Vector2 cpd = cp - pp;
                Vector2 npd = np - cp;
                if (cpd.magnitude < 0.001f || npd.magnitude < 0.001f)
                    continue;

                Vector2 vl = cpd.normalized;
                Vector2 vr = npd.normalized;

                vl = new Vector2(-vl.y, vl.x);
                vr = new Vector2(-vr.y, vr.x);

                Vector2 va = vl.normalized + vr.normalized;
                Vector2 vn = -va.normalized;

                if (va.magnitude > 0 && vn.magnitude > 0)
                {
                    Vector2 dir = new Vector2(vn.x, vn.y);
                    extrusionDir.Add(dir);
                }
            }
        }

        static object InterpCustomVertexData(Vec3 position, object[] data, float[] weights)
        {
            return data[0];
        }


        public static void GetFalloffShape(Vector3[] shapePath, ref List<Vector2> extrusionDir)
        {
            int pointCount = shapePath.Length;
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = null };

            GetFalloffExtrusion(inputs, pointCount, ref extrusionDir);
        }

        public static Bounds GenerateShapeMesh(ref Mesh mesh, Vector3[] shapePath, float falloffDistance)
        {
            Bounds localBounds;
            Color meshInteriorColor = new Color(0,0,0,1);
            List<Vector3> finalVertices = new List<Vector3>();
            List<int> finalIndices = new List<int>();
            List<Color> finalColors = new List<Color>();

            // Create interior geometry
            int pointCount = shapePath.Length;
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = meshInteriorColor };

            Tess tessI = new Tess();
            tessI.AddContour(inputs, ContourOrientation.Original);
            tessI.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);

            var indicesI = tessI.Elements.Select(i => i).ToArray();
            var verticesI = tessI.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var colorsI = tessI.Vertices.Select(v => new Color(((Color)v.Data).r, ((Color)v.Data).g, ((Color)v.Data).b, ((Color)v.Data).a)).ToArray();

            finalVertices.AddRange(verticesI);
            finalIndices.AddRange(indicesI);
            finalColors.AddRange(colorsI);

            // Create falloff geometry
            List<Vector2> extrusionDirs = new List<Vector2>();
            GetFalloffShape(shapePath, ref extrusionDirs);

            pointCount = finalVertices.Count;
            int falloffPointCount = 2 * shapePath.Length;
            for (int i = 0; i < shapePath.Length; i++)
            {
                // Making triangles ABD and DCA
                int triangleIndex = 2 * i;
                int aIndex = pointCount + triangleIndex;
                int bIndex = pointCount + triangleIndex + 1;
                int cIndex = pointCount + (triangleIndex + 2) % falloffPointCount;
                int dIndex = pointCount + (triangleIndex + 3) % falloffPointCount;

                Vector3 point = shapePath[i];

                // We are making degenerate triangles which will be extruded by the shader
                finalVertices.Add(point);
                finalVertices.Add(point);

                finalIndices.Add(aIndex);
                finalIndices.Add(bIndex);
                finalIndices.Add(dIndex);

                finalIndices.Add(dIndex);
                finalIndices.Add(cIndex);
                finalIndices.Add(aIndex);

                Color aColor = new Color(0, 0, 0, 1);
                Color bColor = new Color(extrusionDirs[i].x, extrusionDirs[i].y, 0, 0);

                finalColors.Add(aColor);
                finalColors.Add(bColor);
            }

            Color[] colors = finalColors.ToArray();
            Vector3[] vertices = finalVertices.ToArray();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetIndices(finalIndices.ToArray(), MeshTopology.Triangles, 0);

            localBounds = CalculateBoundingSphere(ref vertices, ref colors, falloffDistance);

            return localBounds;
        }


        public static void AddShadowCasterGroupToList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            int positionToInsert = 0;
            for (positionToInsert = 0; positionToInsert < list.Count; positionToInsert++)
            {
                if (shadowCaster.GetShadowGroup() == list[positionToInsert].GetShadowGroup())
                    break;
            }

            list.Insert(positionToInsert, shadowCaster);
        }


        public static void RemoveShadowCasterGroupFromList(ShadowCasterGroup2D shadowCaster, List<ShadowCasterGroup2D> list)
        {
            list.Remove(shadowCaster);
        }


        static CompositeShadowCaster2D FindTopMostCompositeShadowCaster(ShadowCaster2D shadowCaster)
        {
            CompositeShadowCaster2D retGroup = null;

            Transform transformToCheck = shadowCaster.transform.parent;
            while(transformToCheck != null)
            {
                CompositeShadowCaster2D currentGroup = transformToCheck.GetComponent<CompositeShadowCaster2D>();
                if (currentGroup != null)
                    retGroup = currentGroup;

                transformToCheck = transformToCheck.parent;
            }

            return retGroup;
        }


        public static bool AddToShadowCasterGroup(ShadowCaster2D shadowCaster, ref ShadowCasterGroup2D shadowCasterGroup)
        {
            ShadowCasterGroup2D newShadowCasterGroup = FindTopMostCompositeShadowCaster(shadowCaster) as ShadowCasterGroup2D;

            if (newShadowCasterGroup == null)
                newShadowCasterGroup = shadowCaster.GetComponent<ShadowCaster2D>();

            if (newShadowCasterGroup != null && shadowCasterGroup != newShadowCasterGroup)
            {
                newShadowCasterGroup.RegisterShadowCaster2D(shadowCaster);
                shadowCasterGroup = newShadowCasterGroup;
                return true;
            }

            return false;
        }

        public static void RemoveFromShadowCasterGroup(ShadowCaster2D shadowCaster, ShadowCasterGroup2D shadowCasterGroup)
        {
            if(shadowCasterGroup != null)
                shadowCasterGroup.UnregisterShadowCaster2D(shadowCaster);
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

