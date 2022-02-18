using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_Bake_Tests
{
    static void clearAll()
    {
        Lightmapping.Clear();
        Lightmapping.ClearDiskCache();
        Lightmapping.ClearLightingDataAsset();
    }

    protected static void SetupLightingSettings()
    {
        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);
        Assert.IsTrue(lightingSettings != null, "Lighting settings are available.");
        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        lightingSettings.prioritizeView = true;
        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper.");
        Assert.IsTrue(lightingSettings.prioritizeView, "Using Progressive.");
    }

    [UnityTest]
    public IEnumerator BakingWithATrousFiltering_DoesNotFallback()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/BakeATrous.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        lightingSettings.mixedBakeMode = MixedLightingMode.IndirectOnly;
        lightingSettings.directionalityMode = LightmapsMode.NonDirectional;

        // Activate A-Trous filtering
        lightingSettings.filterTypeIndirect = LightingSettings.FilterType.ATrous;

        Lightmapping.Clear();
        Lightmapping.Bake();

        // Check that we did not fallback to CPULM
        Assert.AreEqual(lightingSettings.lightmapper, LightingSettings.Lightmapper.ProgressiveGPU);

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    // Case1356606
    [Test]
    public void GPULM_BakeSceneWithOnlyBlackLight_DoesNotFallbackToCPU()
    {
        // Open the initial scene
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/GPU-Rebake.unity");

        // Clear baked data and cache
        clearAll();

        // VerifySettings are correct
        SetupLightingSettings();

        // Get the settings to be able to check for a fallback to CPU
        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);
        Assert.IsTrue(lightingSettings != null, "Lighting settings are available.");

        // Get the light
        Light light = GameObject.Find("Directional Light").GetComponent<Light>();
        Assert.IsNotNull(light);

        // Set the light color
        light.color = Color.black;

        // Bake the scene GI
        Lightmapping.Bake();

        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after initial bake.");

        clearAll();
    }
}
