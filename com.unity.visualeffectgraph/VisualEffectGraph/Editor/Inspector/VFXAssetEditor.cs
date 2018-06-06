using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using System.IO;

using UnityObject = UnityEngine.Object;

[CustomEditor(typeof(VisualEffectAsset))]
public class VisualEffectAssetEditor : Editor
{
    void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;


        bool enabled = GUI.enabled;
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open Editor"))
        {
            EditorWindow.GetWindow<VFXViewWindow>();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        VisualEffectResource resource = asset.GetResource();

        if (resource == null) return;


        UnityObject[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));

        foreach (var shader in objects)
        {
            if (shader is Shader || shader is ComputeShader)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(shader.name);
                if (GUILayout.Button("Reveal"))
                {
                    OpenTempFile(shader);
                }
                GUILayout.EndHorizontal();
            }
        }
        GUI.enabled = enabled;
    }

    void OpenTempFile(UnityObject shader)
    {
        string source = GetShaderSource(shader);

        if (!string.IsNullOrEmpty(source))
        {
            string path = AssetDatabase.GetAssetPath(target);
            string name = Path.GetFileNameWithoutExtension(path);
            string fileName = "Temp/" + name + "_" + shader.name.Replace("/", "_");
            File.WriteAllText(fileName, source);
            EditorUtility.RevealInFinder(fileName);
        }
    }

    string GetShaderSource(UnityObject shader)
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;
        VisualEffectResource resource = asset.GetResource();

        int index = resource.GetShaderIndex(shader);
        if (index < 0 || index >= resource.shaderSources.Length)
            return "";

        return resource.shaderSources[index].source;
    }
}
