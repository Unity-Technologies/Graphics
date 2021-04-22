using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using System.Linq;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;


namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShadowUtility
    {
        // I need another function to generate the mesh from the outline.
        public static BoundingSphere GenerateShadowMesh(Mesh mesh, Vector3[] shapePath)
        {
            Debug.AssertFormat(shapePath.Length > 3, "Shadow shape path must have 3 or more vertices");

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector4> normals = new List<Vector4>();

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // Add outline vertices
            int pathLength = shapePath.Length;
            for (int i = 0; i < pathLength; i++)
            {
                Vector3 vertex = shapePath[i];
                vertices.Add(vertex);
                normals.Add(Vector3.zero);

                if (minX > vertex.x)
                    minX = vertex.x;
                if (maxX < vertex.x)
                    maxX = vertex.x;

                if (minY > vertex.y)
                    minY = vertex.y;
                if (maxY < vertex.y)
                    maxY = vertex.y;
            }

            // Add extrusion vertices, normals, and triangles
            int vertexCount = pathLength;
            for (int i = 0; i < pathLength; i++)
            {
                int startIndex = i;
                int endIndex = (i + 1) % pathLength;

                Vector3 start = shapePath[startIndex];
                Vector3 end = shapePath[endIndex];

                Vector4 normal = Vector3.Cross(Vector3.Normalize(end - start), -Vector3.forward);
                normal = new Vector4(normal.x, normal.y, end.x, end.y);
                normals.Add(normal);
                normal = new Vector4(normal.x, normal.y, start.x, start.y);
                normals.Add(normal);

                // Triangle 1
                triangles.Add(startIndex);
                triangles.Add(vertexCount);
                triangles.Add(vertexCount + 1);
                // Triangle 2
                triangles.Add(vertexCount + 1);
                triangles.Add(endIndex);
                triangles.Add(startIndex);

                vertices.Add(start);
                vertices.Add(end);

                vertexCount += 2;
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.tangents = normals.ToArray();

            // Calculate bounding sphere (circle)
            Vector3 origin = new Vector2(0.5f * (minX + maxX), 0.5f * (minY + maxY));
            float deltaX = maxX - minX;
            float deltaY = maxY - minY;
            float radius = 0.5f * Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

            BoundingSphere retSphere = new BoundingSphere(origin, radius);

            return retSphere;
        }
    }
}
