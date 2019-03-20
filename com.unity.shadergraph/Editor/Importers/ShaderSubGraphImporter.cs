using System;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor;

[ScriptedImporter(4, Extension, 1)]
class ShaderSubGraphImporter : ScriptedImporter
{
    public const string Extension = "shadersubgraph";

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graphAsset = ScriptableObject.CreateInstance<SubGraphAsset>();
        graphAsset.importedAt = DateTime.Now.Ticks;
        
        ctx.AddObjectToAsset("MainAsset", graphAsset);
        ctx.SetMainObject(graphAsset);
        
        AssetDatabase.ImportAsset(SubGraphDatabaseImporter.path);
    }
}
