using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.TestTools;
using UnityEditor;
using Unity.Mathematics;
using NUnit.Framework.Internal;
using System.Reflection;
using System;

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

        private GPUResidentDrawer m_GPUResidentDrawer;
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private InstanceDataSystem m_InstanceDataSystem;
        private InstanceCuller m_Culler;
        private InstanceCullingBatcher m_Batcher;
        private LODGroupProcessor m_LODGroupProcessor;
        private MeshRendererProcessor m_MeshRendererProcessor;

        private bool m_OldEnableLodCrossFade;

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
            m_OldEnableLodCrossFade = QualitySettings.enableLODCrossFade;
            QualitySettings.enableLODCrossFade = true;
        }

        [TearDown]
        public void OnTearDown()
        {
            m_RenderPipe = null;
            m_MeshTestData.Dispose();
            GraphicsSettings.defaultRenderPipeline = m_OldPipelineAsset;
            QualitySettings.enableLODCrossFade = m_OldEnableLodCrossFade;
        }

        private void InitializeGPUResidentDrawer(OnCullingCompleteCallback onCompleteCallback = null, bool supportDitheringCrossFade = false, float smallMeshScreenPercentage = 0f)
        {
            m_GPUResidentDrawer = new GPUResidentDrawer(new GPUResidentDrawerSettings
            {
                mode = GPUResidentDrawerMode.InstancedDrawing,
                supportDitheringCrossFade = supportDitheringCrossFade,
                smallMeshScreenPercentage = smallMeshScreenPercentage
            },
            new InternalGPUResidentDrawerSettings
            {
                renderPipelineAsset = m_RenderPipe,
                resources = m_Resources,
                onCompleteCallback = onCompleteCallback,
                isManagedByUnitTest = true,
            });

            m_GPUDrivenProcessor = m_GPUResidentDrawer.m_GPUDrivenProcessor;
            m_InstanceDataSystem = m_GPUResidentDrawer.m_InstanceDataSystem;
            m_Culler = m_GPUResidentDrawer.m_Culler;
            m_Batcher = m_GPUResidentDrawer.m_Batcher;
            m_LODGroupProcessor = m_GPUResidentDrawer.m_WorldProcessor.lodDGroupProcessor;
            m_MeshRendererProcessor = m_GPUResidentDrawer.m_WorldProcessor.meshRendererProcessor;
        }

        private void ShutdownGPUResidentDrawer()
        {
            m_GPUDrivenProcessor = null;
            m_InstanceDataSystem = null;
            m_Culler = null;
            m_Batcher = null;
            m_LODGroupProcessor = null;
            m_MeshRendererProcessor = null;
            m_GPUResidentDrawer.Dispose();
            m_GPUResidentDrawer = null;
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceCullingBatcherAddRemove()
        {
            InitializeGPUResidentDrawer();

            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var instanceIDs = new NativeList<EntityId>(Allocator.TempJob);

            Shader dotsShader = Shader.Find("Unlit/SimpleDots");
            var dotsMaterial = new Material(dotsShader);
            foreach (var obj in objList)
            {
                obj.material = dotsMaterial;
                instanceIDs.Add(obj.GetEntityId());
            }

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());
            Assert.IsTrue(m_Batcher.GetDrawInstanceData().valid);
            Assert.IsTrue(m_Batcher.GetDrawInstanceData().drawInstances.Length == 3);

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());
            Assert.IsTrue(m_Batcher.GetDrawInstanceData().drawInstances.Length == 0);

            instanceIDs.Dispose();

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceCullingTier0()
        {
            var callbackCounter = new BoxedCounter();
            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
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

            InitializeGPUResidentDrawer(onCompleteCallback: onCompleteCallback);

            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var instanceIDs = new NativeList<EntityId>(Allocator.TempJob);

            Shader simpleDots = Shader.Find("Unlit/SimpleDots");
            Material simpleDotsMat = new Material(simpleDots);

            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                instanceIDs.Add(obj.GetEntityId());
            }

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());

            var cameraObject = new GameObject("myCamera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            SubmitCameraRenderRequest(mainCamera);
            Assert.AreEqual(1, callbackCounter.Value);

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());

            mainCamera = null;
            GameObject.DestroyImmediate(cameraObject);

            instanceIDs.Dispose();

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        [Ignore("Disabled for Instability https://jira.unity3d.com/browse/UUM-71039")]
        public void TestSceneViewHiddenRenderersCullingTier0()
        {
            var callbackCounter = new BoxedCounter();
            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
            {
                jobHandle.Complete();

                if (cc.viewType != BatchCullingViewType.Camera)
                    return;

                BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];
                callbackCounter.Value = drawCommands.visibleInstanceCount;
            };

            InitializeGPUResidentDrawer(onCompleteCallback: onCompleteCallback);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = simpleDotsMat;

            var instanceIDs = new NativeArray<EntityId>(1, Allocator.TempJob);
            instanceIDs[0] = renderer.GetEntityId();

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs);

            var cameraObject = new GameObject("SceneViewCamera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.cameraType = CameraType.SceneView;

            SceneVisibilityManager.instance.Hide(go, true);

            m_Culler.OnBeginCameraRendering(mainCamera);
            SubmitCameraRenderRequest(mainCamera);
            m_Culler.OnEndCameraRendering(mainCamera);
            Assert.AreEqual(callbackCounter.Value, 0);

            SceneVisibilityManager.instance.Show(go, true);

            m_Culler.OnBeginCameraRendering(mainCamera);
            SubmitCameraRenderRequest(mainCamera);
            m_Culler.OnEndCameraRendering(mainCamera);
            Assert.AreEqual(callbackCounter.Value, 1);

            m_MeshRendererProcessor.DestroyInstances(instanceIDs);

            instanceIDs.Dispose();

            ShutdownGPUResidentDrawer();
        }

        [Test, Ignore("Error in test shader (it is not DOTS compatible"), ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestMultipleMetadata()
        {
            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
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

            InitializeGPUResidentDrawer(onCompleteCallback: onCompleteCallback);

            var go0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var go1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var go2 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var objList = new List<MeshRenderer>();
            objList.Add(go0.GetComponent<MeshRenderer>());
            objList.Add(go1.GetComponent<MeshRenderer>());
            objList.Add(go2.GetComponent<MeshRenderer>());

            var instanceIDs = new NativeList<EntityId>(Allocator.TempJob);

            Shader simpleDots = Shader.Find("Unlit/SimpleDots");
            Material simpleDotsMat = new Material(simpleDots);

            foreach (var obj in objList)
            {
                obj.receiveGI = ReceiveGI.LightProbes;
                obj.lightProbeUsage = LightProbeUsage.BlendProbes;
                obj.material = simpleDotsMat;
                instanceIDs.Add(obj.GetEntityId());
            }
            objList[2].lightProbeUsage = LightProbeUsage.Off;

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());

            var cameraObject = new GameObject("myCamera");
            var mainCamera = cameraObject.AddComponent<Camera>();

            SubmitCameraRenderRequest(mainCamera);

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());

            mainCamera = null;
            GameObject.DestroyImmediate(cameraObject);

            instanceIDs.Dispose();

            ShutdownGPUResidentDrawer();
        }

        [Test, Ignore("Error in test shader (it is not DOTS compatible"), ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestCPULODSelection()
        {
            var callbackCounter = new BoxedCounter();
            var expectedMeshID = 1;
            var expectedDrawCommandCount = 2;
            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
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

            InitializeGPUResidentDrawer(onCompleteCallback: onCompleteCallback);

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

            var lodGroupIDs = new NativeList<EntityId>(Allocator.TempJob);
            lodGroupIDs.Add(lodGroup.GetEntityId());

            var objList = new List<MeshRenderer>();
            for (var i = 0; i < lodCount; i++)
            {
                objList.Add(gos[i].GetComponent<MeshRenderer>());
            }
            objList.Add(gos[lodCount].GetComponent<MeshRenderer>());

            var rendererIDs = new NativeList<EntityId>(Allocator.TempJob);

            Shader dotsShader = Shader.Find("Unlit/SimpleDots");
            var dotsMaterial = new Material(dotsShader);
            foreach (var obj in objList)
            {
                obj.material = dotsMaterial;
                rendererIDs.Add(obj.GetEntityId());
            }

            m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupIDs.AsArray(), transformOnly: false);
            m_MeshRendererProcessor.ProcessGameObjectChanges(rendererIDs.AsArray());

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

            var transformedLODGroupIDs = new NativeArray<EntityId>(1, Allocator.Temp);
            transformedLODGroupIDs[0] = lodGroup.GetEntityId();

            m_LODGroupProcessor.ProcessGameObjectChanges(transformedLODGroupIDs, transformOnly: true);

            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -6.5f);
            SubmitCameraRenderRequest(mainCamera);
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -40.3f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 3 - Should size cull (range 99.9 - Inf.)
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -50.4f);
            expectedMeshID = 4;
            expectedDrawCommandCount = 1;
            SubmitCameraRenderRequest(mainCamera);

            Assert.AreEqual(7, callbackCounter.Value);

            m_LODGroupProcessor.DestroyInstances(lodGroupIDs.AsArray());
            m_MeshRendererProcessor.DestroyInstances(rendererIDs.AsArray());

            mainCamera = null;
            GameObject.DestroyImmediate(cameraObject);

            lodGroupIDs.Dispose();
            rendererIDs.Dispose();

            QualitySettings.lodBias = previousLodBias;

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        [Ignore("Unstable - see https://jira.unity3d.com/browse/UUM-134437")]
        public void TestCPULODCrossfade()
        {
            if (Coverage.enabled)
                Assert.Ignore("Test disabled for code coverage runs.");
                
            var expectedMeshIDs = new List<uint>();
            var expectedFlags = new List<BatchDrawCommandFlags>();
            var expectedDrawCommandCount = new BoxedCounter();
            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
            {
                jobHandle.Complete();

                if (cc.viewType != BatchCullingViewType.Camera)
                    return;

                BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                unsafe
                {
                    Assert.AreEqual(1, drawCommands.drawRangeCount);
                    BatchDrawRange range = drawCommands.drawRanges[0];
                    Assert.AreEqual(range.drawCommandsCount, expectedDrawCommandCount.Value, " Incorrect draw Command Count");
                    for (int i = 0; i < range.drawCommandsCount; ++i)
                    {
                        BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin + i];
                        Assert.AreEqual(expectedMeshIDs[i], cmd.meshID.value, "Incorrect mesh rendered");
                        Assert.AreEqual(expectedFlags[i], cmd.flags & BatchDrawCommandFlags.LODCrossFade, "Incorrect flag for the current draw command");
                    }
                }
            };

            InitializeGPUResidentDrawer(supportDitheringCrossFade: true, onCompleteCallback: onCompleteCallback);

            var previousLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;

            var gameObject = new GameObject("LODGroup");
            gameObject.AddComponent<LODGroup>();

            GameObject[] gos = new GameObject[]
            {
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
            for (var i = 0; i < gos.Length; i++)
            {
                objList.Add(gos[i].GetComponent<MeshRenderer>());
            }

            var lodGroupIDs = new NativeList<EntityId>(Allocator.TempJob);
            lodGroupIDs.Add(lodGroup.GetEntityId());

            var rendererIDs = new NativeList<EntityId>(Allocator.TempJob);

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                rendererIDs.Add(obj.GetEntityId());
            }

            m_LODGroupProcessor.ProcessGameObjectChanges(lodGroupIDs.AsArray(), transformOnly: false);
            m_MeshRendererProcessor.ProcessGameObjectChanges(rendererIDs.AsArray());

            EntityId cubeMeshInstanceID = gos[0].GetComponent<MeshFilter>().sharedMesh.GetEntityId();
            EntityId sphereMeshInstanceID = gos[1].GetComponent<MeshFilter>().sharedMesh.GetEntityId();

            BatchMeshID cubeMeshID = m_Batcher.meshMap[cubeMeshInstanceID].meshID;
            BatchMeshID sphereMeshID = m_Batcher.meshMap[sphereMeshInstanceID].meshID;

            var cameraObject = new GameObject("myCamera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.fieldOfView = 60;

            // Cube Mesh ID : 1 (Lod 0)
            // Sphere Mesh ID : 2 (Lod 1 + non Loded)
            //Test 0 - Should render Lod0 (cube) + non loded sphere
            expectedMeshIDs.Add(cubeMeshID.value);
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedDrawCommandCount.Value = 2;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 1 - Should render Lod0 and 1 crossfaded + non loded sphere
            expectedMeshIDs.Clear();
            expectedMeshIDs.Add(cubeMeshID.value);
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedFlags.Clear();
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
            expectedDrawCommandCount.Value = 3;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -2.0f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 2 - Should render Lod1 + non loded sphere (single Draw Command as they are both spheres)
            expectedMeshIDs.Clear();
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedFlags.Clear();
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedDrawCommandCount.Value = 1;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -3.0f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 3 - Should render Lod1 crossfaded + non loded sphere
            expectedMeshIDs.Clear();
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedMeshIDs.Add(sphereMeshID.value);
            expectedFlags.Clear();
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
            expectedDrawCommandCount.Value = 2;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -4.0f);
            SubmitCameraRenderRequest(mainCamera);

            m_MeshRendererProcessor.DestroyInstances(rendererIDs.AsArray());
            m_LODGroupProcessor.DestroyInstances(lodGroupIDs.AsArray());

            mainCamera = null;
            GameObject.DestroyImmediate(cameraObject);

            lodGroupIDs.Dispose();
            rendererIDs.Dispose();

            QualitySettings.lodBias = previousLodBias;

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        [Ignore("Unstable - see https://jira.unity3d.com/browse/UUM-134437")]
        public void TestGpuDrivenSmallMeshCulling()
        {
            if (Coverage.enabled)
                Assert.Ignore("Test disabled for code coverage runs.");

            var expectedMeshIDs = new List<int>();
            var expectedFlags = new List<BatchDrawCommandFlags>();
            var expectedDrawCommandCount = new BoxedCounter();

            var lastLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;

            OnCullingCompleteCallback onCompleteCallback = (JobHandle jobHandle, in BatchCullingContext cc, in BatchCullingOutput cullingOutput) =>
            {
                jobHandle.Complete();

                if (cc.viewType != BatchCullingViewType.Camera)
                    return;

                BatchCullingOutputDrawCommands drawCommands = cullingOutput.drawCommands[0];

                unsafe
                {
                    Assert.AreEqual(1, drawCommands.drawRangeCount);
                    BatchDrawRange range = drawCommands.drawRanges[0];
                    Assert.AreEqual(range.drawCommandsCount, expectedDrawCommandCount.Value, " Incorrect draw Command Count");
                    for (int i = 0; i < range.drawCommandsCount; ++i)
                    {
                        BatchDrawCommand cmd = drawCommands.drawCommands[range.drawCommandsBegin + i];
                        Assert.AreEqual(expectedMeshIDs[i], cmd.meshID.value, "Incorrect mesh rendered");
                        Assert.AreEqual(expectedFlags[i], cmd.flags & BatchDrawCommandFlags.LODCrossFade, "Incorrect flag for the current draw command");
                    }
                }
            };

            InitializeGPUResidentDrawer(supportDitheringCrossFade: true, smallMeshScreenPercentage: 10f, onCompleteCallback: onCompleteCallback);

            var gameObject = new GameObject("Root");
            var sphere0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere0.transform.parent = gameObject.transform;
            var sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere1.AddComponent<DisallowSmallMeshCulling>();
            sphere1.transform.parent = gameObject.transform;

            var objList = new List<MeshRenderer>();
            objList.Add(sphere0.GetComponent<MeshRenderer>());
            objList.Add(sphere1.GetComponent<MeshRenderer>());

            var instanceIDs = new NativeList<EntityId>(Allocator.TempJob);

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);
            foreach (var obj in objList)
            {
                obj.material = simpleDotsMat;
                instanceIDs.Add(obj.GetEntityId());
            }

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());
            
            var cameraObject = new GameObject("myCamera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            mainCamera.fieldOfView = 60;

            //Test 0 - (1m) Should render both spheres.
            expectedMeshIDs.Add(1);
            expectedMeshIDs.Add(1);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedDrawCommandCount.Value = 1;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -1.0f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 1 - (8.5m) Should render sphere1 + crossfaded sphere0.
            expectedMeshIDs.Clear();
            expectedMeshIDs.Add(1);
            expectedMeshIDs.Add(1);
            expectedFlags.Clear();
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFade);
            expectedDrawCommandCount.Value = 2;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -8.5f);
            SubmitCameraRenderRequest(mainCamera);

            //Test 2 - (10m) Should only render sphere1.
            expectedMeshIDs.Clear();
            expectedMeshIDs.Add(1);
            expectedFlags.Clear();
            expectedFlags.Add(BatchDrawCommandFlags.LODCrossFadeValuePacked);
            expectedDrawCommandCount.Value = 1;
            cameraObject.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
            SubmitCameraRenderRequest(mainCamera);

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());

            QualitySettings.lodBias = lastLodBias;

            mainCamera = null;
            GameObject.DestroyImmediate(cameraObject);

            instanceIDs.Dispose();
            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestInstanceData()
        {
            InitializeGPUResidentDrawer();

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

            var meshRenderers = new MeshRenderer[gameObjects.Length];

            for (int i = 0; i < meshRenderers.Length;i ++)
                meshRenderers[i] = gameObjects[i].GetComponent<MeshRenderer>();

            foreach (MeshRenderer renderer in meshRenderers)
                renderer.sharedMaterial = simpleDotsMat;

            var instanceIDs = new NativeList<EntityId>(8, Allocator.TempJob);

            instanceIDs.Add(meshRenderers[0].GetEntityId());
            instanceIDs.Add(meshRenderers[1].GetEntityId());
            instanceIDs.Add(meshRenderers[2].GetEntityId());
            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());
            instanceIDs.Clear();


            instanceIDs.Add(meshRenderers[3].GetEntityId());
            instanceIDs.Add(meshRenderers[4].GetEntityId());
            instanceIDs.Add(meshRenderers[5].GetEntityId());
            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());
            instanceIDs.Clear();


            instanceIDs.Add(meshRenderers[6].GetEntityId());
            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());
            Assert.IsTrue(m_InstanceDataSystem.AreAllAllocatedInstancesValid());
            instanceIDs.Clear();


            foreach (var go in gameObjects)
                GameObject.DestroyImmediate(go);

            GameObject.DestroyImmediate(simpleDotsMat);

            instanceIDs.Dispose();
            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestStaticBatching()
        {
            InitializeGPUResidentDrawer();
            ref DefaultGPUComponents defaultGPUComponents = ref m_InstanceDataSystem.defaultGPUComponents;

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var staticBatchingRoot = new GameObject();
            staticBatchingRoot.transform.position = new Vector3(10, 0, 0);

            var gameObjects = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cube),
                GameObject.CreatePrimitive(PrimitiveType.Cube)
            };

            foreach (GameObject go in gameObjects)
            {
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                renderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.sharedMaterial = simpleDotsMat;
            }

            gameObjects[0].transform.position = new Vector3(2, 0, 0);
            gameObjects[1].transform.position = new Vector3(-2, 0, 0);

            StaticBatchingUtility.Combine(gameObjects, staticBatchingRoot);

            var instanceIDs = new NativeArray<EntityId>(2, Allocator.TempJob);
            instanceIDs[0] = gameObjects[0].GetComponent<MeshRenderer>().GetEntityId();
            instanceIDs[1] = gameObjects[1].GetComponent<MeshRenderer>().GetEntityId();

            var localToWorldMatrices = new NativeArray<float4x4>(2, Allocator.Temp);
            localToWorldMatrices[0] = gameObjects[0].transform.localToWorldMatrix;
            localToWorldMatrices[1] = gameObjects[1].transform.localToWorldMatrix;

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs);

            var instances = new NativeArray<InstanceHandle>(instanceIDs.Length, Allocator.TempJob);
            m_InstanceDataSystem.QueryRendererInstances(instanceIDs, instances);

            foreach (InstanceHandle instance in instances)
            {
                int index = m_InstanceDataSystem.renderWorld.HandleToIndex(instance);
                InternalMeshRendererSettings settings = m_InstanceDataSystem.renderWorld.rendererSettings[index];
                Assert.IsTrue(settings.IsPartOfStaticBatch);
            }

            instanceIDs.Dispose();
            instances.Dispose();

            foreach (var go in gameObjects)
                GameObject.DestroyImmediate(go);

            GameObject.DestroyImmediate(staticBatchingRoot);
            GameObject.DestroyImmediate(simpleDotsMat);

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestDisallowGPUDrivenRendering()
        {
            InitializeGPUResidentDrawer();

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var gameObject0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer0 = gameObject0.GetComponent<MeshRenderer>();
            var renderer1 = gameObject1.GetComponent<MeshRenderer>();
            renderer0.sharedMaterial = simpleDotsMat;
            renderer1.sharedMaterial = simpleDotsMat;

            var instanceIDs = new NativeArray<EntityId>(2, Allocator.Temp);
            instanceIDs[0] = renderer0.GetEntityId();
            instanceIDs[1] = renderer1.GetEntityId();

            bool dispatched = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.renderer.Length == 2);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer0.allowGPUDrivenRendering = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.renderer.Length == 1);
                Assert.IsTrue(rendererData.renderer[0] == renderer1.GetEntityId());
                Assert.IsTrue(rendererData.invalidRenderer.Length == 1);
                Assert.IsTrue(rendererData.invalidRenderer[0] == renderer0.GetEntityId());
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            dispatched = false;
            renderer1.allowGPUDrivenRendering = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.invalidRenderer.Length == 2);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestUnsupportedCallbacks()
        {
            InitializeGPUResidentDrawer();

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

            var instanceIDs = new NativeArray<EntityId>(4, Allocator.Temp);
            instanceIDs[0] = renderer0.GetEntityId();
            instanceIDs[1] = renderer1.GetEntityId();
            instanceIDs[2] = renderer2.GetEntityId();
            instanceIDs[3] = renderer3.GetEntityId();

            bool dispatched = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.renderer.Length == 4);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            gameObject1.AddComponent<OnWillRenderObjectBehaviour>();
            gameObject2.AddComponent<OnBecameInvisibleBehaviour>();
            gameObject3.AddComponent<OnBecameVisibleBehaviour>();

            dispatched = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.renderer.Length == 1);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);
            Object.DestroyImmediate(gameObject2);
            Object.DestroyImmediate(gameObject3);

            ShutdownGPUResidentDrawer();
        }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestForceRenderingOff()
        {
            InitializeGPUResidentDrawer();

            var simpleDots = Shader.Find("Unlit/SimpleDots");
            var simpleDotsMat = new Material(simpleDots);

            var gameObject0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var renderer0 = gameObject0.GetComponent<MeshRenderer>();
            var renderer1 = gameObject1.GetComponent<MeshRenderer>();

            renderer0.sharedMaterial = simpleDotsMat;
            renderer1.sharedMaterial = simpleDotsMat;

            renderer0.forceRenderingOff = true;

            var instanceIDs = new NativeArray<EntityId>(2, Allocator.Temp);
            instanceIDs[0] = renderer0.GetEntityId();
            instanceIDs[1] = renderer1.GetEntityId();

            bool dispatched = false;

            m_GPUDrivenProcessor.EnableGPUDrivenRenderingAndDispatchRendererData(instanceIDs, (in GPUDrivenMeshRendererData rendererData) =>
            {
                Assert.IsTrue(rendererData.renderer.Length == 1);
                dispatched = true;
            });

            Assert.IsTrue(dispatched);

            Object.DestroyImmediate(simpleDotsMat);
            Object.DestroyImmediate(gameObject0);
            Object.DestroyImmediate(gameObject1);

            ShutdownGPUResidentDrawer();
        }

        class OnWillRenderObjectBehaviour : MonoBehaviour { void OnWillRenderObject() { } }
        class OnBecameInvisibleBehaviour : MonoBehaviour { void OnBecameInvisible() { } }
        class OnBecameVisibleBehaviour : MonoBehaviour { void OnBecameVisible() { } }

        [Test, ConditionalIgnore("IgnoreGfxAPI", "Graphics API Not Supported.")]
        public void TestSimpleSpeedTree()
        {
            InitializeGPUResidentDrawer(supportDitheringCrossFade: true);
            ref DefaultGPUComponents defaultGPUComponents = ref m_InstanceDataSystem.defaultGPUComponents;

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

            var renderers = new MeshRenderer[]
            {
                tree0.GetComponent<MeshRenderer>(),
                tree1.GetComponent<MeshRenderer>()
            };

            var instanceIDs = new NativeList<EntityId>(Allocator.TempJob);

            foreach (var renderer in renderers)
            {
                renderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.material = simpleSpeedTreeDotsMat;
                instanceIDs.Add(renderer.GetEntityId());
            }

            GPUComponentSet componentSet = defaultGPUComponents.defaultSpeedTreeComponentSet;
            componentSet.AddSet(defaultGPUComponents.lightProbesComponentSet);
            GPUArchetypeHandle expectedArchetype = m_InstanceDataSystem.archetypeManager.GetRef().GetOrCreateArchetype(componentSet);

            m_MeshRendererProcessor.ProcessGameObjectChanges(instanceIDs.AsArray());

            var instances = new NativeArray<InstanceHandle>(renderers.Length, Allocator.TempJob);
            instances.FillArray(InstanceHandle.Invalid);
            m_InstanceDataSystem.QueryRendererInstances(instanceIDs.AsArray(), instances);

            var instanceIndex0 = m_InstanceDataSystem.renderWorld.HandleToIndex(instances[0]);
            InternalMeshRendererSettings settings0 = m_InstanceDataSystem.renderWorld.rendererSettings[instanceIndex0];
            var instanceIndex1 = m_InstanceDataSystem.renderWorld.HandleToIndex(instances[1]);
            InternalMeshRendererSettings settings1 = m_InstanceDataSystem.renderWorld.rendererSettings[instanceIndex1];

            Assert.AreEqual(2, m_InstanceDataSystem.totalTreeCount);

            Assert.AreEqual(tree0.GetComponent<Renderer>().GetEntityId(), m_InstanceDataSystem.renderWorld.instanceIDs[instanceIndex0]);
            Assert.AreEqual(LightProbeUsage.BlendProbes, settings0.LightProbeUsage);
            Assert.IsTrue(settings0.HasTree);

            Assert.AreEqual(tree1.GetComponent<Renderer>().GetEntityId(), m_InstanceDataSystem.renderWorld.instanceIDs[instanceIndex1]);
            Assert.AreEqual(LightProbeUsage.BlendProbes, settings1.LightProbeUsage);
            Assert.IsTrue(settings1.HasTree);

            Assert.AreEqual(2, instances.Length);
            Assert.AreEqual(0, m_InstanceDataSystem.GetGPUArchetypeAliveInstancesCount(defaultGPUComponents.defaultGOArchetype));
            Assert.AreEqual(2, m_InstanceDataSystem.GetGPUArchetypeAliveInstancesCount(expectedArchetype));

            m_MeshRendererProcessor.DestroyInstances(instanceIDs.AsArray());

            Assert.AreEqual(0, m_InstanceDataSystem.GetGPUArchetypeAliveInstancesCount(defaultGPUComponents.defaultGOArchetype));
            Assert.AreEqual(0, m_InstanceDataSystem.GetGPUArchetypeAliveInstancesCount(expectedArchetype));

            GameObject.DestroyImmediate(tree0);
            GameObject.DestroyImmediate(tree1);

            instances.Dispose();
            instanceIDs.Dispose();
            ShutdownGPUResidentDrawer();
            dummyWindAsset = null;
        }
    }
}
