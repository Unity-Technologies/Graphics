using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class HDGlobalSettingsTests : MonoBehaviour
    {
        HDRenderPipelineGlobalSettings initialGlobalSettings;
        HDRenderPipelineGlobalSettings otherGlobalSettings;


        [SetUp]
        public void SetUp()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            otherGlobalSettings = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            ScriptableObject.DestroyImmediate(otherGlobalSettings);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        void EnsureHDRPIsActivePipeline()
        {
            Camera.main.Render();

            // Skip test if project is not configured to be SRP project
            if (RenderPipelineManager.currentPipeline == null)
                Assert.Ignore("Test project has no SRP configured, skipping test");

            initialGlobalSettings = HDRenderPipelineGlobalSettings.instance;
            Assert.IsInstanceOf<HDRenderPipeline>(RenderPipelineManager.currentPipeline);
            Assert.IsNotNull(initialGlobalSettings);
        }

        [Test]
        public void HDRPFrameRendered_GlobalSettingsShouldBeAssigned()
        {
            EnsureHDRPIsActivePipeline();
        }

        [Test]
        public void HDRPFrameRendered_EnsureGlobalSettingsIfNullAssigned()
        {
            EnsureHDRPIsActivePipeline();

            HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(null);
            Assert.IsNull(HDRenderPipelineGlobalSettings.instance);

            Camera.main.Render();
            Assert.IsNotNull(HDRenderPipelineGlobalSettings.instance);
        }

        [Test]
        [Description("Case 1342987 - Support undo on Global Settings assignation ")]
        public void Undo_HDRPActive_ChangeGlobalSettings()
        {
            EnsureHDRPIsActivePipeline();
            Undo.IncrementCurrentGroup();
            HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(otherGlobalSettings);
            Assert.AreEqual(otherGlobalSettings, HDRenderPipelineGlobalSettings.instance);

            Undo.PerformUndo();
            Assert.AreEqual(initialGlobalSettings, HDRenderPipelineGlobalSettings.instance);
        }

        [Test]
        [Description("Case 1342987 - Support undo on Global Settings assignation ")]
        public void Undo_HDRPActive_UnregisterGlobalSettings()
        {
            EnsureHDRPIsActivePipeline();
            Undo.IncrementCurrentGroup();
            HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(null);
            Assert.IsNull(HDRenderPipelineGlobalSettings.instance);

            Undo.PerformUndo();
            Assert.AreEqual(initialGlobalSettings, HDRenderPipelineGlobalSettings.instance);
        }
    }
}
