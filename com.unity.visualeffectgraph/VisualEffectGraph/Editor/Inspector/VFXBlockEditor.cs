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

[CustomEditor(typeof(VFXBlock), true)]
[CanEditMultipleObjects]
public class VFXBlockEditor : VFXSlotContainerEditor
{
    SerializedObject dataObject;

    bool bShowContextInfo;
    protected new void OnEnable()
    {
        UnityEngine.Object[] allData = targets.Cast<VFXBlock>().Cast<UnityEngine.Object>().ToArray();
        if (allData.Length > 0)
        {
            dataObject = new SerializedObject(allData);
        }
        else
        {
            dataObject = null;
        }
        bShowContextInfo = true;

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        if (dataObject != null)
            dataObject.Update();

        base.OnInspectorGUI();

        EditorGUILayout.Space();
        bShowContextInfo = EditorGUILayout.Foldout(bShowContextInfo, "Block Summary", Styles.foldout);
        if (bShowContextInfo)
        {
            EditorGUILayout.LabelField("Attributes", EditorStyles.boldLabel);

            VFXBlock block = dataObject.targetObject as VFXBlock;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(Contents.name, Styles.header);
                GUILayout.Label(Contents.type, Styles.header, GUILayout.Width(80));
                GUILayout.Label(Contents.mode, Styles.header, GUILayout.Width(80));
            }

            foreach (var attribute in block.attributes)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(attribute.attrib.name, Styles.cell);
                    Styles.DataTypeLabel(attribute.attrib.type.ToString(), attribute.attrib.type, Styles.cell, GUILayout.Width(80));
                    Styles.AttributeModeLabel(attribute.mode.ToString(), attribute.mode, Styles.cell, GUILayout.Width(80));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(Contents.name, Styles.header);
                GUILayout.Label(Contents.type, Styles.header, GUILayout.Width(160));
            }

            foreach (var param in block.parameters)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(param.name, Styles.cell);
                    Styles.DataTypeLabel(param.exp.valueType.ToString(), param.exp.valueType, Styles.cell, GUILayout.Width(160));
                }
            }

            if (block.source != string.Empty)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Computed Source Code", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(block.source);
            }
        }
    }
}
