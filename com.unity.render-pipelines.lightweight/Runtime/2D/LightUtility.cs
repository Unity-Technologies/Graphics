using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering.LWRP.LibTessDotNet;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal static class LightUtility
    {
        public static bool CheckForChange<T>(T a, ref T b)
        {
            bool changed = !Equals(a,b);
            b = a;
            return changed;
        }

        public static Bounds CalculateBoundingSphere(ref Vector3[] vertices)
        {
            Bounds localBounds = new Bounds();

            Vector3 minimum = new Vector3(float.MaxValue, float.MaxValue);
            Vector3 maximum = new Vector3(float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                minimum.x = vertex.x < minimum.x ? vertex.x : minimum.x;
                minimum.y = vertex.y < minimum.y ? vertex.y : minimum.y;
                maximum.x = vertex.x > maximum.x ? vertex.x : maximum.x;
                maximum.y = vertex.y > maximum.y ? vertex.y : maximum.y;
            }

            localBounds.max = maximum;
            localBounds.min = minimum;

            return localBounds;
        }

        // Takes in a mesh that
        public static Bounds GenerateParametricMesh(ref Mesh mesh, float radius, Vector2 falloffOffset, float angle, int sides, float feathering, Color color, float volumeOpacity)
        {
            if (mesh == null)
                mesh = new Mesh();

            float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                sides = 4;
            }

            if(sides == 4)
            {
                angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
            }

            // Return a shape with radius = 1
            Vector3[] vertices;
            int[] triangles;
            Color[] colors;
            Vector4[] volumeColors;

            int centerIndex;
            if (feathering <= 0.0f || radius == 0)
            {
                vertices = new Vector3[1 + sides];
                triangles = new int[3 * sides];
                colors = new Color[1 + sides];
                volumeColors = new Vector4[1 + sides];
                centerIndex = sides;
            }
            else
            {
                vertices = new Vector3[1 + 2 * sides];
                colors = new Color[1 + 2 * sides];
                triangles = new int[3 * 3 * sides];
                volumeColors = new Vector4[1 + 2 * sides];
                centerIndex = 2 * sides;
            }

            color.a = 1;
            Vector3 featherOffset = new Vector3(falloffOffset.x, falloffOffset.y);
            Color transparentColor = new Color(color.r, color.g, color.b, 0);
            Color volumeColor = new Vector4(1, 1, 1, volumeOpacity);
            vertices[centerIndex] = Vector3.zero;
            colors[centerIndex] = color;
            volumeColors[centerIndex] = volumeColor;
            float radiansPerSide = 2 * Mathf.PI / sides;

            bool createSimpleShape = false;
            if (radius <= 0 || feathering <= 0)
            {
                color = transparentColor;
                if(radius == 0)
                    radius = feathering;
                createSimpleShape = true;
            }

            for (int i = 0; i < sides; i++)
            {
                float endAngle = (i + 1) * radiansPerSide;
                Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0);

                int vertexIndex;
                if(createSimpleShape)
                {
                    vertexIndex = i % sides;
                    vertices[vertexIndex] = endPoint;
                    colors[vertexIndex] = color;
                    volumeColors[vertexIndex] = volumeColor;

                    int triangleIndex = 3 * i;
                    triangles[triangleIndex] = (i + 1) % sides;
                    triangles[triangleIndex + 1] = i;
                    triangles[triangleIndex + 2] = centerIndex;
                }
                else
                {
                    Vector3 endSplitPoint = endPoint + feathering * Vector3.Normalize(endPoint);
                    vertexIndex = (2 * i + 2) % (2 * sides);

                    vertices[vertexIndex] = endSplitPoint + featherOffset;
                    vertices[vertexIndex + 1] = endPoint;

                    colors[vertexIndex] = transparentColor;
                    colors[vertexIndex + 1] = color;
                    volumeColors[vertexIndex] = volumeColor;
                    volumeColors[vertexIndex + 1] = volumeColor;

                    // Triangle 1 (Tip)
                    int triangleIndex = 9 * i;
                    triangles[triangleIndex] = vertexIndex + 1;
                    triangles[triangleIndex + 1] = 2 * i + 1;
                    triangles[triangleIndex + 2] = centerIndex;

                    // Triangle 2 (Upper Top Left)
                    triangles[triangleIndex + 3] = vertexIndex;
                    triangles[triangleIndex + 4] = 2 * i;
                    triangles[triangleIndex + 5] = 2 * i + 1;

                    // Triangle 2 (Bottom Top Left)
                    triangles[triangleIndex + 6] = vertexIndex + 1;
                    triangles[triangleIndex + 7] = vertexIndex;
                    triangles[triangleIndex + 8] = 2 * i + 1;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.tangents = volumeColors;

            return CalculateBoundingSphere(ref vertices);
        }

        public static Bounds GenerateSpriteMesh(ref Mesh mesh, Sprite sprite, Color color, float volumeOpacity, float scale)
        {
            if (mesh == null)
                mesh = new Mesh();

            if (sprite != null)
            {
                Vector2[] vertices2d = sprite.vertices;
                Vector3[] vertices3d = new Vector3[vertices2d.Length];
                Color[] colors = new Color[vertices2d.Length];
                Vector4[] volumeColor = new Vector4[vertices2d.Length];

                ushort[] triangles2d = sprite.triangles;
                int[] triangles3d = new int[triangles2d.Length];


                Vector3 center = 0.5f * scale * (sprite.bounds.min + sprite.bounds.max);

                for (int vertexIdx = 0; vertexIdx < vertices2d.Length; vertexIdx++)
                {
                    Vector3 pos = new Vector3(vertices2d[vertexIdx].x, vertices2d[vertexIdx].y) - center;
                    vertices3d[vertexIdx] = scale * pos;
                    colors[vertexIdx] = color;
                    volumeColor[vertexIdx] = new Vector4(1, 1, 1, volumeOpacity);
                }

                for (int triangleIdx = 0; triangleIdx < triangles2d.Length; triangleIdx++)
                {
                    triangles3d[triangleIdx] = (int)triangles2d[triangleIdx];
                }

                mesh.Clear();
                mesh.vertices = vertices3d;
                mesh.uv = sprite.uv;
                mesh.triangles = triangles3d;
                mesh.colors = colors;
                mesh.tangents = volumeColor;

                return CalculateBoundingSphere(ref vertices3d);
            }

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        static List<Vector2> UpdateFeatheredShapeLightMesh(ContourVertex[] contourPoints, int contourPointCount, float feathering)
        {
            List<Vector2> feathered = new List<Vector2>();
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
                    var t = cp + (vn * feathering);
                    feathered.Add(t);
                }
            }

            return feathered;
        }

        static object InterpCustomVertexData(Vec3 position, object[] data, float[] weights)
        {
            return data[0];
        }


        public static List<Vector2> GetFeatheredShape(Vector3[] shapePath, float feathering)
        {
            int pointCount = shapePath.Length;
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = null };

            var feathered = UpdateFeatheredShapeLightMesh(inputs, pointCount, feathering);
            return feathered;
        }
        

        public static Bounds GenerateShapeMesh(ref Mesh mesh, Color color, Vector3[] shapePath, Vector2 falloffOffset, float volumeOpacity, float feathering)
        {
            color.a = 1;
            Bounds localBounds;
            Color meshInteriorColor = color;
            Color meshFeatherColor = new Color(color.r, color.g, color.b, 0);

            int pointCount = shapePath.Length;
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = meshFeatherColor };

            var feathered = UpdateFeatheredShapeLightMesh(inputs, pointCount, feathering);
            int featheredPointCount = feathered.Count + pointCount;

            Tess tessI = new Tess();  // Interior
            Tess tessF = new Tess();  // Feathered Edge

            var inputsI = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount - 1; ++i)
            {
                var inputsF = new ContourVertex[4];
                inputsF[0] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = meshInteriorColor };
                inputsF[1] = new ContourVertex() { Position = new Vec3() { X = feathered[i].x, Y = feathered[i].y }, Data = meshFeatherColor };
                inputsF[2] = new ContourVertex() { Position = new Vec3() { X = feathered[i + 1].x, Y = feathered[i + 1].y }, Data = meshFeatherColor };
                inputsF[3] = new ContourVertex() { Position = new Vec3() { X = shapePath[i + 1].x, Y = shapePath[i + 1].y }, Data = meshInteriorColor };
                tessF.AddContour(inputsF, ContourOrientation.Original);

                inputsI[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = meshInteriorColor };
            }

            var inputsL = new ContourVertex[4];
            inputsL[0] = new ContourVertex() { Position = new Vec3() { X = shapePath[pointCount - 1].x, Y = shapePath[pointCount - 1].y }, Data = meshInteriorColor };
            inputsL[1] = new ContourVertex() { Position = new Vec3() { X = feathered[pointCount - 1].x, Y = feathered[pointCount - 1].y }, Data = meshFeatherColor };
            inputsL[2] = new ContourVertex() { Position = new Vec3() { X = feathered[0].x, Y = feathered[0].y }, Data = meshFeatherColor };
            inputsL[3] = new ContourVertex() { Position = new Vec3() { X = shapePath[0].x, Y = shapePath[0].y }, Data = meshInteriorColor };
            tessF.AddContour(inputsL, ContourOrientation.Original);

            inputsI[pointCount - 1] = new ContourVertex() { Position = new Vec3() { X = shapePath[pointCount - 1].x, Y = shapePath[pointCount - 1].y }, Data = meshInteriorColor };
            tessI.AddContour(inputsI, ContourOrientation.Original);

            tessI.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);
            tessF.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);

            var indicesI = tessI.Elements.Select(i => i).ToArray();
            var verticesI = tessI.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var colorsI = tessI.Vertices.Select(v => new Color(((Color)v.Data).r, ((Color)v.Data).g, ((Color)v.Data).b, ((Color)v.Data).a)).ToArray();

            var indicesF = tessF.Elements.Select(i => i + verticesI.Length).ToArray();
            var verticesF = tessF.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var colorsF = tessF.Vertices.Select(v => new Color(((Color)v.Data).r, ((Color)v.Data).g, ((Color)v.Data).b, ((Color)v.Data).a)).ToArray();


            List<Vector3> finalVertices = new List<Vector3>();
            List<int> finalIndices = new List<int>();
            List<Color> finalColors = new List<Color>();
            finalVertices.AddRange(verticesI);
            finalVertices.AddRange(verticesF);
            finalIndices.AddRange(indicesI);
            finalIndices.AddRange(indicesF);
            finalColors.AddRange(colorsI);
            finalColors.AddRange(colorsF);

            var volumeColors = new Vector4[finalColors.Count];
            for (int i = 0; i < volumeColors.Length; i++)
                volumeColors[i] = new Vector4(1, 1, 1, volumeOpacity);

            Vector3[] vertices = finalVertices.ToArray();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.tangents = volumeColors;
            mesh.colors = finalColors.ToArray();
            mesh.SetIndices(finalIndices.ToArray(), MeshTopology.Triangles, 0);

            localBounds = CalculateBoundingSphere(ref vertices);

            return localBounds;
        }
    }
}
