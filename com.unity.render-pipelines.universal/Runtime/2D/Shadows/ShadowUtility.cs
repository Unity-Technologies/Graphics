using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    internal class ShadowUtility
    {
        const int k_AdditionalVerticesPerVertex = 2;
        const int k_VerticesPerTriangle = 3;
        //const int k_TrianglesPerEdge = 2;
        const int k_TrianglesPerEdge = 3;
        const int k_MinimumEdges = 3;

        static public int debugVert = 0;

        private class EdgeComparer : IEqualityComparer<ShadowEdge>
        {
            public bool Equals(ShadowEdge edge0, ShadowEdge edge1)
            {
                return (edge0.v0 == edge1.v0 && edge0.v1 == edge1.v1) || (edge0.v1 == edge1.v0 && edge0.v0 == edge1.v1);
            }

            public int GetHashCode(ShadowEdge edge)
            {
                int v0 = edge.v0;
                int v1 = edge.v1;

                if (edge.v1 < edge.v0)
                {
                    v0 = edge.v1;
                    v1 = edge.v0;
                }

                int hashCode = v0 << 15 | v1;
                return hashCode.GetHashCode();
            }
        }


        static private Dictionary<ShadowEdge, int> m_EdgeDictionary = new Dictionary<ShadowEdge, int>(new EdgeComparer());  // This is done so we don't create garbage allocating and deallocating a dictionary object


        [StructLayout(LayoutKind.Sequential)]
        struct ShadowMeshVertex
        {
            public Vector3 position;
            public Vector4 tangent;

            public ShadowMeshVertex(Vector3 inPosition, Vector4 inTangent)
            {
                position = inPosition;
                tangent = inTangent;
            }
        }


        static VertexAttributeDescriptor[] m_VertexLayout = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent,  VertexAttributeFormat.Float32, 4)
        };


        static void CalculateTangents(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<Vector3> contractionDirection, ref NativeArray<Vector4> outTangents)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector3 start = inVertices[v0];
                Vector3 end = inVertices[v1];

                Vector3 contractDir0 = contractionDirection[v0];
                Vector3 contractDir1 = contractionDirection[v1];

                outTangents[v0] = new Vector4(contractDir0.x, contractDir0.y, end.x, end.y);

                int additionalVerticesStart = 2 * i + inVertices.Length;
                outTangents[additionalVerticesStart] = new Vector4(contractDir0.x, contractDir0.y, end.x, end.y);
                outTangents[additionalVerticesStart + 1] = new Vector4(contractDir1.x, contractDir1.y, start.x, start.y);
            }
        }

        static void CalculateContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingIndices, NativeArray<bool> inShapeIsClosedArray, bool allowContraction, ref NativeArray<Vector3> outContractionDirection)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingIndices.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingIndices[shapeIndex];

                if (startingIndex < 0)
                    return;

                int nextStartingIndex = -1;
                if (shapeIndex + 1 < inShapeStartingIndices.Length)
                    nextStartingIndex = inShapeStartingIndices[shapeIndex + 1];

                int numEdgesInCurShape = nextStartingIndex >= 0 ? nextStartingIndex - startingIndex : inEdges.Length - startingIndex;

                int edgeIndex = startingIndex;
                for (int i = 0; i < numEdgesInCurShape; i++)
                {
                    if (inShapeIsClosedArray[shapeIndex] && allowContraction)
                    {
                        int curEdge = startingIndex + i;
                        int nextEdge = i + 1 < numEdgesInCurShape ? startingIndex + i + 1 : startingIndex;

                        int v0 = inEdges[curEdge].v0;
                        int v1 = inEdges[curEdge].v1;
                        int v2 = inEdges[nextEdge].v1;

                        Vector3 pt0 = inVertices[v0];
                        Vector3 pt1 = inVertices[v1];
                        Vector3 pt2 = inVertices[v2];

                        Vector3 tangent0 = Vector3.Cross(Vector3.Normalize(pt1 - pt0), -Vector3.forward).normalized;
                        Vector3 tangent1 = Vector3.Cross(Vector3.Normalize(pt2 - pt1), -Vector3.forward).normalized;

                        outContractionDirection[v1] = (-0.5f * (tangent0 + tangent1)).normalized;
                    }
                    else
                    {
                        // Open shapes should not have contraction direction.
                        int curEdge = startingIndex + i;
                        int v1 = inEdges[curEdge].v1;
                        outContractionDirection[v1] = Vector3.zero;
                    }
                }
            }
        }

        static void ApplyContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<Vector3> inContractionDirection, float inContractionDistance, ref NativeArray<Vector3> outVertices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int index = inEdges[i].v0;
                outVertices[i] = inVertices[index];
            }
        }

        static void CalculateVertices(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<Vector4> inTangents, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            for(int i=0; i < inVertices.Length; i++)
            {
                Vector4 pt0 = inVertices[i];
                pt0.z = 1;
                ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(pt0, inTangents[i]);
                outMeshVertices[i] = originalShadowMesh;
            }

            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector4 pt0 = inVertices[v0];
                Vector4 pt1 = inVertices[v1];

                pt0.z = 0;
                pt1.z = 0;
                int additionalVerticesStart = 2 * i + inVertices.Length;
                ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(pt0, inTangents[additionalVerticesStart]);
                ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(pt1, inTangents[additionalVerticesStart + 1]);

                outMeshVertices[additionalVerticesStart] = additionalVertex0;
                outMeshVertices[additionalVerticesStart + 1] = additionalVertex1;
            }
        }

        static void CalculateTriangles(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingIndices, NativeArray<bool> inShapeIsClosedArray, ref NativeArray<int> outMeshIndices)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingIndices.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingIndices[shapeIndex];
                if (startingIndex < 0)
                    return;

                int endIndex = inEdges.Length;
                if ((shapeIndex + 1) < inShapeStartingIndices.Length && inShapeStartingIndices[shapeIndex + 1] > -1)
                    endIndex = inShapeStartingIndices[shapeIndex + 1];

                int prevEdge = endIndex - 1;
                for (int i = startingIndex; i < endIndex; i++)
                {
                    int v0 = inEdges[i].v0;
                    int v1 = inEdges[i].v1;

                    int additionalVerticesStart = k_AdditionalVerticesPerVertex * i + inVertices.Length;

                    int startingMeshIndex = k_VerticesPerTriangle * k_TrianglesPerEdge * i;
                    // Add a degenerate rectangle 
                    outMeshIndices[startingMeshIndex] = (ushort)v0;
                    outMeshIndices[startingMeshIndex + 1] = (ushort)additionalVerticesStart;
                    outMeshIndices[startingMeshIndex + 2] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[startingMeshIndex + 3] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[startingMeshIndex + 4] = (ushort)v1;
                    outMeshIndices[startingMeshIndex + 5] = (ushort)v0;

                    if (inShapeIsClosedArray[shapeIndex] || i > startingIndex)
                    {
                        //// Add a triangle to connect that rectangle to the neighboring rectangle so we have a seamless mesh
                        int prevAdditionalVertex = k_AdditionalVerticesPerVertex * prevEdge + inVertices.Length;
                        outMeshIndices[startingMeshIndex + 6] = (ushort)(additionalVerticesStart);
                        outMeshIndices[startingMeshIndex + 7] = (ushort)v0;
                        outMeshIndices[startingMeshIndex + 8] = (ushort)prevAdditionalVertex;
                    }

                    prevEdge = i;
                }
            }
        }

        static BoundingSphere CalculateBoundingSphere(NativeArray<Vector3> inVertices)
        {
            Debug.AssertFormat(inVertices.Length > 0, "At least one vertex is required to calculate a bounding sphere");

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // Add outline vertices
            for (int i = 0; i < inVertices.Length; i++)
            {
                Vector3 vertex = inVertices[i];

                if (minX > vertex.x)
                    minX = vertex.x;
                if (maxX < vertex.x)
                    maxX = vertex.x;

                if (minY > vertex.y)
                    minY = vertex.y;
                if (maxY < vertex.y)
                    maxY = vertex.y;
            }

            // Calculate bounding sphere (circle)
            Vector3 origin = new Vector2(0.5f * (minX + maxX), 0.5f * (minY + maxY));
            float deltaX = maxX - minX;
            float deltaY = maxY - minY;
            float radius = 0.5f * Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

            return new BoundingSphere(origin, radius);
        }



        // inEdges is expected to be contiguous
        static public BoundingSphere GenerateShadowMesh(Mesh mesh, NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingIndices, NativeArray<bool> inShapeIsClosedArray, bool allowContraction, IShadowShape2DProvider.OutlineTopology topology)
        {
            // Setup our buffers
            int meshVertexCount = inVertices.Length + 2 * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)
            
            NativeArray<Vector4> meshTangents = new NativeArray<Vector4>(meshVertexCount, Allocator.Temp);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Temp);
            NativeArray<ShadowMeshVertex> meshFinalVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Temp);
            NativeArray<Vector3> contractionDirection = new NativeArray<Vector3>(inVertices.Length, Allocator.Temp);

            CalculateContraction(inVertices, inEdges, inShapeStartingIndices, inShapeIsClosedArray, allowContraction, ref contractionDirection);
            CalculateTangents(inVertices, inEdges, contractionDirection, ref meshTangents);                            // meshVertices contain a normal component
            CalculateVertices(inVertices, inEdges, meshTangents, ref meshFinalVertices);
            CalculateTriangles(inVertices, inEdges, inShapeStartingIndices, inShapeIsClosedArray, ref meshIndices);

            // Set the mesh data
            mesh.SetVertexBufferParams(meshVertexCount, m_VertexLayout);
            mesh.SetVertexBufferData<ShadowMeshVertex>(meshFinalVertices, 0, 0, meshVertexCount);
            mesh.SetIndexBufferParams(meshIndexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData<int>(meshIndices, 0, 0, meshIndexCount);

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, meshIndexCount));
            
            meshTangents.Dispose();
            meshIndices.Dispose();
            meshFinalVertices.Dispose();
            contractionDirection.Dispose();

            BoundingSphere retBoundingSphere = CalculateBoundingSphere(inVertices);
            return retBoundingSphere;
        }


        static public void UpdateShadowMeshVertices(Mesh mesh, NativeArray<Vector3> inVertices)
        {
            int vertexCount = inVertices.Length;
            mesh.SetVertexBufferParams(vertexCount, m_VertexLayout);

            NativeArray<ShadowMeshVertex> meshVertices = new NativeArray<ShadowMeshVertex>(vertexCount, Allocator.Temp);
            unsafe
            {
                ShadowMeshVertex* vertexPtr = (ShadowMeshVertex *)mesh.GetNativeVertexBufferPtr(0).ToPointer();
                for(int i=0;i<vertexCount;i++)
                {
                    ShadowMeshVertex curVertex = vertexPtr[i];
                    curVertex.position = inVertices[i];
                    meshVertices[i] = curVertex;
                }
            }

            mesh.SetVertexBufferData<ShadowMeshVertex>(meshVertices, 0, 0, vertexCount);

            meshVertices.Dispose();
        }

        static public int GetFirstUnusedIndex(NativeArray<bool> usedValues)
        {
            for (int i = 0; i < usedValues.Length; i++)
            {
                if (!usedValues[i])
                    return i;
            }

            return -1;
        }

        static public void SortEdges(NativeArray<ShadowEdge> unsortedEdges, out NativeArray<ShadowEdge> sortedEdges, out NativeArray<int> shapeStartingIndices)
        {
            sortedEdges = new NativeArray<ShadowEdge>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            shapeStartingIndices = new NativeArray<int>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            NativeArray<int> edgeMap = new NativeArray<int>(unsortedEdges.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<bool> usedEdges = new NativeArray<bool>(unsortedEdges.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                edgeMap[unsortedEdges[i].v0] = i;
                usedEdges[i] = false;
                shapeStartingIndices[i] = -1;
            }

            int currentShape = 0;
            bool findStartingEdge = true;
            int edgeIndex = -1;
            int startingEdge = 0;
            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                if (findStartingEdge)
                {
                    edgeIndex = GetFirstUnusedIndex(usedEdges);
                    startingEdge = edgeIndex;
                    shapeStartingIndices[currentShape++] = edgeIndex;
                    findStartingEdge = false;
                }

                usedEdges[edgeIndex] = true;
                sortedEdges[i] = unsortedEdges[edgeIndex];
                int nextVertex = unsortedEdges[edgeIndex].v1;
                edgeIndex = edgeMap[nextVertex];

                if (edgeIndex == startingEdge)
                    findStartingEdge = true;
            }

            usedEdges.Dispose();
            edgeMap.Dispose();
        }

        static public void InitializeShapeIsClosedArray(NativeArray<int> inShapeStartingIndices, out NativeArray<bool> outShapeIsClosedArray)
        {
            outShapeIsClosedArray = new NativeArray<bool>(inShapeStartingIndices.Length, Allocator.Temp);
            for(int i=0;i< outShapeIsClosedArray.Length;i++)
                outShapeIsClosedArray[i] = true;
        }



        static public void CalculateEdgesFromLines(NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingIndices, out NativeArray<bool> outShapeIsClosedArray)
        {
            int numOfEdges = indices.Length >> 1;
            NativeArray<int> tempShapeStartIndices = new NativeArray<int>(numOfEdges, Allocator.Temp);
            NativeArray<bool> tempShapeIsClosedArray = new NativeArray<bool>(numOfEdges, Allocator.Temp);

            // Find the shape starting indices and allow contraction
            int shapeCount = 0;
            int shapeStart = indices[0];
            int lastIndex = indices[0];
            bool closedShapeFound = false;
            tempShapeStartIndices[0] = 0;

            for (int i=0;i<indices.Length;i+=2)
            {
                if(closedShapeFound)
                {
                    shapeStart = indices[i];
                    tempShapeIsClosedArray[shapeCount] = true;
                    tempShapeStartIndices[++shapeCount] = i >> 1;
                    closedShapeFound = false;
                }

                // If the last shape was not closed ie (A,B),(C,D)
                if (indices[i] != lastIndex)
                {
                    tempShapeIsClosedArray[shapeCount] = false;
                    tempShapeStartIndices[++shapeCount] = i >> 1;
                    shapeStart = indices[i];
                }

                if(shapeStart == indices[i+1])
                    closedShapeFound = true;

                lastIndex = indices[i + 1];
            }

            tempShapeIsClosedArray[shapeCount++] = closedShapeFound; 

            // Copy the our data to a smaller array
            outShapeStartingIndices = new NativeArray<int>(shapeCount, Allocator.Temp);
            outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Temp);
            for (int i = 0; i < shapeCount; i++)
            {
                outShapeStartingIndices[i] = tempShapeStartIndices[i];
                outShapeIsClosedArray[i] = tempShapeIsClosedArray[i];
            }
            tempShapeStartIndices.Dispose();
            tempShapeIsClosedArray.Dispose();

            // Add edges
            outEdges = new NativeArray<ShadowEdge>(numOfEdges, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfEdges; i++)
            {
                int indicesIndex = i << 1;
                int v0Index = indices[indicesIndex];
                int v1Index = indices[indicesIndex + 1];

                outEdges[i] = new ShadowEdge(v0Index, v1Index);
            }
        }

        static public void CalculateEdgesFromLineStrip(NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingIndices, out NativeArray<bool> outShapeIsClosedArray)
        {
            int numOfIndices = indices.Length;
            int lastIndex = indices[numOfIndices - 1];

            NativeArray<ShadowEdge> unsortedEdges = new NativeArray<ShadowEdge>(numOfIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i++)
            {
                int curIndex = indices[i];
                unsortedEdges[i] = new ShadowEdge(lastIndex, curIndex); ;
                lastIndex = curIndex;
            }

            SortEdges(unsortedEdges, out outEdges, out outShapeStartingIndices);
            unsortedEdges.Dispose();

            InitializeShapeIsClosedArray(outShapeStartingIndices, out outShapeIsClosedArray);
        }


        static public void CalculateEdgesForSimpleLineStrip(int numOfIndices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingIndices, out NativeArray<bool> outShapeIsClosedArray)
        {
            outShapeStartingIndices = new NativeArray<int>(1, Allocator.Persistent);
            outShapeStartingIndices[0] = 0;

            int lastIndex = numOfIndices - 1;
            outEdges = new NativeArray<ShadowEdge>(numOfIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i++)
            {
                outEdges[i] = new ShadowEdge(lastIndex, i); ;
                lastIndex = i;
            }

            InitializeShapeIsClosedArray(outShapeStartingIndices, out outShapeIsClosedArray);
        }

        static public int CreateEdgeDictionaryFromTriangles(NativeArray<int> indices)
        {
            m_EdgeDictionary.Clear();
            for (int i = 0; i < indices.Length; i += 3)
            {
                int v0Index = indices[i];
                int v1Index = indices[i + 1];
                int v2Index = indices[i + 2];

                ShadowEdge edge0 = new ShadowEdge(v0Index, v1Index);
                ShadowEdge edge1 = new ShadowEdge(v1Index, v2Index);
                ShadowEdge edge2 = new ShadowEdge(v2Index, v0Index);

                // When a key comparison is made edges (A, B) and (B, A) are equal (see EdgeComparer)
                if (m_EdgeDictionary.ContainsKey(edge0))
                    m_EdgeDictionary[edge0] = m_EdgeDictionary[edge0] + 1;
                else
                    m_EdgeDictionary.Add(edge0, 1);

                if (m_EdgeDictionary.ContainsKey(edge1))
                    m_EdgeDictionary[edge1] = m_EdgeDictionary[edge1] + 1;
                else
                    m_EdgeDictionary.Add(edge1, 1);

                if (m_EdgeDictionary.ContainsKey(edge2))
                    m_EdgeDictionary[edge2] = m_EdgeDictionary[edge2] + 1;
                else
                    m_EdgeDictionary.Add(edge2, 1);
            }


            // Count the outside edges
            int outsideEdges = 0;
            foreach (KeyValuePair<ShadowEdge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                    outsideEdges++;
            }

            return outsideEdges;
        }

        static public void RemapEdges(NativeArray<ShadowEdge> edges, NativeArray<int> map)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                ShadowEdge edge = edges[i];
                edge.v0 = map[edge.v0];
                edge.v1 = map[edge.v1];
                edges[i] = edge;
            }
        }

        static public void CalculateEdgesFromTriangles(NativeArray<Vector3> vertices, NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingIndices, out NativeArray<bool> outShapeIsClosedArray)
        {
            // Add our edges to an edge list
            int outsideEdges = CreateEdgeDictionaryFromTriangles(indices);

            // Create unsorted edges array
            int edgeCount = 0;
            NativeArray<ShadowEdge> unsortedEdges = new NativeArray<ShadowEdge>(outsideEdges, Allocator.Temp);
            foreach (KeyValuePair<ShadowEdge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                {
                    unsortedEdges[edgeCount++] = keyValuePair.Key;
                }
            }

            // Create vertex remapping arrays
            NativeArray<int> originalToSubsetMap = new NativeArray<int>(vertices.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> subsetToOriginalMap = new NativeArray<int>(vertices.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < vertices.Length; i++)
                originalToSubsetMap[i] = -1;

            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                originalToSubsetMap[unsortedEdges[i].v0] = i;
                subsetToOriginalMap[i] = unsortedEdges[i].v0;
            }

            // Sort edges so they are in order
            RemapEdges(unsortedEdges, originalToSubsetMap);
            SortEdges(unsortedEdges, out outEdges, out outShapeStartingIndices);
            RemapEdges(outEdges, subsetToOriginalMap);

            // Cleanup
            originalToSubsetMap.Dispose();
            subsetToOriginalMap.Dispose();
            unsortedEdges.Dispose();

            InitializeShapeIsClosedArray(outShapeStartingIndices, out outShapeIsClosedArray);
        }


        static public void FixWindingOrder(NativeArray<Vector3> inVertices, NativeArray<int> inShapeStartingIndices, NativeArray<ShadowEdge> inOutSortedEdges)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingIndices.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingIndices[shapeIndex];
                if (startingIndex < 0)
                    return;

                int endIndex = inShapeStartingIndices.Length;
                if ((shapeIndex + 1) < inShapeStartingIndices.Length && inShapeStartingIndices[shapeIndex + 1] > -1)
                    endIndex = inShapeStartingIndices[shapeIndex + 1];

                // Determine if the shape needs to have its winding order fixed..
                float sum0 = 0;
                float sum1 = 0;
                for (int i = startingIndex; i < endIndex; i++)
                {
                    int v0 = inOutSortedEdges[i].v0;
                    int v1 = inOutSortedEdges[i].v1;

                    sum0 += (inVertices[v0].x * inVertices[v1].y);
                    sum1 += (inVertices[v0].y * inVertices[v1].x);
                }

                // If the edges are backward, reverse them...
                float twoTimesSignedArea = sum1 - sum0;
                if (twoTimesSignedArea < 0)
                {
                    int count = (endIndex - startingIndex);
                    for (int i = 0; i < (count >> 1); i++)
                    {
                        int edgeAIndex = startingIndex + i;
                        int edgeBIndex = startingIndex + count - 1 - i;

                        ShadowEdge edgeA = inOutSortedEdges[edgeAIndex];
                        ShadowEdge edgeB = inOutSortedEdges[edgeBIndex];
                        edgeA.Reverse();
                        edgeB.Reverse();

                        inOutSortedEdges[edgeAIndex] = edgeB;
                        inOutSortedEdges[edgeBIndex] = edgeA;
                    }
                }
            }
        }
    }
}
