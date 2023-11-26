using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{

    class LookDevVolumeProfileIsMigratedTests : RenderPipelineGraphicsSettingsMigrationTestBase<LookDevVolumeProfileSettings>

    {
        class TestCase : IRenderPipelineGraphicsSettingsTestCase<LookDevVolumeProfileSettings>
        {
            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_ObsoleteLookDevVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                globalSettingsAsset.m_ObsoleteLookDevVolumeProfile.name = "MigratedProfile";
#pragma warning restore 618

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.CustomPostProcessOrdersSettings - 1;
            }

            public bool IsMigrationCorrect(LookDevVolumeProfileSettings settings, out string message)
            {
                message = string.Empty;

                bool isMigrationCorrect = settings.volumeProfile != null;
                isMigrationCorrect &= settings.volumeProfile.name.Equals("MigratedProfile");

                return isMigrationCorrect;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase())
                .SetName("When performing a migration of LookDevVolumeProfileSettings, settings are being transferred correctly")
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<LookDevVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
