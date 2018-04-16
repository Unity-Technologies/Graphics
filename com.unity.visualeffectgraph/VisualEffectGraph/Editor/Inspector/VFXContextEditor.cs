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

        bShowContextInfo = false;
        bShowSystemInfo = false;
        bShowParticleInfo = false;

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
            bShowParticleInfo = EditorGUILayout.Foldout(bShowParticleInfo, "Particle System Summary", Styles.foldout);
            if (bShowParticleInfo)
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Capacity", particleData.capacity.ToString());
                Styles.Row(Styles.cell, "Source Count", particleData.sourceCount.ToString());
            }
        }

        // Generic context data
        EditorGUILayout.Space();
        bShowContextInfo = EditorGUILayout.Foldout(bShowContextInfo, "Context Summary", Styles.foldout);
        if (bShowContextInfo)
        {
            Styles.Row(Styles.header, "Name", "Value");
            Styles.Row(Styles.cell, "Context Type", context.contextType.ToString());
            Styles.Row(Styles.cell, "Input Data Type", context.inputType.ToString());
            Styles.Row(Styles.cell, "Context Data Type", data.type.ToString());
            Styles.Row(Styles.cell, "Output Type", context.outputType.ToString());

            Styles.Row(Styles.cell, "Source Count", data.sourceCount.ToString());
            Styles.Row(Styles.cell, "Task Type", context.taskType.ToString());
            Styles.Row(Styles.cell, "Can Be Compiled", context.CanBeCompiled().ToString());


            EditorGUILayout.Space();

            if (data.storedCurrentAttributes.Count > 0)
            {
                EditorGUILayout.LabelField("Stored Attributes", Styles.header);

                foreach (var kvp in data.storedCurrentAttributes)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(kvp.Key.name, Styles.cell);
                        Styles.DataTypeLabel(kvp.Key.type.ToString(), kvp.Key.type, Styles.cell, GUILayout.Width(80));
                        EditorGUILayout.LabelField(kvp.Value.ToString(), Styles.cell, GUILayout.Width(80));
                    }
                }
            }
            EditorGUILayout.Space();

            if (data.attributesToContexts.Count > 0)
            {
                EditorGUILayout.LabelField("Local Attributes", Styles.header);

                foreach (var attrib in data.localCurrentAttributes)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(attrib.name, Styles.cell);
                        Styles.DataTypeLabel(attrib.type.ToString(), attrib.type, Styles.cell, GUILayout.Width(160));
                    }
                }
            }
        }

        EditorGUILayout.Space();
        bShowSystemInfo = EditorGUILayout.Foldout(bShowSystemInfo, "System Attribute Summary", Styles.foldout);
        if (bShowSystemInfo)
        {
            foreach (var ctx in data.owners)
            {
                Dictionary<VFXAttribute, VFXAttributeMode> attributeInfos;
                if (data.contextsToAttributes.TryGetValue(ctx, out attributeInfos))
                {
                    EditorGUILayout.LabelField(string.Format("{0} ({1}) ", ctx.GetHashCode(), ctx.contextType), Styles.header);

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
            }
        }
    }

    bool bShowParticleInfo;
    bool bShowContextInfo;
    bool bShowSystemInfo;
}
