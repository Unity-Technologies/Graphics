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

### Test Filters
Since test cases are automatically generated based on the scenes in the project build settings the usual way of filtering test cases may not work.  To filter scenes out of specific configurations create a test filter scriptable object by right clicking in the asset list and choosing Create -> Testing -> Test Filter ScriptableObject.  This will create a filter asset which you may want to place in the Editor directory as it is not used at runtime.  Select this newly created object in the inspector then add a scene in the scene asset field to exclude from the configuration specified.  If multiple scenes need to be excluded in the same configuration you can add them to the filter by incrementing the number next to the scene field and adding additional scenes to the fields that appear.  Remove scenes by decrementing the number, this will remove entries at the bottom of the list.

Note that if options are left blank or otherwise unspecified it will indicate all values.  For example, if color space is set to Unititialized the scene will be removed from the test case list when running in either linear or gamma colorspace.  To indicate a test filter should only be applied to any vr configuration, leave the XR SDK field blank but check all stereo rendering modes.  Also note the using None in the XR SDK will indicate to exclude the case only from non-vr modes (if the stereo modes are not checked).  See the tooltips in the column headers for more details on this.

To add more filters just click the Add Filter button at the bottom.  You can have as many filters per scene as you need but will need to create one for each configuration you want.  However, you should only have one test filter scriptable object in the project as the filters are applied from only the first scriptable object found in the assets.

### Reference images

The simplest way to set up your initial reference images is to allow the tests to generate them. Once you have created your tests and added them to the Build Settings, you can either run the tests in-Editor to generate images using the Editor renderer, or you can exit Unity and run the tests on-device from the commandline.

When the run completes, you should be able to obtain a TestResults.xml file for the run. As well as reporting that the tests failed (because they have no reference images), the TestResults.xml file will also contain encoded versions of the rendered images.

In Unity, go to `Tests -> Extract images from TestResults.xml...` and select the TestResults.xml file created by the run. The framework will process the images in the test results and put them into a folder called "ActualImages" in the root of your Assets folder, where you can inspect them to make sure they look correct.

Once you're happy that the images look correct, you should rename the `ActualImages` folder to `ReferenceImages` (or merge the images into the existing ReferenceImages folder, if there is one). Run the tests again and you should see that they now use the reference images successfully!

By default, reference images will be set up in a three-level hierarchy of folders: `ColorSpace/Platform/GraphicsAPI`. If you want to use the same reference image across multiple graphics APIs, you can put it directly into the `ColorSpace/Platform` folder.

## Scene view tests

To capture from the scene view instead of the game view, use `yield return CaptureSceneView.Capture()` or `yield return CaptureSceneView.CaptureFromMainCamera()` in a `UnityTest`. This will return an `IEnumerator` that will follow this procedure: 1. Instantiate a scene view window in the specified width and height (default is 512), 2. Match the camera with the specified transform (or `Camera.main`) and 3. Read the pixels on your screen at the scene view window location. The captured Texture2D will be accessible as `CaptureSceneView.Result`.

Note: On Windows, only DX12 is supported at this time.

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
|Feb 16, 2022|Added scene view tests section. Matches package version 7.8.19-exp.1|
