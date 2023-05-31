using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if ENABLE_VR && USE_XR_MOCK_HMD
using UnityEngine.XR;
#endif

namespace Unity.Testing.XR.Runtime
{
    public class ConfigureMockHMD
    {
        static public int SetupTest(bool xrCompatible, int waitFrames, UnityEngine.TestTools.Graphics.ImageComparisonSettings settings)
        {
#if ENABLE_VR && USE_XR_MOCK_HMD
            if (XRGraphicsAutomatedTests.enabled)
            {
                if (xrCompatible)
                {
                    XRGraphicsAutomatedTests.running = true;

                    // Validate MockHMD is enabled and running
                    List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
                    SubsystemManager.GetSubsystems(xrDisplays);
                    Assume.That(xrDisplays.Count == 1 && xrDisplays[0].running, "XR display MockHMD is not running!");

                    // Configure MockHMD to use single-pass and compare reference image against second view (right eye)
                    xrDisplays[0].SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.RightEye);

                    // Configure MockHMD stereo mode
                    xrDisplays[0].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                    Unity.XR.MockHMD.MockHMD.SetRenderMode(Unity.XR.MockHMD.MockHMDBuildSettings.RenderMode.SinglePassInstanced);

                    // Configure MockHMD to match the original settings from the test scene
                    UnityEngine.TestTools.Graphics.ImageAssert.GetImageResolution(settings, out int w, out int h);
                    Unity.XR.MockHMD.MockHMD.SetEyeResolution(w, h);
                    Unity.XR.MockHMD.MockHMD.SetMirrorViewCrop(0.0f);

#if UNITY_EDITOR
                    UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.SetGameViewSize(w, h);
#else
                    Screen.SetResolution(w, h, FullScreenMode.Windowed);
#endif
                }
                else
                {
                    Assert.Ignore("Test scene is not compatible with XR and will be skipped.");
                }

                // XR plugin MockHMD requires a few frames to resize eye textures
                return Mathf.Max(waitFrames, 4);
            }
#endif

            return waitFrames;
        }
    }
}
