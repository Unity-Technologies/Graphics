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
using UnityEngine.TestTools.Graphics.Contexts;
using Object = UnityEngine.Object;
#if OCULUS_SDK || OPENXR_SDK
using UnityEngine.XR;
#endif

namespace Unity.Rendering.Universal.Tests
{
    public static class UniversalGraphicsTests
    {
#if UNITY_ANDROID
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;
#endif
        public static IEnumerator RunGraphicsTest(SceneGraphicsTestCase testCase)
        {
            Watermark.showDeveloperWatermark = false;
            GraphicsTestLogger.Log(
                $"Running test case '{testCase}' with scene '{testCase.ScenePath}'.");

            SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());

            // Disable camera track for OCULUS_SDK and OPENXR_SDK so we ensure we get a consistent screen capture for image comparison
#if OCULUS_SDK || OPENXR_SDK
            // This code is added to hande a case where some test(001_SimpleCube_deferred_RenderPass) would throw error on Quest Vulkan, which would pollute the console for the tests running after.
            UnityEngine.Debug.ClearDeveloperConsole();
#endif
            var settings = Object.FindAnyObjectByType<UniversalGraphicsTestSettings>();
            Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

#if OCULUS_SDK || OPENXR_SDK
            if(!settings.XRCompatible)
            {
                Assert.Ignore("Quest XR Automation: Test scene is not compatible with XR and will be skipped.");
            }
#endif
            int waitFrames = 1;

            // for OCULUS_SDK or OPENXR_SDK, this ensures we wait for a reliable image rendering before screen capture and image comparison
#if OCULUS_SDK || OPENXR_SDK
            waitFrames = 4;
#elif ENABLE_VR && USE_XR_MOCK_HMD
            waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.XRCompatible, settings.WaitFrames, settings.ImageComparisonSettings);
#else
            waitFrames = settings.WaitFrames;
#endif
            Scene scene = SceneManager.GetActiveScene();

            yield return null;

            if (settings.ImageComparisonSettings.UseBackBuffer)
            {
                waitFrames = Mathf.Max(waitFrames, 1);
            }

            if (settings.SetBackBufferResolution)
            {
                //This option is disabled and we keep it only to keep the extra wait frames and preserve the existing
                // reference image.

                // Changing resolution during a test run introduce instabilities.
                // Decides of a resolution for a test run and leave it to that. The resolution for most
                // test projects is set to a constant 1080p,except when running in XR compatibility mode.

                // XR compatibility mode changes resolution depending on the target size, because the image is
                // always captured from the backbuffer.

                GraphicsTestLogger.Log(LogType.Log, "Set back buffer resolution is being deprecated and does not change the resolution anymore, it only introduces extra wait frames to the test.");

                // Removing this line causes a subset of tests to fail. Removing it would require to update images
                // and revisit some tests.
                waitFrames = Mathf.Max(waitFrames, 2);
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

        // If we're running using OCULUS_SDK or OPENXR_SDK, we need to use the ScreenCapture API to get stereo images for comparison
#if OCULUS_SDK || OPENXR_SDK
        yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();
        var screenShot = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        screenShot = ScreenCapture.CaptureScreenshotAsTexture(ScreenCapture.StereoScreenCaptureMode.BothEyes);

        // Log the frame we are comparing to catch/debug waitFrame differences.
        Debug.Log($"OCULUS_SDK || OPENXR_SDK == true: ImageAssert.AreEqual called on Frame #{Time.frameCount} using capture from {nameof(ScreenCapture.CaptureScreenshotAsTexture)}");
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, screenShot, settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);

        // Else continue to use the camera for image comparison
#else
            // Log the frame we are comparing to catch/debug waitFrame differences.
            Debug.Log($"ImageAssert.AreEqual called on Frame #{Time.frameCount} using capture from {nameof(cameras)}");

#if UNITY_2023_2_OR_NEWER
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
            {
                yield return ImageAssert.AreEqualAsync(testCase.ReferenceImage.Image, cameras.Where(x => x != null),
                    (res) => { }, settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
            }
            else
            {
                ImageAssert.AreEqual(testCase.ReferenceImage.Image, cameras.Where(x => x != null),
                    settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
            }
#else
        ImageAssert.AreEqual(testCase.ReferenceImage.Image, cameras.Where(x => x != null), settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
#endif

#endif

            if (settings == null || settings.CheckMemoryAllocation)
            {
                // Does it allocate memory when it renders what's on the main camera?
                var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
                yield return ImageAssert.CheckGCAllocWithCallstack(mainCamera, settings?.ImageComparisonSettings);
            }
        }
    }
}
