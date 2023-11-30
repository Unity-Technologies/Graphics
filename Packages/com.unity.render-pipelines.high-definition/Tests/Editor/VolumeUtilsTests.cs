using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class VolumeUtilsTests : MonoBehaviour
    {
        static readonly string k_ProfilePath = $"Assets/Temp/{nameof(VolumeUtilsTests)}/VolumeProfile.asset";

        private VolumeProfile m_Profile;
        private HDRenderPipelineGlobalSettings m_PipelineSettings;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
            {
                Assert.Ignore("This test is only available when HDRP is the current pipeline.");
                return;
            }

            m_Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            CoreUtils.EnsureFolderTreeInAssetFilePath(k_ProfilePath);
            AssetDatabase.CreateAsset(m_Profile, k_ProfilePath);

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var hdGlobalSettings));
            Assert.IsInstanceOf<HDRenderPipelineGlobalSettings>(hdGlobalSettings);

            m_PipelineSettings = hdGlobalSettings as HDRenderPipelineGlobalSettings;
            Assert.IsNotNull(m_PipelineSettings);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (m_Profile != null) // If it is null, no setup has been done
            {
                AssetDatabase.DeleteAsset($"Assets/Temp/{nameof(VolumeUtilsTests)}");
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void BuildDefaultNameForVolumeProfile()
        {
            string result = VolumeUtils.BuildDefaultNameForVolumeProfile(m_Profile);
            Assert.AreEqual($"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/{m_Profile.name}.asset", result);
        }

        [Test]
        public void CopyVolumeProfileFromResourcesToAssets()
        {
            string result = VolumeUtils.BuildDefaultNameForVolumeProfile(m_Profile);

            Assert.IsFalse(AssetDatabase.AssetPathExists(result));

            VolumeUtils.CopyVolumeProfileFromResourcesToAssets(m_Profile);

            Assert.IsTrue(AssetDatabase.AssetPathExists(result));

            AssetDatabase.DeleteAsset(result);
        }

        [Test]
        public void IsDefaultVolumeProfile_ReturnsTrueWhenIsTheDefault()
        {
            Assert.IsTrue(VolumeUtils.IsDefaultVolumeProfile(m_Profile, m_Profile));
        }

        [Test]
        public void IsDefaultVolumeProfile_ReturnsFalseWhenIsNull()
        {
            Assert.IsFalse(VolumeUtils.IsDefaultVolumeProfile(null, m_Profile));
        }

        [Test]
        public void IsDefaultVolumeProfile_ReturnsFalseWhenIsNotTheDefault()
        {
            var otherProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            Assert.IsFalse(VolumeUtils.IsDefaultVolumeProfile(otherProfile, m_Profile));
            ScriptableObject.DestroyImmediate(otherProfile);
        }
    }
}
