using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Globalization;
using Unity.PerformanceTesting;
using UnityEditor.Rendering.HighDefinition;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

public class HDRPEditorPerformanceTests : EditorPerformanceTests
{
    const int k_BuildTimeout = 10 * 60 * 1000;    // 10 min for each build test
    const string k_ShaderNameFilter = "HDRP";

    // Match this line in the editor log: Compiled shader 'HDRP/Lit' in 69.48s
    static readonly Regex s_CompiledShaderRegex = new Regex(@"^\s*Compiled shader '(.*)' in (\d{1,}.\d{1,})s$");
    // Match this line in the editor log: d3d11 (total internal programs: 204, unique: 192)
    static readonly Regex s_ShaderProgramCountRegex = new Regex(@"^\s*(\w{1,}) \(total internal programs: (\d{1,}), unique: (\d{1,})\)$");

    string m_LastCompiledShader = k_NA;

    static IEnumerable<BuildTestDescription> GetBuildTests()
    {
        if (testScenesAsset == null)
            yield break;

        // testName is used to filter results in grafana, right now we already have the scene tag
        // system which is easier to use than a list of tags in code. Thus we hardcode it for now,
        // If we need more filtering one day we can use this.
        foreach (var (scene, asset) in testScenesAsset.buildTestSuite.GetTestList())
            yield return new BuildTestDescription{ assetData = asset, sceneData = scene, testName = "MainTest" };
    }

    [Timeout(k_BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Build([ValueSource(nameof(GetBuildTests))] BuildTestDescription testDescription)
    {
        HDRPreprocessShaders.shaderPreprocessed += ReportShaderStrippingData;

        using (new EditorLogWatcher(OnEditorLogWritten))
        {
            var buildReport = BuildPlayer(testScenesAsset.GetScenePath(testDescription.sceneData.scene));

            EditorPerformanceTests.ReportBuildData(buildReport);
            EditorPerformanceTests.ReportShaderSize(buildReport, k_ShaderNameFilter);
        }

        HDRPreprocessShaders.shaderPreprocessed -= ReportShaderStrippingData;

        yield return null;
    }

    static void ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount, double strippingTime)
    {
        if (!shader.name.Contains(k_ShaderNameFilter))
            return;

        Measure.Custom(FormatSampleGroupName(k_Striping, shader.name, data.passName).ToSampleGroup(), currentVariantCount);
        Measure.Custom(FormatSampleGroupName(k_StripingTime, shader.name, data.passName).ToSampleGroup(SampleUnit.Millisecond), strippingTime);
    }

    void OnEditorLogWritten(string line)
    {
        Match match = default;

        if ((match = s_CompiledShaderRegex.Match(line)).Success)
        {
            m_LastCompiledShader = match.Groups[1].Value; // store the value of the shader for internal program count report
            SampleGroup shaderCompilationTime = new SampleGroup(FormatSampleGroupName(k_CompilationTime, match.Groups[1].Value), SampleUnit.Second);
            Measure.Custom(shaderCompilationTime, double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }
        else if ((match = s_ShaderProgramCountRegex.Match(line)).Success)
        {
            // Note that we only take the unique internal programs count.
            Measure.Custom(FormatSampleGroupName(k_ShaderProgramCount, m_LastCompiledShader, match.Groups[1].Value).ToSampleGroup(), double.Parse(match.Groups[3].Value));
        }
    }
}
