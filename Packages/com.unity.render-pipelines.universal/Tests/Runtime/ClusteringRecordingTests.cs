using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class ClusteringRecordingTests
    {
        RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
        UniversalRenderPipelineAsset m_Asset;
        UniversalRendererData m_RendererData;
        ThrowingRendererFeature m_Feature;
        RenderTexture m_Target;
        Camera m_Camera;
        GameObject m_Light;

        [SetUp]
        public void SetUp()
        {
            m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;

            m_Feature = ScriptableObject.CreateInstance<ThrowingRendererFeature>();
            m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            m_RendererData.renderingMode = RenderingMode.ForwardPlus;
            m_RendererData.postProcessData = null;
            m_RendererData.rendererFeatures.Add(m_Feature);

            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_Asset.m_RendererDataList[0] = m_RendererData;
            QualitySettings.renderPipeline = m_Asset;

            m_Target = new RenderTexture(64, 64, 24);
            var gameObject = new GameObject("Clustering Recording Test Camera");
            m_Camera = gameObject.AddComponent<Camera>();
            gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            m_Camera.enabled = false;
            m_Camera.targetTexture = m_Target;

            m_Light = new GameObject("Clustering Recording Test Light");
            m_Light.AddComponent<Light>().type = LightType.Point;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Light);
            m_Camera.targetTexture = null;
            Object.DestroyImmediate(m_Camera.gameObject);
            m_Target.Release();
            Object.DestroyImmediate(m_Target);

            QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;
            Object.DestroyImmediate(m_Asset);
            Object.DestroyImmediate(m_RendererData);
            Object.DestroyImmediate(m_Feature);
        }

        [Description("A camera renders cleanly after a previous render graph recording was aborted after the clustering jobs were scheduled, and the aborted frame's job data does not outlive the frame.")]
        [Test]
        public void Clustering_RenderAfterAbortedRecording_DoesNotThrow()
        {
            m_Camera.Render();
            Assume.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(m_Asset), "The test URP asset is not the active pipeline.");
            LogAssert.NoUnexpectedReceived();

            m_Feature.throwOnNextRecord = true;
            LogAssert.Expect(LogType.Error, new Regex("Render Graph Execution error"));
            LogAssert.Expect(LogType.Exception, new Regex(ThrowingRendererFeature.k_Message));
            m_Camera.Render();
            var forwardLights = ((UniversalRenderer)m_Asset.scriptableRenderer).forwardLights;
            Assert.IsFalse(forwardLights.hasPendingClusteringData, "The aborted frame's clustering data outlived the frame.");

            m_Camera.Render();
            m_Camera.Render();
            LogAssert.NoUnexpectedReceived();
        }

        [HideInInspector]
        class ThrowingRendererFeature : ScriptableRendererFeature
        {
            internal const string k_Message = "Recording aborted by the test";

            internal bool throwOnNextRecord;
            ThrowingPass m_Pass;

            public override void Create()
            {
                m_Pass = new ThrowingPass(this) { renderPassEvent = RenderPassEvent.BeforeRenderingShadows };
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                renderer.EnqueuePass(m_Pass);
            }

            class ThrowingPass : ScriptableRenderPass
            {
                readonly ThrowingRendererFeature m_Feature;

                public ThrowingPass(ThrowingRendererFeature feature) => m_Feature = feature;

                public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
                {
                    if (!m_Feature.throwOnNextRecord)
                        return;

                    m_Feature.throwOnNextRecord = false;
                    throw new InvalidOperationException(k_Message);
                }
            }
        }
    }
}
