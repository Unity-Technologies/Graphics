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
using System.Reflection;

[CustomEditor(typeof(VFXContext), true)]
[CanEditMultipleObjects]
public class VFXContextEditor : VFXSlotContainerEditor
{
    SerializedProperty spaceProperty;
    protected new void OnEnable()
    {
        spaceProperty = serializedObject.FindProperty("m_Space");

        base.OnEnable();
    }

    public override void DoInspectorGUI()
    {
        EditorGUILayout.PropertyField(spaceProperty);

        base.DoInspectorGUI();
    }
}
