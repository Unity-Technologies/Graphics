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

        private static bool GPUResidentDrawerRequested()
        {
            bool forcedOn = false;
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (
                    arg.Equals(
                        "-force-gpuresidentdrawer",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
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

        public static IEnumerator RunGraphicsTest(SceneGraphicsTestCase testCase)
        {
            Watermark.showDeveloperWatermark = false;
            GraphicsTestLogger.Log(
                $"Running test case '{testCase}' with scene '{testCase.ScenePath}'.");
            GlobalResolutionSetter.SetResolution(RuntimePlatform.Android, width: 1920, height: 1080);
            GlobalResolutionSetter.SetResolution(RuntimePlatform.EmbeddedLinuxArm64, width: 1920, height: 1080);

            SceneManager.LoadScene(testCase.ScenePath);

            // Always wait one frame for scene load
            yield return null;

            var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
            Assert.True(cameras != null && cameras.Any(),
                "Invalid test scene, couldn't find a camera with MainCamera tag.");

            // Disable camera track for OCULUS_SDK and OPENXR_SDK so we ensure we get a consistent screen capture for image comparison
#if OCULUS_SDK || OPENXR_SDK
            // This code is added to hande a case where some test(001_SimpleCube_deferred_RenderPass) would throw error on Quest Vulkan, which would pollute the console for the tests running after.
            UnityEngine.Debug.ClearDeveloperConsole();

            XRDevice.DisableAutoXRCameraTracking(Camera.main, true);
#endif
            var settings = Object.FindAnyObjectByType<UniversalGraphicsTestSettings>();
            Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

            if (!settings.gpuDrivenCompatible && GPUResidentDrawerRequested())
                Assert.Ignore("Test scene is not compatible with GPU Driven and and will be skipped.");

            // Check for RenderGraph compatibility and skip test if needed.
            bool isUsingRenderGraph = RenderGraphGlobalContext.IsRenderGraphActive();

            if (isUsingRenderGraph && settings.renderBackendCompatibility ==
                UniversalGraphicsTestSettings.RenderBackendCompatibility.NonRenderGraph)
                Assert.Ignore("Test scene is not compatible with Render Graph and will be skipped.");
            else if (!isUsingRenderGraph && settings.renderBackendCompatibility ==
                     UniversalGraphicsTestSettings.RenderBackendCompatibility.RenderGraph)
                Assert.Ignore("Test scene is not compatible with non-Render Graph and will be skipped.");

            int waitFrames = 1;

            // for OCULUS_SDK or OPENXR_SDK, this ensures we wait for a reliable image rendering before screen capture and image comparison
#if OCULUS_SDK || OPENXR_SDK
            if(!settings.XRCompatible)
            {
                Assert.Ignore("Quest XR Automation: Test scene is not compatible with XR and will be skipped.");
            }

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
                // Set screen/backbuffer resolution before doing the capture in ImageAssert.AreEqual. This will avoid doing
                // any resizing/scaling of the rendered image when comparing with the reference image in ImageAssert.AreEqual.
                // This has to be done before WaitForEndOfFrame, as the request will only be applied after the frame ends.
                int targetWidth = settings.ImageComparisonSettings.TargetWidth;
                int targetHeight = settings.ImageComparisonSettings.TargetHeight;
                Screen.SetResolution(targetWidth, targetHeight, settings.ImageComparisonSettings.UseBackBuffer ? FullScreenMode.FullScreenWindow : Screen.fullScreenMode);

                // Yield once to finish the current frame (this code runs before the rendering in a frame) with the former
                // resolution.
                // Yield twice to finish the next frame with the new resolution taking effect.
                // Note that once the yields finish and the test resumes after the next for loop, the rendering will be
                // in the same frame where the new resolution first took place. For effects such as motion vector
                // rendering it means if the aspect ratio changes after setting the resolution, the previous camera matrix
                // will be reset, cancelling out all the camera-based motions.
                // In this case (e.g. UniversalGraphicsTest_Terrain, test scene 300 and 301) increase the wait frame to 3
                // on the UniversalGraphicsTestSettings component.
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
            // Does it allocate memory when it renders what's on the main camera?
            var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

            if (settings == null || settings.CheckMemoryAllocation)
            {
                yield return ImageAssert.CheckGCAllocWithCallstack(mainCamera, settings?.ImageComparisonSettings);
            }
        }
    }
}
