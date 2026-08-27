using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class ShadowBiasSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<URPShadowBiasSettings>
    {
        class LegacyProjectTestCase : IRenderPipelineGraphicsSettingsTestCase<URPShadowBiasSettings>
        {
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
                globalSettingsAsset.m_AssetVersion = 12;
            }

            public bool IsMigrationCorrect(URPShadowBiasSettings settings, out string message)
            {
                message = "A global settings asset from before URPShadowBiasSettings existed must migrate to the legacy depth bias mode to preserve its shadows.";
                return settings != null && settings.depthBiasMode == ShadowDepthBiasMode.Legacy;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new LegacyProjectTestCase())
                .SetName("When migrating a global settings asset from before the depth bias mode existed, the mode is pinned to Depth Bias (Legacy)"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPShadowBiasSettings> testCase)
        {
            base.DoTest(testCase);
        }

        [Test]
        public void NewSettingsDefaultToSlopeScale()
        {
            Assert.AreEqual(ShadowDepthBiasMode.SlopeScale, new URPShadowBiasSettings().depthBiasMode,
                "A freshly created URPShadowBiasSettings must default to the slope-scale depth bias mode.");
        }
    }
}
