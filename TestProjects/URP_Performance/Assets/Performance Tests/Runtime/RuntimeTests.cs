using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using static PerformanceTestUtils;

public class RuntimeTests : PerformanceTests
{
    // Number of frames before we start recording the profiling samplers
    const int WarmupCount = 20;
    // Number of frames we measure
    const int MeasurementCount = 30;
    // Timeout of a test in milliseconds
    const int GlobalTimeout = 120 * 1000; // 2 min

    // This function will help to generate the test you see in the test runner window.
    // The testScenesAsset is a reference to the Test Asset Description you
    // referenced in the performance settings tab.
    static IEnumerable<CounterTestDescription> GetCounterTests()
    {
        if (testScenesAsset == null)
            yield break;
        foreach (var (scene, asset) in testScenesAsset.counterTestSuite.GetTestList())
            yield return new CounterTestDescription{ assetData = asset, sceneData = scene };
    }

    // return the list of all markers we want to profile
    static IEnumerable<ProfilingSampler> GetAllMarkers()
    {
        foreach (var val in Enum.GetValues(typeof(HDProfileId)))
            yield return ProfilingSampler.Get((HDProfileId)val);
    }

    // This is the actual test function, note the Performance attribute here.
    // The ValueSource attribute is used to generate the tests we see in the test
    // runner, the name of the test is a ToString of the `CounterTestDescription`
    // struct returned by GetCounterTests(). If you want to change the test
    // name structure, you'll have to create a new struct and function to
    // iterate over your test list.
    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        // This function will load the scene and assign the SRP asset in parameter.
        LoadScene(testDescription.sceneData.scene, testDescription.assetData.asset);
        // This function setup the camera based on the settings you have in your PerformanceTestSettings MonoBehavior.
        // It also returns the PerformanceTestSettings MonoBehavior so you can setup additional things.
        var sceneSettings = SetupTestScene();

        // Here you load objects from the scene like the camera if you want to setup the camera rendering resolution for example.
        var camera = GameObject.FindObjectOfType<Camera>();

        // Then we call an utility function that will measure all the markers we want
        // And send the data using this format: `Timing,CPU,sampler.name`
        yield return MeasureProfilingSamplers(GetAllMarkers(), WarmupCount, MeasurementCount);
    }
}