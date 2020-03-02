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

    // Counter example: Timing_GPU_Gbuffer
    // Memory example: AllocatedBytes_CPU
    public static string FormatSampleGroupName(string metricName, string category, string dataName = null)
        => $"{metricName}_{category}_{dataName ?? "Default"}";

    // Turn a string into a sample group
    public static SampleGroup ToSampleGroup(this string groupName, SampleUnit unit = SampleUnit.Undefined, bool increaseIsBetter = false) => new SampleGroup(groupName, unit, increaseIsBetter);
}