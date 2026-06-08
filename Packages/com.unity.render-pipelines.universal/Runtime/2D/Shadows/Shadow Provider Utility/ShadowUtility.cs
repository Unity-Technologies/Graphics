using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.U2D;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

#if USING_2DCOMMON
using UnityEngine.U2D.Common;
#endif

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    internal static class ShadowUtility
    {
        internal const int k_AdditionalVerticesPerEdge = 4;
        internal const int k_VerticesPerTriangle = 3;
        internal const int k_TrianglesPerEdge = 3;
        internal const int k_MinimumEdges = 3;
        internal const int k_SafeSize = 40;

        public enum ProjectionType
        {
            ProjectionNone = -1,
            ProjectionHard = 0,
            ProjectionSoftLeft = 1,
            ProjectionSoftRight = 3,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct ShadowMeshVertex
        {
            [SerializeField] internal Vector3 position;
            [SerializeField] internal Vector4 tangent;

            internal ShadowMeshVertex(ProjectionType inProjectionType, float2 inEdgePosition0, float2 inEdgePosition1)
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

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        static void ResetStaticsOnLoad()
        {
            m_VertexLayout = new VertexAttributeDescriptor[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position,   VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Tangent,    VertexAttributeFormat.Float32, 4),
            };
        }
#endif

        [BurstCompile]
        unsafe static int GetNextShapeStart(int currentShape, int* inShapeStartingEdgePtr, int inShapeStartingEdgeLength, int maxValue)
        {
            return ((currentShape + 1 < inShapeStartingEdgeLength) && (inShapeStartingEdgePtr[currentShape + 1] >= 0)) ? inShapeStartingEdgePtr[currentShape + 1] : maxValue;
        }

        [BurstCompile]
        static internal void CalculateProjectionInfo_FromTriangles(ref NativeArray<float3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<float2> outProjectionInfo)
        {
            unsafe
            {
                float3* inVerticesPtr = (float3*)inVertices.GetUnsafePtr();
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();
                int* inShapeStartingEdgePtr = (int*)inShapeStartingEdge.GetUnsafePtr();
                float2* outProjectionInfoPtr = (float2*)outProjectionInfo.GetUnsafePtr();

                int inEdgesLength = inEdges.Length;
                int inShapeStartingEdgeLength = inShapeStartingEdge.Length;
                int inVerticesLength = inVertices.Length;

                int currentShape = 0;
                int shapeStart = 0;
                int nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdgePtr, inShapeStartingEdgeLength, inEdgesLength);
                int shapeSize = nextShapeStart;

                for (int i = 0; i < inEdgesLength; i++)
                {
                    while (i >= nextShapeStart && currentShape < inShapeStartingEdgeLength - 1)
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

                    float2 startPt = inVerticesPtr[v0].xy;
                    float2 endPt = inVerticesPtr[v1].xy;
                    float2 prevPt = inVerticesPtr[prev1].xy;
                    float2 nextPt = inVerticesPtr[next0].xy;

                    // Original Vertex
                    outProjectionInfoPtr[v0] = endPt;

                    // Hard Shadows
                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;
                    outProjectionInfoPtr[additionalVerticesStart] = endPt;
                    outProjectionInfoPtr[additionalVerticesStart + 1] = startPt;

                    // Soft Triangles
                    outProjectionInfoPtr[additionalVerticesStart + 2] = prevPt;
                    outProjectionInfoPtr[additionalVerticesStart + 3] = nextPt;
                }
            }
        }

        [BurstCompile]
        static internal void CalculateProjectionInfo_FromLines(ref NativeArray<float3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<float2> outProjectionInfo)
        {
            unsafe
            {
                float3* inVerticesPtr = (float3*)inVertices.GetUnsafePtr();
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();
                int* inShapeStartingEdgePtr = (int*)inShapeStartingEdge.GetUnsafePtr();
                float2* outProjectionInfoPtr = (float2*)outProjectionInfo.GetUnsafePtr();

                int inEdgesLength = inEdges.Length;
                int inShapeStartingEdgeLength = inShapeStartingEdge.Length;
                int inVerticesLength = inVertices.Length;

                int currentShape = 0;
                int shapeStart = 0;
                int nextShapeStart = GetNextShapeStart(currentShape, inShapeStartingEdgePtr, inShapeStartingEdgeLength, inEdgesLength);
                int shapeSize = nextShapeStart;

                for (int i = 0; i < inEdgesLength; i++)
                {
                    while (i >= nextShapeStart && currentShape < inShapeStartingEdgeLength - 1)
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

                    float2 startPt = inVerticesPtr[v0].xy;
                    float2 endPt = inVerticesPtr[v1].xy;
                    float2 prevPt = inVerticesPtr[prev1].xy;
                    float2 nextPt = inVerticesPtr[next0].xy;

                    // Original Vertex
                    outProjectionInfoPtr[v0] = endPt;

                    // Hard Shadows
                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;
                    outProjectionInfoPtr[additionalVerticesStart] = endPt;
                    outProjectionInfoPtr[additionalVerticesStart + 1] = startPt;

                    // Soft Triangles
                    outProjectionInfoPtr[additionalVerticesStart + 2] = prevPt;
                    outProjectionInfoPtr[additionalVerticesStart + 3] = nextPt;
                }
            }
        }

        [BurstCompile]
        static internal void CalculateVertices(ref NativeArray<float3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<float2> inEdgeOtherPoints, ref NativeArray<ShadowMeshVertex> outMeshVertices)
        {
            unsafe
            {
                float3* inVerticesPtr = (float3*)inVertices.GetUnsafePtr();
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();
                float2* inEdgeOtherPointsPtr = (float2*)inEdgeOtherPoints.GetUnsafePtr();
                ShadowMeshVertex* outMeshVerticesPtr = (ShadowMeshVertex*)outMeshVertices.GetUnsafePtr();

                int inEdgesLength = inEdges.Length;
                int inVerticesLength = inVertices.Length;

                for (int i = 0; i < inVerticesLength; i++)
                {
                    float2 pt = inVerticesPtr[i].xy;
                    outMeshVerticesPtr[i] = new ShadowMeshVertex(ProjectionType.ProjectionNone, pt, inEdgeOtherPointsPtr[i]);
                }

                for (int i = 0; i < inEdgesLength; i++)
                {
                    int v0 = inEdgesPtr[i].v0;
                    int v1 = inEdgesPtr[i].v1;

                    float2 pt0 = inVerticesPtr[v0].xy;
                    float2 pt1 = inVerticesPtr[v1].xy;

                    int additionalVerticesStart = k_AdditionalVerticesPerEdge * i + inVerticesLength;
                    outMeshVerticesPtr[additionalVerticesStart] = new ShadowMeshVertex(ProjectionType.ProjectionHard, pt0, inEdgeOtherPointsPtr[additionalVerticesStart]);
                    outMeshVerticesPtr[additionalVerticesStart + 1] = new ShadowMeshVertex(ProjectionType.ProjectionHard, pt1, inEdgeOtherPointsPtr[additionalVerticesStart + 1]);
                    outMeshVerticesPtr[additionalVerticesStart + 2] = new ShadowMeshVertex(ProjectionType.ProjectionSoftLeft, pt0, inEdgeOtherPointsPtr[additionalVerticesStart + 2]);
                    outMeshVerticesPtr[additionalVerticesStart + 3] = new ShadowMeshVertex(ProjectionType.ProjectionSoftRight, pt0, inEdgeOtherPointsPtr[additionalVerticesStart + 3]);
                }
            }
        }

        [BurstCompile]
        static internal void CalculateShadowTriangles_FromTriangles(ref NativeArray<float3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<int> outMeshIndices)
        {
            unsafe
            {
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();
                int* inShapeStartingEdgePtr = (int*)inShapeStartingEdge.GetUnsafePtr();
                int* outMeshIndicesPtr = (int*)outMeshIndices.GetUnsafePtr();

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

                    // Hard Shadow Geometry - optimized and unrolled
                    for (int i = startingIndex; i < endIndex; i++)
                    {
                        int v0 = inEdgesPtr[i].v0;
                        int v1 = inEdgesPtr[i].v1;
                        int additionalVerticesStart = (k_AdditionalVerticesPerEdge * i) + inVerticesLength;
                        int av1 = additionalVerticesStart + 1;

                        // Unroll the degenerate rectangle writes
                        outMeshIndicesPtr[meshIndex] = v0;
                        outMeshIndicesPtr[meshIndex + 1] = additionalVerticesStart;
                        outMeshIndicesPtr[meshIndex + 2] = av1;
                        outMeshIndicesPtr[meshIndex + 3] = av1;
                        outMeshIndicesPtr[meshIndex + 4] = v1;
                        outMeshIndicesPtr[meshIndex + 5] = v0;
                        meshIndex += 6;
                    }

                    // Soft Shadow Geometry - unrolled
                    for (int i = startingIndex; i < endIndex; i++)
                    {
                        int v0 = inEdgesPtr[i].v0;
                        int additionalVerticesStart = (k_AdditionalVerticesPerEdge * i) + inVerticesLength;

                        outMeshIndicesPtr[meshIndex] = v0;
                        outMeshIndicesPtr[meshIndex + 1] = additionalVerticesStart + 2;
                        outMeshIndicesPtr[meshIndex + 2] = additionalVerticesStart + 3;
                        meshIndex += 3;
                    }
                }
            }
        }

        [BurstCompile]
        static internal void CalculateShadowTriangles_FromLines(ref NativeArray<float3> inVertices, ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> outMeshIndices)
        {
            unsafe
            {
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();
                int* outMeshIndicesPtr = (int*)outMeshIndices.GetUnsafePtr();

                int inEdgesLength = inEdges.Length;
                int inVerticesLength = inVertices.Length;

                int meshIndex = 0;

                // Hard Shadow Geometry - optimized and unrolled
                for (int i = 0; i < inEdgesLength; i++)
                {
                    int v0 = inEdgesPtr[i].v0;
                    int v1 = inEdgesPtr[i].v1;
                    int additionalVerticesStart = (k_AdditionalVerticesPerEdge * i) + inVerticesLength;
                    int av1 = additionalVerticesStart + 1;

                    // Unroll the degenerate rectangle writes
                    outMeshIndicesPtr[meshIndex] = v0;
                    outMeshIndicesPtr[meshIndex + 1] = additionalVerticesStart;
                    outMeshIndicesPtr[meshIndex + 2] = av1;
                    outMeshIndicesPtr[meshIndex + 3] = av1;
                    outMeshIndicesPtr[meshIndex + 4] = v1;
                    outMeshIndicesPtr[meshIndex + 5] = v0;
                    meshIndex += 6;
                }

                // Soft Shadow Geometry - unrolled
                for (int i = 0; i < inEdgesLength; i++)
                {
                    int v0 = inEdgesPtr[i].v0;
                    int additionalVerticesStart = (k_AdditionalVerticesPerEdge * i) + inVerticesLength;

                    outMeshIndicesPtr[meshIndex] = v0;
                    outMeshIndicesPtr[meshIndex + 1] = additionalVerticesStart + 2;
                    outMeshIndicesPtr[meshIndex + 2] = additionalVerticesStart + 3;
                    meshIndex += 3;
                }
            }
        }

        [BurstCompile]
        static internal void CalculateLocalBounds(ref NativeArray<float3> inVertices, out Bounds retBounds)
        {
            if (inVertices.Length <= 0)
            {
                retBounds = default;
                retBounds.center = Vector3.zero;
                retBounds.size = Vector3.zero;
                return;
            }

            unsafe
            {
                float3* inVerticesPtr = (float3*)inVertices.GetUnsafePtr();
                int inVerticesLength = inVertices.Length;

                // Initialize with first vertex instead of infinity
                float3 first = inVerticesPtr[0];
                float minX = first.x;
                float minY = first.y;
                float maxX = first.x;
                float maxY = first.y;

                // Start from 1 since we used 0 for initialization
                for (int i = 1; i < inVerticesLength; i++)
                {
                    float3 v = inVerticesPtr[i];
                    minX = math.min(minX, v.x);
                    minY = math.min(minY, v.y);
                    maxX = math.max(maxX, v.x);
                    maxY = math.max(maxY, v.y);
                }

                retBounds = default;
                retBounds.SetMinMax(
                    new Vector3(minX, minY, 0),
                    new Vector3(maxX, maxY, 0));
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

#if USING_2DCOMMON
            int tessOutEdgeCount = 0;
            UnityEngine.U2D.Common.UTess.ModuleHandle.Tessellate(Allocator.Temp, tessInVertices, tessInEdges, ref tessOutVertices, out tessOutVertexCount, ref tessOutIndices, out tessOutIndexCount, ref tessOutEdges, out tessOutEdgeCount, false);
#endif

            int indexOffset = inIndices.Length;
            int vertexOffset = inVertices.Length;
            int totalOutVertices = tessOutVertexCount + inVertices.Length;
            int totalOutIndices = tessOutIndexCount + inIndices.Length;
            outVertices = new NativeArray<ShadowMeshVertex>(totalOutVertices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            outIndices = new NativeArray<int>(totalOutIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // Copy vertices using MemCpy for better performance
            unsafe
            {
                UnsafeUtility.MemCpy(
                    outVertices.GetUnsafePtr(),
                    inVertices.GetUnsafePtr(),
                    inVertices.Length * UnsafeUtility.SizeOf<ShadowMeshVertex>());
            }

            // Generate new vertices from tessellation
            for (int i = 0; i < tessOutVertexCount; i++)
            {
                float2 tessVertex = tessOutVertices[i];
                ShadowMeshVertex vertex = new ShadowMeshVertex(ProjectionType.ProjectionNone, tessVertex, float2.zero);
                outVertices[i + vertexOffset] = vertex;
            }

            // Copy indices using MemCpy
            unsafe
            {
                UnsafeUtility.MemCpy(
                    outIndices.GetUnsafePtr(),
                    inIndices.GetUnsafePtr(),
                    inIndices.Length * UnsafeUtility.SizeOf<int>());
            }

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

        // NON-BURST WRAPPER - Mesh operations are not Burst compatible
        static public void GenerateShadowMesh(ref Mesh mesh, NativeArray<ShadowMeshVertex> inVertices, NativeArray<int> inIndices)
        {
            if (mesh == null)
                mesh = new Mesh();

            if (inVertices.IsCreated && inIndices.IsCreated)
            {
                // Set the mesh data
                mesh.SetVertexBufferParams(inVertices.Length, m_VertexLayout);
                mesh.SetVertexBufferData<ShadowMeshVertex>(inVertices, 0, 0, inVertices.Length);
                mesh.SetIndexBufferParams(inIndices.Length, IndexFormat.UInt32);
                mesh.SetIndexBufferData<int>(inIndices, 0, 0, inIndices.Length);
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, inIndices.Length));
                mesh.subMeshCount = 1;
            }
            else
            {
                mesh.Clear();
            }
        }

        // BURST-COMPATIBLE internal function - Triangle version
        [BurstCompile]
        static void GenerateShadowGeometry_Internal_FromTriangles(
            ref NativeArray<float3> inVertices,
            ref NativeArray<ShadowEdge> inEdges,
            ref NativeArray<int> inShapeStartingEdge,
            bool fill,
            out NativeArray<ShadowMeshVertex> newOutVertices,
            out NativeArray<int> newOutIndices,
            out Bounds retLocalBound)
        {
            // Setup our buffers
            int meshVertexCount = inVertices.Length + k_AdditionalVerticesPerEdge * inEdges.Length;
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;

            NativeArray<float2> meshProjectionInfo = new NativeArray<float2>(meshVertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<ShadowMeshVertex> meshVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            CalculateProjectionInfo_FromTriangles(ref inVertices, ref inEdges, ref inShapeStartingEdge, ref meshProjectionInfo);
            CalculateVertices(ref inVertices, ref inEdges, ref meshProjectionInfo, ref meshVertices);
            CalculateShadowTriangles_FromTriangles(ref inVertices, ref inEdges, ref inShapeStartingEdge, ref meshIndices);

            NativeArray<ShadowMeshVertex> finalVertices;
            NativeArray<int> finalIndices;
            int fillSubmeshStartIndex = 0;
            int fillSubmeshIndexCount = 0;

            if (fill)
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

            newOutVertices = finalVertices;
            newOutIndices = finalIndices;

            meshProjectionInfo.Dispose();

            CalculateLocalBounds(ref inVertices, out retLocalBound);
        }

        // BURST-COMPATIBLE internal function - Line version
        [BurstCompile]
        static void GenerateShadowGeometry_Internal_FromLines(
            ref NativeArray<float3> inVertices,
            ref NativeArray<ShadowEdge> inEdges,
            ref NativeArray<int> inShapeStartingEdge,
            bool fill,
            out NativeArray<ShadowMeshVertex> newOutVertices,
            out NativeArray<int> newOutIndices,
            out Bounds retLocalBound)
        {
            // Setup our buffers
            int meshVertexCount = inVertices.Length + k_AdditionalVerticesPerEdge * inEdges.Length;
            int meshIndexCount = inEdges.Length * k_VerticesPerTriangle * k_TrianglesPerEdge;

            NativeArray<float2> meshProjectionInfo = new NativeArray<float2>(meshVertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> meshIndices = new NativeArray<int>(meshIndexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<ShadowMeshVertex> meshVertices = new NativeArray<ShadowMeshVertex>(meshVertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            CalculateProjectionInfo_FromLines(ref inVertices, ref inEdges, ref inShapeStartingEdge, ref meshProjectionInfo);
            CalculateVertices(ref inVertices, ref inEdges, ref meshProjectionInfo, ref meshVertices);
            CalculateShadowTriangles_FromLines(ref inVertices, ref inEdges, ref meshIndices);

            NativeArray<ShadowMeshVertex> finalVertices;
            NativeArray<int> finalIndices;
            int fillSubmeshStartIndex = 0;
            int fillSubmeshIndexCount = 0;

            if (fill)
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

            newOutVertices = finalVertices;
            newOutIndices = finalIndices;

            meshProjectionInfo.Dispose();

            CalculateLocalBounds(ref inVertices, out retLocalBound);
        }

        // NON-BURST WRAPPER - Handles disposal of old arrays - Triangle version
        static public Bounds GenerateShadowGeometry(
            ref NativeArray<ShadowMeshVertex> outVertices,
            ref NativeArray<int> outIndices,
            NativeArray<Vector3> inVertices,
            NativeArray<ShadowEdge> inEdges,
            NativeArray<int> inShapeStartingEdge,
            bool fill, ShadowShape2D.OutlineTopology topology)
        {
            // Dispose old arrays if they exist
            if (outIndices.IsCreated)
                outIndices.Dispose();
            if (outVertices.IsCreated)
                outVertices.Dispose();

            // Reinterpret Vector3 array as float3 (same memory layout)
            NativeArray<float3> inVerticesFloat3 = inVertices.Reinterpret<float3>();

            // Call Burst-compatible function
            NativeArray<ShadowMeshVertex> newOutVertices;
            NativeArray<int> newOutIndices;
            Bounds retLocalBound;

            if (topology == ShadowShape2D.OutlineTopology.Triangles)
            {
                GenerateShadowGeometry_Internal_FromTriangles(
                    ref inVerticesFloat3,
                    ref inEdges,
                    ref inShapeStartingEdge,
                    fill,
                    out newOutVertices,
                    out newOutIndices,
                    out retLocalBound);
            }
            else
            {
                GenerateShadowGeometry_Internal_FromLines(
                    ref inVerticesFloat3,
                    ref inEdges,
                    ref inShapeStartingEdge,
                    fill,
                    out newOutVertices,
                    out newOutIndices,
                    out retLocalBound);
            }

            // Assign new arrays to ref parameters
            outVertices = newOutVertices;
            outIndices = newOutIndices;

            return retLocalBound;
        }

        [BurstCompile]
        static public void CalculateEdgesFromLines(ref NativeArray<int> indices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            unsafe
            {
                int numOfEdges = indices.Length >> 1;
                NativeArray<int> tempShapeStartIndices = new NativeArray<int>(numOfEdges, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<bool> tempShapeIsClosedArray = new NativeArray<bool>(numOfEdges, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                int* indicesPtr = (int*)indices.GetUnsafePtr();
                int* tempShapeStartIndicesPtr = (int*)tempShapeStartIndices.GetUnsafePtr();
                bool* tempShapeIsClosedArrayPtr = (bool*)tempShapeIsClosedArray.GetUnsafePtr();

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
                outShapeStartingEdge = new NativeArray<int>(shapeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Use MemCpy for faster copy
                UnsafeUtility.MemCpy(
                    outShapeStartingEdge.GetUnsafePtr(),
                    tempShapeStartIndices.GetUnsafePtr(),
                    shapeCount * UnsafeUtility.SizeOf<int>());

                UnsafeUtility.MemCpy(
                    outShapeIsClosedArray.GetUnsafePtr(),
                    tempShapeIsClosedArray.GetUnsafePtr(),
                    shapeCount * UnsafeUtility.SizeOf<bool>());

                tempShapeStartIndices.Dispose();
                tempShapeIsClosedArray.Dispose();

                // Add edges
                outEdges = new NativeArray<ShadowEdge>(numOfEdges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.GetUnsafePtr();

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
        static internal void GetVertexReferenceStats(ref NativeArray<float3> vertices, ref NativeArray<ShadowEdge> edges, int vertexCount, out bool hasReusedVertices, out int newVertexCount, out NativeArray<RemappingInfo> remappingInfo)
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
                    if (remappingInfoPtr[v1].count == 0)
                    {
                        remappingInfoPtr[v1].count = 1;
                        newVertexCount++;
                    }
                }

                // Find the starts of the new indices
                int startPos = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    if (remappingInfoPtr[i].count > 0)
                    {
                        remappingInfoPtr[i].index = startPos;
                        startPos += remappingInfoPtr[i].count;
                    }
                }
            }
        }

        [BurstCompile]
        static public bool IsTriangleReversed(ref NativeArray<float3> vertices, int idx0, int idx1, int idx2)
        {
            float3 v0 = vertices[idx0];
            float3 v1 = vertices[idx1];
            float3 v2 = vertices[idx2];

            float twiceArea = (v0.x * v1.y + v1.x * v2.y + v2.x * v0.y) - (v0.y * v1.x + v1.y * v2.x + v2.y * v0.x);
            return math.sign(twiceArea) >= 0;
        }

        [BurstCompile]
        static public void CalculateEdgesFromTriangles(ref NativeArray<Vector3> vertices, ref NativeArray<int> indices, bool duplicatesVertices, out NativeArray<Vector3> newVertices, out NativeArray<ShadowEdge> outEdges, out NativeArray<int> outShapeStartingEdge, out NativeArray<bool> outShapeIsClosedArray)
        {
            unsafe
            {
                // Run clipper to calculate edges
                Clipper2D.Solution solution = new Clipper2D.Solution();
                Clipper2D.ExecuteArguments executeArguments = new Clipper2D.ExecuteArguments(Clipper2D.InitOptions.ioDefault, Clipper2D.ClipType.ctUnion);

                int triangleCount = indices.Length / 3;
                NativeArray<Vector2> points = new NativeArray<Vector2>(indices.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> pathSizes = new NativeArray<int>(triangleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                NativeArray<Clipper2D.PathArguments> pathArguments = new NativeArray<Clipper2D.PathArguments>(triangleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Pointers - cast between Vector3/float3 and Vector2/float2 since they have same layout
                float2* pointsPtr = (float2*)points.GetUnsafePtr();
                int* pathSizesPtr = (int*)pathSizes.GetUnsafePtr();
                Clipper2D.PathArguments* pathArgumentsPtr = (Clipper2D.PathArguments*)pathArguments.GetUnsafePtr();
                float3* verticesPtr = (float3*)vertices.GetUnsafePtr();

                // Copy input data for Clipper2D.Execute
                Clipper2D.PathArguments sharedPathArg = new Clipper2D.PathArguments(Clipper2D.PolyType.ptSubject, true);
                for (int i = 0; i < triangleCount; i++)
                {
                    pathSizesPtr[i] = 3;
                    pathArgumentsPtr[i] = sharedPathArg;

                    int pointOffset = 3 * i;
                    // Use .xy swizzle to extract float2 from float3
                    pointsPtr[pointOffset] = verticesPtr[indices[pointOffset]].xy;
                    pointsPtr[pointOffset + 1] = verticesPtr[indices[pointOffset + 1]].xy;
                    pointsPtr[pointOffset + 2] = verticesPtr[indices[pointOffset + 2]].xy;
                }

                Clipper2D.Execute(ref solution, points, pathSizes, pathArguments, executeArguments, Allocator.Persistent);

                // Cleanup execute inputs because we have necessary data in our solution
                points.Dispose();
                pathSizes.Dispose();
                pathArguments.Dispose();

                // Copy solution to outputs
                int pointLen = solution.points.Length;
                int shapeCount = solution.pathSizes.Length;
                newVertices = new NativeArray<Vector3>(pointLen, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                outEdges = new NativeArray<ShadowEdge>(pointLen, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                outShapeStartingEdge = new NativeArray<int>(shapeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                outShapeIsClosedArray = new NativeArray<bool>(shapeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                int* solutionPathSizesPtr = (int*)solution.pathSizes.GetUnsafePtr();
                float2* solutionPointsPtr = (float2*)solution.points.GetUnsafePtr();

                float3* newVerticesPtr = (float3*)newVertices.GetUnsafePtr();
                ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.GetUnsafePtr();
                int* outShapeStartingEdgePtr = (int*)outShapeStartingEdge.GetUnsafePtr();
                bool* outShapeIsClosedArrayPtr = (bool*)outShapeIsClosedArray.GetUnsafePtr();

                // Copy output data from the solution
                int nextStart = 0;
                for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
                {
                    int curStart = nextStart;
                    int curPathSize = solutionPathSizesPtr[shapeIndex];
                    outShapeStartingEdgePtr[shapeIndex] = nextStart;
                    nextStart += curPathSize;

                    int previousVertex = nextStart - 1;
                    for (int pointIndex = curStart; pointIndex < nextStart; pointIndex++)
                    {
                        // Convert float2 to float3 with z=0
                        newVerticesPtr[pointIndex] = new float3(solutionPointsPtr[pointIndex], 0);
                        outEdgesPtr[pointIndex] = new ShadowEdge(previousVertex, pointIndex);
                        previousVertex = pointIndex;
                    }

                    outShapeIsClosedArrayPtr[shapeIndex] = true;
                }

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
                int halfCount = count >> 1;

                for (int i = 0; i < halfCount; i++)
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

                // Handle odd count
                if ((count & 1) == 1)
                {
                    int edgeAIndex = startingIndex + halfCount;
                    ShadowEdge edgeA = inOutSortedEdges[edgeAIndex];
                    edgeA.Reverse();
                    inOutSortedEdges[edgeAIndex] = edgeA;
                }
            }
        }

        [BurstCompile]
        static int GetClosedPathCount(ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray)
        {
            int count = 0;
            for (int i = 0; i < inShapeStartingEdge.Length; i++)
            {
                if (inShapeStartingEdge[i] < 0)
                    break;

                count++;
            }

            return count;
        }

        [BurstCompile]
        static void GetPathInfo(ref NativeArray<ShadowEdge> inEdges, ref NativeArray<int> inShapeStartingEdge, ref NativeArray<bool> inShapeIsClosedArray, out int closedPathArrayCount, out int closedPathsCount, out int openPathArrayCount, out int openPathsCount)
        {
            closedPathArrayCount = 0;
            openPathArrayCount = 0;
            closedPathsCount = 0;
            openPathsCount = 0;

            for (int i = 0; i < inShapeStartingEdge.Length; i++)
            {
                // If this shape starting edge is invalid stop
                if (inShapeStartingEdge[i] < 0)
                    break;

                int start = inShapeStartingEdge[i];
                int end = (i < (inShapeStartingEdge.Length - 1)) && (inShapeStartingEdge[i + 1] != -1) ? inShapeStartingEdge[i + 1] : inEdges.Length;
                int edges = end - start;
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
                GetPathInfo(ref inEdges, ref inShapeStartingEdge, ref inShapeIsClosedArray, out closedPathArrayCount, out closedPathCount, out openPathArrayCount, out openPathCount);

                NativeArray<Clipper2D.PathArguments> clipperPathArguments = new NativeArray<Clipper2D.PathArguments>(closedPathCount, k_ClippingAllocator, NativeArrayOptions.ClearMemory);
                NativeArray<int> closedPathSizes = new NativeArray<int>(closedPathCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector2> closedPath = new NativeArray<Vector2>(closedPathArrayCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<int> openPathSizes = new NativeArray<int>(openPathCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector2> openPath = new NativeArray<Vector2>(openPathArrayCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);

                Clipper2D.PathArguments* clipperPathArgumentsPtr = (Clipper2D.PathArguments*)clipperPathArguments.GetUnsafePtr();
                int* closedPathSizesPtr = (int*)closedPathSizes.GetUnsafePtr();
                float2* closedPathPtr = (float2*)closedPath.GetUnsafePtr();
                int* openPathSizesPtr = (int*)openPathSizes.GetUnsafePtr();
                float2* openPathPtr = (float2*)openPath.GetUnsafePtr();

                int* inShapeStartingEdgePtr = (int*)inShapeStartingEdge.GetUnsafePtr();
                bool* inShapeIsClosedArrayPtr = (bool*)inShapeIsClosedArray.GetUnsafePtr();
                float3* inVerticesPtr = (float3*)inVertices.GetUnsafePtr();
                ShadowEdge* inEdgesPtr = (ShadowEdge*)inEdges.GetUnsafePtr();

                int inEdgesLength = inEdges.Length;

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

                    if (inShapeIsClosedArrayPtr[shapeStartIndex])
                    {
                        closedPathSizesPtr[closedPathSizesIndex] = numberOfEdges + 1;
                        clipperPathArgumentsPtr[closedPathSizesIndex] = new Clipper2D.PathArguments(Clipper2D.PolyType.ptSubject, true);
                        closedPathSizesIndex++;

                        for (int i = 0; i < numberOfEdges; i++)
                        {
                            // Use .xy swizzle on float3
                            closedPathPtr[closedPathArrayIndex++] = inVerticesPtr[inEdgesPtr[i + currentShapeStart].v0].xy;
                        }

                        closedPathPtr[closedPathArrayIndex++] = inVerticesPtr[inEdgesPtr[numberOfEdges + currentShapeStart - 1].v1].xy;
                    }
                    else
                    {
                        openPathSizesPtr[openPathSizesIndex++] = numberOfEdges + 1;
                        for (int i = 0; i < numberOfEdges; i++)
                        {
                            openPathPtr[openPathArrayIndex++] = inVerticesPtr[inEdgesPtr[i + currentShapeStart].v0].xy;
                        }

                        openPathPtr[openPathArrayIndex++] = inVerticesPtr[inEdgesPtr[numberOfEdges + currentShapeStart - 1].v1].xy;
                    }
                }

                NativeArray<Vector2> clipperOffsetPath = closedPath;
                NativeArray<int> clipperOffsetPathSizes = closedPathSizes;

                Clipper2D.Solution clipperSolution = new Clipper2D.Solution();

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

                    int solutionPathLens = offsetSolution.pathSizes.Length + openPathCount;
                    outVertices = new NativeArray<Vector3>(offsetSolution.points.Length + openPathArrayCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);
                    // Fix: openPathArrayCount is vertex count for open paths, but open paths have (vertices - 1) edges
                    // Closed paths from offsetSolution have edges == vertices, but open paths have edges = vertices - 1
                    outEdges = new NativeArray<ShadowEdge>(offsetSolution.points.Length + openPathArrayCount - openPathCount, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);
                    outShapeStartingEdge = new NativeArray<int>(solutionPathLens, k_ClippingAllocator, NativeArrayOptions.UninitializedMemory);

                    float3* outVerticesPtr = (float3*)outVertices.GetUnsafePtr();
                    ShadowEdge* outEdgesPtr = (ShadowEdge*)outEdges.GetUnsafePtr();
                    int* outShapeStartingEdgePtr = (int*)outShapeStartingEdge.GetUnsafePtr();

                    float2* offsetSolutionPointsPtr = (float2*)offsetSolution.points.GetUnsafePtr();
                    int offsetSolutionPointsLength = offsetSolution.points.Length;

                    int* offsetSolutionPathSizesPtr = (int*)offsetSolution.pathSizes.GetUnsafePtr();
                    int offsetSolutionPathSizesLength = offsetSolution.pathSizes.Length;

                    // Copy with float2 to float3 conversion
                    for (int i = 0; i < offsetSolutionPointsLength; i++)
                    {
                        outVerticesPtr[vertexPos++] = new float3(offsetSolutionPointsPtr[i], 0);
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

                    int pathStartIndex = offsetSolutionPathSizesLength;
                    start = vertexPos;

                    for (int i = 0; i < openPath.Length; i++)
                    {
                        outVerticesPtr[vertexPos++] = new float3(openPathPtr[i], 0);
                    }

                    for (int openPathIndex = 0; openPathIndex < openPathCount; openPathIndex++)
                    {
                        int pathSize = openPathSizesPtr[openPathIndex];
                        int end = start + pathSize;
                        outShapeStartingEdgePtr[pathStartIndex + openPathIndex] = start;

                        for (int shapeIndex = 0; shapeIndex < pathSize - 1; shapeIndex++)
                        {
                            ShadowEdge edge = new ShadowEdge(shapeIndex + start, shapeIndex + 1 + start);
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
