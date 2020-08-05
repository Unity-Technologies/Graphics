# Graphics Performance Test Framework

This package will allow you to measure and report performance markers for your graphic package. It's build on top of the [performance test package](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@2.0/manual/index.html) and contains utility functions to measure frame timings, memory usage, build performance and static shader analysis data (VGPR, SGPR, Occupancy, etc.).  
You'll also learn how to automate performance testing using yamato and create beautiful graphs to visualize your performance data using grafana.

![Grafana HDRP](Images/Grafana-HDRP.png)

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

Note that Core Runtime library is not required here, We added it for convenience and also because the example below wouldn't compile without it. 

### Setting up the Test Assets

Nothing complicated here, start by creating both the **Performance Test Description** and **Static Analysis Tests** assets which are in the **Assets > Create > Testing** menu. Note that they need to be created inside a Resources folder so they can be loaded at runtime (thus the Resources folder in the hierarchy above).

Then, to say that you'll use these assets go to **Project Settings > Performance Tests** and reference both assets you created.

For your first runtime test, you'll need at least one Scene and one Render Pipeline Asset. You can go ahead and create a simple scene with a camera that will be used during the tests.

Last step: register the scene in the test description asset, doing so will generate a list of tests in the test runner window. Note that you also need to reference which SRP asset will be used to render the scene. You can have more details about the configuration of the [Test Description Asset in his documentation page](test-description-asset.md)

If you want to setup shader static analysis too, please refer to this documentation page: [Static Shader Analysis](static-shader-analysis.md)

### Writing your first performance test

Here is an example of the test we made to capture the frame timings, you can start from this to create your test:

```CSharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using static PerformanceTestUtils;

public class MyRuntimePerformanceTests : PerformanceTests
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
```

Note that for editor tests, you'll need to inherit from `EditorPerformanceTests` instead of `PerformanceTests`.

### Running and analysing the result

Now that your test script is done, they should appear in the test runner window.  
You can run the test locally and analyse the results under **Window > Analysis > Performance Test Report**. This window will allow you to show all the data that have been gathered during the test, it's useful to debug when you have capture the timings during multiple frames.

Note that only the min, max, median, average, standard derivation, percentile, and sum will be sent to the database due to weight constraints (i.e. if you gather the timing over 30 frames, you'll only have these 7 numbers and not the detail of every frame).

### Yamato setup

Now that you have all your tests setup in local, it's time to automate them.
I'll not go into much detail here and assume that you already have a working yamato setup for your project.

So i'd suggest you to add a new yamato pipeline for each test you'll perform (one for timings, one for build profiling, static analysis, etc.) and just modify the utr command line to support performance testing. So in utr you need to add both `--performance-project-id=ProjectName`, replace ProjectName by the name you'll use to filter your project in the performance database and `--report-performance-data` to enable the performance reporting.

Here's an example of the utr command line we use:
```bash
- utr/utr --timeout=2400 --loglevel=verbose --scripting-backend=Il2Cpp --suite={{ suite.mode }} --testfilter={{ suite.filter }} --platform={{ platform.name }} --testproject=C:/Link/TestProjects/{{ project.folder }} --editor-location=.Editor  --report-performance-data --performance-project-id=HDRP --artifacts_path=test-results --player-connection-ip=%BOKKEN_HOST_IP%
```

Additionally you can setup daily yamato run to automatically update the performance metrics on a certain branch:
```YML
  triggers:
    recurring:
      - branch: master
        frequency: daily
```

If you encounter some issues during the data reporting, you can check what is sent to the database in the artifacts on yamato. The default path for the file is `results/test-results/PerformanceTestReport.html`.

### Grafana setup

Time for the data visualization :) 

For customization and visibility purposes we won't use the [observer website](https://observer.cds.internal.unity3d.com/project) because it can become easily difficult to read your performance metrics when you have a lot of them. That's why we use grafana instead, it requires a lot more setup but the result is definitely worth it.

You can access the unity grafana here: https://grafana.internal.unity3d.com/ (you can sign in with okta) and directly start creating your dashboard. I recommend this guide if you're not used to grafana:[Grafana Getting Started](https://grafana.com/docs/grafana/latest/guides/getting_started/) and this online dashboard to give you an insight of what is possible with Grafana: [Playground](https://play.grafana.org/).

