using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;
using UnityEditor.Rendering;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

using Object = UnityEngine.Object;

public class EditorPerformanceTests
{
    const int BuildTimeout = 10 * 60 * 1000; // 10 min for each build test
    const string buildLocation = "TmpBuild";

    // TODO
    public const string testSceneResourcePath = "TestScenes";
    static TestSceneAsset testScenesAsset = Resources.Load<TestSceneAsset>(testSceneResourcePath);

    protected BuildReport BuildPlayer(string scenePath)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = buildLocation;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.Development; // TODO: remove dev build test

        // Make sure we compile the shaders when we build:
        ClearShaderCache();

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Wait for the Editor.log file to be updated so we can gather infos from it.
        return report;
    }

    protected void ReportBuildData(BuildReport report)
    {
        BuildSummary summary = report.summary;
        Measure.Custom(FormatSampleGroupName(kSize, kTotal).ToSampleGroup(SampleUnit.Byte), summary.totalSize);
        Measure.Custom(FormatSampleGroupName(kSize, kShader).ToSampleGroup(SampleUnit.Byte), GetAssetSizeInBuild(report, typeof(Shader)));
        Measure.Custom(FormatSampleGroupName(kSize, kComputeShader).ToSampleGroup(SampleUnit.Byte), GetAssetSizeInBuild(report, typeof(ComputeShader)));
        Measure.Custom(FormatSampleGroupName(kTime, kTotal).ToSampleGroup(SampleUnit.Millisecond), summary.totalTime.TotalMilliseconds);
        Measure.Custom(FormatSampleGroupName(kBuild, kWarnings).ToSampleGroup(), summary.totalWarnings);
        Measure.Custom(FormatSampleGroupName(kBuild, kSuccess).ToSampleGroup(), summary.result == BuildResult.Succeeded ? 1 : 0);
    }

    protected ulong GetAssetSizeInBuild(BuildReport report, Type assetType)
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

    protected void ReportShaderSize(BuildReport report, string shaderNameFilter)
    {
        foreach (var packedAsset in report.packedAssets)
        {
            foreach (var content in packedAsset.contents)
            {
                if (content.type == typeof(ComputeShader))
                {
                    var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(content.sourceAssetPath);
                    Measure.Custom(FormatSampleGroupName(kSize, kComputeShader, computeShader.name).ToSampleGroup(SampleUnit.Byte), content.packedSize);
                }
                else if (content.type == typeof(Shader))
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(content.sourceAssetPath);

                    if (shader?.name?.Contains(shaderNameFilter) ?? false)
                        Measure.Custom(FormatSampleGroupName(kSize, kShader, shader.name).ToSampleGroup(SampleUnit.Byte), content.packedSize);
                }
            }
        }
    }

    protected void ClearShaderCache()
    {
        // Didn't found any public / internal C# API to clear the shader cache so ...
        try {
            Directory.Delete("Library/ShaderCache", true);
        } catch {}
    }
}
