using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using EditorSceneManagement = UnityEditor.SceneManagement;
#endif

public class HDRP_GraphicTestRunner : IPrebuildSetup
{
    [UnityTest, Category("HDRP Graphic Tests")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i=0 ; i<5 ; ++i)
            yield return null;

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        Time.captureFramerate = settings.captureFramerate;

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        for (int i=0 ; i<settings.waitFrames ; ++i)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
    }

    public void Setup()
    {
#if UNITY_EDITOR
        // For each scene in the build settings, force build of the lightmaps if it has "DoLightmap" label.
        // Note that in the PreBuildSetup stage, TestRunner has already created a new scene with its testing monobehaviours

        Scene trScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(0);

        foreach( EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            var labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(sceneAsset));
            if ( labels.Contains("DoLightmap") )
            {

                EditorSceneManagement.EditorSceneManager.OpenScene(scene.path, EditorSceneManagement.OpenSceneMode.Additive);

                Scene currentScene = EditorSceneManagement.EditorSceneManager.GetSceneAt(1);

                EditorSceneManagement.EditorSceneManager.SetActiveScene(currentScene);
                
                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

                Lightmapping.Bake();
 
                EditorSceneManagement.EditorSceneManager.SaveScene( currentScene );

                EditorSceneManagement.EditorSceneManager.SetActiveScene(trScene);

                EditorSceneManagement.EditorSceneManager.CloseScene(currentScene, true);
            }
        }
#endif
    }

#if UNITY_EDITOR

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

}