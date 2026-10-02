using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class LightFalloffSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<URPLightFalloffSettings>
    {
        class QuadraticAssetTestCase : IRenderPipelineGraphicsSettingsTestCase<URPLightFalloffSettings>
        {
            private LightFalloffMode m_PreviousAssetMode;

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
                globalSettingsAsset.m_AssetVersion = 15;
#pragma warning disable 618 // Type or member is obsolete
                m_PreviousAssetMode = renderPipelineAsset.m_ObsoleteLightFalloffMode;
                renderPipelineAsset.m_ObsoleteLightFalloffMode = LightFalloffMode.Quadratic;
#pragma warning restore 618
            }

            public void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                renderPipelineAsset.m_ObsoleteLightFalloffMode = m_PreviousAssetMode;
#pragma warning restore 618
            }

            public bool IsMigrationCorrect(URPLightFalloffSettings settings, out string message)
            {
                message = "A URP Asset that used the Quadratic falloff before URPLightFalloffSettings existed must migrate the project to Quadratic to preserve its lighting.";
                return settings != null && settings.lightFalloffMode == LightFalloffMode.Quadratic;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new QuadraticAssetTestCase())
                .SetName("When migrating a Quadratic URP Asset, the falloff mode is set to Quadratic"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPLightFalloffSettings> testCase)
        {
            base.DoTest(testCase);
        }

        [Test]
        public void NewSettingsDefaultToInverseSquared()
        {
            Assert.AreEqual(LightFalloffMode.InverseSquared, new URPLightFalloffSettings().lightFalloffMode,
                "A freshly created URPLightFalloffSettings must default to the Inverse Squared falloff mode.");
        }
    }
}
