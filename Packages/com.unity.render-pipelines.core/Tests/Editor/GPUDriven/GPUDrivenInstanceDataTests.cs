using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    internal class GPUDrivenInstanceDataTests
    {
        private CommandBuffer m_Cmd;
        private GPUArchetypeManager m_ArchetypeMgr;
        private RenderPassTest m_RenderPipe;
        private RenderPipelineAsset m_OldPipelineAsset;
        private GPUResidentDrawerResources m_Resources;
        private RenderPassGlobalSettings m_GlobalSettings;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_GlobalSettings = ScriptableObject.CreateInstance<RenderPassGlobalSettings>();
#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderPassTestCullInstance>(m_GlobalSettings);
#endif
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderPassTestCullInstance>(null);
#endif
            Object.DestroyImmediate(m_GlobalSettings);
        }

        [SetUp]
        public void OnSetup()
        {
            m_Cmd = new CommandBuffer();
            m_ArchetypeMgr.Initialize();
            m_RenderPipe = ScriptableObject.CreateInstance<RenderPassTest>();
            m_OldPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.defaultRenderPipeline = m_RenderPipe;
            m_Resources = GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
        }

        [TearDown]
        public void OnTearDown()
        {
            m_Resources = null;
            GraphicsSettings.defaultRenderPipeline = m_OldPipelineAsset;
            m_OldPipelineAsset = null;
            m_RenderPipe = null;
            m_ArchetypeMgr.Dispose();
            m_ArchetypeMgr = default;
            m_Cmd.Dispose();
            m_Cmd = null;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public unsafe void TestGPUComponentSet()
        {
            for(int i = 0; i < GPUArchetypeManager.kMaxComponentsCount; ++i)
                m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID($"Component{i}"), isPerInstance: true);
            
            var components = new NativeList<GPUComponentHandle>(Allocator.Temp);
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component0")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component3")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component6")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component18")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component24")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component45")));
            components.Add(m_ArchetypeMgr.FindComponent(Shader.PropertyToID($"Component63")));

            var componentSet = new GPUComponentSet(components.AsArray());
            Assert.IsTrue(componentSet.GetComponentsCount() == components.Length);

            for (int i = 0; i < components.Length; ++i)
                Assert.AreEqual(componentSet.GetComponentByIndex(i), components[i]);

            var archetype = m_ArchetypeMgr.CreateArchetype(componentSet);
            var archetypeComponents = m_ArchetypeMgr.GetArchetypeDesc(archetype).components;
            Assert.IsTrue(archetypeComponents.ArraysEqual(components));
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public unsafe void TestInstanceUploadDataSimple()
        {
            const int instancesCount = 16;

            var component = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector4"), isPerInstance: true);
            var components = new NativeList<GPUComponentHandle>(Allocator.Temp) { component };

            using (var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, components.AsArray(), instancesCount, Allocator.Temp))
            {
                var writeBuffer = new NativeArray<uint>(upload.uploadDataUIntSize, Allocator.TempJob);

                var componentData = new NativeArray<Vector4>(instancesCount, Allocator.TempJob);
                for (int i = 0; i < instancesCount; ++i)
                    componentData[i] = new Vector4(i, i, i, i);
                upload.ScheduleWriteComponentsJob(componentData, component, writeBuffer).Complete();

                uint* writeBufferPtr = (uint*)writeBuffer.GetUnsafePtr();

                Assert.AreEqual(upload.uploadDataUIntSize, instancesCount * 4);

                for (int i = 0; i < instancesCount; ++i)
                {
                    Vector4* data = ((Vector4*)writeBufferPtr) + i;
                    Assert.AreEqual(*data, new Vector4(i, i, i, i));
                }

                componentData.Dispose();
                writeBuffer.Dispose();
            }
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public unsafe void TestInstanceUploadDataMultiple()
        {
            var uintComponent = m_ArchetypeMgr.CreateComponent<uint>(Shader.PropertyToID("UInt"), isPerInstance: true);
            var floatComponent = m_ArchetypeMgr.CreateComponent<float>(Shader.PropertyToID("Float"), isPerInstance: true);
            var vecComponent = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector4"), isPerInstance: true);

            var components = new NativeList<GPUComponentHandle>(Allocator.Temp)
            {
                uintComponent,
                floatComponent,
                vecComponent
            };

            const int instancesCount = 31;

            using (var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, components.AsArray(), instancesCount, Allocator.Temp))
            {
                var writeBuffer = new NativeArray<uint>(upload.uploadDataUIntSize, Allocator.TempJob);
                var uintData = new NativeArray<uint>(instancesCount, Allocator.TempJob);
                var floatData = new NativeArray<float>(instancesCount, Allocator.TempJob);
                var vecData = new NativeArray<Vector4>(instancesCount, Allocator.TempJob);
                for (int i = 0; i < instancesCount; ++i)
                {
                    uintData[i] = (uint)i;
                    floatData[i] = i;
                    vecData[i] = new Vector4(i, i, i, i);
                }
                upload.ScheduleWriteComponentsJob(uintData, uintComponent, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(floatData, floatComponent, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(vecData, vecComponent, writeBuffer).Complete();

                uint* bufferPtr = (uint*)writeBuffer.GetUnsafePtr();
                Assert.AreEqual(upload.uploadDataUIntSize, instancesCount * 6);

                for (int i = 0; i < instancesCount; ++i)
                {
                    uint* uintComp = bufferPtr + i;
                    float* floatComp = (float*)(bufferPtr + instancesCount) + i;
                    Vector4* vecComp = (Vector4*)(bufferPtr + instancesCount + instancesCount) + i;
                    Assert.AreEqual(*uintComp, i);
                    Assert.AreEqual(*floatComp, i);
                    Assert.AreEqual(*vecComp, new Vector4(i, i, i, i));
                }

                uintData.Dispose();
                floatData.Dispose();
                vecData.Dispose();
                writeBuffer.Dispose();
            }
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public unsafe void TestInstanceUploadDataNonPerInstanceHasOneElement()
        {
            const int instancesCount = 4;

            var singleComponent = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector4Single"), isPerInstance: false);
            var perInstanceComponent = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector4PerInstance"), isPerInstance: true);
            var components = new NativeList<GPUComponentHandle>(Allocator.Temp)
            {
                singleComponent,
                perInstanceComponent
            };

            using (var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, components.AsArray(), instancesCount, Allocator.Temp))
            {
                var writeBuffer = new NativeArray<uint>(upload.uploadDataUIntSize, Allocator.TempJob);
                var singleComponentData = new NativeArray<Vector4>(1, Allocator.TempJob);
                var perInstanceComponentData = new NativeArray<Vector4>(instancesCount, Allocator.TempJob);
                singleComponentData[0] = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
                for (int i = 0; i < instancesCount; ++i)
                    perInstanceComponentData[i] = new Vector4(i, i, i, i);
                upload.ScheduleWriteComponentsJob(singleComponentData, singleComponent, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(perInstanceComponentData, perInstanceComponent, writeBuffer).Complete();

                uint* writeBufferPtr = (uint*)writeBuffer.GetUnsafePtr();
                Assert.AreEqual(upload.uploadDataUIntSize, (1 + instancesCount) * 4);

                Vector4* singleData = ((Vector4*)writeBufferPtr);
                Assert.AreEqual(*singleData, new Vector4(1.0f, 2.0f, 3.0f, 4.0f));

                for (int i = 0; i < instancesCount; ++i)
                {
                    Vector4* perInstanceData = ((Vector4*)writeBufferPtr) + 1 + i;
                    Assert.AreEqual(*perInstanceData, new Vector4(i, i, i, i));
                }

                singleComponentData.Dispose();
                perInstanceComponentData.Dispose();
                writeBuffer.Dispose();
            }
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferUploadSimple()
        {
            const int count = 16;

            var comp = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var comps = new NativeList<GPUComponentHandle>(Allocator.Temp) { comp };
            var arch = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch, count } }, m_Resources);
            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps.AsArray(), count, Allocator.Temp);

            var compData = new NativeArray<Vector4>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
                compData[i] = new Vector4(i, i, i, i);

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            upload.ScheduleWriteComponentsJob(compData, comp, writeBuffer).Complete();
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex = buffer.GetArchetypeIndex(arch);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < count; i++)
            {
                Vector4 element = readback.LoadData<Vector4>(comp, gpuIndices[i]);
                Assert.AreEqual(element, new Vector4(i, i, i, i));
            }

            uploadBuffer.Release();
            upload.Dispose();
            compData.Dispose();
            buffer.Dispose();
            readback.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferUploadMultiple()
        {
            const int count = 32;
            const int salt = 31;

            var compVector = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var compInt = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("Int"), isPerInstance: true);
            var comps = new NativeList<GPUComponentHandle>(Allocator.Temp) { compVector, compInt };
            var arch = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch, count } }, m_Resources);
            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps.AsArray(), count, Allocator.Temp);

            var compVectorData = new NativeArray<Vector4>(count, Allocator.TempJob);
            var compIntData = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                compVectorData[i] = new Vector4(i, i, i, i);
                compIntData[i] = i + salt;
            }

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            {
                upload.ScheduleWriteComponentsJob(compVectorData, compVector, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compIntData, compInt, writeBuffer).Complete();
            }
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex = buffer.GetArchetypeIndex(arch);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < count; i++)
            {
                Vector4 elementVec = readback.LoadData<Vector4>(compVector, gpuIndices[i]);
                int elementInt = readback.LoadData<int>(compInt, gpuIndices[i]);
                Assert.AreEqual(elementVec, new Vector4(i, i, i, i));
                Assert.AreEqual(elementInt, i + salt);
            }

            uploadBuffer.Release();
            upload.Dispose();
            compVectorData.Dispose();
            compIntData.Dispose();
            buffer.Dispose();
            readback.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public unsafe void TestInstanceDataBufferNonPerInstanceComponentHasOneElement()
        {
            const int count = 32;
            const float floatSingle = 999.0f;

            var compVector = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var compInt = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("Int"), isPerInstance: true);
            var compFloatSingle = m_ArchetypeMgr.CreateComponent<float>(Shader.PropertyToID("FloatSingle"), isPerInstance: false);

            var comps = new NativeList<GPUComponentHandle>(Allocator.Temp) { compVector, compFloatSingle, compInt };
            var arch = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch, count } }, m_Resources);
            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps.AsArray(), count, Allocator.Temp);

            var compVectorData = new NativeArray<Vector4>(count, Allocator.TempJob);
            var compIntData = new NativeArray<int>(count, Allocator.TempJob);
            var compFloatSingleData = new NativeArray<float>(1, Allocator.TempJob);
            compFloatSingleData[0] = floatSingle;
            for (int i = 0; i < count; i++)
            {
                compVectorData[i] = new Vector4(i, i, i, i);
                compIntData[i] = i;
            }

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            {
                upload.ScheduleWriteComponentsJob(compVectorData, compVector, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compIntData, compInt, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compFloatSingleData, compFloatSingle, writeBuffer).Complete();
            }
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex = buffer.GetArchetypeIndex(arch);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < count; i++)
            {
                Vector4 elementVec = readback.LoadData<Vector4>(compVector, gpuIndices[i]);
                int elementInt = readback.LoadData<int>(compInt, gpuIndices[i]);
                float elementFloatSingle = readback.LoadData<float>(compFloatSingle, gpuIndices[i]);
                Assert.AreEqual(elementVec, new Vector4(i, i, i, i));
                Assert.AreEqual(elementInt, i);
                Assert.AreEqual(elementFloatSingle, floatSingle);
            }

            uploadBuffer.Release();
            upload.Dispose();
            compVectorData.Dispose();
            compIntData.Dispose();
            compFloatSingleData.Dispose();
            buffer.Dispose();
            readback.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferUploadMultipleArchetypes()
        {
            const int archCount0 = 31;
            const int archCount1 = 15;
            const int archCount2 = 7;

            var compVector = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var compInt = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("Int"), isPerInstance: true);
            var compFloat = m_ArchetypeMgr.CreateComponent<float>(Shader.PropertyToID("Float"), isPerInstance: true);
            var compUInt = m_ArchetypeMgr.CreateComponent<uint>(Shader.PropertyToID("UInt"), isPerInstance: true);

            var comps0 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compVector, compInt };
            var comps1 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compVector, compInt, compFloat };
            var comps2 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compUInt };

            var arch0 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps0.AsArray()));
            var arch1 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps1.AsArray()));
            var arch2 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps2.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout
            {
                { arch0, archCount0 },
                { arch1, archCount1 },
                { arch2, archCount2 }
            },
            m_Resources);

            var archIndex0 = buffer.GetArchetypeIndex(arch0);
            var archIndex1 = buffer.GetArchetypeIndex(arch1);
            var archIndex2 = buffer.GetArchetypeIndex(arch2);

            {
                var archDesc = m_ArchetypeMgr.GetArchetypeDesc(arch0);
                var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, archDesc.components.AsArray(), archCount0, Allocator.Temp);

                var compVectorData = new NativeArray<Vector4>(archCount0, Allocator.TempJob);
                var compIntData = new NativeArray<int>(archCount0, Allocator.TempJob);
                for (int i = 0; i < archCount0; i++)
                {
                    compVectorData[i] = new Vector4(i, i, i, i);
                    compIntData[i] = i;
                }

                GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                    GraphicsBuffer.UsageFlags.LockBufferForWrite,
                    upload.uploadDataUIntSize,
                    sizeof(uint));

                var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
                {
                    upload.ScheduleWriteComponentsJob(compVectorData, compVector, writeBuffer).Complete();
                    upload.ScheduleWriteComponentsJob(compIntData, compInt, writeBuffer).Complete();
                }
                uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

                var gpuIndices = new NativeArray<GPUInstanceIndex>(archCount0, Allocator.Temp);
                for (int i = 0; i < archCount0; i++)
                    gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex0, i);

                buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

                uploadBuffer.Release();
                upload.Dispose();
                compVectorData.Dispose();
                compIntData.Dispose();
            }

            {
                var archDesc = m_ArchetypeMgr.GetArchetypeDesc(arch1);
                var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, archDesc.components.AsArray(), archCount1, Allocator.Temp);

                var compVectorData = new NativeArray<Vector4>(archCount1, Allocator.TempJob);
                var compIntData = new NativeArray<int>(archCount1, Allocator.TempJob);
                var compFloatData = new NativeArray<float>(archCount1, Allocator.TempJob);
                for (int i = 0; i < archCount1; i++)
                {
                    compVectorData[i] = new Vector4(archCount0 + i, archCount0 + i, archCount0 + i, archCount0 + i);
                    compIntData[i] = archCount0 + i;
                    compFloatData[i] = archCount0 + i;
                }

                GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                    GraphicsBuffer.UsageFlags.LockBufferForWrite,
                    upload.uploadDataUIntSize,
                    sizeof(uint));

                var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
                {
                    upload.ScheduleWriteComponentsJob(compVectorData, compVector, writeBuffer).Complete();
                    upload.ScheduleWriteComponentsJob(compIntData, compInt, writeBuffer).Complete();
                    upload.ScheduleWriteComponentsJob(compFloatData, compFloat, writeBuffer).Complete();
                }
                uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

                var gpuIndices = new NativeArray<GPUInstanceIndex>(archCount1, Allocator.Temp);
                for (int i = 0; i < archCount1; i++)
                    gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex1, i);

                buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

                uploadBuffer.Release();
                upload.Dispose();
                compVectorData.Dispose();
                compIntData.Dispose();
                compFloatData.Dispose();
            }

            {
                var archDesc = m_ArchetypeMgr.GetArchetypeDesc(arch2);
                var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, archDesc.components.AsArray(), archCount2, Allocator.Temp);

                var compUIntData = new NativeArray<uint>(archCount2, Allocator.TempJob);
                for (int i = 0; i < archCount2; i++)
                    compUIntData[i] = archCount0 + archCount1 + (uint)i;

                GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                    GraphicsBuffer.UsageFlags.LockBufferForWrite,
                    upload.uploadDataUIntSize,
                    sizeof(uint));

                var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
                upload.ScheduleWriteComponentsJob(compUIntData, compUInt, writeBuffer).Complete();
                uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

                var gpuIndices = new NativeArray<GPUInstanceIndex>(archCount2, Allocator.Temp);
                for (int i = 0; i < archCount2; i++)
                    gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex2, i);

                buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

                uploadBuffer.Release();
                upload.Dispose();
                compUIntData.Dispose();
            }

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < archCount0; i++)
            {
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex0, i);
                Vector4 elementVec = readback.LoadData<Vector4>(compVector, gpuIndex);
                int elementInt = readback.LoadData<int>(compInt, gpuIndex);
                Assert.AreEqual(elementVec, new Vector4(i, i, i, i));
                Assert.AreEqual(elementInt, i);
            }

            for (int i = 0; i < archCount1; i++)
            {
                int index = archCount0 + i;
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex1, i);
                Vector4 elementVec = readback.LoadData<Vector4>(compVector, gpuIndex);
                int elementInt = readback.LoadData<int>(compInt, gpuIndex);
                float elementFloat = readback.LoadData<float>(compFloat, gpuIndex);
                Assert.AreEqual(elementVec, new Vector4(index, index, index, index));
                Assert.AreEqual(elementInt, index);
                Assert.AreEqual(elementFloat, index);
            }

            for (int i = 0; i < archCount2; i++)
            {
                int index = archCount0 + archCount1 + i;
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex2, i);
                uint elementUInt = readback.LoadData<uint>(compUInt, gpuIndex);
                Assert.AreEqual(elementUInt, index);
            }

            buffer.Dispose();
            readback.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferGrowNonPerInstanceComponentsHasOneElement()
        {
            const int initCount = 16;
            const int newCount = 32;

            var compVecNonInst = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("VectorNonInst"), isPerInstance: false);
            var compVec = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var compIntNonInst = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("IntNonInst"), isPerInstance: false);
            var comps = new NativeList<GPUComponentHandle>(Allocator.Temp) { compVecNonInst, compVec, compIntNonInst };

            var arch = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch, initCount } }, m_Resources);
            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps.AsArray(), initCount, Allocator.Temp);

            var compVecNonInstData = new NativeArray<Vector4>(1, Allocator.TempJob);
            compVecNonInstData[0] = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);

            var compVecData = new NativeArray<Vector4>(initCount, Allocator.TempJob);
            var compIntNonInstData = new NativeArray<int>(1, Allocator.TempJob);
            for (int i = 0; i < initCount; i++)
                compVecData[i] = new Vector4(i, i, i, i);
            compIntNonInstData[0] = 123;

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            {
                upload.ScheduleWriteComponentsJob(compVecNonInstData, compVecNonInst, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compVecData, compVec, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compIntNonInstData, compIntNonInst, writeBuffer).Complete();
            }
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex = buffer.GetArchetypeIndex(arch);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(initCount, Allocator.Temp);
            for (int i = 0; i < initCount; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            buffer.SetGPULayout(m_Cmd, ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch, newCount } }, submitCmdBuffer: true);

            int newUploadCount = newCount - initCount;
            var newUpload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps.AsArray(), newUploadCount, Allocator.Temp);

            var newCompData = new NativeArray<Vector4>(newUploadCount, Allocator.TempJob);
            for (int i = 0; i < newUploadCount; i++)
                newCompData[i] = new Vector4(initCount + i, initCount + i, initCount + i, initCount + i);

            GraphicsBuffer newUploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                newUpload.uploadDataUIntSize,
                sizeof(uint));

            var newWriteBuffer = newUploadBuffer.LockBufferForWrite<uint>(0, newUpload.uploadDataUIntSize);
            newUpload.ScheduleWriteComponentsJob(newCompData, compVec, newWriteBuffer).Complete();
            newUploadBuffer.UnlockBufferAfterWrite<uint>(newUpload.uploadDataUIntSize);

            archIndex = buffer.GetArchetypeIndex(arch);

            var newGPUIndices = new NativeArray<GPUInstanceIndex>(newUploadCount, Allocator.Temp);
            for (int i = 0; i < newUploadCount; i++)
                newGPUIndices[i] = buffer.InstanceToGPUIndex(archIndex, initCount + i);

            buffer.UploadDataToGPU(m_Cmd, newUploadBuffer, newUpload, newGPUIndices);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < newCount; i++)
            {
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex, i);
                Vector4 element = readback.LoadData<Vector4>(compVec, gpuIndex);
                Vector4 elementSingle = readback.LoadData<Vector4>(compVecNonInst, gpuIndex);
                int elementInt = readback.LoadData<int>(compIntNonInst, gpuIndex);
                Assert.AreEqual(element, new Vector4(i, i, i, i));
                Assert.AreEqual(elementSingle, new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
                Assert.AreEqual(elementInt, 123);
            }

            newUploadBuffer.Release();
            uploadBuffer.Release();
            upload.Dispose();
            newUpload.Dispose();
            newCompData.Dispose();
            compVecNonInstData.Dispose();
            compVecData.Dispose();
            compIntNonInstData.Dispose();
            readback.Dispose();
            buffer.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferNewLayoutWithMoreArchetypes()
        {
            const int count = 63;

            var compMatrix = m_ArchetypeMgr.CreateComponent<Matrix4x4>(Shader.PropertyToID("Matrix"), isPerInstance: true);
            var compIntNonInst = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("IntNonInst"), isPerInstance: false);
            var comps0 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compMatrix, compIntNonInst };

            var arch0 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps0.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch0, count } }, m_Resources);
            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps0.AsArray(), count, Allocator.Temp);

            var compMatrixData = new NativeArray<Matrix4x4>(count, Allocator.TempJob);
            var compIntNonInstData = new NativeArray<int>(1, Allocator.TempJob);
            for (int i = 0; i < count; i++)
                compMatrixData[i] = new Matrix4x4(new Vector4(i, i, i, i), new Vector4(i, i, i, i), new Vector4(i, i, i, i), new Vector4(i, i, i, i));
            compIntNonInstData[0] = 123;

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            {
                upload.ScheduleWriteComponentsJob(compMatrixData, compMatrix, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compIntNonInstData, compIntNonInst, writeBuffer).Complete();
            }
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex0 = buffer.GetArchetypeIndex(arch0);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex0, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            var compVec = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);
            var comps1 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compMatrix, compIntNonInst, compVec };
            var arch1 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps1.AsArray()));

            buffer.SetGPULayout(m_Cmd, ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch0, count }, { arch1, count } }, submitCmdBuffer: true);

            var newUpload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps1.AsArray(), count, Allocator.Temp);

            var newCompMatrixData = new NativeArray<Matrix4x4>(count, Allocator.TempJob);
            var newCompVecData = new NativeArray<Vector4>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                var vec = new Vector4(i + count, i + count, i + count, i + count);
                newCompMatrixData[i] = new Matrix4x4(vec, vec, vec, vec);
                newCompVecData[i] = new Vector4(i + 123, i + 123, i + 123, i + 123);
            }

            GraphicsBuffer newUploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                newUpload.uploadDataUIntSize,
                sizeof(uint));

            var newWriteBuffer = newUploadBuffer.LockBufferForWrite<uint>(0, newUpload.uploadDataUIntSize);
            {
                newUpload.ScheduleWriteComponentsJob(newCompMatrixData, compMatrix, newWriteBuffer).Complete();
                newUpload.ScheduleWriteComponentsJob(newCompVecData, compVec, newWriteBuffer).Complete();
            }
            newUploadBuffer.UnlockBufferAfterWrite<uint>(newUpload.uploadDataUIntSize);

            archIndex0 = buffer.GetArchetypeIndex(arch0);
            var archIndex1 = buffer.GetArchetypeIndex(arch1);

            var newGPUIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                newGPUIndices[i] = buffer.InstanceToGPUIndex(archIndex1, i);

            buffer.UploadDataToGPU(m_Cmd, newUploadBuffer, newUpload, newGPUIndices);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < count; i++)
            {
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex0, i);
                Matrix4x4 matrix = readback.LoadData<Matrix4x4>(compMatrix, gpuIndex);
                int singleInt = readback.LoadData<int>(compIntNonInst, gpuIndex);
                Assert.AreEqual(matrix, new Matrix4x4(new Vector4(i, i, i, i), new Vector4(i, i, i, i), new Vector4(i, i, i, i), new Vector4(i, i, i, i)));
                Assert.AreEqual(singleInt, 123);
            }

            for (int i = 0; i < count; i++)
            {
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex1, i);
                Matrix4x4 matrix = readback.LoadData<Matrix4x4>(compMatrix, gpuIndex);
                int singleInt = readback.LoadData<int>(compIntNonInst, gpuIndex);
                Vector4 vector = readback.LoadData<Vector4>(compVec, gpuIndex);
                var vec = new Vector4(i + count, i + count, i + count, i + count);
                Assert.AreEqual(matrix, new Matrix4x4(vec, vec, vec, vec));
                Assert.AreEqual(singleInt, 123);
                Assert.AreEqual(vector, new Vector4(i + 123, i + 123, i + 123, i + 123));
            }

            newUploadBuffer.Release();
            uploadBuffer.Release();
            upload.Dispose();
            newUpload.Dispose();
            newCompMatrixData.Dispose();
            newCompVecData.Dispose();
            compMatrixData.Dispose();
            compIntNonInstData.Dispose();
            readback.Dispose();
            buffer.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferNewLayoutWithLessArchetypes()
        {
            const int count = 16001;

            var compMatrix = m_ArchetypeMgr.CreateComponent<Matrix4x4>(Shader.PropertyToID("Matrix"), isPerInstance: true);
            var compIntNonInst = m_ArchetypeMgr.CreateComponent<int>(Shader.PropertyToID("IntNonInst"), isPerInstance: false);
            var compVec = m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector"), isPerInstance: true);

            var comps0 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compMatrix, compIntNonInst, compVec };
            var comps1 = new NativeList<GPUComponentHandle>(Allocator.Temp) { compIntNonInst, compVec };

            var arch0 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps0.AsArray()));
            var arch1 = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps1.AsArray()));

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout
            {
                { arch0, count },
                { arch1, count }
            },
            m_Resources);

            var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, comps1.AsArray(), count, Allocator.Temp);

            var compVecData = new NativeArray<Vector4>(count, Allocator.TempJob);
            var compIntNonInstData = new NativeArray<int>(1, Allocator.TempJob);
            for (int i = 0; i < count; i++)
                compVecData[i] = new Vector4(i, i, i, i);
            compIntNonInstData[0] = 123;

            GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.LockBufferForWrite,
                upload.uploadDataUIntSize,
                sizeof(uint));

            var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
            {
                upload.ScheduleWriteComponentsJob(compVecData, compVec, writeBuffer).Complete();
                upload.ScheduleWriteComponentsJob(compIntNonInstData, compIntNonInst, writeBuffer).Complete();
            }
            uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

            var archIndex1 = buffer.GetArchetypeIndex(arch1);

            var gpuIndices = new NativeArray<GPUInstanceIndex>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                gpuIndices[i] = buffer.InstanceToGPUIndex(archIndex1, i);

            buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, gpuIndices);

            buffer.SetGPULayout(m_Cmd, ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { arch1, count } }, submitCmdBuffer: true);

            archIndex1 = buffer.GetArchetypeIndex(arch1);

            var readback = new GPUInstanceDataBufferReadback<uint>();
            Assert.IsTrue(readback.Load(m_Cmd, buffer));

            for (int i = 0; i < count; i++)
            {
                var gpuIndex = buffer.InstanceToGPUIndex(archIndex1, i);
                Vector4 vec = readback.LoadData<Vector4>(compVec, gpuIndex);
                int singleInt = readback.LoadData<int>(compIntNonInst, gpuIndex);
                Assert.AreEqual(vec, new Vector4(i, i, i, i));
                Assert.AreEqual(singleInt, 123);
            }

            uploadBuffer.Release();
            upload.Dispose();
            compVecData.Dispose();
            compIntNonInstData.Dispose();
            readback.Dispose();
            buffer.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBufferGrowManyArchetypes()
        {
            const int archetypesCount = 64;

            var archetypes = new NativeArray<GPUArchetypeHandle>(archetypesCount, Allocator.Temp);
            var comps = new NativeList<GPUComponentHandle>(Allocator.Temp);

            for (int i = 0; i < archetypesCount; i++)
            {
                comps.Add(m_ArchetypeMgr.CreateComponent<Vector4>(Shader.PropertyToID("Vector" + i), isPerInstance: true));
                archetypes[i] = m_ArchetypeMgr.CreateArchetype(new GPUComponentSet(comps.AsArray()));
            }

            var buffer = new GPUInstanceDataBuffer(ref m_ArchetypeMgr, new GPUInstanceDataBufferLayout { { archetypes[0], 0 } }, m_Resources);

            for(int i = 0; i < archetypesCount; ++i)
                ChangeLayoutAndTest(i + 1);

            buffer.Dispose();

            void ChangeLayoutAndTest(int newCount)
            {
                var newLayout = new GPUInstanceDataBufferLayout();
                for (int i = 0; i < newCount; i++)
                    newLayout.Add(archetypes[i], i + 1);

                buffer.SetGPULayout(m_Cmd, ref m_ArchetypeMgr, newLayout, submitCmdBuffer: true);

                var archIndex = buffer.GetArchetypeIndex(newLayout.archetypes.Last());
                var instancesCount = newLayout.instancesCount.Last();

                var archetypeDesc = m_ArchetypeMgr.GetArchetypeDesc(newLayout.archetypes.Last());
                var upload = new GPUInstanceUploadData(ref m_ArchetypeMgr, archetypeDesc.components.AsArray(), instancesCount, Allocator.Temp);

                GraphicsBuffer uploadBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                    GraphicsBuffer.UsageFlags.LockBufferForWrite,
                    upload.uploadDataUIntSize,
                    sizeof(uint));

                var writeBuffer = uploadBuffer.LockBufferForWrite<uint>(0, upload.uploadDataUIntSize);
                for (int i = 0; i < archetypeDesc.components.Length; i++)
                {
                    var comp = archetypeDesc.components[i];
                    var checkValue = new Vector4(comp.index, comp.index, comp.index, comp.index);
                    var compData = new NativeArray<Vector4>(instancesCount, Allocator.TempJob);
                    for(int j = 0; j < instancesCount; j++)
                        compData[j] = checkValue;
                    upload.ScheduleWriteComponentsJob(compData, comp, writeBuffer).Complete();
                    compData.Dispose();
                }
                uploadBuffer.UnlockBufferAfterWrite<uint>(upload.uploadDataUIntSize);

                var newGPUIndices = new NativeArray<GPUInstanceIndex>(instancesCount, Allocator.Temp);
                for (int i = 0; i < instancesCount; i++)
                    newGPUIndices[i] = buffer.InstanceToGPUIndex(archIndex, i);

                buffer.UploadDataToGPU(m_Cmd, uploadBuffer, upload, newGPUIndices);

                var readback = new GPUInstanceDataBufferReadback<Vector4>();
                Assert.IsTrue(readback.Load(m_Cmd, buffer));

                for (int a = 0; a < newCount; a++)
                {
                    archetypeDesc = m_ArchetypeMgr.GetArchetypeDesc(newLayout.archetypes[a]);
                    archIndex = buffer.GetArchetypeIndex(newLayout.archetypes[a]);
                    instancesCount = newLayout.instancesCount[a];

                    for (int c = 0; c < archetypeDesc.components.Length; c++)
                    {
                        var comp = archetypeDesc.components[c];
                        var checkValue = new Vector4(comp.index, comp.index, comp.index, comp.index);

                        for (int i = 0; i < instancesCount; i++)
                        {
                            Vector4 element = readback.LoadData<Vector4>(comp, buffer.InstanceToGPUIndex(archIndex, i));
                            Assert.AreEqual(element, checkValue);
                        }
                    }
                }

                uploadBuffer.Release();
                upload.Dispose();
                readback.Dispose();
            }
        }
    }
}
