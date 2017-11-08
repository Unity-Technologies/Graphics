using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

[CustomEditor(typeof(ShaderGraphImporter))]
public class ShaderGraphImporterEditor : ScriptedImporterEditor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Shader Editor"))
        {
            AssetImporter importer = target as AssetImporter;
            Debug.Assert(importer != null, "importer != null");
            ShowGraphEditWindow(importer.assetPath);
        }
    }

    internal static void ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        var extension = Path.GetExtension(path);
        Type graphType;
        if (extension == ".ShaderGraph")
            graphType = typeof(MaterialGraph);
        else if (extension == ".LayeredShaderGraph")
            graphType = typeof(LayeredShaderGraph);
        else if (extension == ".ShaderSubGraph")
            graphType = typeof(SubGraph);
        else if (extension == ".ShaderRemapGraph")
            graphType = typeof(MasterRemapGraph);
        else
            return;

        var foundWindow = false;
        foreach (var w in Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>())
        {
            if (w.selected == asset)
            {
                foundWindow = true;
                w.Focus();
            }
        }

        if (!foundWindow)
        {
            var window = ScriptableObject.CreateInstance<MaterialGraphEditWindow>();
            window.Show();
            window.ChangeSelection(asset, graphType);
        }
    }

}
