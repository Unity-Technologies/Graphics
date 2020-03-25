# Graphics Performance Test Framework

TODO

## How to install ?

This package is meant to be used by test projects inside the SRP repository, so to include it just add this line in the manifest of your test project (which should is in the TestProjects/ folder):
```
"com.unity.testing.graphics-performance": "file:../../../com.unity.testing.graphics-performance"
```

<a name="GettingStarted"></a>
## Getting Started


### Assembly Definition Setup
After installing the graphic performance test package in your project, you'll need to setup some tests. The first thing to do is to create a test directory, generally you'll want to have both Runtime and Editor performance tests so we recommend to use a file hierarchy like this:
```
Assets/
+-- Performance Tests/
|   +-- Editor/
|   |
|   +-- Runtime/
+-- Resources/
+-- Scenes
```

To create the Editor and Runtime test folders you can use the **Create > Testing > Test Assembly Folder** menu, it will automatically create a folder and configure an assembly definition file in test mode.

Now in the assembly definition file, we need to add all the references we will use to write our tests. A fast way to to it is to paste this in your asmdef file.  
Runtime:
```
    "references": [
        "GUID:91836b14885b8a34196f4aa8303d7793",
        "GUID:27619889b8ba8c24980f49ee34dbb44a",
        "GUID:0acc523941302664db1f4e527237feb3",
        "GUID:df380645f10b7bc4b97d4f5eb6303d95",
        "GUID:295068ed467c2ce4a9cda3833065f650"
    ],
```
Editor:
```
    "references": [
        "GUID:91836b14885b8a34196f4aa8303d7793",
        "GUID:df380645f10b7bc4b97d4f5eb6303d95",
        "GUID:295068ed467c2ce4a9cda3833065f650",
        "GUID:27619889b8ba8c24980f49ee34dbb44a",
        "GUID:cbbcbe5a7206638449ebcb9382eeb3a8",
        "GUID:78bd2ddd6e276394a9615c203e574844"
    ],
```

Otherwise you can add the assembly definition files from the UI in Unity, you'll need:
- Unity.PerformanceTesting 
- Unity.GraphicTests.Performance.Runtime
- Unity.RenderPipelines.Core.Runtime

And for Editor, all above plus
- Unity.GraphicTests.Performance.Editor

### Setting up the Test Assets

Nothing complicated here, start by creating both the **Performance Test Description** and **Static Analysis Tests** assets which are in the **Assets > Create > Testing** menu. Note that they need to be created inside a Resources folder so they can be loaded at runtime (thus the Resources folder in the hierarchy above).

Then, to say that you'll use these assets go to **Project Settings > Performance Tests** and reference both assets you created.

For your first runtime test, you'll need at least one Scene and one Render Pipeline Asset. You can go ahead and create a simple scene with a camera that will be used during the tests.

Last step: register the scene in the test description asset, doing so will generate a list of tests in the test runner window. Note that you also need to reference which SRP asset will be used to render the scene. You can have more details about the configuration of the [Test Description Asset in his documentation page](test-description-asset.md)

### Writing your first performance test

To go a bit faster, you can start from this C# template to create your tests:

```CSharp
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

public class MyRuntimePerformanceTests : PerformanceTests
{
    const int WarmupCount = 20;
    const int MeasurementCount = 30;  // Number of frames to measure
    const int GlobalTimeout = 120 * 1000;       // 2 min

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

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        // This function will load the scene and assign the SRP asset in parameter.
        yield return SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        var camera = GameObject.FindObjectOfType<Camera>();
        // Here you can setup things based on the camera setup.

        yield return MeasureProfilingSamplers(GetAllMarkers(), WarmupCount, MeasurementCount);
    }
}
```

TODO: explain the code and clean it

This C# script allow you to gather the timings from the ProfilingSampler you have in your frame.

### Running and analysing the result

Now that your test script is done, they should appear in the test runner window.  
You can run the test locally and analyse the results under **Window > Analysis > Performance Test Report**. This window will allow you to show all the data that have been gathered during the test, it's useful to debug when you have capture the timings during multiple frames.

Note that only the min, max, median, average, standard derivation, percentile, and sum will be sent to the database due to weight constraints (i.e. if you gather the timing over 30 frames, you'll only have these 7 numbers and not the detail of every frame).

### Yamato setup

TODO

```bash
- utr/utr --timeout=2400 --loglevel=verbose --scripting-backend=Il2Cpp --suite={{ suite.mode }} --testfilter={{ suite.filter }} --platform={{ platform.name }} --testproject=C:/Link/TestProjects/{{ project.folder }} --editor-location=.Editor  --report-performance-data --performance-project-id=HDRP --artifacts_path=test-results --player-connection-ip=%BOKKEN_HOST_IP%
```

```YML
  triggers:
    recurring:
      - branch: test-auto-perf
        frequency: daily
```

### Grafana setup

TODO

https://grafana.internal.unity3d.com/
https://grafana.com/docs/grafana/latest/guides/getting_started/
https://play.grafana.org/