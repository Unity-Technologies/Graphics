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

[CustomEditor(typeof(VFXModel), true)]
[CanEditMultipleObjects]
public class VFXSlotContainerEditor : Editor
{
    protected void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    protected void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    public virtual void DoInspectorGUI()
    {
        var slotContainer = targets[0] as VFXModel;
        IEnumerable<FieldInfo> settingFields = slotContainer.GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

        for (int i = 1; i < targets.Length; ++i)
        {
            IEnumerable<FieldInfo> otherSettingFields = (targets[i] as VFXModel).GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

            settingFields = settingFields.Intersect(otherSettingFields);
        }

        foreach (var prop in settingFields.Select(t => new KeyValuePair<FieldInfo, SerializedProperty>(t, serializedObject.FindProperty(t.Name))).Where(t => t.Value != null))
        {
            var attrs = prop.Key.GetCustomAttributes(typeof(StringProviderAttribute), true);
            if (attrs.Length > 0)
            {
                var strings = StringPropertyRM.FindStringProvider(attrs)();

                int selected = prop.Value.hasMultipleDifferentValues ? -1 : System.Array.IndexOf(strings, prop.Value.stringValue);
                int result = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(prop.Value.name), selected, strings);
                if (result != selected)
                {
                    prop.Value.stringValue = strings[result];
                }
            }
            else
            {
                bool visibleChildren = EditorGUILayout.PropertyField(prop.Value);
                if (visibleChildren)
                {
                    SerializedProperty childProp = prop.Value.Copy();
                    while (childProp != null && childProp.NextVisible(visibleChildren) && childProp.propertyPath.StartsWith(prop.Value.propertyPath + "."))
                    {
                        visibleChildren = EditorGUILayout.PropertyField(childProp);
                    }
                }
            }
        }
    }

    void OnSceneGUI(SceneView sv)
    {
        try // make sure we don't break the whole scene
        {
            var slotContainer = targets[0] as VFXModel;
            if (VFXViewWindow.currentWindow != null)
            {
                VFXView view = VFXViewWindow.currentWindow.graphView;
                if (view.controller != null && view.controller.model && view.controller.graph == slotContainer.GetGraph())
                {
                    if (slotContainer is VFXParameter)
                    {
                        var controller = view.controller.GetParameterController(slotContainer as VFXParameter);

                        controller.DrawGizmos(view.attachedComponent);
                    }
                    else
                    {
                        var controller = view.controller.GetNodeController(slotContainer, 0);
                        if (controller != null)
                            controller.DrawGizmos(view.attachedComponent);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DoInspectorGUI();

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (VFXModel context in targets.OfType<VFXModel>())
            {
                // notify that something changed.
                context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
    }

    protected class Contents
    {
        public static GUIContent name = EditorGUIUtility.TrTextContent("Name");
        public static GUIContent type = EditorGUIUtility.TrTextContent("Type");
        public static GUIContent mode = EditorGUIUtility.TrTextContent("Mode");
    }

    protected class Styles
    {
        public static GUIStyle header;
        public static GUIStyle cell;
        public static GUIStyle foldout;
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

            foldout = new GUIStyle(EditorStyles.foldout);
            foldout.fontStyle = FontStyle.Bold;
        }

        static Dictionary<VFXAttributeMode, Color> attributeModeColors = new Dictionary<VFXAttributeMode, Color>()
        {
            { VFXAttributeMode.None, new Color32(160, 160, 160, 255) },
            { VFXAttributeMode.Read, new Color32(160, 160, 255, 255) },
            { VFXAttributeMode.ReadSource, new Color32(160, 160, 255, 255) },
            { VFXAttributeMode.ReadWrite, new Color32(160, 255, 160, 255) },
            { VFXAttributeMode.Write, new Color32(255, 160, 160, 255) },
            { VFXAttributeMode.Write | VFXAttributeMode.ReadSource, new Color32(255, 160, 255, 255) },
        };

        static Dictionary<VFXValueType, Color> valueTypeColors = new Dictionary<VFXValueType, Color>()
        {
            { VFXValueType.Boolean, new Color32(125, 110, 191, 255) },
            { VFXValueType.ColorGradient, new Color32(130, 223, 226, 255) },
            { VFXValueType.Curve, new Color32(130, 223, 226, 255) },
            { VFXValueType.Float, new Color32(130, 223, 226, 255) },
            { VFXValueType.Float2, new Color32(154, 239, 146, 255) },
            { VFXValueType.Float3, new Color32(241, 250, 151, 255) },
            { VFXValueType.Float4, new Color32(246, 199, 239, 255) },
            { VFXValueType.Int32, new Color32(125, 110, 191, 255) },
            { VFXValueType.Matrix4x4, new Color32(118, 118, 118, 255) },
            { VFXValueType.Mesh, new Color32(130, 223, 226, 255) },
            { VFXValueType.None, new Color32(118, 118, 118, 255) },
            { VFXValueType.Spline, new Color32(130, 223, 226, 255) },
            { VFXValueType.Texture2D, new Color32(250, 137, 137, 255) },
            { VFXValueType.Texture2DArray, new Color32(250, 137, 137, 255) },
            { VFXValueType.Texture3D, new Color32(250, 137, 137, 255) },
            { VFXValueType.TextureCube, new Color32(250, 137, 137, 255) },
            { VFXValueType.TextureCubeArray, new Color32(250, 137, 137, 255) },
            { VFXValueType.Uint32, new Color32(125, 110, 191, 255) },
        };

        internal static void DataTypeLabel(string Label, VFXValueType type, GUIStyle style, params GUILayoutOption[] options)
        {
            Color backup = GUI.color;
            GUI.color = valueTypeColors[type];
            GUILayout.Label(Label, style, options);
            GUI.color = backup;
        }

        internal static void AttributeModeLabel(string Label, VFXAttributeMode mode, GUIStyle style, params GUILayoutOption[] options)
        {
            Color backup = GUI.color;
            GUI.color = attributeModeColors[mode];
            EditorGUILayout.LabelField(Label, style, options);
            GUI.color = backup;
        }

        public static void Row(GUIStyle style, params string[] labels)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (string label in labels)
                    EditorGUILayout.LabelField(label, style);
            }
        }
    }
}
