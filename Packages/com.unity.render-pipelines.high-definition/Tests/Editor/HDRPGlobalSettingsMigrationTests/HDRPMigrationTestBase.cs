using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Test
{
    interface IRenderPipelineGraphicsSettingsTestCase<TRenderPipelineGraphicsSettings>
        where TRenderPipelineGraphicsSettings : class, IRenderPipelineGraphicsSettings
    {
        void SetUp(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset);

        void TearDown(HDRenderPipelineGlobalSettings globalSettingsAsset, HDRenderPipelineAsset renderPipelineAsset)
        {
        }

        bool IsMigrationCorrect(TRenderPipelineGraphicsSettings settings, out string message);
    }

    abstract class RenderPipelineGraphicsSettingsMigrationTestBase<TRenderPipelineGraphicsSettings>
        where TRenderPipelineGraphicsSettings : class, IRenderPipelineGraphicsSettings
    {
        protected HDRenderPipelineGlobalSettings m_Instance;
        protected HDRenderPipelineGlobalSettings m_OldInstance;
        protected HDRenderPipelineAsset m_Asset;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset rpAsset)
            {
                var pipelineName = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in Pipeline";
                Assert.Ignore($"This test will be only executed when HDRP is the current pipeline");
                return;
            }


            m_Asset = rpAsset;
            m_OldInstance = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>() as HDRenderPipelineGlobalSettings;

            m_Instance = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
            m_Instance.name = $"{typeof(TRenderPipelineGraphicsSettings).Name}";
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(m_Instance);

            string assetPath = $"Assets/HDRP/MigrationTests/{m_Instance.name}.asset";
            CoreUtils.EnsureFolderTreeInAssetFilePath(assetPath);
            AssetDatabase.CreateAsset(m_Instance, assetPath);
            AssetDatabase.SaveAssets();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(m_OldInstance);

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

            bool migrationIsPerformed = m_Instance.Migrate();
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
