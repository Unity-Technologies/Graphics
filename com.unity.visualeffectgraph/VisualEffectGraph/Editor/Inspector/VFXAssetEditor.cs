using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using System.IO;

using Object = UnityEngine.Object;
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

        VFXGraph graph = asset.GetOrCreateGraph();
        /*
        m_Outputs = graph.children.OfType<VFXAbstractParticleOutput>().ToList();

        outputList = new ReorderableList(m_Outputs, typeof(VFXAbstractParticleOutput));
        */
    }

    public override void OnInspectorGUI()
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open Editor"))
        {
            EditorWindow.GetWindow<VFXViewWindow>();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        foreach (var shaderSource in asset.shaderSources)
        {
            if (shaderSource.shader != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(shaderSource.name);
                if (GUILayout.Button("Reveal"))
                {
                    OpenTempFile(shaderSource);
                }
                GUILayout.EndHorizontal();
                var errors = shaderSource.compute ? ShaderUtil.GetComputeShaderErrors(shaderSource.shader as ComputeShader) : ShaderUtil.GetShaderErrors(shaderSource.shader as Shader);

                foreach (var error in errors)
                {
                    GUILayout.Label(new GUIContent(error.message, error.warning != 0 ? VisualEffectAssetEditorStyles.warningIcon : VisualEffectAssetEditorStyles.errorIcon, string.Format("{0} line:{1} shader:{2}", error.messageDetails, error.line, shaderSource.name)));
                }
            }
        }
    }

    void OpenTempFile(VFXShaderSourceDesc shader)
    {
        string path = AssetDatabase.GetAssetPath(target);
        string name = Path.GetFileNameWithoutExtension(path);
        string fileName = "Temp/" + name + "_" + shader.name;
        File.WriteAllText(fileName, shader.source);
        EditorUtility.RevealInFinder(fileName);
    }
}
