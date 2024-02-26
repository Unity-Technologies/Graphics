using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class DiffusionProfileTests : MonoBehaviour
    {
        static readonly string k_ProfilePath = $"Assets/Temp/{nameof(DiffusionProfileTests)}/{nameof(DiffusionProfile_AutoRegister)}/DiffusionProfile.asset";

        private DiffusionProfileSettings m_Profile;
        private bool m_AutoRegister = false;
        private DiffusionProfileSettings[] m_RollBackProfiles;
        private DiffusionProfileList m_List;

        static DiffusionProfileSettings CreateValidDiffusionProfile()
        {
            // Some profile guids cannot be saved when float serializes to nan, recreate an asset if that's the case to avoid instabitities
            while (true)
            {
                var profile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
                var path = AssetDatabase.GenerateUniqueAssetPath(k_ProfilePath);
                AssetDatabase.CreateAsset(profile, path);
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                if (guid == HDUtils.ConvertVector4ToGUID(HDUtils.ConvertGUIDToVector4(guid)))
                    return profile;
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
            {
                Assert.Ignore("This test is only available when HDRP is the current pipeline.");
                return;
            }

            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            m_Profile = CreateValidDiffusionProfile();

            Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings<DiffusionProfileDefaultSettings>(out var diffusionProfileDefaultSettings));
            Assert.IsInstanceOf<DiffusionProfileDefaultSettings>(diffusionProfileDefaultSettings);

            Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>(out var defaultVolumeProfileSettings));
            Assert.IsInstanceOf<HDRPDefaultVolumeProfileSettings>(defaultVolumeProfileSettings);

            m_AutoRegister = diffusionProfileDefaultSettings.autoRegister;
            m_List = VolumeUtils.GetOrCreateDiffusionProfileList(defaultVolumeProfileSettings.volumeProfile);
            m_RollBackProfiles = m_List.ToArray();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_Profile != null) // If it is null, no setup has been done
            {
                GraphicsSettings.GetRenderPipelineSettings<DiffusionProfileDefaultSettings>().autoRegister = m_AutoRegister;
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
            GraphicsSettings.GetRenderPipelineSettings<DiffusionProfileDefaultSettings>().autoRegister = true;

            var profile1 = CreateValidDiffusionProfile();

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
        public void RegisterReferencedDiffusionProfiles()
        {
            GraphicsSettings.GetRenderPipelineSettings<DiffusionProfileDefaultSettings>().autoRegister = true;
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
