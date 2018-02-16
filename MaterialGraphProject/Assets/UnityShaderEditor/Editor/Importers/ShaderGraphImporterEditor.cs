using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

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

    internal static bool ShowGraphEditWindow(string path)
    {
        var guid = AssetDatabase.AssetPathToGUID(path);
        var extension = Path.GetExtension(path);
        if (extension != ".ShaderGraph" && extension != ".LayeredShaderGraph" && extension != ".ShaderSubGraph" && extension != ".ShaderRemapGraph")
            return false;

        var foundWindow = false;
        foreach (var w in Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>())
        {
            if (w.selectedGuid == guid)
            {
                foundWindow = true;
                w.Focus();
            }
        }

        if (!foundWindow)
        {
            var window = CreateInstance<MaterialGraphEditWindow>();
            window.Show();
            window.Initialize(guid);
        }
        return true;
    }

    [OnOpenAsset(0)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var path = AssetDatabase.GetAssetPath(instanceID);
        return ShowGraphEditWindow(path);
    }
}
