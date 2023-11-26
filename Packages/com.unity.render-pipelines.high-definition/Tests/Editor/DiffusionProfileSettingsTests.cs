using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class DiffusionProfileSettingsTests
    {
        static readonly string k_ProfilePath = $"Assets/Temp/{nameof(DiffusionProfileSettingsTests)}/DiffusionProfile.asset";
        static readonly string k_MaterialPath = $"Assets/Temp/{nameof(DiffusionProfileSettingsTests)}/Material.mat";

        private DiffusionProfileDefaultSettings m_DiffusionProfileSettings;
        private DiffusionProfileSettings m_Profile = null;
        private Material m_Material = null;
        private bool m_OldAutoRegisterValue = false;

        [SetUp]
        public void Setup()
        {
            if (GraphicsSettings.currentRenderPipelineAssetType != typeof(HDRenderPipelineAsset))
            {
                Assert.Ignore("Test project has no SRP configured, skipping test");
                return;
            }

            m_DiffusionProfileSettings = GraphicsSettings.GetRenderPipelineSettings<DiffusionProfileDefaultSettings>();
            Assert.IsNotNull(m_DiffusionProfileSettings);

            m_OldAutoRegisterValue = m_DiffusionProfileSettings.autoRegister;
            m_DiffusionProfileSettings.autoRegister = false;

            m_Profile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            AssetDatabase.CreateAsset(m_Profile, k_ProfilePath);

            m_Material = new Material(Shader.Find("HDRP/Lit"));
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_MaterialPath);
            AssetDatabase.CreateAsset(m_Material, k_MaterialPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_DiffusionProfileSettings == null)
                return;

            m_DiffusionProfileSettings.autoRegister = m_OldAutoRegisterValue;

            AssetDatabase.DeleteAsset(k_MaterialPath);
            AssetDatabase.DeleteAsset(k_ProfilePath);
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void When_AutoRegister_IsTrue_TheDiffusionProfileIsRegistered()
        {
            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var hdGlobalSettings));
            Assert.IsInstanceOf<HDRenderPipelineGlobalSettings>(hdGlobalSettings);

            Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>(out var defaultVolumeProfileSettings));
            Assert.IsInstanceOf<HDRPDefaultVolumeProfileSettings>(defaultVolumeProfileSettings);


            var diffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(defaultVolumeProfileSettings.volumeProfile);
            var profiles = diffusionProfileList.ToArray();

            diffusionProfileList.ReplaceWithArray(Array.Empty<DiffusionProfileSettings>());
            m_DiffusionProfileSettings.autoRegister = true;

            HDMaterial.SetDiffusionProfile(m_Material, m_Profile);
            AssetDatabase.SaveAssets();

            bool found = false;
            foreach (var d in diffusionProfileList.ToArray())
            {
                if (d != m_Profile)
                    continue;

                found = true;
                break;
            }
            Assert.IsTrue(found);

            // Rollback previous data
            diffusionProfileList.ReplaceWithArray(profiles);
        }
    }
}
