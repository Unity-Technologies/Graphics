using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Test.GlobalSettingsMigration
{
    class DefaultVolumeProfileSettingsMigrationTests : RenderPipelineGraphicsSettingsMigrationTestBase<URPDefaultVolumeProfileSettings>
    {
        class TestCase : IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings>
        {
            bool m_ProfileExists;

            public TestCase(bool profileExists)
            {
                m_ProfileExists = profileExists;
            }

            public void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
            {
#pragma warning disable 618 // Type or member is obsolete
                if (m_ProfileExists)
                {
                    globalSettingsAsset.m_ObsoleteDefaultVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    globalSettingsAsset.m_ObsoleteDefaultVolumeProfile.name = "MigratedProfile";
                }
                else
                {
                    globalSettingsAsset.m_ObsoleteDefaultVolumeProfile = null;
                }

                globalSettingsAsset.m_AssetVersion = 4;
#pragma warning restore 618

            }

            public bool IsMigrationCorrect(URPDefaultVolumeProfileSettings settings, out string message)
            {
                if (settings.volumeProfile == null)
                {
                    message = "The default volume profile is null";
                    return false;
                }

                message = $"{settings.volumeProfile.name} is not correct";
                return settings.volumeProfile.name.Equals(m_ProfileExists
                        ? "MigratedProfile"
                        : "DefaultVolumeProfile");
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase(true))
                .SetName("When migrating an existing volume profile, settings are being transferred correctly"),
            new TestCaseData(new TestCase(false))
                .SetName("When migrating a non-existing volume profile, the volume profile is being gather correctly"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<URPDefaultVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
