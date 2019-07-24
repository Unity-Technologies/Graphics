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
        internal struct Edge : IComparable<Edge>, IComparer<Edge>
        {
            public int vertexIndex0;
            public int vertexIndex1;
            public Vector4 tangent;
            private bool compareReversed; // This is done so that edge AB can equal edge BA

            static public Edge Comparer
            {
                get
                {
                    return new Edge();
                }
            }

            public void AssignVertexIndices(int vi0, int vi1)
            {
                vertexIndex0 = vi0;
                vertexIndex1 = vi1;
                compareReversed = vi0 > vi1;
            }


            public int Compare(Edge a, Edge b)
            {
                int adjustedVertexIndex0A = a.compareReversed ? a.vertexIndex1 : a.vertexIndex0;
                int adjustedVertexIndex1A = a.compareReversed ? a.vertexIndex0 : a.vertexIndex1;
                int adjustedVertexIndex0B = b.compareReversed ? b.vertexIndex1 : b.vertexIndex0;
                int adjustedVertexIndex1B = b.compareReversed ? b.vertexIndex0 : b.vertexIndex1;

                // Sort first by VI0 then by VI1
                int deltaVI0 = adjustedVertexIndex0A - adjustedVertexIndex0B;
                int deltaVI1 = adjustedVertexIndex1A - adjustedVertexIndex1B;

                if (deltaVI0 == 0)
                    return deltaVI1;
                else
                    return deltaVI0;
            }

            public int CompareTo(Edge edgeToCompare)
            {
                return Compare(this, edgeToCompare);
            }
        }

        static Edge CreateEdge(int triangleIndexA, int triangleIndexB, List<Vector3> vertices, List<int> triangles)
        {
            Edge retEdge = new Edge();

            retEdge.AssignVertexIndices(triangles[triangleIndexA], triangles[triangleIndexB]);
            
            Vector3 vertex0 = vertices[retEdge.vertexIndex0];
            Vector3 vertex1 = vertices[retEdge.vertexIndex1];

            Vector3 edgeDir = Vector3.Normalize(vertex1 - vertex0);
            retEdge.tangent = Vector3.Cross(-Vector3.forward, edgeDir);

            return retEdge;
        }

        static void PopulateEdgeArray(List<Vector3> vertices, List<int> triangles, List<Edge> edges)
        {
            for(int triangleIndex=0;triangleIndex<triangles.Count;triangleIndex+=3)
            {
                edges.Add(CreateEdge(triangleIndex, triangleIndex + 1, vertices, triangles));
                edges.Add(CreateEdge(triangleIndex+1, triangleIndex + 2, vertices, triangles));
                edges.Add(CreateEdge(triangleIndex+2, triangleIndex, vertices, triangles));
            }
        }

        static bool IsOutsideEdge(int edgeIndex, List<Edge> edgesToProcess)
        {
            int previousIndex = edgeIndex - 1;
            int nextIndex = edgeIndex + 1;
            int numberOfEdges = edgesToProcess.Count;
            Edge currentEdge = edgesToProcess[edgeIndex];

            return (previousIndex < 0 || (currentEdge.CompareTo(edgesToProcess[edgeIndex - 1]) != 0)) && (nextIndex >= numberOfEdges || (currentEdge.CompareTo(edgesToProcess[edgeIndex + 1]) != 0));
        }

        static void SortEdges(List<Edge> edgesToProcess)
        {
            edgesToProcess.Sort(Edge.Comparer);
        }

        static void CreateShadowTriangles(List<Vector3> vertices, List<int> triangles, List<Vector4> tangents, List<Edge> edges)
        {
            for(int edgeIndex=0; edgeIndex<edges.Count; edgeIndex++)
            {
                if(IsOutsideEdge(edgeIndex, edges))
                {
                    Edge edge = edges[edgeIndex];
                    tangents[edge.vertexIndex1] = -edge.tangent;

                    int newVertexIndex = vertices.Count;
                    vertices.Add(vertices[edge.vertexIndex0]);
                    tangents.Add(-edge.tangent);

                    triangles.Add(edge.vertexIndex0);
                    triangles.Add(newVertexIndex);
                    triangles.Add(edge.vertexIndex1);

                    Vector3 vertex0 = vertices[edge.vertexIndex0];
                    Vector3 vertex1 = vertices[edge.vertexIndex1];
                    Debug.DrawLine(vertex0, vertex0 + -(Vector3)edge.tangent, Color.red, 1);
                    Debug.DrawLine(vertex1, vertex1 + -(Vector3)edge.tangent, Color.blue, 1);
                }
            }
        }

        static object InterpCustomVertexData(Vec3 position, object[] data, float[] weights)
        {
            return data[0];
        }

        static void InitializeTangents(int tangentsToAdd, List<Vector4> tangents)
        {
            for (int i = 0; i < tangentsToAdd; i++)
                tangents.Add(Vector4.zero);
        }

        public static void GenerateShadowMesh(ref Mesh mesh, Vector3[] shapePath)
        {
            Color meshInteriorColor = new Color(0, 0, 0, 1);
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector4> tangents = new List<Vector4>();

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

            vertices.AddRange(verticesI);
            triangles.AddRange(indicesI);

            InitializeTangents(vertices.Count, tangents);

            List<Edge> edges = new List<Edge>();
            PopulateEdgeArray(vertices, triangles, edges);
            SortEdges(edges);
            CreateShadowTriangles(vertices, triangles, tangents, edges);

            Vector3[] finalVertices = vertices.ToArray();
            int[] finalTriangles = triangles.ToArray();
            Vector4[] finalTangents = tangents.ToArray();

            mesh.Clear();
            mesh.vertices = finalVertices;
            mesh.triangles = finalTriangles;
            mesh.tangents = finalTangents;
        }

        Mesh CreateShadowPolygon(Vector3 position, float radius, float angle, int sides, ref Mesh mesh)
        {
            if (mesh == null)
                mesh = new Mesh();

            float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                sides = 4;
            }

            if (sides == 4)
            {
                angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
            }

            Vector3[] vertices;
            Vector4[] tangents;
            int[] triangles;

            int extraTriangles = sides; // 1 new triangle for the hard shadow.
            int extraVertices = sides;  // 1 new vertex per side for the hard shadow.

            vertices = new Vector3[1 + sides + extraVertices];
            tangents = new Vector4[1 + sides + extraVertices];
            triangles = new int[3 * (sides + extraTriangles)];


            int centerIndex = sides + extraVertices;
            int lastVertexIndex = 0;

            vertices[centerIndex] = position;
            tangents[centerIndex] = Vector4.zero;
            float radiansPerSide = 2 * Mathf.PI / sides;
            Vector3 lastEndPoint = radius * new Vector3(Mathf.Cos(angleOffset), Mathf.Sin(angleOffset), 0);
            for (int i = 0; i < sides; i++)
            {
                float endAngle = (i + 1) * radiansPerSide;
                float nextEndAngle = (i + 2) * radiansPerSide;
                Vector3 endPoint = radius * new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0); ;
                Vector3 nextEndPoint = radius * new Vector3(Mathf.Cos(nextEndAngle + angleOffset), Mathf.Sin(nextEndAngle + angleOffset), 0); ;

                Vector3 curCross = -Vector3.Normalize(Vector3.Cross((endPoint - lastEndPoint), Vector3.forward));
                Vector3 nextCross = -Vector3.Normalize(Vector3.Cross((nextEndPoint - endPoint), Vector3.forward));

                // Create triangle
                int vertexIndex;

                vertexIndex = (i + 1) % (sides);
                vertices[vertexIndex] = endPoint;
                tangents[vertexIndex] = new Vector4(nextCross.x, nextCross.y, 0, 0);
                tangents[vertexIndex].z = 0;
                tangents[vertexIndex].w = 0;

                int triangleIndex = 3 * i;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = lastVertexIndex;
                triangles[triangleIndex + 2] = centerIndex;

                // Create extra shadow triangle
                int extraVertexIndex = vertexIndex + sides;
                vertices[extraVertexIndex] = endPoint;
                tangents[extraVertexIndex] = new Vector4(curCross.x, curCross.y, 0, 0);

                int extraTriangleIndex = 3 * (i + sides);
                triangles[extraTriangleIndex] = vertexIndex;
                triangles[extraTriangleIndex + 1] = lastVertexIndex;
                triangles[extraTriangleIndex + 2] = extraVertexIndex;


                lastEndPoint = endPoint;
                lastVertexIndex = vertexIndex;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.tangents = tangents;


            return mesh;
        }
    }
}
