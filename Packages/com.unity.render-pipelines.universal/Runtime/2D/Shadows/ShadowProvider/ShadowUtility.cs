using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;
using Unity.Mathematics;
using UnityEngine.Rendering.Universal.UTess;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    internal class ShadowUtility
    {
        internal const int k_AdditionalVerticesPerEdge = 4;
        internal const int k_VerticesPerTriangle = 3;
        internal const int k_TrianglesPerEdge = 3;
        internal const int k_MinimumEdges = 3;
        internal const int k_SafeSize = 40;

        public enum ProjectionType
        {
            ProjectionNone      = -1,
            ProjectionHard      =  0,
            ProjectionSoftLeft  =  1,
            ProjectionSoftRight =  3,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ShadowMeshVertex
        {
            internal Vector3 position;   // stores: xy: position           z: projection type    w: soft shadow value (0 is fully shadowed)
            internal Vector4 tangent;    // stores: xy: contraction dir    zw: other edge position

            internal ShadowMeshVertex(ProjectionType inProjectionType, Vector2 inEdgePosition0, Vector2 inEdgePosition1)
            {
                position = new Vector3(inEdgePosition0.x, inEdgePosition0.y, (int)inProjectionType);
                tangent  = new Vector4(0, 0, inEdgePosition1.x, inEdgePosition1.y);
            }

            internal ShadowMeshVertex(ProjectionType inProjectionType, Vector2 inEdgePosition0, ProjectionInfo info)
            {
                position = new Vector4(inEdgePosition0.x, inEdgePosition0.y, (int)inProjectionType);
                tangent  = new Vector4(0, 0, info.edgeOtherPoint.x, info.edgeOtherPoint.y);
            }
        }

        static VertexAttributeDescriptor[] m_VertexLayout = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position,   VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent,    VertexAttributeFormat.Float32, 4),
        };

        internal struct ProjectionInfo
        {
            internal Vector2 edgeOtherPoint;

            internal ProjectionInfo(Vector2 inEdgeOtherPoint)
            {
                edgeOtherPoint = inEdgeOtherPoint;
            }

            internal ProjectionInfo(float inEdgeOtherPointX, float inEdgeOtherPointY)
            {
                edgeOtherPoint = new Vector2(inEdgeOtherPointX, inEdgeOtherPointY);
            }
        }

        static int GetNextShapeStart(int currentShape, NativeArray<int> inShapeStartingEdge, int maxValue)
        {
            // Make sure we are in the bounds of the shapes we have. Also make sure our starting edge isn't negative
            return ((currentShape + 1 < inShapeStartingEdge.Length) && (inShapeStartingEdge[currentShape + 1] >= 0)) ? inShapeStartingEdge[currentShape + 1] : maxValue;
        }


        static Vector2 CalculateTangent(Vector2 start, Vector2 end)
        {
            Vector3 direction = end - start;
            return Vector3.Cross(direction, Vector3.forward);
        }


        static internal void CalculateProjectionInfo(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, ref NativeArray<ProjectionInfo> outProjectionInfo)
        {
            int currentShape = 0;
            int shapeStart = 0;
            int nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdge, inEdges.Length);
            int shapeSize = nextShapeStart;
            for (int i = 0; i < inEdges.Length; i++)
            {
                if (i == nextShapeStart)
                {
                    currentShape++;
                    shapeStart = nextShapeStart;
                    nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdge, inEdges.Length);
                    shapeSize = nextShapeStart - shapeStart;
                }

                int nextEdgeIndex = (i - shapeStart + 1) % shapeSize + shapeStart;
                int prevEdgeIndex = (i - shapeStart + shapeSize - 1) % shapeSize + shapeStart;  


                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                int prev1 = inEdges[prevEdgeIndex].v0;
                int next0 = inEdges[nextEdgeIndex].v1;

                Vector2 startPt = inVertices[v0];
                Vector2 endPt = inVertices[v1];
                Vector2 prevPt = inVertices[prev1];
                Vector2 nextPt = inVertices[next0];


                // Original Vertex
                outProjectionInfo[v0] = new ProjectionInfo(endPt);

                // Hard Shadows
                int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVertices.Length;
                outProjectionInfo[additionalVerticesStart]     = new ProjectionInfo(endPt);
                outProjectionInfo[additionalVerticesStart + 1] = new ProjectionInfo(startPt);

                // Soft Triangles
                outProjectionInfo[additionalVerticesStart + 2] = new ProjectionInfo(endPt);
                outProjectionInfo[additionalVerticesStart + 3] = new ProjectionInfo(endPt);
            }
        }

        static internal void CalculateContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, bool allowContraction, ref NativeArray<Vector2> outContractionDirection)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingEdge.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingEdge[shapeIndex];

                if (startingIndex < 0)
                    return;

                int nextStartingIndex = -1;
                if (shapeIndex + 1 < inShapeStartingEdge.Length)
                    nextStartingIndex = inShapeStartingEdge[shapeIndex + 1];

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
                        outContractionDirection[v1] = Vector2.zero;
                    }
                }
            }
        }

        static internal void ApplyContraction(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<Vector3> inContractionDirection, float inContractionDistance, ref NativeArray<Vector3> outVertices)
        {
            for (int i = 0; i < inEdges.Length; i++)
            {
                int index = inEdges[i].v0;
                outVertices[i] = inVertices[index];
            }
        }

        static internal void CalculateVertices(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<ProjectionInfo> inProjectionInfo, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            for (int i = 0; i < inVertices.Length; i++)
            {
                Vector3 pt0 = inVertices[i];
                ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(ProjectionType.ProjectionNone, pt0, inProjectionInfo[i].edgeOtherPoint);
                outMeshVertices[i] = originalShadowMesh;
            }

            for (int i = 0; i < inEdges.Length; i++)
            {
                int v0 = inEdges[i].v0;
                int v1 = inEdges[i].v1;

                Vector2 pt0 = inVertices[v0];
                Vector2 pt1 = inVertices[v1];

                int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVertices.Length;
                ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(ProjectionType.ProjectionHard,      pt0, inProjectionInfo[additionalVerticesStart]);
                ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(ProjectionType.ProjectionHard,      pt1, inProjectionInfo[additionalVerticesStart + 1]);
                ShadowMeshVertex additionalVertex2 = new ShadowMeshVertex(ProjectionType.ProjectionSoftLeft,  pt0, inProjectionInfo[additionalVerticesStart + 2]);
                ShadowMeshVertex additionalVertex3 = new ShadowMeshVertex(ProjectionType.ProjectionSoftRight, pt0, inProjectionInfo[additionalVerticesStart + 3]);

                outMeshVertices[additionalVerticesStart]     = additionalVertex0;
                outMeshVertices[additionalVerticesStart + 1] = additionalVertex1;
                outMeshVertices[additionalVerticesStart + 2] = additionalVertex2;
                outMeshVertices[additionalVerticesStart + 3] = additionalVertex3;
            }
        }

        static internal void CalculateTriangles(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, ref NativeArray<int> outMeshIndices)
        {
            int meshIndex = 0;
            for (int shapeIndex = 0; shapeIndex < inShapeStartingEdge.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingEdge[shapeIndex];
                if (startingIndex < 0)
                    return;

                int endIndex = inEdges.Length;
                if ((shapeIndex + 1) < inShapeStartingEdge.Length && inShapeStartingEdge[shapeIndex + 1] > -1)
                    endIndex = inShapeStartingEdge[shapeIndex + 1];

                //// Hard Shadow Geometry
                int prevEdge = endIndex - 1;
                for (int i = startingIndex; i < endIndex; i++)
                {
                    int v0 = inEdges[i].v0;
                    int v1 = inEdges[i].v1;

                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVertices.Length;

                    // Add a degenerate rectangle
                    outMeshIndices[meshIndex++] = (ushort)v0;
                    outMeshIndices[meshIndex++] = (ushort)additionalVerticesStart;
                    outMeshIndices[meshIndex++] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[meshIndex++] = (ushort)(additionalVerticesStart + 1);
                    outMeshIndices[meshIndex++] = (ushort)v1;
                    outMeshIndices[meshIndex++] = (ushort)v0;

                    //if (inShapeIsClosedArray[shapeIndex] || i > startingIndex)
                    //{
                    //    //// Add a triangle to connect that rectangle to the neighboring rectangle so we have a seamless mesh
                    //    int prevAdditionalVertex = k_AdditionalVerticesPerEdge * prevEdge + inVertices.Length;
                    //    outMeshIndices[meshIndex++] = (ushort)(additionalVerticesStart);
                    //    outMeshIndices[meshIndex++] = (ushort)v0;
                    //    outMeshIndices[meshIndex++] = (ushort)prevAdditionalVertex;
                    //}

                    prevEdge = i;
                }

                // Soft Shadow Geometry
                for (int i = startingIndex; i < endIndex; i++)
                //int i = 0;
                {
                    int v0 = inEdges[i].v0;
                    int v1 = inEdges[i].v1;

                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVertices.Length;

                    // We also need 1 more triangles for soft shadows (3 indices)
                    outMeshIndices[meshIndex++] = (ushort)v0 ;
                    outMeshIndices[meshIndex++] = (ushort)additionalVerticesStart + 2;
                    outMeshIndices[meshIndex++] = (ushort)additionalVerticesStart + 3;
                }
            }
        }

        static internal BoundingSphere CalculateBoundingSphere(NativeArray<Vector3> inVertices)
        {
            if (inVertices.Length <= 0)
                return new BoundingSphere(Vector3.zero, 0);

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

        static void GenerateInteriorMesh(NativeArray<ShadowMeshVertex> inVertices, NativeArray<int> inIndices, NativeArray<ShadowEdge> inEdges, out NativeArray<ShadowMeshVertex> outVertices, out NativeArray<int> outIndices, out int outStartIndex, out int outIndexCount)
        {
            int inEdgeCount = inEdges.Length;

            // Do tessellation
            NativeArray<int2> tessInEdges = new NativeArray<int2>(inEdgeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<float2> tessInVertices = new NativeArray<float2>(inEdgeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < inEdgeCount; i++)
            {
                int2 edge = new int2(inEdges[i].v0, inEdges[i].v1);
                tessInEdges[i] = edge;

                int index = edge.x;
                tessInVertices[index] = new float2(inVertices[index].position.x, inVertices[index].position.y);
            }

            NativeArray<int> tessOutIndices = new NativeArray<int>(tessInVertices.Length * 8, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<float2> tessOutVertices = new NativeArray<float2>(tessInVertices.Length * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<int2> tessOutEdges = new NativeArray<int2>(tessInEdges.Length * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            int tessOutVertexCount = 0;
            int tessOutIndexCount = 0;
            int tessOutEdgeCount = 0;

            UTess.ModuleHandle.Tessellate(Allocator.Persistent, tessInVertices, tessInEdges, ref tessOutVertices, ref tessOutVertexCount, ref tessOutIndices, ref tessOutIndexCount, ref tessOutEdges, ref tessOutEdgeCount);

            int indexOffset = inIndices.Length;
            int vertexOffset = inVertices.Length;
            int totalOutVertices = tessOutVertexCount + inVertices.Length;
            int totalOutIndices = tessOutIndexCount + inIndices.Length;
            outVertices = new NativeArray<ShadowMeshVertex>(totalOutVertices, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            outIndices = new NativeArray<int>(totalOutIndices, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // Copy vertices
            for (int i = 0; i < inVertices.Length; i++)
                outVertices[i] = inVertices[i];

            for (int i = 0; i < tessOutVertexCount; i++)
            {
                float2 tessVertex = tessOutVertices[i];
                ShadowMeshVertex vertex = new ShadowMeshVertex(ProjectionType.ProjectionNone, tessVertex, Vector2.zero);
                outVertices[i + vertexOffset] = vertex;
            }

            // Copy indices
            for (int i = 0; i < inIndices.Length; i++)
                outIndices[i] = inIndices[i];

            // Copy and remap indices
            for (int i = 0; i < tessOutIndexCount; i++)
            {
                outIndices[i + indexOffset] = tessOutIndices[i] + vertexOffset;
            }

            outStartIndex = indexOffset;
            outIndexCount = tessOutIndexCount;
        }

        //inEdges is expected to be contiguous
        static public BoundingSphere GenerateShadowMesh(Mesh mesh, NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, bool allowContraction, bool fill, ShadowShape2D.OutlineTopology topology)
        {
            // Setup our buffers
            int meshVertexCount = inVertices.Length + k_AdditionalVerticesPerEdge * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)

            NativeArray<ProjectionInfo> meshProjectionInfo = new NativeArray<ProjectionInfo>(meshVertexCount, Allocator.Persistent);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Persistent);
            NativeArray<ShadowMeshVertex> meshVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Persistent);
            
            CalculateProjectionInfo(inVertices, inEdges, inShapeStartingEdge, inShapeIsClosedArray, ref meshProjectionInfo);
            CalculateVertices(inVertices, inEdges, meshProjectionInfo, ref meshVertices);
            CalculateTriangles(inVertices, inEdges, inShapeStartingEdge, inShapeIsClosedArray, ref meshIndices);

            NativeArray<ShadowMeshVertex> finalVertices;
            NativeArray<int> finalIndices;
            int fillSubmeshStartIndex = 0;
            int fillSubmeshIndexCount = 0;

            if (fill) // This has limited utility at the moment as contraction is not calculated. More work will need to be done to generalize this
            {
                GenerateInteriorMesh(meshVertices, meshIndices, inEdges, out finalVertices, out finalIndices, out fillSubmeshStartIndex, out fillSubmeshIndexCount);
                meshVertices.Dispose();
                meshIndices.Dispose();
            }
            else
            {
                finalVertices = meshVertices;
                finalIndices = meshIndices;
            }

            // Set the mesh data
            mesh.SetVertexBufferParams(finalVertices.Length, m_VertexLayout);
            mesh.SetVertexBufferData<ShadowMeshVertex>(finalVertices, 0, 0, finalVertices.Length);
            mesh.SetIndexBufferParams(finalIndices.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData<int>(finalIndices, 0, 0, finalIndices.Length);

            mesh.SetSubMesh(0, new SubMeshDescriptor(0, finalIndices.Length));
            mesh.subMeshCount = 1;

            meshProjectionInfo.Dispose();
            finalVertices.Dispose();
            finalIndices.Dispose();

            BoundingSphere retBoundingSphere = CalculateBoundingSphere(inVertices);
            return retBoundingSphere;
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

        static public void SortEdges(NativeArray<ShadowEdge> unsortedEdges, out NativeArray<ShadowEdge> sortedEdges, out NativeArray<int> shapeStartingEdge)
        {
            sortedEdges = new NativeArray<ShadowEdge>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            shapeStartingEdge = new NativeArray<int>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            NativeArray<int> edgeMap = new NativeArray<int>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<bool> usedEdges = new NativeArray<bool>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                edgeMap[unsortedEdges[i].v0] = i;
                usedEdges[i] = false;
                shapeStartingEdge[i] = -1;
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
                    shapeStartingEdge[currentShape++] = i;
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

        static public void InitializeShapeIsClosedArray(NativeArray<int> inShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            outShapeIsClosedArray = new NativeArray<bool>(inShapeStartingEdge.Length, Allocator.Persistent);
            for (int i = 0; i < outShapeIsClosedArray.Length; i++)
                outShapeIsClosedArray[i] = true;
        }

        static public void CalculateEdgesFromLines(NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            int numOfEdges = indices.Length >> 1;
            NativeArray<int> tempShapeStartIndices = new NativeArray<int>(numOfEdges, Allocator.Persistent);
            NativeArray<bool> tempShapeIsClosedArray = new NativeArray<bool>(numOfEdges, Allocator.Persistent);

            // Find the shape starting indices and allow contraction
            int shapeCount = 0;
            int shapeStart = indices[0];
            int lastIndex = indices[0];
            bool closedShapeFound = false;
            tempShapeStartIndices[0] = 0;

            for (int i = 0; i < indices.Length; i += 2)
            {
                if (closedShapeFound)
                {
                    shapeStart = indices[i];
                    tempShapeIsClosedArray[shapeCount] = true;
                    tempShapeStartIndices[++shapeCount] = i >> 1;
                    closedShapeFound = false;
                }
                else if (indices[i] != lastIndex)
                {
                    tempShapeIsClosedArray[shapeCount] = false;
                    tempShapeStartIndices[++shapeCount] = i >> 1;
                    shapeStart = indices[i];
                }

                if (shapeStart == indices[i + 1])
                    closedShapeFound = true;

                lastIndex = indices[i + 1];
            }

            tempShapeIsClosedArray[shapeCount++] = closedShapeFound;

            // Copy the our data to a smaller array
            outShapeStartingEdge = new NativeArray<int>(shapeCount, Allocator.Persistent);
            outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Persistent);
            for (int i = 0; i < shapeCount; i++)
            {
                outShapeStartingEdge[i] = tempShapeStartIndices[i];
                outShapeIsClosedArray[i] = tempShapeIsClosedArray[i];
            }
            tempShapeStartIndices.Dispose();
            tempShapeIsClosedArray.Dispose();

            // Add edges
            outEdges = new NativeArray<ShadowEdge>(numOfEdges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfEdges; i++)
            {
                int indicesIndex = i << 1;
                int v0Index = indices[indicesIndex];
                int v1Index = indices[indicesIndex + 1];

                outEdges[i] = new ShadowEdge(v0Index, v1Index);
            }
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

        static public void CalculateEdgesFromTriangles(NativeArray<Vector3> vertices, NativeArray<int> indices, bool duplicatesVertices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            NativeArray<int> processedIndices = indices;
            if (duplicatesVertices) // If this duplicates vertices (like sprite shape does)
            {
                VertexDictionary vertexDictionary = new VertexDictionary();
                processedIndices = vertexDictionary.GetIndexRemap(vertices, indices);
            }

            // Add our edges to an edge list
            EdgeDictionary edgeDictionary = new EdgeDictionary();
            NativeArray<ShadowEdge> unsortedEdges = edgeDictionary.GetOutsideEdges(vertices, processedIndices);

            // Create vertex remapping arrays
            NativeArray<int> originalToSubsetMap = new NativeArray<int>(vertices.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> subsetToOriginalMap = new NativeArray<int>(vertices.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < vertices.Length; i++)
                originalToSubsetMap[i] = -1;

            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                originalToSubsetMap[unsortedEdges[i].v0] = i;
                subsetToOriginalMap[i] = unsortedEdges[i].v0;
            }

            RemapEdges(unsortedEdges, originalToSubsetMap);
            SortEdges(unsortedEdges, out outEdges, out outShapeStartingEdge);
            RemapEdges(outEdges, subsetToOriginalMap);

            // Cleanup
            originalToSubsetMap.Dispose();
            subsetToOriginalMap.Dispose();
            unsortedEdges.Dispose();

            InitializeShapeIsClosedArray(outShapeStartingEdge, out outShapeIsClosedArray);
        }

        static public void ReverseWindingOrder(NativeArray<int> inShapeStartingEdge, NativeArray<ShadowEdge> inOutSortedEdges)
        {
            for (int shapeIndex = 0; shapeIndex < inShapeStartingEdge.Length; shapeIndex++)
            {
                int startingIndex = inShapeStartingEdge[shapeIndex];
                if (startingIndex < 0)
                    return;

                int endIndex = inOutSortedEdges.Length;
                if ((shapeIndex + 1) < inShapeStartingEdge.Length && inShapeStartingEdge[shapeIndex + 1] > -1)
                    endIndex = inShapeStartingEdge[shapeIndex + 1];

                // Reverse the winding order
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

                bool isOdd = (count & 1) == 1;
                if (isOdd)
                {
                    int edgeAIndex = startingIndex + (count >> 1);
                    ShadowEdge edgeA = inOutSortedEdges[edgeAIndex];

                    edgeA.Reverse();
                    inOutSortedEdges[edgeAIndex] = edgeA;
                }
            }
        }

        static int GetClosedPathCount(NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray)
        {
            int count = 0;
            for(int i=0;i<inShapeStartingEdge.Length;i++)
            {
                if (inShapeStartingEdge[i] < 0)
                    break;

                count++;
            }

            return count;
        }


        static void GetPathInfo(NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, out int closedPathArrayCount, out int closedPathsCount, out int openPathArrayCount, out int openPathsCount)
        {
            closedPathArrayCount = 0;
            openPathArrayCount = 0;
            closedPathsCount = 0;
            openPathsCount = 0;

            for (int i = 0; i < inShapeStartingEdge.Length; i++)
            {
                // If this shape starting edge is invalid stop..
                if (inShapeStartingEdge[i] < 0)
                    break;


                int start = inShapeStartingEdge[i];
                int end   = (i < (inShapeStartingEdge.Length - 1 )) && (inShapeStartingEdge[i + 1] != -1) ? inShapeStartingEdge[i + 1] : inEdges.Length;
                int edges  = end - start;
                if (inShapeIsClosedArray[i])
                {
                    closedPathArrayCount += edges + 1;
                    closedPathsCount++;
                }
                else
                {
                    openPathArrayCount += edges + 1;
                    openPathsCount++;
                }
            }

        }

        static public void ClipEdges(NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, float contractEdge, out NativeArray<Vector3> outVertices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge)
        {
            Allocator k_ClippingAllocator = Allocator.Persistent;
            int k_Precision = 65536;

            int closedPathCount;
            int closedPathArrayCount;
            int openPathCount;
            int openPathArrayCount;
            GetPathInfo(inEdges, inShapeStartingEdge, inShapeIsClosedArray, out closedPathArrayCount, out closedPathCount, out openPathArrayCount, out openPathCount);

            NativeArray<Clipper2D.PathArguments> clipperPathArguments = new NativeArray<Clipper2D.PathArguments>(closedPathCount, k_ClippingAllocator, NativeArrayOptions.ClearMemory);
            NativeArray<int> closedPathSizes = new NativeArray<int>(closedPathCount, k_ClippingAllocator);
            NativeArray<Vector2> closedPath = new NativeArray<Vector2>(closedPathArrayCount, k_ClippingAllocator);
            NativeArray<int> openPathSizes = new NativeArray<int>(openPathCount, k_ClippingAllocator);
            NativeArray<Vector2> openPath = new NativeArray<Vector2>(openPathArrayCount, k_ClippingAllocator);

            // Seperate out our closed and open shapes. Closed shapes will go through clipper. Open shapes will just be copied.
            int closedPathArrayIndex = 0;
            int closedPathSizesIndex = 0;
            int openPathArrayIndex = 0;
            int openPathSizesIndex = 0;
            int totalPathCount = closedPathCount + openPathCount;
            for (int shapeStartIndex = 0; (shapeStartIndex < totalPathCount); shapeStartIndex++)
            {
                int currentShapeStart = inShapeStartingEdge[shapeStartIndex];
                int nextShapeStart = (shapeStartIndex + 1) < (totalPathCount) ? inShapeStartingEdge[shapeStartIndex + 1] : inEdges.Length;
                int numberOfEdges = nextShapeStart - currentShapeStart;

                // If we have a closed shape then add it to our path and path sizes.
                if (inShapeIsClosedArray[shapeStartIndex])
                {
                    closedPathSizes[closedPathSizesIndex] = numberOfEdges + 1;
                    clipperPathArguments[closedPathSizesIndex] = new Clipper2D.PathArguments(Clipper2D.PolyType.ptSubject, true);
                    closedPathSizesIndex++;

                    for (int i = 0; i < numberOfEdges; i++)
                    {
                        closedPath[closedPathArrayIndex++] = inVertices[inEdges[i + currentShapeStart].v0];
                    }

                    closedPath[closedPathArrayIndex++] = inVertices[inEdges[numberOfEdges + currentShapeStart - 1].v1];
                }
                else
                {
                    openPathSizes[openPathSizesIndex++] = numberOfEdges + 1;

                    for (int i = 0; i < numberOfEdges; i++)
                        openPath[openPathArrayIndex++] = inVertices[inEdges[i + currentShapeStart].v0];

                    openPath[openPathArrayIndex++] = inVertices[inEdges[numberOfEdges + currentShapeStart - 1].v1];
                }
            }

            Clipper2D.Solution clipperSolution = new Clipper2D.Solution();
            Clipper2D.ExecuteArguments executeArguments = new Clipper2D.ExecuteArguments();
            executeArguments.clipType = Clipper2D.ClipType.ctUnion;
            executeArguments.clipFillType = Clipper2D.PolyFillType.pftEvenOdd;
            executeArguments.subjFillType = Clipper2D.PolyFillType.pftEvenOdd;
            executeArguments.strictlySimple = false;
            executeArguments.preserveColinear = false;
            Clipper2D.Execute(ref clipperSolution, closedPath, closedPathSizes, clipperPathArguments, executeArguments, k_ClippingAllocator, inIntScale: k_Precision, useRounding: true);

            ClipperOffset2D.Solution offsetSolution = new ClipperOffset2D.Solution();
            NativeArray<ClipperOffset2D.PathArguments> offsetPathArguments = new NativeArray<ClipperOffset2D.PathArguments>(clipperSolution.pathSizes.Length, k_ClippingAllocator, NativeArrayOptions.ClearMemory);
            ClipperOffset2D.Execute(ref offsetSolution, clipperSolution.points, clipperSolution.pathSizes, offsetPathArguments, k_ClippingAllocator, -contractEdge, inIntScale: k_Precision);

            if (offsetSolution.pathSizes.Length > 0 || openPathCount > 0)
            {
                int vertexPos = 0;

                // Combine the solutions from clipper and our open paths
                int solutionPathLens = offsetSolution.pathSizes.Length + openPathCount;
                outVertices = new NativeArray<Vector3>(offsetSolution.points.Length + openPathArrayCount, k_ClippingAllocator);
                outEdges = new NativeArray<ShadowEdge>(offsetSolution.points.Length + openPathArrayCount, k_ClippingAllocator);
                outShapeStartingEdge = new NativeArray<int>(solutionPathLens, k_ClippingAllocator);
                
                // Copy out the solution first..
                for (int i = 0; i < offsetSolution.points.Length; i++)
                    outVertices[vertexPos++] = offsetSolution.points[i];

                int start = 0;
                for (int pathSizeIndex = 0; pathSizeIndex < offsetSolution.pathSizes.Length; pathSizeIndex++)
                {
                    int pathSize = offsetSolution.pathSizes[pathSizeIndex];
                    int end = start + pathSize;
                    outShapeStartingEdge[pathSizeIndex] = start;

                    for (int shapeIndex = 0; shapeIndex < pathSize; shapeIndex++)
                    {
                        ShadowEdge edge = new ShadowEdge(shapeIndex + start, (shapeIndex + 1) % pathSize + start);
                        outEdges[shapeIndex + start] = edge;
                    }

                    start = end;
                }

                // Copy out the open vertices
                int pathStartIndex = offsetSolution.pathSizes.Length;
                start = vertexPos;  // We need to remap our vertices;

                for (int i = 0; i < openPath.Length; i++)
                    outVertices[vertexPos++] = openPath[i];

                for (int openPathIndex = 0; openPathIndex < openPathCount; openPathIndex++)
                {
                    int pathSize = openPathSizes[openPathIndex];
                    int end = start + pathSize;
                    outShapeStartingEdge[pathStartIndex + openPathIndex] = start;

                    for (int shapeIndex = 0; shapeIndex < pathSize-1; shapeIndex++)
                    {
                        ShadowEdge edge = new ShadowEdge(shapeIndex + start, shapeIndex + 1);
                        outEdges[shapeIndex + start] = edge;
                    }

                    start = end;
                }
            }
            else
            {
                outVertices = new NativeArray<Vector3>(0, k_ClippingAllocator);
                outEdges = new NativeArray<ShadowEdge>(0, k_ClippingAllocator);
                outShapeStartingEdge = new NativeArray<int>(0, k_ClippingAllocator);
            }

            closedPathSizes.Dispose();
            closedPath.Dispose();

            clipperPathArguments.Dispose();
            offsetPathArguments.Dispose();
            clipperSolution.Dispose();
            offsetSolution.Dispose();
        }
    }
}
