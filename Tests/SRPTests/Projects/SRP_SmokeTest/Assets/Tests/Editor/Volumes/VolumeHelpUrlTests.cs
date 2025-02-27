using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Tests
{
    public class VolumeHelpUrlTests
    {
        RenderPipelineAsset m_GraphicsSettingsRPAsset = null;

        [SetUp]
        public void SetUp()
        {
            m_GraphicsSettingsRPAsset = GraphicsSettings.defaultRenderPipeline;
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_GraphicsSettingsRPAsset;
        }

        private RenderPipelineAsset LoadAsset(string renderPipelineAssetPath)
        {
            if (string.IsNullOrEmpty(renderPipelineAssetPath))
                return null;

            var asset = AssetDatabase.LoadMainAssetAtPath(renderPipelineAssetPath) as RenderPipelineAsset;
            Assert.IsNotNull(asset, renderPipelineAssetPath, $"Unable to load {renderPipelineAssetPath}");
            return asset;
        }

        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset" , typeof(UniversalRenderPipeline))
                .SetName("Volumes URL's are correct when URP is the active pipeline"),
            new TestCaseData("Assets/PipelineAssets/HDRenderPipelineAsset.asset", typeof(HDRenderPipeline))
                .SetName("Volumes URL's are correct when HDRP is the active pipeline"),
        };

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public void CheckVolumeUrls(string renderPipelineAsset, Type renderPipelineType)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAsset);

            var volumeProfileAsset = AssetDatabase.LoadMainAssetAtPath("Assets/DefaultResources/DefaultVolumeProfile.asset");
            Assert.IsNotNull(volumeProfileAsset);
            
            Assert.IsTrue(DocumentationUtils.TryGetPackageInfoForType(renderPipelineType, out var name, out var version));

            var expectedLink = DocumentationInfo.GetPackageLink(name, version, "Volume-Profile"); 

            Assert.AreEqual(expectedLink, Help.GetHelpURLForObject(volumeProfileAsset));

            var go = new GameObject("CheckVolumeUrlAreCorrectForURP");
            var component = go.AddComponent<Volume>();

            expectedLink = DocumentationInfo.GetPackageLink(name, version, "Volumes");

            Assert.AreEqual(expectedLink, Help.GetHelpURLForObject(component));

            GameObject.DestroyImmediate(go);
        }
    }
}
