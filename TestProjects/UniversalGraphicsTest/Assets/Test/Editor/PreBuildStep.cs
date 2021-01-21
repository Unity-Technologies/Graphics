using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuildStep : IPreprocessBuildWithReport
{
    List<string> ScenesToNotBuild = new List<string> {"035_Shader_TerrainShaders", };
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.StandaloneLinux64)
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
