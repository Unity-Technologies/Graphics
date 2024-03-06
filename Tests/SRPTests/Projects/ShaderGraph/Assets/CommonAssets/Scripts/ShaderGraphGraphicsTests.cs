using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class ShaderGraphGraphicsTests
{
#if UNITY_WEBGL || UNITY_ANDROID
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return RuntimeGraphicsTestCaseProvider.EnsureGetReferenceImageBundlesAsync();
    }
#endif

    [UnityTest, Category("ShaderGraph")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        Debug.Log($"Running test case {testCase.ScenePath} with reference image {testCase.ScenePath}. {testCase.ReferenceImagePathLog}.");
#if UNITY_WEBGL || UNITY_ANDROID
        RuntimeGraphicsTestCaseProvider.AssociateReferenceImageWithTest(testCase);
#endif

        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var settings = Object.FindObjectOfType<ShaderGraphGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find ShaderGraphGraphicsTestSettings");
        settings.OnTestBegin();

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings);
        settings.OnTestComplete();
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
