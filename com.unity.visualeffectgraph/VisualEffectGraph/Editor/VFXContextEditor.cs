using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;

[CustomEditor(typeof(VFXContext), true)]
[CanEditMultipleObjects]
public class VFXContextEditor : Editor
{
    SerializedProperty spaceProperty;
    void OnEnable()
    {
        spaceProperty = serializedObject.FindProperty("m_Space");
    }

    void OnDisable()
    {
    }

    public void OnSceneGUI()
    {
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(spaceProperty);

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (VFXContext context in targets.OfType<VFXContext>())
            {
                // notify that something changed.
                context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
    }
}
