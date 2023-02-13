// Code taken from https://github.cds.internal.unity3d.com/unity/com.unity.testing.gi/blob/b4a5465441d58d47fb11eb7bdfaab6bf92f0a952/Tests/Utilities/TestUtilities.cs#L260

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public class LightBakingBackendSwitcher
{
    [InitializeOnLoadMethod]
    static void CheckAndChangeBackingBackend()
    {
        #if UNITY_EDITOR_OSX
        // Double check if we are on MacOS, just in case
        if (SystemInfo.operatingSystemFamily != OperatingSystemFamily.MacOSX) return;
        
        string bakeLabel = "TestRunnerBake";

        bool isIntelMachine = CultureInfo.InvariantCulture.CompareInfo.IndexOf(SystemInfo.processorType, "Intel", CompareOptions.IgnoreCase) >= 0;

        var scenes = EditorBuildSettings.scenes;

        var previousSceneManagerSetup = EditorSceneManager.GetSceneManagerSetup();

        foreach (var scene in scenes)
        {
            var labels = AssetDatabase.GetLabels(scene.guid);
            if (labels.Contains(bakeLabel))
            {
                EditorSceneManager.OpenScene(scene.path);

                bool isUsingGPU = Lightmapping.lightingSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveGPU;

                // Prevent GPU running on Intel
                if ((isIntelMachine && isUsingGPU))
                {
                    Lightmapping.lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
                    Debug.Log($"Switch {scene.path} to CPU baking");
                }

                // Prevent CPU from running on Arm binaries on Apple Silicon
                else if (!isIntelMachine && !isUsingGPU)
                {
                    Lightmapping.lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
                }
                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }
        }

        EditorSceneManager.RestoreSceneManagerSetup(previousSceneManagerSetup);
        #endif
    }
}
