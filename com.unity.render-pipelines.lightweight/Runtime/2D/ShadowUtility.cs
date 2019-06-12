using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class ShadowUtility
    {
        internal struct Edge : IComparable<Edge>
        {
            public int objectIndex;
            public ushort vertexIndex0;
            public ushort vertexIndex1;
            public bool swapped;
            public Vector3 tangent;
            public Vector3 normal;

            public void AssignVertexIndices(ushort vi0, ushort vi1)
            {

                if (vi0 < vi1)
                {
                    vertexIndex0 = vi0;
                    vertexIndex1 = vi1;
                    swapped = false;
                }
                else
                {
                    vertexIndex0 = vi1;
                    vertexIndex1 = vi0;
                    swapped = true;
                }
            }

            public int CompareTo(Edge edgeToCompare)
            {
                int objDelta = objectIndex - edgeToCompare.objectIndex;

                // sort like objects together first
                if (objDelta == 0)
                {
                    // sort like edges together as well
                    int deltaVI0 = vertexIndex0 - edgeToCompare.vertexIndex0;
                    int deltaVI1 = vertexIndex1 - edgeToCompare.vertexIndex1;

                    if (deltaVI0 == 0)
                        return deltaVI1;
                    else
                        return deltaVI0; ;
                }
                else
                    return objDelta;
            }
        }

        internal struct VertexLink
        {
            public int nextVertex;
            public int previousVertex;
        }


        internal static void PopulateVerticesPerObject(List<Light2DReactorManager.MeshInfo> meshesToProcess, NativeArray<int> vertexStartIndices, out int totalVertices, out int totalEdges)
        {
            totalVertices = 0;
            totalEdges = 0;
            for (int meshIndex = 0; meshIndex < meshesToProcess.Count; meshIndex++)
            {
                vertexStartIndices[meshIndex] = totalVertices;
                totalVertices += meshesToProcess[meshIndex].vertices.Length;
                totalEdges += meshesToProcess[meshIndex].triangles.Length;
            }
        }

        internal static void PopulateEdgesToProcess(List<Light2DReactorManager.MeshInfo> meshesToProcess, int totalEdges, NativeArray<Edge> edgesToProcess)
        {
            // May want to reconsider this code later so that we can better leverage native container functionality
            int edgeIndex = 0;
            for (int meshIndex = 0; meshIndex < meshesToProcess.Count; meshIndex++)
            {
                Light2DReactorManager.MeshInfo currentMesh = meshesToProcess[meshIndex];
                Vector3 normal = Vector3.forward; // currentMesh.mesh.normals[0];
                for (int triIndex = 0; triIndex < totalEdges; triIndex += 3)
                {

                    Edge edge0 = new Edge();
                    Edge edge1 = new Edge();
                    Edge edge2 = new Edge();

                    
                    edge0.objectIndex  = meshIndex;
                    edge0.AssignVertexIndices(currentMesh.triangles[triIndex], currentMesh.triangles[triIndex + 1]);
                    edge0.normal = normal;

                    edge1.objectIndex  = meshIndex;
                    edge1.AssignVertexIndices(currentMesh.triangles[triIndex + 1], currentMesh.triangles[triIndex + 2]);
                    edge1.normal = normal;

                    edge2.objectIndex = meshIndex;
                    edge2.AssignVertexIndices(currentMesh.triangles[triIndex + 2], currentMesh.triangles[triIndex]);
                    edge2.normal = normal;

                    edgesToProcess[edgeIndex] = edge0;
                    edgesToProcess[edgeIndex + 1] = edge1;
                    edgesToProcess[edgeIndex + 2] = edge2;

                    edgeIndex += 3;
                }
            }
        }

        internal static void SortEdges(NativeArray<Edge> edgesToProcess)
        {
            edgesToProcess.Sort<Edge>();
        }

        static void InitializeVertexLinks(NativeArray<VertexLink> vertexLinks)
        {
            for (int i = 0; i < vertexLinks.Length; i++)
            {
                VertexLink link;
                link.nextVertex = -1;
                link.previousVertex = -1;
                vertexLinks[i] = link;
            }
        }

        static void AddLinkIndex(NativeArray<VertexLink> vertexLinks, int objectStartIndex, int vertexIndex, bool next)
        {
            int vertexLinkIndex = objectStartIndex;
            VertexLink link = vertexLinks[vertexLinkIndex];
            
            if (next)
                link.nextVertex = vertexIndex;
            else
                link.previousVertex = vertexIndex;

            vertexLinks[vertexLinkIndex] = link;
        }

        internal static void FindEdges(NativeArray<Edge> edgesToProcess, NativeArray<VertexLink> vertexLinks, NativeArray<int> vertexStartIndices)
        {
            InitializeVertexLinks(vertexLinks);

            int numberOfEdgesToProcess = edgesToProcess.Length;
            for (int i = 0; i < numberOfEdgesToProcess; i++)
            {
                // Iterate through the edges to do not do anything with duplicates
                int previousIndex = i - 1;
                int nextIndex = i + 1;

                Edge currentEdge = edgesToProcess[i];
                if ((previousIndex < 0 || (currentEdge.CompareTo(edgesToProcess[i - 1]) != 0)) && (nextIndex >= numberOfEdgesToProcess || (currentEdge.CompareTo(edgesToProcess[i + 1]) != 0)))
                {
                    int objectStartIndex = vertexStartIndices[currentEdge.objectIndex];
                    AddLinkIndex(vertexLinks, objectStartIndex + currentEdge.vertexIndex0, currentEdge.vertexIndex1, true);
                    AddLinkIndex(vertexLinks, objectStartIndex + currentEdge.vertexIndex1, currentEdge.vertexIndex0, false);
                }
            }
        }

        internal static void CalculateEdgeTangents(NativeArray<Edge> edgesToProcess, NativeArray<VertexLink> vertexLinks, NativeArray<int> vertexStartIndices)
        {

        }

        static void DebugDrawEdgesAndTangents(List<Light2DReactorManager.MeshInfo> meshesToProcess, NativeArray<Edge> edgesToProcess)
        {
            int numberOfEdgesToProcess = edgesToProcess.Length;
            for (int i=0;i<numberOfEdgesToProcess;i++)
            {
                Edge currentEdge = edgesToProcess[i];

                int previousIndex = i - 1;
                int nextIndex = i + 1;
                if ((previousIndex < 0 || (currentEdge.CompareTo(edgesToProcess[i - 1]) != 0)) && (nextIndex >= numberOfEdgesToProcess || (currentEdge.CompareTo(edgesToProcess[i + 1]) != 0)))
                {
                    Light2DReactorManager.MeshInfo meshInfo = meshesToProcess[currentEdge.objectIndex];
                    Vector3 vertex0 = meshInfo.vertices[currentEdge.vertexIndex0];
                    Vector3 vertex1 = meshInfo.vertices[currentEdge.vertexIndex1];
                    Debug.DrawLine(vertex0, vertex1, Color.red, 30);

                    float tanLen = 0.2f;
                    Vector3 right;
                    if (currentEdge.swapped)
                        right = Vector3.Normalize(vertex0 - vertex1);
                    else
                        right = Vector3.Normalize(vertex1 - vertex0);

                    Vector3 tangent = Vector3.Cross(Vector3.forward, right);
                    Debug.DrawLine(vertex0, vertex0 + tanLen * tangent, Color.red, 30);
                    Debug.DrawLine(vertex1, vertex1 + tanLen * tangent, Color.red, 30);
                }
            }
        }


        internal static void ProcessMeshesForShadows(List<Light2DReactorManager.MeshInfo> meshesToProcess)
        {
            int totalVertices;
            int totalEdges;

            NativeArray<int> vertexStartIndices = new NativeArray<int>(meshesToProcess.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory); ;
            PopulateVerticesPerObject(meshesToProcess, vertexStartIndices, out totalVertices, out totalEdges);

            NativeArray<Edge> edgesToProcess = new NativeArray<Edge>(totalEdges, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            PopulateEdgesToProcess(meshesToProcess, totalEdges, edgesToProcess);

            SortEdges(edgesToProcess); // Can be multithreaded later as an optimization

            NativeArray<VertexLink> vertexLinks = new NativeArray<VertexLink>(totalVertices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            FindEdges(edgesToProcess, vertexLinks, vertexStartIndices);  // Can be multithreaded later as an optimization
            CalculateEdgeTangents(edgesToProcess, vertexLinks, vertexStartIndices);  // Can be multithreaded later as an optimization

            DebugDrawEdgesAndTangents(meshesToProcess, edgesToProcess);

            // Cleanup array allocations
            edgesToProcess.Dispose();
            vertexStartIndices.Dispose();
            vertexLinks.Dispose();

            meshesToProcess.Clear();
        }
    }
}
