using System;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false)]
    enum GPUInstanceDataBufferConstants
    {
        ThreadGroupSize = 128,
        UIntPerThread = 16,
        UIntPerThreadGroup = ThreadGroupSize * UIntPerThread,
        MaxThreadGroupsPerDispatch = 65535,
    }

    // GPU Buffer layout looks like this:
    // Archetype0 { Component0, Component1 }
    // Archetype1 { Component0, Component1, Component2 }
    // Archetype2 { Component0, Component2, Component3 }
    // See GPU Archetype PR for details https://github.cds.internal.unity3d.com/unity/unity/pull/50000
    internal class GPUInstanceDataBuffer : IDisposable
    {
        //@ For now 1Gb but this is should be smaller for OpenGL ES 3.1
        public const int MaxGPUInstancDataBufferSize = 1024 * 1024 * 1024;

        internal const int InvalidIndex = -1;

        private const int ThreadGroupSize = (int)GPUInstanceDataBufferConstants.ThreadGroupSize;
        private const int UIntPerThread = (int)GPUInstanceDataBufferConstants.UIntPerThread;
        private const int UIntPerThreadGroup = (int)GPUInstanceDataBufferConstants.UIntPerThreadGroup;
        private const int MaxThreadGroupsPerDispatch = (int)GPUInstanceDataBufferConstants.MaxThreadGroupsPerDispatch;

        private int m_MainUploadScatterInstancesKernelID;
        private int m_MainCopyInstancesKernelID;

        private InternalGPUInstanceDataBuffer m_InternalBuffer;
        private int m_LayoutVersion = 0;

        private uint3 m_UploaKernelThreadGroupSize;
        private ComputeShader m_InstanceDataBufferUploadKernels;
        private ComputeShader m_InstanceDataBufferCopyKernels;
        private GraphicsBuffer m_InputInstanceIndicesBuffer;
        private GraphicsBuffer m_InputComponentAddressesBuffer;
        private GraphicsBuffer m_OutputComponentIndicesBuffer;

        public NativeArray<int>.ReadOnly componentPerInstance => m_InternalBuffer.componentPerInstance.AsReadOnly();
        public NativeArray<int>.ReadOnly componentsGPUAddress => m_InternalBuffer.componentsGPUAddress.AsReadOnly();
        public NativeArray<int>.ReadOnly componentByteSizes => m_InternalBuffer.componentByteSizes.AsReadOnly();
        public NativeArray<int2>.ReadOnly componentInstanceIndexRanges => m_InternalBuffer.componentInstanceIndexRanges.AsReadOnly();
        public NativeArray<MetadataValue>.ReadOnly componentsMetadata => m_InternalBuffer.componentsMetadata.AsReadOnly();
        public NativeArray<GPUComponentHandle>.ReadOnly components => m_InternalBuffer.components.AsReadOnly();
        public int gpuBufferByteSize => m_InternalBuffer.gpuBufferByteSize;
        public GraphicsBuffer nativeBuffer => m_InternalBuffer.gpuBuffer;
        public int layoutVersion => m_LayoutVersion;

        public GPUInstanceDataBuffer(ref GPUArchetypeManager archetypeManager, in GPUInstanceDataBufferLayout layout, GPUResidentDrawerResources resources)
        {
            m_InstanceDataBufferUploadKernels = resources.instanceDataBufferUploadKernels;
            m_InstanceDataBufferCopyKernels = resources.instanceDataBufferCopyKernels;
            m_InternalBuffer = new InternalGPUInstanceDataBuffer(ref archetypeManager, layout);

            m_MainUploadScatterInstancesKernelID = m_InstanceDataBufferUploadKernels.FindKernel("MainUploadScatterInstances");
            m_MainCopyInstancesKernelID = m_InstanceDataBufferCopyKernels.FindKernel("MainCopyInstances");
            Assert.IsTrue(m_MainUploadScatterInstancesKernelID != -1, "Unable to load MainUploadScatterInstances compute shader kernel.");
            Assert.IsTrue(m_MainCopyInstancesKernelID != -1, "Unable to load MainCopyInstances compute shader kernel.");

            m_InstanceDataBufferUploadKernels.GetKernelThreadGroupSizes(m_MainUploadScatterInstancesKernelID,
                out m_UploaKernelThreadGroupSize.x,
                out m_UploaKernelThreadGroupSize.y,
                out m_UploaKernelThreadGroupSize.z);
        }

        public void Dispose()
        {
            m_InputInstanceIndicesBuffer?.Release();
            m_InputComponentAddressesBuffer?.Release();
            m_OutputComponentIndicesBuffer?.Release();
            m_InternalBuffer.Dispose();
            m_InternalBuffer = null;
        }

        private static MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isPerInstance)
        {
            // See UnityDOTSInstancing.hlsl
            const uint kPerInstanceDataBit = 0x80000000;
            return new MetadataValue { NameID = nameID, Value = (uint)gpuAddress | (isPerInstance ? kPerInstanceDataBit : 0) };
        }

        public ComponentIndex GetComponentIndex(GPUComponentHandle component)
        {
            int componentIndex = m_InternalBuffer.FindComponentIndex(component);
            Assert.IsTrue(componentIndex != InvalidIndex, "Component is not allocated.");
            return new ComponentIndex(componentIndex, m_LayoutVersion);
        }

        public int GetComponentGPUAddress(ComponentIndex compIndex)
        {
            Assert.IsTrue(compIndex.layoutVersion == m_LayoutVersion, "Component index was acquired from previous layout. Update component index.");
            return m_InternalBuffer.componentsGPUAddress[compIndex.index];
        }

        public int GetComponentGPUAddress(GPUComponentHandle component)
        {
            ComponentIndex compIndex = GetComponentIndex(component);
            return m_InternalBuffer.componentsGPUAddress[compIndex.index];
        }

        public int GetComponentGPUUIntOffset(GPUComponentHandle component)
        {
            return GetComponentGPUAddress(component) / UnsafeUtility.SizeOf<uint>();
        }

        public bool IsArchetypeAllocated(GPUArchetypeHandle archetype)
        {
            return m_InternalBuffer.FindArchetypeIndex(archetype) != InvalidIndex;
        }

        public ArchetypeIndex GetArchetypeIndex(GPUArchetypeHandle archetype)
        {
            int archetypeIndex = m_InternalBuffer.FindArchetypeIndex(archetype);
            Assert.IsTrue(archetypeIndex != InvalidIndex, "Archetype is not allocated.");
            return new ArchetypeIndex(archetypeIndex, m_LayoutVersion);
        }

        public int GetInstancesCount(in ArchetypeIndex archIndex)
        {
            Assert.IsTrue(archIndex.layoutVersion == m_LayoutVersion, "Archetype index was acquired from previous layout. Update archetype index.");
            return m_InternalBuffer.layout.instancesCount[archIndex.index];
        }

        public GPUInstanceIndex InstanceToGPUIndex(in ArchetypeIndex archIndex, int instanceIndex)
        {
            Assert.IsTrue(archIndex.layoutVersion == m_LayoutVersion, "Archetype index was acquired from previous layout. Update archetype index.");
            int instancesBegin = m_InternalBuffer.instancesCountPrefixSum[archIndex.index];
            int instancesCount = m_InternalBuffer.layout.instancesCount[archIndex.index];
            Assert.IsTrue(instanceIndex >= 0 && instanceIndex < instancesCount);
            return GPUInstanceIndex.Create(instancesBegin + instanceIndex);
        }

        public void QueryInstanceGPUIndices(in RenderWorld renderWorld, NativeArray<InstanceHandle> instances, NativeArray<GPUInstanceIndex> gpuIndices)
        {
            Assert.AreEqual(instances.Length, gpuIndices.Length);

            Profiler.BeginSample("QueryInstanceGPUIndices");

            new InstancesToGPUIndicesJob
            {
                renderWorld = renderWorld,
                instancesCountPrefixSum = m_InternalBuffer.instancesCountPrefixSum,
                layout = m_InternalBuffer.layout,
                instances = instances,
                gpuIndices = gpuIndices
            }
            .RunParallel(instances.Length, 512);

            Profiler.EndSample();
        }

        public void UploadDataToGPU(CommandBuffer cmd, GraphicsBuffer uploadBuffer, in GPUInstanceUploadData uploadData, NativeArray<GPUInstanceIndex> scatterGPUIndices)
        {
            Assert.IsNotNull(cmd);

            if (uploadData.length == 0)
                return;

            Profiler.BeginSample("UploadGPUInstanceData");

            Assert.IsTrue(scatterGPUIndices.Length <= uploadData.length);

            var outputComponentIndices = new NativeArray<int>(uploadData.writtenComponents.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var inputComponentAddresses = new NativeArray<int>(uploadData.writtenComponents.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < uploadData.writtenComponents.Length; ++i)
            {
                outputComponentIndices[i] = m_InternalBuffer.FindComponentIndex(uploadData.writtenComponents[i]);
                Assert.AreNotEqual(outputComponentIndices[i], InvalidIndex);
                int inputComponentIndex = uploadData.FindComponentIndex(uploadData.writtenComponents[i]);
                inputComponentAddresses[i] = uploadData.componentGPUAddress[inputComponentIndex];
            }

            m_InputInstanceIndicesBuffer = EnsureBufferCountOrResize(m_InputInstanceIndicesBuffer, scatterGPUIndices.Length, UnsafeUtility.SizeOf<GPUInstanceIndex>());
            m_InputComponentAddressesBuffer = EnsureBufferCountOrResize(m_InputComponentAddressesBuffer, inputComponentAddresses.Length, sizeof(int));
            m_OutputComponentIndicesBuffer = EnsureBufferCountOrResize(m_OutputComponentIndicesBuffer, outputComponentIndices.Length, sizeof(int));

            m_InputInstanceIndicesBuffer.SetData(scatterGPUIndices);
            m_InputComponentAddressesBuffer.SetData(inputComponentAddresses);
            m_OutputComponentIndicesBuffer.SetData(outputComponentIndices);

            ComputeShader shader = m_InstanceDataBufferUploadKernels;
            cmd.SetComputeIntParam(shader, UploadKernelID.kInputComponentsCount, uploadData.writtenComponents.Length);
            cmd.SetComputeIntParam(shader, UploadKernelID.kInputInstancesCount, scatterGPUIndices.Length);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kInputInstanceData, uploadBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kInputInstanceIndices, m_InputInstanceIndicesBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kInputComponentAddresses, m_InputComponentAddressesBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputComponentByteCounts, m_InternalBuffer.componentByteCountsGPUBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputComponentIndices, m_OutputComponentIndicesBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputComponentInstanceIndexRanges, m_InternalBuffer.componentGPUInstanceIndexRangesGPUBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputComponentIsPerInstance, m_InternalBuffer.componentsPerInstanceGPUBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputComponentAddresses, m_InternalBuffer.componentsGPUAddressGPUBuffer);
            cmd.SetComputeBufferParam(shader, m_MainUploadScatterInstancesKernelID, UploadKernelID.kOutputBuffer, m_InternalBuffer.gpuBuffer);

            int threadGroupCountX = CoreUtils.DivRoundUp(scatterGPUIndices.Length, (int)m_UploaKernelThreadGroupSize.x);
            Assert.IsTrue(m_UploaKernelThreadGroupSize.y == 1);
            Assert.IsTrue(m_UploaKernelThreadGroupSize.z == 1);

            cmd.DispatchCompute(shader, m_MainUploadScatterInstancesKernelID, threadGroupCountX, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Profiler.EndSample();
        }

        public void SetGPULayout(CommandBuffer cmd, ref GPUArchetypeManager archetypeManager, in GPUInstanceDataBufferLayout newLayout, bool submitCmdBuffer)
        {
            if (submitCmdBuffer)
                Assert.IsNotNull(cmd);

            if (m_InternalBuffer.layout.Equals(newLayout))
                return;

            //@ If the GPU layout is smaller, we could perform the re-layout in place using a smaller scratch GPU helper buffer, instead of always allocating a new one.
            var newInternalBuffer = new InternalGPUInstanceDataBuffer(ref archetypeManager, newLayout);

            var inputComponentDataAddresses = new NativeList<int>(Allocator.Temp);
            var outputComponentDataAddresses = new NativeList<int>(Allocator.Temp);
            var outputComponentDataUIntSizes = new NativeList<int>(Allocator.Temp);
            var inputThreadGroupBeginIndices = new NativeList<int>(Allocator.Temp);
            var threadGroupsCount = 0;

            for (int newArchetypeIndex = 0; newArchetypeIndex < newInternalBuffer.layout.archetypes.Length; ++newArchetypeIndex)
            {
                var archetype = newInternalBuffer.layout.archetypes[newArchetypeIndex];
                int archetypeIndex = m_InternalBuffer.FindArchetypeIndex(archetype);

                if (archetypeIndex == InvalidIndex)
                    continue;

                var instancesBegin = m_InternalBuffer.instancesCountPrefixSum[archetypeIndex];
                var instancesCount = m_InternalBuffer.layout.instancesCount[archetypeIndex];
                var newInstancesBegin = newInternalBuffer.instancesCountPrefixSum[newArchetypeIndex];
                var newInstancesCount = newInternalBuffer.layout.instancesCount[newArchetypeIndex];

                if (instancesCount == 0 || newInstancesCount == 0)
                    continue;

                ref readonly GPUArchetypeDesc archetypeDesc = ref archetypeManager.GetArchetypeDesc(archetype);

                foreach (GPUComponentHandle component in archetypeDesc.components)
                {
                    int componentIndex = m_InternalBuffer.FindComponentIndex(component);
                    int newComponentIndex = newInternalBuffer.FindComponentIndex(component);
                    Assert.AreNotEqual(componentIndex, InvalidIndex);
                    Assert.AreNotEqual(newComponentIndex, InvalidIndex);

                    var newComponentArchetypeIndexSpan = newInternalBuffer.componentsArchetypeIndexSpan[newComponentIndex];
                    var newComponentInstanceIndexRange = newInternalBuffer.componentInstanceIndexRanges[newComponentIndex];
                    Assert.IsTrue(newArchetypeIndex >= newComponentArchetypeIndexSpan.x && newArchetypeIndex < newComponentArchetypeIndexSpan.y);
                    Assert.IsTrue(newInstancesBegin >= newComponentInstanceIndexRange.x && newInstancesBegin + newInstancesCount <= newComponentInstanceIndexRange.y);
                    Assert.AreEqual(newInternalBuffer.componentByteSizes[newComponentIndex], m_InternalBuffer.componentByteSizes[componentIndex]);

                    ref readonly GPUComponentDesc componentDesc = ref archetypeManager.GetComponentDesc(component);
                    int isPerInstance = componentDesc.isPerInstance ? 1 : 0;
                    int instancesToCopy = componentDesc.isPerInstance ? math.min(instancesCount, newInstancesCount) : 1;
                    int componentByteSize = newInternalBuffer.componentByteSizes[newComponentIndex];
                    int componentDataAddress = m_InternalBuffer.componentsGPUAddress[componentIndex] + instancesBegin * componentByteSize * isPerInstance;
                    int newCopmonentDataAddress = newInternalBuffer.componentsGPUAddress[newComponentIndex] + newInstancesBegin * componentByteSize * isPerInstance;
                    int componentDataUIntSize = (componentByteSize * instancesToCopy) / sizeof(uint);

                    int threadGroupBeginIndex = threadGroupsCount;
                    int threadGroups = CoreUtils.DivRoundUp(componentDataUIntSize, UIntPerThreadGroup);
                    threadGroupsCount += threadGroups;

                    inputComponentDataAddresses.Add(componentDataAddress);
                    outputComponentDataAddresses.Add(newCopmonentDataAddress);
                    outputComponentDataUIntSizes.Add(componentDataUIntSize);
                    inputThreadGroupBeginIndices.Add(threadGroupBeginIndex);
                }
            }

            inputThreadGroupBeginIndices.Add(threadGroupsCount);

            if (threadGroupsCount > 0)
            {
                inputThreadGroupBeginIndices.ResizeUninitialized(CollectionHelper.Align(inputThreadGroupBeginIndices.Length, sizeof(uint)));
                inputComponentDataAddresses.ResizeUninitialized(CollectionHelper.Align(inputComponentDataAddresses.Length, sizeof(uint)));
                outputComponentDataAddresses.ResizeUninitialized(CollectionHelper.Align(outputComponentDataAddresses.Length, sizeof(uint)));
                outputComponentDataUIntSizes.ResizeUninitialized(CollectionHelper.Align(outputComponentDataUIntSizes.Length, sizeof(uint)));

                var inputThreadGroupBeginIndicesGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, inputThreadGroupBeginIndices.Length, sizeof(uint));
                var inputComponentDataAddressesGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, inputComponentDataAddresses.Length, sizeof(uint));
                var outputComponentDataAddressesGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, outputComponentDataAddresses.Length, sizeof(uint));
                var outputComponentDataUIntSizesGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, outputComponentDataUIntSizes.Length, sizeof(uint));

                inputThreadGroupBeginIndicesGPUBuffer.SetData(inputThreadGroupBeginIndices.AsArray());
                inputComponentDataAddressesGPUBuffer.SetData(inputComponentDataAddresses.AsArray());
                outputComponentDataAddressesGPUBuffer.SetData(outputComponentDataAddresses.AsArray());
                outputComponentDataUIntSizesGPUBuffer.SetData(outputComponentDataUIntSizes.AsArray());

                var shader = m_InstanceDataBufferCopyKernels;
                cmd.SetComputeIntParam(shader, CopyKernelID.kInputComponentsCount, inputComponentDataAddresses.Length);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kInputThreadGroupBeginIndices, inputThreadGroupBeginIndicesGPUBuffer);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kInputComponentDataAddresses, inputComponentDataAddressesGPUBuffer);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kOutputComponentDataAddresses, outputComponentDataAddressesGPUBuffer);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kOutputComponentDataUIntSizes, outputComponentDataUIntSizesGPUBuffer);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kInputBuffer, m_InternalBuffer.gpuBuffer);
                cmd.SetComputeBufferParam(shader, m_MainCopyInstancesKernelID, CopyKernelID.kOutputBuffer, newInternalBuffer.gpuBuffer);
                int dispatchesCount = CoreUtils.DivRoundUp(threadGroupsCount, MaxThreadGroupsPerDispatch);
                int dispatchedThreadGroups = 0;
                for (int i = 0; i < dispatchesCount; ++i)
                {
                    int dispatchThreadGroupCount = math.min(threadGroupsCount - dispatchedThreadGroups, MaxThreadGroupsPerDispatch);
                    cmd.SetComputeIntParam(shader, CopyKernelID.kDispatchThreadGroupBase, dispatchedThreadGroups);
                    cmd.DispatchCompute(shader, m_MainCopyInstancesKernelID, dispatchThreadGroupCount, 1, 1);
                    dispatchedThreadGroups += dispatchThreadGroupCount;
                }
                Assert.AreEqual(dispatchedThreadGroups, threadGroupsCount);

                if (submitCmdBuffer)
                {
                    Graphics.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                inputThreadGroupBeginIndicesGPUBuffer.Release();
                inputComponentDataAddressesGPUBuffer.Release();
                outputComponentDataAddressesGPUBuffer.Release();
                outputComponentDataUIntSizesGPUBuffer.Release();
            }

            m_InternalBuffer.Dispose();
            m_InternalBuffer = newInternalBuffer;
            m_LayoutVersion += 1;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        static GraphicsBuffer EnsureBufferCountOrResize(GraphicsBuffer buffer, int requestedCount, int stride)
        {
            if (buffer == null || buffer.count < requestedCount)
            {
                int currentCount = 0;
                if (buffer != null)
                {
                    // Stride changes not handled
                    Assert.IsTrue(buffer.stride == stride);

                    currentCount = buffer.count;
                    buffer.Release();
                }

                // At least double on resize
                int newCount = math.max(currentCount * 2, requestedCount);
                buffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, newCount, stride);
            }

            return buffer;
        }

        internal readonly struct ReadOnly
        {
            public readonly GPUInstanceDataBufferLayout.ReadOnly layout;
            public readonly NativeArray<int>.ReadOnly componentsGPUAddress;
            public readonly NativeArray<int>.ReadOnly instancesCountPrefixSum;
            public readonly NativeArray<int>.ReadOnly componentIndices;

            public ReadOnly(GPUInstanceDataBuffer buffer)
            {
                layout = buffer.m_InternalBuffer.layout.AsReadOnly();
                componentsGPUAddress = buffer.m_InternalBuffer.componentsGPUAddress.AsReadOnly();
                instancesCountPrefixSum = buffer.m_InternalBuffer.instancesCountPrefixSum.AsReadOnly();
                componentIndices = buffer.m_InternalBuffer.componentIndices.AsReadOnly();
            }

            public int GetComponentIndex(GPUComponentHandle component)
            {
                Assert.IsTrue(component.valid);
                if (component.index < componentIndices.Length)
                    return componentIndices[component.index];

                Assert.IsTrue(false, "Component is not allocated.");
                return InvalidIndex;
            }

            public int GetComponentGPUAddress(GPUComponentHandle component)
            {
                Assert.IsTrue(component.valid && component.index < componentIndices.Length);
                return componentsGPUAddress[componentIndices[component.index]];
            }

            public GPUInstanceIndex InstanceGPUHandleToGPUIndex(InstanceGPUHandle gpuHandle)
            {
                Assert.IsTrue(gpuHandle.isValid);
                int archetypeIndex = layout.FindArchetypeIndex(gpuHandle.archetype);
                int instancesBegin = instancesCountPrefixSum[archetypeIndex];
                int instancesCount = layout.instancesCount[archetypeIndex];
                Assert.IsTrue(gpuHandle.archetypeInstanceIndex >= 0 && gpuHandle.archetypeInstanceIndex < instancesCount);
                return GPUInstanceIndex.Create(instancesBegin + gpuHandle.archetypeInstanceIndex);
            }
        }

        internal class InternalGPUInstanceDataBuffer
        {
            public const int kComponentAddressAlignment = 32 * 4;

            internal readonly GPUInstanceDataBufferLayout layout;

            internal readonly NativeList<GPUComponentHandle> components;
            internal readonly NativeList<int2> componentsArchetypeIndexSpan;
            internal readonly NativeArray<MetadataValue> componentsMetadata;
            internal readonly NativeArray<int> componentsGPUAddress;
            internal readonly NativeArray<int> componentPerInstance;
            internal readonly NativeArray<int> componentByteSizes;
            internal readonly NativeArray<int2> componentInstanceIndexRanges;
            internal readonly NativeArray<int> componentIndices;

            internal readonly NativeArray<int> instancesCountPrefixSum;

            internal readonly int gpuBufferByteSize;
            internal readonly GraphicsBuffer gpuBuffer;
            internal readonly GraphicsBuffer componentsPerInstanceGPUBuffer;
            internal readonly GraphicsBuffer componentsGPUAddressGPUBuffer;
            internal readonly GraphicsBuffer componentGPUInstanceIndexRangesGPUBuffer;
            internal readonly GraphicsBuffer componentByteCountsGPUBuffer;

            public InternalGPUInstanceDataBuffer(ref GPUArchetypeManager archetypeManager, in GPUInstanceDataBufferLayout layout)
            {
                this.layout = new GPUInstanceDataBufferLayout(layout, Allocator.Persistent);

                components = new NativeList<GPUComponentHandle>(Allocator.Persistent);
                componentsArchetypeIndexSpan = new NativeList<int2>(Allocator.Persistent);
                componentIndices = new NativeArray<int>(archetypeManager.GetComponentsCount(), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                componentIndices.FillArray(InvalidIndex);

                instancesCountPrefixSum = new NativeArray<int>(layout.archetypes.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                int instancesCountSum = 0;

                for (int i = 0; i < layout.archetypes.Length; ++i)
                {
                    instancesCountPrefixSum[i] = instancesCountSum;
                    instancesCountSum += layout.instancesCount[i];

                    ref readonly GPUArchetypeDesc archetypeDesc = ref archetypeManager.GetArchetypeDesc(layout.archetypes[i]);

                    for (int j = 0; j < archetypeDesc.components.Length; ++j)
                    {
                        GPUComponentHandle component = archetypeDesc.components[j];
                        Assert.IsTrue(component.valid);

                        int compIndex = componentIndices[component.index];

                        if (compIndex == InvalidIndex)
                        {
                            compIndex = components.Length;
                            components.Add(component);
                            componentsArchetypeIndexSpan.Add(new int2(i, i + 1));
                            componentIndices[component.index] = compIndex;
                        }
                        else
                        {
                            int2 archetypeSpan = componentsArchetypeIndexSpan[compIndex];
                            archetypeSpan.x = math.min(archetypeSpan.x, i);
                            archetypeSpan.y = math.max(archetypeSpan.y, i + 1);
                            componentsArchetypeIndexSpan[compIndex] = archetypeSpan;
                        }
                    }
                }

                Assert.AreEqual(components.Length, componentsArchetypeIndexSpan.Length);

                componentsMetadata = new NativeArray<MetadataValue>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                componentsGPUAddress = new NativeArray<int>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                componentPerInstance = new NativeArray<int>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                componentByteSizes = new NativeArray<int>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                componentInstanceIndexRanges = new NativeArray<int2>(components.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Initial offset, must be {0, 0, 0, 0} for BatchRendererGroup header.
                int byteOffset = 4 * UnsafeUtility.SizeOf<Vector4>();

                for (int compIndex = 0; compIndex < components.Length; ++compIndex)
                {
                    ref readonly GPUComponentDesc componentDesc = ref archetypeManager.GetComponentDesc(components[compIndex]);
                    int2 archetypeSpan = componentsArchetypeIndexSpan[compIndex];
                    Assert.IsTrue(archetypeSpan.y > archetypeSpan.x);

                    int firstArchetypeIndex = archetypeSpan.x;
                    int lastArchetypeIndex = archetypeSpan.y - 1;

                    int instancesBegin = instancesCountPrefixSum[firstArchetypeIndex];
                    int instancesEnd = instancesCountPrefixSum[lastArchetypeIndex] + layout.instancesCount[lastArchetypeIndex];
                    int instancesNum = componentDesc.isPerInstance ? instancesEnd - instancesBegin : 1;
                    Assert.IsTrue(instancesNum >= 0);

                    byteOffset = CollectionHelper.Align(byteOffset, kComponentAddressAlignment);

                    int componentGPUAddress;
                    // Offset for the address of the component when it does not start at the zero instance index.
                    int componentInstanceOffset = instancesBegin * componentDesc.byteSize;
                    //@ In the future, for OpenGL ES 3.1 support, make sure that an instance data element doesn't cross 16KB boundary.
                    if (componentDesc.isPerInstance)
                        componentGPUAddress = byteOffset - componentInstanceOffset;
                    else
                        componentGPUAddress = byteOffset;
                    // GPU component address should not become negative. This generally should not happen often. See 'kIsOverriddenBit'.
                    if (componentGPUAddress < 0)
                    {
                        byteOffset = CollectionHelper.Align(byteOffset + Math.Abs(componentGPUAddress), kComponentAddressAlignment);
                        componentGPUAddress = byteOffset - componentInstanceOffset;
                    }

                    componentsGPUAddress[compIndex] = componentGPUAddress;
                    componentsMetadata[compIndex] = CreateMetadataValue(componentDesc.propertyID, componentGPUAddress, componentDesc.isPerInstance);

                    componentPerInstance[compIndex] = componentDesc.isPerInstance ? 1 : 0;
                    componentInstanceIndexRanges[compIndex] = new int2(instancesBegin, instancesEnd);
                    componentByteSizes[compIndex] = componentDesc.byteSize;

                    int componentTotalByteSize = componentDesc.byteSize * instancesNum;
                    byteOffset += componentTotalByteSize;
                }

                gpuBufferByteSize = byteOffset;

                Assert.IsTrue((gpuBufferByteSize % sizeof(uint)) == 0 && gpuBufferByteSize <= MaxGPUInstancDataBufferSize);

                gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, gpuBufferByteSize / sizeof(uint), sizeof(uint));
                gpuBuffer.SetData(new NativeArray<Vector4>(4, Allocator.Temp), 0, 0, 4);
                componentsPerInstanceGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, components.Length, sizeof(int));
                componentsPerInstanceGPUBuffer.SetData(componentPerInstance, 0, 0, components.Length);
                componentsGPUAddressGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, components.Length, sizeof(int));
                componentsGPUAddressGPUBuffer.SetData(componentsGPUAddress, 0, 0, components.Length);
                componentGPUInstanceIndexRangesGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, components.Length, UnsafeUtility.SizeOf<int2>());
                componentGPUInstanceIndexRangesGPUBuffer.SetData(componentInstanceIndexRanges, 0, 0, components.Length);
                componentByteCountsGPUBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, components.Length, sizeof(int));
                componentByteCountsGPUBuffer.SetData(componentByteSizes, 0, 0, components.Length);
            }

            public void Dispose()
            {
                layout.Dispose();
                components.Dispose();
                componentsMetadata.Dispose();
                componentsGPUAddress.Dispose();
                componentPerInstance.Dispose();
                componentByteSizes.Dispose();
                componentInstanceIndexRanges.Dispose();
                componentsArchetypeIndexSpan.Dispose();
                componentIndices.Dispose();
                instancesCountPrefixSum.Dispose();
                gpuBuffer.Dispose();
                componentsPerInstanceGPUBuffer.Dispose();
                componentsGPUAddressGPUBuffer.Dispose();
                componentGPUInstanceIndexRangesGPUBuffer.Dispose();
                componentByteCountsGPUBuffer.Dispose();
            }

            public int FindComponentIndex(GPUComponentHandle component)
            {
                Assert.IsTrue(component.valid);
                if (component.index < componentIndices.Length)
                    return componentIndices[component.index];
                return InvalidIndex;
            }

            public int FindArchetypeIndex(GPUArchetypeHandle archetype)
            {
                return layout.FindArchetypeIndex(archetype);
            }
        }

        internal readonly struct ComponentIndex
        {
            public readonly int index;
            public readonly int layoutVersion;

            public ComponentIndex(int index, int layoutVersion)
            {
                this.index = index;
                this.layoutVersion = layoutVersion;
            }
        }

        internal readonly struct ArchetypeIndex
        {
            public readonly int index;
            public readonly int layoutVersion;

            public ArchetypeIndex(int index, int layoutVersion)
            {
                this.index = index;
                this.layoutVersion = layoutVersion;
            }
        }

        internal static class UploadKernelID
        {
            public static readonly int kInputComponentsCount = Shader.PropertyToID("_InputComponentsCount");
            public static readonly int kInputInstancesCount = Shader.PropertyToID("_InputInstancesCount");
            public static readonly int kInputInstanceData = Shader.PropertyToID("_InputInstanceData");
            public static readonly int kInputInstanceIndices = Shader.PropertyToID("_InputInstanceIndices");
            public static readonly int kInputComponentAddresses = Shader.PropertyToID("_InputComponentAddresses");
            public static readonly int kOutputComponentByteCounts = Shader.PropertyToID("_OutputComponentByteCounts");
            public static readonly int kOutputComponentIndices = Shader.PropertyToID("_OutputComponentIndices");
            public static readonly int kOutputComponentInstanceIndexRanges = Shader.PropertyToID("_OutputComponentInstanceIndexRanges");
            public static readonly int kOutputComponentIsPerInstance = Shader.PropertyToID("_OutputComponentIsPerInstance");
            public static readonly int kOutputComponentAddresses = Shader.PropertyToID("_OutputComponentAddresses");
            public static readonly int kOutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }

        internal static class CopyKernelID
        {
            public static readonly int kDispatchThreadGroupBase = Shader.PropertyToID("_DispatchThreadGroupBase");
            public static readonly int kInputComponentsCount = Shader.PropertyToID("_InputComponentsCount");
            public static readonly int kInputThreadGroupBeginIndices = Shader.PropertyToID("_InputThreadGroupBeginIndices");
            public static readonly int kInputComponentDataAddresses = Shader.PropertyToID("_InputComponentDataAddresses");
            public static readonly int kOutputComponentDataAddresses = Shader.PropertyToID("_OutputComponentDataAddresses");
            public static readonly int kOutputComponentDataUIntSizes = Shader.PropertyToID("_OutputComponentDataUIntSizes");
            public static readonly int kInputBuffer = Shader.PropertyToID("_InputBuffer");
            public static readonly int kOutputBuffer = Shader.PropertyToID("_OutputBuffer");
        }
    }

    internal struct GPUInstanceUploadData : IDisposable
    {
        private NativeArray<GPUComponentHandle> m_Components;
        private NativeArray<int> m_ComponentGPUAddress;
        private NativeArray<int> m_ComponentSize;
        private NativeArray<bool> m_ComponentPerInstance;
        private NativeArray<int> m_ComponentIndices;
        private NativeList<GPUComponentHandle> m_WrittenComponents;
        private int m_Length;
        private int m_UploadDataUIntSize;

        public int length => m_Length;
        public int uploadDataUIntSize => m_UploadDataUIntSize;
        public NativeArray<int>.ReadOnly componentGPUAddress => m_ComponentGPUAddress.AsReadOnly();
        public NativeArray<GPUComponentHandle>.ReadOnly writtenComponents => m_WrittenComponents.AsReadOnly();

        public GPUInstanceUploadData(ref GPUArchetypeManager archetypeManager, NativeArray<GPUComponentHandle> components, int length, Allocator allocator)
        {
            Assert.IsTrue(length > 0);
            Assert.IsTrue(components.Length != 0);

            m_Length = length;
            m_Components = new NativeArray<GPUComponentHandle>(components.Length, allocator);
            m_Components.CopyFrom(components);
            m_ComponentGPUAddress = new NativeArray<int>(components.Length, allocator, NativeArrayOptions.UninitializedMemory);
            m_ComponentSize = new NativeArray<int>(components.Length, allocator, NativeArrayOptions.UninitializedMemory);
            m_ComponentPerInstance = new NativeArray<bool>(components.Length, allocator, NativeArrayOptions.UninitializedMemory);
            m_ComponentIndices = new NativeArray<int>(archetypeManager.GetComponentsCount(), allocator);
            m_ComponentIndices.FillArray(GPUInstanceDataBuffer.InvalidIndex);
            m_WrittenComponents = new NativeList<GPUComponentHandle>(components.Length, allocator);

            int byteOffset = 0;

            for (int i = 0; i < components.Length; ++i)
            {
                GPUComponentHandle component = components[i];
                Assert.IsTrue(component.valid);

                if (m_ComponentIndices[component.index] == GPUInstanceDataBuffer.InvalidIndex)
                    m_ComponentIndices[component.index] = i;
                else
                    Assert.IsTrue(false, "Component is added more than once.");

                ref readonly GPUComponentDesc componentDesc = ref archetypeManager.GetComponentDesc(component);
                Assert.IsTrue(componentDesc.byteSize % sizeof(uint) == 0);

                m_ComponentGPUAddress[i] = byteOffset;
                m_ComponentSize[i] = componentDesc.byteSize;
                m_ComponentPerInstance[i] = componentDesc.isPerInstance;
                byteOffset += componentDesc.byteSize * (componentDesc.isPerInstance ? length : 1);
            }

            Assert.IsTrue(byteOffset % sizeof(uint) == 0);
            m_UploadDataUIntSize = byteOffset / sizeof(uint);
        }

        public void Dispose()
        {
            m_Components.Dispose();
            m_ComponentGPUAddress.Dispose();
            m_ComponentSize.Dispose();
            m_ComponentPerInstance.Dispose();
            m_ComponentIndices.Dispose();
            m_WrittenComponents.Dispose();
        }

        public int FindComponentIndex(GPUComponentHandle component)
        {
            Assert.IsTrue(component.valid);
            Assert.IsTrue(component.index < m_ComponentIndices.Length, "Component index is invalid.");
            int componentIndex = m_ComponentIndices[component.index];
            Assert.IsTrue(componentIndex != GPUInstanceDataBuffer.InvalidIndex, "Component is not allocated.");
            return componentIndex;
        }

        public int PrepareComponentWrite<T>(GPUComponentHandle component) where T : unmanaged
        {
            int componentIndex = FindComponentIndex(component);
            Assert.IsTrue(UnsafeUtility.SizeOf<T>() == m_ComponentSize[componentIndex],
                "Component to write is incompatible, must be same stride as destination.");

            if (!m_WrittenComponents.Contains(component))
                m_WrittenComponents.Add(component);

            return m_ComponentGPUAddress[componentIndex] / UnsafeUtility.SizeOf<uint>();
        }

        public JobHandle ScheduleWriteComponentsJob<T>(NativeArray<T> instanceData, GPUComponentHandle component, NativeArray<uint> uploadBuffer) where T : unmanaged
        {
            var jaggedInstanceData = instanceData.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()).ToJaggedSpan(Allocator.TempJob);
            JobHandle jobHandle = ScheduleWriteComponentsJob(jaggedInstanceData, component, UnsafeUtility.SizeOf<T>(), uploadBuffer);
            jaggedInstanceData.Dispose(jobHandle);
            return jobHandle;
        }

        public unsafe JobHandle ScheduleWriteComponentsJob(JaggedSpan<byte> instanceData, GPUComponentHandle component, int componentSize, NativeArray<uint> uploadBuffer)
        {
            if (instanceData.sectionCount == 0)
                return default;

            Assert.IsTrue(uploadBuffer.Length >= m_UploadDataUIntSize);
            Assert.IsTrue(m_Length > 0, "Space is not reserved. Did you forget to call ReserveUploadSpace()?");

            int componentCount = instanceData.totalLength / componentSize;
            int componentIndex = FindComponentIndex(component);
            int componentOffset = m_ComponentGPUAddress[componentIndex];
            bool isPerInstance = m_ComponentPerInstance[componentIndex];

            if (isPerInstance)
            {
                Assert.IsTrue(componentCount == m_Length, "Wrong instance data length.");
            }
            else
            {
                Assert.IsTrue(m_Length >= 1 && componentCount == 1);
            }

            Assert.IsTrue(componentSize == m_ComponentSize[componentIndex],
                "Component to write is incompatible, must be same stride as destination.");

            if (!m_WrittenComponents.Contains(component))
                m_WrittenComponents.Add(component);
            else
                Assert.IsTrue(false, "Component is already written.");

            if (isPerInstance)
            {
                // Each job writes 16 cache lines (1KiB)
                NativeList<JaggedJobRange> jobRanges = JaggedJobRange.FromSpanWithRelaxedBatchSize(instanceData, 16 * 64, Allocator.TempJob);

                var jobHandle = new WriteGPUComponentDataJob
                {
                    JobRanges = jobRanges.AsArray(),
                    ComponentOffsetInBytes = m_ComponentGPUAddress[componentIndex],
                    JaggedInstanceData = instanceData,
                    UploadBuffer = uploadBuffer.Reinterpret<byte>(sizeof(uint))
                }
                .Schedule(jobRanges);
                jobRanges.Dispose(jobHandle);

                return jobHandle;
            }
            else
            {
                Assert.IsTrue(instanceData.sectionCount == 1);
                Assert.IsTrue(instanceData.sections[0].Length == componentSize);

                UnsafeList<byte> section = instanceData.sections[0];
                UnsafeUtility.MemCpy((byte*)uploadBuffer.GetUnsafePtr() + componentOffset, section.Ptr, section.Length);

                return default;
            }
        }
    }

    internal struct GPUInstanceDataBufferLayout : IDisposable, IEnumerable, IEquatable<GPUInstanceDataBufferLayout>
    {
        private NativeList<GPUArchetypeHandle> m_Archetypes;
        private NativeList<int> m_InstancesCount;
        private NativeList<int> m_ArchetypeIndex;

        public NativeArray<GPUArchetypeHandle>.ReadOnly archetypes => m_Archetypes.AsReadOnly();
        public NativeArray<int>.ReadOnly instancesCount => m_InstancesCount.AsReadOnly();

        public GPUInstanceDataBufferLayout(int capacity, Allocator allocator)
        {
            m_Archetypes = new NativeList<GPUArchetypeHandle>(capacity, allocator);
            m_InstancesCount = new NativeList<int>(capacity, allocator);
            m_ArchetypeIndex = new NativeList<int>(capacity, allocator);
        }

        public GPUInstanceDataBufferLayout(in GPUInstanceDataBufferLayout otherLayout, Allocator allocator)
        {
            m_Archetypes = new NativeList<GPUArchetypeHandle>(otherLayout.m_Archetypes.Length, allocator);
            m_InstancesCount = new NativeList<int>(otherLayout.m_InstancesCount.Length, allocator);
            m_ArchetypeIndex = new NativeList<int>(otherLayout.m_ArchetypeIndex.Length, allocator);
            m_Archetypes.CopyFrom(otherLayout.m_Archetypes);
            m_InstancesCount.CopyFrom(otherLayout.m_InstancesCount);
            m_ArchetypeIndex.CopyFrom(otherLayout.m_ArchetypeIndex);
        }

        public void Add(GPUArchetypeHandle archetype, int instanceCount)
        {
            Assert.IsTrue(archetype.valid, "The archetype is invalid.");
            Assert.IsTrue(instanceCount >= 0, "The instance count is negative.");

            if (!m_Archetypes.IsCreated)
                m_Archetypes = new NativeList<GPUArchetypeHandle>(1, Allocator.Temp);
            if (!m_InstancesCount.IsCreated)
                m_InstancesCount = new NativeList<int>(1, Allocator.Temp);
            if (!m_ArchetypeIndex.IsCreated)
                m_ArchetypeIndex = new NativeList<int>(1, Allocator.Temp);

            if (archetype.index >= m_ArchetypeIndex.Length)
                m_ArchetypeIndex.AddReplicate(GPUArchetypeHandle.Invalid.index, archetype.index - m_Archetypes.Length + 1);

            Assert.IsTrue(m_ArchetypeIndex[archetype.index] == GPUArchetypeHandle.Invalid.index, "The layout already contains the archetype.");

            m_ArchetypeIndex[archetype.index] = m_Archetypes.Length;
            m_Archetypes.Add(archetype);
            m_InstancesCount.Add(instanceCount);
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < m_Archetypes.Length; i++)
                yield return (m_Archetypes[i], m_InstancesCount[i]);
        }

        public void Dispose()
        {
            m_Archetypes.Dispose();
            m_InstancesCount.Dispose();
            m_ArchetypeIndex.Dispose();
        }

        public bool Equals(GPUInstanceDataBufferLayout other)
        {
            return m_Archetypes.ArraysEqual(other.m_Archetypes) && m_InstancesCount.ArraysEqual(other.m_InstancesCount);
        }

        public int FindArchetypeIndex(GPUArchetypeHandle archetype)
        {
            Assert.IsTrue(archetype.valid, "The archetype is invalid.");
            if(archetype.index < m_ArchetypeIndex.Length)
                return m_ArchetypeIndex[archetype.index];
            else
                return GPUInstanceDataBuffer.InvalidIndex;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        public struct ReadOnly
        {
            public readonly NativeArray<GPUArchetypeHandle>.ReadOnly archetypes;
            public readonly NativeArray<int>.ReadOnly instancesCount;
            public readonly NativeArray<int>.ReadOnly archetypeIndex;

            public ReadOnly(GPUInstanceDataBufferLayout layout)
            {
                archetypes = layout.m_Archetypes.AsReadOnly();
                instancesCount = layout.m_InstancesCount.AsReadOnly();
                archetypeIndex = layout.m_ArchetypeIndex.AsReadOnly();
            }

            public int FindArchetypeIndex(GPUArchetypeHandle archetype)
            {
                Assert.IsTrue(archetype.valid, "The archetype is invalid.");
                if (archetype.index < archetypeIndex.Length)
                    return archetypeIndex[archetype.index];
                else
                    return -1;
            }
        }
    }

    internal struct GPUInstanceDataBufferReadback<TData> : IDisposable where TData : unmanaged
    {
        private GPUInstanceDataBuffer m_InstanceDataBuffer;

        public NativeArray<TData> data { get; private set; }

        public bool Load(CommandBuffer cmd, GPUInstanceDataBuffer instanceDataBuffer)
        {
            Assert.IsNotNull(instanceDataBuffer);

            m_InstanceDataBuffer = instanceDataBuffer;

            var dataSize = UnsafeUtility.SizeOf<TData>();
            var localData = new NativeArray<TData>((instanceDataBuffer.gpuBufferByteSize + (dataSize - 1)) / dataSize, Allocator.Persistent);
            var errorCount = 0;

            cmd.RequestAsyncReadback(instanceDataBuffer.nativeBuffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                    localData.CopyFrom(req.GetData<TData>());
                else ++errorCount;
            });

            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            data = localData;

            if (errorCount != 0)
                Debug.LogError("GPUInstanceDataBufferReadback: request async readback failed.");

            return errorCount == 0;
        }

        public unsafe T LoadData<T>(GPUComponentHandle component, GPUInstanceIndex gpuInstanceIndex) where T : unmanaged
        {
            var compIndex = m_InstanceDataBuffer.GetComponentIndex(component);
            var componentIndexRange = m_InstanceDataBuffer.componentInstanceIndexRanges[compIndex.index];
            Assert.IsTrue(gpuInstanceIndex.index >= componentIndexRange.x && gpuInstanceIndex.index < componentIndexRange.y, "GPUInstanceIndex is out of component range.");
            var componentByteSize = m_InstanceDataBuffer.componentByteSizes[compIndex.index];
            Assert.IsTrue(componentByteSize == UnsafeUtility.SizeOf<T>(), "Component byte size doesn't match.");
            int isPerInstance = m_InstanceDataBuffer.componentPerInstance[compIndex.index];
            int gpuBaseAddress = m_InstanceDataBuffer.componentsGPUAddress[compIndex.index];
            int index = (gpuBaseAddress + componentByteSize * gpuInstanceIndex.index * isPerInstance) / UnsafeUtility.SizeOf<uint>();
            uint* dataPtr = (uint*)data.GetUnsafePtr() + index;
            T result = *(T*)(dataPtr);
            return result;
        }

        public void Dispose()
        {
            data.Dispose();
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct WriteGPUComponentDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<JaggedJobRange> JobRanges;
        [ReadOnly] public int ComponentOffsetInBytes;
        [NativeDisableContainerSafetyRestriction, NoAlias][ReadOnly] public JaggedSpan<byte> JaggedInstanceData;

        [NativeDisableContainerSafetyRestriction, NoAlias][WriteOnly] public NativeArray<byte> UploadBuffer;

        public unsafe void Execute(int jobIndex)
        {
            JaggedJobRange jobRange = JobRanges[jobIndex];

            NativeArray<byte> section = JaggedInstanceData[jobRange.sectionIndex];

            byte* srcBasePtr = (byte*)section.GetUnsafeReadOnlyPtr();
            byte* dstBasePtr = (byte*)UploadBuffer.GetUnsafePtr();

            byte* srcPtr = srcBasePtr + jobRange.localStart;
            byte* dstPtr = dstBasePtr + ComponentOffsetInBytes + jobRange.absoluteStart;

            Assert.IsTrue(srcPtr + jobRange.length <= srcBasePtr + section.Length);
            Assert.IsTrue(dstPtr + jobRange.length <= dstPtr + UploadBuffer.Length);

            UnsafeUtility.MemCpy(dstPtr, srcPtr, jobRange.length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct InstancesToGPUIndicesJob : IJobParallelFor
    {
        [ReadOnly] public RenderWorld renderWorld;
        [ReadOnly] public NativeArray<int> instancesCountPrefixSum;
        [ReadOnly] public GPUInstanceDataBufferLayout layout;
        [ReadOnly] public NativeArray<InstanceHandle> instances;

        [WriteOnly] public NativeArray<GPUInstanceIndex> gpuIndices;

        public void Execute(int index)
        {
            InstanceHandle instance = instances[index];
            Assert.IsTrue(instance.isValid, "Invalid Instance");
            if (!instance.isValid)
            {
                gpuIndices[index] = GPUInstanceIndex.Invalid;
                return;
            }

            int instanceIndex = renderWorld.HandleToIndex(instance);
            InstanceGPUHandle gpuHandle = renderWorld.gpuHandles[instanceIndex];
            Assert.IsTrue(gpuHandle.archetype.valid && gpuHandle.archetypeInstanceIndex >= 0);
            int archetypeIndex = layout.FindArchetypeIndex(gpuHandle.archetype);
            Assert.IsTrue(archetypeIndex >= 0);
            int instancesBegin = instancesCountPrefixSum[archetypeIndex];
            int instancesCount = layout.instancesCount[archetypeIndex];
            Assert.IsTrue(gpuHandle.archetypeInstanceIndex < instancesCount);
            gpuIndices[index] = GPUInstanceIndex.Create(instancesBegin + gpuHandle.archetypeInstanceIndex);
        }
    }

    // This is the actual instance offset from the component base address on the GPU.
    // Note: The component base address might differ from the address of the first allocated component element.
    // It may have a negative offset to allow proper referencing by GPUInstanceIndex.
    internal struct GPUInstanceIndex : IEquatable<GPUInstanceIndex>, IComparable<GPUInstanceIndex>
    {
        public int index { get; private set; }
        public bool valid => index >= 0;
        public static GPUInstanceIndex Create(int index) { return new GPUInstanceIndex { index = index }; }
        public static readonly GPUInstanceIndex Invalid = new GPUInstanceIndex { index = -1 };
        public bool Equals(GPUInstanceIndex other) => index == other.index;
        public int CompareTo(GPUInstanceIndex other) { return index.CompareTo(other.index); }
        public override int GetHashCode() { return index; }
    }
}
