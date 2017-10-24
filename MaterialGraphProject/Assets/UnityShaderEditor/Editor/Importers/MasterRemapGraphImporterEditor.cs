using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;
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

    static bool ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path) as MasterRemapGraphAsset;
        if (asset == null)
            return false;

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
            var window = CreateInstance<MaterialGraphEditWindow>();
            window.Show();
            window.ChangeSelection(asset, typeof(MasterRemapGraph));
        }
        return true;
    }

    [OnOpenAsset]
    static bool OnOpenAsset(int instanceID, int line)
    {
        var path = AssetDatabase.GetAssetPath(instanceID);
        return ShowGraphEditWindow(path);
    }
}
