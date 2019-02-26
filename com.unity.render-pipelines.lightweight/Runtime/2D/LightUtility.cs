using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class LightUtility
    {
        public static bool CheckForColorChange(Color i, ref Color j)
        {
            bool retVal = i.r != j.r || i.g != j.g || i.b != j.b || i.a != j.a;
            j = i;
            return retVal;
        }

        public static bool CheckForVector2Change(Vector2 i, ref Vector2 j)
        {
            bool retVal = i.x != j.x || i.y != j.y;
            j = i;
            return retVal;
        }

        public static bool CheckForSpriteChange(Sprite i, ref Sprite j)
        {
            // If both are null
            bool retVal = false;

            // If one is not null but the other is
            if (i == null ^ j == null)
                retVal = true;

            // if both are not null then do another test
            if (i != null && j != null)
                retVal = i.GetInstanceID() != j.GetInstanceID();

            j = i;
            return retVal;
        }

        public static bool CheckForChange<T>(T a, ref T b)
        {
            int compareResult = Comparer<T>.Default.Compare(a, b);
            b = a;
            return compareResult != 0;
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
        public static Bounds GenerateParametricMesh(ref Mesh mesh, float radius, Vector2 offset, float angle, int sides, float feathering, Color color, float volumeOpacity)
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
            if (feathering <= 0.0f)
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


            Vector3 posOffset = new Vector3(offset.x, offset.y);
            Color transparentColor = new Color(color.r, color.g, color.b, 0);
            Color volumeColor = new Vector4(1, 1, 1, volumeOpacity);
            vertices[centerIndex] = Vector3.zero + posOffset;
            colors[centerIndex] = color;
            volumeColors[centerIndex] = volumeColor;

            float radiansPerSide = 2 * Mathf.PI / sides;
            for (int i = 0; i < sides; i++)
            {
                float endAngle = (i + 1) * radiansPerSide;
                Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0) + posOffset;

                int vertexIndex;
                if (feathering <= 0.0f)
                {
                    vertexIndex = (i + 1) % sides;
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
                    Vector3 endSplitPoint = (1.0f + feathering * 2.0f) * endPoint;
                    vertexIndex = (2 * i + 2) % (2 * sides);

                    vertices[vertexIndex] = endSplitPoint;
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
                    pos = new Vector3(vertices2d[vertexIdx].x / sprite.bounds.size.x, vertices2d[vertexIdx].y / sprite.bounds.size.y);
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

    }
}
