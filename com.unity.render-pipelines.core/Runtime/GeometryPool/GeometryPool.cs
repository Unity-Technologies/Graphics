using System;
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
        public int batchInstancePoolByteSize;
        public int clusterPoolByteSize;
        public int maxMeshes;


        public static GeometryPoolDesc NewDefault()
        {
            return new GeometryPoolDesc()
            {
                vertexPoolByteSize = 32 * 1024 * 1024, //32 mb
                indexPoolByteSize = 16 * 1024 * 1024, //16 mb
                subMeshLookupPoolByteSize = 3 * 1024 * 1024, // 3mb
                subMeshEntryPoolByteSize = 2 * 1024 * 1024, // 2mb
                batchInstancePoolByteSize = 4 * 1024 * 1024, //4 mb
                clusterPoolByteSize = 16 * 1024 * 1024, //16mb
                maxMeshes = 4096
            };
        }
    }

    public struct GeometryPoolHandle : IEquatable<GeometryPoolHandle>
    {
        public int index;
        public static GeometryPoolHandle Invalid = new GeometryPoolHandle() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(GeometryPoolHandle other) => index == other.index;
    }

    public struct GeometryPoolBatchHandle : IEquatable<GeometryPoolBatchHandle>
    {
        public int index;
        public static GeometryPoolBatchHandle Invalid = new GeometryPoolBatchHandle() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(GeometryPoolBatchHandle other) => index == other.index;
    }

    public struct GeometryPoolEntryInfo
    {
        public bool valid;
        public int refCount;
        public NativeArray<int> materialHashes;

        public static GeometryPoolEntryInfo NewDefault()
        {
            return new GeometryPoolEntryInfo()
            {
                valid = false,
                refCount = 0
            };
        }
    }

    public struct GeometryPoolSubmeshData
    {
        public int submeshIndex;
        public Material material;
    }

    public struct GeometryPoolEntryDesc
    {
        public Mesh mesh;
        public GeometryPoolSubmeshData[] submeshData;
    }

    public struct GeometryPoolMaterialEntry
    {
        public uint materialGPUKey;
        public int refCount;
        public Material material;

        public static GeometryPoolMaterialEntry NewDefault()
        {
            return new GeometryPoolMaterialEntry()
            {
                materialGPUKey = 0,
                refCount = 0,
                material = null
            };
        }
    }

    public struct GeometryPoolBatchInstanceBuffer
    {
        public NativeArray<short> instanceValues;
        public bool valid => instanceValues.IsCreated;
        public void Dispose()
        {
            if (valid)
                instanceValues.Dispose();
        }
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
            public static readonly int _GeoSubMeshEntryOffset_VertexFlags = Shader.PropertyToID("_GeoSubMeshEntryOffset_VertexFlags");
            public static readonly int _InputSubMeshMaterialKey = Shader.PropertyToID("_InputSubMeshMaterialKey");
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
            public static readonly int _GeoPoolGlobalSubMeshLookupBuffer = Shader.PropertyToID("_GeoPoolGlobalSubMeshLookupBuffer");
            public static readonly int _GeoPoolGlobalSubMeshEntryBuffer = Shader.PropertyToID("_GeoPoolGlobalSubMeshEntryBuffer");
            public static readonly int _GeoPoolGlobalMetadataBuffer = Shader.PropertyToID("_GeoPoolGlobalMetadataBuffer");
            public static readonly int _GeoPoolGlobalBatchTableBuffer = Shader.PropertyToID("_GeoPoolGlobalBatchTableBuffer");
            public static readonly int _GeoPoolGlobalClusterEntriesBuffer = Shader.PropertyToID("_GeoPoolGlobalClusterEntriesBuffer");
            public static readonly int _GeoPoolMeshEntriesBuffer = Shader.PropertyToID("_GeoPoolMeshEntriesBuffer");
            public static readonly int _GeoPoolGlobalBatchInstanceBuffer = Shader.PropertyToID("_GeoPoolGlobalBatchInstanceBuffer");
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
            public static readonly int _InputClusterCounts = Shader.PropertyToID("_InputClusterCounts");
            public static readonly int _InputClusterBaseOffset = Shader.PropertyToID("_InputClusterBaseOffset");
            public static readonly int _InputMaterialKey = Shader.PropertyToID("_InputMaterialKey");
            public static readonly int _InputIndexBufferBaseOffset = Shader.PropertyToID("_InputIndexBufferBaseOffset");
            public static readonly int _InputVertexBufferBaseOffset = Shader.PropertyToID("_InputVertexBufferBaseOffset");
            public static readonly int _InputIndexBufferCounts = Shader.PropertyToID("_InputIndexBufferCounts");
            public static readonly int _OutputClusterBuffer = Shader.PropertyToID("_OutputClusterBuffer");
            public static readonly int _InputClusterBufferIndex = Shader.PropertyToID("_InputClusterBufferIndex");
            public static readonly int _InputClusterBufferCounts = Shader.PropertyToID("_InputClusterBufferCounts");
            public static readonly int _InputMeshVertFlags = Shader.PropertyToID("_InputMeshVertFlags");
            public static readonly int _OutputMeshEntries = Shader.PropertyToID("_OutputMeshEntries");
        }

        private struct MeshSlot
        {
            public int refCount;
            public uint meshHash;
        }

        private struct GeometrySlot
        {
            public BlockAllocator.Allocation vertexAlloc;
            public BlockAllocator.Allocation indexAlloc;
            public BlockAllocator.Allocation subMeshLookupAlloc;
            public BlockAllocator.Allocation subMeshEntryAlloc;
            public BlockAllocator.Allocation clusterBufferAlloc;

            public static GeometrySlot Invalid = new GeometrySlot()
            {
                vertexAlloc = BlockAllocator.Allocation.Invalid,
                indexAlloc = BlockAllocator.Allocation.Invalid,
                subMeshLookupAlloc = BlockAllocator.Allocation.Invalid,
                subMeshEntryAlloc = BlockAllocator.Allocation.Invalid,
                clusterBufferAlloc = BlockAllocator.Allocation.Invalid
            };

            public bool valid => vertexAlloc.valid && indexAlloc.valid && subMeshLookupAlloc.valid && subMeshEntryAlloc.valid;
        }

        public static int GetVertexByteSize() => GeometryPoolConstants.GeoPoolVertexByteSize;
        public static int GetIndexByteSize() => GeometryPoolConstants.GeoPoolIndexByteSize;
        public static int GetSubMeshLookupByteSize() => 1; //1 byte.
        public static int GetSubMeshEntryByteSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolSubMeshEntry>();
        public static int GetGeoMetadataByteSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolMetadataEntry>();
        public static int GetGeoBatchInstancesInfoByteSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolBatchTableEntry>();
        public static int GetGeoBatchInstancesDataByteSize() => GeometryPoolConstants.GeoPoolBatchInstanceDataByteSize;

        public static int GetMeshEntryBufferSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolMeshEntry>();
        public static int GetClusterEntryBufferSize() => System.Runtime.InteropServices.Marshal.SizeOf<GeoPoolClusterEntry>();

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
        public ComputeBuffer globalBatchTableBuffer { get { return m_GlobalBatchTableBuffer; } }
        public ComputeBuffer globalBatchInstanceBuffer { get { return m_GlobalBatchInstanceBuffer; } }
        public ComputeBuffer globalClusterEntryBuffer { get { return m_GlobalClusterEntryBuffer; } }
        public ComputeBuffer globalMeshEntryBuffer { get { return m_GlobalMeshEntryBuffer; } }
        public Dictionary<int, GeometryPoolMaterialEntry> globalMaterialEntries { get { return m_MaterialEntries; } }

        public int maxMeshes => m_Desc.maxMeshes;
        public int indicesCount => m_MaxIndexCounts;
        public int verticesCount => m_MaxVertCounts;
        public int subMeshLookupCount => m_MaxSubMeshLookupCounts;
        public int subMeshEntryCount => m_MaxSubMeshEntryCounts;
        public int maxBatchCount => m_MaxBatchCount;
        public int maxBatchInstanceCount => m_MaxBatchInstanceCount;
        public int maxClusterEntryCount => m_MaxPoolClusterEntryCounts;

        private GraphicsBuffer m_GlobalIndexBuffer = null;
        private ComputeBuffer m_GlobalVertexBuffer = null;
        private ComputeBuffer m_GlobalSubMeshLookupBuffer = null;
        private ComputeBuffer m_GlobalSubMeshEntryBuffer = null;
        private ComputeBuffer m_GlobalGeoMetadataBuffer = null;
        private ComputeBuffer m_GlobalBatchTableBuffer = null;
        private ComputeBuffer m_GlobalBatchInstanceBuffer = null;
        private ComputeBuffer m_GlobalClusterEntryBuffer = null;
        private ComputeBuffer m_GlobalMeshEntryBuffer = null;

        private int m_MaxVertCounts;
        private int m_MaxIndexCounts;
        private int m_MaxSubMeshLookupCounts;
        private int m_MaxSubMeshEntryCounts;
        private int m_MaxBatchCount;
        private int m_MaxBatchInstanceCount;
        private int m_MaxPoolClusterEntryCounts;

        private BlockAllocator m_VertexAllocator;
        private BlockAllocator m_IndexAllocator;
        private BlockAllocator m_SubMeshLookupAllocator;
        private BlockAllocator m_SubMeshEntryAllocator;
        private BlockAllocator m_ClusterEntryAllocator;

        private NativeHashMap<GeometryPoolHandle, MeshSlot> m_MeshSlots;
        private Dictionary<GeometryPoolHandle, NativeArray<int>> m_MaterialHashes;
        private NativeHashMap<uint, GeometryPoolHandle> m_MeshHashToHandle;
        private NativeList<GeometrySlot> m_GeoSlots;
        private NativeList<GeometryPoolHandle> m_FreeGeoSlots;
        private Dictionary<int, GeometryPoolMaterialEntry> m_MaterialEntries;

        private List<GraphicsBuffer> m_InputBufferReferences;

        private int m_UsedGeoSlots;

        private ComputeShader m_GeometryPoolKernelsCS;
        private int m_KernelMainUpdateIndexBuffer16;
        private int m_KernelMainUpdateIndexBuffer32;
        private int m_KernelMainUpdateVertexBuffer;
        private int m_KernelMainUpdateSubMeshData;
        private int m_KernelMainUpdateMeshMetadata;
        private int m_KernelMainClearSubMeshData;
        private int m_KernelMainUpdateSequentialClusterData;
        private int m_KernelMainUpdateMeshEntry;

        private CommandBuffer m_CmdBuffer;
        private bool m_MustClearCmdBuffer;
        private int m_PendingCmds;

        private uint m_NextMaterialGPUKey;

        private NativeArray<GeoPoolBatchTableEntry> m_BatchTable;
        private NativeArray<BlockAllocator.Allocation> m_BatchTableAllocations;
        private BlockAllocator m_BatchInstancesAllocator;

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
            m_MaxPoolClusterEntryCounts = DivUp(m_Desc.clusterPoolByteSize, GetClusterEntryBufferSize());
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
            m_GlobalMeshEntryBuffer = new ComputeBuffer(maxMeshes, GetMeshEntryBufferSize(), ComputeBufferType.Structured);
            m_GlobalClusterEntryBuffer = new ComputeBuffer(m_MaxPoolClusterEntryCounts, GetClusterEntryBufferSize(), ComputeBufferType.Structured);

            m_MaxBatchCount = 256; //up to 256 batches, 8 bits per batch index.
            m_GlobalBatchTableBuffer = new ComputeBuffer(m_MaxBatchCount, GetGeoBatchInstancesInfoByteSize(), ComputeBufferType.Structured);
            m_BatchTable = new NativeArray<GeoPoolBatchTableEntry>(m_MaxBatchCount, Allocator.Persistent);
            m_BatchTableAllocations = new NativeArray<BlockAllocator.Allocation>(m_MaxBatchCount, Allocator.Persistent);
            for (int i = 0; i < m_MaxBatchCount; ++i)
                m_BatchTableAllocations[i] = BlockAllocator.Allocation.Invalid;

            m_MaxBatchInstanceCount = DivUp(m_Desc.batchInstancePoolByteSize, GetGeoBatchInstancesDataByteSize());
            m_BatchInstancesAllocator = new BlockAllocator();
            m_BatchInstancesAllocator.Initialize(m_MaxBatchInstanceCount);
            m_GlobalBatchInstanceBuffer = new ComputeBuffer(DivUp(m_MaxBatchInstanceCount * GetGeoBatchInstancesDataByteSize(), 4), 4, ComputeBufferType.Raw);


            Assertions.Assert.IsTrue(m_GlobalIndexBuffer != null);
            Assertions.Assert.IsTrue((m_GlobalIndexBuffer.target & GraphicsBuffer.Target.Raw) != 0);

            m_MeshSlots = new NativeHashMap<GeometryPoolHandle, MeshSlot>(desc.maxMeshes, Allocator.Persistent);
            m_MaterialHashes = new Dictionary<GeometryPoolHandle, NativeArray<int>>();
            m_MeshHashToHandle = new NativeHashMap<uint, GeometryPoolHandle>(desc.maxMeshes, Allocator.Persistent);

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

            m_ClusterEntryAllocator = new BlockAllocator();
            m_ClusterEntryAllocator.Initialize(m_MaxPoolClusterEntryCounts);

            m_MaterialEntries = new Dictionary<int, GeometryPoolMaterialEntry>();
            m_NextMaterialGPUKey = 0x1;
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
            m_ClusterEntryAllocator.Dispose();

            m_BatchTable.Dispose();
            m_BatchTableAllocations.Dispose();
            m_BatchInstancesAllocator.Dispose();

            m_FreeGeoSlots.Dispose();
            m_GeoSlots.Dispose();
            m_MeshSlots.Dispose();
            foreach (var p in m_MaterialHashes)
            {
                if (p.Value.IsCreated)
                    p.Value.Dispose();
            }
            m_MaterialHashes = null;
            m_MeshHashToHandle.Dispose();

            m_GlobalIndexBuffer.Dispose();
            m_GlobalVertexBuffer.Release();
            m_GlobalSubMeshLookupBuffer.Dispose();
            m_GlobalSubMeshEntryBuffer.Dispose();
            m_GlobalGeoMetadataBuffer.Dispose();
            m_GlobalBatchTableBuffer.Dispose();
            m_GlobalBatchInstanceBuffer.Dispose();
            m_CmdBuffer.Release();

            m_GlobalMeshEntryBuffer.Dispose();
            m_GlobalClusterEntryBuffer.Dispose();

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
            m_KernelMainUpdateSequentialClusterData = m_GeometryPoolKernelsCS.FindKernel("MainUpdateSequentialClusterData");
            m_KernelMainUpdateMeshEntry = m_GeometryPoolKernelsCS.FindKernel("MainUpdateMeshEntry");
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

            if (slot.clusterBufferAlloc.valid)
                m_ClusterEntryAllocator.FreeAllocation(slot.clusterBufferAlloc);

            slot = GeometrySlot.Invalid;
        }

        private bool AllocateGeo(int vertexCount, int indexCount, int subMeshEntries, int clusterCounts, out GeometryPoolHandle outHandle)
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

            if (allocationSuccess)
            {
                newSlot.clusterBufferAlloc = m_ClusterEntryAllocator.Allocate(clusterCounts);
                allocationSuccess = newSlot.clusterBufferAlloc.valid;
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

        public GeometryPoolEntryInfo GetEntryInfo(GeometryPoolHandle handle)
        {
            if (m_MeshSlots.TryGetValue(handle, out MeshSlot slot))
            {
                NativeArray<int> materialHashes;
                m_MaterialHashes.TryGetValue(handle, out materialHashes);
                return new GeometryPoolEntryInfo()
                {
                    valid = true,
                    refCount = slot.refCount,
                    materialHashes = materialHashes
                };
            }

            return GeometryPoolEntryInfo.NewDefault();
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

        private void UpdateGeoGpuState(Mesh mesh, NativeArray<uint> materialKeys, GeometryPoolHandle handle)
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

            GeoPoolInputFlags vertexFlags = GeoPoolInputFlags.None;
            AddVertexUpdateCommand(
                cmdBuffer, posBuffer, uvBuffer, uv1Buffer, nBuffer, tBuffer,
                posStride, posOffset, uvStride, uvOffset, uv1Stride, uv1Offset, nStride, nOffset, tStride, tOffset,
                geoSlot.vertexAlloc, out vertexFlags, m_GlobalVertexBuffer);

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
                    uint materialKey = materialKeys.IsCreated && subMeshId < materialKeys.Length ? materialKeys[subMeshId] : 0;
                    SubMeshDescriptor submeshDescriptor = mesh.GetSubMesh(subMeshId);
                    //Update submeshLookup
                    AddSubMeshDataUpdateCommand(
                        cmdBuffer,
                        subMeshId,
                        subMeshIndexOffset,
                        submeshDescriptor,
                        materialKey,
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
                PackGpuGeoPoolMetadataEntry(geoSlot, vertexFlags), m_GlobalGeoMetadataBuffer);

            {
                var baseVertexAlloc = geoSlot.vertexAlloc;
                var baseIndexAlloc = geoSlot.indexAlloc;
                var baseClusterAlloc = geoSlot.clusterBufferAlloc;
                int subMeshIndexOffset = 0;
                int clusterOffset = 0;
                for (int subMeshId = 0; subMeshId < mesh.subMeshCount; ++subMeshId)
                {
                    SubMeshDescriptor submeshDescriptor = mesh.GetSubMesh(subMeshId);

                    int clusterCounts = DivUp(submeshDescriptor.indexCount / 3, GeometryPoolConstants.GeoPoolClusterPrimitiveCount);
                    uint materialKey = materialKeys.IsCreated && subMeshId < materialKeys.Length ? materialKeys[subMeshId] : 0;

                    var submeshIndexAllocation = baseIndexAlloc;
                    submeshIndexAllocation.block.offset = baseIndexAlloc.block.offset + subMeshIndexOffset;
                    submeshIndexAllocation.block.count = submeshDescriptor.indexCount;

                    var submeshClusterAllocation = baseClusterAlloc;
                    submeshClusterAllocation.block.offset = baseClusterAlloc.block.offset + clusterOffset;
                    submeshClusterAllocation.block.count = clusterCounts;

                    AddSequentialClustersInformation(
                        cmdBuffer,
                        submeshClusterAllocation,
                        baseVertexAlloc,
                        submeshIndexAllocation,
                        materialKey,
                        m_GlobalClusterEntryBuffer);

                    subMeshIndexOffset += submeshDescriptor.indexCount;
                    clusterOffset += clusterCounts;
                    Assertions.Assert.IsTrue((subMeshIndexOffset / 3) <= geoSlot.subMeshLookupAlloc.block.count);
                    Assertions.Assert.IsTrue(clusterOffset <= geoSlot.clusterBufferAlloc.block.count);
                }
            }

            //Update mesh entry buffer
            AddMeshEntryUpdateCommand(
                cmdBuffer, handle.index,
                PackGpuGeoPoolMeshEntry(geoSlot, vertexFlags), m_GlobalMeshEntryBuffer);
        }

        private GeoPoolMetadataEntry PackGpuGeoPoolMetadataEntry(in GeometrySlot geoSlot, GeoPoolInputFlags vertexFlags)
        {
            return new GeoPoolMetadataEntry()
            {
                vertexOffset = geoSlot.vertexAlloc.block.offset,
                indexOffset = geoSlot.indexAlloc.block.offset,
                subMeshLookupOffset = geoSlot.subMeshLookupAlloc.block.offset,
                subMeshEntryOffset_VertexFlags = (int)((geoSlot.subMeshEntryAlloc.block.offset << 16) | ((int)vertexFlags & 0xFFFF))
            };
        }

        private GeoPoolMeshEntry PackGpuGeoPoolMeshEntry(in GeometrySlot geoSlot, GeoPoolInputFlags vertexFlags)
        {
            return new GeoPoolMeshEntry()
            {
                clustersBufferIndex = (int)geoSlot.clusterBufferAlloc.block.offset,
                clustersCounts = (int)geoSlot.clusterBufferAlloc.block.count,
                vertexFlags = (int)vertexFlags
            };
        }

        private uint FNVHash(uint prevHash, uint dword)
        {
            //https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
            const uint fnvPrime = 0x811C9DC5;
            prevHash *= fnvPrime;
            prevHash ^= ((dword >> 0) & 0xFF);
            prevHash ^= ((dword >> 8) & 0xFF);
            prevHash ^= ((dword >> 16) & 0xFF);
            prevHash ^= ((dword >> 24) & 0xFF);
            return prevHash;
        }

        private uint CalculateEntryHash(in GeometryPoolEntryDesc entryDesc)
        {
            uint meshHashCode = 0;
            meshHashCode = FNVHash(meshHashCode, (uint)entryDesc.mesh.GetHashCode());
            if (entryDesc.submeshData != null)
            {
                foreach (var data in entryDesc.submeshData)
                {
                    meshHashCode = FNVHash(meshHashCode, (uint)data.submeshIndex);
                    meshHashCode = FNVHash(meshHashCode, (uint)data.material.GetHashCode());
                }
            }
            return meshHashCode;
        }

        public bool CreateBatch(int maxInstanceBatchCount, out GeometryPoolBatchHandle outBatchHandle)
        {
            outBatchHandle = GeometryPoolBatchHandle.Invalid;

            //align to 4 dwords at minimum
            int alignedInstanceBatchCount = DivUp(maxInstanceBatchCount * GetGeoBatchInstancesDataByteSize(), 4) * GeometryPoolConstants.GeoPoolBatchInstancesPerDword;

            //find free batch
            for (int bi = 0; bi < m_BatchTable.Length; ++bi)
            {
                if (!m_BatchTableAllocations[bi].valid)
                {
                    var allocation = m_BatchInstancesAllocator.Allocate(alignedInstanceBatchCount);
                    if (!allocation.valid)
                        return false;

                    m_BatchTableAllocations[bi] = allocation;
                    m_BatchTable[bi] = new GeoPoolBatchTableEntry()
                    {
                        offset = allocation.block.offset,
                        count = allocation.block.count
                    };
                    outBatchHandle = new GeometryPoolBatchHandle()
                    {
                        index = bi
                    };

                    CommandBuffer cmdBuffer = AllocateCommandBuffer();
                    cmdBuffer.SetBufferData(m_GlobalBatchTableBuffer, m_BatchTable, bi, bi, 1);

                    return true;
                }
            }

            return false;
        }

        public GeometryPoolBatchInstanceBuffer CreateGeometryPoolBatchInstanceBuffer(GeometryPoolBatchHandle batchHandle, bool isPersistant = false)
        {
            if (!batchHandle.valid)
                return new GeometryPoolBatchInstanceBuffer();

            GeoPoolBatchTableEntry entry = m_BatchTable[batchHandle.index];

            return new GeometryPoolBatchInstanceBuffer()
            {
                instanceValues = new NativeArray<short>(entry.count, isPersistant ? Allocator.Persistent : Allocator.Temp)
            };
        }

        public void SetBatchInstanceData(GeometryPoolBatchHandle batchHandle, GeometryPoolBatchInstanceBuffer data)
        {
            if (!batchHandle.valid)
                return;

            GeoPoolBatchTableEntry entry = m_BatchTable[batchHandle.index];
            Assertions.Assert.IsTrue(data.instanceValues.Length <= entry.count);

            CommandBuffer cmdBuffer = AllocateCommandBuffer();
            cmdBuffer.SetBufferData(m_GlobalBatchInstanceBuffer, data.instanceValues, 0, entry.offset, data.instanceValues.Length);
        }

        public void DestroyBatch(GeometryPoolBatchHandle handle)
        {
            if (!handle.valid)
                return;

            m_BatchInstancesAllocator.FreeAllocation((m_BatchTableAllocations[handle.index]));
            m_BatchTableAllocations[handle.index] = BlockAllocator.Allocation.Invalid;
        }

        public bool Register(Mesh mesh, out GeometryPoolHandle outHandle)
        {
            return Register(new GeometryPoolEntryDesc()
            {
                mesh = mesh,
                submeshData = null
            }, out outHandle);
        }

        private static int FindSubmeshEntryInDesc(int submeshIndex, in GeometryPoolEntryDesc entry)
        {
            if (entry.submeshData == null)
                return -1;

            for (int i = 0; i < entry.submeshData.Length; ++i)
            {
                if (entry.submeshData[i].submeshIndex == submeshIndex)
                    return i;
            }

            return -1;
        }

        private uint RegisterMaterial(Material m)
        {
            int materialHashCode = m.GetHashCode();
            if (m_MaterialEntries.TryGetValue(materialHashCode, out GeometryPoolMaterialEntry entry))
            {
                ++entry.refCount;
                m_MaterialEntries[materialHashCode] = entry;
                return entry.materialGPUKey;
            }
            else
            {
                uint materialGPUKey = m_NextMaterialGPUKey++;
                var materialEntry = new GeometryPoolMaterialEntry()
                {
                    refCount = 1,
                    materialGPUKey = materialGPUKey,
                    material = m
                };

                m_MaterialEntries.Add(materialHashCode, materialEntry);
                return materialGPUKey;
            }
        }

        private void UnregisterMaterial(int materialHashCode)
        {
            GeometryPoolMaterialEntry entry;
            if (!m_MaterialEntries.TryGetValue(materialHashCode, out entry))
                return;

            --entry.refCount;
            if (entry.refCount == 0)
                m_MaterialEntries.Remove(materialHashCode);
            else
                m_MaterialEntries[materialHashCode] = entry;
        }

        public bool Register(in GeometryPoolEntryDesc entryDesc, out GeometryPoolHandle outHandle)
        {
            if (entryDesc.mesh == null)
            {
                outHandle = GeometryPoolHandle.Invalid;
                return false;
            }

            Mesh mesh = entryDesc.mesh;
            uint meshHashCode = CalculateEntryHash(entryDesc);
            if (m_MeshHashToHandle.TryGetValue(meshHashCode, out outHandle))
            {
                MeshSlot meshSlot = m_MeshSlots[outHandle];
                Assertions.Assert.IsTrue(meshHashCode == meshSlot.meshHash);
                ++meshSlot.refCount;
                m_MeshSlots[outHandle] = meshSlot;
                return true;
            }

            var newSlot = new MeshSlot()
            {
                refCount = 1,
                meshHash = meshHashCode,
            };

            int clusterCounts = 0;
            int indexCount = 0;
            for (int i = 0; i < (int)mesh.subMeshCount; ++i)
            {
                int submeshIndexCount = (int)mesh.GetIndexCount(i);
                int submeshClusterCount = DivUp(submeshIndexCount / 3, GeometryPoolConstants.GeoPoolClusterPrimitiveCount);
                indexCount += submeshIndexCount;
                clusterCounts += submeshClusterCount;
            }

            if (!AllocateGeo(mesh.vertexCount, indexCount, mesh.subMeshCount, clusterCounts, out outHandle))
                return false;

            if (!m_MeshSlots.TryAdd(outHandle, newSlot))
            {
                //revert the allocation.
                DeallocateGeo(outHandle);
                outHandle = GeometryPoolHandle.Invalid;
                return false;
            }

            if (!m_MeshHashToHandle.TryAdd(meshHashCode, outHandle))
            {
                DeallocateGeo(outHandle);
                m_MeshSlots.Remove(outHandle);
                outHandle = GeometryPoolHandle.Invalid;
                return false;
            }

            //register material information
            var materialHashes = new NativeArray<int>(mesh.subMeshCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var materialKeys = new NativeArray<uint>(mesh.subMeshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (mesh.subMeshCount > 0)
            {
                for (int submeshIndex = 0; submeshIndex < materialKeys.Length; ++submeshIndex)
                {
                    int entryIndex = FindSubmeshEntryInDesc(submeshIndex, entryDesc);
                    int materialHash = 0;
                    uint materialKey = 0;

                    if (entryIndex != -1)
                    {
                        Material m = entryDesc.submeshData[entryIndex].material;
                        materialHash = m.GetHashCode();
                        materialKey = RegisterMaterial(m);
                    }

                    materialHashes[submeshIndex] = materialHash;
                    materialKeys[submeshIndex] = materialKey;
                }
            }

            m_MaterialHashes.Add(outHandle, materialHashes);
            UpdateGeoGpuState(mesh, materialKeys, outHandle);
            materialKeys.Dispose();

            return true;
        }

        public void Unregister(GeometryPoolHandle handle)
        {
            if (!m_MeshSlots.TryGetValue(handle, out MeshSlot outSlot))
                return;

            --outSlot.refCount;
            if (outSlot.refCount != 0)
            {
                m_MeshSlots[handle] = outSlot;
                return;
            }

            m_MeshHashToHandle.Remove(outSlot.meshHash);
            m_MeshSlots.Remove(handle);

            if (m_MaterialHashes.TryGetValue(handle, out var materialHashes))
            {
                if (materialHashes.IsCreated)
                {
                    foreach (var hash in materialHashes)
                        UnregisterMaterial(hash);
                    materialHashes.Dispose();
                }

                m_MaterialHashes.Remove(handle);
            }
            DeallocateGeo(handle);
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
                var currIdxBuffer = mesh.GetIndexBuffer();
                m_InputBufferReferences.Add(currIdxBuffer);
                return currIdxBuffer;
            }

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

        private void AddMeshEntryUpdateCommand(
            CommandBuffer cmdBuffer,
            int geoHandleIndex,
            in GeoPoolMeshEntry meshEntry, ComputeBuffer outputMeshEntryBuffer)
        {
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoHandle, geoHandleIndex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputClusterBufferIndex, meshEntry.clustersBufferIndex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputClusterBufferCounts, meshEntry.clustersCounts);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputMeshVertFlags, meshEntry.vertexFlags);
            int kernel = m_KernelMainUpdateMeshEntry;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputMeshEntries, outputMeshEntryBuffer);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, 1, 1, 1);
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
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._GeoSubMeshEntryOffset_VertexFlags, metadataEntry.subMeshEntryOffset_VertexFlags);

            int kernel = m_KernelMainUpdateMeshMetadata;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputGeoMetadataBuffer, outputMetadataBuffer);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, 1, 1, 1);
        }

        private void AddIndexUpdateCommand(
            CommandBuffer cmdBuffer,
            IndexFormat inputFormat, in GraphicsBuffer inputBuffer, in BlockAllocator.Allocation location, GraphicsBuffer outputIdxBuffer)
        {
            if (location.block.count == 0)
                return;

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
            out GeoPoolInputFlags ouputFlags,
            ComputeBuffer outputVertexBuffer)
        {
            GeoPoolInputFlags flags =
                (uv1 != null ? GeoPoolInputFlags.HasUV1 : GeoPoolInputFlags.None)
              | (t != null ? GeoPoolInputFlags.HasTangent : GeoPoolInputFlags.None);

            ouputFlags = flags;

            if (location.block.count == 0)
                return;

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
            if (size == 0)
                return;

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
            uint materialKey,
            in BlockAllocator.Allocation lookupAllocation,
            in BlockAllocator.Allocation entryAllocation,
            ComputeBuffer outputLookupBuffer,
            ComputeBuffer outputEntryBuffer)
        {
            int lookupCounts = DivUp(submeshDescriptor.indexCount, 3);
            if (lookupCounts == 0)
                return;

            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshIndexStart, submeshDescriptor.indexStart);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshIndexCount, submeshDescriptor.indexCount);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshBaseVertex, submeshDescriptor.baseVertex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshDestIndex, entryAllocation.block.offset + descriptorIndex);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubMeshMaterialKey, (int)materialKey);

            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupDestOffset, indexOffset / 3 + lookupAllocation.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupBufferCount, lookupCounts);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputSubmeshLookupData, descriptorIndex);

            int kernel = m_KernelMainUpdateSubMeshData;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputSubMeshLookupBuffer, outputLookupBuffer);
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputSubMeshEntryBuffer, outputEntryBuffer);

            int groupCountsX = DivUp(lookupCounts, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private void AddSequentialClustersInformation(
            CommandBuffer cmdBuffer,
            in BlockAllocator.Allocation baseClusterAllocation,
            in BlockAllocator.Allocation baseVertexAllocation,
            in BlockAllocator.Allocation baseIndexAllocation,
            uint materialKey,
            ComputeBuffer outputClusterEntryBuffer)
        {
            if (baseClusterAllocation.block.count == 0)
                return;

            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputClusterCounts, baseClusterAllocation.block.count);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputClusterBaseOffset, baseClusterAllocation.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputMaterialKey, (int)materialKey);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputIndexBufferBaseOffset, baseIndexAllocation.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputVertexBufferBaseOffset, baseVertexAllocation.block.offset);
            cmdBuffer.SetComputeIntParam(m_GeometryPoolKernelsCS, GeoPoolShaderIDs._InputIndexBufferCounts, baseIndexAllocation.block.count);

            int kernel = m_KernelMainUpdateSequentialClusterData;
            cmdBuffer.SetComputeBufferParam(m_GeometryPoolKernelsCS, kernel, GeoPoolShaderIDs._OutputClusterBuffer, outputClusterEntryBuffer);
            int groupCountsX = DivUp(baseClusterAllocation.block.count, 64);
            cmdBuffer.DispatchCompute(m_GeometryPoolKernelsCS, kernel, groupCountsX, 1, 1);
        }

        private Vector4 GetPackedGeoPoolParam0()
        {
            return new Vector4(m_MaxVertCounts, 0.0f, 0.0f, 0.0f);
        }

        public void BindResourcesCS(CommandBuffer cmdBuffer, ComputeShader cs, int kernel)
        {
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalVertexBuffer, globalVertexBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalIndexBuffer, globalIndexBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalSubMeshLookupBuffer, globalSubMeshLookupBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalSubMeshEntryBuffer, globalSubMeshEntryBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalBatchTableBuffer, globalBatchTableBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalBatchInstanceBuffer, globalBatchInstanceBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolGlobalClusterEntriesBuffer, globalClusterEntryBuffer);
            cmdBuffer.SetComputeBufferParam(cs, kernel, GeoPoolShaderIDs._GeoPoolMeshEntriesBuffer, globalMeshEntryBuffer);
            cmdBuffer.SetComputeVectorParam(cs, GeoPoolShaderIDs._GeoPoolGlobalParams, GetPackedGeoPoolParam0());
        }

        public void BindResources(Material material)
        {
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalVertexBuffer, globalVertexBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalIndexBuffer, globalIndexBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalSubMeshLookupBuffer, globalSubMeshLookupBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalSubMeshEntryBuffer, globalSubMeshEntryBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalBatchTableBuffer, globalBatchTableBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalBatchInstanceBuffer, globalBatchInstanceBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolGlobalClusterEntriesBuffer, globalClusterEntryBuffer);
            material.SetBuffer(GeoPoolShaderIDs._GeoPoolMeshEntriesBuffer, globalMeshEntryBuffer);
            material.SetVector(GeoPoolShaderIDs._GeoPoolGlobalParams, GetPackedGeoPoolParam0());
        }

        public void BindResourcesGlobal(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalVertexBuffer, globalVertexBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalIndexBuffer, globalIndexBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalSubMeshLookupBuffer, globalSubMeshLookupBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalSubMeshEntryBuffer, globalSubMeshEntryBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalMetadataBuffer, globalMetadataBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalBatchTableBuffer, globalBatchTableBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalBatchInstanceBuffer, globalBatchInstanceBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolGlobalClusterEntriesBuffer, globalClusterEntryBuffer);
            cmdBuffer.SetGlobalBuffer(GeoPoolShaderIDs._GeoPoolMeshEntriesBuffer, globalMeshEntryBuffer);
            cmdBuffer.SetGlobalVector(GeoPoolShaderIDs._GeoPoolGlobalParams, GetPackedGeoPoolParam0());
        }
    }
}
