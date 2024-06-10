using System;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class NativeRenderPassTests
    {
        internal class TestHelper
        {
            internal UniversalRendererData rendererData;
            internal UniversalCameraData cameraData;
            internal UniversalRenderPipelineAsset urpAsset;
            internal ScriptableRenderer scriptableRenderer;

            public TestHelper()
            {
                try
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    
                    urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                    urpAsset.name = "TestHelper_URPAsset";
                    GraphicsSettings.defaultRenderPipeline = urpAsset;

                    scriptableRenderer = urpAsset.GetRenderer(0);

                    cameraData = new UniversalCameraData();

                    ResetData();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.StackTrace);
                    Cleanup();
                }
            }

            internal void ResetData()
            {
                scriptableRenderer.useRenderPassEnabled = true;
            }

            internal void Cleanup()
            {
                ScriptableObject.DestroyImmediate(urpAsset);
                ScriptableObject.DestroyImmediate(rendererData);
            }
        }

        private TestHelper m_TestHelper;
        
        private RenderPipelineAsset m_PreviousRenderPipelineAssetGraphicsSettings;
        private RenderPipelineAsset m_PreviousRenderPipelineAssetQualitySettings;
        public class TestRenderPassUseNRP : ScriptableRenderPass
        {
            public TestRenderPassUseNRP()
            {
                // Initialize with this argument to true, to avoid other unrelated errors
                overrideCameraTarget = true;
                // Enable the use of Native Render Pass. This is set to true by defalult, but we want to make it explicit
                useNativeRenderPass = true;
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_PreviousRenderPipelineAssetGraphicsSettings = GraphicsSettings.defaultRenderPipeline;
            m_PreviousRenderPipelineAssetQualitySettings = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
        }

        [SetUp]
        public void Setup()
        {
            m_TestHelper = new();
            m_TestHelper.ResetData();
        }

        [TearDown]
        public void TearDown()
        {
            m_TestHelper.Cleanup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_PreviousRenderPipelineAssetGraphicsSettings;
            QualitySettings.renderPipeline = m_PreviousRenderPipelineAssetQualitySettings;
        }

        public void InitializeRenderPassQueue(ScriptableRenderer renderer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                renderer.EnqueuePass(new TestRenderPassUseNRP());
            }
        }

        [Test]
        public void UnderLimitRenderPassInNRP()
        {
            // Use kRenderPassMaxCount so this is the maximun allowed
            InitializeRenderPassQueue(m_TestHelper.scriptableRenderer, ScriptableRenderer.kRenderPassMaxCount);
            // Check that no exception is thrown.
            Assert.DoesNotThrow(() => m_TestHelper.scriptableRenderer.SetupNativeRenderPassFrameData(m_TestHelper.cameraData, true));
        }

        [Test]
        public void OverLimitRenderPassInNRP()
        {
            // Increase by one the maximum allowed render passes
            InitializeRenderPassQueue(m_TestHelper.scriptableRenderer, ScriptableRenderer.kRenderPassMaxCount+1);
            // Check that a logError is thrown, but no other errors are thrown.
            m_TestHelper.scriptableRenderer.SetupNativeRenderPassFrameData( m_TestHelper.cameraData, true );
            LogAssert.Expect($"Exceeded the maximum number of Render Passes (${ScriptableRenderer.kRenderPassMaxCount}). Please consider using Render Graph to support a higher number of render passes with Native RenderPass, note support will be enabled by default.");
        }

    }

    

}
