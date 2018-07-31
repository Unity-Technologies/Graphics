using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class Helpers
{
    [MenuItem("Internal/TestRenderPipeline/Create Test Render Pipeline Asset")]
    static void CreateTestRenderPipelineAsset()
    {
        TestRenderPipelineAsset rpAsset = ScriptableObject.CreateInstance<TestRenderPipelineAsset>();
        AssetDatabase.CreateAsset(rpAsset, "Assets/TestRenderPipelineAsset.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Internal/TestRenderPipeline/Create Resources Asset")]
    static void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<TestRenderPipelineResources>();
        AssetDatabase.CreateAsset(asset, "Assets/TestRenderPipelineResources.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

