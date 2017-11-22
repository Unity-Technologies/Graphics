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

        ComputeSettings();
    }

    void ComputeSettings()
    {
        VFXContext context = targets[0] as VFXContext;
        IEnumerable<FieldInfo> settingFields = context.GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

        for (int i = 1; i < targets.Length; ++i)
        {
            IEnumerable<FieldInfo> otherSettingFields = (targets[i] as VFXContext).GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

            settingFields = settingFields.Intersect(otherSettingFields);
        }

        settingsProperty = settingFields.Select(t => serializedObject.FindProperty(t.Name)).Where(t => t != null).ToArray();
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
            bool visibleChildren = EditorGUILayout.PropertyField(prop);
            if (visibleChildren)
            {
                SerializedProperty childProp = serializedObject.FindProperty(prop.propertyPath);
                while (childProp.NextVisible(visibleChildren) && childProp.propertyPath.StartsWith(prop.propertyPath + "."))
                {
                    visibleChildren = EditorGUILayout.PropertyField(childProp);
                }
            }
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (VFXContext context in targets.OfType<VFXContext>())
            {
                // notify that something changed.
                context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }

            ComputeSettings(); // need to recompute settings because some might show/hide
        }
    }
}
