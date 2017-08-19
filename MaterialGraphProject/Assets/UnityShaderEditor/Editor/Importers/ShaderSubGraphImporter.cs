using UnityEditor.Experimental.AssetImporters;
using UnityEngine.MaterialGraph;
using UnityEngine;
using System.IO;
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