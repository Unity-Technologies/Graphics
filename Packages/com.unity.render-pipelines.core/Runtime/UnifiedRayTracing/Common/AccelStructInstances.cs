using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
                if (geomHandle.valid)
                    m_GeometryPool.Unregister(geomHandle);
            }
            m_GeometryPool.SendGpuCommands();

            m_InstanceBuffer?.Dispose();
            m_TerrainBuffer?.Dispose();
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

        public int AddInstance(in ProceduralInstanceDesc procInstance, uint materialID, uint renderingLayerMask, RTTerrain terrainData)
        {
            var terrainSlot = m_TerrainBuffer.Add(1)[0];
            m_TerrainBuffer.Set(terrainSlot, terrainData);

            var instanceSlot = m_InstanceBuffer.Add(1)[0];
            m_InstanceBuffer.Set(instanceSlot,
                new RTInstance
                {
                    localToWorld = ToFloat4x3(procInstance.localToWorldMatrix),
                    localToWorldNormals = NormalMatrix(procInstance.localToWorldMatrix),
                    previousLocalToWorld = ToFloat4x3(procInstance.localToWorldMatrix),
                    userTerrainIndex = terrainSlot.block.offset,
                    userMaterialID = materialID,
                    instanceMask = procInstance.mask,
                    renderingLayerMask = renderingLayerMask,
                    geometryIndex = 0xFFFFFFFF
                });

            var instanceEntry = new InstanceEntry
            {
                geometryPoolHandle = GeometryPoolHandle.Invalid,
                indexInTerrainBuffer = terrainSlot,
                indexInInstanceBuffer = instanceSlot,
                instanceMask = procInstance.mask,
                vertexOffset = 0xFFFFFFFF,
                indexOffset = 0xFFFFFFFF,
            };
            m_Instances.Add(instanceSlot.block.offset, instanceEntry);

            return instanceSlot.block.offset;
        }

        void AddInstance(BlockAllocator.Allocation slotAllocation, in MeshInstanceDesc meshInstance, uint materialID, uint renderingLayerMask)
        {
            Debug.Assert(meshInstance.mesh != null, "targetRenderer.mesh is null");

            GeometryPoolHandle geometryHandle;
            if (!m_GeometryPool.Register(meshInstance.mesh, out geometryHandle))
                throw new System.InvalidOperationException("Failed to allocate geometry data for instance");
            m_GeometryPool.SendGpuCommands();

            float localToWorldDet = meshInstance.localToWorldMatrix.determinant;

             m_InstanceBuffer.Set(slotAllocation,
                new RTInstance
                {
                    localToWorld = ToFloat4x3(meshInstance.localToWorldMatrix),
                    localToWorldDeterminant = localToWorldDet,
                    localToWorldDetSign = localToWorldDet > 0 ? 1.0f : -1.0f,
                    userTerrainIndex = -1,
                    localToWorldNormals = NormalMatrix(meshInstance.localToWorldMatrix),
                    previousLocalToWorld = ToFloat4x3(meshInstance.localToWorldMatrix),
                    userMaterialID = materialID,
                    instanceMask = meshInstance.mask,
                    renderingLayerMask = renderingLayerMask,
                    geometryIndex = (uint)(m_GeometryPool.GetEntryGeomAllocation(geometryHandle).meshChunkTableAlloc.block.offset + meshInstance.subMeshIndex)
                });


            var allocInfo = m_GeometryPool.GetEntryGeomAllocation(geometryHandle).meshChunks[meshInstance.subMeshIndex];

            var instanceEntry = new InstanceEntry
            {
                geometryPoolHandle = geometryHandle,
                indexInTerrainBuffer = BlockAllocator.Allocation.Invalid,
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

            var terrainHandle = removedEntry.indexInTerrainBuffer;
            if (terrainHandle.valid)
                m_TerrainBuffer.Remove(terrainHandle);

            var geomHandle = removedEntry.geometryPoolHandle;
            if (geomHandle.valid)
            {
                m_GeometryPool.Unregister(geomHandle);
                m_GeometryPool.SendGpuCommands();
            }
        }

        public void ClearInstances()
        {
            foreach (InstanceEntry instanceEntry in m_Instances.Values)
            {
                GeometryPoolHandle geomHandle = instanceEntry.geometryPoolHandle;
                if (geomHandle.valid)
                    m_GeometryPool.Unregister(geomHandle);
            }
            m_GeometryPool.SendGpuCommands();

            m_Instances.Clear();
            m_TerrainBuffer.Clear();
            m_InstanceBuffer.Clear();
        }

        public void UpdateInstanceTransform(int instanceHandle, Matrix4x4 localToWorldMatrix)
        {
            bool success = m_Instances.TryGetValue(instanceHandle, out InstanceEntry instanceEntry);
            Assert.IsTrue(success);

            var instanceInfo = m_InstanceBuffer.Get(instanceEntry.indexInInstanceBuffer);
            instanceInfo.localToWorld = ToFloat4x3(localToWorldMatrix);
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
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_AccelStructInstanceList"), m_InstanceBuffer.GetGpuBuffer(cmd));
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_TerrainList"), m_TerrainBuffer.GetGpuBuffer(cmd));
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_globalIndexBuffer"), m_GeometryPool.globalIndexBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_globalVertexBuffer"), m_GeometryPool.globalVertexBuffer);
            shader.SetIntParam(cmd, Shader.PropertyToID("g_globalVertexBufferStride"), m_GeometryPool.globalVertexBufferStrideBytes/4);
            shader.SetBufferParam(cmd, Shader.PropertyToID("g_MeshList"), m_GeometryPool.globalMeshChunkTableEntryBuffer);
        }

        public int GetInstanceCount()
        {
            return m_Instances.Count;
        }

        static float4x3 NormalMatrix(float4x4 m)
        {
            float3x3 t = new float3x3(m);
            var res = math.inverse(math.transpose(t));

            return new float4x3(new float4(res.c0, 0.0f), new float4(res.c1, 0.0f), new float4(res.c2, 0.0f));
        }
        static float4x3 ToFloat4x3(in float4x4 m)
        {
            return new float4x3(m.c0.x, m.c0.y, m.c0.z, m.c1.x, m.c1.y, m.c1.z, m.c2.x, m.c2.y, m.c2.z, m.c3.x, m.c3.y, m.c3.z);
        }

        readonly GeometryPool m_GeometryPool;
        readonly PersistentGpuArray<RTInstance> m_InstanceBuffer = new PersistentGpuArray<RTInstance>(100);
        readonly PersistentGpuArray<RTTerrain> m_TerrainBuffer = new PersistentGpuArray<RTTerrain>(20);

        [StructLayout(LayoutKind.Sequential)]
        public struct RTInstance
        {
            public float4x3 localToWorld;
            public float localToWorldDeterminant;
            public float localToWorldDetSign;
            public int userTerrainIndex;
            public uint padding1;
            public float4x3 previousLocalToWorld;
            public float4x3 localToWorldNormals;
            public uint renderingLayerMask;
            public uint instanceMask;
            public uint userMaterialID;
            public uint geometryIndex;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct RTTerrain
        {
            public float3 terrainScale;
            public float heightmapWidthInTexels;
            public float3 invTerrainScale;
            public float invHeightmapWidthInTexels;
            public int pow2DivideTileCountX;
            public int pow2ModuloTileCountX;
            public int tileWidthInCells;
            public float invTerrainWidthInCells;
        }

        public class InstanceEntry
        {
            public GeometryPoolHandle geometryPoolHandle;
            public BlockAllocator.Allocation indexInTerrainBuffer;
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
