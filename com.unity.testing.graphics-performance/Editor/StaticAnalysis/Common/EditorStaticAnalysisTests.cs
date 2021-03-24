using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEditor.ShaderAnalysis;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

public class EditorStaticAnalysisTests
{
    public struct StaticAnalysisEntry
    {
        public readonly Object asset;
        public readonly ShaderProgramFilter filter;

        public readonly TestName testName;

        public readonly string errorString;
        public readonly BuildTarget buildTarget;
        public readonly float timeout;

        public StaticAnalysisEntry(string error)
        {
            errorString = error;
            asset = null;
            filter = null;
            testName = default;
            buildTarget = 0;
            timeout = 0;
        }

        public StaticAnalysisEntry(
            Object asset,
            string assetAlias,
            string assetCategory,
            ShaderProgramFilter filter,
            string filterName,
            string filterCategory,
            string testName,
            BuildTarget buildTarget,
            float timeout
            )
        {
            errorString = null;
            this.asset = asset;
            this.filter = filter;
            this.buildTarget = buildTarget;
            this.timeout = timeout;
            this.testName = new TestName(
                string.IsNullOrEmpty(assetAlias) ? asset.ToString() : assetAlias,
                assetCategory,
                filterName,
                filterCategory,
                testName
            );
        }

        public override string ToString() =>
            string.IsNullOrEmpty(errorString)
                ? testName.ToString()
                : PerformanceTestUtils.FormatTestName(
                    errorString, "NA", "NA", "NA", "NA"
                );
    }

    const int k_MaxMeasurePerTest = 500;

    public static IEnumerable<StaticAnalysisEntry> GetStaticAnalysisEntries(BuildTarget buildTarget)
    {
        var resource = PerformanceTestSettings.GetStaticAnalysisAsset() as EditorShaderStaticAnalysisAsset;
        if (resource == null)
            yield break;

        foreach (var definition in resource.processAssetDefinitions)
        {
            // Skip when not included in this build target
            if (definition.includeInTargets != null && Array.IndexOf(definition.includeInTargets, buildTarget) == -1)
                continue;

            if (definition.asset == null || definition.asset.Equals(null))
            {
                yield return new StaticAnalysisEntry("Missing asset in definition.");
                continue;
            }

            var assetType = definition.asset.GetType();

            if (assetType != typeof(Material)
                && assetType != typeof(Shader)
                && assetType != typeof(ComputeShader))
            {
                yield return new StaticAnalysisEntry($"Invalid asset type {assetType}");
                continue;
            }

            if (!definition.filter.Resolve(resource.filters, out var filter, out var errorMessage))
            {
                yield return new StaticAnalysisEntry($"Invalid filter ({errorMessage})");
                continue;
            }

            var programFilter = ShaderProgramFilter.Parse(filter.passNameFilter, filter.keywordFilter);
            yield return new StaticAnalysisEntry(definition.asset,
                definition.assetAlias,
                definition.assetCategory,
                programFilter,
                filter.name,
                filter.category,
                definition.testName,
                buildTarget,
                resource.staticAnalysisTimeout);
        }
    }

    public static void StaticAnalysisExecute(StaticAnalysisEntry entry)
    {
        var buildReportJob = (AsyncBuildReportJob)EditorShaderTools.GenerateBuildReportAsyncGeneric(ShaderAnalysisReport.New(entry.asset, entry.buildTarget, entry.filter));
        buildReportJob.throwOnError = true;

        var time = Time.realtimeSinceStartup;
        var startTime = time;
        while (!buildReportJob.IsComplete())
        {
            if (Time.realtimeSinceStartup - time > 3)
            {
                Debug.Log($"[Build Report] {entry.asset} {buildReportJob.progress:P} {buildReportJob.message}");
                time = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - startTime > entry.timeout)
            {
                buildReportJob.Cancel();
                throw new Exception($"Timeout {entry.timeout} s");
            }

            try
            {
                if (buildReportJob.Tick())
                    buildReportJob.SetProgress(1, "Completed")  ;
                EditorUpdateManager.Tick();
            }
            catch (Exception e)
            {
                buildReportJob.Cancel();
                Debug.LogException(e);
                Assert.Fail(e.Message);
            }
        }

        var report = buildReportJob.builtReport;

        // Evaluate the number of metrics to send
        // We fail if we exceed the allowed number of metrics per test
        var numberOfMetrics = 0;
        foreach (var program in report.programs)
            numberOfMetrics += k_MesureCount * program.performanceUnits.Count();
        Assert.LessOrEqual(numberOfMetrics, k_MaxMeasurePerTest, $"We are trying to send {numberOfMetrics} metrics while the capacity is {k_MaxMeasurePerTest}." +
            $"Please reduce the number of variants to evaluate for this test.");

        foreach (var program in report.programs)
        {
            foreach (var performanceUnit in program.performanceUnits)
                SendMeasure(performanceUnit);
        }
    }

    static (
        SampleGroup vgprCount,
        SampleGroup vgprUsedCount,
        SampleGroup sgprCount,
        SampleGroup sgprUsedCount,
        SampleGroup sgprUserCount,
        SampleGroup microCodeSize,
        SampleGroup threadGroupWaves,
        SampleGroup CUOccupancyCount,
        SampleGroup CUOccupancyMax,
        SampleGroup LDSSize,
        SampleGroup SIMDOccupancyCount,
        SampleGroup SIMDOccupancyMax
    ) GetReportSampleGroups(ShaderBuildReport.PerformanceUnit unit)
    {
        var name = string.Join(";", unit.compileUnit.defines);
        var fullName = $"{unit.program.name}:{name}";
        return (
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("VGPR_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("VGPR_Used_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("SGPR_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("SGPR_Used_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("SGPR_User_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("MicroCode_Size", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("ThreadGroup_Waves", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("CU_Occupancy_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("CU_Occupancy_Max", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("LDS_Size", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("SIMD_Occupancy_Count", "GPU", fullName), SampleUnit.Undefined),
            new SampleGroup(PerformanceTestUtils.FormatSampleGroupName("SIMD_Occupancy_Max", "GPU", fullName), SampleUnit.Undefined)
        );
    }

    // Update this counter if you add a measure in the 'SendMeasure' function
    // We use this to know if we reach the measure capacity per test.
    const int k_MesureCount = 12;
    static void SendMeasure(ShaderBuildReport.PerformanceUnit unit)
    {
        var sg = GetReportSampleGroups(unit);
        Measure.Custom(sg.sgprCount, unit.parsedReport.SGPRCount);
        Measure.Custom(sg.sgprUsedCount, unit.parsedReport.SGPRUsedCount);
        Measure.Custom(sg.sgprUserCount, unit.parsedReport.UserSGPRCount);
        Measure.Custom(sg.vgprCount, unit.parsedReport.VGPRCount);
        Measure.Custom(sg.vgprUsedCount, unit.parsedReport.VGPRUsedCount);
        Measure.Custom(sg.microCodeSize, unit.parsedReport.microCodeSize);
        Measure.Custom(sg.threadGroupWaves, unit.parsedReport.threadGroupWaves);
        Measure.Custom(sg.CUOccupancyCount, unit.parsedReport.CUOccupancyCount);
        Measure.Custom(sg.CUOccupancyMax, unit.parsedReport.CUOccupancyMax);
        Measure.Custom(sg.LDSSize, unit.parsedReport.LDSSize);
        Measure.Custom(sg.SIMDOccupancyCount, unit.parsedReport.SIMDOccupancyCount);
        Measure.Custom(sg.SIMDOccupancyMax, unit.parsedReport.SIMDOccupancyMax);
    }
}
