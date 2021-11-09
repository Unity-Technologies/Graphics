using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_BakeRestart_Tests
{
    public System.Collections.Generic.IEnumerable<bool> RunABake(float convergenceStop)
    {
        // Wait a few frames for the restart (cause a preparing stage)
        int frameCounter = 0;
        while (!Lightmapping.isPreparing)
        {
            frameCounter++;
            yield return false;
        }

        // Wait for the bake to start the baking stage
        while (!Lightmapping.isBaking)
        {
            frameCounter++;
            yield return false;
        }

        //Debug.Log("Start baking at frame "+frameCounter);

        // Let it bake for a few frames
        frameCounter = 0;
        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        while (lc.progress < convergenceStop)
        {
            frameCounter++;
            lc = Lightmapping.GetLightmapConvergence(0);
            // Debug.Log(lc.progress);
            yield return false;
        }
        //Debug.Log("end baking at frame "+frameCounter);
        Assert.IsTrue(frameCounter > 0);

        yield return true;
    }

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
    public IEnumerator ActivateDirectLighting_DuringABake_DoesNotFallback()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/BakeRestartScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        lightingSettings.mixedBakeMode = MixedLightingMode.IndirectOnly;
        lightingSettings.directionalityMode = LightmapsMode.NonDirectional;

        GameObject dirLightGO = GameObject.FindGameObjectsWithTag("TheLight")[0];
        dirLightGO.SetActive(false);
        Light light = dirLightGO.GetComponent(typeof(Light)) as Light;
        light.lightmapBakeType = LightmapBakeType.Baked;

        Lightmapping.Clear();
        Lightmapping.BakeAsync();

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Make sure it is still baking before we
        // switch the light on and restart the bake
        Assert.IsTrue(Lightmapping.isBaking);

        // Activate the light, cause the bake to restart
        dirLightGO.SetActive(true);

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Check that baking is still running
        Assert.IsTrue(Lightmapping.isBaking);
        // Check that we did not fallback to CPULM
        Assert.AreEqual(lightingSettings.lightmapper, LightingSettings.Lightmapper.ProgressiveGPU);

        Lightmapping.Cancel();
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator ActivateAO_DuringABake_DoesNotFallback()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/BakeRestartScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        GameObject dirLightGO = GameObject.FindGameObjectsWithTag("TheLight")[0];
        dirLightGO.SetActive(false);
        lightingSettings.ao = false;

        Lightmapping.Clear();
        Lightmapping.BakeAsync();

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Make sure it is still baking before we
        // switch the AO on and restart the bake
        Assert.IsTrue(Lightmapping.isBaking);

        // Switch AO on, cause the bake to restart
        lightingSettings.ao = true;

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Check that baking is still running
        Assert.IsTrue(Lightmapping.isBaking);
        // Check that we did not fallback to CPULM
        Assert.AreEqual(lightingSettings.lightmapper, LightingSettings.Lightmapper.ProgressiveGPU);

        Lightmapping.Cancel();
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator ActivateShadowmask_DuringABake_DoesNotFallback()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/BakeRestartScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        lightingSettings.mixedBakeMode = MixedLightingMode.Shadowmask;

        GameObject dirLightGO = GameObject.FindGameObjectsWithTag("TheLight")[0];
        dirLightGO.SetActive(true);
        Light light = dirLightGO.GetComponent(typeof(Light)) as Light;
        light.lightmapBakeType = LightmapBakeType.Baked;

        Lightmapping.Clear();
        Lightmapping.BakeAsync();

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Make sure it is still baking before we
        // switch the light mode and restart the bake
        Assert.IsTrue(Lightmapping.isBaking);

        // Switch the light to shadowmask, cause the bake to restart
        light.lightmapBakeType = LightmapBakeType.Mixed;

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Check that baking is still running
        Assert.IsTrue(Lightmapping.isBaking);
        // Check that we did not fallback to CPULM
        Assert.AreEqual(lightingSettings.lightmapper, LightingSettings.Lightmapper.ProgressiveGPU);

        Lightmapping.Cancel();
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [Test] // Case1356714
    public void GPULM_ChangeLightBetweenBakes_DoesNotFallbackToCPU()
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
        light.color = Color.red;

        // Bake the scene GI
        Lightmapping.Bake();

        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after initial bake.");

        // Change the light color
        light.color = Color.blue;

        //Bake the scene GI
        Lightmapping.Bake();

        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after subsequent bake.");

        clearAll();
    }

    [Test] // Case1356714
    public void GPULM_ChangeSamplesBetweenBakes_DoesNotFallbackToCPU()
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

        lightingSettings.directSampleCount = 8;

        // Bake the scene GI
        Lightmapping.Bake();

        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after initial bake.");

        lightingSettings.directSampleCount = 16;

        //Bake the scene GI
        Lightmapping.Bake();

        Assert.IsTrue(lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU, "Using GPU Lightmapper after subsequent bake.");

        clearAll();
    }

    [UnityTest] // Case 1364204
    public IEnumerator RadeonProDenoiser_DisablingRadeonProDenoiserDuringABake_DoesNotFallbackToCPU()
    {
        // Open the initial scene
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/BakeRestartScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        lightingSettings.prioritizeView = true;

        Lightmapping.lightingSettings.filteringMode = LightingSettings.FilterMode.Advanced;
        Lightmapping.lightingSettings.denoiserTypeDirect = LightingSettings.DenoiserType.RadeonPro;
        Lightmapping.lightingSettings.denoiserTypeIndirect = LightingSettings.DenoiserType.RadeonPro;
        Lightmapping.lightingSettings.denoiserTypeAO = LightingSettings.DenoiserType.RadeonPro;

        Lightmapping.Clear();
        Lightmapping.BakeAsync();

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Make sure it is still baking before we turn off denoising
        Assert.IsTrue(Lightmapping.isBaking);

        // Disable radeon pro denoiser and restart the bake manually
        Lightmapping.lightingSettings.filteringMode = LightingSettings.FilterMode.None;
        Lightmapping.Clear();
        Lightmapping.BakeAsync();

        foreach (bool b in RunABake(0.2f))
        {
            yield return null;
        }

        // Check that baking is still running
        Assert.IsTrue(Lightmapping.isBaking);
        // Check that we did not fallback to CPULM
        Assert.AreEqual(lightingSettings.lightmapper, LightingSettings.Lightmapper.ProgressiveGPU);

        Lightmapping.Cancel();
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }
}
