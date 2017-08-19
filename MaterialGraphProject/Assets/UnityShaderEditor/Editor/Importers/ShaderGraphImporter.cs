using UnityEngine.MaterialGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine.Events;
using Object = UnityEngine.Object;

class ShaderGraphTextGenerator : ICustomShaderImporter
{
    public string GetShaderText(string path)
    {
        try
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);

            IMasterNode masterNode = graph.masterNode;
            if (masterNode == null)
                return null;

            var name = Path.GetFileNameWithoutExtension(path);

            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = masterNode.GetFullShader(GenerationMode.ForReals, string.Format("graphs/{0}", name), out configuredTextures);
            return shaderString;
        }
        catch (Exception)
        {
            // ignored
        }
        return null;
    }

    public bool IsValidForPath(string path)
    {
        try
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);

            return graph != null;
        }
        catch (Exception)
        {
            // ignored
        }
        return false;
    }


    public void OpenAsset(string path)
    {
        ShowGraphEditWindow(path);
    }

    public void OnInspectorGUI(string path, [NotNull] UnityAction defaultOnInspectorGUI)
    {
        if (defaultOnInspectorGUI == null)
            throw new ArgumentNullException("defaultOnInspectorGUI");
        defaultOnInspectorGUI();

        if (GUILayout.Button("Open Shader Editor"))
        {
            ShowGraphEditWindow(path);
        }
    }

    private static void ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        var windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
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
            var window = ScriptableObject.CreateInstance<MaterialGraphEditWindow>();
            window.Show();
            window.ChangeSelction(asset);
        }
    }
}
