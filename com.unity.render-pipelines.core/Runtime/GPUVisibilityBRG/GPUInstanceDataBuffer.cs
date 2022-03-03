using System;
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
            Assert.IsTrue((inByteSize % UnsafeUtility.SizeOf<Vector4>()) == 0, "Component size must be a multiple of Vec4 type.");
        }
    }

    internal struct GPUInstanceDataBuffer : IDisposable
    {
        public int maxInstances;
        public int byteSize;
        public int perInstanceComponentCount;
        public GraphicsBuffer gpuBuffer;
        public GraphicsBuffer validComponentsIndicesGpuBuffer;
        public GraphicsBuffer componentAddressesGpuBuffer;
        public GraphicsBuffer componentByteCountsGpuBuffer;
        public NativeArray<GPUInstanceComponentDesc> descriptions;
        public NativeArray<MetadataValue> batchMetadata;
        public NativeArray<int> gpuBufferComponentAddress;
        public NativeHashMap<int, int> nameToMetadataMap;

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

        public void SubmitParamter<T>(int propertyIndex, T value) where T : unmanaged
        {
            if (!descriptions[propertyIndex].isPerInstance)
                throw new Exception("Cannot set a parameter that is per instance. For per instance parameters use a GPUInstanceDataBufferUploader");

            int byteSize = UnsafeUtility.SizeOf<T>();
            if (byteSize != descriptions[propertyIndex].byteSize)
                throw new Exception("Parameter does not match destination expected stride");

            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int vec4Index = gpuBufferComponentAddress[propertyIndex] / vec4Size;
            var val = new NativeArray<T>(1, Allocator.Temp);
            gpuBuffer.SetData(val, 0, vec4Index, 1);
            val.Dispose();
        }

        public void Dispose()
        {
            if (descriptions.IsCreated)
                descriptions.Dispose();

            if (batchMetadata.IsCreated)
                batchMetadata.Dispose();

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

        public static class DefaultSchema
        {
            public static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int unity_SpecCube0_HDR = Shader.PropertyToID("unity_SpecCube0_HDR");
            public static readonly int unity_SHAr = Shader.PropertyToID("unity_SHAr");
            public static readonly int unity_SHAg = Shader.PropertyToID("unity_SHAg");
            public static readonly int unity_SHAb = Shader.PropertyToID("unity_SHAb");
            public static readonly int unity_SHBr = Shader.PropertyToID("unity_SHBr");
            public static readonly int unity_SHBg = Shader.PropertyToID("unity_SHBg");
            public static readonly int unity_SHBb = Shader.PropertyToID("unity_SHBb");
            public static readonly int unity_SHC = Shader.PropertyToID("unity_SHC");
            public static readonly int unity_ProbesOcclusion = Shader.PropertyToID("unity_ProbesOcclusion");
            public static readonly int unity_LightmapIndex = Shader.PropertyToID("unity_LightmapIndex");
            public static readonly int unity_LightmapST = Shader.PropertyToID("unity_LightmapST");
            public static readonly int unity_ObjectToWorld = Shader.PropertyToID("unity_ObjectToWorld");
            public static readonly int unity_WorldToObject = Shader.PropertyToID("unity_WorldToObject");
            public static readonly int _DeferredMaterialInstanceData = Shader.PropertyToID("_DeferredMaterialInstanceData");
        }

        public static GPUInstanceDataBuffer CreateDefaultInstanceBuffer(int instanceCount)
        {
            using (var builder = new GPUInstanceDataBufferBuilder())
            {
                builder.AddComponent<Vector4>(DefaultSchema._BaseColor, isOverriden: false, isPerInstance: false);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SpecCube0_HDR, isOverriden: false, isPerInstance: false);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHAr, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHAg, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHAb, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHBr, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHBg, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHBb, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_SHC, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_ProbesOcclusion, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_LightmapIndex, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema.unity_LightmapST, isOverriden: true, isPerInstance: true);
                builder.AddComponent<BRGMatrix>(DefaultSchema.unity_ObjectToWorld, isOverriden: true, isPerInstance: true);
                builder.AddComponent<BRGMatrix>(DefaultSchema.unity_WorldToObject, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(DefaultSchema._DeferredMaterialInstanceData, isOverriden: true, isPerInstance: true);
                return builder.Build(instanceCount);
            }
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
            if (!m_Components.IsCreated)
                m_Components = new NativeList<GPUInstanceComponentDesc>(64, Allocator.Temp);

            int byteSize = UnsafeUtility.SizeOf<T>();
            m_Components.Add(new GPUInstanceComponentDesc(propertyID, byteSize, isOverriden, isPerInstance));
        }

        public GPUInstanceDataBuffer Build(int numberOfInstances)
        {
            int perInstanceComponentCounts = 0;
            var perInstanceComponentIndices = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentAddresses = new NativeArray<int>(m_Components.Length, Allocator.Temp);
            var componentByteSizes = new NativeArray<int>(m_Components.Length, Allocator.Temp);

            GPUInstanceDataBuffer newBuffer = new GPUInstanceDataBuffer();
            newBuffer.batchMetadata = new NativeArray<MetadataValue>(m_Components.Length, Allocator.Persistent);
            newBuffer.descriptions = new NativeArray<GPUInstanceComponentDesc>(m_Components.Length, Allocator.Persistent);
            newBuffer.nameToMetadataMap = new NativeHashMap<int, int>(m_Components.Length, Allocator.Persistent);
            newBuffer.gpuBufferComponentAddress = new NativeArray<int>(m_Components.Length, Allocator.Persistent);
            //Initial offset, must be 0, 0, 0, 0.
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int byteOffset = 4 * vec4Size;

            for (int c = 0; c < m_Components.Length; ++c)
            {
                var componentDesc = m_Components[c];
                newBuffer.descriptions[c] = componentDesc;
                newBuffer.gpuBufferComponentAddress[c] = byteOffset;
                newBuffer.batchMetadata[c] = CreateMetadataValue(componentDesc.propertyID, byteOffset, componentDesc.isOverriden);
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

    internal struct GPUInstanceDataUploadHandle
    {
        public int index;
        public static GPUInstanceDataUploadHandle Invalid = new GPUInstanceDataUploadHandle() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(GPUInstanceDataUploadHandle other) => index == other.index;
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
            public static readonly int _InputBigBufferValidComponentIndices = Shader.PropertyToID("_InputBigBufferValidComponentIndices");
            public static readonly int _InputBigBufferComponentAddresses = Shader.PropertyToID("_InputBigBufferComponentAddresses");
            public static readonly int _InputBigBufferComponentByteCounts = Shader.PropertyToID("_InputBigBufferComponentByteCounts");
            public static readonly int _OutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }

        public struct GPUResources : IDisposable
        {
            public CommandBuffer cmdBuffer;
            public ComputeBuffer instanceData;
            public ComputeBuffer instanceIndices;
            public ComputeBuffer inputComponentOffsets;
            public ComputeShader cs;
            public int kernelId;

            private int m_InstanceDataByteSize;
            private int m_InstanceCount;
            private int m_ComponentCounts;

            public void CreateResources(int newInstanceCount, int sizePerInstance, int newComponentCounts)
            {
                if (cs == null)
                {
                    cs = (ComputeShader)Resources.Load("BigBufferUploadKernels");
                    kernelId = cs.FindKernel("MainUploadScatterInstances");
                }

                if (cmdBuffer == null)
                {
                    cmdBuffer = new CommandBuffer();
                }

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
                if (cs != null)
                    cs = null;

                if (cmdBuffer != null)
                    cmdBuffer.Release();

                if (instanceData != null)
                    instanceData.Release();

                if (instanceIndices != null)
                    instanceIndices.Release();

                if (inputComponentOffsets != null)
                    inputComponentOffsets.Release();
            }
        }

        int m_V4sPerInstance;
        int m_Capacity;
        int m_InstanceCount;
        GPUInstanceDataBuffer m_BigBuffer;
        NativeArray<bool> m_ComponentIsInstanced;
        NativeArray<int> m_ComponentDataIndex;
        NativeArray<Vector4> m_TmpDataBuffer;
        NativeArray<int> m_InstanceIndices;

        public GPUInstanceDataBufferUploader(GPUInstanceDataBuffer bigBuffer)
        {
            m_V4sPerInstance = 0;
            m_BigBuffer = bigBuffer;
            m_ComponentDataIndex = new NativeArray<int>(bigBuffer.descriptions.Length, Allocator.Temp);
            m_ComponentIsInstanced = new NativeArray<bool>(bigBuffer.descriptions.Length, Allocator.Temp);
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            for (int c = 0; c < bigBuffer.descriptions.Length; ++c)
            {
                var componentDesc = bigBuffer.descriptions[c];
                m_ComponentIsInstanced[c] = componentDesc.isPerInstance;
                m_ComponentDataIndex[c] = m_V4sPerInstance;
                m_V4sPerInstance += componentDesc.isPerInstance ? (componentDesc.byteSize / vec4Size) : 0;
            }

            m_InstanceCount = 0;
            m_Capacity = 1024;
            m_TmpDataBuffer = new NativeArray<Vector4>(m_Capacity * m_V4sPerInstance, Allocator.Temp);
            m_InstanceIndices = new NativeArray<int>(m_Capacity, Allocator.Temp);
        }

        public GPUInstanceDataUploadHandle AllocateInstance(int instanceIndex)
        {
            if (m_Capacity == 0)
                throw new Exception("Utilize the big buffer constructor to create the uploader");
            GPUInstanceDataUploadHandle handle = new GPUInstanceDataUploadHandle() { index = m_InstanceCount };
            m_InstanceCount++;
            if (m_Capacity == m_InstanceCount)
            {
                m_Capacity *= 2;
                m_TmpDataBuffer.ResizeArray(m_Capacity * m_V4sPerInstance);
                m_InstanceIndices.ResizeArray(m_Capacity);
            }

            m_InstanceIndices[handle.index] = instanceIndex;
            return handle;
        }

        unsafe public void WriteParameter(GPUInstanceDataUploadHandle instanceHandle, int parameterIndex, Vector4* data, int dataCounts)
        {
            if (instanceHandle.index * m_V4sPerInstance >= m_TmpDataBuffer.Length)
                throw new Exception("Trying to write to an instance buffer out of bounds");

            if (parameterIndex >= m_ComponentDataIndex.Length)
                throw new Exception("Parameter index invalid");

            if (!m_ComponentIsInstanced[parameterIndex])
                throw new Exception("Component is non instanced. Can only call this function on paramters that are for all instances");

            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            if (dataCounts != m_BigBuffer.descriptions[parameterIndex].byteSize / vec4Size)
                throw new Exception("Parameter to write is incompatible, must be same stride as destination");

            UnsafeUtility.MemCpy((Vector4*)m_TmpDataBuffer.GetUnsafePtr<Vector4>() + instanceHandle.index * m_V4sPerInstance + m_ComponentDataIndex[parameterIndex], data, dataCounts * vec4Size);
        }

        public void WriteParameter<T>(GPUInstanceDataUploadHandle instanceIndex, int parameterIndex, T value) where T : unmanaged
        {
            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int sz = UnsafeUtility.SizeOf<T>();
            unsafe { WriteParameter(instanceIndex, parameterIndex, (Vector4*)&value, sz / vec4Size); }
        }

        public void SubmitToGpu(ref GPUResources gpuResources)
        {
            if (m_InstanceCount == 0)
                return;

            int vec4Size = UnsafeUtility.SizeOf<Vector4>();
            int instanceByteSize = m_V4sPerInstance * vec4Size;
            gpuResources.CreateResources(m_InstanceCount, instanceByteSize, m_ComponentDataIndex.Length);

            gpuResources.cmdBuffer.Clear();
            gpuResources.cmdBuffer.SetBufferData(gpuResources.instanceData, m_TmpDataBuffer, 0, 0, m_InstanceCount * m_V4sPerInstance);
            gpuResources.cmdBuffer.SetBufferData(gpuResources.instanceIndices, m_InstanceIndices, 0, 0, m_InstanceCount);
            gpuResources.cmdBuffer.SetBufferData(gpuResources.inputComponentOffsets, m_ComponentDataIndex, 0, 0, m_ComponentDataIndex.Length);
            gpuResources.cmdBuffer.SetComputeIntParam(gpuResources.cs, UploadKernelIDs._InputValidComponentCounts, m_BigBuffer.perInstanceComponentCount);
            gpuResources.cmdBuffer.SetComputeIntParam(gpuResources.cs, UploadKernelIDs._InputInstanceCounts, m_InstanceCount);
            gpuResources.cmdBuffer.SetComputeIntParam(gpuResources.cs, UploadKernelIDs._InputInstanceByteSize, instanceByteSize);
            gpuResources.cmdBuffer.SetComputeBufferParam(gpuResources.cs, gpuResources.kernelId, UploadKernelIDs._InputInstanceData, gpuResources.instanceData);
            gpuResources.cmdBuffer.SetComputeBufferParam(gpuResources.cs, gpuResources.kernelId, UploadKernelIDs._InputInstanceIndices, gpuResources.instanceIndices);
            gpuResources.cmdBuffer.SetComputeBufferParam(gpuResources.cs, gpuResources.kernelId, UploadKernelIDs._InputComponentOffsets, gpuResources.inputComponentOffsets);
            gpuResources.cmdBuffer.SetGlobalBuffer(UploadKernelIDs._InputBigBufferValidComponentIndices, m_BigBuffer.validComponentsIndicesGpuBuffer);
            gpuResources.cmdBuffer.SetGlobalBuffer(UploadKernelIDs._InputBigBufferComponentAddresses, m_BigBuffer.componentAddressesGpuBuffer);
            gpuResources.cmdBuffer.SetGlobalBuffer(UploadKernelIDs._InputBigBufferComponentByteCounts, m_BigBuffer.componentByteCountsGpuBuffer);
            gpuResources.cmdBuffer.SetGlobalBuffer(UploadKernelIDs._OutputBuffer, m_BigBuffer.gpuBuffer);
            gpuResources.cmdBuffer.DispatchCompute(gpuResources.cs, gpuResources.kernelId, (m_InstanceCount + 63) / 64, 1, 1);
            Graphics.ExecuteCommandBuffer(gpuResources.cmdBuffer);
        }

        public void Dispose()
        {
            if (m_ComponentDataIndex.IsCreated)
                m_ComponentDataIndex.Dispose();

            if (m_ComponentIsInstanced.IsCreated)
                m_ComponentIsInstanced.Dispose();

            if (m_TmpDataBuffer.IsCreated)
                m_TmpDataBuffer.Dispose();

            if (m_InstanceIndices.IsCreated)
                m_InstanceIndices.Dispose();
        }
    }

}
