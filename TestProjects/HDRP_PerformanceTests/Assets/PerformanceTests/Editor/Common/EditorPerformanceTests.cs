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
using UnityEngine.Rendering;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

using Object = UnityEngine.Object;

public class EditorPerformaceTests
{
    const int BuildTimeout = 10 * 60 * 1000; // 10 min for each build test
    const string buildLocation = "TmpBuild";

    public const string testSceneResourcePath = "TestScenes";

    static TestSceneAsset testScenesAsset = Resources.Load<TestSceneAsset>(testSceneResourcePath);
    static HDRenderPipelineAsset defaultHDAsset = Resources.Load<HDRenderPipelineAsset>("defaultHDAsset");

    public static IEnumerable<BuildTestDescription> GetBuildTests()
    {
        // testName is hardcoded for now
        foreach (var (scene, asset) in testScenesAsset.buildTestSuite.GetTestList())
            yield return new BuildTestDescription{ assetData = asset, sceneData = scene, testName = "MainTest" };
    }

    public struct BuildTestDescription
    {
        public TestSceneAsset.SceneData     sceneData;
        public TestSceneAsset.HDAssetData   assetData;
        public string                       testName;

        public override string ToString()
            => PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels, String.IsNullOrEmpty(assetData.alias) ? assetData.asset.name : assetData.alias, assetData.assetLabels, testName);
    }

    [Timeout(BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Build([ValueSource(nameof(GetBuildTests))] BuildTestDescription testDescription)
    {
        SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        HDRPreprocessShaders.reportShaderStrippingData += ReportShaderStrippingData;

        var match = new Regex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$").Match("Compiled shader 'HDRP/Lit' in 103.43s");

        using (new EditorLogWatcher(OnEditorLogWritten))
        {
            yield return BuildPlayer(testScenesAsset.GetScenePath(testDescription.sceneData.scene));
        }

        HDRPreprocessShaders.reportShaderStrippingData -= ReportShaderStrippingData;

        yield return null;
    }

    // The list of shaders we want to keep track of
    IEnumerable<Object> EnumerateWatchedShaders()
    {
        var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp == null)
            yield break;

        var sr = hdrp.defaultResources.shaders;

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

        // Make sure we compile the shaders when we build:
        ClearShaderCache();

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        Measure.Custom(FormatSampleGroupName(kSize, kTotal), summary.totalSize);
        Measure.Custom(FormatSampleGroupName(kSize, kShader), GetAssetSizeInBuild(report, typeof(Shader)));
        Measure.Custom(FormatSampleGroupName(kSize, kComputeShader), GetAssetSizeInBuild(report, typeof(ComputeShader)));
        Measure.Custom(FormatSampleGroupName(kTime, kTotal), summary.totalTime.TotalMilliseconds);
        Measure.Custom(FormatSampleGroupName(kBuild, kWarnings), summary.totalWarnings);
        Measure.Custom(FormatSampleGroupName(kBuild, kSuccess), summary.result == BuildResult.Succeeded ? 1 : 0);

        // Shader report:
        foreach (var s in EnumerateWatchedShaders())
        {
            var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(s));
            MeasureShaderSize(report, fileName);
        }

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

        Measure.Custom(FormatSampleGroupName(kSize, kShader, shaderFileName), assetSize);
    }

    void ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount, double strippingTime)
    {
        // filter out shaders that we don't care about
        if (!EnumerateWatchedShaders().Contains(shader))
            return;

        SampleGroup strippingPassCount = new SampleGroup(FormatSampleGroupName(kStriping, shader.name, data.passName), SampleUnit.Undefined, false);
        SampleGroup strippingTimeSample = new SampleGroup(FormatSampleGroupName(kStripingTime, shader.name, data.passName), SampleUnit.Millisecond, false);
        Measure.Custom(strippingPassCount, currentVariantCount);
        Measure.Custom(strippingTimeSample, strippingTime);
    }

    static string lastCompiledShader = kNA;
    void OnEditorLogWritten(string line)
    {
        switch (line)
        {
            // Match this line in the editor log: Compiled shader 'HDRP/Lit' in 69.48s
            case var _ when MatchRegex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$", line, out var match):
                lastCompiledShader = match.Groups[1].Value; // store the value of the shader for internal program count report
                SampleGroup shaderCompilationTime = new SampleGroup(FormatSampleGroupName(kCompilationTime, match.Groups[1].Value), SampleUnit.Second);
                Measure.Custom(shaderCompilationTime, double.Parse(match.Groups[2].Value));
                break;

            // Match this line in the editor log: d3d11 (total internal programs: 204, unique: 192)
            case var _ when MatchRegex(@"^(\w{1,}) \(total internal programs: (\d{1,}), unique: (\d{1,})\)$", line, out var match):
                // Note that we only take the unique internal programs count.
                Measure.Custom(FormatSampleGroupName(kShaderProgramCount, lastCompiledShader, match.Groups[1].Value), double.Parse(match.Groups[3].Value));
                break;
        }

        bool MatchRegex(string regex, string input, out Match match)
        {
            match = new Regex(regex).Match(input);
            return match.Success;
        }
    }

    void ClearShaderCache()
    {
        // Didn't found any public / internal C# API to clear the shader cache so ...
        try {
            Directory.Delete("Library/ShaderCache", true);
        } catch {}
    }
}