using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Rendering;
using Unity.PerformanceTesting;
using NUnit.Framework;
using UnityEngine.TestTools;
using static PerformanceTestUtils;
using static PerformanceMetricNames;

public class HDRP_RuntimePerformanceTests : PerformanceTests
{
    const int WarmupCount = 10;
    const int MeasurementCount = 30;  // Number of frames to measure
    const int GlobalTimeout = 120 * 1000;       // 2 min
    const int minMemoryReportSize = 128 * 1024; // in bytes

    public static IEnumerable<CounterTestDescription> GetCounterTests()
    {
        foreach (var (scene, asset) in testScenesAsset.counterTestSuite.GetTestList())
            yield return new CounterTestDescription{ assetData = asset, sceneData = scene };
    }

    IEnumerable<ProfilingSampler> GetAllMarkers(HDCamera hDCamera)
    {
        yield return hDCamera.profilingSampler;
        foreach (var val in Enum.GetValues(typeof(HDProfileId)))
            yield return ProfilingSampler.Get((HDProfileId)val);
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        yield return SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        var camera = GameObject.FindObjectOfType<Camera>();
        var hdCamera = HDCamera.GetOrCreate(camera, 0); // We don't support XR for now

        yield return MeasureProfilingSamplers(GetAllMarkers(hdCamera), 20, MeasurementCount);
    }

    public static IEnumerable<MemoryTestDescription> GetMemoryTests()
    {
        foreach (var (scene, asset) in testScenesAsset.memoryTestSuite.GetTestList())
            foreach (var objectType in GetMemoryObjectTypes())
                yield return new MemoryTestDescription{ assetData = asset, sceneData = scene, assetType = objectType };
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Memory([ValueSource(nameof(GetMemoryTests))] MemoryTestDescription testDescription)
    {
        yield return ReportMemoryUsage(testDescription, minMemoryReportSize);
    }
}
