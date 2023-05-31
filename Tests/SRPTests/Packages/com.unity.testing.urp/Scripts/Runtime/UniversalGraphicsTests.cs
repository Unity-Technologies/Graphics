using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
#if OCULUS_SDK
using UnityEngine.XR;
#endif

public class UniversalGraphicsTests
{
#if UNITY_ANDROID
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;
#endif

    public const string universalPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("UniversalRP"), UnityPlatform(exclude = new[] { RuntimePlatform.GameCoreXboxSeries, RuntimePlatform.GameCoreXboxOne })] // Disabled for Instability https://jira.unity3d.com/browse/UUM-27717
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(universalPackagePath)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
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

        int waitFrames = 1;

        // for OCULUS_SDK, this ensures we wait for a reliable image rendering before screen capture and image comparison
#if OCULUS_SDK
         waitFrames = 4;
#else
        waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.XRCompatible, settings.WaitFrames, settings.ImageComparisonSettings);
#endif
        Scene scene = SceneManager.GetActiveScene();

        yield return null;

        if (settings.ImageComparisonSettings.UseBackBuffer && waitFrames < 1)
            waitFrames = 1;

        if (settings.ImageComparisonSettings.UseBackBuffer && settings.SetBackBufferResolution)
        {
            // Set screen/backbuffer resolution before doing the capture in ImageAssert.AreEqual. This will avoid doing
            // any resizing/scaling of the rendered image when comparing with the reference image in ImageAssert.AreEqual.
            // This has to be done before WaitForEndOfFrame, as the request will only be applied after the frame ends.
            int targetWidth = settings.ImageComparisonSettings.TargetWidth;
            int targetHeight = settings.ImageComparisonSettings.TargetHeight;
            Screen.SetResolution(targetWidth, targetHeight, true);
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
        bool allocatesMemory = false;
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        if (settings == null || settings.CheckMemoryAllocation)
        {
            try
            {
                ImageAssert.AllocatesMemory(mainCamera, settings?.ImageComparisonSettings);
            }
            catch (AssertionException)
            {
                allocatesMemory = true;
            }

            if (allocatesMemory)
                Assert.Fail("Allocated memory when rendering what is on main camera");
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
