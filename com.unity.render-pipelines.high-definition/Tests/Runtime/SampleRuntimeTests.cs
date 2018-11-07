using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

class SampleRuntimeTests : IPrebuildSetup
{
    const float k_Epsilon = 1e-4f;

    static List<string> s_Scenes;

    static SampleRuntimeTests()
    {
        s_Scenes = new List<string>
        {
            "Packages/com.unity.render-pipelines.high-definition/Tests/Runtime/Scenes/0010_Volumes.unity",
        };
    }

    public void Setup()
    {
#if UNITY_EDITOR
        Debug.Log("Adding scenes to build settings...");

        var scenes = new EditorBuildSettingsScene[s_Scenes.Count];

        for (int i = 0; i < s_Scenes.Count; i++)
            scenes[i] = new EditorBuildSettingsScene(s_Scenes[i], true);

        EditorBuildSettings.scenes = scenes;
#endif
    }

   // [UnityTest]
    public IEnumerator SampleLoadSceneTest()
    {
        SceneManager.LoadScene(s_Scenes[0]);

        yield return null; // Skip a frame

        Assert.True(true);
    }
}
