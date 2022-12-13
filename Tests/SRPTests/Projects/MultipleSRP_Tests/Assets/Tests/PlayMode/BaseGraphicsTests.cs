using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

public class BaseGraphicsTests
{
    [UnityTest, Category("Base")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        var srpTestCase = testCase.SRPAsset != null;
        var defaultSRPAsset = GraphicsSettings.defaultRenderPipeline;
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var settings = Object.FindObjectOfType<CrossPipelineTestsSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find CrossPipelineTestsSettings");

        if (srpTestCase && testCase.SRPAsset != defaultSRPAsset)
        {
            // set it to the Test Case SRPAsset
            GraphicsSettings.defaultRenderPipeline = testCase.SRPAsset;
            Debug.Log($"Setting SRP to {testCase.SRPAsset.name}");
        }

        for (var i = 0; i < settings.WaitFrames; i++)
            yield return null;

        try
        {
            ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings);
        }
        finally
        {
            // Need to reset the SRP pipeline here
            GraphicsSettings.defaultRenderPipeline = defaultSRPAsset;
        }
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
