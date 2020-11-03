using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

public class UniversalGraphicsTests
{
#if UNITY_ANDROID
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;
#endif

    public const string universalPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("UniversalRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(universalPackagePath)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<UniversalGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

        int waitFrames = settings.WaitFrames;

        if (XRGraphicsAutomatedTests.enabled)
        {
            int xrWaitFrames = ConfigureXR(settings.XRCompatible, settings.ImageComparisonSettings);
            waitFrames = Mathf.Max(xrWaitFrames, waitFrames);
        }

        Scene scene = SceneManager.GetActiveScene();

        yield return null;

        if (settings.ImageComparisonSettings.UseBackBuffer && waitFrames < 1)
            waitFrames = 1;

        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

#if UNITY_ANDROID
        // On Android first scene often needs a bit more frames to load all the assets
        // otherwise the screenshot is just a black screen
        if (!wasFirstSceneRan)
        {
            for(int i = 0; i < firstSceneAdditionalFrames; i++)
            {
                yield return null;
            }
            wasFirstSceneRan = true;
        }
#endif

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);

        // Does it allocate memory when it renders what's on the main camera?
        bool allocatesMemory = false;
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

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

    int ConfigureXR(bool xrCompatible, ImageComparisonSettings settings)
    {
#if ENABLE_VR
        if (xrCompatible)
        {
            Debug.Log("Testing XR code path with MockHMD.");
            XRGraphicsAutomatedTests.running = true;

            // Validate MockHMD is enabled and running
            List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(xrDisplays);
            Assume.That(xrDisplays.Count == 1 && xrDisplays[0].running, "XR display MockHMD is not running!");

            // Configure MockHMD to use single-pass and compare reference image against second view (right eye)
            xrDisplays[0].SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.RightEye);

            // Configure MockHMD stereo mode
            xrDisplays[0].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
            Unity.XR.MockHMD.MockHMD.SetRenderMode(Unity.XR.MockHMD.MockHMDBuildSettings.RenderMode.SinglePassInstanced);

            // Configure MockHMD to match the original settings from the test scene
            ImageAssert.GetImageResolution(settings, out int w, out int h);
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
        return 4;
#endif
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
