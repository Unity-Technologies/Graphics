
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

    [UnityTest, Category("GraphicsTest")]
    [SceneGraphicsTest("Assets/SampleScenes")]
    [IgnoreGraphicsTest("ErrorMaterial", "Ignoring this specially designed test that fails to build the build", isInclusive: true, runtimePlatforms: new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        if (testCase.ScenePath.Contains("ErrorMaterial"))
        {
            LogAssert.ignoreFailingMessages = true;
        }

        GraphicsTestLogger.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImage.LoadMessage}.");
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

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, cameras.Where(x => x != null), settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
    }

    [TearDown]
    public void DumpImagesInEditor()
    {
        XRGraphicsAutomatedTests.running = false;
    }
}
