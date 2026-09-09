using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

#if ENABLE_VR && USE_XR_MOCK_HMD
using UnityEngine.XR;
#endif

namespace Unity.Testing.XR.Runtime
{
    public class ConfigureMockHMD
    {
        /// <summary>
        /// Configures MockHMD for XR testing with specified image comparison settings.
        /// Sets up the XR display subsystem and configures the player resolution to match.
        /// On XR, the player resolution must match the target width and target height specified in the settings.
        /// </summary>
        /// <param name="xrCompatible">Whether the test scene is compatible with XR.</param>
        /// <param name="waitFrames">Number of frames to wait before test execution.</param>
        /// <param name="settings">Image comparison settings containing target resolution.</param>
        /// <returns>Number of frames to wait. Returns at least 4 frames for XR eye texture resizing when XR is enabled.</returns>
        static public int SetupTest(bool xrCompatible, int waitFrames, UnityEngine.TestTools.Graphics.ImageComparisonSettings settings)
        {
#if ENABLE_VR && USE_XR_MOCK_HMD
            if (XRGraphicsAutomatedTests.enabled)
            {
                if (xrCompatible)
                {
                    ConfigureXRDisplay(xrCompatible, settings);
                    int resolutionWaitFrames = SetPlayerResolutionForXR(xrCompatible, settings, waitFrames);

                    // XR plugin MockHMD requires a few frames to resize eye textures
                    return Mathf.Max(waitFrames, resolutionWaitFrames);
                }
                else
                {
                    Assert.Ignore("Test scene is not compatible with XR and will be skipped.");
                }
            }
#endif

            return waitFrames;
        }

        /// <summary>
        /// Configures the XR display subsystem for MockHMD testing.
        /// Sets up single-pass instanced rendering, mirror blit mode, eye resolution, and validates the display is running.
        /// On XR, configures eye resolution to match the target width and target height specified in the settings.
        /// Safe to call regardless of XR context - will only execute when ENABLE_VR and USE_XR_MOCK_HMD are defined and test is XR compatible.
        /// </summary>
        /// <param name="xrCompatible">Whether the test scene is compatible with XR.</param>
        /// <param name="settings">Image comparison settings containing target resolution.</param>
        static public void ConfigureXRDisplay(bool xrCompatible, UnityEngine.TestTools.Graphics.ImageComparisonSettings settings)
        {
        #if ENABLE_VR && USE_XR_MOCK_HMD
            if (XRGraphicsAutomatedTests.enabled && xrCompatible)
            {
                XRGraphicsAutomatedTests.running = true;

                var resolution = GetResolutionFromSettings(settings);

                // Validate MockHMD is enabled and running
                List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(xrDisplays);
                Assume.That(xrDisplays.Count == 1 && xrDisplays[0].running, "XR display MockHMD is not running!");

                // Configure MockHMD to use single-pass and compare reference image against second view (right eye)
                xrDisplays[0].SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.RightEye);

                // Configure MockHMD stereo mode
                xrDisplays[0].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                Unity.XR.MockHMD.MockHMD.SetRenderMode(Unity.XR.MockHMD.MockHMDBuildSettings.RenderMode.SinglePassInstanced);

                Unity.XR.MockHMD.MockHMD.SetEyeResolution(resolution.width, resolution.height);
                Unity.XR.MockHMD.MockHMD.SetMirrorViewCrop(0.0f);
            }
        #endif
        }


        /// <summary>
        /// Sets the player/game view resolution to match XR eye texture resolution.
        /// On XR, this resolution must match the target width and target height of the test settings.
        /// Safe to call regardless of XR context - will only execute when ENABLE_VR and USE_XR_MOCK_HMD are defined and test is XR compatible.
        /// </summary>
        /// <param name="xrCompatible">Whether the test scene is compatible with XR.</param>
        /// <param name="settings">Image comparison settings containing target resolution.</param>
        /// <param name="waitFrames">Base number of frames to wait.</param>
        /// <returns>Number of frames to wait (4) for XR eye textures to properly resize, or original waitFrames if not in XR context.</returns>
        static public int SetPlayerResolutionForXR(bool xrCompatible, UnityEngine.TestTools.Graphics.ImageComparisonSettings settings, int waitFrames)
        {
        #if ENABLE_VR && USE_XR_MOCK_HMD
            if (XRGraphicsAutomatedTests.enabled && xrCompatible)
            {
                XRGraphicsAutomatedTests.running = true;

                var resolution = GetResolutionFromSettings(settings);

        #if UNITY_EDITOR
                UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.SetGameViewSize(resolution.width, resolution.height);
        #else
                Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);
        #endif
                return Mathf.Max(waitFrames, 4);
            }
        #endif
            return waitFrames;
        }

        /// <summary>
        /// Waits for the specified number of frames when XR is active and the test is XR compatible.
        /// This is useful for allowing XR eye textures and display settings to properly update after configuration changes.
        /// Only yields frames when ENABLE_VR and USE_XR_MOCK_HMD are defined, XRGraphicsAutomatedTests is enabled, and test is XR compatible.
        /// Otherwise, completes immediately without waiting.
        /// </summary>
        /// <param name="xrCompatible">Whether the test scene is compatible with XR.</param>
        /// <param name="frameCount">Number of frames to wait.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        static public IEnumerator WaitForXRFrames(bool xrCompatible, int frameCount)
        {
        #if ENABLE_VR && USE_XR_MOCK_HMD
            if (XRGraphicsAutomatedTests.enabled && xrCompatible)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
        #endif
            yield break;
        }

        /// <summary>
        /// Extracts the target resolution from image comparison settings.
        /// Returns the specified target width/height, or defaults to 1920x1080 if using back buffer.
        /// </summary>
        /// <param name="settings">Image comparison settings.</param>
        /// <returns>Resolution as (width, height) tuple.</returns>
        static private (int width, int height) GetResolutionFromSettings(UnityEngine.TestTools.Graphics.ImageComparisonSettings settings)
        {
            int w = 1920;
            int h = 1080;

            if (!settings.UseBackBuffer)
            {
                w = settings.TargetWidth;
                h = settings.TargetHeight;
            }

            return (w, h);
        }
    }
}
