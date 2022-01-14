using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public class BakeAPV : MonoBehaviour
{
    // Hack class to bake apv for testing
    // Lightmapping is not available during playmode, so we have to trigger it before
    // UTF supports adding label "TestRunnerBake" on a scene to start lightmapper before entering playmode
    // But it does that so early so hdrp isn't loaded at this point so APV isn't either
    // We hook ourselves to the bake event via a static constructor, then we register a function for update
    // This update is called next tick where apparently HDRP is initialized so we can start our bake from here

    static List<string> scenesToBake = new List<string>();

    static BakeAPV()
    {
        Lightmapping.bakeStarted += BakeStarted;
    }

    static void BakeStarted()
    {
        if (FindObjectOfType<BakeAPV>() == null)
            return;

        scenesToBake.Clear();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != -1)
                scenesToBake.Add(scene.path);
        }
        EditorApplication.update += BakeScenes;
    }

    static void BakeScenes()
    {
        var pipe = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
        Debug.Assert(pipe != null);
        Scene trScene = EditorSceneManager.GetSceneAt(0);

        Lightmapping.bakeStarted -= BakeStarted;
        EditorApplication.update -= BakeScenes;

        foreach (var scene in scenesToBake)
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);

        EditorSceneManager.SetActiveScene(EditorSceneManager.GetSceneAt(1));
        Lightmapping.Bake();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene == trScene) continue;
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        EditorSceneManager.SetActiveScene(trScene);
    }
}
