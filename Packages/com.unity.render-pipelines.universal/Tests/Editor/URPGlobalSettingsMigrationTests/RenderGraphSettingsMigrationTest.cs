using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShaderVariantLogLevel = UnityEngine.Rendering.ShaderVariantLogLevel;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class RenderGraphSettingsMigrationTest : RenderPipelineGraphicsSettingsMigrationTestBase<RenderGraphSettings>
    {
        class TestCase1 : IRenderPipelineGraphicsSettingsTestCase<RenderGraphSettings>
        {
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_EnableRenderGraph = true;
#pragma warning restore 618

                globalSettingsAsset.m_AssetVersion = 5;
            }

            public bool IsMigrationCorrect(RenderGraphSettings settings, out string message)
            {
                message = string.Empty;
                return !settings.enableRenderCompatibilityMode;
            }
        }

        class TestCase2 : IRenderPipelineGraphicsSettingsTestCase<RenderGraphSettings>
        {
            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset,
                UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_EnableRenderGraph = false;
#pragma warning restore 618

                globalSettingsAsset.m_AssetVersion = 5;
            }

            public bool IsMigrationCorrect(RenderGraphSettings settings, out string message)
            {
                message = string.Empty;
                return settings.enableRenderCompatibilityMode;
            }
        }


        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase1())
                .SetName(
                    "When performing a migration of m_EnableRenderGraph ( true ), settings are being transferred correctly"),
            new TestCaseData(new TestCase2())
                .SetName(
                    "When performing a migration of m_EnableRenderGraph ( false ), settings are being transferred correctly"),

        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<RenderGraphSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
