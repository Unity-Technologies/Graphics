using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    public class RenderPipelineGlobalSettingsTests
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

        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset")
                .SetName("URP Asset settings are found")
                .Returns("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings"),
            new TestCaseData("Assets/PipelineAssets/New HDRenderPipelineAsset.asset")
                .SetName("HDRP Asset settings are found")
                .Returns("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings"),
        };

        private RenderPipelineAsset LoadAsset(string renderPipelineAssetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(renderPipelineAssetPath);
            if (!string.IsNullOrEmpty(renderPipelineAssetPath))
                Assert.IsNotNull(asset, renderPipelineAssetPath);
            return asset as RenderPipelineAsset;
        }

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public string TryGetCurrentRenderPipelineGlobalSettings(string renderPipelineAssetPath)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAssetPath);
            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings));
            return settings.GetType().FullName;
        }
    }
}
