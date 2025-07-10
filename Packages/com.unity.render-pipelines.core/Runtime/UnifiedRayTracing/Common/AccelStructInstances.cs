using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal sealed class AccelStructInstances : IDisposable
    {
        internal AccelStructInstances(GeometryPool geometryPool)
        {
            m_GeometryPool = geometryPool;
        }

        public void Dispose()
        {
            foreach (InstanceEntry instanceEntry in m_Instances.Values)
            {
                GeometryPoolHandle geomHandle = instanceEntry.geometryPoolHandle;
                m_GeometryPool.Unregister(geomHandle);
            }
            m_GeometryPool.SendGpuCommands();

            m_InstanceBuffer?.Dispose();
            m_GeometryPool.Dispose();
        }

        public PersistentGpuArray<RTInstance> instanceBuffer  { get => m_InstanceBuffer; }
        public IReadOnlyCollection<InstanceEntry> instances { get => m_Instances.Values; }
        public GeometryPool geometryPool { get => m_GeometryPool; }

        public int AddInstance(MeshInstanceDesc meshInstance, uint materialID, uint renderingLayerMask)
        {
            var slot = m_InstanceBuffer.Add(1)[0];
            AddInstance(slot, meshInstance, materialID, renderingLayerMask);
            return slot.block.offset;
        }

        public int AddInstances(Span<MeshInstanceDesc> meshInstances, Span<uint> materialIDs, Span<uint> renderingLayerMask)
        {
            Assert.IsTrue(meshInstances.Length == materialIDs.Length);

            var slots = m_InstanceBuffer.Add(meshInstances.Length);

            for (int i = 0; i < meshInstances.Length; ++i)
                AddInstance(slots[i], meshInstances[i], materialIDs[i], renderingLayerMask[i]);

            return slots[0].block.offset;
        }

        void AddInstance(BlockAllocator.Allocation slotAllocation, in MeshInstanceDesc meshInstance, uint materialID, uint renderingLayerMask)
        {
            Debug.Assert(meshInstance.mesh != null, "targetRenderer.mesh is null");

            GeometryPoolHandle geometryHandle;
            if (!m_GeometryPool.Register(meshInstance.mesh, out geometryHandle))
                throw new System.InvalidOperationException("Failed to allocate geometry data for instance");
            m_GeometryPool.SendGpuCommands();

             m_InstanceBuffer.Set(slotAllocation,
                new RTInstance
                {
                    localToWorld = meshInstance.localToWorldMatrix,
                    localToWorldNormals = NormalMatrix(meshInstance.localToWorldMatrix),
                    previousLocalToWorld = meshInstance.localToWorldMatrix,
                    userMaterialID = materialID,
                    instanceMask = meshInstance.mask,
                    renderingLayerMask = renderingLayerMask,
                    geometryIndex = (uint)(m_GeometryPool.GetEntryGeomAllocation(geometryHandle).meshChunkTableAlloc.block.offset + meshInstance.subMeshIndex)
                });


            var allocInfo = m_GeometryPool.GetEntryGeomAllocation(geometryHandle).meshChunks[meshInstance.subMeshIndex];

            var instanceEntry = new InstanceEntry
            {
                geometryPoolHandle = geometryHandle,
                indexInInstanceBuffer = slotAllocation,
                instanceMask = meshInstance.mask,
                vertexOffset = (uint)(allocInfo.vertexAlloc.block.offset) * ((uint)GeometryPool.GetVertexByteSize() / 4),
                indexOffset = (uint)allocInfo.indexAlloc.block.offset,
            };
            m_Instances.Add(slotAllocation.block.offset, instanceEntry);
        }

        public GeometryPool.MeshChunk GetEntryGeomAllocation(GeometryPoolHandle handle, int submeshIndex)
        {
            return m_GeometryPool.GetEntryGeomAllocation(handle).meshChunks[submeshIndex];
        }

        public GraphicsBuffer indexBuffer { get { return m_GeometryPool.globalIndexBuffer; } }
        public GraphicsBuffer vertexBuffer { get { return m_GeometryPool.globalVertexBuffer; } }

        public void RemoveInstance(int instanceHandle)
        {
            bool success = m_Instances.TryGetValue(instanceHandle, out InstanceEntry removedEntry);
            Assert.IsTrue(success);

            m_Instances.Remove(instanceHandle);
            m_InstanceBuffer.Remove(removedEntry.indexInInstanceBuffer);

            var geomHandle = removedEntry.geometryPoolHandle;
            m_GeometryPool.Unregister(geomHandle);
            m_GeometryPool.SendGpuCommands();
        }

        public void ClearInstances()
        {
            foreach (InstanceEntry instanceEntry in m_Instances.Values)
            {
                GeometryPoolHandle geomHandle = instanceEntry.geometryPoolHandle;
                m_GeometryPool.Unregister(geomHandle);
            }
            m_GeometryPool.SendGpuCommands();

            m_Instances.Clear();
            m_InstanceBuffer.Clear();
        }

        public void UpdateInstanceTransform(int instanceHandle, Matrix4x4 localToWorldMatrix)
        {
            bool success = m_Instances.TryGetValue(instanceHandle, out InstanceEntry instanceEntry);
            Assert.IsTrue(success);

            var instanceInfo = m_InstanceBuffer.Get(instanceEntry.indexInInstanceBuffer);
            instanceInfo.localToWorld = localToWorldMatrix;
            instanceInfo.localToWorldNormals = NormalMatrix(localToWorldMatrix);
            m_InstanceBuffer.Set(instanceEntry.indexInInstanceBuffer, instanceInfo);

            m_TransformTouchedLastTimestamp = m_FrameTimestamp;
        }

        public void UpdateInstanceMaterialID(int instanceHandle, uint materialID)
        {
            InstanceEntry instanceEntry;
            bool success = m_Instances.TryGetValue(instanceHandle, out instanceEntry);
            Assert.IsTrue(success);

            var instanceInfo = m_InstanceBuffer.Get(instanceEntry.indexInInstanceBuffer);
            instanceInfo.userMaterialID = materialID;
            m_InstanceBuffer.Set(instanceEntry.indexInInstanceBuffer, instanceInfo);
        }

        public void UpdateRenderingLayerMask(int instanceHandle, uint renderingLayerMask)
        {
            InstanceEntry instanceEntry;
            bool success = m_Instances.TryGetValue(instanceHandle, out instanceEntry);
            Assert.IsTrue(success);

            var instanceInfo = m_InstanceBuffer.Get(instanceEntry.indexInInstanceBuffer);
            instanceInfo.renderingLayerMask = renderingLayerMask;
            m_InstanceBuffer.Set(instanceEntry.indexInInstanceBuffer, instanceInfo);
        }

        public void UpdateInstanceMask(int instanceHandle, uint mask)
        {
            bool success = m_Instances.TryGetValue(instanceHandle, out InstanceEntry instanceEntry);
            Assert.IsTrue(success);

            instanceEntry.instanceMask = mask;

            var instanceInfo = m_InstanceBuffer.Get(instanceEntry.indexInInstanceBuffer);
            instanceInfo.instanceMask = mask;
            m_InstanceBuffer.Set(instanceEntry.indexInInstanceBuffer, instanceInfo);
        }

        public void NextFrame()
        {
            if ((m_FrameTimestamp - m_TransformTouchedLastTimestamp) <= 1)
            {
                m_InstanceBuffer.ModifyForEach(
                instance =>
                {
                    instance.previousLocalToWorld = instance.localToWorld;
                    return instance;
                });
            }

            m_FrameTimestamp++;
        }

        public bool instanceListValid => m_InstanceBuffer != null;

        public void Bind(CommandBuffer cmd, IRayTracingShader shader)
        {
            var gpuBuffer = m_InstanceBuffer.GetGpuBuffer(cmd);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_AccelStructInstanceList"), gpuBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_globalIndexBuffer"), m_GeometryPool.globalIndexBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_globalVertexBuffer"), m_GeometryPool.globalVertexBuffer);
            shader.SetIntParam(cmd, Shader.PropertyToID("g_globalVertexBufferStride"), m_GeometryPool.globalVertexBufferStrideBytes/4);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_MeshList"), m_GeometryPool.globalMeshChunkTableEntryBuffer);
        }

        public int GetInstanceCount()
        {
            return m_Instances.Count;
        }

        static private float4x4 NormalMatrix(float4x4 m)
        {
            float3x3 t = new float3x3(m);
            return new float4x4(math.inverse(math.transpose(t)), new float3(0.0));
        }

        readonly GeometryPool m_GeometryPool;
        readonly PersistentGpuArray<RTInstance> m_InstanceBuffer = new PersistentGpuArray<RTInstance>(100);

        public struct RTInstance
        {
            public float4x4 localToWorld;
            public float4x4 previousLocalToWorld;
            public float4x4 localToWorldNormals;
            public uint renderingLayerMask;
            public uint instanceMask;
            public uint userMaterialID;
            public uint geometryIndex;
        };

        public class InstanceEntry
        {
            public GeometryPoolHandle geometryPoolHandle;
            public BlockAllocator.Allocation indexInInstanceBuffer;
            public uint instanceMask;
            public uint vertexOffset;
            public uint indexOffset;
        }

        readonly Dictionary<int, InstanceEntry> m_Instances = new Dictionary<int, InstanceEntry>();
        uint m_FrameTimestamp = 0;
        uint m_TransformTouchedLastTimestamp = 0;
    }
}
