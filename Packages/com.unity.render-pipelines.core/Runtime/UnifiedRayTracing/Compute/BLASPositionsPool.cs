using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RadeonRays;
using UnityEngine.UIElements;

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
                // Capping capacity to 2,147,483,647 vertices because m_VerticesAllocator is using NativeList which has int max capacity
                int maxCapacity = (int)math.min((long)Int32.MaxValue, GraphicsHelpers.MaxGraphicsBufferSizeInBytes / (long)UnsafeUtility.SizeOf<float3>());

                if (!m_VerticesAllocator.GetExpectedGrowthToFitAllocation((int)info.vertexCount, maxCapacity, out int newCapacity))
                    throw new UnifiedRayTracingException($"VerticesAllocator can't grow to {maxCapacity } elements", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                if (!GraphicsHelpers.ReallocateBuffer(m_CopyShader, oldCapacity, newCapacity, UnsafeUtility.SizeOf<float3>(), ref m_VerticesBuffer))
                    throw new UnifiedRayTracingException($"Failed to allocate buffer of size: {newCapacity * UnsafeUtility.SizeOf<float3>()} bytes", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                verticesAllocation = m_VerticesAllocator.GrowAndAllocate((int)info.vertexCount, maxCapacity, out oldCapacity, out newCapacity);
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


