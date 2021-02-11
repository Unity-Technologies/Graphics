using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEditor;

public class ShaderGraphIndividualTests
{
    [OneTimeSetUp]
    public void SetupTestScene()
    {
        GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = Vector3.zero; //this is an example - just make a sphere in the scene. Could load a specific template scene as well
        
    }

    private Camera camera;
    private Renderer sphereRenderer;


    [UnityTest, Category("ShaderGraph")]
    [PrebuildSetup("SetupTestAssetTestCases")]
    //[UseGraphicsTestCases]
    [UseTestAssetTestCase]
    public IEnumerator RunIndividualTests(Material mat, bool isPerspective, Texture2D refImage, ImageComparisonSettings settings, Mesh customMesh = null) //reference image, test hash, reference hash
    {
        // Always wait one frame for scene load
        yield return null;
        Debug.Log(mat.name + " " + isPerspective + " " + settings.ToString());

        //Adding New Tests Materials
        //1.find shadergraphTestAsset and get test materials
        //2.Swap Materials
        //3.capture camera and save images
        //4.image comparison
        //var shaderGraphTestAssets = FindAssets<ShaderGraphTestAsset>("ShaderGraphTestAsset");
        //List<Material> testMaterials = new List<Material>();
       // foreach (var asset in shaderGraphTestAssets)
        //{
        //    if(asset.testMaterial!= null)
        //    {
        //        testMaterials.AddRange(asset.testMaterial);
        //    }
       // }
       // var sphereRenderer = Object.FindObjectOfType<Renderer>();

       // var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        //var settings = Object.FindObjectOfType<ShaderGraphIndividualTestSetting>();
       // Assert.IsNotNull(settings, "Invalid test scene, couldn't find ShaderGraphIndividualTestSetting");

       // for (int i = 0; i < settings.WaitFrames; i++)
       //     yield return null;

       // if (testMaterials != null)
       //     foreach (var mat in testMaterials)
       // {
       //     sphereRenderer.material = mat;
       //         //TODO: Save images with material/shader names
       //         Debug.Log("Captured image for " + mat.name);
       //         ImageAssert.AreEqual(testCase.ReferenceImage, camera, settings.ImageComparisonSettings);
       // }

    }
    public static List<T> FindAssets<T>(string type) where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets("t:"+type, null);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }
        return assets;
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

}
