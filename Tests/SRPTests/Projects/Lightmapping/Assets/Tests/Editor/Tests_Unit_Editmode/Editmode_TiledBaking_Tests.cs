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
    public IEnumerator Extract_AO_With_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/AOTestScene.unity", OpenSceneMode.Single);
        yield return null;

        Lightmapping.TryGetLightingSettings(out var lightingSettings);

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
    [Ignore("We need to decide if we want to continue supporting the 'export training data' feature. This is tracked by https://jira.unity3d.com/browse/LIGHT-1034")]
    public IEnumerator Export_Training_Data_With_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/ExtractTrainingDataScene.unity", OpenSceneMode.Single);
        yield return null;

        Lightmapping.TryGetLightingSettings(out var lightingSettings);

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
    [Ignore("We need to decide if we want to continue supporting the 'export training data' feature. This is tracked by https://jira.unity3d.com/browse/LIGHT-1034")]
    public IEnumerator Export_Training_Data_With_AO_And_Tiled_Baking_On()
    {
        EditorSceneManager.OpenScene("Assets/Tests/Editor/Tests_Unit_Editmode/ExtractTrainingDataScene.unity", OpenSceneMode.Single);
        yield return null;

        Lightmapping.TryGetLightingSettings(out var lightingSettings);

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
