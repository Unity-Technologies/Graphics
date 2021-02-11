using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using static PerformanceTestUtils;
using Unity.PerformanceTesting;

public class HDRPRuntimePerformanceTests : PerformanceTests
{
    const int WarmupCount = 20;
    const int GlobalTimeout = 120 * 1000;       // 2 min

    static IEnumerable<CounterTestDescription> GetCounterTests()
    {
        if (testScenesAsset == null)
            yield break;
        foreach (var (scene, asset) in testScenesAsset.counterTestSuite.GetTestList())
            yield return new CounterTestDescription{ assetData = asset, sceneData = scene };
    }

    static IEnumerable<ProfilingSampler> GetAllMarkers(HDCamera hDCamera)
    {
        yield return hDCamera.profilingSampler;
        foreach (var val in Enum.GetValues(typeof(HDProfileId)))
            yield return ProfilingSampler.Get((HDProfileId)val);
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        yield return LoadScene(testDescription.sceneData.scene, testDescription.assetData.asset);
        var sceneSettings = SetupTestScene();

        var hdCamera = HDCamera.GetOrCreate(sceneSettings.testCamera, 0); // We don't support XR for now

        yield return MeasureProfilingSamplers(GetAllMarkers(hdCamera), WarmupCount, sceneSettings.measurementCount);
    }

    static IEnumerable<MemoryTestDescription> GetMemoryTests()
    {
        if (testScenesAsset == null)
            yield break;
        foreach (var (scene, asset) in testScenesAsset.memoryTestSuite.GetTestList())
            foreach (var objectType in GetMemoryObjectTypes())
                yield return new MemoryTestDescription{ assetData = asset, sceneData = scene, assetType = objectType };
    }

    static readonly Vector2Int[] memoryTestResolutions = new Vector2Int[]{
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
    };

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Memory([ValueSource(nameof(GetMemoryTests))] MemoryTestDescription testDescription)
    {
        yield return LoadScene(testDescription.sceneData.scene, testDescription.assetData.asset);

        var unloadTask = Resources.UnloadUnusedAssets();
        while (!unloadTask.isDone)
            yield return new WaitForEndOfFrame();

        // We run memory tests with 3 different resolutions for texture asset types:
        if (testDescription.assetType.IsSubclassOf(typeof(Texture)))
        {
            foreach (var resolution in memoryTestResolutions)
            {
                var sceneSettings = SetupTestScene(resolution);
                yield return ReportMemoryUsage(sceneSettings, testDescription);
            }
        }
        else
        {
            var sceneSettings = SetupTestScene();
            yield return ReportMemoryUsage(sceneSettings, testDescription);
        }
    }
}
