# Test Description Asset

The Test Description asset allow you to setup the test suite that will be executed for each type of test. Currently, these 3 are supported:
- Performance Counters: this one is mainly for frame timings, gathered from ProfilingScopes but can be used for any timings.
- Memory: use for every memory related tests that requires to load a scene.
- Build Time: these tests will profile the build time of one scene at a time using the BuildPipeline.

There is also a list of SRP assets that you can reference for each test category, every asset referenced in this list will execute all the scenes. It means that if you have two SRP assets adn 4 scene like in the picture below, 8 tests will be generated in the test runner window: 4 with the first SRP asset and then 4 with the second one.

![](Images/TestAssetDescription.png)

