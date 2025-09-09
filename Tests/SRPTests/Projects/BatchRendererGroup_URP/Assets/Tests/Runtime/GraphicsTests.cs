
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class GraphicsTests
{
#if UNITY_ANDROID
        static bool wasFirstSceneRan = false;
        const int firstSceneAdditionalFrames = 3;
#endif
    public const string path = "Assets/ReferenceImages";

#if UNITY_WEBGL || UNITY_ANDROID
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return RuntimeGraphicsTestCaseProvider.EnsureGetReferenceImageBundlesAsync();
    }
#endif

    [UnityTest, Category("GraphicsTest")]
#if UNITY_EDITOR
    [PrebuildSetup("SetupGraphicsTestCases")]
#endif
    [UseGraphicsTestCases(path)]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        if (testCase.ScenePath.Contains("ErrorMaterial"))
        {
            LogAssert.ignoreFailingMessages = true;
        }

#if UNITY_WEBGL || UNITY_ANDROID
        // Do this near the beginning of the test case method before you test or assert
        RuntimeGraphicsTestCaseProvider.AssociateReferenceImageWithTest(testCase);
#endif
		Debug.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImagePathLog}.");
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        //Get Test settings
        //ignore instead of failing, because some scenes might not be used for GraphicsTest
        var settings = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
        if (settings == null) Assert.Ignore("Ignoring this test for GraphicsTest because couldn't find GraphicsTestSettingsCustom");

#if !UNITY_EDITOR
        Screen.SetResolution(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight, false);
#endif

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());

        yield return null;

        // Add a maximum amount of wait frames to avoid infinite loop
        int maxWaitFrames = 300;
        while (settings.Wait && maxWaitFrames > 0)
        {
            maxWaitFrames--;
            yield return new WaitForEndOfFrame();
        }

        int waitFrames = settings.WaitFrames;

        if (settings.ImageComparisonSettings.UseBackBuffer && settings.WaitFrames < 1)
        {
            waitFrames = 1;
        }

        if (XRGraphicsAutomatedTests.enabled)
        {
            waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(true, waitFrames, settings.ImageComparisonSettings);
        }

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

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings, testCase.ReferenceImagePathLog);
    }

    [TearDown]
    public void DumpImagesInEditor()
    {
#if UNITY_EDITOR
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
#endif

        XRGraphicsAutomatedTests.running = false;
    }
}
