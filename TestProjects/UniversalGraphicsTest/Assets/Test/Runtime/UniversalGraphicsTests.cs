using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using System.IO;
using System;

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
        var settings = UnityEngine.Object.FindObjectOfType<UniversalGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name.Substring(3, 4).Equals("_xr_"))
        {
#if ENABLE_VR && ENABLE_VR_MODULE
            Assume.That((Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer), "Stereo Universal tests do not run on MacOSX.");

            XRSettings.LoadDeviceByName("MockHMD");
            yield return null;

            XRSettings.enabled = true;
            yield return null;

            XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
            yield return null;

            foreach (var camera in cameras)
                camera.stereoTargetEye = StereoTargetEyeMask.Both;
#else
            yield return null;
#endif
        }
        else
        {
#if ENABLE_VR && ENABLE_VR_MODULE
            XRSettings.enabled = false;
#endif
            yield return null;
        }

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

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

        //Hack to revert (output yamato result)
        if (testCase.ScenePath.Contains("005"))
        {
            var desc = new RenderTextureDescriptor(640, 320, SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR), 24);
            var rt = RenderTexture.GetTemporary(desc);

            foreach (var camera in cameras)
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.targetTexture = null;
            }

            Texture2D actual = null;
            actual = new Texture2D(640, 320, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            actual.ReadPixels(new Rect(0, 0, 640, 320), 0, 0);
            RenderTexture.active = null;
            actual.Apply();

            var basePath = "C:/build/output/Unity-Technologies/ScriptableRenderPipeline/TestProjects/UniversalGraphicsTest/test-results/";
            string guid = Guid.NewGuid().ToString("N").Substring(0, 4);
            var outputPath = System.IO.Path.Combine(basePath, "005_Hack_" + guid + ".png");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            byte[] bytes = actual.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);
        }


        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);

#if CHECK_ALLOCATIONS_WHEN_RENDERING
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
#endif

    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
