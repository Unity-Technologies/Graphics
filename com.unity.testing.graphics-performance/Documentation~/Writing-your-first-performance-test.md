## Writing your first performance test
The following test captures frame timings at runtime. You can use this as a foundation for your first test:

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using static PerformanceTestUtils;
// If you are running a test in the Editor, inherit from EditorPerformanceTests instead of PerformanceTests.
public class MyRuntimePerformanceTests : PerformanceTests
{
    // Determine the number of frames before Unity starts recording the profiling samplers.
    const int WarmupCount = 20;
    // The number of frames Unity measures.
    const int MeasurementCount = 30;
    // The timeout of a test in milliseconds.
    const int GlobalTimeout = 120 * 1000; // 2 min
    // The IEnumerable function helps generate the tests that appear in the Test Runner window.
    // The testScenesAsset refers to the Performance Test Description asset you referenced in Project Settings > Performance Tests.
    static IEnumerable<CounterTestDescription> GetCounterTests()
    {
        if (testScenesAsset == null)
            yield break;
        foreach (var (scene, asset) in testScenesAsset.counterTestSuite.GetTestList())
            yield return new CounterTestDescription{ assetData = asset, sceneData = scene };
    }
    // Returns the list of markers you want to profile.
    static IEnumerable<ProfilingSampler> GetAllMarkers()
    {
        foreach (var val in Enum.GetValues(typeof(HDProfileId)))
            yield return ProfilingSampler.Get((HDProfileId)val);
    }
    // This is the test function. Note the Performance attribute.
    // The ValueSource attribute helps generate the tests that appear in the Test Runner window.
    // The name of the test is a ToString of the CounterTestDescription
    // struct that GetCounterTests() returns. If you want to change the test
    // name structure, create a new struct and function to iterate
    // over your test list.
    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        // The LoadScene function loads the scene and assigns an SRP asset in the parameter.
        LoadScene(testDescription.sceneData.scene, testDescription.assetData.asset);
        // The SetupTestScene function sets up the camera based on the settings you have in your PerformanceTestSettings MonoBehavior.
        // It also returns the PerformanceTestSettings MonoBehavior so you can set up anything else you need for your test.
        var sceneSettings = SetupTestScene();
        //The FindObjectOfType function loads GameObjects from the scene. This line prepares the camera for setting up the camera rendering resolution.
        var camera = GameObject.FindObjectOfType<Camera>();
        // Calls a utility function to measure all markers and sends the data using the following format: Timing,CPU,sampler.name.
        yield return MeasureProfilingSamplers(GetAllMarkers(), WarmupCount, MeasurementCount);
    }
}
```

<a name="running-and-analyzing-the-result"></a>
## Running and analyzing the result
Once Unity has compiled your tests, you can view them and run them locally in the Test Runner window. Once you have run a test, go to **Window > Analysis > Performance Test Report** to open the **Test Report** window and view your test results.

If you have captured timings over multiple frames, you can view the data in the **Test Report** window to debug the results.

If you have gathered the timing over 30 frames, a test might not report the detail of every frame. This is because Unity can only send the following details to the database due to data weight constraints:

- Min
- Max
- Median
- Average
- Standard derivation
- Percentile
- Sum

If you are running multiple tests, you can use Yamato to automate them (see [Using Yamato to automate your tests](Using-Yamato-to-automate-your-tests.md)). To visualize the data Unity gathers from your tests, you can use Grafana (see [Using Grafana to view a performance test report](Using-Grafana-to-view-a-performance-test-report.md)).