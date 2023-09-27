using System;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal struct GPUInstanceComponentDesc
    {
        public int propertyID;
        public int byteSize;
        public bool isOverriden;
        public bool isPerInstance;

        public GPUInstanceComponentDesc(int inPropertyID, int inByteSize, bool inIsOverriden, bool inPerInstance)
        {
            propertyID = inPropertyID;
            byteSize = inByteSize;
            isOverriden = inIsOverriden;
            isPerInstance = inPerInstance;
        }
    }

    internal class GPUInstanceDataBuffer : IDisposable
    {
        private static int s_NextLayoutVersion = 0;
        public static int NextVersion() { return ++s_NextLayoutVersion; }

        public int maxInstances;
        public int byteSize;
        public int perInstanceComponentCount;
        public int version;
        public int layoutVersion;
        public GraphicsBuffer gpuBuffer;
        public GraphicsBuffer validComponentsIndicesGpuBuffer;
        public GraphicsBuffer componentAddressesGpuBuffer;
        public GraphicsBuffer componentByteCountsGpuBuffer;
        public NativeArray<GPUInstanceComponentDesc> descriptions;
        public NativeArray<MetadataValue> defaultMetadata;
        public NativeArray<int> gpuBufferComponentAddress;
        public NativeParallelHashMap<int, int> nameToMetadataMap;

        public bool valid => maxInstances > 0;

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

        public void Dispose()
        {
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

            if (componentByteCountsGpuBuffer != null)
                componentByteCountsGpuBuffer.Release();
        }
    }

    internal struct GPUInstanceDataBufferBuilder : IDisposable
    {
        NativeList<GPUInstanceComponentDesc> m_Components;

        MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
        {
            const uint kIsOverriddenBit = 0x80000000;
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
            };
        }

        public void AddComponent<T>(int propertyID, bool isOverriden, bool isPerInstance = true) where T : unmanaged
        {
            AddComponent(propertyID, isOverriden, UnsafeUtility.SizeOf<T>(), isPerInstance);
        }

        public void AddComponent(int propertyID, bool isOverriden, int byteSize, bool isPerInstance = true)
        {
            if (!m_Components.IsCreated)
                m_Components = new NativeList<GPUInstanceComponentDesc>(64, Allocator.Temp);

            m_Components.Add(new GPUInstanceComponentDesc(propertyID, byteSize, isOverriden, isPerInstance));
        }

        public GPUInstanceDataBuffer Build(int numberOfInstances)
        {
            int perInstanceComponentCounts = 0;
            var perInstanceComponentIndices = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentAddresses = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentByteSizes = new NativeArray<int>(m_Components.Length, Allocator.Temp);

            GPUInstanceDataBuffer newBuffer = new GPUInstanceDataBuffer();
            newBuffer.layoutVersion = GPUInstanceDataBuffer.NextVersion();
            newBuffer.version = 0;
            newBuffer.defaultMetadata = new NativeArray<MetadataValue>(m_Components.Length, Allocator.Persistent);
            newBuffer.descriptions = new NativeArray<GPUInstanceComponentDesc>(m_Components.Length, Allocator.Persistent);
            newBuffer.nameToMetadataMap = new NativeParallelHashMap<int, int>(m_Components.Length, Allocator.Persistent);
            newBuffer.gpuBufferComponentAddress = new NativeArray<int>(m_Components.Length, Allocator.Persistent);
            //Initial offset, must be 0, 0, 0, 0.
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int byteOffset = 4 * vec4Size;

            for (int c = 0; c < m_Components.Length; ++c)
            {
                var componentDesc = m_Components[c];
                newBuffer.descriptions[c] = componentDesc;
                newBuffer.gpuBufferComponentAddress[c] = byteOffset;
                newBuffer.defaultMetadata[c] = CreateMetadataValue(componentDesc.propertyID, byteOffset, componentDesc.isOverriden);
                int componentByteSize = componentDesc.byteSize * (componentDesc.isPerInstance ? numberOfInstances : 1);
                componentAddresses[c] = byteOffset;
                componentByteSizes[c] = componentDesc.byteSize;
                byteOffset += componentByteSize;
                bool addedToMap = newBuffer.nameToMetadataMap.TryAdd(componentDesc.propertyID, c);
                Assert.IsTrue(addedToMap, "Repetitive metadata element added to object.");
                if (componentDesc.isPerInstance)
                {
                    perInstanceComponentIndices[perInstanceComponentCounts] = c;
                    perInstanceComponentCounts++;
                }
            }

            newBuffer.maxInstances = numberOfInstances;
            newBuffer.byteSize = byteOffset;
            newBuffer.gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newBuffer.byteSize / 4, 4);
            var headerData = new Vector4[4]
            {
                new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
            };

            newBuffer.gpuBuffer.SetData(headerData, 0, 0, 4);
            newBuffer.validComponentsIndicesGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, perInstanceComponentCounts, 4);
            newBuffer.validComponentsIndicesGpuBuffer.SetData(perInstanceComponentIndices, 0, 0, perInstanceComponentCounts);

            newBuffer.componentAddressesGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_Components.Length, 4);
            newBuffer.componentAddressesGpuBuffer.SetData(componentAddresses, 0, 0, m_Components.Length);

            newBuffer.componentByteCountsGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_Components.Length, 4);
            newBuffer.componentByteCountsGpuBuffer.SetData(componentByteSizes, 0, 0, m_Components.Length);
            newBuffer.perInstanceComponentCount = perInstanceComponentCounts;
            perInstanceComponentIndices.Dispose();
            componentAddresses.Dispose();
            componentByteSizes.Dispose();
            return newBuffer;
        }

        public void Dispose()
        {
            if (m_Components.IsCreated)
                m_Components.Dispose();
        }
    }
    internal struct GPUInstanceDataBufferUploader : IDisposable
    {
        private static class UploadKernelIDs
        {
            public static readonly int _InputValidComponentCounts = Shader.PropertyToID("_InputValidComponentCounts");
            public static readonly int _InputInstanceCounts = Shader.PropertyToID("_InputInstanceCounts");
            public static readonly int _InputInstanceByteSize = Shader.PropertyToID("_InputInstanceByteSize");
            public static readonly int _InputComponentOffsets = Shader.PropertyToID("_InputComponentOffsets");
            public static readonly int _InputInstanceData = Shader.PropertyToID("_InputInstanceData");
            public static readonly int _InputInstanceIndices = Shader.PropertyToID("_InputInstanceIndices");
            public static readonly int _InputValidComponentIndices = Shader.PropertyToID("_InputValidComponentIndices");
            public static readonly int _InputComponentAddresses = Shader.PropertyToID("_InputComponentAddresses");
            public static readonly int _InputComponentByteCounts = Shader.PropertyToID("_InputComponentByteCounts");
            public static readonly int _OutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }

        public struct GPUResources : IDisposable
        {
            public ComputeBuffer instanceData;
            public ComputeBuffer instanceIndices;
            public ComputeBuffer inputComponentOffsets;
            public ComputeShader cs;
            public int kernelId;

            private int m_InstanceDataByteSize;
            private int m_InstanceCount;
            private int m_ComponentCounts;

            public void LoadShaders(GPUResidentDrawerResources resources)
            {
                if (cs == null)
                {
                    cs = resources.instanceDataBufferUploadKernels;
                    kernelId = cs.FindKernel("MainUploadScatterInstances");
                }
            }

            public void CreateResources(int newInstanceCount, int sizePerInstance, int newComponentCounts)
            {
                int newInstanceDataByteSize = newInstanceCount * sizePerInstance;
                if (newInstanceDataByteSize > m_InstanceDataByteSize || instanceData == null)
                {
                    if (instanceData != null)
                        instanceData.Release();

                    instanceData = new ComputeBuffer((newInstanceDataByteSize + 3) / 4, 4, ComputeBufferType.Raw);
                    m_InstanceDataByteSize = newInstanceDataByteSize;
                }

                if (newInstanceCount > m_InstanceCount || instanceIndices == null)
                {
                    if (instanceIndices != null)
                        instanceIndices.Release();

                    instanceIndices = new ComputeBuffer(newInstanceCount, 4, ComputeBufferType.Raw);
                    m_InstanceCount = newInstanceCount;
                }

                if (newComponentCounts > m_ComponentCounts || inputComponentOffsets == null)
                {
                    if (inputComponentOffsets != null)
                        inputComponentOffsets.Release();

                    inputComponentOffsets = new ComputeBuffer(newComponentCounts, 4, ComputeBufferType.Raw);
                    m_ComponentCounts = newComponentCounts;
                }
            }

            public void Dispose()
            {
                cs = null;

                if (instanceData != null)
                    instanceData.Release();

                if (instanceIndices != null)
                    instanceIndices.Release();

                if (inputComponentOffsets != null)
                    inputComponentOffsets.Release();
            }
        }

        int m_UintPerInstance;
        int m_Capacity;
        int m_InstanceCount;
        NativeArray<bool> m_ComponentIsInstanced;
        NativeArray<int> m_ComponentDataIndex;
        NativeArray<int> m_DescriptionsUintSize;
        NativeArray<uint> m_TmpDataBuffer;

        public GPUInstanceDataBufferUploader(in NativeArray<GPUInstanceComponentDesc> descriptions, int capacity)
        {
            m_Capacity = capacity;
            m_InstanceCount = 0;
            m_UintPerInstance = 0;
            m_ComponentDataIndex = new NativeArray<int>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_ComponentIsInstanced = new NativeArray<bool>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_DescriptionsUintSize = new NativeArray<int>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int uintSize = UnsafeUtility.SizeOf<uint>();
            for (int c = 0; c < descriptions.Length; ++c)
            {
                var componentDesc = descriptions[c];
                m_ComponentIsInstanced[c] = componentDesc.isPerInstance;
                m_ComponentDataIndex[c] = m_UintPerInstance;
                m_DescriptionsUintSize[c] = descriptions[c].byteSize / uintSize;
                m_UintPerInstance += componentDesc.isPerInstance ? (componentDesc.byteSize / uintSize) : 0;
            }

            m_TmpDataBuffer = new NativeArray<uint>(m_Capacity * m_UintPerInstance, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        public unsafe void AllocateInstanceHandles(NativeArray<InstanceHandle> instances)
        {
            // No need to preallocate instances anymore, as those are passed as parameters to SubmitToGPU to avoid data duplication
            // We just set the instance count here to ensure that a) we have the correct capacity and b) write/gatherInstanceData copies the correct amount
            Assert.IsTrue(m_Capacity >= instances.m_Length);
            m_InstanceCount = instances.m_Length;
        }

        public unsafe void WriteInstanceData<T>(int parameterIndex, NativeArray<T> instanceData) where T : unmanaged
        {
            var dummyArray = new NativeArray<InstanceHandle>(0, Allocator.TempJob);
            GatherInstanceData(parameterIndex, dummyArray, instanceData);
            dummyArray.Dispose();
        }

        public unsafe void GatherInstanceData<T>(int parameterIndex, NativeArray<InstanceHandle> gatherIndices, NativeArray<T> instanceData) where T : unmanaged
        {
            if (m_InstanceCount == 0)
                return;

            var gatherData = gatherIndices.Length != 0;
            Assert.IsTrue(gatherData || instanceData.Length == m_InstanceCount);
            Assert.IsTrue(!gatherData || gatherIndices.Length == m_InstanceCount);
            Assert.IsTrue(UnsafeUtility.SizeOf<T>() >= UnsafeUtility.SizeOf<uint>());

            new WriteInstanceDataParameterJob
            {
                gatherData = gatherData,
                gatherHandles = gatherIndices,
                parameterIndex = parameterIndex,
                uintPerParameter = UnsafeUtility.SizeOf<T>() / UnsafeUtility.SizeOf<uint>(),
                uintPerInstance = m_UintPerInstance,
                componentIsInstanced = m_ComponentIsInstanced,
                componentDataIndex = m_ComponentDataIndex,
                descriptionsUintSize = m_DescriptionsUintSize,
                instanceCount = m_InstanceCount,
                instanceData = instanceData.Reinterpret<uint>(UnsafeUtility.SizeOf<T>()),
                tmpDataBuffer = m_TmpDataBuffer
            }.Run();
        }

        public void SubmitToGpu(GPUInstanceDataBuffer instanceDataBuffer, NativeArray<InstanceHandle> instancesScatterIndices, ref GPUResources gpuResources)
        {
            if (m_InstanceCount == 0)
                return;

            Assert.IsTrue(instancesScatterIndices.Length == m_InstanceCount);

            ++instanceDataBuffer.version;
            int uintSize = UnsafeUtility.SizeOf<uint>();
            int instanceByteSize = m_UintPerInstance * uintSize;
            gpuResources.CreateResources(m_InstanceCount, instanceByteSize, m_ComponentDataIndex.Length);
            var instanceIndicesBuffer = instancesScatterIndices.Reinterpret<int>(UnsafeUtility.SizeOf<InstanceHandle>());
            gpuResources.instanceData.SetData(m_TmpDataBuffer, 0, 0, m_InstanceCount * m_UintPerInstance);
            gpuResources.instanceIndices.SetData(instanceIndicesBuffer, 0, 0, m_InstanceCount);
            gpuResources.inputComponentOffsets.SetData(m_ComponentDataIndex, 0, 0, m_ComponentDataIndex.Length);
            gpuResources.cs.SetInt(UploadKernelIDs._InputValidComponentCounts, instanceDataBuffer.perInstanceComponentCount);
            gpuResources.cs.SetInt(UploadKernelIDs._InputInstanceCounts, m_InstanceCount);
            gpuResources.cs.SetInt(UploadKernelIDs._InputInstanceByteSize, instanceByteSize);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputInstanceData, gpuResources.instanceData);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputInstanceIndices, gpuResources.instanceIndices);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputComponentOffsets, gpuResources.inputComponentOffsets);
            Shader.SetGlobalBuffer(UploadKernelIDs._InputValidComponentIndices, instanceDataBuffer.validComponentsIndicesGpuBuffer);
            Shader.SetGlobalBuffer(UploadKernelIDs._InputComponentAddresses, instanceDataBuffer.componentAddressesGpuBuffer);
            Shader.SetGlobalBuffer(UploadKernelIDs._InputComponentByteCounts, instanceDataBuffer.componentByteCountsGpuBuffer);
            Shader.SetGlobalBuffer(UploadKernelIDs._OutputBuffer, instanceDataBuffer.gpuBuffer);
            gpuResources.cs.Dispatch(gpuResources.kernelId, (m_InstanceCount + 63) / 64, 1, 1);
            m_InstanceCount = 0;
        }

        public void Dispose()
        {
            if (m_ComponentDataIndex.IsCreated)
                m_ComponentDataIndex.Dispose();

            if (m_ComponentIsInstanced.IsCreated)
                m_ComponentIsInstanced.Dispose();

            if (m_DescriptionsUintSize.IsCreated)
                m_DescriptionsUintSize.Dispose();

            if (m_TmpDataBuffer.IsCreated)
                m_TmpDataBuffer.Dispose();

        }
    }

    [BurstCompile(DisableSafetyChecks = true)]
    internal struct WriteInstanceDataParameterJob : IJob
    {
        [ReadOnly] public bool gatherData;
        [ReadOnly] public int instanceCount;
        [ReadOnly] public int parameterIndex;
        [ReadOnly] public int uintPerParameter;
        [ReadOnly] public int uintPerInstance;
        [ReadOnly] public NativeArray<bool> componentIsInstanced;
        [ReadOnly] public NativeArray<int> componentDataIndex;
        [ReadOnly] public NativeArray<int> descriptionsUintSize;
        [ReadOnly] public NativeArray<InstanceHandle> gatherHandles;
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeArray<uint> instanceData;

        public NativeArray<uint> tmpDataBuffer;

        public unsafe void Execute()
        {
            Assert.IsTrue(componentIsInstanced[parameterIndex], "Component is non instanced. Can only call this function on parameters that are for all instances.");
            Assert.IsTrue(uintPerParameter == descriptionsUintSize[parameterIndex], "Parameter to write is incompatible, must be same stride as destination.");
            Assert.IsTrue(parameterIndex >= 0 && parameterIndex < componentDataIndex.Length, "Parameter index invalid.");

            int uintSize = UnsafeUtility.SizeOf<uint>();

            for (int i = 0; i < instanceCount; ++i)
            {
                Assert.IsTrue(i * uintPerInstance < tmpDataBuffer.Length, "Trying to write to an instance buffer out of bounds.");

                int dataOffset = (gatherData ? gatherHandles[i].index : i) * uintPerParameter;
                Assert.IsTrue(dataOffset < instanceData.Length);

                uint* data = (uint*)instanceData.GetUnsafePtr() + dataOffset;
                UnsafeUtility.MemCpy((uint*)tmpDataBuffer.GetUnsafePtr() + i * uintPerInstance + componentDataIndex[parameterIndex], data, uintPerParameter * uintSize);
            }
        }
    }

    internal struct GPUInstanceDataBufferGrower : IDisposable
    {
        private static class CopyInstancesKernelIDs
        {
            public static readonly int _InputValidComponentCounts = Shader.PropertyToID("_InputValidComponentCounts");
            public static readonly int _InstanceCounts = Shader.PropertyToID("_InstanceCounts");
            public static readonly int _ValidComponentIndices = Shader.PropertyToID("_ValidComponentIndices");
            public static readonly int _ComponentByteCounts = Shader.PropertyToID("_ComponentByteCounts");
            public static readonly int _InputComponentAddresses = Shader.PropertyToID("_InputComponentAddresses");
            public static readonly int _OutputComponentAddresses = Shader.PropertyToID("_OutputComponentAddresses");
            public static readonly int _InputBuffer = Shader.PropertyToID("_InputBuffer");
            public static readonly int _OutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }

        public struct GPUResources : IDisposable
        {
            public ComputeShader cs;
            public int kernelId;

            public void LoadShaders(GPUResidentDrawerResources resources)
            {
                if (cs == null)
                {
                    cs = resources.instanceDataBufferCopyKernels;
                    kernelId = cs.FindKernel("MainCopyInstances");
                }
            }

            public void CreateResources()
            {
            }

            public void Dispose()
            {
                cs = null;
            }
        }

        private GPUInstanceDataBuffer m_SrcBuffer;
        private GPUInstanceDataBuffer m_DstBuffer;

        public GPUInstanceDataBufferGrower(GPUInstanceDataBuffer sourceBuffer, int newInstanceCount)
        {
            m_SrcBuffer = sourceBuffer;
            m_DstBuffer = null;
            if (newInstanceCount < sourceBuffer.maxInstances)
                return;

            GPUInstanceDataBufferBuilder builder = new GPUInstanceDataBufferBuilder();
            foreach (GPUInstanceComponentDesc descriptor in sourceBuffer.descriptions)
                builder.AddComponent(descriptor.propertyID, descriptor.isOverriden, descriptor.byteSize, descriptor.isPerInstance);

            m_DstBuffer = builder.Build(newInstanceCount);
            builder.Dispose();
        }

        public GPUInstanceDataBuffer SubmitToGpu(ref GPUResources gpuResources)
        {
            if (m_DstBuffer == null)
                return m_SrcBuffer;

            Assert.IsTrue(m_SrcBuffer.perInstanceComponentCount == m_DstBuffer.perInstanceComponentCount);
            gpuResources.CreateResources();
            gpuResources.cs.SetInt(CopyInstancesKernelIDs._InputValidComponentCounts, m_SrcBuffer.perInstanceComponentCount);
            gpuResources.cs.SetInt(CopyInstancesKernelIDs._InstanceCounts, m_SrcBuffer.maxInstances);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._ValidComponentIndices, m_SrcBuffer.validComponentsIndicesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._ComponentByteCounts, m_SrcBuffer.componentByteCountsGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._InputComponentAddresses, m_SrcBuffer.componentAddressesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._OutputComponentAddresses, m_DstBuffer.componentAddressesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._InputBuffer, m_SrcBuffer.gpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._OutputBuffer, m_DstBuffer.gpuBuffer);
            gpuResources.cs.Dispatch(gpuResources.kernelId, (m_SrcBuffer.maxInstances + 63) / 64, 1, 1);
            return m_DstBuffer;
        }

        public void Dispose()
        {
        }
    }
}
