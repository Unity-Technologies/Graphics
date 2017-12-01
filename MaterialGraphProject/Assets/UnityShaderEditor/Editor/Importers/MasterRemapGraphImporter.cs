using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEditor.ShaderGraph;

[ScriptedImporter(1, "ShaderRemapGraph")]
public class MasterRemapGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var textGraph = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
        var graph = JsonUtility.FromJson<MasterRemapGraph>(textGraph);

        if (graph == null)
            return;

        var graphAsset = ScriptableObject.CreateInstance<MasterRemapGraphAsset>();
        graphAsset.remapGraph = graph;
        ctx.AddObjectToAsset("MainAsset", graphAsset);
        ctx.SetMainObject(graphAsset);
    }
}
