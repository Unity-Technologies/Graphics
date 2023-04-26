using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class HDGlobalSettingsTests : MonoBehaviour
    {
        static readonly string k_ProfilePath = $"Assets/Temp/{nameof(HDGlobalSettingsTests)}/{nameof(DiffusionProfile_AutoRegister)}/DiffusionProfile.asset";
        static readonly string k_MaterialPath = $"Assets/Temp/{nameof(HDGlobalSettingsTests)}/{nameof(DiffusionProfile_AutoRegister)}/Material.mat";

        [Test]
        public void DiffusionProfile_AutoRegister()
        {
            if (GraphicsSettings.currentRenderPipeline == null ||
                GraphicsSettings.currentRenderPipeline.GetType() != typeof(HDRenderPipelineAsset))
            {
                Assert.Ignore("Test project has no SRP configured, skipping test");
                return;
            }

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var hdGlobalSettings));
            Assert.IsInstanceOf<HDRenderPipelineGlobalSettings>(hdGlobalSettings);

            var instance = hdGlobalSettings as HDRenderPipelineGlobalSettings;
            Assert.IsNotNull(instance);

            var profiles = instance.diffusionProfileSettingsList;
            var autoRegister = instance.autoRegisterDiffusionProfiles;

            instance.diffusionProfileSettingsList = Array.Empty<DiffusionProfileSettings>();
            instance.autoRegisterDiffusionProfiles = true;

            var profile = ScriptableObject.CreateInstance<DiffusionProfileSettings>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            AssetDatabase.CreateAsset(profile, k_ProfilePath);

            var material = new Material(Shader.Find("HDRP/Lit"));
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_MaterialPath);
            AssetDatabase.CreateAsset(material, k_MaterialPath);
            
            HDMaterial.SetDiffusionProfile(material, profile);
            AssetDatabase.SaveAssets();

            Assert.IsTrue(instance.diffusionProfileSettingsList.Any(d => d == profile));

            // Rollback previous data
            instance.diffusionProfileSettingsList = profiles;
            instance.autoRegisterDiffusionProfiles = autoRegister;

            // Delete temporary assets
            AssetDatabase.DeleteAsset(k_MaterialPath);
            AssetDatabase.DeleteAsset(k_ProfilePath);
            AssetDatabase.SaveAssets();
        }
    }
}
