using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEditor.Build.Reporting;
using static PerformanceTestUtils;

public class EditorPerformaceTests
{
    protected const int BuildTimeout = 10 * 60 * 1000; // 10 min for each build test

    public const string testSceneResourcePath = "TestScenes";

    static TestSceneAsset testScenesAsset = Resources.Load<TestSceneAsset>(testSceneResourcePath);
    static HDRenderPipelineAsset defaultHDAsset = Resources.Load<HDRenderPipelineAsset>("defaultHDAsset");

    static IEnumerable<string> EnumerateTestScenes(IEnumerable<TestSceneAsset.SceneData> sceneDatas)
    {
        foreach (var sceneData in sceneDatas)
            if (sceneData.enabled)
                yield return sceneData.scene;
    }

    public static IEnumerable<string> GetScenesForBuild() => EnumerateTestScenes(testScenesAsset.buildTestScenes);
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForBuild() => testScenesAsset.buildHDAssets;

    [Timeout(BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Build(
        [ValueSource(nameof(GetScenesForBuild))] string sceneName,
        [ValueSource(nameof(GetHDAssetsForBuild))] HDRenderPipelineAsset hdAsset)
    {
        SetupTest(sceneName, hdAsset);

        HDRPreprocessShaders.reportShaderStrippingData += ReportShaderStrippingData;

        BuildPlayer();

        HDRPreprocessShaders.reportShaderStrippingData -= ReportShaderStrippingData;

        yield return null;
    }

    void BuildPlayer()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scene1.unity", "Assets/Scene2.unity" };
        buildPlayerOptions.locationPathName = "iOSBuild";
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }

    void ReportShaderStrippingData(Shader shader, ShaderSnippetData data, uint currentVariantCount)
    {

    }
}