using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PreBuildStep : IPrebuildSetup
{
    List<string> ScenesToNotBuild = new List<string> {"035_Shader_TerrainShaders", "132_DetailMapping"};
    public void Setup()
    {
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        if (buildTarget == BuildTarget.StandaloneLinux64)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                foreach (string sceneName in ScenesToNotBuild)
                {
                    if (scene.path.Contains(sceneName))
                    {
                        scene.enabled = false;
                    }
                }
            }

            EditorBuildSettings.scenes = scenes;
        }
    }
}
