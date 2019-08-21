using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class LWGraphicsTests
{

    public const string lwPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("LightWeightRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(lwPackagePath)]
    

    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<LWGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find LWGraphicsTestSettings");
        
        // Stereo screen capture on Mac generates monoscopic images and won't be fixed.
        Assume.That((Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer), "Stereo tests do not run on MacOSX.");
            
        XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
        yield return null;
        
        foreach (var camera in cameras)
            camera.stereoTargetEye = StereoTargetEyeMask.Both;

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
