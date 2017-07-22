using UnityEngine.MaterialGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.MaterialGraph.Drawing;
using UnityEngine.Events;

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

            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = masterNode.GetFullShader(GenerationMode.ForReals, graph.name, out configuredTextures);
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

    public void OnInspectorGUI(string path, UnityAction defaultOnInspectorGUI)
    {
        defaultOnInspectorGUI();
        if (GUILayout.Button("Open Shader Editor"))
        {
            var window = new MaterialGraphEditWindow();
            window.Show();
            window.ChangeSelction(AssetDatabase.LoadAssetAtPath<Shader>(path));
        }
    }

    public void OpenAsset(string path)
    {
        var window = new MaterialGraphEditWindow();
        window.Show();
        window.ChangeSelction(AssetDatabase.LoadAssetAtPath<Shader>(path));
    }
}
