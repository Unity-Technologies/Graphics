using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using static PerformanceTestUtils;
using Unity.PerformanceTesting;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXRuntimePerformanceTests : PerformanceTests
    {
        const int WarmupCount = 20;
        const int GlobalTimeout = 120 * 1000;

        static IEnumerable<MemoryTestDescription> GetMemoryTests()
        {
            if (testScenesAsset == null)
                yield break;
            foreach (var (scene, asset) in testScenesAsset.memoryTestSuite.GetTestList())
                foreach (var objectType in GetMemoryObjectTypes())
                    yield return new MemoryTestDescription { assetData = asset, sceneData = scene, assetType = objectType };
        }

        [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
        public IEnumerator DUMMY_456([ValueSource(nameof(GetMemoryTests))] MemoryTestDescription testDescription)
        {
            yield return ReportMemoryUsage(testDescription);
        }
    }
}
