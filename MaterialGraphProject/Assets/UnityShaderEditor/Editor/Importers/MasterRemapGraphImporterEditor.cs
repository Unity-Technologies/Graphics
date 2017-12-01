using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using Debug = System.Diagnostics.Debug;

[CustomEditor(typeof(MasterRemapGraphImporter))]
public class MasterRemapGraphImporterEditor : ScriptedImporterEditor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Shader Editor"))
        {
            AssetImporter importer = target as AssetImporter;
            Debug.Assert(importer != null, "importer != null");
            ShaderGraphImporterEditor.ShowGraphEditWindow(importer.assetPath);
        }
    }
}
