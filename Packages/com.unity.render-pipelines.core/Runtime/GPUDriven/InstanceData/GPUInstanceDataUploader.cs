using Unity.Burst;
using Unity.Collections;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System;

namespace UnityEngine.Rendering
{
    internal struct GPUInstanceDataBufferBuilder : IDisposable
    {
        private NativeList<GPUInstanceComponentDesc> m_Components;

        private MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
        {
            const uint kIsOverriddenBit = 0x80000000;
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
            };
        }

        public void AddComponent<T>(int propertyID, bool isOverriden, bool isPerInstance, InstanceType instanceType, InstanceComponentGroup componentGroup = InstanceComponentGroup.Default) where T : unmanaged
        {
            AddComponent(propertyID, isOverriden, UnsafeUtility.SizeOf<T>(), isPerInstance, instanceType, componentGroup);
        }

        public void AddComponent(int propertyID, bool isOverriden, int byteSize, bool isPerInstance, InstanceType instanceType, InstanceComponentGroup componentGroup)
        {
            if (!m_Components.IsCreated)
                m_Components = new NativeList<GPUInstanceComponentDesc>(64, Allocator.Temp);

            if (m_Components.Length > 0)
                Assert.IsTrue(m_Components[m_Components.Length - 1].instanceType <= instanceType, "Added components must be sorted by InstanceType for better memory layout.");

            m_Components.Add(new GPUInstanceComponentDesc(propertyID, byteSize, isOverriden, isPerInstance, instanceType, componentGroup));
        }

        public unsafe GPUInstanceDataBuffer Build(in InstanceNumInfo instanceNumInfo)
        {
            int perInstanceComponentCounts = 0;
            var perInstanceComponentIndices = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentAddresses = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentByteSizes = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentInstanceIndexRanges = new NativeArray<Vector2Int>(m_Components.Length, Allocator.Temp);

            GPUInstanceDataBuffer newBuffer = new GPUInstanceDataBuffer();
            newBuffer.instanceNumInfo = instanceNumInfo;
            newBuffer.instancesNumPrefixSum = new NativeArray<int>((int)InstanceType.Count, Allocator.Persistent);
            newBuffer.instancesSpan = new NativeArray<int>((int)InstanceType.Count, Allocator.Persistent);

            int sum = 0;

            for (int i = 0; i < (int)InstanceType.Count; ++i)
            {
                newBuffer.instancesNumPrefixSum[i] = sum;
                sum += instanceNumInfo.InstanceNums[i];
                newBuffer.instancesSpan[i] = instanceNumInfo.GetInstanceNumIncludingChildren((InstanceType)i);
            }

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

                int instancesBegin = newBuffer.instancesNumPrefixSum[(int)componentDesc.instanceType];
                int instancesEnd = instancesBegin + newBuffer.instancesSpan[(int)componentDesc.instanceType];
                int instancesNum = componentDesc.isPerInstance ? instancesEnd - instancesBegin : 1;
                Assert.IsTrue(instancesNum >= 0);

                componentInstanceIndexRanges[c] = new Vector2Int(instancesBegin, instancesBegin + instancesNum);

                int componentGPUAddress = byteOffset - instancesBegin * componentDesc.byteSize;
                Assert.IsTrue(componentGPUAddress >= 0, "GPUInstanceDataBufferBuilder: GPU address is negative. This is not supported for now. See kIsOverriddenBit." +
                    "In general, if there is only one root InstanceType (MeshRenderer in our case) with a component that is larger or equal in size than any component in a derived InstanceType." +
                    "And the number of parent gpu instances are always larger or equal to the number of derived type gpu instances. Than GPU address cannot become negative.");

                newBuffer.gpuBufferComponentAddress[c] = componentGPUAddress;
                newBuffer.defaultMetadata[c] = CreateMetadataValue(componentDesc.propertyID, componentGPUAddress, componentDesc.isOverriden);

                componentAddresses[c] = componentGPUAddress;
                componentByteSizes[c] = componentDesc.byteSize;

                int componentByteSize = componentDesc.byteSize * instancesNum;
                byteOffset += componentByteSize;

                bool addedToMap = newBuffer.nameToMetadataMap.TryAdd(componentDesc.propertyID, c); 
                Assert.IsTrue(addedToMap, "Repetitive metadata element added to object.");

                if (componentDesc.isPerInstance)
                {
                    perInstanceComponentIndices[perInstanceComponentCounts] = c;
                    perInstanceComponentCounts++;
                }
            }

