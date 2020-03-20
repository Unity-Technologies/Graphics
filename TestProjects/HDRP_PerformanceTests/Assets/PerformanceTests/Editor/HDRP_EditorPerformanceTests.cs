using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using NUnit.Framework;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
using System.Text.RegularExpressions;
using System.Globalization;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

public class HDRP_EditorPerformanceTests : EditorPerformanceTests
{
    const int BuildTimeout = 10 * 60 * 1000;    // 10 min for each build test
    const string shaderNameFilter = "HDRP";
    
    public static IEnumerable<BuildTestDescription> GetBuildTests()
    {
        // testName is hardcoded for now
        foreach (var (scene, asset) in testScenesAsset.buildTestSuite.GetTestList())
            yield return new BuildTestDescription{ assetData = asset, sceneData = scene, testName = "MainTest" };
    }

    [Timeout(BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Build([ValueSource(nameof(GetBuildTests))] BuildTestDescription testDescription)
    {
        SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        HDRPreprocessShaders.reportShaderStrippingData += ReportShaderStrippingData;

        var match = new Regex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$").Match("Compiled shader 'HDRP/Lit' in 103.43s");

        using (new EditorLogWatcher(OnEditorLogWritten))
        {
            var buildReport = BuildPlayer(testScenesAsset.GetScenePath(testDescription.sceneData.scene));

            ReportBuildData(buildReport);
            ReportShaderSize(buildReport, shaderNameFilter);
        }

        HDRPreprocessShaders.reportShaderStrippingData -= ReportShaderStrippingData;

        yield return null;
    }
    
    void ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount, double strippingTime)
    {
        if (!shader.name.Contains(shaderNameFilter))
            return;

        Measure.Custom(FormatSampleGroupName(kStriping, shader.name, data.passName).ToSampleGroup(), currentVariantCount);
        Measure.Custom(FormatSampleGroupName(kStripingTime, shader.name, data.passName).ToSampleGroup(SampleUnit.Millisecond), strippingTime);
    }

    string lastCompiledShader = kNA;
    void OnEditorLogWritten(string line)
    {
        switch (line)
        {
            // Match this line in the editor log: Compiled shader 'HDRP/Lit' in 69.48s
            case var _ when MatchRegex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$", line, out var match):
                lastCompiledShader = match.Groups[1].Value; // store the value of the shader for internal program count report
                SampleGroup shaderCompilationTime = new SampleGroup(FormatSampleGroupName(kCompilationTime, match.Groups[1].Value), SampleUnit.Second);
                Measure.Custom(shaderCompilationTime, double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
                break;

            // Match this line in the editor log: d3d11 (total internal programs: 204, unique: 192)
            case var _ when MatchRegex(@"^\s*(\w{1,}) \(total internal programs: (\d{1,}), unique: (\d{1,})\)$", line, out var match):
                // Note that we only take the unique internal programs count.
                Measure.Custom(FormatSampleGroupName(kShaderProgramCount, lastCompiledShader, match.Groups[1].Value).ToSampleGroup(), double.Parse(match.Groups[3].Value));
                break;
        }

        bool MatchRegex(string regex, string input, out Match match)
        {
            match = new Regex(regex).Match(input);
            return match.Success;
        }
    }
}
