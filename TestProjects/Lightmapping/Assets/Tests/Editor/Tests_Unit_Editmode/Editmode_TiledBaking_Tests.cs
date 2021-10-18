using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_TiledBaking_Tests
{
    //https://stackoverflow.com/questions/32809888/how-can-i-save-unity-statistics-or-unity-profiler-statistics-stats-on-cpu-rend

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_4_tiles_2_Empty_Tiles()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/TwoPlanes.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(4), "Max tiling pass num should be 4");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_4_tiles_1_Empty_Tile()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/ThreePlanes.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(4), "Max tiling pass num should be 4");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    public string scenePath = "Assets/Tests/Editor/Tests_Unit_Editmode/OnePlane.unity";

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_4_tiles()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;
        lightingSettings.lightmapResolution = 100;
        lightingSettings.lightmapMaxSize = 1024;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(4), "Max tiling pass num should be 4");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_16_tiles()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Sixtenth;
        lightingSettings.lightmapResolution = 100;
        lightingSettings.lightmapMaxSize = 1024;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(16), "Max tiling pass num should be 16");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_64_tiles()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.SixtyFourth;
        lightingSettings.lightmapResolution = 100;
        lightingSettings.lightmapMaxSize = 1024;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(64), "Max tiling pass num should be 64");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator TiledBaking_GPU_1k_lightmap_256_tiles()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.TwoHundredFiftySixth;
        lightingSettings.lightmapResolution = 100;
        lightingSettings.lightmapMaxSize = 1024;

        Lightmapping.Clear();
        Lightmapping.Bake();

        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(0);
        Assert.That(lc.GetTileCount(), Is.EqualTo(256), "Max tiling pass num should be 256");

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator Extract_AO_With_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/AOTestScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;
        lightingSettings.extractAO = false;

        Lightmapping.Clear();
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }
        Assert.That(!System.IO.File.Exists("Assets/Tests/Editor/Tests_Unit_Editmode/AOTestScene/Lightmap-0_ao.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
        lightingSettings.extractAO = true;
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Assert.That(System.IO.File.Exists("Assets/Tests/Editor/Tests_Unit_Editmode/AOTestScene/Lightmap-0_ao.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
    }

    [UnityTest]
    public IEnumerator Export_Training_Data_With_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/ExtractTrainingDataScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;
        lightingSettings.exportTrainingData = false;
        lightingSettings.ao = false;

        Lightmapping.Clear();
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Assert.That(!System.IO.Directory.Exists("Assets/TrainingData/Lightmap-0"));
        Assert.That(!System.IO.File.Exists("Assets/TrainingData/Lightmap-0/indirect.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
        lightingSettings.exportTrainingData = true;
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Assert.That(System.IO.Directory.Exists("Assets/TrainingData/Lightmap-0"));
        Assert.That(System.IO.File.Exists("Assets/TrainingData/Lightmap-0/indirect.exr"));
        Assert.That(!System.IO.File.Exists("Assets/TrainingData/Lightmap-0/ambient_occlusion.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();

        System.IO.Directory.Delete("Assets/TrainingData", true);
        Assert.That(!System.IO.Directory.Exists("Assets/TrainingData"));
    }

    [UnityTest]
    public IEnumerator Export_Training_Data_With_AO_And_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/ExtractTrainingDataScene.unity", OpenSceneMode.Single);
        yield return null;

        LightingSettings lightingSettings = null;
        Lightmapping.TryGetLightingSettings(out lightingSettings);

        Assert.That(lightingSettings, !Is.EqualTo(null), "LightingSettings is null");

        lightingSettings.tiledBaking = LightingSettings.TiledBaking.Quarter;
        lightingSettings.exportTrainingData = false;
        lightingSettings.ao = true;

        Lightmapping.Clear();
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Assert.That(!System.IO.Directory.Exists("Assets/TrainingData/Lightmap-0"));
        Assert.That(!System.IO.File.Exists("Assets/TrainingData/Lightmap-0/indirect.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
        lightingSettings.exportTrainingData = true;
        Lightmapping.Bake();

        while (Lightmapping.isRunning)
        {
            yield return null;
        }

        Assert.That(System.IO.Directory.Exists("Assets/TrainingData/Lightmap-0"));
        Assert.That(System.IO.File.Exists("Assets/TrainingData/Lightmap-0/indirect.exr"));
        Assert.That(System.IO.File.Exists("Assets/TrainingData/Lightmap-0/ambient_occlusion.exr"));

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();

        System.IO.Directory.Delete("Assets/TrainingData", true);
        Assert.That(!System.IO.Directory.Exists("Assets/TrainingData"));
    }
}
