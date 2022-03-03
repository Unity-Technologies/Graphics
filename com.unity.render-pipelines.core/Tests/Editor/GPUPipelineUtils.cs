using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    //Helper class containing a snapshot of the GPU buffers of a GeometryPool
    internal struct GeometryPoolCPUReadbackData : IDisposable
    {
        CommandBuffer m_cmdBuffer;
        AsyncGPUReadbackRequest m_request;
        GeometryPool m_geometryPool;

        public GeometryPool geoPool { get { return m_geometryPool; } }
        public NativeArray<int> gpuIndexData;
        public NativeArray<float> gpuVertexData;
        public NativeArray<int> gpuSubMeshLookupData;
        public NativeArray<GeoPoolSubMeshEntry> gpuSubMeshEntryData;
        public NativeArray<GeoPoolMetadataEntry> gpuMetadatas;
        public NativeArray<GeoPoolBatchTableEntry> gpuBatchTable;
        public NativeArray<GeoPoolClusterEntry> gpuClusterEntries;
        public NativeArray<GeoPoolMeshEntry> gpuMeshEntries;
        public NativeArray<short> gpuBatchInstanceData;

        public void Load(GeometryPool geometryPool)
        {
            m_cmdBuffer = new CommandBuffer();
            m_geometryPool = geometryPool;

            var indexData = new NativeArray<int>(geometryPool.indicesCount, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalIndexBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    indexData.CopyFrom(req.GetData<int>());
            });

            var vertData = new NativeArray<float>(geometryPool.verticesCount * (GeometryPool.GetVertexByteSize() / 4), Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalVertexBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    vertData.CopyFrom(req.GetData<float>());
            });

            var subMeshLookupData = new NativeArray<int>(geometryPool.subMeshLookupCount / 4, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalSubMeshLookupBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    subMeshLookupData.CopyFrom(req.GetData<int>());
            });

            var subMeshEntryData = new NativeArray<GeoPoolSubMeshEntry>(geometryPool.subMeshEntryCount, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalSubMeshEntryBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    subMeshEntryData.CopyFrom(req.GetData<GeoPoolSubMeshEntry>());
            });

            var metaData = new NativeArray<GeoPoolMetadataEntry>(geometryPool.maxMeshes, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalMetadataBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    metaData.CopyFrom(req.GetData<GeoPoolMetadataEntry>());
            });

            var batchTable = new NativeArray<GeoPoolBatchTableEntry>(geometryPool.maxBatchCount, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalBatchTableBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    batchTable.CopyFrom(req.GetData<GeoPoolBatchTableEntry>());
            });


            var batchInstances = new NativeArray<short>(geometryPool.maxBatchInstanceCount, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalBatchInstanceBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    batchInstances.CopyFrom(req.GetData<short>());
            });

            var clusterEntries = new NativeArray<GeoPoolClusterEntry>(geometryPool.maxClusterEntryCount, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalClusterEntryBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    clusterEntries.CopyFrom(req.GetData<GeoPoolClusterEntry>());
            });


            var meshEntries = new NativeArray<GeoPoolMeshEntry>(geometryPool.maxMeshes, Allocator.Persistent);
            m_cmdBuffer.RequestAsyncReadback(geometryPool.globalMeshEntryBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    meshEntries.CopyFrom(req.GetData<GeoPoolMeshEntry>());
            });

            m_cmdBuffer.WaitAllAsyncReadbackRequests();

            Graphics.ExecuteCommandBuffer(m_cmdBuffer);
            gpuIndexData = indexData;
            gpuVertexData = vertData;
            gpuSubMeshLookupData = subMeshLookupData;
            gpuSubMeshEntryData = subMeshEntryData;
            gpuMetadatas = metaData;
            gpuBatchTable = batchTable;
            gpuBatchInstanceData = batchInstances;
            gpuClusterEntries = clusterEntries;
            gpuMeshEntries = meshEntries;
        }

        public void Dispose()
        {
            gpuIndexData.Dispose();
            gpuVertexData.Dispose();
            gpuMetadatas.Dispose();
            gpuSubMeshLookupData.Dispose();
            gpuSubMeshEntryData.Dispose();
            gpuBatchTable.Dispose();
            gpuBatchInstanceData.Dispose();
            gpuClusterEntries.Dispose();
            gpuMeshEntries.Dispose();
            m_cmdBuffer.Dispose();
        }
    }

    //Helper class containing a snapshot of the GPU big instance buffer data
    internal struct BigBufferCPUReadbackData : IDisposable
    {
        public NativeArray<Vector4> data;
        GPUInstanceDataBuffer m_BigBuffer;

        public void Load(GPUInstanceDataBuffer bigBuffer)
        {
            m_BigBuffer = bigBuffer;
            var cmdBuffer = new CommandBuffer();
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            var localData = new NativeArray<Vector4>(bigBuffer.byteSize / vec4Size, Allocator.Persistent);
            cmdBuffer.RequestAsyncReadback(bigBuffer.gpuBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    localData.CopyFrom(req.GetData<Vector4>());
            });
            cmdBuffer.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Release();
            data = localData;
        }

        public T LoadData<T>(int instanceId, int propertyID) where T : unmanaged
        {
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int propertyIndex = m_BigBuffer.GetPropertyIndex(propertyID);
            Assert.IsTrue(m_BigBuffer.descriptions[propertyIndex].isPerInstance);
            int gpuBaseAddress = m_BigBuffer.gpuBufferComponentAddress[propertyIndex];
            int indexInArray = (gpuBaseAddress + m_BigBuffer.descriptions[propertyIndex].byteSize * instanceId) / vec4Size;

            unsafe
            {
                Vector4* dataPtr = (Vector4*)data.GetUnsafePtr<Vector4>() + indexInArray;
                T result = *(T*)(dataPtr);
                return result;
            }
        }

        public void Dispose()
        {
            data.Dispose();
        }
    }

    //Helper class containing snapshots of a cluster culling result
    internal struct ClusterCullingResultsCPUReadbackData : IDisposable
    {
        public BRGDrawCallArgument drawArgs;
        public NativeArray<int> visibleIndexData;
        public NativeArray<int> visibleClusterData;

        public void Load(
            GraphicsBuffer indirectDrawArgsBuffer,
            GraphicsBuffer visibleIndexBuffer,
            GraphicsBuffer visibleClusterBuffer)
        {
            var cmdBuffer = new CommandBuffer();

            var tmpDrawArgs = new NativeArray<BRGDrawCallArgument>(1, Allocator.Temp);
            cmdBuffer.RequestAsyncReadback(indirectDrawArgsBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    tmpDrawArgs.CopyFrom(req.GetData<BRGDrawCallArgument>());
            });

            cmdBuffer.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();

            drawArgs = tmpDrawArgs[0];
            tmpDrawArgs.Dispose();

            var tmpVisibleIndexData = new NativeArray<int>((int)drawArgs.indexCountPerInstance, Allocator.Persistent);
            cmdBuffer.RequestAsyncReadback(visibleIndexBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    tmpVisibleIndexData.CopyFrom(req.GetData<int>().GetSubArray(0, tmpVisibleIndexData.Length));
            });

            var tmpVisibleClusterData = new NativeArray<int>((int)drawArgs.indexCountPerInstance / (3 * GeometryPoolConstants.GeoPoolClusterPrimitiveCount), Allocator.Persistent);
            cmdBuffer.RequestAsyncReadback(visibleClusterBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    tmpVisibleClusterData.CopyFrom(req.GetData<int>().GetSubArray(0, tmpVisibleClusterData.Length));
            });

            cmdBuffer.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Release();

            visibleIndexData = tmpVisibleIndexData;
            visibleClusterData = tmpVisibleClusterData;
        }

        public void Dispose()
        {
            if (visibleIndexData.IsCreated)
                visibleIndexData.Dispose();

            if (visibleClusterData.IsCreated)
                visibleClusterData.Dispose();
        }
    }

}
