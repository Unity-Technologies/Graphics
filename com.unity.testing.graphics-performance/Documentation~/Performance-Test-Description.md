# Performance Test Description 

Use the Performance Test Description asset to configure the test suite that Unity uses for three supported test categories: **Performance Counters**, **Memory,** and **Build Time.**

You can reference multiple SRP assets for each test category. Unity pairs each SRP asset with each scene, then generates a test for each pairing within each test category. 

For example, if you have two SRP assets and four scenes, Unity generates eight tests in the Test Runner window (four for each SRP asset).

![Test Description Asset](Images/Test Description Asset.png)

A. **Performance Counters**
The Performance Counters test evaluates timings. It is particularly useful for counting frame timings gathered from profiling scopes using either:

- The Unity Profiler, using BeginSample and EndSample.
- The markers implemented in SRP.

B. **Memory**
The Memory test is for any memory-related test that requires Unity to load a scene.

C. **Build Time**
The Build Time test uses the BuildPipeline to profile the build time of a scene with a particular SRP asset.

D. **Refresh Test Runner List**
Click this button to update the Test Runner window. This triggers a domain reload and updates the Test Runner window with any new data. 

E. **SRP Asset Aliases**
This section lists the SRP assets that the Performance Test Description asset uses to send the data to the Google BigQuery database. Using aliases allows you to rename your assets without changing your queries in Grafana. You can add an alias; to do this, select the plus (**+**) button at the bottom of this panel. If your SRP asset does not have a defined alias, it takes the name of the asset on the disk.

<a name="test-classification"></a>
## Test classification
You can use categories to classify your tests. Unity automatically creates categories from asset tags, which you can see on each asset’s Inspector. To create a new tag, type the name of your tag in the search box and press `Enter`.

![AssetLabels](Images/AssetLabels.png)

Each category is aggregated in the test name with the '_' symbol. This means that each asset can have multiple categories, which you can then use to filter test results on Grafana.

![Filename_Aggregate](Images/Filename_Aggregate.png)

SRP assets and scene assets support these tags. The **ToString()** of the struct in parameter of the test function handles the asset name generation. For example, the **Counters** test function takes a `CounterTestDescription` struct in parameter:

`public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)`

This structure has the following **ToString()** override:
```
public override string ToString()
​    => PerformanceTestUtils.FormatTestName(
​        sceneData.scene,
​        sceneData.sceneLabels,
​        String.IsNullOrEmpty(assetData.alias) ? assetData.asset.name : assetData.alias,
​        assetData.assetLabels,
​        k_Default)
```

