using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

public class BuiltInGraphicsTests
{
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;

    [UnityTest, Category("BuiltInRP")]
    [SceneGraphicsTest("Assets/Scenes")]
    [IgnoreGraphicsTest("051_Shader_Graphs_canvas", "Disabled from Test Filters")]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
		Debug.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImagePathLog}.");
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<BuiltInGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find BuiltInGraphicsTestSettings");

        int waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(settings.XRCompatible, settings.WaitFrames, settings.ImageComparisonSettings);

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
            for (int i = 0; i < firstSceneAdditionalFrames; i++)
            {
                yield return new WaitForEndOfFrame();
            }
            wasFirstSceneRan = true;
        }
#endif

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, cameras.Where(x => x != null), settings.ImageComparisonSettings, testCase.ReferenceImagePathLog);

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

#if UNITY_EDITOR && ENABLE_VR
    [TearDown]
    public void TearDownXR()
    {
        XRGraphicsAutomatedTests.running = false;
    }
#endif
}
