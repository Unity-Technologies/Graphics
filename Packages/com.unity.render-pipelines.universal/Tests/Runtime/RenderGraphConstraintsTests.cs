using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class RenderGraphConstraintsTests
    {
        RenderPipelineAsset m_PreviousQualityRenderPipelineAsset;
        Type m_PreviousSessionType;

        UniversalRenderPipelineAsset m_UniversalRenderPipelineAsset;
        UniversalRendererData m_UniversalRendererData;
        OnTileValidationConfiguration m_DefaultOnTileValidationConfiguration;
        EntityId m_CameraEntityId;

        static UniversalAdditionalCameraData s_AdditionalCameraData;
        static PostProcessData s_PostProcessData;
        static HashSet<RenderingMode> s_RenderingModes;

        readonly HashSet<string> m_ResourceTestNames = new()
        {
            "_CameraTargetAttachmentA",
            "_CameraTargetAttachmentB",
            "_CameraTargetAttachment",
            "_CameraDepthAttachment",
            "_CameraDepthTexture",
            "_CameraOpaqueTexture",
            "_CameraNormalsTexture",
            "_MotionVectorTexture",
            "_MotionVectorDepthTexture",
            "_CameraRenderingLayersTexture",
            "_GBuffer0",
            "_GBuffer1",
            "_GBuffer2",
            "_GBuffer3",
            "_GBuffer4",
            "_GBuffer5",
            "_GBuffer6",
            "_DBufferTexture0",
            "_DBufferTexture1",
            "_DBufferTexture2",
            "_ScreenSpaceShadowmapTexture",
            "_CameraColorAfterPostProcessing",
            "_CameraColorFullScreenPass",
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore($"This test will be only executed when URP is the current pipeline");

#if UNITY_EDITOR
            if (UnityEditor.EditorWindow.HasOpenInstances<UnityEditor.Rendering.RenderGraphViewer>())
                UnityEditor.EditorWindow.GetWindow<UnityEditor.Rendering.RenderGraphViewer>().Close();
#endif

            m_PreviousSessionType = RenderGraphDebugSession.currentDebugSession?.GetType();
            m_PreviousQualityRenderPipelineAsset = QualitySettings.renderPipeline;

            RenderGraphDebugSession.Create<TestRenderGraphDebugSession>();

            // Create new URP asset
            m_UniversalRenderPipelineAsset = Create();
            QualitySettings.renderPipeline = m_UniversalRenderPipelineAsset;
            Assume.That(m_UniversalRenderPipelineAsset.rendererDataList != null && m_UniversalRenderPipelineAsset.rendererDataList.Length > 0);

            // Get URP Renderer Data
            m_UniversalRendererData = m_UniversalRenderPipelineAsset.rendererDataList[0] as UniversalRendererData;
            Assume.That(m_UniversalRendererData != null);

            m_DefaultOnTileValidationConfiguration = new OnTileValidationConfiguration();

            var gameObject = new GameObject();
            var camera = gameObject.AddComponent<Camera>();
            m_CameraEntityId = camera.GetEntityId();
            s_AdditionalCameraData = gameObject.AddComponent<UniversalAdditionalCameraData>();
            s_AdditionalCameraData.renderPostProcessing = true;
            s_AdditionalCameraData.SetRenderer(0);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_DefaultOnTileValidationConfiguration.ApplyToAssetAndRenderer(m_UniversalRenderPipelineAsset, m_UniversalRendererData);

            yield return null;

            if (!RenderGraphDebugSession.GetRegisteredGraphs().Contains("URPRenderGraph"))
                Assert.Ignore("Ignore this test as we couldn't setup debug session for URPRenderGraph.");
        }

        UniversalRenderPipelineAsset Create()
        {
            // Create Universal RP Asset
            var instance = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();

            // Initialize default renderer data
            instance.m_RendererDataList[0] = ScriptableObject.CreateInstance<UniversalRendererData>();

            // Setup PP from existing urp asset
            s_RenderingModes = new HashSet<RenderingMode>();

            ProcessRenderPipelineAsset(GraphicsSettings.defaultRenderPipeline, instance);
            for (int i = 0; i < QualitySettings.count; i++)
                ProcessRenderPipelineAsset(QualitySettings.GetRenderPipelineAssetAt(i), instance);

            return instance;
        }

        static void ProcessRenderPipelineAsset(RenderPipelineAsset rpAsset, UniversalRenderPipelineAsset instance)
        {
            if (rpAsset == null || rpAsset is not UniversalRenderPipelineAsset urpAsset)
                return;

            foreach (var existingRendererData in urpAsset.rendererDataList)
            {
                if (existingRendererData == null || existingRendererData is not UniversalRendererData urpRendererData)
                    continue;

                if (urpRendererData.postProcessData != null && s_PostProcessData == null)
                {
                    s_PostProcessData = urpRendererData.postProcessData;
                    ((UniversalRendererData)instance.m_RendererDataList[0]).postProcessData =
                        urpRendererData.postProcessData;
                }

                s_RenderingModes.Add(urpRendererData.renderingMode);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_PreviousSessionType != null)
                RenderGraphDebugSession.Create(m_PreviousSessionType);
            else
                RenderGraphDebugSession.EndSession();

            QualitySettings.renderPipeline = m_PreviousQualityRenderPipelineAsset;

            Object.DestroyImmediate(m_UniversalRenderPipelineAsset);
            Object.DestroyImmediate(m_UniversalRendererData);
            Object.DestroyImmediate(s_AdditionalCameraData.gameObject);
        }

        public class OnTileValidationConfiguration
        {
            public bool supportsCameraOpaqueTexture { get; set; } = false;
            public bool supportsCameraDepthTexture { get; set; } = false;
            public bool supportsHDR { get; set; } = false;
            public int msaaSampleCount { get; set; } = 1;
            public RenderingMode renderingMode { get; set; } = RenderingMode.Forward;
            public bool occlusionCulling { get; set; } = false;
            public float renderScale { get; set; } = 1.0f;
            public bool postProcessingEnabled { get; set; } = false;
            public bool onTileValidation { get; set; } = false;

            public List<ScriptableRendererFeature> rendererFeatures { get; set; } = new();

            bool TryApplyToAssetAndRenderer(UniversalRenderPipelineAsset asset, UniversalRendererData rendererData)
            {
                // Apply asset settings
                asset.supportsCameraDepthTexture = supportsCameraDepthTexture;
                asset.supportsCameraOpaqueTexture = supportsCameraOpaqueTexture;
                asset.supportsHDR = supportsHDR;
                asset.msaaSampleCount = msaaSampleCount;
                asset.renderScale = renderScale;
                if (occlusionCulling)
                {
                    if (!Application.isEditor)
                        return false;
#if UNITY_EDITOR
                    if (UnityEditor.Rendering.EditorGraphicsSettings.batchRendererGroupShaderStrippingMode !=
                        UnityEditor.Rendering.BatchRendererGroupStrippingMode.KeepAll)
                        return false;
#endif
                    asset.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
                    asset.gpuResidentDrawerEnableOcclusionCullingInCameras = occlusionCulling;
                    renderingMode = RenderingMode.ForwardPlus;
                }

                // Apply renderer settings
                rendererData.onTileValidation = onTileValidation;

                if (renderingMode != RenderingMode.Forward && !s_RenderingModes.Contains(renderingMode))
                    return false;

                rendererData.renderingMode = renderingMode;

                if (postProcessingEnabled && s_PostProcessData == null)
                    return false;

                rendererData.postProcessData = s_PostProcessData != null ? s_PostProcessData : null;

                // Clear
                rendererData.rendererFeatures.Clear();
                foreach (var feature in rendererFeatures)
                    rendererData.rendererFeatures.Add(feature);
                return true;
            }

            public void ApplyToAssetAndRenderer(UniversalRenderPipelineAsset asset, UniversalRendererData rendererData)
            {
                if (!TryApplyToAssetAndRenderer(asset, rendererData))
                    Assert.Ignore("We can't run this test for On Tile Validation because some resources could be missing.");
            }
        }

        static TestCaseData[] s_OnTileValidationCases =
        {
            new TestCaseData(new OnTileValidationConfiguration { supportsCameraOpaqueTexture = true })
                .SetName("Camera Opaque Texture on Universal Render Pipeline Asset.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { supportsCameraDepthTexture = true })
                .SetName("Camera Depth Texture on Universal Render Pipeline Asset.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { supportsHDR = true })
                .SetName("Support HDR on Universal Render Pipeline Asset.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { msaaSampleCount = 4 })
                .SetName("MSAA Samples > 1 on Universal Render Pipeline Asset.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { occlusionCulling = true })
                .SetName("GPU Occlusion Culling on Universal Render Pipeline Asset.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { renderingMode = RenderingMode.Deferred })
                .SetName("Deferred Rendering Mode on Universal Renderer.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { renderingMode = RenderingMode.DeferredPlus })
                .SetName("Deferred Plus Rendering Mode on Universal Renderer.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { renderScale = 0.65f })
                .SetName("Render Scale on Universal Renderer.").Returns(null),
            new TestCaseData(new OnTileValidationConfiguration { postProcessingEnabled = true })
                .SetName("Post Processing on Universal Renderer.").Returns(null),
        };

        [UnityTest]
        [TestCaseSource(nameof(s_OnTileValidationCases))]
        public IEnumerator URPSettingsShouldNotCauseNonMemorylessTargets(OnTileValidationConfiguration config)
        {
            // Arrange
            LogAssert.ignoreFailingMessages = true;
            config.ApplyToAssetAndRenderer(m_UniversalRenderPipelineAsset, m_UniversalRendererData);

            // Act
            m_UniversalRendererData.onTileValidation = true;

            yield return null;

            Assert.That(OnlyBackbufferOrMemoryless(outputNonMemoryless: true), Is.True, "Non-memoryless intermediate targets were produced.");
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator AnyScriptableRendererFeatureProduceAnException()
        {
            // Arrange
            var config = new OnTileValidationConfiguration();
            config.rendererFeatures.Add(ScriptableObject.CreateInstance<TestScriptableRendererFeature>());
            config.ApplyToAssetAndRenderer(m_UniversalRenderPipelineAsset, m_UniversalRendererData);

            yield return null;

            // Act
            LogAssert.Expect(LogType.Error, new Regex("Render Graph Execution error"));
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));

            m_UniversalRendererData.onTileValidation = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraStackingProduceAnException()
        {
            // Arrange
            var gameObject = new GameObject();
            var camera = gameObject.AddComponent<Camera>();
            var additionalCameraData = gameObject.AddComponent<UniversalAdditionalCameraData>();
            additionalCameraData.renderType = CameraRenderType.Overlay;

            Assume.That(s_AdditionalCameraData.TryAddCameraToStack(camera), "Couldn't add camera to stack.");
            yield return null;

            Assume.That(OnlyBackbufferOrMemoryless(), Is.False, "Intermediate Targets expected without On Tile Validation.");

            yield return null;

            // Act
            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));

            m_UniversalRendererData.onTileValidation = true;
            yield return null;

            m_UniversalRendererData.onTileValidation = false;
            ((UniversalRenderer)additionalCameraData.scriptableRenderer).onTileValidation = true;

            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));

            yield return null;

            Object.DestroyImmediate(gameObject);
            s_AdditionalCameraData.UpdateCameraStack();
        }


        bool OnlyBackbufferOrMemoryless(bool outputNonMemoryless = false)
        {
            var onlyMemoryless = true;
            var debugData = RenderGraphDebugSession.GetDebugData("URPRenderGraph", m_CameraEntityId);
            var textureList = debugData.resourceLists[(int)RenderGraphResourceType.Texture];
            for (int i = 0; i < textureList.Count; i++)
            {
                var resource = textureList[i];
                if (resource is { releasePassIndex: -1, creationPassIndex: -1 })
                    continue;

                if (!IsCameraTargetAttachment(resource.name) || resource.memoryless)
                    continue;

                if (!outputNonMemoryless)
                    return false;

                Debug.Log($"{resource.name} isn't memoryless.");
                onlyMemoryless = false;
            }

            return onlyMemoryless;
        }

        bool IsCameraTargetAttachment(string name)
        {
            return m_ResourceTestNames.Contains(name);
        }

        class TestRenderGraphDebugSession : RenderGraphDebugSession
        {
            public override bool isActive => true;

            public TestRenderGraphDebugSession()
            {
                RegisterAllLocallyKnownGraphsAndExecutions();
            }
        }

        class TestScriptableRendererFeature : ScriptableRendererFeature
        {
            TestScriptableRendererPass m_TestScriptableRendererPass;

            public override void Create()
            {
                m_TestScriptableRendererPass = new TestScriptableRendererPass();
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                renderer.EnqueuePass(m_TestScriptableRendererPass);
            }
        }

        public class TestPass { }

        class TestScriptableRendererPass : ScriptableRenderPass
        {
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                using var builder = renderGraph.AddRasterRenderPass<TestPass>("Test Scriptable Renderer Pass", out _);
                builder.SetRenderFunc(static (TestPass _, RasterGraphContext _) => { });
            }
        }
    }
}
