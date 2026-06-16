using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class PostProcessingGraphicsTests
{
    [UnityTest, Category("PostProcessing")]
    [SceneGraphicsTest("Assets/Scenes")]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

#if UNITY_2020_3_OR_NEWER
        var settings = Object.FindAnyObjectByType<PostProcessingGraphicsTestSettings>();
#else
        var settings = Object.FindObjectOfType<PostProcessingGraphicsTestSettings>();
#endif

        Assert.IsNotNull(settings, "Invalid test scene, couldn't find PostProcessingGraphicsTestSettings");

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, settings.ImageComparisonSettings);
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        // TearDown is handled automatically by the graphics test framework
    }

#endif
}
