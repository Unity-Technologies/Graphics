using System;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor;

<<<<<<< HEAD
[ScriptedImporter(4, Extension, 1)]
=======
[ScriptedImporter(6, Extension, 1)]
>>>>>>> master
class ShaderSubGraphImporter : ScriptedImporter
{
    public const string Extension = "shadersubgraph";

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var graphAsset = ScriptableObject.CreateInstance<SubGraphAsset>();
        graphAsset.importedAt = DateTime.Now.Ticks;
<<<<<<< HEAD
        
        ctx.AddObjectToAsset("MainAsset", graphAsset);
=======

        Texture2D texture = Resources.Load<Texture2D>("Icons/sg_subgraph_icon@64");
        ctx.AddObjectToAsset("MainAsset", graphAsset, texture);
>>>>>>> master
        ctx.SetMainObject(graphAsset);
        
        AssetDatabase.ImportAsset(SubGraphDatabaseImporter.path);
    }
}
