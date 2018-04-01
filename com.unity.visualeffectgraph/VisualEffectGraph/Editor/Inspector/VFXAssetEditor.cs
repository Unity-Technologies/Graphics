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
        //outputList.DoLayoutList();
    }
}
