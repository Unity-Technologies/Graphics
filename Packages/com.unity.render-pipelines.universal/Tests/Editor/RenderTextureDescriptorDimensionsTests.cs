using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class RenderTextureDescriptorDimensionsTests
    {
        private Camera m_Camera;
        private UniversalCameraData m_CameraData;
        public RenderTexture m_RT;

        public class RenderScaleTestCase
        {
            public float renderScale { get; }
            public bool cameraTargetIsRenderTexture { get; }

            public RenderScaleTestCase(float renderScale, bool cameraTargetIsRenderTexture)
            {
                this.renderScale = renderScale;
                this.cameraTargetIsRenderTexture = cameraTargetIsRenderTexture;
            }

            public override string ToString()
            {
                return $"Render Scale : {renderScale}, Rendering To Texture : {cameraTargetIsRenderTexture}";
            }
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            var go = new GameObject(nameof(RenderTextureDescriptorDimensionsTests));
            m_Camera = go.AddComponent<Camera>();

            m_CameraData = new UniversalCameraData
            {
                camera = m_Camera
            };

            m_RT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            m_RT.Create();
        }

        [OneTimeTearDown]
        public void GlobalCleanup()
        {
            Object.DestroyImmediate(m_Camera.gameObject);
            m_RT.Release();
        }

        public RenderTextureDescriptor CreateRenderTextureDescriptor()
        {
            bool isHdrEnabled = false;
            HDRColorBufferPrecision requestHDRColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            int msaaSamples = 1;
            bool needsAlpha = false;
            bool requiresOpaqueTexture = false;

            return UniversalRenderPipeline.CreateRenderTextureDescriptor(
                m_Camera,
                m_CameraData,
                isHdrEnabled,
                requestHDRColorBufferPrecision,
                msaaSamples,
                needsAlpha,
                requiresOpaqueTexture);
        }

        public void CheckDimensions(RenderTextureDescriptor desc, RenderScaleTestCase testCase)
        {
            var expectedWidth = Mathf.Max(1, (int)(m_Camera.pixelWidth * testCase.renderScale));
            var expectedHeight = Mathf.Max(1, (int)(m_Camera.pixelHeight * testCase.renderScale));

            Assert.AreEqual(expectedWidth, desc.width);
            Assert.AreEqual(expectedHeight, desc.height);
        }

        public static IEnumerable<RenderScaleTestCase> TestCasesTextureDimension()
        {
            // Texture target
            yield return new RenderScaleTestCase(0f, true);
            yield return new RenderScaleTestCase(0.5f, true);
            yield return new RenderScaleTestCase(1f, true);
            yield return new RenderScaleTestCase(2f, true);

            // Backbuffer target
            yield return new RenderScaleTestCase(0f, false);
            yield return new RenderScaleTestCase(0.5f, false);
            yield return new RenderScaleTestCase(1f, false);
            yield return new RenderScaleTestCase(2f, false);
        }

        [TestCaseSource(nameof(TestCasesTextureDimension))]
        public void TextureDescriptor_FromCameraData(RenderScaleTestCase testCase)
        {
            // Setup needed data for the test
            m_CameraData.renderScale = testCase.renderScale;
            m_Camera.targetTexture = (testCase.cameraTargetIsRenderTexture) ? m_RT : null;

            var desc = CreateRenderTextureDescriptor();
            CheckDimensions(desc, testCase);
        }

        public class TestRTDimensionNativeRenderPass : ScriptableRenderPass {}

        [TestCaseSource(nameof(TestCasesTextureDimension))]
        public void TextureDescriptor_FromNativeRenderPass(RenderScaleTestCase testCase)
        {
            // Setup needed data for the test
            m_CameraData.renderScale = testCase.renderScale;
            m_Camera.targetTexture = (testCase.cameraTargetIsRenderTexture) ? m_RT : null;

            m_CameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor();

            var nativeRenderPass = new TestRTDimensionNativeRenderPass();
            ScriptableRenderer.GetRenderTextureDescriptor(m_CameraData, nativeRenderPass, out var desc);
            CheckDimensions(desc, testCase);
        }
    }
}
