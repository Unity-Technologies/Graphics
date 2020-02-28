using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class ShaderGraphGraphicsTests
{
    [UnityTest, Category("ShaderGraph")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var settings = Object.FindObjectOfType<ShaderGraphGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find ShaderGraphGraphicsTestSettings");
        int waitFrames = settings.WaitFrames;

        if (settings.ImageComparisonSettings.UseBackBuffer && settings.WaitFrames < 1)
        {
            waitFrames = 1;
        }
        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

        ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings);
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
