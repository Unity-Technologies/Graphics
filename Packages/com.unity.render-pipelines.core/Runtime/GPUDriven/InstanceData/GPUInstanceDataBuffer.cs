using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using static UnityEngine.Rendering.RenderersParameters;

namespace UnityEngine.Rendering
{
    internal struct GPUInstanceComponentDesc
    {
        public int propertyID;
        public int byteSize;
        public bool isOverriden;
        public bool isPerInstance;
        public InstanceType instanceType;
        public InstanceComponentGroup componentGroup;

        public GPUInstanceComponentDesc(int inPropertyID, int inByteSize, bool inIsOverriden, bool inPerInstance, InstanceType inInstanceType, InstanceComponentGroup inComponentType)
        {
            propertyID = inPropertyID;
            byteSize = inByteSize;
            isOverriden = inIsOverriden;
            isPerInstance = inPerInstance;
            instanceType = inInstanceType;
            componentGroup = inComponentType;
        }
    }

    internal class GPUInstanceDataBuffer : IDisposable
    {
        private static int s_NextLayoutVersion = 0;
        public static int NextVersion() { return ++s_NextLayoutVersion; }

        public InstanceNumInfo instanceNumInfo;
        public NativeArray<int> instancesNumPrefixSum;
        public NativeArray<int> instancesSpan;
        public int byteSize;
        public int perInstanceComponentCount;
        public int version;
        public int layoutVersion;
        public GraphicsBuffer gpuBuffer;
        public GraphicsBuffer validComponentsIndicesGpuBuffer;
        public GraphicsBuffer componentAddressesGpuBuffer;
        public GraphicsBuffer componentInstanceIndexRangesGpuBuffer;
        public GraphicsBuffer componentByteCountsGpuBuffer;
        public NativeArray<GPUInstanceComponentDesc> descriptions;
        public NativeArray<MetadataValue> defaultMetadata;
        public NativeArray<int> gpuBufferComponentAddress;
        public NativeParallelHashMap<int, int> nameToMetadataMap;

        public bool valid => instancesSpan.IsCreated;

        private static GPUInstanceIndex CPUInstanceToGPUInstance(in NativeArray<int> instancesNumPrefixSum, InstanceHandle instance)
        {
            bool valid = instance.valid && instance.type < InstanceType.Count;
#if DEBUG
            Assert.IsTrue(valid);
#endif

            if (!valid)
                return GPUInstanceIndex.Invalid;

            int instanceType = (int)instance.type;
            int perTypeInstanceIndex = instance.instanceIndex;
            int gpuInstanceIndex = instancesNumPrefixSum[instanceType] + perTypeInstanceIndex;

            return new GPUInstanceIndex { index = gpuInstanceIndex };
        }

        public int GetPropertyIndex(int propertyID, bool assertOnFail = true)
        {
            if (nameToMetadataMap.TryGetValue(propertyID, out int componentIndex))
            {
                return componentIndex;
            }

            if (assertOnFail)
                Assert.IsTrue(false, "Count not find gpu address for parameter specified: " + propertyID);
            return -1;
        }

        public int GetGpuAddress(string strName, bool assertOnFail = true)
        {
            int componentIndex = GetPropertyIndex(Shader.PropertyToID(strName), false);
            if (assertOnFail && componentIndex == -1)
                Assert.IsTrue(false, "Count not find gpu address for parameter specified: " + strName);

            return componentIndex != -1 ? gpuBufferComponentAddress[componentIndex] : -1;
        }

        public int GetGpuAddress(int propertyID, bool assertOnFail = true)
        {
            int componentIndex = GetPropertyIndex(propertyID, assertOnFail);
            return componentIndex != -1 ? gpuBufferComponentAddress[componentIndex] : -1;
        }

        public GPUInstanceIndex CPUInstanceToGPUInstance(InstanceHandle instance)
        {
            return CPUInstanceToGPUInstance(instancesNumPrefixSum, instance);
        }

