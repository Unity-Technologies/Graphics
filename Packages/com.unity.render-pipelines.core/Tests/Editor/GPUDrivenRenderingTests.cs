using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.TestTools;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    [InitializeOnLoad]
    public class OnLoad
    {
        static bool IsGraphicsAPISupported()
        {
            var gfxAPI = SystemInfo.graphicsDeviceType;
            //@Any other API we should ignore ?
            if (gfxAPI == GraphicsDeviceType.OpenGLCore)
                return false;
            return true;
        }

        static OnLoad()
        {
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("ignoreGfxAPI", !IsGraphicsAPISupported());
        }
    }

    class GPUDrivenRenderingTests
    {
        private MeshTestData m_MeshTestData;
        private RenderPassTest m_RenderPipe;
        private RenderPipelineAsset m_OldPipelineAsset;
        private GPUResidentDrawerResources m_Resources;

        class BoxedCounter
        {
            public int Value { get; set; }
        }

        [SetUp]
        public void OnSetup()
        {
            m_MeshTestData.Initialize();
            m_RenderPipe = ScriptableObject.CreateInstance<RenderPassTest>();
            m_OldPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.defaultRenderPipeline = m_RenderPipe;
            m_Resources = ScriptableObject.CreateInstance<GPUResidentDrawerResources>();
            ResourceReloader.ReloadAllNullIn(m_Resources, "Packages/com.unity.render-pipelines.core/");
        }

        [TearDown]
        public void OnTearDown()
        {
            m_RenderPipe = null;
            m_MeshTestData.Dispose();
            GraphicsSettings.defaultRenderPipeline = m_OldPipelineAsset;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceCullingBatcherAddRemove()
        {
            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var objIDs = new NativeList<int>(Allocator.TempJob);

            Shader dotsShader = Shader.Find("Unlit/SimpleDots");
            var dotsMaterial = new Material(dotsShader);
            foreach (var obj in objList)
            {
                obj.material = dotsMaterial;
                objIDs.Add(obj.GetInstanceID());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.maxInstances = 4096;
            rbcDesc.supportDitheringCrossFade = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                using (var brg = new InstanceCullingBatcher(brgContext, InstanceCullingBatcherDesc.NewDefault(), gpuDrivenProcessor, null, null, null))
                {
                    brg.UpdateRenderers(objIDs.AsArray());

                    Assert.IsTrue(brg.GetInstanceData().valid);
                    Assert.IsTrue(brg.GetInstanceData().drawInstances.Length == 3);

                    brgContext.QueryInstanceData(objIDs.AsArray(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(),
                        instances, new NativeArray<InstanceHandle>(), new NativeArray<InstanceHandle>(),
                        new NativeList<KeyValuePair<InstanceHandle, int>>(), new NativeList<InstanceHandle>());

                    brg.DestroyInstances(instances);

                    Assert.IsTrue(brg.GetInstanceData().drawInstances.Length == 0);
                }
            }

            gpuDrivenProcessor.Dispose();

            instances.Dispose();
            objIDs.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceCullingTier0()
        {
            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var objIDs = new NativeList<int>(Allocator.TempJob);

            Shader simpleDots = Shader.Find("Unlit/SimpleDots");
            Material simpleDotsMat = new Material(simpleDots);

            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                objIDs.Add(obj.GetInstanceID());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.maxInstances = 1;
            rbcDesc.supportDitheringCrossFade = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            //Using instance count of 1 to test for instance grow
            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var callbackCounter = new BoxedCounter();
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

                    jobHandle.Complete();
                    BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                    var materials = new NativeParallelHashSet<BatchMaterialID>(10, Allocator.Temp);

                    var drawCommandCount = 0U;
                    unsafe
                    {
                        for (int i = 0; i < drawCommands.drawRangeCount; ++i)
                        {
                            BatchDrawRange range = drawCommands.drawRanges[i];
                            drawCommandCount += range.drawCommandsCount;
                            for (int c = 0; c < range.drawCommandsCount; ++c)
                            {
                                BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin + c];
                                materials.Add(cmd.materialID);
                            }
                        }
                    }

                    Assert.AreEqual(2, drawCommandCount);
                    Assert.AreEqual(1, materials.Count());
                    callbackCounter.Value += 1;

                    materials.Dispose();
                };
                using (var brg = new InstanceCullingBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor, null, null, null))
                {
                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();

                    mainCamera.Render();
                    Assert.AreEqual(1, callbackCounter.Value);

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.QueryInstanceData(objIDs.AsArray(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(),
                        instances, new NativeArray<InstanceHandle>(), new NativeArray<InstanceHandle>(),
                        new NativeList<KeyValuePair<InstanceHandle, int>>(), new NativeList<InstanceHandle>());

                    brg.DestroyInstances(instances);
                }
            }

            gpuDrivenProcessor.Dispose();

            instances.Dispose();
            objIDs.Dispose();
        }
        [Test, Ignore("Error in test shader (it is not DOTS compatible"), ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestMultipleMetadata()
        {
            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var objIDs = new NativeList<int>(Allocator.TempJob);

            Shader simpleDots = Shader.Find("Unlit/SimpleDots");
            Material simpleDotsMat = new Material(simpleDots);

            foreach (var obj in objList)
            {
                obj.receiveGI = ReceiveGI.LightProbes;
                obj.lightProbeUsage = LightProbeUsage.BlendProbes;
                obj.material = simpleDotsMat;
                objIDs.Add(obj.GetInstanceID());
            }
            objList[2].lightProbeUsage = LightProbeUsage.Off;

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(new RenderersBatchersContextDesc() { maxInstances = 64, supportDitheringCrossFade = false }, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

                    jobHandle.Complete();
                    BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                    var drawCommandCount = 0U;
                    unsafe
                    {
                        for (int i = 0; i < drawCommands.drawRangeCount; ++i)
                        {
                            BatchDrawRange range = drawCommands.drawRanges[i];
                            drawCommandCount += range.drawCommandsCount;
                            for (int c = 0; c < range.drawCommandsCount; ++c)
                            {
                                BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin + c];
                            }
                        }
                    }
                    Assert.AreEqual(3, drawCommandCount);
                };
                using (var brg = new InstanceCullingBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor, null, null, null))
                {
                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();

                    mainCamera.Render();

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.QueryInstanceData(objIDs.AsArray(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(),
                        instances, new NativeArray<InstanceHandle>(), new NativeArray<InstanceHandle>(),
                        new NativeList<KeyValuePair<InstanceHandle, int>>(), new NativeList<InstanceHandle>());

                    brg.DestroyInstances(instances);
                }
            }

            gpuDrivenProcessor.Dispose();

            instances.Dispose();
            objIDs.Dispose();
        }

        [Test, Ignore("Error in test shader (it is not DOTS compatible"), ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestCPULODSelection()
        {
            var previousLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;

            var gameObject = new GameObject("LODGroup");
            gameObject.AddComponent<LODGroup>();

            GameObject[] gos = new GameObject[] {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Capsule),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder)
            };

            var lodGroup = gameObject.GetComponent<LODGroup>();
            var lodCount = 3;
            LOD[] lods = new LOD[lodCount];
            for (var i = 0; i < lodCount; i++)
            {
                gos[i].transform.parent = gameObject.transform;
                lods[i].screenRelativeTransitionHeight = 0.3f - (0.14f * i);
                lods[i].fadeTransitionWidth = 0.0f;
                lods[i].renderers = new Renderer[1] { gos[i].GetComponent<MeshRenderer>() as Renderer };
            }
            gos[lodCount].transform.parent = gameObject.transform;
            lodGroup.SetLODs(lods);

            var lodGroupInstancesID = new NativeList<int>(Allocator.Temp);
            lodGroupInstancesID.Add(lodGroup.GetInstanceID());

            var objList = new List<MeshRenderer>();
            for (var i = 0; i < lodCount; i++)
            {
                objList.Add(gos[i].GetComponent<MeshRenderer>());
            }
            objList.Add(gos[lodCount].GetComponent<MeshRenderer>());

            var objIDs = new NativeList<int>(Allocator.TempJob);

            Shader dotsShader = Shader.Find("Unlit/SimpleDots");
            var dotsMaterial = new Material(dotsShader);
            foreach (var obj in objList)
            {
                obj.material = dotsMaterial;
                objIDs.Add(obj.GetInstanceID());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.maxInstances = 64;
            rbcDesc.supportDitheringCrossFade = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var callbackCounter = new BoxedCounter();
                var expectedMeshID = 1;
                var expectedDrawCommandCount = 2;
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

                    jobHandle.Complete();
                    BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                    var drawCommandCount = 0U;
                    unsafe
                    {
                        for (int i = 0; i < drawCommands.drawRangeCount; ++i)
                        {
                            BatchDrawRange range = drawCommands.drawRanges[i];
                            drawCommandCount += range.drawCommandsCount;
                            BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin];
                            Assert.AreEqual(expectedMeshID, cmd.meshID.value, "Incorrect mesh rendered");
                        }
                    }
                    Assert.IsTrue(drawCommandCount == expectedDrawCommandCount, "Incorrect draw command count");

                    callbackCounter.Value += 1;
                };
                using (var brg = new InstanceCullingBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor, null, null, null))
                {
                    brgContext.UpdateLODGroups(lodGroupInstancesID.AsArray());
                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();
                    mainCamera.fieldOfView = 60;

                    //Test 1 - Should render Lod0 (range 0 - 6.66)
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                    mainCamera.Render();
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -5.65f);
                    mainCamera.Render();

                    //Test 2 - Should render Lod1(range 6.66 - 12.5)
                    expectedMeshID = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -6.67f);
                    mainCamera.Render();
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -10.5f);
                    mainCamera.Render();

                    //Test 3 - Should render Lod2 (range 12.5 - 99.9)
                    expectedMeshID = 3;
                    gameObject.transform.localScale *= 0.5f;

                    // For now we have to manually dispatch lod group transform changes.
                    Vector3 worldRefPoint = lodGroup.GetWorldReferencePoint();
                    float worldSize = lodGroup.GetWorldSpaceSize();

                    var transformedLODGroups = new NativeArray<int>(1, Allocator.Temp);
                    transformedLODGroups[0] = lodGroup.GetInstanceID();

                    brgContext.TransformLODGroups(transformedLODGroups);

                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -6.5f);
                    mainCamera.Render();
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -40.3f);
                    mainCamera.Render();

                    //Test 3 - Should size cull (range 99.9 - Inf.)
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -50.4f);
                    expectedMeshID = 4;
                    expectedDrawCommandCount = 1;
                    mainCamera.Render();

                    Assert.AreEqual(7, callbackCounter.Value);

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.QueryInstanceData(objIDs.AsArray(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(),
                        instances, new NativeArray<InstanceHandle>(), new NativeArray<InstanceHandle>(),
                        new NativeList<KeyValuePair<InstanceHandle, int>>(), new NativeList<InstanceHandle>());

                    brg.DestroyInstances(instances);
                }
            }

            gpuDrivenProcessor.Dispose();

            objIDs.Dispose();
            instances.Dispose();

            QualitySettings.lodBias = previousLodBias;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestCPULODCrossfade()
        {
            var previousLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;

            var gameObject = new GameObject("LODGroup");
            gameObject.AddComponent<LODGroup>();

            GameObject[] gos = new GameObject[] {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
            };

            var lodGroup = gameObject.GetComponent<LODGroup>();
            var lodCount = 2;
            LOD[] lods = new LOD[lodCount];
            for (var i = 0; i < lodCount; i++)
            {
                gos[i].transform.parent = gameObject.transform;
                lods[i].screenRelativeTransitionHeight = 0.4f - (0.2f * i);
                lods[i].fadeTransitionWidth = 0.3f;
                lods[i].renderers = new Renderer[1] { gos[i].GetComponent<MeshRenderer>() as Renderer };
            }
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.SetLODs(lods);

            var objList = new List<MeshRenderer>();
            for (var i = 0; i < lodCount; i++)
            {
                objList.Add(gos[i].GetComponent<MeshRenderer>());
            }
            objList.Add(gos[lodCount].GetComponent<MeshRenderer>());

            var lodGroupInstancesID = new NativeList<int>(Allocator.Temp);
            lodGroupInstancesID.Add(lodGroup.GetInstanceID());

            var objIDs = new NativeList<int>(Allocator.TempJob);

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                objIDs.Add(obj.GetInstanceID());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.maxInstances = 64;
            rbcDesc.supportDitheringCrossFade = true;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var expectedMeshIDs = new List<int>();
                var expectedFlags = new List<BatchDrawCommandFlags>();
                var expectedDrawCommandCount = 0;
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

                    jobHandle.Complete();
                    BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                    unsafe
                    {
                        Assert.AreEqual(1, drawCommands.drawRangeCount);
                        BatchDrawRange range = drawCommands.drawRanges[0];
                        Assert.AreEqual(range.drawCommandsCount, expectedDrawCommandCount, " Incorrect draw Command Count");
                        for (int i = 0; i < range.drawCommandsCount; ++i)
                        {
                            BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin + i];
                            Assert.AreEqual(expectedMeshIDs[i], cmd.meshID.value, "Incorrect mesh rendered");
                            Assert.AreEqual(cmd.flags & BatchDrawCommandFlags.LODCrossFade, expectedFlags[i], "Incorrect flag for the current draw command");
                        }
                    }
                };
                using (var brg = new InstanceCullingBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor, null, null, null))
                {
                    brgContext.UpdateLODGroups(lodGroupInstancesID.AsArray());
                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();
                    mainCamera.fieldOfView = 60;

                    // Cube Mesh ID : 1 (Lod 0)
                    // Sphere Mesh ID : 2 (Lod 1 + non Loded)
                    //Test 0 - Should render Lod0 (cube) + non loded sphere
                    expectedMeshIDs.Add(1);
                    expectedMeshIDs.Add(2);
                    expectedFlags.Add(BatchDrawCommandFlags.None);
                    expectedFlags.Add(BatchDrawCommandFlags.None);
                    expectedDrawCommandCount = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                    mainCamera.Render();

                    //Test 1 - Should render Lod0 and 1 crossfaded + non loded sphere
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(1);
                    expectedMeshIDs.Add(2);
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedFlags.Add(BatchDrawCommandFlags.None);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedDrawCommandCount = 3;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -2.0f);
                    mainCamera.Render();

                    //Test 2 - Should render Lod1 + non loded sphere (single Draw Command as they are both spheres)
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.None);
                    expectedDrawCommandCount = 1;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                    mainCamera.Render();

                    //Test 3 - Should render Lod1 crossfaded + non loded sphere
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(2);
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.None);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedDrawCommandCount = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -4.0f);
                    mainCamera.Render();

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.QueryInstanceData(objIDs.AsArray(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(), new NativeArray<int>(),
                        instances, new NativeArray<InstanceHandle>(), new NativeArray<InstanceHandle>(),
                        new NativeList<KeyValuePair<InstanceHandle, int>>(), new NativeList<InstanceHandle>());

                    brg.DestroyInstances(instances);
                }
            }

            gpuDrivenProcessor.Dispose();

            objIDs.Dispose();
            instances.Dispose();

            QualitySettings.lodBias = previousLodBias;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBuffer()
        {
            var gpuResources = new GPUInstanceDataBufferUploader.GPUResources();
            gpuResources.LoadShaders(m_Resources);

            const int instanceCount = 4;

            using (var instanceBuffer = RenderersParameters.CreateInstanceDataBuffer(instanceCount))
            {
                var renderersParameters = new RenderersParameters(instanceBuffer);

                var instances = new NativeArray<InstanceHandle>(instanceCount, Allocator.TempJob);
                var lightmapTextureIndices = new NativeArray<Vector4>(instanceCount, Allocator.TempJob);
                var lightmapScaleOffsets = new NativeArray<Vector4>(instanceCount, Allocator.TempJob);

                for (int i = 0; i < instanceCount; ++i)
                    instances[i] = new InstanceHandle { index = i };

                for (int i = 0; i < instanceCount; ++i)
                    lightmapTextureIndices[i] = new Vector4(16 + i, 0.0f, 0.0f, 0.0f);

                for (int i = 0; i < instanceCount; ++i)
                    lightmapScaleOffsets[i] = Vector4.one * i;

                using (var instanceUploader0 = new GPUInstanceDataBufferUploader(instanceBuffer.descriptions, instanceCount))
                {
                    instanceUploader0.AllocateInstanceHandles(instances);

                    instanceUploader0.WriteInstanceData(renderersParameters.lightmapIndex.index, lightmapTextureIndices);
                    instanceUploader0.WriteInstanceData(renderersParameters.lightmapScale.index, lightmapScaleOffsets);
                    instanceUploader0.SubmitToGpu(instanceBuffer, instances, ref gpuResources);

                }

                using (var readbackData = new InstanceDataBufferCPUReadbackData())
                {
                    readbackData.Load(instanceBuffer);

                    for (int i = 0; i < instanceCount; ++i)
                    {
                        var lightmapIndex = readbackData.LoadData<Vector4>(instances[i].index, RenderersParameters.ParamNames.unity_LightmapIndex);
                        var lightmapScaleOffset = readbackData.LoadData<Vector4>(instances[i].index, RenderersParameters.ParamNames.unity_LightmapST);

                        Assert.AreEqual(lightmapIndex, lightmapTextureIndices[i]);
                        Assert.AreEqual(lightmapScaleOffset, lightmapScaleOffsets[i]);
                    }
                }

                instances.Dispose();
                lightmapTextureIndices.Dispose();
                lightmapScaleOffsets.Dispose();
            }
            gpuResources.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestGrowInstanceDataBuffer()
        {
            var uploadResources = new GPUInstanceDataBufferUploader.GPUResources();
            var growResources = new GPUInstanceDataBufferGrower.GPUResources();
            uploadResources.LoadShaders(m_Resources);
            growResources.LoadShaders(m_Resources);

            const int instanceCount = 8;

            using (var instanceBuffer = RenderersParameters.CreateInstanceDataBuffer(instanceCount))
            {
                var renderersParameters = new RenderersParameters(instanceBuffer);

                var instances = new NativeArray<InstanceHandle>(instanceCount, Allocator.TempJob);
                var lightmapTextureIndices = new NativeArray<Vector4>(instanceCount, Allocator.TempJob);
                var lightmapScaleOffsets = new NativeArray<Vector4>(instanceCount, Allocator.TempJob);

                for (int i = 0; i < instanceCount; ++i)
                    instances[i] = new InstanceHandle { index = i };

                for (int i = 0; i < instanceCount; ++i)
                    lightmapTextureIndices[i] = new Vector4(16 + i, 0.0f, 0.0f, 0.0f);

                for (int i = 0; i < instanceCount; ++i)
                    lightmapScaleOffsets[i] = Vector4.one * i;

                using (var instanceUploader0 = new GPUInstanceDataBufferUploader(instanceBuffer.descriptions, instanceCount))
                {
                    instanceUploader0.AllocateInstanceHandles(instances);

                    instanceUploader0.WriteInstanceData(renderersParameters.lightmapIndex.index, lightmapTextureIndices);
                    instanceUploader0.WriteInstanceData(renderersParameters.lightmapScale.index, lightmapScaleOffsets);
                    instanceUploader0.SubmitToGpu(instanceBuffer, instances, ref uploadResources);

                }

                var instanceGrower = new GPUInstanceDataBufferGrower(instanceBuffer, instanceCount * 2);
                var newGPUDataBuffer = instanceGrower.SubmitToGpu(ref growResources);
                instanceGrower.Dispose();

                using (var readbackData = new InstanceDataBufferCPUReadbackData())
                {
                    readbackData.Load(newGPUDataBuffer);

                    for (int i = 0; i < instanceCount; ++i)
                    {
                        var lightmapIndex = readbackData.LoadData<Vector4>(instances[i].index, RenderersParameters.ParamNames.unity_LightmapIndex);
                        var lightmapScaleOffset = readbackData.LoadData<Vector4>(instances[i].index, RenderersParameters.ParamNames.unity_LightmapST);

                        Assert.AreEqual(lightmapIndex, lightmapTextureIndices[i]);
                        Assert.AreEqual(lightmapScaleOffset, lightmapScaleOffsets[i]);
                    }
                }

                newGPUDataBuffer.Dispose();

                instances.Dispose();
                lightmapTextureIndices.Dispose();
                lightmapScaleOffsets.Dispose();
            }
            uploadResources.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstancePool()
        {
            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var instancePool = new GPURendererInstancePool(5, enableBoundingSpheres: false, m_Resources))
            {
                var simpleDots = Shader.Find("Unlit/SimpleDots");
                var simpleDotsMat = new Material(simpleDots);

                var gameObjects = new GameObject[7]
                {
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube)
                };

                foreach(var go in gameObjects)
                {
                    MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = simpleDotsMat;
                }

                var renderersID = new NativeArray<int>(3, Allocator.TempJob);
                renderersID[0] = gameObjects[0].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[1] = gameObjects[1].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[2] = gameObjects[2].GetComponent<MeshRenderer>().GetInstanceID();

                var lodGroupDataMap = new NativeParallelHashMap<int, InstanceHandle>(64, Allocator.TempJob);

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(3, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;
                    instances[1] = InstanceHandle.Invalid;
                    instances[2] = InstanceHandle.Invalid;

                    instancePool.Resize(3);
                    instancePool.AllocateInstances(renderersID, instances, 3);
                    instancePool.UpdateInstanceData(instances, rendererData, lodGroupDataMap);

                    Assert.IsTrue(instancePool.InternalSanityCheckStates());

                    instancePool.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instancePool.InternalSanityCheckStates());

                renderersID[0] = gameObjects[3].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[1] = gameObjects[4].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[2] = gameObjects[5].GetComponent<MeshRenderer>().GetInstanceID();

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(3, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;
                    instances[1] = InstanceHandle.Invalid;
                    instances[2] = InstanceHandle.Invalid;

                    instancePool.AllocateInstances(renderersID, instances, 3);
                    instancePool.UpdateInstanceData(instances, rendererData, lodGroupDataMap);

                    Assert.IsTrue(instancePool.InternalSanityCheckStates());

                    instancePool.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instancePool.InternalSanityCheckStates());

                renderersID.Dispose();

                renderersID = new NativeArray<int>(1, Allocator.TempJob);
                renderersID[0] = gameObjects[6].GetComponent<MeshRenderer>().GetInstanceID();

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(1, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;

                    instancePool.AllocateInstances(renderersID, instances, 1);
                    instancePool.UpdateInstanceData(instances, rendererData, lodGroupDataMap);

                    Assert.IsTrue(instancePool.InternalSanityCheckStates());

                    instancePool.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instancePool.InternalSanityCheckStates());

                renderersID.Dispose();
                lodGroupDataMap.Dispose();

                foreach (var go in gameObjects)
                    GameObject.DestroyImmediate(go);

                GameObject.DestroyImmediate(simpleDotsMat);
            }

            gpuDrivenProcessor.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestStaticBatching()
        {
            var gpuDrivenProcessor = new GPUDrivenProcessor();

            var dispatcher = new ObjectDispatcher();
            dispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);

            using (var brgContext = new RenderersBatchersContext(RenderersBatchersContextDesc.NewDefault(), gpuDrivenProcessor, m_Resources))
            {
                var simpleDots = Shader.Find("Unlit/SimpleDots");
                var simpleDotsMat = new Material(simpleDots);

                var staticBatchingRoot = new GameObject();
                staticBatchingRoot.transform.position = new Vector3(10, 0, 0);

                var gameObjects = new GameObject[2]
                {
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                    GameObject.CreatePrimitive(PrimitiveType.Cube)
                };

                foreach (var go in gameObjects)
                {
                    MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = simpleDotsMat;
                }

                gameObjects[0].transform.position = new Vector3(2, 0, 0);
                gameObjects[1].transform.position = new Vector3(-2, 0, 0);

                StaticBatchingUtility.Combine(gameObjects, staticBatchingRoot);

                var renderersID = new NativeArray<int>(2, Allocator.TempJob);
                renderersID[0] = gameObjects[0].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[1] = gameObjects[1].GetComponent<MeshRenderer>().GetInstanceID();

                var lodGroupDataMap = new NativeParallelHashMap<int, InstanceHandle>(64, Allocator.TempJob);
                var instances = new NativeArray<InstanceHandle>(2, Allocator.TempJob);
                var localToWorldMatrices = new NativeArray<Matrix4x4>(2, Allocator.Temp);

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    Assert.IsTrue(rendererData.packedRendererData[0].isPartOfStaticBatch);
                    Assert.IsTrue(rendererData.packedRendererData[1].isPartOfStaticBatch);

                    brgContext.AllocateOrGetInstances(rendererData.rendererID, instances);
                    brgContext.UpdateInstanceData(instances, rendererData);
                    brgContext.ReinitializeInstanceTransforms(instances, rendererData.localToWorldMatrix, rendererData.localToWorldMatrix);

                    localToWorldMatrices.CopyFrom(rendererData.localToWorldMatrix);
                });

                gameObjects[0].transform.position = new Vector3(100, 0, 0);
                gameObjects[1].transform.position = new Vector3(-100, 0, 0);

                var transfomData = dispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
                Assert.AreEqual(transfomData.transformedID.Length, 2);

                brgContext.TransformInstances(instances, transfomData.localToWorldMatrices);

                Assert.AreEqual(brgContext.GetInstanceLocalToWorldMatrix(instances[0]), localToWorldMatrices[0]);
                Assert.AreEqual(brgContext.GetInstanceLocalToWorldMatrix(instances[1]), localToWorldMatrices[1]);

                transfomData.Dispose();
                renderersID.Dispose();
                lodGroupDataMap.Dispose();
                instances.Dispose();

                foreach (var go in gameObjects)
                    GameObject.DestroyImmediate(go);

                GameObject.DestroyImmediate(simpleDotsMat);
                GameObject.DestroyImmediate(staticBatchingRoot);
            }

            gpuDrivenProcessor.Dispose();
            dispatcher.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestDisallowGPUDrivenRendering()
        {
            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var gameObject0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer0 = gameObject0.GetComponent<MeshRenderer>();
            var renderer1 = gameObject1.GetComponent<MeshRenderer>();
            renderer0.sharedMaterial = simpleDotsMat;
            renderer1.sharedMaterial = simpleDotsMat;

            var rendererIDs = new NativeArray<int>(2, Allocator.Temp);
            rendererIDs[0] = renderer0.GetInstanceID();
            rendererIDs[1] = renderer1.GetInstanceID();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererID.Length == 2);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer0.allowGPUDrivenRendering = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererID.Length == 1);
                Assert.IsTrue(rendererData.rendererID[0] == renderer1.GetInstanceID());
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer1.allowGPUDrivenRendering = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                dispatched = true;
            });

            Assert.IsFalse(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);

            gpuDrivenProcessor.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestUnsupportedCallbacks()
        {
            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var gameObject0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject3 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var renderer0 = gameObject0.GetComponent<MeshRenderer>();
            var renderer1 = gameObject1.GetComponent<MeshRenderer>();
            var renderer2 = gameObject1.GetComponent<MeshRenderer>();
            var renderer3 = gameObject1.GetComponent<MeshRenderer>();

            renderer0.sharedMaterial = simpleDotsMat;
            renderer1.sharedMaterial = simpleDotsMat;
            renderer2.sharedMaterial = simpleDotsMat;
            renderer3.sharedMaterial = simpleDotsMat;

            var rendererIDs = new NativeArray<int>(4, Allocator.Temp);
            rendererIDs[0] = renderer0.GetInstanceID();
            rendererIDs[1] = renderer1.GetInstanceID();
            rendererIDs[2] = renderer2.GetInstanceID();
            rendererIDs[3] = renderer3.GetInstanceID();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererID.Length == 4);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            gameObject1.AddComponent<OnWillRenderObjectBehaviour>();
            gameObject2.AddComponent<OnBecameInvisibleBehaviour>();
            gameObject3.AddComponent<OnBecameVisibleBehaviour>();

            dispatched = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererID.Length == 1);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);
            Object.DestroyImmediate(gameObject2);
            Object.DestroyImmediate(gameObject3);

            gpuDrivenProcessor.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestForceRenderingOff()
        {
            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var gameObject0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var renderer0 = gameObject0.GetComponent<MeshRenderer>();
            var renderer1 = gameObject1.GetComponent<MeshRenderer>();

            renderer0.sharedMaterial = simpleDotsMat;
            renderer1.sharedMaterial = simpleDotsMat;

            renderer0.forceRenderingOff = true;

            var rendererIDs = new NativeArray<int>(2, Allocator.Temp);
            rendererIDs[0] = renderer0.GetInstanceID();
            rendererIDs[1] = renderer1.GetInstanceID();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (GPUDrivenRendererData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererID.Length == 1);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);

            gpuDrivenProcessor.Dispose();
        }

        class OnWillRenderObjectBehaviour : MonoBehaviour { void OnWillRenderObject() { } }
        class OnBecameInvisibleBehaviour : MonoBehaviour { void OnBecameInvisible() { } }
        class OnBecameVisibleBehaviour : MonoBehaviour { void OnBecameVisible() { } }
    }
}
