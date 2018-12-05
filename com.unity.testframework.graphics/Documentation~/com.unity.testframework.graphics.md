# About the Graphics Test Framework

Use the Graphics Test Framework package to create automated tests for rendering outputs - tests that render an image and compare it to a 'known good' reference image.

# Installing the Graphics Test Framework

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html). 

<a name="UsingPackageName"></a>
# Using the Graphics Test Framework

There are two main components to the framework:

## ImageAssert.AreEqual()

This is a new assertion method that you can use in your tests to check two images for equality. There is also an overload that takes a camera instead of an image, and automatically captures the camera output for you and compares that.

An optional third parameter allows you to configure the sensitivity of the check. Even when everything else is the same, it's common for there to be small differences between images due to changes in hardware, driver version, and so on. You can use this third parameter to set a threshold for how different pixels need to be before they are counted, and you can set a threshold for how different the overall image needs to be before the assertion will fail.

## Automatic test case management

The framework can automatically generate test cases based on the scenes in the project and manage reference images for them.

Using this feature requires a little bit of setup. Firstly, on your test method itself, you should add two attributes, `[PrebuildSetup("SetupGraphicsTestCases")]` and `[UseGraphicsTestCases]`, like this:

```
[UnityTest]
[PrebuildSetup("SetupGraphicsTestCases")]
[UseGraphicsTestCases]
public IEnumerator DoTest(GraphicsTestCase testCase)
{
   
}
```

Your test method should also take a single `GraphicsTestCase` parameter. You will also usually want to use `[UnityTest]` and return `IEnumerator`, rather than using `[Test]` and returning `void`, because usually you will want to load a new scene in your test, and this requires yielding one frame for the load to complete.

With this in place, any scene added to the build systems will result in a test case for the scene being generated in the test runner.

### Reference images

The simplest way to set up your initial reference images is to allow the tests to generate them. Once you have created your tests and added them to the Build Settings, you can either run the tests in-Editor to generate images using the Editor renderer, or you can exit Unity and run the tests on-device from the commandline.

When the run completes, you should be able to obtain a TestResults.xml file for the run. As well as reporting that the tests failed (because they have no reference images), the TestResults.xml file will also contain encoded versions of the rendered images.

In Unity, go to `Tests -> Extract images from TestResults.xml...` and select the TestResults.xml file created by the run. The framework will process the images in the test results and put them into a folder called "ActualImages" in the root of your Assets folder, where you can inspect them to make sure they look correct.

Once you're happy that the images look correct, you should rename the `ActualImages` folder to `ReferenceImages` (or merge the images into the existing ReferenceImages folder, if there is one). Run the tests again and you should see that they now use the reference images successfully!

By default, reference images will be set up in a three-level hierarchy of folders: `ColorSpace/Platform/GraphicsAPI`. If you want to use the same reference image across multiple graphics APIs, you can put it directly into the `ColorSpace/Platform` folder.

# Technical details
## Requirements

This version of the Graphics Test Framework is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

## Known limitations

Graphics Test Framework version 0.1.0 includes the following known limitations:

* Actual/Diff images cannot be retrieved from test results when running in-player tests from the Unity Test Runner interactively. The commandline must be used instead.

## Document revision history
|Date|Reason|
|---|---|
|May 10, 2018|Document created. Matches package version 0.1|
