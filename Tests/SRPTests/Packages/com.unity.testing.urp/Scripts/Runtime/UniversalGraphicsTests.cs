using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;
#if OCULUS_SDK
using UnityEngine.XR;
#endif

public class UniversalGraphicsTests
{
#if UNITY_ANDROID
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;
#endif

    private bool GPUResidentDrawerRequested()
    {
        bool forcedOn = false;
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.Equals("-force-gpuresidentdrawer", StringComparison.InvariantCultureIgnoreCase))
            {
                forcedOn = true;
                break;
            }
        }

        var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
        if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
            return forcedOn || mbAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled;

        return false;
    }

    public const string universalPackagePath = "Assets/ReferenceImages";
#if UNITY_WEBGL || UNITY_ANDROID
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return RuntimeGraphicsTestCaseProvider.EnsureGetReferenceImageBundlesAsync();
    }
#endif

    [UnityTest, Category("UniversalRP")]
#if UNITY_EDITOR
    [PrebuildSetup("SetupGraphicsTestCases")]
#endif
    [UseGraphicsTestCases(universalPackagePath)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        Debug.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImagePathLog}.");
#if UNITY_WEBGL || UNITY_ANDROID
        RuntimeGraphicsTestCaseProvider.AssociateReferenceImageWithTest(testCase);
#endif
        GlobalResolutionSetter.SetResolution(RuntimePlatform.Android, width: 1920, height: 1080);
        GlobalResolutionSetter.SetResolution(RuntimePlatform.EmbeddedLinuxArm64, width: 1920, height: 1080);

        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
        Assert.True(cameras != null && cameras.Any(), "Invalid test scene, couldn't find a camera with MainCamera tag.");

        // Disable camera track for OCULUS_SDK so we ensure we get a consistent screen capture for image comparison
#if OCULUS_SDK
        XRDevice.DisableAutoXRCameraTracking(Camera.main, true);
#endif
        var settings = Object.FindAnyObjectByType<UniversalGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

        if (!settings.gpuDrivenCompatible && GPUResidentDrawerRequested())
            Assert.Ignore("Test scene is not compatible with GPU Driven and and will be skipped.");

        // Check for RenderGraph compatibility and skip test if needed.
        bool isUsingRenderGraph = RenderGraphGraphicsAutomatedTests.enabled ||
            (!GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()?.enableRenderCompatibilityMode ?? false);

        if (isUsingRenderGraph && settings.renderBackendCompatibility == UniversalGraphicsTestSettings.RenderBackendCompatibility.NonRenderGraph)
            Assert.Ignore("Test scene is not compatible with Render Graph and will be skipped.");
        else if (!isUsingRenderGraph && settings.renderBackendCompatibility == UniversalGraphicsTestSettings.RenderBackendCompatibility.RenderGraph)
            Assert.Ignore("Test scene is not compatible with non-Render Graph and will be skipped.");

        int waitFrames = 1;

        // for OCULUS_SDK, this ensures we wait for a reliable image rendering before screen capture and image comparison
#if OCULUS_SDK
        if(!settings.XRCompatible)
        {
            Assert.Ignore("Quest XR Automation: Test scene is not compatible with XR and will be skipped.");
        }

        waitFrames = 4;
#else
        waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.XRCompatible, settings.WaitFrames, settings.ImageComparisonSettings);
#endif
        Scene scene = SceneManager.GetActiveScene();

        yield return null;

        if (settings.ImageComparisonSettings.UseBackBuffer)
        {
            waitFrames = Mathf.Max(waitFrames, 1);

            if (settings.SetBackBufferResolution)
            {
                // Set screen/backbuffer resolution before doing the capture in ImageAssert.AreEqual. This will avoid doing
                // any resizing/scaling of the rendered image when comparing with the reference image in ImageAssert.AreEqual.
                // This has to be done before WaitForEndOfFrame, as the request will only be applied after the frame ends.
                int targetWidth = settings.ImageComparisonSettings.TargetWidth;
                int targetHeight = settings.ImageComparisonSettings.TargetHeight;
                Screen.SetResolution(targetWidth, targetHeight, true);

                // We need to wait at least 2 frames for the Screen.SetResolution to take effect.
                // After that, Screen.width and Screen.height will have the target resolution.
                waitFrames = Mathf.Max(waitFrames, 2);
            }
        }

        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

#if UNITY_ANDROID
        // On Android first scene often needs a bit more frames to load all the assets
        // otherwise the screenshot is just a black screen
        // NOTE: Added frames can cause a different frame captured on "single test" vs. "all tests"
        // HACK: To alleviate the "different frame captured" issue, we wait at least N additional frames on the first scene.
        if (!wasFirstSceneRan && firstSceneAdditionalFrames > waitFrames)
        {
            for (int i = 0; i < firstSceneAdditionalFrames; i++)
            {
                yield return new WaitForEndOfFrame();
            }
            wasFirstSceneRan = true;
        }
#endif

        // If we're running using OCULUS_SDK, we need to use the ScreenCapture API to get stereo images for comparison
#if OCULUS_SDK
        yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();
        var screenShot = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        screenShot = ScreenCapture.CaptureScreenshotAsTexture(ScreenCapture.StereoScreenCaptureMode.BothEyes);

        // Log the frame we are comparing to catch/debug waitFrame differences.
        Debug.Log($"OCULUS_SDK == true: ImageAssert.AreEqual called on Frame #{Time.frameCount} using capture from {nameof(ScreenCapture.CaptureScreenshotAsTexture)}");
        ImageAssert.AreEqual(testCase.ReferenceImage, screenShot, settings.ImageComparisonSettings, testCase.ReferenceImagePathLog);

        // Else continue to use the camera for image comparison
#else
        // Log the frame we are comparing to catch/debug waitFrame differences.
        Debug.Log($"ImageAssert.AreEqual called on Frame #{Time.frameCount} using capture from {nameof(cameras)}");
        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings, testCase.ReferenceImagePathLog);
#endif
        // Does it allocate memory when it renders what's on the main camera?
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        if (settings == null || settings.CheckMemoryAllocation)
        {
			yield return ImageAssert.CheckGCAllocWithCallstack(mainCamera, settings?.ImageComparisonSettings);
        }
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

#if ENABLE_VR
    [TearDown]
    public void TearDownXR()
    {
        XRGraphicsAutomatedTests.running = false;
    }

#endif
#endif
}
