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

        internal struct SoftShadowInput
        {
            public List<Edge>    edges;
            public List<Vector3> vertices;
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

        public static SoftShadowInput GenerateHardShadowMesh(Vector3[] shapePath, Mesh mesh)
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

            SoftShadowInput shadowInput = new SoftShadowInput();
            shadowInput.edges = edges;
            shadowInput.vertices = vertices;

            return shadowInput;
        }

        public static void GenerateSoftShadowMesh(SoftShadowInput input, Mesh mesh)
        {
            // This shadow mesh will be rendered twice.
            int numberOfEdgeConnections = input.vertices.Count;
            int[] cwEdgeConnections = new int[numberOfEdgeConnections];
            int[] ccwEdgeConnections = new int[numberOfEdgeConnections];

            int outsideEdgeCount = 0;
            for (int i=0;i<input.edges.Count;i++)
            {
                if (IsOutsideEdge(i, input.edges))
                {
                    Edge edge = input.edges[i];
                    cwEdgeConnections[edge.vertexIndex0] = edge.vertexIndex1;
                    ccwEdgeConnections[edge.vertexIndex1] = edge.vertexIndex0;
                    outsideEdgeCount++;
                }
            }

            // Create 1 triangles per outside vertex. Triangle will be 0,1,2 order. 0 will be pinned to the vertex, 1 will project from light center, 2 will be projected from a tangent
            int vertexCount = 3 * outsideEdgeCount;
            int triangleCount = 3 * outsideEdgeCount;

            Vector3[] outputVertices = new Vector3[vertexCount];
            int[] outputTriangles = new int[triangleCount];
            Vector4[] outputTangents = new Vector4[vertexCount]; // xy is the tangent of one edge, zw is the tangent of the other edge.
            Vector2[] outputUV0 = new Vector2[vertexCount];  // This will define which part of the light we will project from x=1 for middle, y=1 for tangent, x=0 y=0 for pinned. This will also allow is to set the uv coordinates in the shader.

            int startingIndex = 0;
            int currentVertexIndex = 0;
            int nextVertexIndex = cwEdgeConnections[0];
            int prevVertexIndex = ccwEdgeConnections[0];
            bool done = false;

            int outputVertexIndex=0;
            int outputTriangleIndex=0;
            while(!done)
            {
                Vector3 currentVertex = input.vertices[currentVertexIndex];
                Vector3 prevVertex = input.vertices[prevVertexIndex];
                Vector3 nextVertex = input.vertices[nextVertexIndex];

                Vector3 nextDirection = Vector3.Normalize(nextVertex - currentVertex);
                Vector3 nextTangent = Vector3.Cross(-Vector3.forward, nextDirection).normalized;
                Vector3 prevDirection = Vector3.Normalize(currentVertex - prevVertex);
                Vector3 prevTangent = Vector3.Cross(-Vector3.forward, prevDirection).normalized; ;
                Vector4 currentTangent = new Vector4(nextTangent.x, nextTangent.y, prevTangent.x, prevTangent.y);

                outputVertices[outputVertexIndex] = currentVertex;
                outputVertices[outputVertexIndex+1] = currentVertex;
                outputVertices[outputVertexIndex+2] = currentVertex;
                outputTriangles[outputTriangleIndex] = outputTriangleIndex;
                outputTriangles[outputTriangleIndex+1] = outputTriangleIndex+1;
                outputTriangles[outputTriangleIndex+2] = outputTriangleIndex+2;
                outputTangents[outputVertexIndex] = Vector4.zero;
                outputTangents[outputVertexIndex + 1] = currentTangent;
                outputTangents[outputVertexIndex + 2] = currentTangent;
                outputUV0[outputVertexIndex] = new Vector2(0, 0);
                outputUV0[outputVertexIndex+1] = new Vector2(1, 0);
                outputUV0[outputVertexIndex+2] = new Vector2(0, 1);

                
                currentVertexIndex = nextVertexIndex;
                prevVertexIndex = ccwEdgeConnections[currentVertexIndex];
                nextVertexIndex = cwEdgeConnections[currentVertexIndex];
                outputVertexIndex += 3;
                outputTriangleIndex += 3;
                done = startingIndex == currentVertexIndex;
            }

            mesh.Clear();
            mesh.vertices = outputVertices.ToArray();
            mesh.triangles = outputTriangles.ToArray();
            mesh.tangents = outputTangents.ToArray();
            mesh.uv = outputUV0.ToArray();
        }
    }
}
