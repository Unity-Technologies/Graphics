using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Test
{
    interface IRenderPipelineGraphicsSettingsTestCase<TRenderPipelineGraphicsSettings>
        where TRenderPipelineGraphicsSettings : class, IRenderPipelineGraphicsSettings
    {
        void SetUp(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset);

        void TearDown(UniversalRenderPipelineGlobalSettings globalSettingsAsset, UniversalRenderPipelineAsset renderPipelineAsset)
        {
        }

        bool IsMigrationCorrect(TRenderPipelineGraphicsSettings settings, out string message);
    }

    abstract class RenderPipelineGraphicsSettingsMigrationTestBase<TRenderPipelineGraphicsSettings>
        where TRenderPipelineGraphicsSettings : class, IRenderPipelineGraphicsSettings
    {
        protected UniversalRenderPipelineGlobalSettings m_Instance;
        protected UniversalRenderPipelineGlobalSettings m_OldInstance;
        protected UniversalRenderPipelineAsset m_Asset;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset rpAsset)
            {
                Assert.Ignore($"This test will be only executed when URP is the current pipeline");
                return;
            }


            m_Asset = rpAsset;
            m_OldInstance = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>() as UniversalRenderPipelineGlobalSettings;

            m_Instance = ScriptableObject.CreateInstance<UniversalRenderPipelineGlobalSettings>();
            m_Instance.name = $"{typeof(TRenderPipelineGraphicsSettings).Name}";
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(m_Instance);

            string assetPath = $"Assets/URP/MigrationTests/{m_Instance.name}.asset";
            CoreUtils.EnsureFolderTreeInAssetFilePath(assetPath);
            AssetDatabase.CreateAsset(m_Instance, assetPath);
            AssetDatabase.SaveAssets();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>(m_OldInstance);

            if (m_Instance != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_Instance);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        protected void DoTest(IRenderPipelineGraphicsSettingsTestCase<TRenderPipelineGraphicsSettings> testCase)
        {
            Assert.IsNotNull(testCase);
            testCase.SetUp(m_Instance, m_Asset);

            UniversalRenderPipelineGlobalSettings.UpgradeAsset(m_Instance.GetInstanceID());
            bool migrationIsPerformed = m_Instance.m_AssetVersion == UniversalRenderPipelineGlobalSettings.k_LastVersion;
            bool migrationIsCorrect = false;
            string errorMessage = string.Empty;
            if (migrationIsPerformed)
            {
                migrationIsCorrect = testCase.IsMigrationCorrect(
                    GraphicsSettings.GetRenderPipelineSettings<TRenderPipelineGraphicsSettings>(),
                    out errorMessage);
            }
            testCase.TearDown(m_Instance, m_Asset);

            Assert.IsTrue(migrationIsPerformed, "Unable to perform the migration");
            Assert.IsTrue(migrationIsCorrect, errorMessage);
        }
    }
}
