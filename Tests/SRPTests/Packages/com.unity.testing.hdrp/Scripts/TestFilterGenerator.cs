#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TestTools.Graphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

class TestFilterGenerator
{
    [MenuItem("Tests/UpdateTestCaseFilter", priority = 5)]
    static void UpdateTestFilter()
    {
        string[] guids1 = AssetDatabase.FindAssets("t:TestFilters", null);
        if (guids1.Length == 0)
        {
            Debug.LogError("TestFilters object not found in project");
            return;
        }

        var filters = new List<TestFilterConfig>();

        Scene activeScene = SceneManager.GetActiveScene();
        string activeScenePath = null;
        if (activeScene != null)
            activeScenePath = activeScene.path;

        foreach (EditorBuildSettingsScene editorBuildSettingsScene in EditorBuildSettings.scenes)
        {
            //Debug.Log("Found Scene " + editorBuildSettingsScene.path);
            EditorSceneManager.OpenScene(editorBuildSettingsScene.path);

            GameObject obj = GameObject.Find("BrokenTestText");

            if (obj)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(editorBuildSettingsScene.path);

                var displayOnPlatformAPI = obj.GetComponent<DisplayOnPlatformAPI>();

                if (displayOnPlatformAPI.D3D11)
                {
                    TestFilterConfig config = new TestFilterConfig();
                    config.FilteredScene = sceneAsset;
                    config.Reason = "Not Working";
                    config.ColorSpace = ColorSpace.Linear;
                    config.BuildPlatform = BuildTarget.NoTarget;
                    config.GraphicsDevice = GraphicsDeviceType.Direct3D11;
                    filters.Add(config);
                }

                if (displayOnPlatformAPI.D3D12)
                {
                    TestFilterConfig config = new TestFilterConfig();
                    config.FilteredScene = sceneAsset;
                    config.Reason = "Not Working";
                    config.ColorSpace = ColorSpace.Linear;
                    config.BuildPlatform = BuildTarget.NoTarget;
                    config.GraphicsDevice = GraphicsDeviceType.Direct3D12;
                    filters.Add(config);
                }

                if (displayOnPlatformAPI.VukanWindows)
                {
                    TestFilterConfig config = new TestFilterConfig();
                    config.FilteredScene = sceneAsset;
                    config.Reason = "Not Working";
                    config.ColorSpace = ColorSpace.Linear;
                    config.BuildPlatform = BuildTarget.NoTarget;
                    config.GraphicsDevice = GraphicsDeviceType.Vulkan;
                    filters.Add(config);
                }

                if (displayOnPlatformAPI.Metal)
                {
                    TestFilterConfig config = new TestFilterConfig();
                    config.FilteredScene = sceneAsset;
                    config.Reason = "Not Working";
                    config.ColorSpace = ColorSpace.Linear;
                    config.BuildPlatform = BuildTarget.NoTarget;
                    config.GraphicsDevice = GraphicsDeviceType.Metal;
                    filters.Add(config);
                }
            }
            else
            {
                Debug.LogError("BrokenTestText not found in scene " + editorBuildSettingsScene.path);
            }
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guids1[0]);
        TestFilters testFilters = (TestFilters)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TestFilters));

        // Sort by platform
        filters.Sort(
            (a, b) => (a.GraphicsDevice != b.GraphicsDevice) ? a.GraphicsDevice.CompareTo(b.GraphicsDevice) : a.FilteredScene.name.CompareTo(b.FilteredScene.name));

        testFilters.filters = filters.ToArray();
        EditorUtility.SetDirty(testFilters);

        if (activeScenePath != null)
            EditorSceneManager.OpenScene(activeScenePath);
    }
}

#endif
