using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    // URP skips the prepass when the camera can't see any prepass layer. The textures it would have produced must still be
    // published as globals, so that shaders sample the same textures that passes declare through the resource data.
    class SkippedPrepassTests
    {
        class PrepassTexturesFeature : ScriptableRendererFeature
        {
            internal bool requireRenderingLayers;
            internal CaptureGlobalsPass capturePass;

            public override void Create()
            {
                capturePass = new CaptureGlobalsPass();
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                renderer.EnqueuePass(capturePass);
            }

            internal override bool RequireRenderingLayers(bool isDeferred, bool needsGBufferAccurateNormals, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
            {
                atEvent = RenderingLayerUtils.Event.DepthNormalPrePass;
                maskSize = RenderingLayerUtils.MaskSize.Bits8;
                return requireRenderingLayers;
            }
        }

        // Records nothing, only compares the globals published by the prepass stage against the resource data
        class CaptureGlobalsPass : ScriptableRenderPass
        {
            static readonly int k_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
            static readonly int k_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
            static readonly int k_CameraRenderingLayersTextureID = Shader.PropertyToID("_CameraRenderingLayersTexture");

            internal readonly List<string> mismatches = new();

            public CaptureGlobalsPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                CheckGlobal(renderGraph, k_CameraDepthTextureID, resourceData.cameraDepthTexture, "_CameraDepthTexture");
                CheckGlobal(renderGraph, k_CameraNormalsTextureID, resourceData.cameraNormalsTexture, "_CameraNormalsTexture");
                if (resourceData.renderingLayersTexture.IsValid())
                    CheckGlobal(renderGraph, k_CameraRenderingLayersTextureID, resourceData.renderingLayersTexture, "_CameraRenderingLayersTexture");
            }

            void CheckGlobal(RenderGraph renderGraph, int propertyId, TextureHandle expected, string name)
            {
                if (renderGraph.GetGlobal(propertyId) != expected)
                    mismatches.Add(name);
            }
        }

        public enum PrepassState
        {
            Drawn,
            SkippedByCullingMask,
            SkippedByLayerMask,
        }

        static readonly RenderingMode[] k_RenderingModes = { RenderingMode.Forward, RenderingMode.Deferred };
        const int k_PrepassLayer = 31;

        RenderPipelineAsset m_PreviousRenderPipelineAsset;
        UniversalRenderPipelineAsset m_Asset;
        readonly PrepassTexturesFeature[] m_Features = new PrepassTexturesFeature[k_RenderingModes.Length];
        readonly List<Object> m_Objects = new();
        Camera m_Camera;
        UniversalAdditionalCameraData m_CameraData;
        RenderTexture m_Destination;

        [UnityOneTimeSetUp]
        public IEnumerator OneTimeSetUp()
        {
            m_PreviousRenderPipelineAsset = QualitySettings.renderPipeline;

            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("These tests only run when URP is the active render pipeline.");

            // One renderer per rendering mode, so the tests only need a single pipeline switch
            var rendererDataList = new ScriptableRendererData[k_RenderingModes.Length];
            for (int i = 0; i < k_RenderingModes.Length; ++i)
            {
                m_Features[i] = ScriptableObject.CreateInstance<PrepassTexturesFeature>();
                m_Objects.Add(m_Features[i]);

                // A prepass restricted to a single layer can be skipped while opaque geometry stays visible
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.renderingMode = k_RenderingModes[i];
                rendererData.prepassLayerMask = 1 << k_PrepassLayer;
                rendererData.rendererFeatures.Add(m_Features[i]);
                m_Objects.Add(rendererData);
                rendererDataList[i] = rendererData;
            }

            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_Asset.m_RendererDataList = rendererDataList;
            m_Objects.Add(m_Asset);
            QualitySettings.renderPipeline = m_Asset;

            var cameraGameObject = new GameObject("Skipped Prepass Camera");
            m_Objects.Add(cameraGameObject);
            m_Camera = cameraGameObject.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_CameraData = cameraGameObject.AddComponent<UniversalAdditionalCameraData>();

            m_Destination = new RenderTexture(64, 64, 24);
            m_Objects.Add(m_Destination);

            // Let the pipeline switch to the test asset. Nothing may render during that frame in batchmode, so render once to
            // make sure the pipeline exists. Creating it recreates the renderers, which would discard the per-test setup.
            yield return null;
            RenderPipeline.SubmitRenderRequest(m_Camera, new RenderPipeline.StandardRequest { destination = m_Destination });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            QualitySettings.renderPipeline = m_PreviousRenderPipelineAsset;

            foreach (var obj in m_Objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            m_Objects.Clear();
        }

        // The drawn prepass cases check the expectations against the regular path
        static IEnumerable<TestCaseData> PrepassCases()
        {
            foreach (var renderingMode in k_RenderingModes)
            {
                foreach (var prepassState in new[] { PrepassState.Drawn, PrepassState.SkippedByCullingMask, PrepassState.SkippedByLayerMask })
                {
                    // Deferred also skips the gbuffer pass with an empty culling mask, which leaves nothing to produce the normals
                    if (renderingMode == RenderingMode.Deferred && prepassState == PrepassState.SkippedByCullingMask)
                        continue;

                    // Deferred ignores depth priming
                    var depthPrimingModes = renderingMode == RenderingMode.Deferred ?
                        new[] { DepthPrimingMode.Disabled } : new[] { DepthPrimingMode.Disabled, DepthPrimingMode.Forced };

                    foreach (var depthPrimingMode in depthPrimingModes)
                    {
                        foreach (bool depthInput in new[] { true, false })
                        {
                            // Without a depth request, depth is only published when the prepass renders into the depth texture
                            if (!depthInput && (renderingMode == RenderingMode.Deferred || depthPrimingMode == DepthPrimingMode.Forced))
                                continue;

                            foreach (bool renderingLayers in new[] { false, true })
                            {
                                string depth = depthInput ? "Yes" : "No";
                                string layers = renderingLayers ? "Yes" : "No";
                                yield return new TestCaseData(renderingMode, prepassState, depthPrimingMode, depthInput, renderingLayers)
                                    .SetName($"PrepassGlobalTextures_MatchResourceData({renderingMode}, Prepass:{prepassState}, DepthPriming:{depthPrimingMode}, DepthInput:{depth}, RenderingLayers:{layers})");
                            }
                        }
                    }
                }
            }
        }

        [TestCaseSource(nameof(PrepassCases))]
        public void PrepassGlobalTextures_MatchResourceData(RenderingMode renderingMode, PrepassState prepassState, DepthPrimingMode depthPrimingMode, bool depthInput, bool renderingLayers)
        {
            m_Camera.cullingMask = prepassState switch
            {
                PrepassState.SkippedByCullingMask => 0,
                PrepassState.SkippedByLayerMask => ~(1 << k_PrepassLayer),
                _ => ~0,
            };

            int rendererIndex = System.Array.IndexOf(k_RenderingModes, renderingMode);
            ((UniversalRenderer)m_Asset.GetRenderer(rendererIndex)).depthPrimingMode = depthPrimingMode;
            m_CameraData.SetRenderer(rendererIndex);

            var feature = m_Features[rendererIndex];
            var capturePass = feature.capturePass;
            feature.requireRenderingLayers = renderingLayers;
            capturePass.mismatches.Clear();
            capturePass.ConfigureInput(depthInput ?
                ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal : ScriptableRenderPassInput.Normal);

            // Deferred produces the normals in the gbuffer pass
            capturePass.renderPassEvent = renderingMode == RenderingMode.Deferred ?
                RenderPassEvent.AfterRenderingGbuffer : RenderPassEvent.AfterRenderingPrePasses;

            RenderPipeline.SubmitRenderRequest(m_Camera, new RenderPipeline.StandardRequest { destination = m_Destination });

            Assert.AreSame(capturePass, feature.capturePass, "The renderer was recreated during the render, discarding the test setup");
            Assert.IsEmpty(capturePass.mismatches, "Globals that don't match the resource data");
        }
    }
}