That being said i'll still show you how to write a query for the performance database because there's some things that doesn't work like it should.  

First you need to create a graph (small bar chart withy a + on top right) then click Add Query in the new panel, after this you enter in edit mode of the panel. In the Query field (source database) you need to select the performance database, look for `rd-perf-test-data-pdr BigQuery` in the list. Then you have to write the SQL query, do not try to use the build, it won't work with our data structure (click on Edit SQL to hide the builder).

Now here's an example query we use in HDRP to retrieve the frame timings:

```SQL
#standardSQL
SELECT
    -- We select the average of the median of all our tests
	AVG(sampleGroup.Median) as median,
    -- run.StartTime doesn't work so we use EndTime
    run.EndTime as time,
    -- Extract the name of the sampler from the name of the samplegroup
    REGEXP_EXTRAC(sampleGroup.Definition.Name, 'Timing,\\w+,(.*)') as metric
FROM
perf_test_results.run,
    -- We need to use UNNEST for every array we have in the database, see db scheme below
	UNNEST(Results) AS result,
	UNNEST(ProjectVersions) as pv,
	UNNEST(result.SampleGroups) AS sampleGroup
WHERE
    -- Start by filtering using the project name we set in the utr command line
    pv.ProjectName = 'HDRP'
    -- Then we can filter by platform
    AND BuildSettings.Platform = "PS4" ANDPlayerSystemInfo.DeviceModel = '$ps4_config'
    -- We use this filter to only get the data in the time window of grafana, it avoids to overload the database.
    -- Note that $__timeFilter doesn't work so this is a workaround
    AND run.EndTime BETWEEN TIMESTAMP_MILLIS($__from) AND TIMESTAMP_MILLI($__to)
    -- Filter by git branch name (notice the dashboard templating variable here)
    AND pv.Branch = '$Git_branch_name'
    -- And finally we can filter by our test name
    AND result.TestName LIKE '%PerformanceTests.Counters%$HDRP_asset_config%'
    -- And sample group name
    AND REGEXP_CONTAINS(sampleGroup.Definition.Name, 'Timing,CPU,{Selected_counter:regex}')
-- In bigquery we always need to group every data we select (otherwise you'll have an error)
GROUP BY time, sampleGroup.Definition.Name
-- Discard median that have 0 in value
HAVING median <> 0
-- And sort by run date and metric name so the metrics are always in the same order in the graph.
ORDER BY time, metric
```

There are some specific functions to the google big query database, so if you want to be sure that something exists / work in a certain way I suggest you to check out the [Google Big Query SQL documentation page](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators).

Here's the scheme of the database, every field should be populated automatically by the performance package. If you encounter any issue with a value in the db, you can ask in #devs-performance-testing on slack.

