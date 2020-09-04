# Test Description Asset

## Description

The Test Description asset allow you to setup the test suite that will be executed for each type of test. Currently, these 3 are supported:
- Performance Counters: this one is mainly for frame timings, gathered from ProfilingScopes but can be used for any timings.
- Memory: use for every memory related tests that requires to load a scene.
- Build Time: these tests will profile the build time of one scene at a time using the BuildPipeline.

There is also a list of SRP assets that you can reference for each test category, every asset referenced in this list will execute all the scenes. It means that if you have two SRP assets adn 4 scene like in the picture below, 8 tests will be generated in the test runner window: 4 with the first SRP asset and then 4 with the second one.

![](Images/TestAssetDescription.png)

There is also a refresh button that you can use to update the test runner window, it actually triggers a domain reload but it's the only way I found to update the test runner window with the new data.

Then at the bottom of the asset, you have the SRP Asset Aliases, these are the name of the SRP assets that will be used to send the data to the google big query database, if you didn't defined an alias for you SRP asset it will take the name of the asset of the disk. Using aliases allow you to rename your assets without changing your queries in grafana.

## Test classification & Name

As you can see in the `Naming convention` section of the graphics performance test framework documentation, you can classify your tests using categories.
These categories will automatically be gathered from your assets using the unity asset tag system at the bottom of any asset inspector:

![](Images/AssetLabels.png)

Note that every label will be aggregated with the '_' symbol so you can have multiple categories for each asset and filter them on grafana.

Assets that support these labels are SRP assets and scene assets, note that the aggregated labels will be visible in the test name. The asset name generation is handled by the ToString() of the struct in parameter of the test function: for example the Counters test function taskes a `CounterTestDescription` struct in parameter
```CSharp
public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
```
And this struct have this ToString() override:
```CSharp
public override string ToString()
    => PerformanceTestUtils.FormatTestName(
        sceneData.scene,
        sceneData.sceneLabels,
        String.IsNullOrEmpty(assetData.alias) ? assetData.asset.name : assetData.alias,
        assetData.assetLabels,
        k_Default);
```