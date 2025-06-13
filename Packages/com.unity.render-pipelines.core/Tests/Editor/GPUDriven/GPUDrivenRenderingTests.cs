using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.TestTools;
using UnityEditor;
using Unity.Mathematics;
using NUnit.Framework.Internal;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    [InitializeOnLoad]
    class OnLoad
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
        private RenderPassGlobalSettings m_GlobalSettings;
        private bool m_oldEnableLodCrossFade;

        class BoxedCounter
        {
            public int Value { get; set; }
        }

        public void SubmitCameraRenderRequest(Camera camera)
        {
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest();

            RenderTextureDescriptor desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, RenderTextureFormat.Default, 32);
            request.destination = RenderTexture.GetTemporary(desc);

            // Check if the active render pipeline supports the render request
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            RenderTexture.ReleaseTemporary(request.destination);
        }

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
            m_MeshTestData.Initialize();
            m_RenderPipe = ScriptableObject.CreateInstance<RenderPassTest>();
            m_OldPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.defaultRenderPipeline = m_RenderPipe;
            m_Resources = GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
            m_oldEnableLodCrossFade = QualitySettings.enableLODCrossFade;
            QualitySettings.enableLODCrossFade = true;
        }

        [TearDown]
        public void OnTearDown()
        {
            m_RenderPipe = null;
            m_MeshTestData.Dispose();
            GraphicsSettings.defaultRenderPipeline = m_OldPipelineAsset;
            QualitySettings.enableLODCrossFade = m_oldEnableLodCrossFade;
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

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

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
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: 4096, 0);
            rbcDesc.supportDitheringCrossFade = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                using (var brg = new GPUResidentBatcher(brgContext, InstanceCullingBatcherDesc.NewDefault(), gpuDrivenProcessor))
                {
                    brg.UpdateRenderers(objIDs.AsArray());

                    Assert.IsTrue(brg.instanceCullingBatcher.GetDrawInstanceData().valid);
                    Assert.IsTrue(brg.instanceCullingBatcher.GetDrawInstanceData().drawInstances.Length == 3);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);

                    Assert.IsTrue(brg.instanceCullingBatcher.GetDrawInstanceData().drawInstances.Length == 0);
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

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

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
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: 1, 0);
            rbcDesc.supportDitheringCrossFade = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            //Using instance count of 1 to test for instance grow
            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var callbackCounter = new BoxedCounter();
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

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

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();

                    SubmitCameraRenderRequest(mainCamera);

                    brg.OnEndContextRendering();

                    Assert.AreEqual(1, callbackCounter.Value);

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);
                }
            }

            gpuDrivenProcessor.Dispose();

            instances.Dispose();
            objIDs.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        [Ignore("Disabled for Instability https://jira.unity3d.com/browse/UUM-71039")]
        public void TestSceneViewHiddenRenderersCullingTier0()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = simpleDotsMat;

            var objIDs = new NativeArray<EntityId>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var instances = new NativeArray<InstanceHandle>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            objIDs[0] = renderer.GetInstanceID();
            instances[0] = InstanceHandle.Invalid;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(RenderersBatchersContextDesc.NewDefault(), gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var callbackCounter = new BoxedCounter();

                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

                    BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];
                    callbackCounter.Value = drawCommands.visibleInstanceCount;
                };

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

                    brg.UpdateRenderers(objIDs);

                    var cameraObject = new GameObject("SceneViewCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();
                    mainCamera.cameraType = CameraType.SceneView;

                    SceneVisibilityManager.instance.Hide(go, true);

                    brg.OnBeginCameraRendering(mainCamera);
                    SubmitCameraRenderRequest(mainCamera);
                    brg.OnEndCameraRendering(mainCamera);
                    Assert.AreEqual(callbackCounter.Value, 0);

                    SceneVisibilityManager.instance.Show(go, true);

                    brg.OnBeginCameraRendering(mainCamera);
                    SubmitCameraRenderRequest(mainCamera);
                    brg.OnEndCameraRendering(mainCamera);
                    Assert.AreEqual(callbackCounter.Value, 1);

                    brg.OnEndContextRendering();

                    GameObject.DestroyImmediate(cameraObject);
                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs, instances).Complete();
                    brg.DestroyDrawInstances(instances);
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

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

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

            using (var brgContext = new RenderersBatchersContext(new RenderersBatchersContextDesc() { instanceNumInfo = new InstanceNumInfo(meshRendererNum: 64, 0), supportDitheringCrossFade = false }, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

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

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();

                    SubmitCameraRenderRequest(mainCamera);

                    brg.OnEndContextRendering();

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);
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

            var lodGroupInstancesID = new NativeList<EntityId>(Allocator.TempJob);
            lodGroupInstancesID.Add(lodGroup.GetEntityId());

            var objList = new List<MeshRenderer>();
            for (var i = 0; i < lodCount; i++)
            {
                objList.Add(gos[i].GetComponent<MeshRenderer>());
            }
            objList.Add(gos[lodCount].GetComponent<MeshRenderer>());

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

            Shader dotsShader = Shader.Find("Unlit/SimpleDots");
            var dotsMaterial = new Material(dotsShader);
            foreach (var obj in objList)
            {
                obj.material = dotsMaterial;
                objIDs.Add(obj.GetEntityId());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: 64, 0);
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
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

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

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

                    brgContext.UpdateLODGroups(lodGroupInstancesID.AsArray());
                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();
                    mainCamera.fieldOfView = 60;

                    //Test 1 - Should render Lod0 (range 0 - 6.66)
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                    SubmitCameraRenderRequest(mainCamera);
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -5.65f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 2 - Should render Lod1(range 6.66 - 12.5)
                    expectedMeshID = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -6.67f);
                    SubmitCameraRenderRequest(mainCamera);
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -10.5f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 3 - Should render Lod2 (range 12.5 - 99.9)
                    expectedMeshID = 3;
                    gameObject.transform.localScale *= 0.5f;

                    // For now we have to manually dispatch lod group transform changes.
                    Vector3 worldRefPoint = lodGroup.GetWorldReferencePoint();
                    float worldSize = lodGroup.GetWorldSpaceSize();

                    var transformedLODGroups = new NativeArray<EntityId>(1, Allocator.Temp);
                    transformedLODGroups[0] = lodGroup.GetEntityId();

                    brgContext.TransformLODGroups(transformedLODGroups);

                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -6.5f);
                    SubmitCameraRenderRequest(mainCamera);
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -40.3f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 3 - Should size cull (range 99.9 - Inf.)
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -50.4f);
                    expectedMeshID = 4;
                    expectedDrawCommandCount = 1;
                    SubmitCameraRenderRequest(mainCamera);

                    brg.OnEndContextRendering();

                    Assert.AreEqual(7, callbackCounter.Value);

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);
                }
            }

            lodGroupInstancesID.Dispose();
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

            var lodGroupInstancesID = new NativeList<EntityId>(Allocator.TempJob);
            lodGroupInstancesID.Add(lodGroup.GetEntityId());

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                objIDs.Add(obj.GetEntityId());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: 64, 0);
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
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

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
                            Assert.AreEqual(expectedFlags[i], cmd.flags & BatchDrawCommandFlags.LODCrossFade, "Incorrect flag for the current draw command");
                        }
                    }
                };

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

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
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedDrawCommandCount = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 1 - Should render Lod0 and 1 crossfaded + non loded sphere
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(1);
                    expectedMeshIDs.Add(2);
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedDrawCommandCount = 3;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -2.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 2 - Should render Lod1 + non loded sphere (single Draw Command as they are both spheres)
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedDrawCommandCount = 1;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 3 - Should render Lod1 crossfaded + non loded sphere
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(2);
                    expectedMeshIDs.Add(2);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedDrawCommandCount = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -4.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    brg.OnEndContextRendering();

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);
                }
            }

            lodGroupInstancesID.Dispose();
            gpuDrivenProcessor.Dispose();

            objIDs.Dispose();
            instances.Dispose();

            QualitySettings.lodBias = previousLodBias;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestGpuDrivenSmallMeshCulling()
        {
            var gameObject = new GameObject("Root");
            var sphere0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere0.transform.parent = gameObject.transform;
            var sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere1.AddComponent<DisallowSmallMeshCulling>();
            sphere1.transform.parent = gameObject.transform;

            var objList = new List<MeshRenderer>();
            objList.Add(sphere0.GetComponent<MeshRenderer>());
            objList.Add(sphere1.GetComponent<MeshRenderer>());

            var objIDs = new NativeList<EntityId>(Allocator.TempJob);

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                objIDs.Add(obj.GetEntityId());
            }

            var instances = new NativeArray<InstanceHandle>(objList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < objList.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var rbcDesc = RenderersBatchersContextDesc.NewDefault();
            rbcDesc.instanceNumInfo = new InstanceNumInfo(meshRendererNum: 64, 0);
            rbcDesc.supportDitheringCrossFade = true;
            rbcDesc.smallMeshScreenPercentage = 10.0f;

            var lastLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var brgContext = new RenderersBatchersContext(rbcDesc, gpuDrivenProcessor, m_Resources))
            {
                var cpuDrivenDesc = InstanceCullingBatcherDesc.NewDefault();
                var expectedMeshIDs = new List<int>();
                var expectedFlags = new List<BatchDrawCommandFlags>();
                var expectedDrawCommandCount = 0;
                cpuDrivenDesc.onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
                {
                    jobHandle.Complete();

                    if (cc.viewType != BatchCullingViewType.Camera)
                        return;

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
                            Assert.AreEqual(expectedFlags[i], cmd.flags & BatchDrawCommandFlags.LODCrossFade, "Incorrect flag for the current draw command");
                        }
                    }
                };

                using (var brg = new GPUResidentBatcher(brgContext, cpuDrivenDesc, gpuDrivenProcessor))
                {
                    brg.OnBeginContextRendering();

                    brg.UpdateRenderers(objIDs.AsArray());

                    var cameraObject = new GameObject("myCamera");
                    var mainCamera = cameraObject.AddComponent<Camera>();
                    mainCamera.fieldOfView = 60;

                    //Test 0 - (1m) Should render both spheres.
                    expectedMeshIDs.Add(1);
                    expectedMeshIDs.Add(1);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedDrawCommandCount = 1;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 1 - (8.5m) Should render sphere1 + crossfaded sphere0.
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(1);
                    expectedMeshIDs.Add(1);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
                    expectedDrawCommandCount = 2;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -8.5f);
                    SubmitCameraRenderRequest(mainCamera);

                    //Test 2 - (10m) Should only render sphere1.
                    expectedMeshIDs.Clear();
                    expectedMeshIDs.Add(1);
                    expectedFlags.Clear();
                    expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
                    expectedDrawCommandCount = 1;
                    cameraObject.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
                    SubmitCameraRenderRequest(mainCamera);

                    brg.OnEndContextRendering();

                    mainCamera = null;
                    GameObject.DestroyImmediate(cameraObject);

                    brgContext.ScheduleQueryRendererGroupInstancesJob(objIDs.AsArray(), instances).Complete();
                    brg.DestroyDrawInstances(instances);
                }
            }

            QualitySettings.lodBias = lastLodBias;

            gpuDrivenProcessor.Dispose();

            objIDs.Dispose();
            instances.Dispose();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceDataBuffer()
        {
            var gpuResources = new GPUInstanceDataBufferUploader.GPUResources();
            gpuResources.LoadShaders(m_Resources);

            var meshInstancesCount = 4;
            var instanceNumInfo = new InstanceNumInfo(meshRendererNum: meshInstancesCount, 0);

            using (var instanceBuffer = RenderersParameters.CreateInstanceDataBuffer(RenderersParameters.Flags.None, instanceNumInfo))
            {
                var renderersParameters = new RenderersParameters(instanceBuffer);

                var instances = new NativeArray<GPUInstanceIndex>(meshInstancesCount, Allocator.TempJob);
                var lightmapScaleOffsets = new NativeArray<Vector4>(meshInstancesCount, Allocator.TempJob);

                for (int i = 0; i < meshInstancesCount; ++i)
                    instances[i] = new GPUInstanceIndex { index = i };

                for (int i = 0; i < meshInstancesCount; ++i)
                    lightmapScaleOffsets[i] = Vector4.one * i;

                using (var instanceUploader0 = new GPUInstanceDataBufferUploader(instanceBuffer.descriptions, meshInstancesCount, InstanceType.MeshRenderer))
                {
                    instanceUploader0.AllocateUploadHandles(instances.Length);
                    instanceUploader0.WriteInstanceDataJob(renderersParameters.lightmapScale.index, lightmapScaleOffsets).Complete();
                    instanceUploader0.SubmitToGpu(instanceBuffer, instances, ref gpuResources, submitOnlyWrittenParams: false);
                }

                using (var readbackData = new InstanceDataBufferCPUReadbackData())
                {
                    if (readbackData.Load(instanceBuffer))
                    {
                        for (int i = 0; i < meshInstancesCount; ++i)
                        {
                            var lightmapScaleOffset = readbackData.LoadData<Vector4>(instances[i], RenderersParameters.ParamNames.unity_LightmapST);
                            Assert.AreEqual(lightmapScaleOffset, lightmapScaleOffsets[i]);
                        }
                    }
                }

                instances.Dispose();
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

            var meshInstancesCount = 8;
            var instanceNumInfo = new InstanceNumInfo(meshRendererNum: meshInstancesCount, 0);

            using (var instanceBuffer = RenderersParameters.CreateInstanceDataBuffer(RenderersParameters.Flags.None, instanceNumInfo))
            {
                var renderersParameters = new RenderersParameters(instanceBuffer);

                var instances = new NativeArray<GPUInstanceIndex>(meshInstancesCount, Allocator.TempJob);
                var lightmapTextureIndices = new NativeArray<Vector4>(meshInstancesCount, Allocator.TempJob);
                var lightmapScaleOffsets = new NativeArray<Vector4>(meshInstancesCount, Allocator.TempJob);

                for (int i = 0; i < meshInstancesCount; ++i)
                    instances[i] = new GPUInstanceIndex {  index = i };

                for (int i = 0; i < meshInstancesCount; ++i)
                    lightmapScaleOffsets[i] = Vector4.one * i;

                using (var instanceUploader0 = new GPUInstanceDataBufferUploader(instanceBuffer.descriptions, meshInstancesCount, InstanceType.MeshRenderer))
                {
                    instanceUploader0.AllocateUploadHandles(instances.Length);
                    instanceUploader0.WriteInstanceDataJob(renderersParameters.lightmapScale.index, lightmapScaleOffsets).Complete();
                    instanceUploader0.SubmitToGpu(instanceBuffer, instances, ref uploadResources, submitOnlyWrittenParams: false);
                }

                var instanceGrower = new GPUInstanceDataBufferGrower(instanceBuffer, new InstanceNumInfo(meshRendererNum: meshInstancesCount * 2, 0));
                var newGPUDataBuffer = instanceGrower.SubmitToGpu(ref growResources);
                instanceGrower.Dispose();

                using (var readbackData = new InstanceDataBufferCPUReadbackData())
                {
                    if (readbackData.Load(newGPUDataBuffer))
                    {
                        for (int i = 0; i < meshInstancesCount; ++i)
                        {
                            var lightmapScaleOffset = readbackData.LoadData<Vector4>(instances[i], RenderersParameters.ParamNames.unity_LightmapST);
                            Assert.AreEqual(lightmapScaleOffset, lightmapScaleOffsets[i]);
                        }
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
        public void TestInstanceData()
        {
            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var instanceSystem = new InstanceDataSystem(5, enableBoundingSpheres: false, m_Resources))
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

                var renderersID = new NativeArray<EntityId>(3, Allocator.TempJob);
                renderersID[0] = gameObjects[0].GetComponent<MeshRenderer>().GetEntityId();
                renderersID[1] = gameObjects[1].GetComponent<MeshRenderer>().GetEntityId();
                renderersID[2] = gameObjects[2].GetComponent<MeshRenderer>().GetEntityId();

                var lodGroupDataMap = new NativeParallelHashMap<int, GPUInstanceIndex>(64, Allocator.TempJob);

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(3, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;
                    instances[1] = InstanceHandle.Invalid;
                    instances[2] = InstanceHandle.Invalid;

                    instanceSystem.ReallocateAndGetInstances(rendererData, instances);
                    instanceSystem.ScheduleUpdateInstanceDataJob(instances, rendererData, lodGroupDataMap).Complete();

                    Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

                    instanceSystem.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

                renderersID[0] = gameObjects[3].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[1] = gameObjects[4].GetComponent<MeshRenderer>().GetInstanceID();
                renderersID[2] = gameObjects[5].GetComponent<MeshRenderer>().GetInstanceID();

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(3, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;
                    instances[1] = InstanceHandle.Invalid;
                    instances[2] = InstanceHandle.Invalid;

                    instanceSystem.ReallocateAndGetInstances(rendererData, instances);
                    instanceSystem.ScheduleUpdateInstanceDataJob(instances, rendererData, lodGroupDataMap).Complete();

                    Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

                    instanceSystem.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

                renderersID.Dispose();

                renderersID = new NativeArray<EntityId>(1, Allocator.TempJob);
                renderersID[0] = gameObjects[6].GetComponent<MeshRenderer>().GetInstanceID();

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    var instances = new NativeArray<InstanceHandle>(1, Allocator.TempJob);
                    instances[0] = InstanceHandle.Invalid;

                    instanceSystem.ReallocateAndGetInstances(rendererData, instances);
                    instanceSystem.ScheduleUpdateInstanceDataJob(instances, rendererData, lodGroupDataMap).Complete();

                    Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

                    instanceSystem.FreeInstances(instances);

                    instances.Dispose();
                });

                Assert.IsTrue(instanceSystem.InternalSanityCheckStates());

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

                var renderersID = new NativeArray<EntityId>(2, Allocator.TempJob);
                renderersID[0] = gameObjects[0].GetComponent<MeshRenderer>().GetEntityId();
                renderersID[1] = gameObjects[1].GetComponent<MeshRenderer>().GetEntityId();

                var lodGroupDataMap = new NativeParallelHashMap<int, InstanceHandle>(64, Allocator.TempJob);
                var instances = new NativeArray<InstanceHandle>(2, Allocator.TempJob);
                var localToWorldMatrices = new NativeArray<float4x4>(2, Allocator.Temp);

                gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
                {
                    Assert.IsTrue(rendererData.packedRendererData[0].isPartOfStaticBatch);
                    Assert.IsTrue(rendererData.packedRendererData[1].isPartOfStaticBatch);

                    brgContext.ReallocateAndGetInstances(rendererData, instances);
                    brgContext.ScheduleUpdateInstanceDataJob(instances, rendererData).Complete();
                    brgContext.InitializeInstanceTransforms(instances, rendererData.localToWorldMatrix, rendererData.localToWorldMatrix);

                    for(int i = 0; i < localToWorldMatrices.Length; ++i)
                        localToWorldMatrices[i] = rendererData.localToWorldMatrix[i];
                });

                gameObjects[0].transform.position = new Vector3(100, 0, 0);
                gameObjects[1].transform.position = new Vector3(-100, 0, 0);

                var transfomData = dispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, Allocator.TempJob);
                Assert.AreEqual(transfomData.transformedID.Length, 2);

                brgContext.UpdateInstanceTransforms(instances, transfomData.localToWorldMatrices);

                using (var readbackData = new InstanceDataBufferCPUReadbackData())
                {
                    if (readbackData.Load(brgContext.GetInstanceDataBuffer()))
                    {
                        var localToWorldMatrix0 = readbackData.LoadData<PackedMatrix>(instances[0], RenderersParameters.ParamNames.unity_ObjectToWorld);
                        var localToWorldMatrix1 = readbackData.LoadData<PackedMatrix>(instances[1], RenderersParameters.ParamNames.unity_ObjectToWorld);

                        Assert.AreEqual(localToWorldMatrix0, PackedMatrix.FromMatrix4x4(localToWorldMatrices[0]));
                        Assert.AreEqual(localToWorldMatrix1, PackedMatrix.FromMatrix4x4(localToWorldMatrices[1]));
                    }
                    else
                    {
                        Assert.IsTrue(false, "Unable to read instance data.");
                    }
                }

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

            var rendererIDs = new NativeArray<EntityId>(2, Allocator.Temp);
            rendererIDs[0] = renderer0.GetEntityId();
            rendererIDs[1] = renderer1.GetEntityId();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererGroupID.Length == 2);
                dispatched = true;
            }, true);

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer0.allowGPUDrivenRendering = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererGroupID.Length == 1);
                Assert.IsTrue(rendererData.rendererGroupID[0] == renderer1.GetInstanceID());
                Assert.IsTrue(rendererData.invalidRendererGroupID.Length == 1);
                Assert.IsTrue(rendererData.invalidRendererGroupID[0] == renderer0.GetInstanceID());
                dispatched = true;
            }, true);

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer1.allowGPUDrivenRendering = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.invalidRendererGroupID.Length == 2);
                dispatched = true;
            }, true);

            Assert.IsTrue(dispatched);

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

            var rendererIDs = new NativeArray<EntityId>(4, Allocator.Temp);
            rendererIDs[0] = renderer0.GetEntityId();
            rendererIDs[1] = renderer1.GetEntityId();
            rendererIDs[2] = renderer2.GetEntityId();
            rendererIDs[3] = renderer3.GetEntityId();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.localBounds.Length == 0);
                Assert.IsTrue(rendererData.localToWorldMatrix.Length == 0);
                Assert.IsTrue(rendererData.prevLocalToWorldMatrix.Length == 0);
                Assert.IsTrue(rendererData.lodGroupID.Length == 0);
                Assert.IsTrue(rendererData.rendererGroupID.Length == 4);
                dispatched = true;
            }, true);

            Assert.IsTrue(dispatched);

            gameObject1.AddComponent<OnWillRenderObjectBehaviour>();
            gameObject2.AddComponent<OnBecameInvisibleBehaviour>();
            gameObject3.AddComponent<OnBecameVisibleBehaviour>();

            dispatched = false;

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererGroupID.Length == 1);
                dispatched = true;
            }, true);

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

            var rendererIDs = new NativeArray<EntityId>(2, Allocator.Temp);
            rendererIDs[0] = renderer0.GetEntityId();
            rendererIDs[1] = renderer1.GetEntityId();

            bool dispatched = false;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            gpuDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(rendererIDs, (in GPUDrivenRendererGroupData rendererData, IList<Mesh> meshes, IList<Material> materials) =>
            {
                Assert.IsTrue(rendererData.rendererGroupID.Length == 1);
                dispatched = true;
            }, true);

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);

            gpuDrivenProcessor.Dispose();
        }

        class OnWillRenderObjectBehaviour : MonoBehaviour { void OnWillRenderObject() { } }
        class OnBecameInvisibleBehaviour : MonoBehaviour { void OnBecameInvisible() { } }
        class OnBecameVisibleBehaviour : MonoBehaviour { void OnBecameVisible() { } }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestSimpleSpeedTree()
        {
            var simpleSpeedTreeDots = Shader.Find("Unlit/SimpleSpeedTreeDots");
            var simpleSpeedTreeDotsMat = new Material(simpleSpeedTreeDots);

            // SpeedTreeWindAsset doesn't have publicly exposed constructor.
            // Without SpeedTreeWindAsset trees will not be treated as speed tree trees with wind.
            SpeedTreeWindAsset CreateDummySpeedTreeWindAsset(params object[] args)
            {
                var type = typeof(SpeedTreeWindAsset);
                var instance = type.Assembly.CreateInstance(type.FullName, false, BindingFlags.Instance | BindingFlags.NonPublic, null, args, null, null);
                return (SpeedTreeWindAsset)instance;
            }

            SpeedTreeWindAsset dummyWindAsset = CreateDummySpeedTreeWindAsset(0, null);
            Assert.NotNull(dummyWindAsset);
            dummyWindAsset.Version = 0; // st8 version.

            var tree0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var tree1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tree0.AddComponent<Tree>().windAsset = dummyWindAsset;
            tree1.AddComponent<Tree>().windAsset = dummyWindAsset;

            var renderers = new List<MeshRenderer>
            {
                tree0.GetComponent<MeshRenderer>(),
                tree1.GetComponent<MeshRenderer>()
            };

            var rendererIDs = new NativeList<EntityId>(Allocator.TempJob);

            foreach (var renderer in renderers)
            {
                renderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.material = simpleSpeedTreeDotsMat;
                rendererIDs.Add(renderer.GetInstanceID());
            }

            var instances = new NativeArray<InstanceHandle>(renderers.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < renderers.Count; ++i)
                instances[i] = InstanceHandle.Invalid;

            var gpuDrivenProcessor = new GPUDrivenProcessor();

            using (var context = new RenderersBatchersContext(new RenderersBatchersContextDesc() { instanceNumInfo = new InstanceNumInfo(speedTreeNum: 8), supportDitheringCrossFade = true }, gpuDrivenProcessor, m_Resources))
            using (var batcher = new GPUResidentBatcher(context, InstanceCullingBatcherDesc.NewDefault(), gpuDrivenProcessor))
            {
                batcher.UpdateRenderers(rendererIDs.AsArray());
                context.ScheduleQueryRendererGroupInstancesJob(rendererIDs.AsArray(), instances).Complete();

                Assert.AreEqual(2, instances.Length);
                Assert.AreEqual(instances[0].type, InstanceType.SpeedTree);
                Assert.AreEqual(instances[1].type, InstanceType.SpeedTree);

                Assert.AreEqual(context.GetAliveInstancesOfType(InstanceType.MeshRenderer), 0);
                Assert.AreEqual(context.GetAliveInstancesOfType(InstanceType.SpeedTree), 2);

                var instanceIndex0 = context.instanceData.InstanceToIndex(instances[0]);
                var instanceIndex1 = context.instanceData.InstanceToIndex(instances[1]);
                var sharedInstance0 = context.instanceData.sharedInstances[instanceIndex0];
                var sharedInstance1 = context.instanceData.sharedInstances[instanceIndex1];
                var sharedInstanceIndex0 = context.sharedInstanceData.SharedInstanceToIndex(sharedInstance0);
                var sharedInstanceIndex1 = context.sharedInstanceData.SharedInstanceToIndex(sharedInstance1);

                Assert.AreEqual(context.sharedInstanceData.rendererGroupIDs[sharedInstanceIndex0], tree0.GetComponent<Renderer>().GetEntityId());
                Assert.AreEqual(context.sharedInstanceData.rendererGroupIDs[sharedInstanceIndex1], tree1.GetComponent<Renderer>().GetEntityId());

                context.FreeInstances(instances);

                Assert.AreEqual(context.GetAliveInstancesOfType(InstanceType.SpeedTree), 0);
            }

            gpuDrivenProcessor.Dispose();

            instances.Dispose();
            rendererIDs.Dispose();
        }
    }
}
