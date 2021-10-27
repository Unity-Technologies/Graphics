using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_ParametricReflectionProbeTests
{
    private readonly string sceneOutputPath = "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/ReflectionProbeBake";
    private readonly string sceneFileName = "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/ReflectionProbeBake.unity";
    private readonly string[] foldersToLookIn = { "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/settings" };

    public enum BakeAPI
    {
        Bake = 0,
        BakeAll,
        BakeSingle
    }

    [UnityEngine.Scripting.Preserve]
    private static object[] GetReflectionProbeTestCases()
    {
        object[] testCaseArray = new object[6];
        for (int i = 0; i < testCaseArray.Length; ++i)
        {
            string settings = "nonAuto-Progressive";
            BakeAPI bakeAPI = BakeAPI.Bake;

            switch (i)
            {
                case 0:
                    settings = "nonAuto-Progressive";
                    bakeAPI = BakeAPI.Bake;
                    break;
                case 1:
                    settings = "nonAuto-nonProgressive";
                    bakeAPI = BakeAPI.Bake;
                    break;
                case 2:
                    settings = "nonAuto-Progressive";
                    bakeAPI = BakeAPI.BakeAll;
                    break;
                case 3:
                    settings = "nonAuto-nonProgressive";
                    bakeAPI = BakeAPI.BakeAll;
                    break;
                case 4:
                    settings = "nonAuto-Progressive";
                    bakeAPI = BakeAPI.BakeSingle;
                    break;
                case 5:
                    settings = "nonAuto-nonProgressive";
                    bakeAPI = BakeAPI.BakeSingle;
                    break;
            }
            testCaseArray[i] = new object[] { settings, bakeAPI };
        }
        return testCaseArray;
    }

    [TestCaseSource("GetReflectionProbeTestCases")]
    public void RefProbeAPI(string settings, BakeAPI bakeAPI)
    {
        EditorSceneManager.OpenScene(sceneFileName, OpenSceneMode.Single);

        // Bake with a lighting settings asset.
        string[] settingsAssets = AssetDatabase.FindAssets(settings + " t:lightingsettings", foldersToLookIn);
        Debug.Log("Found " + settingsAssets.Length + " matching lighting settings assets in " + foldersToLookIn[0]);
        Assert.That(settingsAssets.Length, Is.EqualTo(1));
        string lsaPath = AssetDatabase.GUIDToAssetPath(settingsAssets[0]);
        Debug.Log("Loading " + lsaPath);
        LightingSettings lightingSettings = (LightingSettings)AssetDatabase.LoadAssetAtPath(lsaPath, typeof(LightingSettings));
        Lightmapping.lightingSettings = lightingSettings;
        string fileName = System.IO.Path.GetFileNameWithoutExtension(lsaPath);
        Assert.That(fileName, Is.EqualTo(settings));
        Lightmapping.Clear();
        // The disk cache needs clearing between runs because we are only changing the API and not necessarily the settings.
        // Changing the API use to bake the probe is assumed to not affect the result so the reflection probe is fetched from the disk cache.
        // To detect that everything works as intended the cached reflection probe needs to be cleared.
        Lightmapping.ClearDiskCache();
        Debug.Log("Baking " + fileName);
        bool result = true;
        switch (bakeAPI)
        {
            case BakeAPI.Bake:
                result = Lightmapping.Bake();
                break;
            case BakeAPI.BakeAll:
            {
                var probe = Object.FindObjectOfType<ReflectionProbe>();
                Assert.That(probe, !Is.EqualTo(null), "Couldn't find ReflectionProbe");
                Debug.Log("Found reflection probe: " + probe.name);

                var oldEnabledValue = probe.enabled;
                probe.enabled = false;
                result = Lightmapping.Bake();
                probe.enabled = oldEnabledValue;
                result &= LightmappingExt.BakeAllReflectionProbesSnapshots();
            }
            break;
            case BakeAPI.BakeSingle:
            {
                var probe = Object.FindObjectOfType<ReflectionProbe>();
                Assert.That(probe, !Is.EqualTo(null), "Couldn't find ReflectionProbe");
                Debug.Log("Found reflection probe: " + probe.name);

                var oldEnabledValue = probe.enabled;
                probe.enabled = false;
                result = Lightmapping.Bake();
                probe.enabled = oldEnabledValue;
                result &= LightmappingExt.BakeReflectionProbeSnapshot(probe);
            }
            break;
        }
        Assert.That(result, Is.True);

        // Get Test settings.
        var graphicsTestSettingsCustom = Object.FindObjectOfType<UniversalGraphicsTestSettings>();
        Assert.That(graphicsTestSettingsCustom, !Is.EqualTo(null), "Couldn't find GraphicsTestSettingsCustom");

        // Load reference image.
        var referenceImagePath = System.IO.Path.Combine("Assets/ReferenceImages", string.Format("{0}/{1}/{2}/{3}/{4}",
            UseGraphicsTestCasesAttribute.ColorSpace,
            UseGraphicsTestCasesAttribute.Platform,
            UseGraphicsTestCasesAttribute.GraphicsDevice,
            UseGraphicsTestCasesAttribute.LoadedXRDevice,
            "RefProbeAPI_" + settings + "-" + bakeAPI.ToString() + "_.png"));

        Debug.Log("referenceImagePath " + referenceImagePath);
        var referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);

        // Compare screenshots.
        GraphicsTestCase testCase = new GraphicsTestCase(settings, referenceImage);
        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), graphicsTestSettingsCustom.ImageComparisonSettings);
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        // Remove all lightmaps.
        System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(sceneOutputPath);
        foreach (System.IO.FileInfo file in di.GetFiles())
            file.Delete();
        foreach (System.IO.DirectoryInfo dir in di.GetDirectories())
            dir.Delete(true);
        System.IO.Directory.Delete(sceneOutputPath);
    }
}
