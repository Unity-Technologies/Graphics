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


        internal static void PopulateVerticesPerObject(List<ShadowGenerationInfo> meshesToProcess, NativeArray<int> vertexStartIndices, out int totalVertices, out int totalEdges)
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

        internal static void PopulateEdgesToProcess(List<ShadowGenerationInfo> meshesToProcess, int totalEdges, NativeArray<Edge> edgesToProcess)
        {
            // May want to reconsider this code later so that we can better leverage native container functionality
            int edgeIndex = 0;
            for (int meshIndex = 0; meshIndex < meshesToProcess.Count; meshIndex++)
            {
                ShadowGenerationInfo currentMesh = meshesToProcess[meshIndex];
                for (int triIndex = 0; triIndex < totalEdges; triIndex += 3)
                {
                    Edge edge0 = new Edge();
                    Edge edge1 = new Edge();
                    Edge edge2 = new Edge();

                    Vector3 triVert0 = currentMesh.vertices[currentMesh.triangles[triIndex]];
                    Vector3 triVert1 = currentMesh.vertices[currentMesh.triangles[triIndex+1]];
                    Vector3 triVert2 = currentMesh.vertices[currentMesh.triangles[triIndex+2]];

                    //Vector3 normal = -Vector3.forward;
                    //Vector3 normal = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(triVert1 - triVert0), Vector3.Normalize(triVert2 - triVert0)));
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(triVert1 - triVert0, triVert2 - triVert0));

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

        static bool IsOutsideEdge(int edgeIndex, NativeArray<Edge> edgesToProcess)
        {
            int previousIndex = edgeIndex - 1;
            int nextIndex = edgeIndex + 1;
            int numberOfEdgesToProcess = edgesToProcess.Length;
            Edge currentEdge = edgesToProcess[edgeIndex];

            return (previousIndex < 0 || (currentEdge.CompareTo(edgesToProcess[edgeIndex - 1]) != 0)) && (nextIndex >= numberOfEdgesToProcess || (currentEdge.CompareTo(edgesToProcess[edgeIndex + 1]) != 0));
        }


        static void CountEdges(List<ShadowGenerationInfo> meshesToProcess, NativeArray<Edge> edgesToProcess)
        {
            for(int i=0;i<edgesToProcess.Length;i++)
            {
                if (IsOutsideEdge(i, edgesToProcess))
                {
                    Edge currentEdge = edgesToProcess[i];
                    ShadowGenerationInfo info = meshesToProcess[currentEdge.objectIndex];
                    info.numberOfOutsideEdges++;
                    meshesToProcess[currentEdge.objectIndex] = info;
                }
            }
        }

        static void DebugDrawMesh(Mesh mesh, Color color, float time)
        {
            for(int i=0;i<mesh.triangles.Length;i+=3)
            {
                int idx0 = mesh.triangles[i];
                int idx1 = mesh.triangles[i + 1];
                int idx2 = mesh.triangles[i + 2];
                Debug.DrawLine(mesh.vertices[idx0], mesh.vertices[idx1], color, time);
                Debug.DrawLine(mesh.vertices[idx1], mesh.vertices[idx2], color, time);
                Debug.DrawLine(mesh.vertices[idx2], mesh.vertices[idx0], color, time);
            }
        }

        static void DebugDrawMesh(int triangleOffset, Mesh mesh, Color color, float time)
        {
            for (int i = triangleOffset; i < mesh.triangles.Length; i += 3)
            {
                int idx0 = mesh.triangles[i];
                int idx1 = mesh.triangles[i + 1];
                int idx2 = mesh.triangles[i + 2];
                Debug.DrawLine(mesh.vertices[idx0], mesh.vertices[idx1], color, time);
                Debug.DrawLine(mesh.vertices[idx1], mesh.vertices[idx2], color, time);
                Debug.DrawLine(mesh.vertices[idx2], mesh.vertices[idx0], color, time);
            }
        }

        static void CopyMeshToShadowMesh(ShadowGenerationInfo meshInfo)
        {
            int numberOfOutsideEdges = meshInfo.numberOfOutsideEdges;
            int[] triangles = new int[meshInfo.triangles.Length + 3 * numberOfOutsideEdges];
            Vector3[] vertices = new Vector3[meshInfo.vertices.Length + numberOfOutsideEdges];
            Vector4[] tangents = new Vector4[meshInfo.vertices.Length + numberOfOutsideEdges];

            for (int i = 0; i < meshInfo.vertices.Length; i++)
            {
                vertices[i] = meshInfo.vertices[i];
                tangents[i] = Vector3.zero;
            }

            for (int i = 0; i < meshInfo.triangles.Length; i++)
                triangles[i] = meshInfo.triangles[i];

            meshInfo.mesh.vertices = vertices;
            meshInfo.mesh.triangles = triangles;
            meshInfo.mesh.tangents = tangents;
            meshInfo.mesh.UploadMeshData(false);
        }

        static Vector3 GetTangentFromEdge(Vector3 vertex0, Vector3 vertex1, Vector3 normal, bool swapped)
        {
            Vector3 right;
            if (swapped)
                right = Vector3.Normalize(vertex0 - vertex1);
            else
                right = Vector3.Normalize(vertex1 - vertex0);

            return Vector3.Cross(normal, right);
        }

        static void CreateHardShadowMeshes(List<ShadowGenerationInfo> meshesToProcess, NativeArray<Edge> edgesToProcess, NativeArray<int> vertexStartIndices)
        {
            int totalMeshesToProcess = meshesToProcess.Count;
            int[] addVertIndices = new int[totalMeshesToProcess];
            int[] addTriIndices = new int[totalMeshesToProcess];

            // Copy the interior of the shadow mesh
            for (int meshIndex = 0; meshIndex < totalMeshesToProcess; meshIndex++)
            {
                addVertIndices[meshIndex] = meshesToProcess[meshIndex].vertices.Length;
                addTriIndices[meshIndex] = meshesToProcess[meshIndex].triangles.Length;

                CopyMeshToShadowMesh(meshesToProcess[meshIndex]);
                //DebugDrawMesh(meshesToProcess[meshIndex].mesh, Color.blue, 30f);
            }

            // Create the extra triangles needed
            int numberOfEdgesToProcess = edgesToProcess.Length;
            for (int i = 0; i < numberOfEdgesToProcess; i++)
            {
                if (IsOutsideEdge(i, edgesToProcess))
                {
                    Edge currentEdge = edgesToProcess[i];
                    int objectIndex = currentEdge.objectIndex;

                    ShadowGenerationInfo meshInfo = meshesToProcess[objectIndex];
                    Vector3 vertex0 = meshInfo.vertices[currentEdge.vertexIndex0];
                    Vector3 vertex1 = meshInfo.vertices[currentEdge.vertexIndex1];
                    //Vector3 tangent = GetTangentFromEdge(vertex0, vertex1, currentEdge.normal, currentEdge.swapped);
                    Vector3 tangent = GetTangentFromEdge(vertex0, vertex1, -Vector3.forward, currentEdge.swapped);

                    float tanLen = 0.2f;
                    Debug.DrawLine(vertex0, vertex0 + tanLen * tangent, Color.red, 30);
                    Debug.DrawLine(vertex1, vertex1 + tanLen * tangent, Color.red, 30);

                    // For each edge we need to add a vertex and triangle
                    int newVertIndex = addVertIndices[objectIndex]++;
                    int newTriIndex = addTriIndices[objectIndex];
                    addTriIndices[objectIndex] += 3;


                    // Add our triangle, vertex, and tangents
                    Mesh mesh = meshInfo.mesh;
                    Vector4 v4Tangent = new Vector4(tangent.x, tangent.y, tangent.z, 0);

                    Vector3[] vertices = mesh.vertices;
                    Vector4[] tangents = mesh.tangents;
                    int[] triangles = mesh.triangles;


                    tangents[newVertIndex] = v4Tangent;
                    triangles[newTriIndex + 1] = newVertIndex;

                    if (!currentEdge.swapped)
                    {
                        vertices[newVertIndex] = vertex0;
                        tangents[currentEdge.vertexIndex1] = v4Tangent;
                        triangles[newTriIndex] = currentEdge.vertexIndex0;
                        triangles[newTriIndex + 2] = currentEdge.vertexIndex1;
                    }
                    else
                    {
                        vertices[newVertIndex] = vertex1;
                        tangents[currentEdge.vertexIndex0] = v4Tangent;
                        triangles[newTriIndex] = currentEdge.vertexIndex1;
                        triangles[newTriIndex + 2] = currentEdge.vertexIndex0;
                        
                    }

                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.tangents = tangents;
                }
            }
        }



        internal static void ProcessMeshesForShadows(List<ShadowGenerationInfo> meshesToProcess)
        {
            int totalVertices;
            int totalEdges;

            NativeArray<int> vertexStartIndices = new NativeArray<int>(meshesToProcess.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory); ;
            PopulateVerticesPerObject(meshesToProcess, vertexStartIndices, out totalVertices, out totalEdges);

            NativeArray<Edge> edgesToProcess = new NativeArray<Edge>(totalEdges, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            PopulateEdgesToProcess(meshesToProcess, totalEdges, edgesToProcess);
            SortEdges(edgesToProcess); // Can be multithreaded later as an optimization
            CountEdges(meshesToProcess, edgesToProcess);
            CreateHardShadowMeshes(meshesToProcess, edgesToProcess, vertexStartIndices);

            //DebugDrawEdgesAndTangents(meshesToProcess, edgesToProcess);

            // Cleanup array allocations
            edgesToProcess.Dispose();
            vertexStartIndices.Dispose();

            meshesToProcess.Clear();
        }
    }
}
