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
    SerializedObject dataObject;
    protected new void OnEnable()
    {
        UnityEngine.Object[] allData = targets.Cast<VFXContext>().Select(t => t.GetData()).Distinct().Where(t => t != null).Cast<UnityEngine.Object>().ToArray();
        if (allData.Length > 0)
        {
            dataObject = new SerializedObject(allData);

            spaceProperty = dataObject.FindProperty("m_Space");
        }
        else
        {
            dataObject = null;
            spaceProperty = null;
        }


        base.OnEnable();
    }

    public override void DoInspectorGUI()
    {
        if (spaceProperty != null)
            EditorGUILayout.PropertyField(spaceProperty);

        base.DoInspectorGUI();
    }

    public override void OnInspectorGUI()
    {
        if (dataObject != null)
            dataObject.Update();

        base.OnInspectorGUI();

        if (dataObject != null)
            if (dataObject.ApplyModifiedProperties())
            {
                foreach (VFXContext context in targets.OfType<VFXContext>())
                {
                    // notify that something changed.
                    context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                }
            }
    }
}
