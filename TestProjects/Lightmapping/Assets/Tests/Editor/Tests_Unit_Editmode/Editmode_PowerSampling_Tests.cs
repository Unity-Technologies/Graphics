using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.TestTools.Graphics;

public class Editmode_PowerSampling_Tests
{
    private readonly string sceneOutputPath = "Assets/Tests/Editor/Tests_Unit_Editmode/ProgressiveUpdates";
    private readonly string sceneFileName = "Assets/Tests/Editor/Tests_Unit_Editmode/ProgressiveUpdates.unity";

    static void clearAll()
    {
        Lightmapping.Clear();
        Lightmapping.ClearDiskCache();
        Lightmapping.ClearLightingDataAsset();
    }

    void makeTextureReadable(string assetPath)
    {
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.textureCompression = TextureImporterCompression.Uncompressed;
            tImporter.isReadable = true;
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }

    void takeScreenshot(string filename)
    {
        Camera camera = GameObject.FindGameObjectsWithTag("MainCamera")[0].GetComponent<Camera>();

        int resWidthN = 2048;
        int resHeightN = 1024;
        RenderTexture rt = new RenderTexture(resWidthN, resHeightN, 24);
        camera.targetTexture = rt;
        TextureFormat tFormat;
        tFormat = TextureFormat.RGB24;

        Texture2D screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        byte[] bytes = screenShot.EncodeToPNG();
        string fullpath = sceneOutputPath + "/" + filename;

        System.IO.File.WriteAllBytes(fullpath, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", fullpath));
        Application.OpenURL(fullpath);
    }

    // Case 1352923
    [Test]
    public void GPULM_BakeSceneWithProgressiveUpdates_MatchesBakeWithoutProgressiveUpdates()
    {
        // Open the initial scene
        EditorSceneManager.OpenScene(sceneFileName);

        // Clear baked data and cache
        clearAll();

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);
        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");
        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;

        lightingSettings.numRaysToShootPerTexel = 1;

        int startSPP = 8;
        int nbIteration = 5;
        int spp = startSPP;

        lightingSettings.prioritizeView = true;
        for (int i = 0; i < nbIteration; i++)
        {
            lightingSettings.directSampleCount = spp;
            lightingSettings.indirectSampleCount = spp;
            lightingSettings.environmentSampleCount = spp;

            // Bake the scene GI
            Lightmapping.Bake();

            Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after initial bake.");

            takeScreenshot("on_" + spp + ".png");

            spp = spp * 2;
            clearAll();
        }

        lightingSettings.prioritizeView = false;
        spp = startSPP;
        for (int i = 0; i < nbIteration; i++)
        {
            lightingSettings.directSampleCount = spp;
            lightingSettings.indirectSampleCount = spp;
            lightingSettings.environmentSampleCount = spp;

            // Bake the scene GI
            Lightmapping.Bake();

            Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after initial bake.");

            takeScreenshot("off_" + spp + ".png");

            spp = spp * 2;
            clearAll();
        }

        // Get Test settings.
        var graphicsTestSettingsCustom = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
        Assert.That(graphicsTestSettingsCustom, !Is.EqualTo(null), "Couldn't find GraphicsTestSettingsCustom");

        AssetDatabase.Refresh();

        spp = startSPP;
        for (int i = 0; i < nbIteration; i++)
        {
            string offImagePath = sceneOutputPath + "/off_" + spp + ".png";
            string onImagePath = sceneOutputPath + "/on_" + spp + ".png";
            var offImage = AssetDatabase.LoadAssetAtPath<Texture2D>(offImagePath);
            var onImage = AssetDatabase.LoadAssetAtPath<Texture2D>(onImagePath);
            makeTextureReadable(offImagePath);
            makeTextureReadable(onImagePath);

            Debug.Log("Compare " + spp);
            ImageAssert.AreEqual(offImage, onImage, graphicsTestSettingsCustom.ImageComparisonSettings);
            spp = spp * 2;
        }

        clearAll();
        AssetDatabase.DeleteAsset(sceneOutputPath);
    }
}
