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

    List<VFXAbstractParticleOutput> m_Outputs = new List<VFXAbstractParticleOutput>();
    void OnEnable()
    {
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
        
        foreach (var shader in objects.Where(t=>t is Shader || t is ComputeShader))
        {
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(shader.name);
                if (GUILayout.Button("Reveal"))
                {
                    OpenTempFile(shader);
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

    void OpenTempFile(UnityObject shader)
    {
        string source = GetShaderSource(shader);

        if( ! string.IsNullOrEmpty(source))
        {
            string path = AssetDatabase.GetAssetPath(target);
            string name = Path.GetFileNameWithoutExtension(path);
            string fileName = "Temp/" + name + "_" + shader.name.Replace("/","_");
            File.WriteAllText(fileName, source);
            EditorUtility.RevealInFinder(fileName);
        }
    }


    string GetShaderSource(UnityObject shader)
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;
        VisualEffectResource resource = asset.GetResource();

        int index = resource.GetShaderIndex(shader);
        if( index < 0 || index >= resource.shaderSources.Length)
            return "";

        return resource.shaderSources[index].source;
    }
}
