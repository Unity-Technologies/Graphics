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

[CustomEditor(typeof(VFXModel), true)]
[CanEditMultipleObjects]
public class VFXSlotContainerEditor : Editor
{
    SerializedProperty[] settingsProperty;
    protected void OnEnable()
    {
        ComputeSettings();
    }

    void ComputeSettings()
    {
        var slotContainer = targets[0] as VFXModel;
        IEnumerable<FieldInfo> settingFields = slotContainer.GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

        for (int i = 1; i < targets.Length; ++i)
        {
            IEnumerable<FieldInfo> otherSettingFields = (targets[i] as VFXModel).GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

            settingFields = settingFields.Intersect(otherSettingFields);
        }

        settingsProperty = settingFields.Select(t => serializedObject.FindProperty(t.Name)).Where(t => t != null).ToArray();
    }

    public virtual void DoInspectorGUI()
    {
        foreach (var prop in settingsProperty)
        {
            bool visibleChildren = EditorGUILayout.PropertyField(prop);
            if (visibleChildren)
            {
                SerializedProperty childProp = prop.Copy();
                while (childProp != null && childProp.NextVisible(visibleChildren) && childProp.propertyPath.StartsWith(prop.propertyPath + "."))
                {
                    visibleChildren = EditorGUILayout.PropertyField(childProp);
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DoInspectorGUI();

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
