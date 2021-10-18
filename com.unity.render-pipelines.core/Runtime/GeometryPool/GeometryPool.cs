using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.VFX;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    public struct GeometryPoolDesc
    {
        public int vertexPoolByteSize;
        public int indexPoolByteSize;
        public int subMeshLookupPoolByteSize;
        public int subMeshEntryPoolByteSize;
        public int maxMeshes;

        public static GeometryPoolDesc NewDefault()
        {
            return new GeometryPoolDesc()
            {
                vertexPoolByteSize = 32 * 1024 * 1024, //32 mb
                indexPoolByteSize = 16 * 1024 * 1024, //16 mb
                subMeshLookupPoolByteSize = 3 * 1024 * 1024, // 3mb
                subMeshEntryPoolByteSize = 2 * 1024 * 1024, // 2mb
                maxMeshes = 4096
            };
        }
    }

    public struct GeometryPoolHandle
    {
        public int index;
        public static GeometryPoolHandle Invalid = new GeometryPoolHandle() { index = -1 };
        public bool valid => index != -1;
    }

    public class GeometryPool
    {
        private static class GeoPoolShaderIDs
        {
            public static readonly int _InputIndexBuffer = Shader.PropertyToID("_InputIndexBuffer");
            public static readonly int _InputIBCount = Shader.PropertyToID("_InputIBCount");
            public static readonly int _OutputIBOffset = Shader.PropertyToID("_OutputIBOffset");
            public static readonly int _GeoHandle = Shader.PropertyToID("_GeoHandle");
            public static readonly int _GeoVertexOffset = Shader.PropertyToID("_GeoVertexOffset");
            public static readonly int _GeoIndexOffset = Shader.PropertyToID("_GeoIndexOffset");
            public static readonly int _GeoSubMeshLookupOffset = Shader.PropertyToID("_GeoSubMeshLookupOffset");
            public static readonly int _GeoSubMeshEntryOffset = Shader.PropertyToID("_GeoSubMeshEntryOffset");
            public static readonly int _InputVBCount = Shader.PropertyToID("_InputVBCount");
            public static readonly int _OutputVBSize = Shader.PropertyToID("_OutputVBSize");
            public static readonly int _OutputVBOffset = Shader.PropertyToID("_OutputVBOffset");
            public static readonly int _InputPosBufferStride = Shader.PropertyToID("_InputPosBufferStride");
            public static readonly int _InputPosBufferOffset = Shader.PropertyToID("_InputPosBufferOffset");
            public static readonly int _InputUv0BufferStride = Shader.PropertyToID("_InputUv0BufferStride");
            public static readonly int _InputUv0BufferOffset = Shader.PropertyToID("_InputUv0BufferOffset");
            public static readonly int _InputUv1BufferStride = Shader.PropertyToID("_InputUv1BufferStride");
            public static readonly int _InputUv1BufferOffset = Shader.PropertyToID("_InputUv1BufferOffset");
            public static readonly int _InputNormalBufferStride = Shader.PropertyToID("_InputNormalBufferStride");
            public static readonly int _InputNormalBufferOffset = Shader.PropertyToID("_InputNormalBufferOffset");
            public static readonly int _InputTangentBufferStride = Shader.PropertyToID("_InputTangentBufferStride");
            public static readonly int _InputTangentBufferOffset = Shader.PropertyToID("_InputTangentBufferOffset");
            public static readonly int _InputFlags = Shader.PropertyToID("_InputFlags");
            public static readonly int _PosBuffer = Shader.PropertyToID("_PosBuffer");
            public static readonly int _Uv0Buffer = Shader.PropertyToID("_Uv0Buffer");
            public static readonly int _Uv1Buffer = Shader.PropertyToID("_Uv1Buffer");
            public static readonly int _NormalBuffer = Shader.PropertyToID("_NormalBuffer");
            public static readonly int _TangentBuffer = Shader.PropertyToID("_TangentBuffer");
            public static readonly int _OutputIndexBuffer = Shader.PropertyToID("_OutputIndexBuffer");
            public static readonly int _OutputVB = Shader.PropertyToID("_OutputVB");
            public static readonly int _OutputGeoMetadataBuffer = Shader.PropertyToID("_OutputGeoMetadataBuffer");
            public static readonly int _GeoPoolGlobalVertexBuffer = Shader.PropertyToID("_GeoPoolGlobalVertexBuffer");
            public static readonly int _GeoPoolGlobalIndexBuffer = Shader.PropertyToID("_GeoPoolGlobalIndexBuffer");
            public static readonly int _GeoPoolGlobalMetadataBuffer = Shader.PropertyToID("_GeoPoolGlobalMetadataBuffer");
            public static readonly int _GeoPoolGlobalParams = Shader.PropertyToID("_GeoPoolGlobalParams");
            public static readonly int _OutputSubMeshLookupBuffer = Shader.PropertyToID("_OutputSubMeshLookupBuffer");
            public static readonly int _OutputSubMeshEntryBuffer = Shader.PropertyToID("_OutputSubMeshEntryBuffer");
            public static readonly int _InputSubMeshIndexStart = Shader.PropertyToID("_InputSubMeshIndexStart");
            public static readonly int _InputSubMeshIndexCount = Shader.PropertyToID("_InputSubMeshIndexCount");
            public static readonly int _InputSubMeshBaseVertex = Shader.PropertyToID("_InputSubMeshBaseVertex");
            public static readonly int _InputSubMeshDestIndex = Shader.PropertyToID("_InputSubMeshDestIndex");
            public static readonly int _InputSubmeshLookupDestOffset = Shader.PropertyToID("_InputSubmeshLookupDestOffset");
            public static readonly int _InputSubmeshLookupBufferCount = Shader.PropertyToID("_InputSubmeshLookupBufferCount");
            public static readonly int _InputSubmeshLookupData = Shader.PropertyToID("_InputSubmeshLookupData");
            public static readonly int _ClearBuffer = Shader.PropertyToID("_ClearBuffer");
            public static readonly int _ClearBufferSize = Shader.PropertyToID("_ClearBufferSize");
            public static readonly int _ClearBufferOffset = Shader.PropertyToID("_ClearBufferOffset");
            public static readonly int _ClearBufferValue = Shader.PropertyToID("_ClearBufferValue");
        }

        private struct MeshSlot
        {
            public int refCount;
            public int meshHash;
            public GeometryPoolHandle geometryHandle;
        }

        private struct GeometrySlot
        {
            public BlockAllocator.Allocation vertexAlloc;
            public BlockAllocator.Allocation indexAlloc;
            public BlockAllocator.Allocation subMeshLookupAlloc;
            public BlockAllocator.Allocation subMeshEntryAlloc;

            public static GeometrySlot Invalid = new GeometrySlot()
            {
                vertexAlloc = BlockAllocator.Allocation.Invalid,
                indexAlloc = BlockAllocator.Allocation.Invalid,
                subMeshLookupAlloc = BlockAllocator.Allocation.Invalid,
                subMeshEntryAlloc = BlockAllocator.Allocation.Invalid
            };

            public bool valid => vertexAlloc.valid && indexAlloc.valid && subMeshLookupAlloc.valid && subMeshEntryAlloc.valid;
        }

        public static int GetVertexByteSize() => GeometryPoolConstants.GeoPoolVertexByteSize;
        public static int GetIndexByteSize() => GeometryPoolConstants.GeoPoolIndexByteSize;
        public static int GetSubMeshLookupByteSize() => 1; //1 byte.
        public static int GetSubMeshEntryByteSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolSubMeshEntry>();
        public static int GetGeoMetadataByteSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolMetadataEntry>();

        private int GetFormatByteCount(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32: return 4;
                case VertexAttributeFormat.Float16: return 2;
                case VertexAttributeFormat.UNorm8: return 1;
                case VertexAttributeFormat.SNorm8: return 1;
                case VertexAttributeFormat.UNorm16: return 2;
                case VertexAttributeFormat.SNorm16: return 2;
                case VertexAttributeFormat.UInt8: return 1;
                case VertexAttributeFormat.SInt8: return 1;
                case VertexAttributeFormat.UInt16: return 2;
                case VertexAttributeFormat.SInt16: return 2;
                case VertexAttributeFormat.UInt32: return 4;
                case VertexAttributeFormat.SInt32: return 4;
            }
            return 4;
        }

        private static int DivUp(int x, int y) => (x + y - 1) / y;

        GeometryPoolDesc m_Desc;

        public Mesh globalMesh = null;
        public GraphicsBuffer globalIndexBuffer { get { return m_GlobalIndexBuffer; } }
        public ComputeBuffer globalVertexBuffer { get { return m_GlobalVertexBuffer; } }
        public ComputeBuffer globalSubMeshLookupBuffer { get { return m_GlobalSubMeshLookupBuffer; } }
        public ComputeBuffer globalSubMeshEntryBuffer { get { return m_GlobalSubMeshEntryBuffer; } }
        public ComputeBuffer globalMetadataBuffer { get { return m_GlobalGeoMetadataBuffer; } }

        public int maxMeshes => m_Desc.maxMeshes;
        public int indicesCount => m_MaxIndexCounts;
        public int verticesCount => m_MaxVertCounts;
        public int subMeshLookupCount => m_MaxSubMeshLookupCounts;
        public int subMeshEntryCount => m_MaxSubMeshEntryCounts;

        private GraphicsBuffer m_GlobalIndexBuffer = null;
        private ComputeBuffer m_GlobalVertexBuffer = null;
        private ComputeBuffer m_GlobalSubMeshLookupBuffer = null;
        private ComputeBuffer m_GlobalSubMeshEntryBuffer = null;
        private ComputeBuffer m_GlobalGeoMetadataBuffer = null;

        private int m_MaxVertCounts;
        private int m_MaxIndexCounts;
        private int m_MaxSubMeshLookupCounts;
        private int m_MaxSubMeshEntryCounts;

        private BlockAllocator m_VertexAllocator;
        private BlockAllocator m_IndexAllocator;
        private BlockAllocator m_SubMeshLookupAllocator;
        private BlockAllocator m_SubMeshEntryAllocator;

        private NativeHashMap<int, MeshSlot> m_MeshSlots;
        private NativeList<GeometrySlot> m_GeoSlots;
        private NativeList<GeometryPoolHandle> m_FreeGeoSlots;

        private List<GraphicsBuffer> m_InputBufferReferences;

        private int m_UsedGeoSlots;

        private ComputeShader m_GeometryPoolKernelsCS;
        private int m_KernelMainUpdateIndexBuffer16;
        private int m_KernelMainUpdateIndexBuffer32;
        private int m_KernelMainUpdateVertexBuffer;
        private int m_KernelMainUpdateSubMeshData;
        private int m_KernelMainUpdateMeshMetadata;
        private int m_KernelMainClearSubMeshData;

        private CommandBuffer m_CmdBuffer;
        private bool m_MustClearCmdBuffer;
        private int m_PendingCmds;

        public GeometryPool(in GeometryPoolDesc desc)
        {
            LoadShaders();

            m_CmdBuffer = new CommandBuffer();
            m_InputBufferReferences = new List<GraphicsBuffer>();
            m_MustClearCmdBuffer = false;
            m_PendingCmds = 0;

            m_Desc = desc;
            m_MaxVertCounts = CalcVertexCount();
            m_MaxIndexCounts = CalcIndexCount();
            m_MaxSubMeshLookupCounts = CalcSubMeshLookupCount();
            m_MaxSubMeshEntryCounts = CalcSubMeshEntryCount();
            m_UsedGeoSlots = 0;

            m_GlobalVertexBuffer = new ComputeBuffer(DivUp(m_MaxVertCounts * GetVertexByteSize(), 4), 4, ComputeBufferType.Raw);

            globalMesh = new Mesh();
            globalMesh.indexBufferTarget = GraphicsBuffer.Target.Raw;
            globalMesh.SetIndexBufferParams(m_MaxIndexCounts, IndexFormat.UInt32);
            globalMesh.subMeshCount = desc.maxMeshes;
            globalMesh.vertices = new Vector3[1];
            globalMesh.UploadMeshData(false);
            m_GlobalIndexBuffer = globalMesh.GetIndexBuffer();

            m_GlobalSubMeshLookupBuffer = new ComputeBuffer(DivUp(m_MaxSubMeshLookupCounts * GetSubMeshLookupByteSize(), 4), 4, ComputeBufferType.Raw);
            m_GlobalSubMeshEntryBuffer = new ComputeBuffer(m_MaxSubMeshEntryCounts, GetSubMeshEntryByteSize(), ComputeBufferType.Structured);
            m_GlobalGeoMetadataBuffer = new ComputeBuffer(m_Desc.maxMeshes, GetGeoMetadataByteSize(), ComputeBufferType.Structured);

            Assertions.Assert.IsTrue(m_GlobalIndexBuffer != null);
            Assertions.Assert.IsTrue((m_GlobalIndexBuffer.target & GraphicsBuffer.Target.Raw) != 0);

            m_MeshSlots = new NativeHashMap<int, MeshSlot>(desc.maxMeshes, Allocator.Persistent);
            m_GeoSlots = new NativeList<GeometrySlot>(Allocator.Persistent);
            m_FreeGeoSlots = new NativeList<GeometryPoolHandle>(Allocator.Persistent);

            m_VertexAllocator = new BlockAllocator();
            m_VertexAllocator.Initialize(m_MaxVertCounts);

            m_IndexAllocator = new BlockAllocator();
            m_IndexAllocator.Initialize(m_MaxIndexCounts);

            m_SubMeshLookupAllocator = new BlockAllocator();
            m_SubMeshLookupAllocator.Initialize(m_MaxSubMeshLookupCounts);

            m_SubMeshEntryAllocator = new BlockAllocator();
            m_SubMeshEntryAllocator.Initialize(m_MaxSubMeshEntryCounts);
        }

        public void DisposeInputBuffers()
        {
            if (m_InputBufferReferences.Count == 0)
                return;

            foreach (var b in m_InputBufferReferences)
                b.Dispose();
            m_InputBufferReferences.Clear();
        }

        public void Dispose()
        {
            m_IndexAllocator.Dispose();
            m_VertexAllocator.Dispose();
            m_SubMeshLookupAllocator.Dispose();
            m_SubMeshEntryAllocator.Dispose();

            m_FreeGeoSlots.Dispose();
            m_GeoSlots.Dispose();
            m_MeshSlots.Dispose();

            m_GlobalIndexBuffer.Dispose();
            m_GlobalVertexBuffer.Release();
            m_GlobalSubMeshLookupBuffer.Dispose();
            m_GlobalSubMeshEntryBuffer.Dispose();
            m_GlobalGeoMetadataBuffer.Dispose();
            m_CmdBuffer.Release();

            CoreUtils.Destroy(globalMesh);
            globalMesh = null;
            DisposeInputBuffers();
        }

        private void LoadShaders()
        {
            m_GeometryPoolKernelsCS = (ComputeShader)Resources.Load("GeometryPoolKernels");

            m_KernelMainUpdateIndexBuffer16 = m_GeometryPoolKernelsCS.FindKernel("MainUpdateIndexBuffer16");
            m_KernelMainUpdateIndexBuffer32 = m_GeometryPoolKernelsCS.FindKernel("MainUpdateIndexBuffer32");
            m_KernelMainUpdateVertexBuffer = m_GeometryPoolKernelsCS.FindKernel("MainUpdateVertexBuffer");
            m_KernelMainUpdateSubMeshData = m_GeometryPoolKernelsCS.FindKernel("MainUpdateSubMeshData");
            m_KernelMainUpdateMeshMetadata = m_GeometryPoolKernelsCS.FindKernel("MainUpdateMeshMetadata");
            m_KernelMainClearSubMeshData = m_GeometryPoolKernelsCS.FindKernel("MainClearBuffer");
        }

        private int CalcVertexCount() => DivUp(m_Desc.vertexPoolByteSize, GetVertexByteSize());
        private int CalcIndexCount() => DivUp(m_Desc.indexPoolByteSize, GetIndexByteSize());
        private int CalcSubMeshLookupCount() => DivUp(m_Desc.subMeshLookupPoolByteSize, GetSubMeshLookupByteSize());
        private int CalcSubMeshEntryCount() => DivUp(m_Desc.subMeshEntryPoolByteSize, GetSubMeshEntryByteSize());

        private void DeallocateSlot(ref GeometrySlot slot)
        {
            if (slot.vertexAlloc.valid)
                m_VertexAllocator.FreeAllocation(slot.vertexAlloc);

            if (slot.indexAlloc.valid)
                m_IndexAllocator.FreeAllocation(slot.indexAlloc);

            if (slot.subMeshLookupAlloc.valid)
                m_SubMeshLookupAllocator.FreeAllocation(slot.subMeshLookupAlloc);

            if (slot.subMeshEntryAlloc.valid)
                m_SubMeshEntryAllocator.FreeAllocation(slot.subMeshEntryAlloc);

            slot = GeometrySlot.Invalid;
        }

        private bool AllocateGeo(int vertexCount, int indexCount, int subMeshEntries, out GeometryPoolHandle outHandle)
        {
            var newSlot = GeometrySlot.Invalid;

            bool allocationSuccess = true;

            if ((m_UsedGeoSlots + 1) > m_Desc.maxMeshes)
            {
                allocationSuccess = false;
                outHandle = GeometryPoolHandle.Invalid;
                return false;
            }

            if (allocationSuccess)
            {
                newSlot.vertexAlloc = m_VertexAllocator.Allocate(vertexCount);
                allocationSuccess = newSlot.vertexAlloc.valid;
            }

            if (allocationSuccess)
            {
                newSlot.indexAlloc = m_IndexAllocator.Allocate(indexCount);
                allocationSuccess = newSlot.indexAlloc.valid;
            }

            if (allocationSuccess)
            {
                newSlot.subMeshLookupAlloc = m_SubMeshLookupAllocator.Allocate(DivUp(indexCount, 3));
                allocationSuccess = newSlot.subMeshLookupAlloc.valid;
            }

            if (allocationSuccess)
            {
                newSlot.subMeshEntryAlloc = m_SubMeshEntryAllocator.Allocate(subMeshEntries);
                allocationSuccess = newSlot.subMeshEntryAlloc.valid;
            }

            if (!allocationSuccess)
            {
                DeallocateSlot(ref newSlot);
                outHandle = GeometryPoolHandle.Invalid;
                return false;
            }

            if (m_FreeGeoSlots.IsEmpty)
            {
                outHandle.index = m_GeoSlots.Length;
                m_GeoSlots.Add(newSlot);
            }
            else
            {
                outHandle = m_FreeGeoSlots[m_FreeGeoSlots.Length - 1];
                m_FreeGeoSlots.RemoveAtSwapBack(m_FreeGeoSlots.Length - 1);
                Assertions.Assert.IsTrue(!m_GeoSlots[outHandle.index].valid);
                m_GeoSlots[outHandle.index] = newSlot;
            }

            ++m_UsedGeoSlots;
            var descriptor = new SubMeshDescriptor();
            descriptor.baseVertex = 0;
            descriptor.firstVertex = 0;
            descriptor.indexCount = newSlot.indexAlloc.block.count;
            descriptor.indexStart = newSlot.indexAlloc.block.offset;
            descriptor.topology = MeshTopology.Triangles;
            descriptor.vertexCount = 1;
            globalMesh.SetSubMesh(outHandle.index, descriptor, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
            return true;
        }

        private void DeallocateGeo(GeometryPoolHandle handle)
        {
            if (!handle.valid)
                throw new System.Exception("Cannot free invalid geo pool handle");

            --m_UsedGeoSlots;
            m_FreeGeoSlots.Add(handle);
            GeometrySlot slot = m_GeoSlots[handle.index];
            DeallocateSlot(ref slot);
            m_GeoSlots[handle.index] = slot;
        }

        private void UpdateGeoGpuState(Mesh mesh, GeometryPoolHandle handle)
        {
            var geoSlot = m_GeoSlots[handle.index];
            CommandBuffer cmdBuffer = AllocateCommandBuffer(); //clear any previous cmd buffers.

            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            //Update index buffer
            GraphicsBuffer buffer = LoadIndexBuffer(cmdBuffer, mesh, out var indexBufferFormat);
            Assertions.Assert.IsTrue((buffer.target & GraphicsBuffer.Target.Raw) != 0);
            AddIndexUpdateCommand(
                cmdBuffer,
                indexBufferFormat, buffer, geoSlot.indexAlloc, m_GlobalIndexBuffer);

            //Update vertex buffer
            GraphicsBuffer posBuffer = LoadVertexAttribInfo(mesh, VertexAttribute.Position, out int posStride, out int posOffset, out int _);
            Assertions.Assert.IsTrue(posBuffer != null);
            Assertions.Assert.IsTrue((posBuffer.target & GraphicsBuffer.Target.Raw) != 0);

            GraphicsBuffer uvBuffer = LoadVertexAttribInfo(mesh, VertexAttribute.TexCoord0, out int uvStride, out int uvOffset, out int _);
            Assertions.Assert.IsTrue(uvBuffer != null);
            Assertions.Assert.IsTrue((uvBuffer.target & GraphicsBuffer.Target.Raw) != 0);

            GraphicsBuffer uv1Buffer = LoadVertexAttribInfo(mesh, VertexAttribute.TexCoord1, out int uv1Stride, out int uv1Offset, out int _);
            if (uv1Buffer != null)
                Assertions.Assert.IsTrue((uv1Buffer.target & GraphicsBuffer.Target.Raw) != 0);

            GraphicsBuffer nBuffer = LoadVertexAttribInfo(mesh, VertexAttribute.Normal, out int nStride, out int nOffset, out int _);
            Assertions.Assert.IsTrue(nBuffer != null);
            Assertions.Assert.IsTrue((nBuffer.target & GraphicsBuffer.Target.Raw) != 0);

            GraphicsBuffer tBuffer = LoadVertexAttribInfo(mesh, VertexAttribute.Tangent, out int tStride, out int tOffset, out int _);
            if (tBuffer != null)
                Assertions.Assert.IsTrue(tBuffer != null);

            AddVertexUpdateCommand(
                cmdBuffer, posBuffer, uvBuffer, uv1Buffer, nBuffer, tBuffer,
                posStride, posOffset, uvStride, uvOffset, uv1Stride, uv1Offset, nStride, nOffset, tStride, tOffset,
                geoSlot.vertexAlloc, m_GlobalVertexBuffer);

            {
                AddClearSubMeshDataBuffer(
                    cmdBuffer,
                    geoSlot.subMeshLookupAlloc.block.offset / 4,
                    geoSlot.subMeshLookupAlloc.block.count / 4,
                    0,
                    m_GlobalSubMeshLookupBuffer);

                int subMeshIndexOffset = 0;
                for (int subMeshId = 0; subMeshId < mesh.subMeshCount; ++subMeshId)
                {
                    SubMeshDescriptor submeshDescriptor = mesh.GetSubMesh(subMeshId);
                    //Update submeshLookup
                    AddSubMeshDataUpdateCommand(
                        cmdBuffer,
                        subMeshId,
                        subMeshIndexOffset,
                        submeshDescriptor,
                        geoSlot.subMeshLookupAlloc,
                        geoSlot.subMeshEntryAlloc,
                        m_GlobalSubMeshLookupBuffer,
                        m_GlobalSubMeshEntryBuffer);

                    subMeshIndexOffset += submeshDescriptor.indexCount;
                    Assertions.Assert.IsTrue((subMeshIndexOffset / 3) <= geoSlot.subMeshLookupAlloc.block.count);
                }
            }

            //Update metadata buffer
            AddMetadataUpdateCommand(
                cmdBuffer, handle.index,
                new GeoPoolMetadataEntry()
                {
                    vertexOffset = geoSlot.vertexAlloc.block.offset,
                    indexOffset = geoSlot.indexAlloc.block.offset,
                    subMeshLookupOffset = geoSlot.subMeshLookupAlloc.block.offset,
                    subMeshEntryOffset = geoSlot.subMeshEntryAlloc.block.offset
                },
                m_GlobalGeoMetadataBuffer);
        }

        public bool Register(Mesh mesh, out GeometryPoolHandle outHandle)
        {
            int meshHashCode = mesh.GetHashCode();
            Assertions.Assert.IsTrue(meshHashCode != -1);
            if (m_MeshSlots.TryGetValue(meshHashCode, out MeshSlot meshSlot))
            {
                Assertions.Assert.IsTrue(meshHashCode == meshSlot.meshHash);
                ++meshSlot.refCount;
                m_MeshSlots[meshSlot.meshHash] = meshSlot;
                outHandle = meshSlot.geometryHandle;
                return true;
            }
            else
            {
                var newSlot = new MeshSlot()
                {
                    refCount = 1,
                    meshHash = meshHashCode,
                };

                int indexCount = 0;
                for (int i = 0; i < (int)mesh.subMeshCount; ++i)
                    indexCount += (int)mesh.GetIndexCount(i);

                if (!AllocateGeo(mesh.vertexCount, indexCount, mesh.subMeshCount, out outHandle))
                    return false;

                newSlot.geometryHandle = outHandle;
                if (!m_MeshSlots.TryAdd(meshHashCode, newSlot))
                {
                    //revert the allocation.
                    DeallocateGeo(outHandle);
                    outHandle = GeometryPoolHandle.Invalid;
                    return false;
                }

                UpdateGeoGpuState(mesh, outHandle);

                return true;
            }
        }

        public void Unregister(Mesh mesh)
        {
            int meshHashCode = mesh.GetHashCode();
            if (!m_MeshSlots.TryGetValue(meshHashCode, out MeshSlot outSlot))
                return;

            --outSlot.refCount;
            if (outSlot.refCount == 0)
            {
                m_MeshSlots.Remove(meshHashCode);
                DeallocateGeo(outSlot.geometryHandle);
            }
            else
                m_MeshSlots[meshHashCode] = outSlot;
        }

        public GeometryPoolHandle GetHandle(Mesh mesh)
        {
            int meshHashCode = mesh.GetHashCode();
            if (!m_MeshSlots.TryGetValue(meshHashCode, out MeshSlot outSlot))
                return GeometryPoolHandle.Invalid;

            return outSlot.geometryHandle;
        }

        public void SendGpuCommands()
        {
            if (m_PendingCmds != 0)
            {
                Graphics.ExecuteCommandBuffer(m_CmdBuffer);
                m_MustClearCmdBuffer = true;
                m_PendingCmds = 0;
            }

            DisposeInputBuffers();
        }

        public BlockAllocator.Allocation GetIndexBufferBlock(GeometryPoolHandle handle)
        {
            if (handle.index < 0 || handle.index >= m_GeoSlots.Length)
                throw new System.Exception("Handle utilized is invalid");

            return m_GeoSlots[handle.index].indexAlloc;
        }

        public BlockAllocator.Allocation GetVertexBufferBlock(GeometryPoolHandle handle)
        {
            if (handle.index < 0 || handle.index >= m_GeoSlots.Length)
                throw new System.Exception("Handle utilized is invalid");

            return m_GeoSlots[handle.index].vertexAlloc;
        }

        public BlockAllocator.Allocation GetSubMeshLookupBlock(GeometryPoolHandle handle)
        {
            if (handle.index < 0 || handle.index >= m_GeoSlots.Length)
                throw new System.Exception("Handle utilized is invalid");

            return m_GeoSlots[handle.index].subMeshLookupAlloc;
        }

        public BlockAllocator.Allocation GetSubMeshEntryBlock(GeometryPoolHandle handle)
        {
            if (handle.index < 0 || handle.index >= m_GeoSlots.Length)
                throw new System.Exception("Handle utilized is invalid");

            return m_GeoSlots[handle.index].subMeshEntryAlloc;
        }

        private GraphicsBuffer LoadIndexBuffer(CommandBuffer cmdBuffer, Mesh mesh, out IndexFormat fmt)
        {
            if ((mesh.indexBufferTarget & GraphicsBuffer.Target.Raw) != 0)
            {
                fmt = mesh.indexFormat;
                var idxBuffer = mesh.GetIndexBuffer();
                m_InputBufferReferences.Add(idxBuffer);
                return idxBuffer;
            }
            else
            {
                fmt = IndexFormat.UInt32;

                int indexCount = 0;
                for (int i = 0; i < (int)mesh.subMeshCount; ++i)
                    indexCount += (int)mesh.GetIndexCount(i);

                var idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Raw, indexCount, 4);
                m_InputBufferReferences.Add(idxBuffer);

                int indexOffset = 0;

                for (int i = 0; i < (int)mesh.subMeshCount; ++i)
                {
                    int currentIndexCount = (int)mesh.GetIndexCount(i);
                    cmdBuffer.SetBufferData(idxBuffer, mesh.GetIndices(i), 0, indexOffset, currentIndexCount);
                    indexOffset += currentIndexCount;
                }

                return idxBuffer;
            }
        }

        GraphicsBuffer LoadVertexAttribInfo(Mesh mesh, VertexAttribute attribute, out int streamStride, out int attributeOffset, out int attributeBytes)
        {
            if (!mesh.HasVertexAttribute(attribute))
            {
                streamStride = attributeOffset = attributeBytes = 0;
                return null;
            }

            int stream = mesh.GetVertexAttributeStream(attribute);
            streamStride = mesh.GetVertexBufferStride(stream);
            attributeOffset = mesh.GetVertexAttributeOffset(attribute);
            attributeBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(attribute)) * mesh.GetVertexAttributeDimension(attribute);

            var gb = mesh.GetVertexBuffer(stream);
            m_InputBufferReferences.Add(gb);
            return gb;
        }


        private CommandBuffer AllocateCommandBuffer()
        {
            if (m_MustClearCmdBuffer)
            {
                m_CmdBuffer.Clear();
                m_MustClearCmdBuffer = false;
            }

            ++m_PendingCmds;
            return m_CmdBuffer;
        }

        private void AddMetadataUpdateCommand(
            CommandBuffer cmdBuffer,
            int geoHandleIndex,
            in GeoPoolMetadataEntry metadataEntry, ComputeBuffer outputMetadataBuffer)
        {
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoHandle, geoHandleIndex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoVertexOffset, metadataEntry.vertexOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoIndexOffset, metadataEntry.indexOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoSubMeshLookupOffset, metadataEntry.subMeshLookupOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoSubMeshEntryOffset, metadataEntry.subMeshEntryOffset);

            int kernel = m_KernelMainUpdateMeshMetadata;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputGeoMetadataBuffer, outputMetadataBuffer);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, 1, 1, 1);
        }

        private void AddIndexUpdateCommand(
            CommandBuffer cmdBuffer,
            IndexFormat inputFormat, in GraphicsBuffer inputBuffer, in BlockAllocator.Allocation location, GraphicsBuffer outputIdxBuffer)
        {
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputIBCount, location.block.count);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._OutputIBOffset, location.block.offset);
            int kernel = inputFormat == IndexFormat.UInt16 ? m_KernelMainUpdateIndexBuffer16 : m_KernelMainUpdateIndexBuffer32;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._InputIndexBuffer, inputBuffer);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputIndexBuffer, outputIdxBuffer);
            int groupCountsX = DivUp(location.block.count, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private void AddVertexUpdateCommand(
            CommandBuffer cmdBuffer,
            in GraphicsBuffer p, in GraphicsBuffer uv0, in GraphicsBuffer uv1, in GraphicsBuffer n, in GraphicsBuffer t,
            int posStride, int posOffset, int uv0Stride, int uv0Offset, int uv1Stride, int uv1Offset, int normalStride, int normalOffset, int tangentStride, int tangentOffset,
            in BlockAllocator.Allocation location,
            ComputeBuffer outputVertexBuffer)
        {

            GeoPoolInputFlags flags =
                (uv1 != null ? GeoPoolInputFlags.HasUV1 : GeoPoolInputFlags.None)
              | (t != null ? GeoPoolInputFlags.HasTangent : GeoPoolInputFlags.None);

            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputVBCount, location.block.count);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._OutputVBSize, m_MaxVertCounts);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._OutputVBOffset, location.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputPosBufferStride, posStride);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputPosBufferOffset, posOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputUv0BufferStride, uv0Stride);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputUv0BufferOffset, uv0Offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputUv1BufferStride, uv1Stride);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputUv1BufferOffset, uv1Offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputNormalBufferStride, normalStride);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputNormalBufferOffset, normalOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputTangentBufferStride, tangentStride);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputTangentBufferOffset, tangentOffset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputFlags, (int)flags);

            int kernel = m_KernelMainUpdateVertexBuffer;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._PosBuffer, p);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._Uv0Buffer, uv0);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._Uv1Buffer, uv1 != null ? t : p); /*unity always wants something set*/
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._NormalBuffer, n);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._TangentBuffer, t != null ? t : p);/*unity always wants something set*/

            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputVB, outputVertexBuffer);

            int groupCountsX = DivUp(location.block.count, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private void AddClearSubMeshDataBuffer(
            CommandBuffer cmdBuffer,
            int offset,
            int size,
            int clearVal,
            ComputeBuffer outputBuffer)
        {
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._ClearBufferSize, size);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._ClearBufferOffset, offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._ClearBufferValue, clearVal);
            int kernel = m_KernelMainClearSubMeshData;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._ClearBuffer, outputBuffer);
            int groupCountsX = DivUp(size, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private void AddSubMeshDataUpdateCommand(
            CommandBuffer cmdBuffer,
            int descriptorIndex,
            int indexOffset,
            SubMeshDescriptor submeshDescriptor,
            in BlockAllocator.Allocation lookupAllocation,
            in BlockAllocator.Allocation entryAllocation,
            ComputeBuffer outputLookupBuffer,
            ComputeBuffer outputEntryBuffer)
        {
            int lookupCounts = submeshDescriptor.indexCount / 3;
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshIndexStart, submeshDescriptor.indexStart);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshIndexCount, submeshDescriptor.indexCount);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshBaseVertex, submeshDescriptor.baseVertex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshDestIndex, entryAllocation.block.offset + descriptorIndex);

            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupDestOffset, indexOffset / 3 + lookupAllocation.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupBufferCount, lookupCounts);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupData, descriptorIndex);

            int kernel = m_KernelMainUpdateSubMeshData;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputSubMeshLookupBuffer, outputLookupBuffer);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputSubMeshEntryBuffer, outputEntryBuffer);

            int groupCountsX = DivUp(lookupCounts, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private Vector4 GetPackedGeoPoolParam0()
        {
            return new Vector4(m_MaxVertCounts, 0.0f, 0.0f, 0.0f);
        }

        public void BindResources(CommandBuffer cmdBuffer, ComputeShader cs, int kernel)
        {
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalVertexBuffer, globalVertexBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalIndexBuffer, globalIndexBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            cmdBuffer.SetComputeVectorParam(cs, GeoPoolShaderIDs._GeoPoolGlobalParams, GetPackedGeoPoolParam0());
        }

        public void BindResources(Material material)
        {
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalVertexBuffer, globalVertexBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalIndexBuffer, globalIndexBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            material.SetVector(GeoPoolShaderIDs._GeoPoolGlobalParams, GetPackedGeoPoolParam0());
        }
    }

}
