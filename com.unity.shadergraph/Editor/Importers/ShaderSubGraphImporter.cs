using System;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor;

[ScriptedImporter(5, Extension, 1)]
class ShaderSubGraphImporter : ScriptedImporter
{
    public const string Extension = "shadersubgraph";

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graphAsset = ScriptableObject.CreateInstance<SubGraphAsset>();
        graphAsset.importedAt = DateTime.Now.Ticks;

        Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon@64");
        ctx.AddObjectToAsset("MainAsset", graphAsset, texture);
        ctx.SetMainObject(graphAsset);
        
        AssetDatabase.ImportAsset(SubGraphDatabaseImporter.path);
    }
}
