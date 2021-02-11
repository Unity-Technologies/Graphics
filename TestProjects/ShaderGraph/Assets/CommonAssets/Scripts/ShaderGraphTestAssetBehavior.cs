using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;
using System.Linq;

public class ShaderGraphTestAssetBehavior : MonoBehaviour
{
    public ShaderGraphTestAsset testAsset;

    Renderer m_ObjectRenderer;
    List<Material> allTestMaterials;
    //TODO: 
    //1. Swap material to the test material
    //2. Position the sphere to the camera
    //3. Take screenshot

    void Start()
    {
        m_ObjectRenderer = GetComponent<Renderer>();
        allTestMaterials = FindAllMaterials<Material>();

        if (testAsset.testMaterial != null)
        {
            foreach (var mat in testAsset.testMaterial)
            {
                m_ObjectRenderer.material = mat.material;
                var screenshotName = mat.material.name;
                ScreenCapture.CaptureScreenshot(Application.dataPath + "/Testing/Screenshots/" + screenshotName + ".png");
                Debug.Log(screenshotName);
            }
        }

    }

    void Update()
    {
        // StartCoroutine(IterateAndCapture(m_ObjectRenderer, allTestMaterials));
    }

    IEnumerator IterateAndCapture(Renderer renderer, List<Material> allMaterials)
    {
        int i = 0;
        foreach (var mat in allMaterials)
        {
            m_ObjectRenderer.material = mat;
            yield return new WaitForEndOfFrame();

            //var screenshotName = AssetDatabase.GetAssetPath(mat);
            var screenshotName = mat.name;
            i++;
            Debug.Log(i + " " + screenshotName);
            //ScreenCapture.CaptureScreenshot(Application.dataPath + "/Testing/Screenshots/" + screenshotName + ".png");
        }

        Debug.Log("Capturing finished!");
        Quit();
    }
    public static void Quit()
    {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static List<T> FindAllMaterials<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Testing/IntegrationTests/Graphs" });
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

}
