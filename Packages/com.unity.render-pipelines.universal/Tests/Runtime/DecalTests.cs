using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    class DecalTests
    {
        GameObject m_DecalProjector;
        GameObject m_DecalProjector2;
        DecalEntityManager m_EntityManager;
        Shader m_Shader;

        [SetUp]
        public void Setup()
        {
            m_EntityManager = new DecalEntityManager();
            m_DecalProjector = new GameObject("DecalProjector");
            m_DecalProjector2 = new GameObject("DecalProjector");
            m_Shader = Shader.Find("Hidden/InternalErrorShader");
        }

        [TearDown]
        public void Cleanup()
        {
            m_EntityManager.Dispose();
            Object.DestroyImmediate(m_DecalProjector);
            Object.DestroyImmediate(m_DecalProjector2);
        }

        [Test]
        public void DecalDestroyEmptyChunk()
        {
            var decalProjector = m_DecalProjector.AddComponent<DecalProjector>();
            decalProjector.material = new Material(m_Shader);
            var decalProjector2 = m_DecalProjector.AddComponent<DecalProjector>();
            decalProjector2.material = new Material(m_Shader);

            var entity = m_EntityManager.CreateDecalEntity(decalProjector);
            var entity2 = m_EntityManager.CreateDecalEntity(decalProjector2);

            m_EntityManager.DestroyDecalEntity(entity);

            m_EntityManager.Update();

            Assert.AreEqual(1, m_EntityManager.chunkCount);
        }
    }

    // The DBuffer technique requires a prepass, which URP skips when the camera can't see any prepass layer.
    class DecalDBufferSkippedPrepassTests
    {
        static readonly RenderingMode[] k_RenderingModes = { RenderingMode.Forward, RenderingMode.Deferred };

        RenderPipelineAsset m_PreviousRenderPipelineAsset;
        UniversalRenderPipelineAsset m_Asset;
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
            if (DecalRendererFeature.isGLDevice)
                Assert.Ignore("The DBuffer technique is not supported with OpenGL.");

            // One renderer per rendering mode and decal layers setting, so the tests only need a single pipeline switch
            var rendererDataList = new ScriptableRendererData[k_RenderingModes.Length * 2];
            for (int i = 0; i < rendererDataList.Length; ++i)
            {
                var decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
                decalFeature.settings.technique = DecalTechniqueOption.DBuffer;
                decalFeature.settings.dBufferSettings = new DBufferSettings();
                decalFeature.settings.decalLayers = i % 2 == 1;
                m_Objects.Add(decalFeature);

                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.renderingMode = k_RenderingModes[i / 2];
                rendererData.rendererFeatures.Add(decalFeature);
                m_Objects.Add(rendererData);
                rendererDataList[i] = rendererData;
            }

            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            m_Asset.m_RendererDataList = rendererDataList;
            m_Objects.Add(m_Asset);
            QualitySettings.renderPipeline = m_Asset;

            var cameraGameObject = new GameObject("DBuffer Skipped Prepass Camera");
            m_Objects.Add(cameraGameObject);
            m_Camera = cameraGameObject.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_Camera.cullingMask = 0;
            m_CameraData = cameraGameObject.AddComponent<UniversalAdditionalCameraData>();

            m_Destination = new RenderTexture(64, 64, 24);
            m_Objects.Add(m_Destination);

            // Let the pipeline switch to the test asset
            yield return null;
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

        static IEnumerable<TestCaseData> RenderCases()
        {
            foreach (var renderingMode in k_RenderingModes)
            {
                foreach (var depthPrimingMode in new[] { DepthPrimingMode.Disabled, DepthPrimingMode.Forced })
                {
                    foreach (bool decalLayers in new[] { false, true })
                    {
                        string layers = decalLayers ? "Yes" : "No";
                        yield return new TestCaseData(renderingMode, depthPrimingMode, decalLayers)
                            .SetName($"RenderWithEmptyCullingMask_DoesNotLogErrors({renderingMode}, DepthPriming:{depthPrimingMode}, DecalLayers:{layers})")
                            .Returns(null);
                    }
                }
            }
        }

        [UnityTest, TestCaseSource(nameof(RenderCases))]
        public IEnumerator RenderWithEmptyCullingMask_DoesNotLogErrors(RenderingMode renderingMode, DepthPrimingMode depthPrimingMode, bool decalLayers)
        {
            int rendererIndex = System.Array.IndexOf(k_RenderingModes, renderingMode) * 2 + (decalLayers ? 1 : 0);
            ((UniversalRenderer)m_Asset.GetRenderer(rendererIndex)).depthPrimingMode = depthPrimingMode;
            m_CameraData.SetRenderer(rendererIndex);

            RenderPipeline.SubmitRenderRequest(m_Camera, new RenderPipeline.StandardRequest { destination = m_Destination });

            // Validation errors are logged from the render thread, so give it time to process the frame
            yield return null;
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }
    }
}
