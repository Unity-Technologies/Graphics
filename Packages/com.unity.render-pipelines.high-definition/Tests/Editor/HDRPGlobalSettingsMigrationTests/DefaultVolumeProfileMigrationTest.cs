using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test.GlobalSettingsMigration
{
    class DefaultVolumeProfileSettingsMigrationTests : RenderPipelineGraphicsSettingsMigrationTestBase<HDRPDefaultVolumeProfileSettings>
    {
        class TestCase : IRenderPipelineGraphicsSettingsTestCase<HDRPDefaultVolumeProfileSettings>
        {
            bool m_ProfileExists;
            bool m_OldDiffusionProfilesExist;

            public TestCase(bool profileExists, bool oldDiffusionProfilesExist)
            {
                m_ProfileExists = profileExists;
                m_OldDiffusionProfilesExist = oldDiffusionProfilesExist;
            }

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
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

                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.ToRenderPipelineGraphicsSettings - 1;

                if (m_OldDiffusionProfilesExist)
                {
                    globalSettingsAsset.m_ObsoleteDiffusionProfileSettingsList = new DiffusionProfileSettings[1];
                    var diffusionProfile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
                    diffusionProfile.name = "MigratedDiffusionProfile";
                    diffusionProfile.profile.scatteringDistanceMultiplier = 987.6f;
                    globalSettingsAsset.m_ObsoleteDiffusionProfileSettingsList[0] = diffusionProfile;
                    globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.MoveDiffusionProfilesToVolume - 1;
                }
#pragma warning restore 618

            }

            public bool IsMigrationCorrect(HDRPDefaultVolumeProfileSettings settings, out string message)
            {
                message = string.Empty;

                if (settings.volumeProfile == null)
                {
                    message = $"The {nameof(VolumeProfile)} is missing";
                    return false;
                }

                var expectedName = m_ProfileExists ? "MigratedProfile" : "DefaultSettingsVolumeProfile";
                if (!settings.volumeProfile.name.Equals(expectedName))
                {
                    message = $"The {nameof(VolumeProfile)} is has name {settings.volumeProfile.name}, when the expected name is {expectedName}";
                    return false;
                }

                if (m_OldDiffusionProfilesExist)
                {

                    if (!settings.volumeProfile.TryGet<DiffusionProfileList>(out var overrides))
                    {
                        message = $"The {nameof(VolumeProfile)} ({settings.volumeProfile.name}) is missing {nameof(DiffusionProfileList)}.";
                        return false;
                    }

                    DiffusionProfileSettings migratedDiffusionProfileSettings = overrides.diffusionProfiles.value.FirstOrDefault(p => p.name.Equals("MigratedDiffusionProfile"));
                    if (migratedDiffusionProfileSettings == null)
                    {
                        message = $"The {nameof(VolumeProfile)} ({settings.volumeProfile.name}) does not have a profile with name MigratedDiffusionProfile";
                        return false;
                    }

                    var value = migratedDiffusionProfileSettings.profile.scatteringDistanceMultiplier;
                    if (!Mathf.Approximately(value, 987.6f))
                    {
                        message = $"The {nameof(VolumeProfile)} ({settings.volumeProfile.name}) 987.6f != {value}";
                        return false;
                    }
                }

                return true;
            }
        }

        class TestCaseMigrationOldDiffusionProfiles : IRenderPipelineGraphicsSettingsTestCase<HDRPDefaultVolumeProfileSettings>
        {
            private DiffusionProfileSettings[] m_Profiles;

            public void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
                globalSettingsAsset.m_Version = HDRenderPipelineGlobalSettings.Version.DisableAutoRegistration;

                m_Profiles = new[]
                {
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                    ScriptableObject.CreateInstance<DiffusionProfileSettings>(),
                };

#pragma warning disable 618 // Type or member is obsolete
                globalSettingsAsset.m_ObsoleteDiffusionProfileSettingsList = m_Profiles;
#pragma warning restore 618
            }

            public void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset,
                HDRenderPipelineAsset renderPipelineAsset)
            {
                foreach (var p in m_Profiles)
                    ScriptableObject.DestroyImmediate(p);
            }

            public bool IsMigrationCorrect(HDRPDefaultVolumeProfileSettings settings, out string message)
            {
                // Check that the list is now on the Volume Component of the Default Volume Profile
                var migratedDiffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(settings.volumeProfile);
                message = string.Empty;
                return m_Profiles.SequenceEqual(migratedDiffusionProfileList.diffusionProfiles.value);
            }
        }


        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new TestCase(profileExists: true, oldDiffusionProfilesExist: false))
                .SetName("When migrating an existing volume profile, settings are being transferred correctly"),
            new TestCaseData(new TestCase(profileExists: false, oldDiffusionProfilesExist: false))
                .SetName("When migrating a non-existing volume profile, the volume profile is being gather correctly"),
            new TestCaseData(new TestCase(profileExists: true, oldDiffusionProfilesExist: true))
                .SetName("When migrating old diffusion profiles to existing default volume profile, settings are being transferred correctly"),
            new TestCaseData(new TestCase(profileExists: false, oldDiffusionProfilesExist: true))
                .SetName("When migrating old diffusion profiles to non-existing default volume profile, settings are being transferred correctly"),
            new TestCaseData(new TestCaseMigrationOldDiffusionProfiles())
                .SetName("When migrating old diffusion profiles assigned directly on HDRPGlobalSettings, diffusion profiles are on the IRenderPipelineGraphicsSettings"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public void PerformMigration(IRenderPipelineGraphicsSettingsTestCase<HDRPDefaultVolumeProfileSettings> testCase)
        {
            base.DoTest(testCase);
        }
    }
}
