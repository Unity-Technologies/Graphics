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
                GUILayout.Label(shader.name, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Show Generated", GUILayout.Width(110)))
                {
                    int index = resource.GetShaderIndex(shader);
                    resource.ShowGeneratedShaderFile(index);
                }
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = shader;
                }
                GUILayout.EndHorizontal();
            }
        }
        GUI.enabled = enabled;
    }
}
