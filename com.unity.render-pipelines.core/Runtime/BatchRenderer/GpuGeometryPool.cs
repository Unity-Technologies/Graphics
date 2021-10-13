using System;
using static Unity.Mathematics.math;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    public struct GpuGeometryPool
    {
        public const int DefaultVertexBufferMemory = 8; //32 mb for vertices
        public const int DefaultIndexBufferMemory = 8 * 1024 * 1024; //8mb for index buffers
        public const int DefaultMeshSlots = 1024;

        private const int VertElementSize = 4; //4 bytes, a single float TODO: pack 16 bit floats? 
        private const int VertElementCount = 3; //3 elements per vertex
        private const int VertexByteSize = VertElementSize /*bytes per float*/ * VertElementCount /* number of elements*/; 
        private const int IndexByteSize = 4 /*32 bit indices. TODO: pack 16 bits indices.*/;

        private ComputeBuffer m_VertexGPUBuffer;
        private ComputeBuffer m_IndexGPUBuffer;

        private int m_VertexPoolCapacity;
        private int m_IndexPoolCapacity;
        private int m_MeshSlotsCapacity;

        private int m_CurrentVertexSize;
        private int m_CurrentIndexSize;
        private int m_CurrentMeshSize;

        CommandBuffer m_GPUCmdBuffer;
        bool m_ShouldClearCmdBuffer;

        private ComputeShader m_CopyGeoBufferCS;
        private int m_CopyGeoBufferKernel;

        private void Reset()
        {
            if (m_VertexPoolCapacity != 0)
                m_VertexGPUBuffer.Dispose();
            if (m_IndexPoolCapacity != 0)
                m_IndexGPUBuffer.Dispose();

            m_ShouldClearCmdBuffer = true;
            m_CurrentVertexSize = 0;
            m_CurrentIndexSize = 0;
            m_VertexPoolCapacity = 0;
            m_IndexPoolCapacity = 0;
            m_MeshSlotsCapacity = 0;
        }

        private void ReserveGPUSpace(int vertexPoolCapacity, int indexPoolCapacity, int meshSlots)
        {
            if (m_ShouldClearCmdBuffer)
            { 
                m_GPUCmdBuffer.Clear();
                m_ShouldClearCmdBuffer = false;
            }

            bool shouldSubmit = false;
            ComputeBuffer disposedVertexBuffer = null;
            ComputeBuffer disposeIndexBuffer = null;

            if (vertexPoolCapacity > m_VertexPoolCapacity)
            {
                var newBuffer = new ComputeBuffer((vertexPoolCapacity * VertexByteSize + 3) / 4, 4, ComputeBufferType.Raw);

                if (m_CurrentVertexSize != 0)
                {
                    CopyBuffers(m_GPUCmdBuffer, m_VertexGPUBuffer, newBuffer, 0, 0, m_CurrentVertexSize * VertElementCount);
                    shouldSubmit = true;
                    disposedVertexBuffer = m_VertexGPUBuffer;
                }

                m_VertexPoolCapacity = vertexPoolCapacity;
                m_VertexGPUBuffer = newBuffer;
            }

            if (indexPoolCapacity > m_IndexPoolCapacity)
            {
                var newBuffer = new ComputeBuffer((indexPoolCapacity * IndexByteSize + 3) / 4, 4, ComputeBufferType.Raw);
                
                if (m_CurrentIndexSize != 0)
                {
                    CopyBuffers(m_GPUCmdBuffer, m_IndexGPUBuffer, newBuffer, 0, 0, m_CurrentIndexSize);
                    shouldSubmit = true;
                    disposeIndexBuffer = m_IndexGPUBuffer;
                }

                m_IndexPoolCapacity = indexPoolCapacity;
                m_IndexGPUBuffer = newBuffer;
            }

            if (meshSlots > m_MeshSlotsCapacity)
            {
                m_MeshSlotsCapacity = meshSlots;
            }

            if (shouldSubmit)
            {
                m_ShouldClearCmdBuffer = true;
                Graphics.ExecuteCommandBuffer(m_GPUCmdBuffer);
            }

            if (disposedVertexBuffer != null)
                disposedVertexBuffer.Dispose();

            if (disposeIndexBuffer != null)
                disposeIndexBuffer.Dispose();
        }

        private void LoadShaders()
        {
            m_CopyGeoBufferCS = (ComputeShader)Resources.Load("CopyGeoBuffer");
            m_CopyGeoBufferKernel = m_CopyGeoBufferCS.FindKernel("CopyRawBuffer");
        }

        public void Initialize(
            int vertexBufferMemory = DefaultVertexBufferMemory,
            int indexBufferMemory = DefaultIndexBufferMemory,
            int meshSlots = DefaultMeshSlots)
        {
            LoadShaders();
            
            m_GPUCmdBuffer = new CommandBuffer();
            m_VertexPoolCapacity = 0;
            m_IndexPoolCapacity = 0;
            Reset();
            int vertexPoolCapacity = (vertexBufferMemory + VertexByteSize - 1)/ VertexByteSize;
            int indexPoolCapacity = (indexBufferMemory + IndexByteSize - 1) / IndexByteSize;
            ReserveGPUSpace(vertexPoolCapacity, indexPoolCapacity, meshSlots);
        }

        public int RegisterMesh(Mesh mesh)
        {
            int newVertSize = m_CurrentVertexSize + mesh.vertexCount;

            int currentIndexCount = 0;
            for (int subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx)
                currentIndexCount += (int)mesh.GetIndexCount(subMeshIdx);

            int newIndexSize = m_CurrentIndexSize + currentIndexCount;
            int newMeshSize = m_CurrentMeshSize + 1;

            ReserveGPUSpace(newVertSize, newIndexSize, newMeshSize);

            //TODO: Fix the allocation.
            int vertexOffset = m_CurrentVertexSize;
            int indexOffset = m_CurrentIndexSize;
            int meshIndex = m_CurrentMeshSize;

            //TODO: do this copy on the GPU
            m_VertexGPUBuffer.SetData(mesh.vertices, 0, vertexOffset, mesh.vertexCount);
            int submeshIndexOffset = indexOffset;
            for (int subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx)
            {
                int subMeshCount = (int)mesh.GetIndexCount(subMeshIdx);
                //TODO: do this copy on the GPU
                m_IndexGPUBuffer.SetData(mesh.GetIndices(subMeshIdx), 0, submeshIndexOffset, subMeshCount);
                submeshIndexOffset += subMeshCount;
            }

            m_CurrentVertexSize = newVertSize;
            m_CurrentIndexSize = newIndexSize;
            m_CurrentMeshSize = newMeshSize;

            return newMeshSize;
        }

        //TODO: make this a cbuffer
        static int _InputOffset = Shader.PropertyToID("_InputOffset");
        static int _OutputOffset = Shader.PropertyToID("_OutputOffset");
        static int _ItemCounts = Shader.PropertyToID("_ItemCounts");
        static int _InputBuffer = Shader.PropertyToID("_InputBuffer");
        static int _OutputBuffer = Shader.PropertyToID("_OutputBuffer");
        private void CopyBuffers(CommandBuffer cmd, ComputeBuffer source, ComputeBuffer dest, int inputOffset, int outputOffset, int elementCounts)
        {
            
            cmd.SetComputeIntParam(m_CopyGeoBufferCS, _InputOffset, inputOffset);
            cmd.SetComputeIntParam(m_CopyGeoBufferCS, _OutputOffset, outputOffset);
            cmd.SetComputeIntParam(m_CopyGeoBufferCS, _ItemCounts, elementCounts);
            cmd.SetComputeBufferParam(m_CopyGeoBufferCS, m_CopyGeoBufferKernel, _InputBuffer, source);
            cmd.SetComputeBufferParam(m_CopyGeoBufferCS, m_CopyGeoBufferKernel, _OutputBuffer, dest);
            cmd.DispatchCompute(m_CopyGeoBufferCS, m_CopyGeoBufferKernel, (elementCounts + 63)/64, 1, 1);
        }

        public void Dispose()
        {
            Reset();
            m_GPUCmdBuffer.Dispose();
            m_CopyGeoBufferCS = null;
        }
    }
        
}
