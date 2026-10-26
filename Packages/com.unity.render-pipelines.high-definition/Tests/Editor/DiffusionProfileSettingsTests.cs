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
        private VolumeProfile m_VolumeProfile;
        private DiffusionProfileList m_DiffusionProfileList;
        private DiffusionProfileSettings[] m_RollbackProfiles;

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

            Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>(out var defaultVolumeProfileSettings));
            m_VolumeProfile = defaultVolumeProfileSettings.volumeProfile;
            m_DiffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(m_VolumeProfile);
            m_RollbackProfiles = m_DiffusionProfileList.ToArray();

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

            // Restore the shared list here (not at the end of the test body) so an assert failure can't leave the
            // default volume profile's DiffusionProfileList emptied and leak into subsequent tests. Mirror the
            // mutation path so the revert flushes to disk and clears VolumeManager's cached state.
            if (m_DiffusionProfileList != null)
            {
                m_DiffusionProfileList.ReplaceWithArray(m_RollbackProfiles);
                EditorUtility.SetDirty(m_VolumeProfile);
                VolumeManager.instance.OnVolumeProfileChanged(m_VolumeProfile);
            }

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


            m_DiffusionProfileList.ReplaceWithArray(Array.Empty<DiffusionProfileSettings>());
            m_DiffusionProfileSettings.autoRegister = true;

            HDMaterial.SetDiffusionProfile(m_Material, m_Profile);
            AssetDatabase.SaveAssets();
            // Registration is driven by OnPostprocessAllAssets over the import batch; SaveAssets alone doesn't
            // reliably re-import the material, so force it to guarantee the postprocessor sees m_Material.
            AssetDatabase.ImportAsset(k_MaterialPath, ImportAssetOptions.ForceUpdate);

            bool found = false;
            foreach (var d in m_DiffusionProfileList.ToArray())
            {
                if (d != m_Profile)
                    continue;

                found = true;
                break;
            }
            Assert.IsTrue(found);
        }
    }
}