        public unsafe InstanceHandle GPUInstanceToCPUInstance(GPUInstanceIndex gpuInstanceIndex)
        {
            var instanceIndex = gpuInstanceIndex.index;
            InstanceType instanceType = InstanceType.Count;

            for(int i = 0; i < (int)InstanceType.Count; ++i)
            {
                int instanceNum = instanceNumInfo.GetInstanceNum((InstanceType)i);
                if(instanceIndex < instanceNum)
                {
                    instanceType = (InstanceType)i;
                    break;
                }
                instanceIndex -= instanceNum;
            }

            if(instanceType == InstanceType.Count)
                return InstanceHandle.Invalid;

            Assert.IsTrue(instanceIndex < instanceNumInfo.GetInstanceNum(instanceType));
            return InstanceHandle.Create(instanceIndex, instanceType);
        }

        public void CPUInstanceArrayToGPUInstanceArray(NativeArray<InstanceHandle> instances, NativeArray<GPUInstanceIndex> gpuInstanceIndices)
        {
            Assert.AreEqual(instances.Length, gpuInstanceIndices.Length);

            Profiling.Profiler.BeginSample("CPUInstanceArrayToGPUInstanceArray");

            new ConvertCPUInstancesToGPUInstancesJob { instancesNumPrefixSum = instancesNumPrefixSum, instances = instances, gpuInstanceIndices = gpuInstanceIndices }
                .Schedule(instances.Length, ConvertCPUInstancesToGPUInstancesJob.k_BatchSize).Complete();

            Profiling.Profiler.EndSample();
        }

        public void Dispose()
        {
            if(instancesSpan.IsCreated)
                instancesSpan.Dispose();

            if(instancesNumPrefixSum.IsCreated)
                instancesNumPrefixSum.Dispose();

            if (descriptions.IsCreated)
                descriptions.Dispose();

            if (defaultMetadata.IsCreated)
                defaultMetadata.Dispose();

            if (gpuBufferComponentAddress.IsCreated)
                gpuBufferComponentAddress.Dispose();

            if (nameToMetadataMap.IsCreated)
                nameToMetadataMap.Dispose();

            if (gpuBuffer != null)
                gpuBuffer.Release();

            if (validComponentsIndicesGpuBuffer != null)
                validComponentsIndicesGpuBuffer.Release();

            if (componentAddressesGpuBuffer != null)
                componentAddressesGpuBuffer.Release();

            if (componentInstanceIndexRangesGpuBuffer != null)
                componentInstanceIndexRangesGpuBuffer.Release();

            if (componentByteCountsGpuBuffer != null)
                componentByteCountsGpuBuffer.Release();
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        internal readonly struct ReadOnly
        {
            private readonly NativeArray<int> instancesNumPrefixSum;

            public ReadOnly(GPUInstanceDataBuffer buffer)
            {
                instancesNumPrefixSum = buffer.instancesNumPrefixSum;
            }

            public GPUInstanceIndex CPUInstanceToGPUInstance(InstanceHandle instance)
            {
                return GPUInstanceDataBuffer.CPUInstanceToGPUInstance(instancesNumPrefixSum, instance);
            }

            public void CPUInstanceArrayToGPUInstanceArray(NativeArray<InstanceHandle> instances, NativeArray<GPUInstanceIndex> gpuInstanceIndices)
            {
                Assert.AreEqual(instances.Length, gpuInstanceIndices.Length);

                Profiling.Profiler.BeginSample("CPUInstanceArrayToGPUInstanceArray");

                new ConvertCPUInstancesToGPUInstancesJob { instancesNumPrefixSum = instancesNumPrefixSum, instances = instances, gpuInstanceIndices = gpuInstanceIndices }
                    .Schedule(instances.Length, ConvertCPUInstancesToGPUInstancesJob.k_BatchSize).Complete();

                Profiling.Profiler.EndSample();
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        struct ConvertCPUInstancesToGPUInstancesJob : IJobParallelFor
        {
            public const int k_BatchSize = 512;

            [ReadOnly] public NativeArray<int> instancesNumPrefixSum;
            [ReadOnly] public NativeArray<InstanceHandle> instances;

            [WriteOnly] public NativeArray<GPUInstanceIndex> gpuInstanceIndices;

            public void Execute(int index)
            {
                gpuInstanceIndices[index] = CPUInstanceToGPUInstance(instancesNumPrefixSum, instances[index]);
            }
        }
    }
}
