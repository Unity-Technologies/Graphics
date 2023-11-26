using NUnit.Framework;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class RenderingPathFrameSettingsMigrationTests : RenderPipelineGraphicsSettingsMigrationTestBase<RenderingPathFrameSettings>

    {
        class TestCase : IRenderPipelineGraphicsSettingsTestCase<RenderingPathFrameSettings>
        {
            private FrameSettingsRenderType m_Type;

            public TestCase(FrameSettingsRenderType type)
            {
                m_Type = type;
            }

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {

#pragma warning disable 618 // Type or member is obsolete
                ref var obsoleteCameraFrameSettings = ref globalSettingsAsset.m_ObsoleteRenderingPath.GetDefaultFrameSettings(m_Type);

                obsoleteCameraFrameSettings.sssCustomSampleBudget = int.MinValue;
                obsoleteCameraFrameSettings.SetEnabled(FrameSettingsField.ClearGBuffers, true);
#pragma warning restore 618

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.RenderingPathFrameSettings;
            }

            public bool IsMigrationCorrect(RenderingPathFrameSettings settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = true;

                var frameSettings = settings.GetDefaultFrameSettings(m_Type);

                isMigrationCorrect &= int.MinValue == frameSettings.sssCustomSampleBudget;
                isMigrationCorrect &= frameSettings.IsEnabled(FrameSettingsField.ClearGBuffers);

                if (!isMigrationCorrect)
                    message = $"{m_Type} has correctly been migrated";
                return isMigrationCorrect;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase(FrameSettingsRenderType.Camera))
                .SetName("When performing a migration of FrameSettings for Camera, settings are being transferred correctly"),
            new TestCaseData(new TestCase(FrameSettingsRenderType.CustomOrBakedReflection))
                .SetName("When performing a migration of FrameSettings for Custom Or Baked Reflection, settings are being transferred correctly"),
            new TestCaseData(new TestCase(FrameSettingsRenderType.RealtimeReflection))
                .SetName("When performing a migration of FrameSettings for Realtime Reflection, settings are being transferred correctly")
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<RenderingPathFrameSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
