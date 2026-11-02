using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture]
    class SurfaceCacheGIGraphStabilityTests
    {
        const string k_WorldUpdatePassName = "Surface Cache World Update";
        const int k_Repaints = 30;

        static int motionVectorFrameIndex =>
            Unity.RenderPipelines.Core.Runtime.Shared.RendererUpdateManagerBridge.GetMotionVectorFrameIndex();

        RenderPipelineAsset m_PreviousRenderPipeline;
        bool m_PreviousStaticBatching;
        DefaultMeshBufferTarget m_PreviousMeshBufferTarget;
        BuildTarget m_BuildTarget;
        bool m_SettingsCaptured;

        UniversalRenderPipelineAsset m_Asset;
        UniversalRendererData m_RendererData;
        SurfaceCacheGIRendererFeature m_Feature;
        RecordedPassObserverFeature m_Observer;
        GameObject m_CameraGameObject;
        Camera m_Camera;
        RenderTexture m_Target;
        SceneView m_SceneView;
        bool m_OpenedSceneView;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // The asset created below only resolves its resources if URP is the project's pipeline.
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("Surface Cache GI is a URP renderer feature; URP is not the current pipeline.");

            if (!SurfaceCacheGIRendererFeature.HasSupportedRayTracingBackend())
                Assert.Ignore("Surface Cache GI requires a hardware or compute ray tracing backend.");

            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            m_PreviousStaticBatching = PlayerSettings.GetStaticBatchingForPlatform(m_BuildTarget);
            m_PreviousMeshBufferTarget = EditorGraphicsSettings.defaultMeshBufferTarget;
            m_PreviousRenderPipeline = QualitySettings.renderPipeline;
            m_SettingsCaptured = true;

            // The feature refuses to run with Static Batching on or without raw mesh buffers, which would leave
            // no Surface Cache passes.
            PlayerSettings.SetStaticBatchingForPlatform(m_BuildTarget, false);
            EditorGraphicsSettings.defaultMeshBufferTarget = DefaultMeshBufferTarget.Raw;

            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_Asset.m_RendererDataList[0] = m_RendererData;

            // URP has to be the active pipeline before the feature is created, because the feature resolves
            // its resources through the active pipeline's settings.
            QualitySettings.renderPipeline = m_Asset;

            m_Feature = ScriptableObject.CreateInstance<SurfaceCacheGIRendererFeature>();
            m_Observer = ScriptableObject.CreateInstance<RecordedPassObserverFeature>();
            m_RendererData.rendererFeatures.Clear();
            m_RendererData.rendererFeatures.Add(m_Feature);
            m_RendererData.rendererFeatures.Add(m_Observer);
            m_RendererData.isInvalidated = true;

            m_Target = new RenderTexture(256, 256, 24) { hideFlags = HideFlags.HideAndDontSave };
            m_CameraGameObject = new GameObject("SurfaceCacheGIGraphStabilityCamera") { hideFlags = HideFlags.HideAndDontSave };
            m_Camera = m_CameraGameObject.AddComponent<Camera>();
            m_CameraGameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            // Preview cameras driven through the render request API are eligible for Surface Cache GI.
            m_Camera.cameraType = CameraType.Preview;
            m_Camera.enabled = false;

            // Scene View repaints are what advance the motion vector frame index without advancing Time.frameCount.
            m_OpenedSceneView = SceneView.sceneViews.Count == 0;
            m_SceneView = EditorWindow.GetWindow<SceneView>();
            m_SceneView.Show();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // NUnit runs this even when OneTimeSetUp ignored the fixture before capturing anything.
            if (m_SettingsCaptured)
            {
                QualitySettings.renderPipeline = m_PreviousRenderPipeline;
                PlayerSettings.SetStaticBatchingForPlatform(m_BuildTarget, m_PreviousStaticBatching);
                EditorGraphicsSettings.defaultMeshBufferTarget = m_PreviousMeshBufferTarget;
            }

            if (m_OpenedSceneView && m_SceneView != null)
                m_SceneView.Close();
            if (m_CameraGameObject != null)
                Object.DestroyImmediate(m_CameraGameObject);
            if (m_Target != null)
            {
                m_Target.Release();
                Object.DestroyImmediate(m_Target);
            }
            if (m_Observer != null)
                Object.DestroyImmediate(m_Observer);
            if (m_Feature != null)
                Object.DestroyImmediate(m_Feature);
            if (m_RendererData != null)
                Object.DestroyImmediate(m_RendererData);
            if (m_Asset != null)
                Object.DestroyImmediate(m_Asset);
        }

        [UnityTest]
        public IEnumerator EachEditorRepaintRecordsTheWorldUpdateExactlyOnce()
        {
            var request = new RenderPipeline.StandardRequest { destination = m_Target };
            if (!RenderPipeline.SupportsRenderRequest(m_Camera, request))
                Assert.Ignore("The active pipeline does not support standard render requests.");

            // The frame current at this point may already have had its world update before the capture began.
            var firstFrame = (Time.frameCount, motionVectorFrameIndex);
            m_Observer.Renders.Clear();
            for (int i = 0; i < k_Repaints; ++i)
            {
                m_SceneView.Repaint();
                yield return null;

                // Two renders between repaints share a frame, so only the first may update the world.
                RenderPipeline.SubmitRenderRequest(m_Camera, request);
                RenderPipeline.SubmitRenderRequest(m_Camera, request);
            }

            var frames = m_Observer.Renders
                .GroupBy(render => (render.FrameCount, render.MotionVectorFrameIndex))
                .Where(frame => frame.Key != firstFrame)
                .ToList();
            Assert.That(frames, Is.Not.Empty, "No eligible camera rendered through the observer.");

            foreach (var frame in frames)
            {
                int worldUpdates = frame.Count(render => render.RecordedWorldUpdate);
                Assert.That(worldUpdates, Is.EqualTo(1),
                    $"Frame {frame.Key} recorded the world update {worldUpdates} times over {frame.Count()} renders.");
                Assert.That(frame.First().RecordedWorldUpdate, Is.True,
                    $"The first render of frame {frame.Key} did not record the world update.");
            }

            bool repaintedWithinOneFrameCount = frames
                .Zip(frames.Skip(1), (previous, next) => previous.Key.FrameCount == next.Key.FrameCount)
                .Any(sameFrameCount => sameFrameCount);
            Assert.That(repaintedWithinOneFrameCount, Is.True,
                "No two repaints shared a Time.frameCount, so a Time.frameCount gate would pass this test too.");
        }

        struct ObservedRender
        {
            public int FrameCount;
            public int MotionVectorFrameIndex;
            public bool RecordedWorldUpdate;
        }

        // Records, for each eligible camera render, whether the world update was recorded before it.
        // It deliberately records no pass of its own so that it does not show up in its own capture.
        class RecordedPassObserverFeature : ScriptableRendererFeature
        {
            RecordedPassObserverPass m_Pass;

            internal List<ObservedRender> Renders { get; } = new List<ObservedRender>();

            public override void Create()
            {
                // The world update records at AfterRenderingPrePasses and the per-camera Surface Cache work at +1.
                m_Pass = new RecordedPassObserverPass(Renders)
                {
                    renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 2
                };
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                if (SurfaceCacheGIRendererFeature.IsEligibleCamera(renderingData.cameraData.camera))
                    renderer.EnqueuePass(m_Pass);
            }
        }

        class RecordedPassObserverPass : ScriptableRenderPass
        {
            readonly List<ObservedRender> m_Renders;

            internal RecordedPassObserverPass(List<ObservedRender> renders)
            {
                m_Renders = renders;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                m_Renders.Add(new ObservedRender
                {
                    FrameCount = Time.frameCount,
                    MotionVectorFrameIndex = motionVectorFrameIndex,
                    RecordedWorldUpdate = renderGraph.m_RenderPasses.Any(pass => pass.name == k_WorldUpdatePassName),
                });
            }
        }
    }
}
