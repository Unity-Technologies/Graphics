using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RadeonRays;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal struct VertexBufferChunk
    {
        public GraphicsBuffer vertices;
        public int verticesStartOffset; // in DWORD
        public uint vertexCount;
        public uint vertexStride; // in DWORD
        public int baseVertex;
    }

    internal sealed class BLASPositionsPool : IDisposable
    {
        public BLASPositionsPool(ComputeShader copyPositionsShader, ComputeShader copyShader)
        {
            m_VerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, intialVertexCount * VertexSizeInDwords, 4);
            m_VerticesAllocator = new BlockAllocator();
            m_VerticesAllocator.Initialize(intialVertexCount);

            m_CopyPositionsShader = copyPositionsShader;
            m_CopyVerticesKernel = m_CopyPositionsShader.FindKernel("CopyVertexBuffer");
            m_CopyShader = copyShader;
        }

        public void Dispose()
        {
            m_VerticesBuffer.Dispose();
            m_VerticesAllocator.Dispose();
        }

        public const int VertexSizeInDwords = 3;

        public GraphicsBuffer VertexBuffer { get { return m_VerticesBuffer; } }

        public void Clear()
        {
            m_VerticesBuffer.Dispose();
            m_VerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, intialVertexCount * VertexSizeInDwords, 4);
            m_VerticesAllocator.Dispose();
            m_VerticesAllocator = new BlockAllocator();
            m_VerticesAllocator.Initialize(intialVertexCount);
        }

        const int intialVertexCount = 1000;

        GraphicsBuffer m_VerticesBuffer;
        BlockAllocator m_VerticesAllocator;
        readonly ComputeShader m_CopyPositionsShader;
        readonly int m_CopyVerticesKernel;
        readonly ComputeShader m_CopyShader;
        const uint kItemsPerWorkgroup = 48u * 128u;

        public void Add(VertexBufferChunk info, out BlockAllocator.Allocation verticesAllocation)
        {
            verticesAllocation = m_VerticesAllocator.Allocate((int)info.vertexCount);
            if (!verticesAllocation.valid)
            {
                int oldCapacity = m_VerticesAllocator.capacity;

                if (!m_VerticesAllocator.GetExpectedGrowthToFitAllocation((int)info.vertexCount, GraphicsHelpers.MaxGraphicsBufferSizeInBytes / UnsafeUtility.SizeOf<float3>(), out int newCapacity))
                    throw new UnifiedRayTracingException("Can't allocate a GraphicsBuffer bigger than 2GB", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                if (!GraphicsHelpers.ReallocateBuffer(m_CopyShader, oldCapacity, newCapacity, UnsafeUtility.SizeOf<float3>(), ref m_VerticesBuffer))
                    throw new UnifiedRayTracingException($"Failed to allocate buffer of size: {newCapacity * UnsafeUtility.SizeOf<float3>()} bytes", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                verticesAllocation = m_VerticesAllocator.GrowAndAllocate((int)info.vertexCount, GraphicsHelpers.MaxGraphicsBufferSizeInBytes / UnsafeUtility.SizeOf<float3>(), out oldCapacity, out newCapacity);
                Debug.Assert(verticesAllocation.valid);
            }

            var cmd = new CommandBuffer();
            cmd.SetComputeIntParam(m_CopyPositionsShader, "_InputPosBufferCount", (int)info.vertexCount);
            cmd.SetComputeIntParam(m_CopyPositionsShader, "_InputPosBufferOffset", info.verticesStartOffset);
            cmd.SetComputeIntParam(m_CopyPositionsShader, "_InputBaseVertex", info.baseVertex);
            cmd.SetComputeIntParam(m_CopyPositionsShader, "_InputPosBufferStride", (int)info.vertexStride);
            cmd.SetComputeIntParam(m_CopyPositionsShader, "_OutputPosBufferOffset", verticesAllocation.block.offset * VertexSizeInDwords);
            cmd.SetComputeBufferParam(m_CopyPositionsShader, m_CopyVerticesKernel, "_InputPosBuffer", info.vertices);
            cmd.SetComputeBufferParam(m_CopyPositionsShader, m_CopyVerticesKernel, "_OutputPosBuffer", m_VerticesBuffer);
            cmd.DispatchCompute(m_CopyPositionsShader, m_CopyVerticesKernel, (int)Common.CeilDivide(info.vertexCount, kItemsPerWorkgroup), 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
        }

        public void Remove(ref BlockAllocator.Allocation verticesAllocation)
        {
            m_VerticesAllocator.FreeAllocation(verticesAllocation);

            verticesAllocation = BlockAllocator.Allocation.Invalid;
        }
    }
}


