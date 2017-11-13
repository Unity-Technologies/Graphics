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
public class VFXContextEditor : Editor
{
    SerializedProperty spaceProperty;
    SerializedProperty[] settingsProperty;
    void OnEnable()
    {
        spaceProperty = serializedObject.FindProperty("m_Space");

        Type type = targets[0].GetType();
        for (int i = 1; i < targets.Length; ++i)
        {
            while (!type.IsAssignableFrom(targets[i].GetType()))
            {
                type = type.BaseType;
            }
        }


        settingsProperty = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f =>
            {
                return f.GetCustomAttributes(typeof(VFXSettingAttribute), true).Length == 1 &&
                VFXSettingAttribute.IsTypeSupported(f.FieldType);
            }).Select(t => serializedObject.FindProperty(t.Name)).ToArray();
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


        foreach (var prop in settingsProperty)
        {
            EditorGUILayout.PropertyField(prop);
        }

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
