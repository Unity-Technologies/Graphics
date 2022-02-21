using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

class UniversalGlobalSettingsTests
{
    UniversalRenderPipelineGlobalSettings initialGlobalSettings;
    UniversalRenderPipelineGlobalSettings otherGlobalSettings;

    [SetUp]
    public void SetUp()
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
        otherGlobalSettings = ScriptableObject.CreateInstance<UniversalRenderPipelineGlobalSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        ScriptableObject.DestroyImmediate(otherGlobalSettings);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    void EnsureUniversalRPIsActivePipeline()
    {
        Camera.main.Render();

        // Skip test if project is not configured to be SRP project
        if (RenderPipelineManager.currentPipeline == null)
            Assert.Ignore("Test project has no SRP configured, skipping test");

        initialGlobalSettings = UniversalRenderPipelineGlobalSettings.instance;
        Assert.IsInstanceOf<UniversalRenderPipeline>(RenderPipelineManager.currentPipeline);
        Assert.IsNotNull(initialGlobalSettings);
    }

    [Test]
    public void URPFrameRendered_GlobalSettingsShouldBeAssigned()
    {
        EnsureUniversalRPIsActivePipeline();
    }

    [Test]
    public void URPFrameRendered_EnsureGlobalSettingsIfNullAssigned()
    {
        EnsureUniversalRPIsActivePipeline();

        UniversalRenderPipelineGlobalSettings.UpdateGraphicsSettings(null);
        Assert.IsNull(UniversalRenderPipelineGlobalSettings.instance);

        Camera.main.Render();
        Assert.IsNotNull(UniversalRenderPipelineGlobalSettings.instance);
    }

    [Test]
    [Description("Case 1342987 - Support undo on Global Settings assignation ")]
    public void Undo_URPActive_ChangeGlobalSettings()
    {
        EnsureUniversalRPIsActivePipeline();
        Undo.IncrementCurrentGroup();
        UniversalRenderPipelineGlobalSettings.UpdateGraphicsSettings(otherGlobalSettings);
        Assert.AreEqual(otherGlobalSettings, UniversalRenderPipelineGlobalSettings.instance);

        Undo.PerformUndo();
        Assert.AreEqual(initialGlobalSettings, UniversalRenderPipelineGlobalSettings.instance);
    }

    [Test]
    [Description("Case 1342987 - Support undo on Global Settings assignation ")]
    public void Undo_UPActive_UnregisterGlobalSettings()
    {
        EnsureUniversalRPIsActivePipeline();
        Undo.IncrementCurrentGroup();
        UniversalRenderPipelineGlobalSettings.UpdateGraphicsSettings(null);
        Assert.IsNull(UniversalRenderPipelineGlobalSettings.instance);

        Undo.PerformUndo();
        Assert.AreEqual(initialGlobalSettings, UniversalRenderPipelineGlobalSettings.instance);
    }
}
