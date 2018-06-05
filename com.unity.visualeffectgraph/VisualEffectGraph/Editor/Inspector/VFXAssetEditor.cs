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
using UnityEditorInternal;


public class VisualEffectAssetEditorStyles
{
    public static Texture2D errorIcon = EditorGUIUtility.LoadIcon("console.erroricon.sml");
    public static Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon.sml");
}

[CustomEditor(typeof(VisualEffectAsset))]
public class VisualEffectAssetEditor : Editor
{
    ReorderableList outputList;

    //List<VFXAbstractParticleOutput> m_Outputs = new List<VFXAbstractParticleOutput>();
    void OnEnable()
    {
        /*
        VisualEffectAsset asset = (VisualEffectAsset)target;
        /*
        VFXGraph graph = asset.GetOrCreateGraph();

        m_Outputs = graph.children.OfType<VFXAbstractParticleOutput>().ToList();

        outputList = new ReorderableList(m_Outputs, typeof(VFXAbstractParticleOutput));
        */
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
                /*
                var errors = ShaderUtil.GetShaderErrors(shader as Shader);

                foreach (var error in errors)
                {
                    GUILayout.Label(new GUIContent(error.message, error.warning != 0 ? VisualEffectAssetEditorStyles.warningIcon : VisualEffectAssetEditorStyles.errorIcon, string.Format("{0} line:{1} shader:{2}", error.messageDetails, error.line, shaderSource.name)));
                }*/
            }
        }
        GUI.enabled = enabled;
    }
}