| Field name                                        | Type      | Mode     | 
|---------------------------------------------------|-----------|----------|
| VersionDate                                       | TIMESTAMP | NULLABLE |             |
| RunId                                             | STRING    | NULLABLE |             |
| TestSuite                                         | STRING    | NULLABLE |             |
| StartTime                                         | TIMESTAMP | NULLABLE |             |
| EndTime                                           | TIMESTAMP | NULLABLE |             |
| PlayerSystemInfo                                  | RECORD    | NULLABLE |             |
| PlayerSystemInfo. OperatingSystem                 | STRING    | NULLABLE |             |
| PlayerSystemInfo. DeviceModel                     | STRING    | NULLABLE |             |
| PlayerSystemInfo. DeviceName                      | STRING    | NULLABLE |             |
| PlayerSystemInfo. ProcessorType                   | STRING    | NULLABLE |             |
| PlayerSystemInfo. ProcessorCount                  | INTEGER   | NULLABLE |             |
| PlayerSystemInfo. GraphicsDeviceName              | STRING    | NULLABLE |             |
| PlayerSystemInfo. SystemMemorySize                | INTEGER   | NULLABLE |             |
| PlayerSystemInfo. XrModel                         | STRING    | NULLABLE |             |
| PlayerSystemInfo. XrDevice                        | STRING    | NULLABLE |             |
| EditorVersion                                     | RECORD    | NULLABLE |             |
| EditorVersion. FullVersion                        | STRING    | NULLABLE |             |
| EditorVersion. DateSeconds                        | INTEGER   | NULLABLE |             |
| EditorVersion. Branch                             | STRING    | NULLABLE |             |
| EditorVersion. RevisionValue                      | INTEGER   | NULLABLE |             |
| ProductVersion                                    | RECORD    | NULLABLE |             |
| ProductVersion. MajorVersion                      | INTEGER   | NULLABLE |             |
| ProductVersion. MinorVersion                      | INTEGER   | NULLABLE |             |
| ProductVersion. RevisionVersion                   | STRING    | NULLABLE |             |
| ProductVersion. RevisionVersionFirstNumber        | INTEGER   | NULLABLE |             |
| ProductVersion. RevisionVersionLetter             | STRING    | NULLABLE |             |
| ProductVersion. RevisionVersionSecondNumber       | INTEGER   | NULLABLE |             |
| ProductVersion. Changeset                         | STRING    | NULLABLE |             |
| ProductVersion. Revision                          | INTEGER   | NULLABLE |             |
| ProductVersion. Branch                            | STRING    | NULLABLE |             |
| ProductVersion. Date                              | TIMESTAMP | NULLABLE |             |
| BuildSettings                                     | RECORD    | NULLABLE |             |
| BuildSettings. Platform                           | STRING    | NULLABLE |             |
| BuildSettings. BuildTarget                        | STRING    | NULLABLE |             |
| BuildSettings. DevelopmentPlayer                  | BOOLEAN   | NULLABLE |             |
| BuildSettings. AndroidBuildSystem                 | STRING    | NULLABLE |             |
| ScreenSettings                                    | RECORD    | NULLABLE |             |
| ScreenSettings. ScreenWidth                       | INTEGER   | NULLABLE |             |
| ScreenSettings. ScreenHeight                      | INTEGER   | NULLABLE |             |
| ScreenSettings. ScreenRefreshRate                 | INTEGER   | NULLABLE |             |
| ScreenSettings. Fullscreen                        | BOOLEAN   | NULLABLE |             |
| QualitySettings                                   | RECORD    | NULLABLE |             |
| QualitySettings. Vsync                            | INTEGER   | NULLABLE |             |
| QualitySettings. AntiAliasing                     | INTEGER   | NULLABLE |             |
| QualitySettings. ColorSpace                       | STRING    | NULLABLE |             |
| QualitySettings. AnisotropicFiltering             | STRING    | NULLABLE |             |
| QualitySettings. BlendWeights                     | STRING    | NULLABLE |             |
| PlayerSettings                                    | RECORD    | NULLABLE |             |
| PlayerSettings. ScriptingBackend                  | STRING    | NULLABLE |             |
| PlayerSettings. VrSupported                       | BOOLEAN   | NULLABLE |             |
| PlayerSettings. MtRendering                       | BOOLEAN   | NULLABLE |             |
| PlayerSettings. GraphicsJobs                      | BOOLEAN   | NULLABLE |             |
| PlayerSettings. GpuSkinning                       | BOOLEAN   | NULLABLE |             |
| PlayerSettings. GraphicsApi                       | STRING    | NULLABLE |             |
| PlayerSettings. StereoRenderingPath               | STRING    | NULLABLE |             |
| PlayerSettings. RenderThreadingMode               | STRING    | NULLABLE |             |
| PlayerSettings. AndroidMinimumSdkVersion          | STRING    | NULLABLE |             |
| PlayerSettings. AndroidTargetSdkVersion           | STRING    | NULLABLE |             |
| PlayerSettings. AndroidSdkVersion                 | STRING    | NULLABLE |             |
| PlayerSettings. ScriptingRuntimeVersion           | STRING    | NULLABLE |             |
| PlayerSettings. EnabledXrTargets                  | STRING    | NULLABLE |             |
| ProjectVersions                                   | RECORD    | REPEATED |             |
| ProjectVersions. ProjectName                      | STRING    | NULLABLE |             |
| ProjectVersions. Changeset                        | STRING    | NULLABLE |             |
| ProjectVersions. Branch                           | STRING    | NULLABLE |             |
| ProjectVersions. Date                             | TIMESTAMP | NULLABLE |             |
| Results                                           | RECORD    | REPEATED |             |
| Results. TestName                                 | STRING    | NULLABLE |             |
| Results. TestCategories                           | RECORD    | REPEATED |             |
| Results.TestCategories. Name                      | STRING    | NULLABLE |             |
| Results. TestVersion                              | STRING    | NULLABLE |             |
| Results. StartTime                                | STRING    | NULLABLE |             |
| Results. EndTime                                  | STRING    | NULLABLE |             |
| Results. SampleGroups                             | RECORD    | REPEATED |             |
| Results.SampleGroups. Samples                     | RECORD    | REPEATED |             |
| Results.SampleGroups.Samples. Value               | FLOAT     | NULLABLE |             |
| Results.SampleGroups. AggregatedSampleValue       | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Min                         | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Max                         | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Median                      | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Average                     | FLOAT     | NULLABLE |             |
| Results.SampleGroups. StandardDeviation           | FLOAT     | NULLABLE |             |
| Results.SampleGroups. PercentileValue             | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Sum                         | FLOAT     | NULLABLE |             |
| Results.SampleGroups. Zeroes                      | INTEGER   | NULLABLE |             |
| Results.SampleGroups. SampleCount                 | INTEGER   | NULLABLE |             |
| Results.SampleGroups. Definition                  | RECORD    | NULLABLE |             |
| Results.SampleGroups.Definition. Name             | STRING    | NULLABLE |             |
| Results.SampleGroups.Definition. SampleUnit       | STRING    | NULLABLE |             |
| Results.SampleGroups.Definition. AggregationType  | STRING    | NULLABLE |             |
| Results.SampleGroups.Definition. Threshold        | FLOAT     | NULLABLE |             |
| Results.SampleGroups.Definition. IncreaseIsBetter | BOOLEAN   | NULLABLE |             |
| Results.SampleGroups.Definition. Percentile       | FLOAT     | NULLABLE |             |
| TestProject                                       | STRING    | NULLABLE |             |

