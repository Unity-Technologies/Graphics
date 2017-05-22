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

[ScriptedImporter(1, "ShaderGraph")]
public class ShaderGraphImporter : ScriptedImporter
{
	public override void OnImportAsset(AssetImportContext ctx)
	{
		var textGraph = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
		var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);

		if (graph == null)
			return;

		var graphAsset = ScriptableObject.CreateInstance<MaterialGraphAsset> ();
		graphAsset.materialGraph = graph;
		ctx.SetMainAsset("MainAsset", graphAsset);

		var shader = RegenerateShader (graphAsset);
		if (shader == null)
			return;
		
		ctx.AddSubAsset("Shader", shader);
	}

	public Shader RegenerateShader(MaterialGraphAsset asset)
	{
		IMasterNode masterNode = asset.materialGraph.masterNode;
		if (masterNode == null)
			return null;
		
		var path = "Assets/UnityShaderEditor/Editor/HelperShader.shader";
		List<PropertyGenerator.TextureInfo> configuredTextures;
		var shaderString = masterNode.GetFullShader(GenerationMode.ForReals, out configuredTextures);

		var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;
		if (shader == null)
			return null;

		File.WriteAllText (path, shaderString);
		ShaderUtil.UpdateShaderAsset (shader, shaderString);

		var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
		if (shaderImporter == null)
			return null;

		var textureNames = new List<string>();
		var textures = new List<Texture>();
		foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.Modifiable))
		{
			var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
			if (texture == null)
				continue;
			textureNames.Add(textureInfo.name);
			textures.Add(texture);
		}
		shaderImporter.SetDefaultTextures(textureNames.ToArray(), textures.ToArray());

		textureNames.Clear();
		textures.Clear();
		foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.NonModifiable))
		{
			var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
			if (texture == null)
				continue;
			textureNames.Add(textureInfo.name);
			textures.Add(texture);
		}
		shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());
		shaderImporter.SaveAndReimport();

		var imported = shaderImporter.GetShader();
		return imported;
	}
}