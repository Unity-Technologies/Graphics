using UnityEditor.Experimental.AssetImporters;
using UnityEngine.MaterialGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine.Graphing;
using System.Text;

[ScriptedImporter(1, "ShaderSubGraph")]
public class ShaderSubGraphImporter : ScriptedImporter
{
	public override void OnImportAsset(AssetImportContext ctx)
	{
		var textGraph = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
		var graph = JsonUtility.FromJson<SubGraph>(textGraph);

		if (graph == null)
			return;

		var graphAsset = ScriptableObject.CreateInstance<MaterialSubGraphAsset> ();
		graphAsset.subGraph = graph;
		ctx.SetMainAsset("MainAsset", graphAsset);
	}
}