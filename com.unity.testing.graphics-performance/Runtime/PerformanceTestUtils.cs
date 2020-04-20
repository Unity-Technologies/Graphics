using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.PerformanceTesting;
using UnityEngine.Experimental.Rendering;

public static class PerformanceTestUtils
{
    public static TestSceneAsset testScenesAsset = PerformanceTestSettings.GetTestSceneDescriptionAsset();

    public static IEnumerator LoadScene(string sceneName, RenderPipelineAsset hdAsset)
    {
        if (GraphicsSettings.renderPipelineAsset != hdAsset)
            GraphicsSettings.renderPipelineAsset = hdAsset;

        SceneManager.LoadScene(sceneName);

        // Wait one frame so the scene finish to load.
        yield return null;
    }

    public static PerformanceTestSceneSettings SetupTestScene()
    {
        var sceneSettings = GameObject.FindObjectOfType<PerformanceTestSceneSettings>();
        var camera = sceneSettings?.GetComponent<Camera>() ?? GameObject.FindObjectOfType<Camera>();

        if (sceneSettings != null)
        {
            RenderTexture tmpCameraRT = new RenderTexture(sceneSettings.cameraWidth, sceneSettings.cameraHeight, 32, (GraphicsFormat)sceneSettings.colorBufferFormat);
            camera.targetTexture = tmpCameraRT;
            sceneSettings.testCamera = camera;
        }
        else
        {
            throw new System.Exception($"No camera test settings detected in the test scene {SceneManager.GetActiveScene().name}. Failed to setup the test camera.");
        }

        return sceneSettings;
    }

    public static void CleanupTestSceneIfNeeded()
    {
        var settings = GameObject.FindObjectOfType<PerformanceTestSceneSettings>();

        if (settings == null || settings.testCamera == null)
            return;

        settings.testCamera.targetTexture = null;
        CoreUtils.Destroy(settings.testCamera.targetTexture);
    }

    // Counter example: 0001_LitCube:Small,Memory:Default,RenderTexture
    // Static analysis example: Deferred:Default,Gbuffer:OpaqueAndDecal,NA
    public static string FormatTestName(string inputData, string inputDataCategory, string settings, string settingsCategory, string testName)
        => $"{inputData}:{inputDataCategory},{settings}:{settingsCategory},{testName}";

    // Counter example: Timing,GPU,Gbuffer
    // Memory example: AllocatedBytes,Texture2D,Default
    public static string FormatSampleGroupName(string metricName, string category, string dataName = null)
        => $"{metricName},{category},{dataName ?? "Default"}";

    // Turn a string into a sample group
    public static SampleGroup ToSampleGroup(this string groupName, SampleUnit unit = SampleUnit.Undefined, bool increaseIsBetter = false)
        => new SampleGroup(groupName, unit, increaseIsBetter);
}

public struct TestName
{
    public readonly string inputData;
    public readonly string inputDataCategory;
    public readonly string settings;
    public readonly string settingsCategory;
    public readonly string name;

    public TestName(string inputData, string inputDataCategory, string settings, string settingsCategory, string name)
    {
        this.inputData = string.IsNullOrEmpty(inputData) ? "NA" : inputData;
        this.inputDataCategory = string.IsNullOrEmpty(inputDataCategory) ? "NA" : inputDataCategory;
        this.settings = string.IsNullOrEmpty(settings) ? "NA" : settings;
        this.settingsCategory = string.IsNullOrEmpty(settingsCategory) ? "NA" : settingsCategory;
        this.name = string.IsNullOrEmpty(name) ? "NA" : name;
    }

    public override string ToString()
        => PerformanceTestUtils.FormatTestName(inputData, inputDataCategory, settings, settingsCategory, name);
}
