using UnityEngine.MaterialGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var name = Path.GetFileNameWithoutExtension(path);

            List<PropertyCollector.TextureInfo> configuredTextures;
            var shaderString = graph.GetShader(string.Format("graphs/{0}", name), GenerationMode.ForReals, out configuredTextures);
            Debug.Log(shaderString);
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

    private static void ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        var extension = Path.GetExtension(path);
        Type windowType;
        if (extension == ".ShaderGraph")
            windowType = typeof(MaterialGraphEditWindow);
        else if (extension == ".ShaderSubGraph")
            windowType = typeof(SubGraphEditWindow);
        else
            return;
        var windows = Resources.FindObjectsOfTypeAll(windowType);
        bool foundWindow = false;
        foreach (var w in windows.OfType<IMaterialGraphEditWindow>())
        {
            if (w.selected == asset)
            {
                foundWindow = true;
                w.Focus();
            }
        }

        if (!foundWindow)
        {
            var window = ScriptableObject.CreateInstance(windowType) as IMaterialGraphEditWindow;
            window.Show();
            window.ChangeSelection(asset);
        }
    }
}
