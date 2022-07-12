using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class GraphicsTests
{

    private const string UniversalPackagePath = "Assets/ReferenceImages";


    [UnityTest, Category("RenderingExamplesTest")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(UniversalPackagePath)]
    

    public IEnumerator Runtime(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;
        
        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<GlobalGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find GlobalGraphicsTestSettings"); // Asset that needs to be added to Main Camera in every scene

        Scene scene = SceneManager.GetActiveScene();

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
