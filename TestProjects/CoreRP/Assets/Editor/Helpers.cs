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
        AssetDatabase.Refresh();
    }
}
