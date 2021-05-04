using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.Networking.PlayerConnection;
using System;
#if UNITY_EDITOR
using System.IO;
#endif

public class ShaderGraphIndividualTests
{
    [OneTimeSetUp]
    public void SetupTestScene()
    {
        var cameraGameObject = new GameObject();
        mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mesh.transform.position = new Vector3(0, 0, 3f);
        mesh.transform.rotation = Quaternion.Euler(0, 0, 0);
        mesh.transform.localScale = new Vector3(2f, 2f, 2f);
        sphereRenderer = mesh.GetComponent<Renderer>();
        camera = cameraGameObject.AddComponent<Camera>();
        camera.transform.position = Vector3.zero;
    }

    private Camera camera;
    private Renderer sphereRenderer;
    private GameObject mesh;


    [UnityTest, Category("ShaderGraph")]
    [PrebuildSetup("SetupTestAssetTestCases")]
    [UseTestAssetTestCase]
    public IEnumerator RunIndividualTests(TestAssetTestData data) //reference image, test hash, reference hash
    {
        
        // Always wait one frame for scene load
        yield return null;
        if (!data.isCameraPersective)
        {
            camera.orthographic = true;
            camera.orthographicSize = 3;
        }
        else
        {
            camera.orthographic = false;

        }
        if (data.customMesh != null)
            mesh.GetComponent<MeshFilter>().mesh = data.customMesh;
        if (data.testMaterial != null)
            sphereRenderer.material = data.testMaterial;
        try
        {
            ImageAssert.AreEqual(data.referenceImage, camera, data.imageComparisonSettings);
            if (!data.SavedResultUpToDate())
            {
#if UNITY_EDITOR
                data.UpdateResult();
                File.AppendAllLines("Assets/Resources/UpdateTests.txt", new string[] { $"{data.testName}-{data.testMaterial.name},{data.FilePath},False" });

#else
                UpdatedTestAssetMessage updatedMessage = new UpdatedTestAssetMessage();
                updatedMessage.testData = data;
                updatedMessage.expectsResultImage = false;
                PlayerConnection.instance.Send(UpdatedTestAssetMessage.MessageId, updatedMessage.Serialize());
#endif
            }

        }
        catch (Exception e)
        {

            if (!data.SavedResultUpToDate())
            {
#if UNITY_EDITOR
                data.UpdateResult();
                File.AppendAllLines("Assets/Resources/UpdateTests.txt", new string[] { $"{data.testName}-{data.testMaterial.name},{data.FilePath},True"});

#else
                UpdatedTestAssetMessage updatedMessage = new UpdatedTestAssetMessage();
                updatedMessage.testData = data;
                updatedMessage.expectsResultImage = true;
                PlayerConnection.instance.Send(UpdatedTestAssetMessage.MessageId, updatedMessage.Serialize());
#endif
            }
            throw e;
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
