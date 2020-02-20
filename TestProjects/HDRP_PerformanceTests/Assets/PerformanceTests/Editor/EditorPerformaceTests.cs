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
using System.Linq;
using System.Text.RegularExpressions;
using static PerformanceTestUtils;

using Object = UnityEngine.Object;

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

        var match = new Regex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$").Match("Compiled shader 'HDRP/Lit' in 103.43s");

        using (new EditorLogWatcher(OnEditorLogWritten))
        {
            yield return BuildPlayer(testScenesAsset.GetScenePath(sceneName));
        }

        HDRPreprocessShaders.reportShaderStrippingData -= ReportShaderStrippingData;

        yield return null;
    }

    // The list of shaders we want to keep track of
    IEnumerable<Object> EnumerateWatchedShaders()
    {
        var sr = HDRenderPipeline.currentPipeline.defaultResources.shaders;

        yield return sr.defaultPS;
        yield return sr.deferredCS;
    }

    IEnumerator BuildPlayer(string scenePath)
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

        // Shader report:
        foreach (var s in EnumerateWatchedShaders())
        {
            var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(s));
            MeasureShaderSize(report, fileName);
        }

        // Remove build:
        // Directory.Delete(Path.GetFullPath(buildLocation), true);

        // Wait for the Editor.log file to be updated so we can gather infos from it.
        yield return null;
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
        // filter out shaders that we don't care about
        if (!EnumerateWatchedShaders().Contains(shader))
            return;

        SampleGroup shaderSampleGroup = new SampleGroup($"Shader Stripping {shader.name} - {data.passName}", SampleUnit.Undefined, false);
        Measure.Custom(shaderSampleGroup, currentVariantCount);
    }

    void OnEditorLogWritten(string line)
    {
        switch (line)
        {
            // Match this line in the editor log: Compiled shader 'HDRP/Lit' in 69.48s
            case var _ when MatchRegex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$", line, out var match):
                SampleGroup shaderCompilationTime = new SampleGroup($"Build Shader Compilation Time {match.Groups[1].Value}", SampleUnit.Second);
                Measure.Custom(shaderCompilationTime, double.Parse(match.Groups[2].Value));
                break;

            // Match this line in the editor log: d3d11 (total internal programs: 204, unique: 192)
            case var _ when MatchRegex(@"^(\w{1,}) \(total internal programs: (\d{1,}), unique: (\d{1,})\)$", line, out var match):
                // Note that we only take the unique internal programs count.
                Measure.Custom($"Build Shader Compilation Programs {match.Groups[1].Value}", double.Parse(match.Groups[3].Value));
                break;
        }

        bool MatchRegex(string regex, string input, out Match match)
        {
            match = new Regex(regex).Match(input);
            return match.Success;
        }
    }
}