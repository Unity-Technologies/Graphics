using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine;
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
            ShowGraphEditWindow(importer.assetPath);
        }
    }

    private static void ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        var windows = Resources.FindObjectsOfTypeAll<MasterReampGraphEditWindow>();
        bool foundWindow = false;
        foreach (var w in windows)
        {
            if (w.selected == asset)
            {
                foundWindow = true;
                w.Focus();
            }
        }

        if (!foundWindow)
        {
            var window = CreateInstance<MasterReampGraphEditWindow>();
            window.Show();
            window.ChangeSelection(asset);
        }
    }
}