using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.U2D;
using UnityEngine.Rendering.Universal.UTess;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;



namespace UnityEngine.Rendering.Universal
{

    [BurstCompile]
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
                position.x = inEdgePosition0.x;
                position.y = inEdgePosition0.y;
                position.z = 0;
                tangent.x = (int)inProjectionType;
                tangent.y = 0;
                tangent.z = inEdgePosition1.x;
                tangent.w = inEdgePosition1.y;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct RemappingInfo
        {
            public int count;
            public int index;
            public int v0Offset;
            public int v1Offset;

            public void Initialize()
            {
                count = 0;
                index = -1;
                v0Offset = 0;
                v1Offset = 0;
            }
        }

        static VertexAttributeDescriptor[] m_VertexLayout = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position,   VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent,    VertexAttributeFormat.Float32, 4),
        };


        unsafe static int GetNextShapeStart(int currentShape, int* inShapeStartingEdgePtr, int inShapeStartingEdgeLength, int maxValue)
        {
            // Make sure we are in the bounds of the shapes we have. Also make sure our starting edge isn't negative
            return ((currentShape + 1 < inShapeStartingEdgeLength) && (inShapeStartingEdgePtr[currentShape + 1] >= 0)) ? inShapeStartingEdgePtr[currentShape + 1] : maxValue;
        }


        [BurstCompile]
        static internal void CalculateProjectionInfo(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray, ref NativeArray<Vector2> outProjectionInfo)
        {
            unsafe
            {
                Vector3*    inVerticesPtr           = (Vector3*)inVertices.m_Buffer;
                ShadowEdge* inEdgesPtr              = (ShadowEdge*)inEdges.m_Buffer;
                int*        inShapeStartingEdgePtr  = (int *)inShapeStartingEdge.m_Buffer;
                bool*       inShapeIsClosedArrayPtr = (bool*)inShapeIsClosedArray.m_Buffer;
                Vector2*    outProjectionInfoPtr    = (Vector2*)outProjectionInfo.m_Buffer;

                Vector2 tmpVec2 = new Vector2(); // So we don't call the constructor

                int inEdgesLength = inEdges.Length;
                int inShapeStartingEdgeLength = inShapeStartingEdge.Length;
                int inVerticesLength = inVertices.Length;

                int currentShape = 0;
                int shapeStart = 0;
                int nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdgePtr, inShapeStartingEdgeLength, inEdgesLength);
                int shapeSize = nextShapeStart;

                for (int i = 0; i < inEdgesLength; i++)
                {
                    if (i == nextShapeStart)
                    {
                        currentShape++;
                        shapeStart = nextShapeStart;
                        nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdgePtr, inShapeStartingEdgeLength, inEdgesLength);
                        shapeSize = nextShapeStart - shapeStart;
                    }

                    int nextEdgeIndex = (i - shapeStart + 1) % shapeSize + shapeStart;
                    int prevEdgeIndex = (i - shapeStart + shapeSize - 1) % shapeSize + shapeStart;


                    int v0 = inEdgesPtr[i].v0;
                    int v1 = inEdgesPtr[i].v1;

                    int prev1 = inEdgesPtr[prevEdgeIndex].v0;
                    int next0 = inEdgesPtr[nextEdgeIndex].v1;


                    tmpVec2.x = inVerticesPtr[v0].x;
                    tmpVec2.y = inVerticesPtr[v0].y;
                    Vector2 startPt = tmpVec2;

                    tmpVec2.x = inVerticesPtr[v1].x;
                    tmpVec2.y = inVerticesPtr[v1].y;
                    Vector2 endPt = tmpVec2;


                    tmpVec2.x = inVerticesPtr[prev1].x;
                    tmpVec2.y = inVerticesPtr[prev1].y;
                    Vector2 prevPt = tmpVec2;

                    tmpVec2.x = inVerticesPtr[next0].x;
                    tmpVec2.y = inVerticesPtr[next0].y;
                    Vector2 nextPt = tmpVec2;

                    // Original Vertex
                    outProjectionInfoPtr[v0] = endPt;

                    // Hard Shadows
                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;
                    outProjectionInfoPtr[additionalVerticesStart]     = endPt;
                    outProjectionInfoPtr[additionalVerticesStart + 1] = startPt;

                    // Soft Triangles
                    outProjectionInfoPtr[additionalVerticesStart + 2] = endPt;
                    outProjectionInfoPtr[additionalVerticesStart + 3] = endPt;
                }
            }
        }


        [BurstCompile]
        static internal void CalculateVertices(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<Vector2> inEdgeOtherPoints, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            unsafe
            {
                Vector3*    inVerticesPtr = (Vector3*)inVertices.m_Buffer;
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.m_Buffer;
                Vector2*    inEdgeOtherPointsPtr = (Vector2*)inEdgeOtherPoints.m_Buffer;
                ShadowMeshVertex* outMeshVerticesPtr = (ShadowMeshVertex*)outMeshVertices.m_Buffer;

                Vector2 tmpVec2 = new Vector2(); // So we don't call the constructor

                int inEdgesLength = inEdges.Length;
                int inVerticesLength = inVertices.Length;

                
                for (int i = 0; i < inVerticesLength; i++)
                {
                    tmpVec2.x = inVerticesPtr[i].x;
                    tmpVec2.y = inVerticesPtr[i].y;
                    ShadowMeshVertex originalShadowMesh = new ShadowMeshVertex(ProjectionType.ProjectionNone, tmpVec2, inEdgeOtherPointsPtr[i]);
                    outMeshVerticesPtr[i] = originalShadowMesh;
                }

                for (int i = 0; i < inEdgesLength; i++)
                {
                    int v0 = inEdgesPtr[i].v0;
                    int v1 = inEdgesPtr[i].v1;


                    tmpVec2.x = inVerticesPtr[v0].x;
                    tmpVec2.y = inVerticesPtr[v0].y;
                    Vector2 pt0 = tmpVec2;

                    tmpVec2.x = inVerticesPtr[v1].x;
                    tmpVec2.y = inVerticesPtr[v1].y;
                    Vector2 pt1 = tmpVec2;

                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;
                    ShadowMeshVertex additionalVertex0 = new ShadowMeshVertex(ProjectionType.ProjectionHard, pt0, inEdgeOtherPointsPtr[additionalVerticesStart]);
                    ShadowMeshVertex additionalVertex1 = new ShadowMeshVertex(ProjectionType.ProjectionHard, pt1, inEdgeOtherPointsPtr[additionalVerticesStart + 1]);
                    ShadowMeshVertex additionalVertex2 = new ShadowMeshVertex(ProjectionType.ProjectionSoftLeft, pt0, inEdgeOtherPointsPtr[additionalVerticesStart + 2]);
                    ShadowMeshVertex additionalVertex3 = new ShadowMeshVertex(ProjectionType.ProjectionSoftRight, pt0, inEdgeOtherPointsPtr[additionalVerticesStart + 3]);

                    outMeshVerticesPtr[additionalVerticesStart] = additionalVertex0;
                    outMeshVerticesPtr[additionalVerticesStart + 1] = additionalVertex1;
                    outMeshVerticesPtr[additionalVerticesStart + 2] = additionalVertex2;
                    outMeshVerticesPtr[additionalVerticesStart + 3] = additionalVertex3;
                }
            }
        }

        [BurstCompile]
        static internal void CalculateTriangles(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray, ref NativeArray<int> outMeshIndices)
        {
            unsafe
            {
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.m_Buffer;
                int*        inShapeStartingEdgePtr = (int*)inShapeStartingEdge.m_Buffer;
                int*        outMeshIndicesPtr = (int*)outMeshIndices.m_Buffer;

                int inEdgesLength = inEdges.Length;
                int inShapeStartingEdgeLength = inShapeStartingEdge.Length;
                int inVerticesLength = inVertices.Length;

                int meshIndex = 0;
                for (int shapeIndex = 0; shapeIndex < inShapeStartingEdgeLength; shapeIndex++)
                {
                    int startingIndex = inShapeStartingEdgePtr[shapeIndex];
                    if (startingIndex < 0)
                        return;

                    int endIndex = inEdgesLength;
                    if ((shapeIndex + 1) < inShapeStartingEdgeLength && inShapeStartingEdgePtr[shapeIndex + 1] > -1)
                        endIndex = inShapeStartingEdgePtr[shapeIndex + 1];

                    //// Hard Shadow Geometry
                    int prevEdge = endIndex - 1;
                    for (int i = startingIndex; i < endIndex; i++)
                    {
                        int v0 = inEdgesPtr[i].v0;
                        int v1 = inEdgesPtr[i].v1;

                        int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;

                        // Add a degenerate rectangle
                        outMeshIndicesPtr[meshIndex++] = (ushort)v0;
                        outMeshIndicesPtr[meshIndex++] = (ushort)additionalVerticesStart;
                        outMeshIndicesPtr[meshIndex++] = (ushort)(additionalVerticesStart + 1);
                        outMeshIndicesPtr[meshIndex++] = (ushort)(additionalVerticesStart + 1);
                        outMeshIndicesPtr[meshIndex++] = (ushort)v1;
                        outMeshIndicesPtr[meshIndex++] = (ushort)v0;

                        prevEdge = i;
                    }

                    // Soft Shadow Geometry
                    for (int i = startingIndex; i < endIndex; i++)
                    //int i = 0;
                    {
                        int v0 = inEdgesPtr[i].v0;
                        int v1 = inEdgesPtr[i].v1;

                        int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;

                        // We also need 1 more triangles for soft shadows (3 indices)
                        outMeshIndicesPtr[meshIndex++] = (ushort)v0;
                        outMeshIndicesPtr[meshIndex++] = (ushort)additionalVerticesStart + 2;
                        outMeshIndicesPtr[meshIndex++] = (ushort)additionalVerticesStart + 3;
                    }
                }
            }
        }

        [BurstCompile]
        static internal void CalculateLocalBounds(ref NativeArray<Vector3> inVertices, out Bounds retBounds)
        {
            if (inVertices.Length <= 0)
            {
                retBounds = new Bounds(Vector3.zero, Vector3.zero);
            }
            else
            {

                Vector2 minVec = Vector2.positiveInfinity;
                Vector2 maxVec = Vector2.negativeInfinity;

                unsafe
                {
                    Vector3* inVerticesPtr = (Vector3*)inVertices.m_Buffer;
                    int inVerticesLength = inVertices.Length;

                    // Add outline vertices
                    for (int i = 0; i < inVerticesLength; i++)
                    {
                        Vector2 vertex = new Vector2(inVerticesPtr[i].x, inVerticesPtr[i].y);

                        minVec = Vector2.Min(minVec, vertex);
                        maxVec = Vector2.Max(maxVec, vertex);
                    }
                }

                retBounds = new Bounds { max = maxVec, min = minVec };
            }
        }

        [BurstCompile]
        static void GenerateInteriorMesh(ref NativeArray<ShadowMeshVertex> inVertices, ref NativeArray<int> inIndices, ref NativeArray<ShadowEdge> inEdges, out NativeArray<ShadowMeshVertex> outVertices, out NativeArray<int> outIndices, out int outStartIndex, out int outIndexCount)
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

            tessInEdges.Dispose();
            tessInVertices.Dispose();
            tessOutIndices.Dispose();
            tessOutVertices.Dispose();
            tessOutEdges.Dispose();
        }

        //inEdges is expected to be contiguous
        static public Bounds GenerateShadowMesh(Mesh mesh, NativeArray<Vector3> inVertices, NativeArray<ShadowEdge> inEdges, NativeArray<int> inShapeStartingEdge, NativeArray<bool> inShapeIsClosedArray, bool allowContraction, bool fill, ShadowShape2D.OutlineTopology topology)
        {
            // Setup our buffers
            int meshVertexCount = inVertices.Length + k_AdditionalVerticesPerEdge * inEdges.Length;                       // Each vertex will have a duplicate that can be extruded.
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;  // There are two triangles per edge making a degenerate rectangle (0 area)

            NativeArray<Vector2> meshProjectionInfo = new NativeArray<Vector2>(meshVertexCount, Allocator.Persistent);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Persistent);
            NativeArray<ShadowMeshVertex> meshVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Persistent);

            CalculateProjectionInfo(ref inVertices, ref inEdges, ref inShapeStartingEdge, ref inShapeIsClosedArray, ref meshProjectionInfo);
            CalculateVertices(ref inVertices, ref inEdges, ref meshProjectionInfo, ref meshVertices);
            CalculateTriangles(ref inVertices, ref inEdges, ref inShapeStartingEdge, ref inShapeIsClosedArray, ref meshIndices);

            NativeArray<ShadowMeshVertex> finalVertices;
            NativeArray<int> finalIndices;
            int fillSubmeshStartIndex = 0;
            int fillSubmeshIndexCount = 0;

            if (fill) // This has limited utility at the moment as contraction is not calculated. More work will need to be done to generalize this
            {
                GenerateInteriorMesh(ref meshVertices, ref meshIndices, ref inEdges, out finalVertices, out finalIndices, out fillSubmeshStartIndex, out fillSubmeshIndexCount);
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

            CalculateLocalBounds(ref inVertices, out Bounds retLocalBound);
            return retLocalBound;
        }


        [BurstCompile]
        static public void CalculateEdgesFromLines(ref NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            unsafe
            {
                int numOfEdges = indices.Length >> 1;
                NativeArray<int> tempShapeStartIndices = new NativeArray<int>(numOfEdges, Allocator.Persistent);
                NativeArray<bool> tempShapeIsClosedArray = new NativeArray<bool>(numOfEdges, Allocator.Persistent);

                int* indicesPtr = (int*)indices.m_Buffer;
                int* tempShapeStartIndicesPtr = (int*)tempShapeStartIndices.m_Buffer;
                bool* tempShapeIsClosedArrayPtr = (bool*)tempShapeIsClosedArray.m_Buffer;

                int indicesLength = indices.Length;

                // Find the shape starting indices and allow contraction
                int shapeCount = 0;
                int shapeStart = indicesPtr[0];
                int lastIndex = indicesPtr[0];
                bool closedShapeFound = false;
                tempShapeStartIndicesPtr[0] = 0;

                for (int i = 0; i < indicesLength; i += 2)
                {
                    if (closedShapeFound)
                    {
                        shapeStart = indicesPtr[i];
                        tempShapeIsClosedArrayPtr[shapeCount] = true;
                        tempShapeStartIndicesPtr[++shapeCount] = i >> 1;
                        closedShapeFound = false;
                    }
                    else if (indicesPtr[i] != lastIndex)
                    {
                        tempShapeIsClosedArrayPtr[shapeCount] = false;
                        tempShapeStartIndicesPtr[++shapeCount] = i >> 1;
                        shapeStart = indicesPtr[i];
                    }

                    if (shapeStart == indicesPtr[i + 1])
                        closedShapeFound = true;

                    lastIndex = indicesPtr[i + 1];
                }

                tempShapeIsClosedArrayPtr[shapeCount++] = closedShapeFound;

                // Copy the our data to a smaller array
                outShapeStartingEdge = new NativeArray<int>(shapeCount, Allocator.Persistent);
                outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Persistent);
                int* outShapeStartingEdgePtr = (int*)outShapeStartingEdge.m_Buffer;
                bool* outShapeIsClosedArrayPtr = (bool*)outShapeIsClosedArray.m_Buffer;

                for (int i = 0; i < shapeCount; i++)
                {
                    outShapeStartingEdgePtr[i] = tempShapeStartIndicesPtr[i];
                    outShapeIsClosedArrayPtr[i] = tempShapeIsClosedArrayPtr[i];
                }
                tempShapeStartIndices.Dispose();
                tempShapeIsClosedArray.Dispose();

                // Add edges
                outEdges = new NativeArray<ShadowEdge>(numOfEdges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.m_Buffer;
                for (int i = 0; i < numOfEdges; i++)
                {
                    int indicesIndex = i << 1;
                    int v0Index = indicesPtr[indicesIndex];
                    int v1Index = indicesPtr[indicesIndex + 1];

                    outEdgesPtr[i] = new ShadowEdge(v0Index, v1Index);
                }
            }
        }


        [BurstCompile]
        static internal void GetVertexReferenceStats(ref NativeArray<Vector3> vertices, ref NativeArray<ShadowEdge> edges, int vertexCount, out bool hasReusedVertices, out int newVertexCount, out NativeArray<RemappingInfo> remappingInfo)
        {
            unsafe
            {
                int edgeCount = edges.Length;

                newVertexCount = 0;
                hasReusedVertices = false;
                remappingInfo = new NativeArray<RemappingInfo>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                RemappingInfo* remappingInfoPtr = (RemappingInfo*)remappingInfo.GetUnsafePtr();
                ShadowEdge* edgesPtr = (ShadowEdge*)edges.GetUnsafePtr();

                // Clear the remapping info
                for (int i = 0; i < vertexCount; i++)
                    remappingInfoPtr[i].Initialize();

                // Process v0
                for (int i = 0; i < edgeCount; i++)
                {
                    int v0 = edgesPtr[i].v0;
                    remappingInfoPtr[v0].count = remappingInfoPtr[v0].count + 1;
                    if (remappingInfoPtr[v0].count > 1)
                        hasReusedVertices = true;

                    newVertexCount++;
                }

                // Process v1
                for (int i = 0; i < edgeCount; i++)
                {
                    int v1 = edgesPtr[i].v1;
                    if (remappingInfoPtr[v1].count == 0)  // This is an open shape
                    {
                        remappingInfoPtr[v1].count = 1;
                        newVertexCount++;
                    }
                }


                // Find the starts of the new indices..
                int startPos = 0;
                for (int i=0;i<vertexCount;i++)
                {
                    // Leave the other indices -1 for easier validation testing
                    if (remappingInfoPtr[i].count > 0)
                    {
                        remappingInfoPtr[i].index = startPos;
                        startPos += remappingInfoPtr[i].count;
                    }
                }
            }
        }


        static public bool IsTriangleReversed(NativeArray<Vector3> vertices, int idx0, int idx1, int idx2)
        {
            Vector3 v0 = vertices[idx0];
            Vector3 v1 = vertices[idx1];
            Vector3 v2 = vertices[idx2];

            float twiceArea = (v0.x * v1.y + v1.x * v2.y + v2.x * v0.y) - (v0.y * v1.x + v1.y * v2.x + v2.y * v0.x);
            return Mathf.Sign(twiceArea) >= 0;
        }


        [BurstCompile]
        static public void CalculateEdgesFromTriangles(ref NativeArray<Vector3> vertices, ref NativeArray<int> indices, bool duplicatesVertices, out NativeArray<Vector3> newVertices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            unsafe
            {
                // Run clipper to calculate edges..
                Clipper2D.Solution solution = new Clipper2D.Solution();
                Clipper2D.ExecuteArguments executeArguments = new Clipper2D.ExecuteArguments(Clipper2D.InitOptions.ioDefault, Clipper2D.ClipType.ctUnion);

                int triangleCount = indices.Length / 3;
                NativeArray<Vector2> points = new NativeArray<Vector2>(indices.Length, Allocator.Persistent);
                NativeArray<int> pathSizes = new NativeArray<int>(triangleCount, Allocator.Persistent);
                NativeArray<Clipper2D.PathArguments> pathArguments = new NativeArray<Clipper2D.PathArguments>(triangleCount, Allocator.Persistent);

                // Pointers to our native arrays for performance in editor
                Vector2* pointsPtr = (Vector2*)points.GetUnsafePtr<Vector2>();
                int* pathSizesPtr = (int*)pathSizes.GetUnsafePtr<int>();
                Clipper2D.PathArguments* pathArgumentsPtr = (Clipper2D.PathArguments*)pathArguments.GetUnsafePtr<Clipper2D.PathArguments>();
                Vector3* verticesPtr = (Vector3*)vertices.GetUnsafePtr<Vector3>();

                // Copy input data for Clipper2D.Execute
                Clipper2D.PathArguments sharedPathArg = new Clipper2D.PathArguments(Clipper2D.PolyType.ptSubject, true);
                for (int i = 0; i < triangleCount; i++)
                {
                    pathSizesPtr[i] = 3;
                    pathArgumentsPtr[i] = sharedPathArg;

                    int pointOffset = 3 * i;
                    pointsPtr[pointOffset] = verticesPtr[indices[pointOffset]];
                    pointsPtr[pointOffset + 1] = verticesPtr[indices[pointOffset + 1]];
                    pointsPtr[pointOffset + 2] = verticesPtr[indices[pointOffset + 2]];
                }

                Clipper2D.Execute(ref solution, points, pathSizes, pathArguments, executeArguments, Allocator.Persistent);

                // Cleanup execute inputs because we have necessary data in our solution
                points.Dispose();
                pathSizes.Dispose();
                pathArguments.Dispose();

                // Copy solution to outputs
                int pointLen = solution.points.Length;
                int shapeCount = solution.pathSizes.Length;
                newVertices = new NativeArray<Vector3>(pointLen, Allocator.Persistent);
                outEdges = new NativeArray<ShadowEdge>(pointLen, Allocator.Persistent);
                outShapeStartingEdge = new NativeArray<int>(shapeCount, Allocator.Persistent);
                outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Persistent);

                // More pointers for edtor time perfomance
                int* solutionPathSizesPtr = (int*)solution.pathSizes.GetUnsafePtr<int>();
                Vector2* solutionPointsPtr = (Vector2*)solution.points.GetUnsafePtr<Vector2>();

                Vector3* newVerticesPtr = (Vector3*)newVertices.GetUnsafePtr<Vector3>();
                ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.GetUnsafePtr<ShadowEdge>();
                int* outShapeStartingEdgePtr = (int*)outShapeStartingEdge.GetUnsafePtr<int>();
                bool* outShapeIsClosedArrayPtr = (bool*)outShapeIsClosedArray.GetUnsafePtr<bool>();

                // Copy output data from the solution
                int nextStart = 0;
                for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
                {
                    // Copy shape info to outputs
                    int curStart = nextStart;
                    int curPathSize = solutionPathSizesPtr[shapeIndex];
                    outShapeStartingEdgePtr[shapeIndex] = nextStart;
                    nextStart += curPathSize;

                    // Copy vertices and edges to outputs;
                    int previousVertex = nextStart - 1;
                    for (int pointIndex = curStart; pointIndex < nextStart; pointIndex++)
                    {
                        newVerticesPtr[pointIndex] = solutionPointsPtr[pointIndex];
                        outEdgesPtr[pointIndex] = new ShadowEdge(previousVertex, pointIndex);
                        previousVertex = pointIndex;
                    }

                    // All shapes are closed since they are created from triangles
                    outShapeIsClosedArrayPtr[shapeIndex] = true;
                }

                // Cleanup solution
                solution.Dispose();
            }
        }


        [BurstCompile]
        static public void ReverseWindingOrder(ref NativeArray<int> inShapeStartingEdge, ref NativeArray<ShadowEdge> inOutSortedEdges)
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

        static int GetClosedPathCount(ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray)
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

        [BurstCompile]
        static public void ClipEdges(ref NativeArray<Vector3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray, float contractEdge, out NativeArray<Vector3> outVertices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge)
        {
            unsafe
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

                Clipper2D.PathArguments* clipperPathArgumentsPtr = (Clipper2D.PathArguments*)clipperPathArguments.m_Buffer;
                int* closedPathSizesPtr = (int*)closedPathSizes.m_Buffer;
                Vector2* closedPathPtr = (Vector2*)closedPath.m_Buffer;
                int* openPathSizesPtr = (int*)openPathSizes.m_Buffer;
                Vector2* openPathPtr = (Vector2*)openPath.m_Buffer;

                int* inShapeStartingEdgePtr = (int*)inShapeStartingEdge.m_Buffer;
                bool* inShapeIsClosedArrayPtr = (bool*)inShapeIsClosedArray.m_Buffer;
                Vector3* inVerticesPtr = (Vector3*)inVertices.m_Buffer;
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.m_Buffer;
                

                int inEdgesLength = inEdges.Length;

                Vector2 tmpVec2 = new Vector2(); // So we don't call the constructor
                Vector3 tmpVec3 = Vector3.zero;
                

                // Seperate out our closed and open shapes. Closed shapes will go through clipper. Open shapes will just be copied.
                int closedPathArrayIndex = 0;
                int closedPathSizesIndex = 0;
                int openPathArrayIndex = 0;
                int openPathSizesIndex = 0;
                int totalPathCount = closedPathCount + openPathCount;
                for (int shapeStartIndex = 0; (shapeStartIndex < totalPathCount); shapeStartIndex++)
                {
                    int currentShapeStart = inShapeStartingEdgePtr[shapeStartIndex];
                    int nextShapeStart = (shapeStartIndex + 1) < (totalPathCount) ? inShapeStartingEdgePtr[shapeStartIndex + 1] : inEdgesLength;
                    int numberOfEdges = nextShapeStart - currentShapeStart;

                    // If we have a closed shape then add it to our path and path sizes.
                    if (inShapeIsClosedArrayPtr[shapeStartIndex])
                    {
                        closedPathSizesPtr[closedPathSizesIndex] = numberOfEdges + 1;
                        clipperPathArgumentsPtr[closedPathSizesIndex] = new Clipper2D.PathArguments(Clipper2D.PolyType.ptSubject, true);
                        closedPathSizesIndex++;

                        for (int i = 0; i < numberOfEdges; i++)
                        {
                            Vector3 vec3 = inVerticesPtr[inEdgesPtr[i + currentShapeStart].v0];
                            tmpVec2.x = vec3.x;
                            tmpVec2.y = vec3.y;
                            closedPathPtr[closedPathArrayIndex++] = tmpVec2;
                        }

                        closedPathPtr[closedPathArrayIndex++] = inVerticesPtr[inEdgesPtr[numberOfEdges + currentShapeStart - 1].v1];
                    }
                    else
                    {
                        openPathSizesPtr[openPathSizesIndex++] = numberOfEdges + 1;
                        for (int i = 0; i < numberOfEdges; i++)
                        {

                            Vector3 vec3 = inVerticesPtr[inEdgesPtr[i + currentShapeStart].v0];
                            tmpVec2.x = vec3.x;
                            tmpVec2.y = vec3.y;
                            openPathPtr[openPathArrayIndex++] = tmpVec2;
                        }

                        openPathPtr[openPathArrayIndex++] = inVerticesPtr[inEdgesPtr[numberOfEdges + currentShapeStart - 1].v1];
                    }
                }


                NativeArray<Vector2> clipperOffsetPath = closedPath;
                NativeArray<int> clipperOffsetPathSizes = closedPathSizes;

                Clipper2D.Solution clipperSolution = new Clipper2D.Solution();

                // Run this to try to merge outlines if there is more than one
                if (closedPathSizes.Length > 1)
                {
                    Clipper2D.ExecuteArguments executeArguments = new Clipper2D.ExecuteArguments();
                    executeArguments.clipType = Clipper2D.ClipType.ctUnion;
                    executeArguments.clipFillType = Clipper2D.PolyFillType.pftEvenOdd;
                    executeArguments.subjFillType = Clipper2D.PolyFillType.pftEvenOdd;
                    executeArguments.strictlySimple = false;
                    executeArguments.preserveColinear = false;
                    Clipper2D.Execute(ref clipperSolution, closedPath, closedPathSizes, clipperPathArguments, executeArguments, k_ClippingAllocator, inIntScale: k_Precision, useRounding: true);

                    clipperOffsetPath = clipperSolution.points;
                    clipperOffsetPathSizes = clipperSolution.pathSizes;
                }

                ClipperOffset2D.Solution offsetSolution = new ClipperOffset2D.Solution();
                NativeArray<ClipperOffset2D.PathArguments> offsetPathArguments = new NativeArray<ClipperOffset2D.PathArguments>(clipperOffsetPathSizes.Length, k_ClippingAllocator, NativeArrayOptions.ClearMemory);
                ClipperOffset2D.Execute(ref offsetSolution, clipperOffsetPath, clipperOffsetPathSizes, offsetPathArguments, k_ClippingAllocator, -contractEdge, inIntScale: k_Precision);


                if (offsetSolution.pathSizes.Length > 0 || openPathCount > 0)
                {
                    int vertexPos = 0;

                    // Combine the solutions from clipper and our open paths
                    int solutionPathLens = offsetSolution.pathSizes.Length + openPathCount;
                    outVertices = new NativeArray<Vector3>(offsetSolution.points.Length + openPathArrayCount, k_ClippingAllocator);
                    outEdges = new NativeArray<ShadowEdge>(offsetSolution.points.Length + openPathArrayCount, k_ClippingAllocator);
                    outShapeStartingEdge = new NativeArray<int>(solutionPathLens, k_ClippingAllocator);

                    Vector3* outVerticesPtr = (Vector3*)outVertices.m_Buffer;
                    ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.m_Buffer;
                    int* outShapeStartingEdgePtr = (int*)outShapeStartingEdge.m_Buffer;

                    Vector2* offsetSolutionPointsPtr = (Vector2*)offsetSolution.points.m_Buffer;
                    int offsetSolutionPointsLength = offsetSolution.points.Length;

                    int* offsetSolutionPathSizesPtr = (int*)offsetSolution.pathSizes.m_Buffer;
                    int offsetSolutionPathSizesLength = offsetSolution.pathSizes.Length;


                    
                    // Copy out the solution first..
                    for (int i = 0; i < offsetSolutionPointsLength; i++)
                    {
                        tmpVec3.x = offsetSolutionPointsPtr[i].x;
                        tmpVec3.y = offsetSolutionPointsPtr[i].y;
                        outVerticesPtr[vertexPos++] = tmpVec3;
                    }

                    int start = 0;
                    for (int pathSizeIndex = 0; pathSizeIndex < offsetSolutionPathSizesLength; pathSizeIndex++)
                    {
                        int pathSize = offsetSolutionPathSizesPtr[pathSizeIndex];
                        int end = start + pathSize;
                        outShapeStartingEdgePtr[pathSizeIndex] = start;

                        for (int shapeIndex = 0; shapeIndex < pathSize; shapeIndex++)
                        {
                            ShadowEdge edge = new ShadowEdge(shapeIndex + start, (shapeIndex + 1) % pathSize + start);
                            outEdgesPtr[shapeIndex + start] = edge;
                        }

                        start = end;
                    }

                    // Copy out the open vertices
                    int pathStartIndex = offsetSolutionPathSizesLength;
                    start = vertexPos;  // We need to remap our vertices;

                    for (int i = 0; i < openPath.Length; i++)
                    {
                        tmpVec3.x = openPathPtr[i].x;
                        tmpVec3.y = openPathPtr[i].y;
                        outVerticesPtr[vertexPos++] = tmpVec3;
                    }

                    for (int openPathIndex = 0; openPathIndex < openPathCount; openPathIndex++)
                    {
                        int pathSize = openPathSizesPtr[openPathIndex];
                        int end = start + pathSize;
                        outShapeStartingEdgePtr[pathStartIndex + openPathIndex] = start;

                        for (int shapeIndex = 0; shapeIndex < pathSize - 1; shapeIndex++)
                        {
                            ShadowEdge edge = new ShadowEdge(shapeIndex + start, shapeIndex + 1);
                            outEdgesPtr[shapeIndex + start] = edge;
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
                openPathSizes.Dispose();
                openPath.Dispose();

                clipperPathArguments.Dispose();
                offsetPathArguments.Dispose();
                clipperSolution.Dispose();
                offsetSolution.Dispose();
            }
        }
    }
}
