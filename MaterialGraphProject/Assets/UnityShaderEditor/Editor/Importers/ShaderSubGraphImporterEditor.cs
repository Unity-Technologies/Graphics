using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using Debug = System.Diagnostics.Debug;

[CustomEditor(typeof(ShaderSubGraphImporter))]
public class ShaderSubGraphImporterEditor : ScriptedImporterEditor
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

    private static bool ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path) as MaterialSubGraphAsset;
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
            window.ChangeSelection(asset, typeof(SubGraph));
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
