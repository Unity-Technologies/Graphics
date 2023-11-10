using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class DiffusionProfileTests : MonoBehaviour
    {
        static readonly string k_ProfilePath = $"Assets/Temp/{nameof(DiffusionProfileTests)}/{nameof(DiffusionProfile_AutoRegister)}/DiffusionProfile.asset";

        private DiffusionProfileSettings m_Profile;
        private bool m_AutoRegister = false;
        private DiffusionProfileSettings[] m_RollBackProfiles;
        private DiffusionProfileList m_List;
        private HDRenderPipelineGlobalSettings m_PipelineSettings;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
            {
                Assert.Ignore("This test is only available when HDRP is the current pipeline.");
                return;
            }

            m_Profile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            AssetDatabase.CreateAsset(m_Profile, k_ProfilePath);

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var hdGlobalSettings));
            Assert.IsInstanceOf<HDRenderPipelineGlobalSettings>(hdGlobalSettings);

            m_PipelineSettings = hdGlobalSettings as HDRenderPipelineGlobalSettings;
            Assert.IsNotNull(m_PipelineSettings);

            m_AutoRegister = m_PipelineSettings.autoRegisterDiffusionProfiles;
            m_List = VolumeUtils.GetOrCreateDiffusionProfileList(m_PipelineSettings.volumeProfile);
            m_RollBackProfiles = m_List.ToArray();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_Profile != null) // If it is null, no setup has been done
            {
                m_PipelineSettings.autoRegisterDiffusionProfiles = m_AutoRegister;
                m_List.ReplaceWithArray(m_RollBackProfiles);

                AssetDatabase.DeleteAsset($"Assets/Temp/{nameof(DiffusionProfileTests)}");
                AssetDatabase.SaveAssets();
            }
        }

        private static void CreateMaterialAndSetProfile(string materialPath, DiffusionProfileSettings profile)
        {
            materialPath = AssetDatabase.GenerateUniqueAssetPath(materialPath);
            var material = new Material(Shader.Find("HDRP/Lit"));

            AssetDatabase.CreateAsset(material, materialPath);
            HDMaterial.SetDiffusionProfile(material, profile);
        }

        [Test]
        public void DiffusionProfile_AutoRegister()
        {
            m_List.ReplaceWithArray(Array.Empty<DiffusionProfileSettings>());
            m_PipelineSettings.autoRegisterDiffusionProfiles = true;

            var profile1 = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            AssetDatabase.CreateAsset(profile1, AssetDatabase.GenerateUniqueAssetPath(k_ProfilePath));

            var materialPath = $"Assets/Temp/{nameof(DiffusionProfileTests)}/{nameof(DiffusionProfile_AutoRegister)}/Material.mat";

            CreateMaterialAndSetProfile(materialPath, m_Profile);
            CreateMaterialAndSetProfile(materialPath, profile1);

            AssetDatabase.SaveAssets(); // Trigger OnPostProcessAllAssets where the registration is being performed
            
            var profiles = m_List.ToArray();
            bool bothAreRegistered = VolumeUtils.IsDiffusionProfileRegistered(m_Profile, profiles) &&
                                     VolumeUtils.IsDiffusionProfileRegistered(profile1, profiles);

            Assert.IsTrue(bothAreRegistered);
        }

        [Test]
        public void IsDiffusionProfileRegistered()
        {
            Assert.IsTrue(VolumeUtils.IsDiffusionProfileRegistered(m_Profile, new DiffusionProfileSettings[]{ m_Profile }));
            Assert.IsFalse(VolumeUtils.IsDiffusionProfileRegistered(m_Profile, Array.Empty<DiffusionProfileSettings>()));
        }

        [Test]
        public void TryAddSingleDiffusionProfile()
        {
            var volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            Assert.IsTrue(VolumeUtils.TryAddSingleDiffusionProfile(volumeProfile, m_Profile));
            Assert.IsFalse(VolumeUtils.TryAddSingleDiffusionProfile(volumeProfile, m_Profile));

            ScriptableObject.DestroyImmediate(volumeProfile);
        }

        [Test]
        public void NullDiffusionProfileSettingsAreRemovedWhenAddingANewDiffusionProfileSettings()
        {
            var volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var list = VolumeUtils.GetOrCreateDiffusionProfileList(volumeProfile);
            list.ReplaceWithArray(new DiffusionProfileSettings[] {null, null});
            
            Assert.IsTrue(VolumeUtils.TryAddSingleDiffusionProfile(volumeProfile, m_Profile));
            Assert.AreEqual(1, list.ToArray().Length);

            ScriptableObject.DestroyImmediate(volumeProfile);
        }

        [Test]
        public void MigrationAddsTheDiffusionProfilesToTheVolumeProfile()
        {
            var instance = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();

            // Set the version just before the migration step MoveDiffusionProfilesToVolume.
            instance.m_Version = HDRenderPipelineGlobalSettings.Version.DisableAutoRegistration;

            var profiles = new [] {
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
            instance.m_ObsoleteDiffusionProfileSettingsList = profiles;
#pragma warning restore 618

            instance.Migrate();

            // Check that the list is now on the Volume Component of the Default Volume Profile
            var migratedDiffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(instance.volumeProfile);
            Assert.AreEqual(profiles, migratedDiffusionProfileList.diffusionProfiles.value);

            foreach (var p in profiles)
                ScriptableObject.DestroyImmediate(p);

            ScriptableObject.DestroyImmediate(instance);
        }

        [Test]
        public void RegisterReferencedDiffusionProfiles()
        {
            m_PipelineSettings.autoRegisterDiffusionProfiles = true;
            m_List.ReplaceWithArray(Array.Empty<DiffusionProfileSettings>());

            var materialPath = $"Assets/Temp/{nameof(DiffusionProfileTests)}/{nameof(DiffusionProfile_AutoRegister)}/Material.mat";

            CreateMaterialAndSetProfile(materialPath, m_Profile);

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            using (var registerer = new VolumeUtils.DiffusionProfileRegisterScope())
            {
                for (int i = 0; i < 4; ++i)
                    registerer.RegisterReferencedDiffusionProfilesFromMaterial(material);
            }

            var profiles = m_List.ToArray();
            Assert.IsTrue(VolumeUtils.IsDiffusionProfileRegistered(m_Profile, profiles));
            Assert.AreEqual(1, m_List.Length);
        }

        [Test]
        public void ReplaceWithArrayExceedsLimitThrowsException()
        {
            using (ListPool<DiffusionProfileSettings>.Get(out var tmp))
            {
                for (int i = 0; i < DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT; ++i)
                    tmp.Add(ScriptableObject.CreateInstance<DiffusionProfileSettings>());

                Assert.That(() => m_List.ReplaceWithArray(tmp.ToArray()), Throws.Exception);

                foreach (var p in tmp)
                    ScriptableObject.DestroyImmediate(p);
            }
        }
    }
}