            newBuffer.byteSize = byteOffset;
            newBuffer.gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newBuffer.byteSize / 4, 4);
            newBuffer.gpuBuffer.SetData(new NativeArray<Vector4>(4, Allocator.Temp), 0, 0, 4);
            newBuffer.validComponentsIndicesGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, perInstanceComponentCounts, 4);
            newBuffer.validComponentsIndicesGpuBuffer.SetData(perInstanceComponentIndices, 0, 0, perInstanceComponentCounts);
            newBuffer.componentAddressesGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_Components.Length, 4);
            newBuffer.componentAddressesGpuBuffer.SetData(componentAddresses, 0, 0, m_Components.Length);
            newBuffer.componentInstanceIndexRangesGpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_Components.Length, 8);
            newBuffer.componentInstanceIndexRangesGpuBuffer.SetData(componentInstanceIndexRanges, 0, 0, m_Components.Length);
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
            public static readonly int _InputComponentInstanceIndexRanges = Shader.PropertyToID("_InputComponentInstanceIndexRanges");
            public static readonly int _OutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }

        public struct GPUResources : IDisposable
        {
            public ComputeBuffer instanceData;
            public ComputeBuffer instanceIndices;
            public ComputeBuffer inputComponentOffsets;
            public ComputeBuffer validComponentIndices;
            public ComputeShader cs;
            public int kernelId;

            private int m_InstanceDataByteSize;
            private int m_InstanceCount;
            private int m_ComponentCounts;
            private int m_ValidComponentIndicesCount;

            public void LoadShaders(GPUResidentDrawerResources resources)
            {
                if (cs == null)
                {
                    cs = resources.instanceDataBufferUploadKernels;
                    kernelId = cs.FindKernel("MainUploadScatterInstances");
                }
            }

            public void CreateResources(int newInstanceCount, int sizePerInstance, int newComponentCounts, int validComponentIndicesCount)
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

                if (validComponentIndicesCount > m_ValidComponentIndicesCount || validComponentIndices == null)
                {
                    if (validComponentIndices != null)
                        validComponentIndices.Release();

                    validComponentIndices = new ComputeBuffer(validComponentIndicesCount, 4, ComputeBufferType.Raw);
                    m_ValidComponentIndicesCount = validComponentIndicesCount;
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

                if(validComponentIndices != null)
                    validComponentIndices.Release();
            }
        }

        int m_UintPerInstance;
        int m_Capacity;
        int m_InstanceCount;
        NativeArray<bool> m_ComponentIsInstanced;
        NativeArray<int> m_ComponentDataIndex;
        NativeArray<int> m_DescriptionsUintSize;
        NativeArray<uint> m_TmpDataBuffer;
        NativeList<int> m_WritenComponentIndices;

        private NativeArray<int> m_DummyArray;

        public GPUInstanceDataBufferUploader(in NativeArray<GPUInstanceComponentDesc> descriptions, int capacity, InstanceType instanceType)
        {
            m_Capacity = capacity;
            m_InstanceCount = 0;
            m_UintPerInstance = 0;
            m_ComponentDataIndex = new NativeArray<int>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_ComponentIsInstanced = new NativeArray<bool>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_DescriptionsUintSize = new NativeArray<int>(descriptions.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_WritenComponentIndices = new NativeList<int>(descriptions.Length, Allocator.TempJob);
            m_DummyArray = new NativeArray<int>(0, Allocator.Persistent);

            int uintSize = UnsafeUtility.SizeOf<uint>();

            for (int c = 0; c < descriptions.Length; ++c)
            {
                var componentDesc = descriptions[c];
                m_ComponentIsInstanced[c] = componentDesc.isPerInstance;
                if(componentDesc.instanceType == instanceType)
                {
                    m_ComponentDataIndex[c] = m_UintPerInstance;
                    m_DescriptionsUintSize[c] = descriptions[c].byteSize / uintSize;
                    m_UintPerInstance += componentDesc.isPerInstance ? (componentDesc.byteSize / uintSize) : 0;
                }
                else
                {
                    m_ComponentDataIndex[c] = -1;
                    m_DescriptionsUintSize[c] = 0;
                }
            }

            m_TmpDataBuffer = new NativeArray<uint>(m_Capacity * m_UintPerInstance, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        public unsafe IntPtr GetUploadBufferPtr()
        {
            Assert.IsTrue(m_TmpDataBuffer.IsCreated);
            Assert.IsTrue(m_TmpDataBuffer.Length > 0 && m_InstanceCount > 0);
            return new IntPtr(m_TmpDataBuffer.GetUnsafePtr());
        }

        public int GetUIntPerInstance()
        {
            return m_UintPerInstance;
        }

        public int GetParamUIntOffset(int parameterIndex)
        {
            Assert.IsTrue(m_ComponentIsInstanced[parameterIndex], "Component is non instanced. Can only call this function on parameters that are for all instances.");
            Assert.IsTrue(parameterIndex >= 0 && parameterIndex < m_ComponentDataIndex.Length, "Parameter index invalid.");
            Assert.IsTrue(m_ComponentDataIndex[parameterIndex] != -1, "Parameter index is not allocated. Did you allocate proper InstanceType parameters?");
            return m_ComponentDataIndex[parameterIndex];
        }

        public int PrepareParamWrite<T>(int parameterIndex) where T : unmanaged
        {
            int uintPerParameter = UnsafeUtility.SizeOf<T>() / UnsafeUtility.SizeOf<uint>();
            Assert.IsTrue(uintPerParameter == m_DescriptionsUintSize[parameterIndex], "Parameter to write is incompatible, must be same stride as destination.");
            if (!m_WritenComponentIndices.Contains(parameterIndex))
                m_WritenComponentIndices.Add(parameterIndex);
            return GetParamUIntOffset(parameterIndex);
        }

        public unsafe void AllocateUploadHandles(int handlesLength)
        {
            // No need to preallocate instances anymore, as those are passed as parameters to SubmitToGPU to avoid data duplication
            // We just set the instance count here to ensure that a) we have the correct capacity and b) write/gatherInstanceData copies the correct amount
            Assert.IsTrue(m_Capacity >= handlesLength);
            m_InstanceCount = handlesLength;
        }

        public unsafe JobHandle WriteInstanceDataJob<T>(int parameterIndex, NativeArray<T> instanceData) where T : unmanaged
        {
            return WriteInstanceDataJob(parameterIndex, instanceData, m_DummyArray);
        }

        public unsafe JobHandle WriteInstanceDataJob<T>(int parameterIndex, NativeArray<T> instanceData, NativeArray<int> gatherIndices) where T : unmanaged
        {
            if (m_InstanceCount == 0)
                return default;

            var gatherData = gatherIndices.Length != 0;
            Assert.IsTrue(gatherData || instanceData.Length == m_InstanceCount);
            Assert.IsTrue(!gatherData || gatherIndices.Length == m_InstanceCount);
            Assert.IsTrue(UnsafeUtility.SizeOf<T>() >= UnsafeUtility.SizeOf<uint>());

            int uintPerParameter = UnsafeUtility.SizeOf<T>() / UnsafeUtility.SizeOf<uint>();
            Assert.IsTrue(m_ComponentIsInstanced[parameterIndex], "Component is non instanced. Can only call this function on parameters that are for all instances.");
            Assert.IsTrue(uintPerParameter == m_DescriptionsUintSize[parameterIndex], "Parameter to write is incompatible, must be same stride as destination.");
            Assert.IsTrue(parameterIndex >= 0 && parameterIndex < m_ComponentDataIndex.Length, "Parameter index invalid.");
            Assert.IsTrue(m_ComponentDataIndex[parameterIndex] != -1, "Parameter index is not allocated. Did you allocate proper InstanceType parameters?");

            if (!m_WritenComponentIndices.Contains(parameterIndex))
                m_WritenComponentIndices.Add(parameterIndex);

            var writeJob = new WriteInstanceDataParameterJob
            {
                gatherData = gatherData,
                gatherIndices = gatherIndices,
                parameterIndex = parameterIndex,
                uintPerParameter = uintPerParameter,
                uintPerInstance = m_UintPerInstance,
                componentDataIndex = m_ComponentDataIndex,
                instanceData = instanceData.Reinterpret<uint>(UnsafeUtility.SizeOf<T>()),
                tmpDataBuffer = m_TmpDataBuffer
            };

            return writeJob.Schedule(m_InstanceCount, WriteInstanceDataParameterJob.k_BatchSize);
        }

        public void SubmitToGpu(GPUInstanceDataBuffer instanceDataBuffer, NativeArray<GPUInstanceIndex> gpuInstanceIndices, ref GPUResources gpuResources, bool submitOnlyWrittenParams)
        {
            if (m_InstanceCount == 0)
                return;

            Assert.IsTrue(gpuInstanceIndices.Length == m_InstanceCount);

            ++instanceDataBuffer.version;
            int uintSize = UnsafeUtility.SizeOf<uint>();
            int instanceByteSize = m_UintPerInstance * uintSize;
            gpuResources.CreateResources(m_InstanceCount, instanceByteSize, m_ComponentDataIndex.Length, m_WritenComponentIndices.Length);
            gpuResources.instanceData.SetData(m_TmpDataBuffer, 0, 0, m_InstanceCount * m_UintPerInstance);
            gpuResources.instanceIndices.SetData(gpuInstanceIndices, 0, 0, m_InstanceCount);
            gpuResources.inputComponentOffsets.SetData(m_ComponentDataIndex, 0, 0, m_ComponentDataIndex.Length);
            gpuResources.cs.SetInt(UploadKernelIDs._InputInstanceCounts, m_InstanceCount);
            gpuResources.cs.SetInt(UploadKernelIDs._InputInstanceByteSize, instanceByteSize);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputInstanceData, gpuResources.instanceData);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputInstanceIndices, gpuResources.instanceIndices);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputComponentOffsets, gpuResources.inputComponentOffsets);
            if (submitOnlyWrittenParams)
            {
                gpuResources.validComponentIndices.SetData(m_WritenComponentIndices.AsArray(), 0, 0, m_WritenComponentIndices.Length);
                gpuResources.cs.SetInt(UploadKernelIDs._InputValidComponentCounts, m_WritenComponentIndices.Length);
                gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputValidComponentIndices, gpuResources.validComponentIndices);
            }
            else
            {
                gpuResources.cs.SetInt(UploadKernelIDs._InputValidComponentCounts, instanceDataBuffer.perInstanceComponentCount);
                gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputValidComponentIndices, instanceDataBuffer.validComponentsIndicesGpuBuffer);
            }
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputComponentAddresses, instanceDataBuffer.componentAddressesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputComponentByteCounts, instanceDataBuffer.componentByteCountsGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._InputComponentInstanceIndexRanges, instanceDataBuffer.componentInstanceIndexRangesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, UploadKernelIDs._OutputBuffer, instanceDataBuffer.gpuBuffer);
            gpuResources.cs.Dispatch(gpuResources.kernelId, (m_InstanceCount + 63) / 64, 1, 1);

            m_InstanceCount = 0;
            m_WritenComponentIndices.Clear();
        }

        public void SubmitToGpu(GPUInstanceDataBuffer instanceDataBuffer, NativeArray<InstanceHandle> instances, ref GPUResources gpuResources, bool submitOnlyWrittenParams)
        {
            if (m_InstanceCount == 0)
                return;

            var gpuInstanceIndices = new NativeArray<GPUInstanceIndex>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            instanceDataBuffer.CPUInstanceArrayToGPUInstanceArray(instances, gpuInstanceIndices);

            SubmitToGpu(instanceDataBuffer, gpuInstanceIndices, ref gpuResources, submitOnlyWrittenParams);

            gpuInstanceIndices.Dispose();
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

            if (m_WritenComponentIndices.IsCreated)
                m_WritenComponentIndices.Dispose();

            if(m_DummyArray.IsCreated)
                m_DummyArray.Dispose();
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct WriteInstanceDataParameterJob : IJobParallelFor
        {
            public const int k_BatchSize = 512;

            [ReadOnly] public bool gatherData;
            [ReadOnly] public int parameterIndex;
            [ReadOnly] public int uintPerParameter;
            [ReadOnly] public int uintPerInstance;
            [ReadOnly] public NativeArray<int> componentDataIndex;
            [ReadOnly] public NativeArray<int> gatherIndices;
            [NativeDisableContainerSafetyRestriction, NoAlias][ReadOnly] public NativeArray<uint> instanceData;

            [NativeDisableContainerSafetyRestriction, NoAlias][WriteOnly] public NativeArray<uint> tmpDataBuffer;

            public unsafe void Execute(int index)
            {
                Assert.IsTrue(index * uintPerInstance < tmpDataBuffer.Length, "Trying to write to an instance buffer out of bounds.");

                int dataOffset = (gatherData ? gatherIndices[index] : index) * uintPerParameter;
                Assert.IsTrue(dataOffset < instanceData.Length);

                int uintSize = UnsafeUtility.SizeOf<uint>();

                uint* data = (uint*)instanceData.GetUnsafePtr() + dataOffset;
                UnsafeUtility.MemCpy((uint*)tmpDataBuffer.GetUnsafePtr() + index * uintPerInstance + componentDataIndex[parameterIndex], data,
                    uintPerParameter * uintSize);
            }
        }
    }

    internal struct GPUInstanceDataBufferGrower : IDisposable
    {
        private static class CopyInstancesKernelIDs
        {
            public static readonly int _InputValidComponentCounts = Shader.PropertyToID("_InputValidComponentCounts");
            public static readonly int _InstanceCounts = Shader.PropertyToID("_InstanceCounts");
            public static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");
            public static readonly int _OutputInstanceOffset = Shader.PropertyToID("_OutputInstanceOffset");
            public static readonly int _ValidComponentIndices = Shader.PropertyToID("_ValidComponentIndices");
            public static readonly int _ComponentByteCounts = Shader.PropertyToID("_ComponentByteCounts");
            public static readonly int _InputComponentAddresses = Shader.PropertyToID("_InputComponentAddresses");
            public static readonly int _OutputComponentAddresses = Shader.PropertyToID("_OutputComponentAddresses");
            public static readonly int _InputComponentInstanceIndexRanges = Shader.PropertyToID("_InputComponentInstanceIndexRanges");
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

        //@ We should implement buffer shrinker too, otherwise lots of instances can be allocated for trees for example
        //@ while there are no trees in scenes that are in use at all.
        public unsafe GPUInstanceDataBufferGrower(GPUInstanceDataBuffer sourceBuffer, in InstanceNumInfo instanceNumInfo)
        {
            m_SrcBuffer = sourceBuffer;
            m_DstBuffer = null;

            bool needToGrow = false;

            for(int i = 0; i < (int)InstanceType.Count; ++i)
            {
                Assert.IsTrue(instanceNumInfo.InstanceNums[i] >= sourceBuffer.instanceNumInfo.InstanceNums[i], "Shrinking GPU instance buffer is not supported yet.");

                if (instanceNumInfo.InstanceNums[i] > sourceBuffer.instanceNumInfo.InstanceNums[i])
                    needToGrow = true;
            }

            if (!needToGrow)
                return;

            GPUInstanceDataBufferBuilder builder = new GPUInstanceDataBufferBuilder();

            foreach (GPUInstanceComponentDesc descriptor in sourceBuffer.descriptions)
                builder.AddComponent(descriptor.propertyID, descriptor.isOverriden, descriptor.byteSize, descriptor.isPerInstance, descriptor.instanceType, descriptor.componentGroup);

            m_DstBuffer = builder.Build(instanceNumInfo);
            builder.Dispose();
        }

        public GPUInstanceDataBuffer SubmitToGpu(ref GPUResources gpuResources)
        {
            if (m_DstBuffer == null)
                return m_SrcBuffer;

            int totalInstanceCount = m_SrcBuffer.instanceNumInfo.GetTotalInstanceNum();

            if(totalInstanceCount == 0)
                return m_DstBuffer;

            Assert.IsTrue(m_SrcBuffer.perInstanceComponentCount == m_DstBuffer.perInstanceComponentCount);

            gpuResources.CreateResources();
            gpuResources.cs.SetInt(CopyInstancesKernelIDs._InputValidComponentCounts, m_SrcBuffer.perInstanceComponentCount);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._ValidComponentIndices, m_SrcBuffer.validComponentsIndicesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._ComponentByteCounts, m_SrcBuffer.componentByteCountsGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._InputComponentAddresses, m_SrcBuffer.componentAddressesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._InputComponentInstanceIndexRanges, m_SrcBuffer.componentInstanceIndexRangesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._OutputComponentAddresses, m_DstBuffer.componentAddressesGpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._InputBuffer, m_SrcBuffer.gpuBuffer);
            gpuResources.cs.SetBuffer(gpuResources.kernelId, CopyInstancesKernelIDs._OutputBuffer, m_DstBuffer.gpuBuffer);

            //@ We could compute new instance indices on CPU and do one dispatch.
            //@ Otherwise in theory these multiple dispatches could overlap with no UAV barrier between them as they write to a different parts of the UAV.
            //@ Need to profile which is better.
            for(int i = 0; i < (int)InstanceType.Count; ++i)
            {
                int instanceCount = m_SrcBuffer.instanceNumInfo.GetInstanceNum((InstanceType)i);

                if(instanceCount > 0)
                {
                    int instanceOffset = m_SrcBuffer.instancesNumPrefixSum[i];
                    int outputInstanceOffset = m_DstBuffer.instancesNumPrefixSum[i];
                    gpuResources.cs.SetInt(CopyInstancesKernelIDs._InstanceCounts, instanceCount);
                    gpuResources.cs.SetInt(CopyInstancesKernelIDs._InstanceOffset, instanceOffset);
                    gpuResources.cs.SetInt(CopyInstancesKernelIDs._OutputInstanceOffset, outputInstanceOffset);
                    gpuResources.cs.Dispatch(gpuResources.kernelId, (instanceCount + 63) / 64, 1, 1);
                }
            }

            return m_DstBuffer;
        }

        public void Dispose()
        {
        }
    }
}