Note: Record type mean that it's an array so you need to `UNNEST` it before accessing it's value

### Naming convention

Because we can only pair one sample name to one metric when reporting data, we need to have a well defined convention to pack the information we need into the sample group name.

The `PerformanceTestUtils` class contains functions to help you format the data in a way that can easily be parsed in grafana using regex:

- `FormatTestName` will format the name of the test, packing the data type, it's category, the settings (generally the SRP asset name alias), the settings category and the test name. Here's an example of the generated format for our memory test: `0001_LitCube:Small,Deferred_SRP:Default,RenderTexture`
- `FormatSampleGroupName` will format the name of the samplegroup used to send a metric value. It contains the metric name, it's category and the data name. Example with Counter test: `Timing,GPU,Gbuffer`

On the grafana side, you can use [REGEXP_CONTAINS](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#regexp_contains) to match a certain format and [REGEXP_EXTRACT](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#regexp_extract) to extract a certain part of the name.
For example in this query with use both of these functions to filter timings and display only the name of the counter:
```SQL
#standardSQL
SELECT
    AVG(sampleGroup.Median) as median, run.EndTime as time,
    REGEXP_EXTRACT(sampleGroup.Definition.Name, 'Timing,\\w+,(.*)') as metric
FROM
perf_test_results.run,
    UNNEST(Results) AS result,
    UNNEST(ProjectVersions) as pv,
    UNNEST(result.SampleGroups) AS sampleGroup
WHERE
    pv.ProjectName = 'HDRP' AND BuildSettings.Platform = "PS4" AND PlayerSystemInfo.DeviceModel = '$ps4_config' -- Mandatory filters, ensure we use the good project, test suite and time window
    AND run.EndTime BETWEEN TIMESTAMP_MILLIS($__from) AND TIMESTAMP_MILLIS($__to) -- Workaround for the $__timeFilter which doens't work for google big query datasource
    AND pv.Branch = '$Git_branch_name'
    AND result.TestName LIKE '%PerformanceTests.Counters%$HDRP_asset_config%'
    AND REGEXP_CONTAINS(sampleGroup.Definition.Name, 'Timing,CPU,${Selected_counter:regex}') -- Test suite filters
GROUP BY time, sampleGroup.Definition.Name
HAVING median <> 0
ORDER BY time, metric
```