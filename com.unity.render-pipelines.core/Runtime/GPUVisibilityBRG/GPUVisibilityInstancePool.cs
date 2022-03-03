using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    internal struct GPUVisibilityInstance
    {
        public int index;
        public static GPUVisibilityInstance Invalid = new GPUVisibilityInstance() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(GPUVisibilityInstance other) => index == other.index;
    }

    internal struct GPUVisibilityInstancePool
    {
        BRGInstanceBufferOffsets m_BigBufferCachedOffsets;
        GPUInstanceDataBuffer m_BigInstanceDataBuffer;
        BRGTransformUpdater m_TransformUpdater;

        public GPUInstanceDataBuffer bigBuffer => m_BigInstanceDataBuffer;
        public NativeArray<int> aliveInstanceIndices => m_TransformUpdater.indices.GetSubArray(0, m_TransformUpdater.length);

        private struct GPUVisibilityInstanceData
        {
            public bool valid;
            public BRGTransformObjectIndex transformIndex;
        }

        private int m_NextInstanceIndex;
        private int m_Capacity;

        NativeArray<GPUVisibilityInstanceData> m_InstanceData;
        NativeArray<GPUVisibilityInstance> m_TransformIndexToInstanceIndexMap;
        NativeList<GPUVisibilityInstance> m_FreeInstances;


        public void Initialize(int maxInstances)
        {
            m_InstanceData = new NativeArray<GPUVisibilityInstanceData>(maxInstances, Allocator.Persistent);
            m_TransformIndexToInstanceIndexMap = new NativeArray<GPUVisibilityInstance>(maxInstances, Allocator.Persistent);
            m_FreeInstances = new NativeList<GPUVisibilityInstance>(maxInstances, Allocator.Persistent);

            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            m_TransformUpdater = new BRGTransformUpdater();
            m_TransformUpdater.Initialize(maxInstances, BRGTransformUpdaterFlags.None);
            m_BigInstanceDataBuffer = GPUInstanceDataBuffer.CreateDefaultInstanceBuffer(maxInstances);
            m_BigBufferCachedOffsets = new BRGInstanceBufferOffsets()
            {
                localToWorld = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_ObjectToWorld) / vec4Size,
                worldToLocal = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_WorldToObject) / vec4Size,
                probeOffsetSHAr = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHAr) / vec4Size,
                probeOffsetSHAg = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHAg) / vec4Size,
                probeOffsetSHAb = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHAb) / vec4Size,
                probeOffsetSHBr = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHBr) / vec4Size,
                probeOffsetSHBg = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHBg) / vec4Size,
                probeOffsetSHBb = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHBb) / vec4Size,
                probeOffsetSHC = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_SHC) / vec4Size,
                probeOffsetOcclusion = m_BigInstanceDataBuffer.GetGpuAddress(GPUInstanceDataBuffer.DefaultSchema.unity_ProbesOcclusion) / vec4Size
            };

            m_NextInstanceIndex = 0;
            m_Capacity = maxInstances;
        }

        public void StartUpdateJobs()
        {
            m_TransformUpdater.StartUpdateJobs();
        }

        public bool EndUpdateJobs(CommandBuffer cmdBuffer)
        {
            return m_TransformUpdater.EndUpdateJobs(cmdBuffer, m_BigBufferCachedOffsets, m_BigInstanceDataBuffer.gpuBuffer);
        }

        public GPUVisibilityInstance AllocateVisibilityEntity(Transform transformObject, bool hasLightProbe)
        {
            var newHandle = GPUVisibilityInstance.Invalid;
            if (m_FreeInstances.IsEmpty)
            {
                if (m_NextInstanceIndex == m_Capacity)
                {
                    Debug.LogError("Exceeded maximum number of instances. Cannot add any more.");
                    return GPUVisibilityInstance.Invalid;
                }

                newHandle = new GPUVisibilityInstance() { index = m_NextInstanceIndex };
                ++m_NextInstanceIndex;
            }
            else
            {
                newHandle = m_FreeInstances[m_FreeInstances.Length - 1];
                m_FreeInstances.RemoveAt(m_FreeInstances.Length - 1);
            }

            var newData = new GPUVisibilityInstanceData();
            newData.valid = true;
            newData.transformIndex = m_TransformUpdater.RegisterTransformObject(newHandle.index, transformObject, hasLightProbe, null);
            m_InstanceData[newHandle.index] = newData;
            m_TransformIndexToInstanceIndexMap[newData.transformIndex.index] = newHandle;
            return newHandle;
        }

        public void FreeVisibilityEntity(GPUVisibilityInstance instance)
        {
            Assert.IsTrue(instance.valid);
            m_FreeInstances.Add(instance);

            var instanceData = m_InstanceData[instance.index];
            int lastTransformIndex = m_TransformUpdater.length - 1;
            var instanceToUpdate = m_TransformIndexToInstanceIndexMap[lastTransformIndex];
            m_TransformIndexToInstanceIndexMap[lastTransformIndex] = GPUVisibilityInstance.Invalid;
            m_TransformUpdater.DeleteTransformObjectSwapBack(instanceData.transformIndex);
            m_InstanceData[instanceToUpdate.index] = instanceData;
            m_TransformIndexToInstanceIndexMap[instanceData.transformIndex.index] = instanceToUpdate;

            m_InstanceData[instance.index] = new GPUVisibilityInstanceData() { valid = false };
        }

        public bool InternalSanityCheckStates()
        {
            Dictionary<int, int> usedInstances = new Dictionary<int, int>();
            for (int i = 0; i < m_InstanceData.Length; ++i)
            {
                var instanceData = m_InstanceData[i];
                if (!instanceData.valid)
                    continue;

                usedInstances.Add(i, 1);
                int backIndex = m_TransformIndexToInstanceIndexMap[instanceData.transformIndex.index].index;
                if (backIndex != i)
                    return false;
            }

            var aliveIndicesArray = aliveInstanceIndices;
            for (int i = 0; i < aliveIndicesArray.Length; ++i)
            {
                if (!usedInstances.TryGetValue(aliveIndicesArray[i], out var counter))
                {
                    return false;
                }

                if (counter != 1)
                    return false;

                usedInstances[aliveIndicesArray[i]] = counter + 1;
            }

            return true;
        }

        public void Dispose()
        {
            m_InstanceData.Dispose();
            m_TransformIndexToInstanceIndexMap.Dispose();
            m_FreeInstances.Dispose();

            m_TransformUpdater.Dispose();
            m_BigInstanceDataBuffer.Dispose();
        }
    }
}
