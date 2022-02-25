using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0802_RenderGraphAsset : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802_RenderGraph", priority = 1)]
    static void CreateSRP0802_RenderGraph()
    {
        var instance = ScriptableObject.CreateInstance<SRP0802_RenderGraphAsset>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802_RenderGraph.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802_RenderGraph();
    }
}