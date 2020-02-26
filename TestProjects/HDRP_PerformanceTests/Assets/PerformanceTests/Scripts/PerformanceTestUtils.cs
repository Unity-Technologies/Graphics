using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.PerformanceTesting;

public static class PerformanceTestUtils
{
    public const string testSceneResourcePath = "TestScenes";
    public static TestSceneAsset testScenesAsset = Resources.Load<TestSceneAsset>(testSceneResourcePath);
    static HDRenderPipelineAsset defaultHDAsset = Resources.Load<HDRenderPipelineAsset>("defaultHDAsset");

    public static IEnumerator SetupTest(string sceneName, HDRenderPipelineAsset hdAsset)
    {
        hdAsset = hdAsset ?? defaultHDAsset;
        if (GraphicsSettings.renderPipelineAsset != hdAsset)
            GraphicsSettings.renderPipelineAsset = hdAsset;

        SceneManager.LoadScene(sceneName);

        // Wait one frame for the scene to finish loading:
        yield return null;
    }

    // Counter example: 0001_LitCube:Small,Memory:Default,RenderTexture
    // Static analysis example: Deferred:Default,Gbuffer:OpaqueAndDecal,NA
    public static string FormatTestName(string inputData, string inputDataCategory, string settings, string settingsCategory, string testName)
        => $"{inputData}:{inputDataCategory},{settings}:{settingsCategory},{testName}";

    // Counter example: Timing,GPU,Gbuffer
    // Memory example: AllocatedBytes,CPU,Default
    public static string FormatSampleGroupName(string metricName, string category, string dataName = null)
        => $"{metricName},{category},{dataName ?? "Default"}";
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
