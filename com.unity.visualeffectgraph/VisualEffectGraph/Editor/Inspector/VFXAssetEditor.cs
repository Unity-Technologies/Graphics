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

using Object = UnityEngine.Object;
using UnityEditorInternal;

[CustomEditor(typeof(VisualEffectAsset))]
public class VisualEffectAssetEditor : Editor
{
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
                var errors = shaderSource.compute ? ShaderUtil.GetComputeShaderErrors(shaderSource.shader as ComputeShader) : ShaderUtil.GetShaderErrors(shaderSource.shader as Shader);

                foreach (var error in errors)
                {
                    GUILayout.Label(new GUIContent(error.message, string.Format("{0} line:{1} shader:{2}", error.messageDetails, error.line, shaderSource.name)));
                }
            }
        }
    }
}
