using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.Networking.PlayerConnection;

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
    public IEnumerator RunIndividualTests(Material mat, bool isPerspective, Texture2D refImage, ImageComparisonSettings settings, Mesh customMesh = null) //reference image, test hash, reference hash
    {
        // Always wait one frame for scene load
        yield return null;
        if (!isPerspective)
        {
            camera.orthographic = true;
            camera.orthographicSize = 3;
        }
        else
        {
            camera.orthographic = false;

        }
        if (customMesh != null)
            mesh.GetComponent<MeshFilter>().mesh = customMesh;
        if (mat != null)
            sphereRenderer.material = mat;
        Debug.Log(mat.name + " " + isPerspective + " " + settings.ToString());
        try {
            ImageAssert.AreEqual(refImage, camera, settings);
        } catch(AssertionException) {
            UpdatedTestAssetMessage updatedMessage = new UpdatedTestAssetMessage(/* TODO: Populate */);
#if UNITY_EDITOR
            // TODO: Write to file
            Debug.Log("Writing to a file.....");
#else
            PlayerConnection.instance.Send(UpdatedTestAssetMessage.MessageId, updatedMessage.Serialize());
#endif
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
