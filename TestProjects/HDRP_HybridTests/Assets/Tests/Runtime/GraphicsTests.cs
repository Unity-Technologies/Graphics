//This script is copied from UniversalRP TestProject 
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/UniversalGraphicsTest/Assets/Test/Runtime/UniversalGraphicsTests.cs

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using Unity.Entities;

public class GraphicsTests
{
    [UnityTest, Category("Custom Graphics Tests")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        CleanUp();
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        //Get Test settings
        //ignore instead of failing, because some scenes might not be used for GraphicsTest
        var settings = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
        if (settings == null) 
        {
           // CleanUp();
            Assert.Ignore("Ignoring this test for GraphicsTest because couldn't find GraphicsTestSettingsCustom");
        }
        //Assert.IsNotNull(settings, "Invalid test scene, couldn't find GraphicsTestSettingsCustom");

        Screen.SetResolution(settings.ImageComparisonSettings.TargetWidth, settings.ImageComparisonSettings.TargetHeight, false);

        // Get the test camera
        GameObject[] camObjects = GameObject.FindGameObjectsWithTag("MainCamera");
        var cameras = camObjects.Select(x=>x.GetComponent<Camera>());

        int waitFrames = settings.WaitFrames;

        if (XRGraphicsAutomatedTests.enabled)
        {
            waitFrames = Unity.Testing.XR.Runtime.ConfigureMockHMD.SetupTest(true, waitFrames, settings.ImageComparisonSettings);
        }

        // WaitFrames according to settings
        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

        // Test Assert
        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);

        // Always wait one frame for scene load
        yield return new WaitForEndOfFrame();
    }


    [TearDown]
    public void DumpImagesInEditor()
    {
        #if UNITY_EDITOR
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
        #endif
        
        CleanUp();

        XRGraphicsAutomatedTests.running = false;
    }

    public void CleanUp()
    {
        EntityManager m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_Manager.DestroyEntity(m_Manager.GetAllEntities());
    }

}
