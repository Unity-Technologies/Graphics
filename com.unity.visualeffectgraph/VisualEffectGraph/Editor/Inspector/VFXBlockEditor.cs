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


        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        if (dataObject != null)
            dataObject.Update();

        base.OnInspectorGUI();

        //if (Event.current.type != EventType.Repaint) return;

        EditorGUILayout.Space();
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
                GUILayout.Label(attribute.attrib.type.ToString(), Styles.cell, GUILayout.Width(80));
                GUILayout.Label(attribute.mode.ToString(), Styles.cell, GUILayout.Width(80));
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
                GUILayout.Label(param.exp.valueType.ToString(), Styles.cell, GUILayout.Width(160));
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Computed Source Code", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(block.source);
    }

    class Contents
    {
        public static GUIContent name = new GUIContent("Name");
        public static GUIContent type = new GUIContent("Type");
        public static GUIContent mode = new GUIContent("Mode");
    }

    class Styles
    {
        public static GUIStyle header;
        public static GUIStyle cell;
        static Styles()
        {
            header = new GUIStyle(EditorStyles.toolbarButton);
            header.fontStyle = FontStyle.Bold;
            header.alignment = TextAnchor.MiddleLeft;

            cell = new GUIStyle(EditorStyles.toolbarButton);
            var bg = cell.onActive.background;

            cell.active.background = bg;
            cell.onActive.background = bg;
            cell.normal.background = bg;
            cell.onNormal.background = bg;
            cell.focused.background = bg;
            cell.onFocused.background = bg;
            cell.hover.background = bg;
            cell.onHover.background = bg;

            cell.alignment = TextAnchor.MiddleLeft;
        }
    }
}
