using UnityEngine.MaterialGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.MaterialGraph.Drawing;
using Object = UnityEngine.Object;

class ShaderGraphImporter : ICustomShaderImporter
{
    private static string GetShaderText<T>(string path) where T : IShaderGraph
    {
        try
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = JsonUtility.FromJson<T>(textGraph);

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

    public string GetShaderText(string path)
    {
        if (path.EndsWith("LayeredShaderGraph", StringComparison.InvariantCultureIgnoreCase))
            return GetShaderText<LayeredShaderGraph>(path);
        if (path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
            return GetShaderText<MaterialGraph>(path);

        return null;
    }

    public bool IsValidForPath(string path)
    {

        return
            path.EndsWith("LayeredShaderGraph", StringComparison.InvariantCultureIgnoreCase)
            || path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase);
    }

    public void OpenAsset(string path)
    {
        ShowGraphEditWindow(path);
    }

    internal static void ShowGraphEditWindow(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        var extension = Path.GetExtension(path);
        Type windowType;
        if (extension == ".ShaderGraph")
            windowType = typeof(MaterialGraphEditWindow);
        else if (extension == ".LayeredShaderGraph")
            windowType = typeof(LayeredGraphEditWindow);
        else if (extension == ".ShaderSubGraph")
            windowType = typeof(SubGraphEditWindow);
        else if (extension == ".ShaderRemapGraph")
            windowType = typeof(MasterRemapGraph);
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
