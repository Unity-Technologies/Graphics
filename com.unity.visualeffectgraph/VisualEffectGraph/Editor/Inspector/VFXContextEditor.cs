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
                foreach (VFXContext ctx in targets.OfType<VFXContext>())
                {
                    // notify that something changed.
                    ctx.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                }
            }

        if (targets.Length > 1) return; // Summary Only visible for single selection

        // Context / SystemData
        if (dataObject == null) return;
        var context = (VFXContext)target;
        var data = (VFXData)dataObject.targetObject;

        // Particle context data
        if (data.type == VFXDataType.kParticle)
        {
            VFXDataParticle particleData = data as VFXDataParticle;
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Capacity", particleData.capacity.ToString());

                EditorGUILayout.Space();

                if (data.storedCurrentAttributes.Count > 0)
                {
                    EditorGUILayout.LabelField("Attribute Layout", Styles.header);

                    foreach (var kvp in data.storedCurrentAttributes)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(kvp.Key.name, Styles.cell);
                            Styles.DataTypeLabel(kvp.Key.type.ToString(), kvp.Key.type, Styles.cell, GUILayout.Width(80));
                            int size = VFXExpressionHelper.GetSizeOfType(kvp.Key.type);
                            EditorGUILayout.LabelField(size + " byte" + (size > 1? "s":"") , Styles.cell, GUILayout.Width(80));
                        }
                    }
                }
            }
        }

        if(VFXViewPreference.displayExtraDebugInfo)
        {

            // Extra debug data
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Context Type", context.contextType.ToString());
                Styles.Row(Styles.cell, "Task Type", context.taskType.ToString());
                Styles.Row(Styles.cell, "Input Data Type", context.inputType.ToString());
                Styles.Row(Styles.cell, "Context Data Type", data.type.ToString());
                Styles.Row(Styles.cell, "Can Be Compiled", context.CanBeCompiled().ToString());

                EditorGUILayout.Space();
                Dictionary<VFXAttribute, VFXAttributeMode> attributeInfos;
                if (data.contextsToAttributes.TryGetValue(context, out attributeInfos))
                {
                    EditorGUILayout.LabelField("Attributes used by Context", Styles.header);

                    foreach (var kvp in attributeInfos)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(kvp.Key.name, Styles.cell);
                            Styles.DataTypeLabel(kvp.Key.type.ToString(), kvp.Key.type, Styles.cell, GUILayout.Width(80));
                            Styles.AttributeModeLabel(kvp.Value.ToString(), kvp.Value, Styles.cell, GUILayout.Width(80));
                        }
                    }
                }

                EditorGUILayout.Space();

                Styles.Row(Styles.header, "Blocks");
                foreach (var block in context.activeChildrenWithImplicit)
                    Styles.Row(Styles.cell, block.name, !context.children.Contains(block) ? "implicit" : "");

                EditorGUILayout.Space();
            }

        }

    }
}
