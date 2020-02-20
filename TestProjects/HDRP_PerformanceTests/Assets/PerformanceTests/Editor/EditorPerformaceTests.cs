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
using System.IO;
using System.Text.RegularExpressions;
using static PerformanceTestUtils;

public class EditorPerformaceTests
{
    const int BuildTimeout = 10 * 60 * 1000; // 10 min for each build test
    const string buildLocation = "TmpBuild";

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
        Application.logMessageReceived += ReportBuildSize;

        Debug.Log("Scenepath: " + testScenesAsset.GetScenePath(sceneName));
        BuildPlayer(testScenesAsset.GetScenePath(sceneName));

        HDRPreprocessShaders.reportShaderStrippingData -= ReportShaderStrippingData;
        Application.logMessageReceived -= ReportBuildSize;

        yield return null;
    }

    void BuildPlayer(string scenePath)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildLocation;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.Development; // TODO: remove dev build test

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        Measure.Custom("Build Total Size", summary.totalSize);
        Measure.Custom("Build Shader Size", GetAssetSizeInBuild(report, typeof(Shader)));
        Measure.Custom("Build ComputeShader Size", GetAssetSizeInBuild(report, typeof(ComputeShader)));
        MeasureShaderSize(report, "Lit");
        MeasureShaderSize(report, "Deferred");
        Measure.Custom("Build Time", summary.totalTime.TotalMilliseconds);
        Measure.Custom("Build Warnings", summary.totalWarnings);
        Measure.Custom("Build Success", summary.result == BuildResult.Succeeded ? 1 : 0);

        // Remove build:
        Directory.Delete(buildLocation, true);
    }

    ulong GetAssetSizeInBuild(BuildReport report, Type assetType)
    {
        ulong assetSize = 0;
        foreach (var packedAsset in report.packedAssets)
        {
            foreach (var content in packedAsset.contents)
                if (content.type == assetType)
                    assetSize += content.packedSize;
        }

        return assetSize;
    }

    void MeasureShaderSize(BuildReport report, string shaderFileName)
    {
        ulong assetSize = 0;
        foreach (var packedAsset in report.packedAssets)
        {
            foreach (var content in packedAsset.contents)
            {
                if (content.type != typeof(Shader) && content.type != typeof(ComputeShader))
                    continue;

                if (Path.GetFileNameWithoutExtension(content.sourceAssetPath) == shaderFileName)
                    assetSize += content.packedSize;
            }
        }

        Measure.Custom($"Build Shader {shaderFileName} Size", assetSize);
    }

    void ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount)
    {
        // filter out some shaders to avoid having too much data
        if (shader.name.Contains("Hidden"))
            return;
        
        SampleGroup shaderSampleGroup = new SampleGroup($"Shader Stripping {shader.name} - {data.passName}", SampleUnit.Undefined, false);
        Measure.Custom(shaderSampleGroup, currentVariantCount);
    }

    void ReportBuildSize(string logString, string stackTrace, LogType type)
    {
        // TODO: match this: 
        //     Compiled shader 'HDRP/Lit' in 69.48s
        // d3d11 (total internal programs: 204, unique: 192)
        // vulkan (total internal programs: 107, unique: 97)
        
        // switch (logString)
        // {
        //     case var s when MatchRegex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$", logString, out var match):
        //         foreach (var group in match.Groups)
        //             group.
        //         break;
        // }
        // var regex = new Regex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$");
        // // We match the total shader compilation time from a shader

        // bool MatchRegex(string regex, string input, out Match match)
        // {
        //     match = new Regex(regex).Match(input);
        //     return match.Success;
        // }
    }
}