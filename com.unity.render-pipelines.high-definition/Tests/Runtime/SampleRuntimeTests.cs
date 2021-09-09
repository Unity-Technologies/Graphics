using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

class SampleRuntimeTests
{
    const float k_Epsilon = 1e-4f;

    static List<string> s_Scenes = new List<string> { };

    public IEnumerator SampleLoadSceneTest()
    {
        SceneManager.LoadScene(s_Scenes[0]);

        yield return null; // Skip a frame

        Assert.True(true);
    }
}
