#pragma warning disable 618 // LightingSettings.lightmapper is deprecated; tests configure the lightmapper backend directly.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class UniversalProjectEditorTests
{
    private static UniversalRenderPipelineAsset currentAsset;

    [Test]
    public void GetCurrentAsset()
    {
        GetUniversalAsset();
    }

    [Test]
    public void CheckAllLightingSettings()
    {
        var guids = AssetDatabase.FindAssets("t:LightingSettings");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LightingSettings lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (lightingSettings.bakedGI)
            {
                Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU,
                    $"Lighting settings ({path}) must use the Progressive GPU lightmapper, but uses {lightingSettings.lightmapper}.");
                Assert.IsTrue(lightingSettings.filteringMode == LightingSettings.FilterMode.None,
                    $"Lighting settings ({path}) have baked GI with filter mode enabled. It is recommended to turn off filter mode to reduce halo effect (If you still want to use it please contact URP team first).");
            }
        }
    }

    const string k_ScenesFolder = "Assets/Scenes";

    [Test]
    public void CheckAllScenesPinAQualityLevelWithTheirRendererIndex()
    {
        var violations = new List<string>();

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { k_ScenesFolder }))
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                CollectRendererSelectionViolations(scenePath, scene, violations);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        Assert.IsEmpty(violations, string.Join("\n", violations));
    }

    static void CollectRendererSelectionViolations(string scenePath, Scene scene, List<string> violations)
    {
        var qualityLevelSetters = new List<SetQualityLevelOnAwake>();
        var cameraDatas = new List<UniversalAdditionalCameraData>();
        foreach (var root in scene.GetRootGameObjects())
        {
            qualityLevelSetters.AddRange(root.GetComponentsInChildren<SetQualityLevelOnAwake>(true));
            cameraDatas.AddRange(root.GetComponentsInChildren<UniversalAdditionalCameraData>(true));
        }

        if (qualityLevelSetters.Count > 1)
        {
            violations.Add($"{scenePath}: {qualityLevelSetters.Count} SetQualityLevelOnAwake components, so the quality level " +
                "the scene settles on, and the level it restores when it unloads, are both undefined.");
            return;
        }

        var qualityLevelSetter = qualityLevelSetters.Count == 1 ? qualityLevelSetters[0] : null;

        foreach (var cameraData in cameraDatas)
        {
            using var serializedCameraData = new SerializedObject(cameraData);
            int rendererIndex = serializedCameraData.FindProperty("m_RendererIndex").intValue;
            if (rendererIndex < 0)
                continue;

            if (qualityLevelSetter == null)
            {
                violations.Add($"{scenePath}: camera '{cameraData.name}' selects renderer index {rendererIndex}, but the scene " +
                    "has no SetQualityLevelOnAwake, so it renders with whichever quality level the previous test left active.");
                continue;
            }

            int qualityLevel = qualityLevelSetter.qualityLevelIndex;
            var asset = QualitySettings.GetRenderPipelineAssetAt(qualityLevel) as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                violations.Add($"{scenePath}: quality level {qualityLevel} has no Universal Render Pipeline asset.");
                continue;
            }

            if (rendererIndex >= asset.rendererDataList.Length || asset.rendererDataList[rendererIndex] == null)
            {
                violations.Add($"{scenePath}: camera '{cameraData.name}' selects renderer index {rendererIndex}, " +
                    $"which '{asset.name}' (quality level {qualityLevel}) does not have.");
            }
        }
    }

    //[Test]
    public void GetDefaultRenderer()
    {
        GetUniversalAsset();

        Assert.IsNotNull(currentAsset.scriptableRenderer, "Current ScriptableRenderer is null.");
    }

    //Utilities
    void GetUniversalAsset()
    {
        var renderpipelineAsset = GraphicsSettings.currentRenderPipeline;

        if (renderpipelineAsset == null)
            Assert.Fail("No Render Pipeline Asset assigned.");

        if (renderpipelineAsset.GetType() == typeof(UniversalRenderPipelineAsset))
        {
            currentAsset = renderpipelineAsset as UniversalRenderPipelineAsset;
        }
        else
        {
            Assert.Inconclusive("Project not setup for Universal RP.");
        }
    }
}
