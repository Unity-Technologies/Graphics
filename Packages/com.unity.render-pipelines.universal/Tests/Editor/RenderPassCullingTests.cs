using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Profiling;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Tests
{
    class CullingTestRenderPass : ScriptableRenderPass
    {
        /// <inheritdoc/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var cullContextData = frameData.Get<CullContextData>();

            Assert.IsTrue(cameraData != null);
            Assert.IsTrue(cullContextData != null);

            cameraData.camera.TryGetCullingParameters(false, out var cullingParameters);

            var cullingResults = cullContextData.Cull(ref cullingParameters);

            var lightsInScene = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Assert.IsTrue(cullingResults.visibleLights.Length == lightsInScene.Length);
        }

#if URP_COMPATIBILITY_MODE
        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // This path does not implement the CullContextData.
        }
#endif
    }

    class RenderGraphTestsCulling
    {
        static Recorder gcAllocRecorder = Recorder.Get("GC.Alloc");

        const int kLightCount = 3;

        CullingTestRenderPass m_TestRenderPass;
        ScriptableRenderContext? m_RenderContext;
        Camera m_Camera;
        List<GameObject> m_GameObjects = new();

        [SetUp]
        public void Setup()
        {
            m_TestRenderPass = new CullingTestRenderPass();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            m_RenderContext = null;

            // We need a real ScriptableRenderContext and a camera to execute the render graph
            // add the default camera
            var cameraGO = new GameObject("Culling_GameObject")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraGO.tag = "MainCamera";
            m_Camera = cameraGO.AddComponent<Camera>();

            m_GameObjects.Add(cameraGO);

            for (int i = 0; i < kLightCount; ++i)
            {
                var lightGO = new GameObject("Light_GameObject" + i);
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Point;
                m_GameObjects.Add(lightGO);
            }
        }

        [TearDown]
        public void TearDown()
        {
            m_TestRenderPass = null;
            m_RenderContext = null;
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

            foreach (var obj in m_GameObjects)
            {
                GameObject.DestroyImmediate(obj);
            }
        }

        [Test]
        public void RenderPassCullingAPIWorks()
        {
            if (DisableTestWhenExecutedOnNonURPProject())
                return;

            SubmitCameraRenderRequest(m_Camera);
        }

        [Test]
        public void RenderPassCullingAPIDoesNotAlloc()
        {
            if (DisableTestWhenExecutedOnNonURPProject())
                return;

            SubmitCameraRenderRequest(m_Camera);

            Assert.IsTrue(m_RenderContext != null);

            var contextContainer = new ContextContainer();
            var cullData = contextContainer.Create<CullContextData>();

            ValidateNoGCAllocs(() =>
            {
                cullData.SetRenderContext(m_RenderContext.Value);
            });
        }

        bool DisableTestWhenExecutedOnNonURPProject()
        {
            return !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset);
        }

        void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            m_RenderContext = context;

            // Use the EnqueuePass method to inject a custom render pass
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_TestRenderPass);
        }

        void SubmitCameraRenderRequest(Camera camera)
        {
            var request = new RenderPipeline.StandardRequest();
            var desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, RenderTextureFormat.Default, 32);
            request.destination = RenderTexture.GetTemporary(desc);

            // Check if the active render pipeline supports the render request
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            RenderTexture.ReleaseTemporary(request.destination);
        }

        void ValidateNoGCAllocs(Action action)
        {
            // Warmup - this will catch static c'tors etc.
            CountGCAllocs(action);

            // Actual test.
            var count = CountGCAllocs(action);
            if (count != 0)
                throw new AssertionException($"Expected 0 GC allocations but there were {count}");
        }

        int CountGCAllocs(Action action)
        {
            gcAllocRecorder.FilterToCurrentThread();
            gcAllocRecorder.enabled = true;

            action();

            gcAllocRecorder.enabled = false;
            return gcAllocRecorder.sampleBlockCount;
        }
    }
}
