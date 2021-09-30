using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(VFXModel), true)]
[CanEditMultipleObjects]
class VFXSlotContainerEditor : Editor
{
    protected void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    protected void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (s_EffectUi == this)
            s_EffectUi = null;
    }

    protected virtual SerializedProperty FindProperty(VFXSetting setting)
    {
        return serializedObject.FindProperty(setting.field.Name);
    }

    struct NameNType
    {
        public string name;
        public Type type;

        public override int GetHashCode()
        {
            return name.GetHashCode() * 23 + type.GetHashCode();
        }
    }

    public virtual SerializedProperty DoInspectorGUI()
    {
        var slotContainer = targets[0] as VFXModel;
        List<VFXSetting> settingFields = slotContainer.GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector).ToList();

        for (int i = 1; i < targets.Length; ++i)
        {
            IEnumerable<VFXSetting> otherSettingFields = (targets[i] as VFXModel).GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector).ToArray();

            var excluded = new HashSet<NameNType>(settingFields.Select(t => new NameNType() { name = t.name, type = t.field.FieldType }).Except(otherSettingFields.Select(t => new NameNType() { name = t.name, type = t.field.FieldType })));
            settingFields.RemoveAll(t => excluded.Any(u => u.name == t.name));
        }

        SerializedProperty modifiedSetting = null;
        foreach (var prop in settingFields.Select(t => new KeyValuePair<VFXSetting, SerializedProperty>(t, FindProperty(t))).Where(t => t.Value != null))
        {
            var fieldInfo = prop.Key.field;
            EditorGUI.BeginChangeCheck();
            var stringAttribute = fieldInfo.GetCustomAttributes<StringProviderAttribute>(true);
            var rangeAttribute = fieldInfo.GetCustomAttributes<RangeAttribute>(false).FirstOrDefault();
            if (stringAttribute.Any())
            {
                var strings = StringPropertyRM.FindStringProvider(stringAttribute.ToArray())();

                int selected = prop.Value.hasMultipleDifferentValues ? -1 : System.Array.IndexOf(strings, prop.Value.stringValue);
                int result = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(prop.Value.name), selected, strings);
                if (result != selected)
                {
                    prop.Value.stringValue = strings[result];
                }
            }
            else if (fieldInfo.FieldType.IsEnum && fieldInfo.FieldType.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
            {
                GUIContent[] enumNames = null;
                int[] enumValues = null;

                Array enums = Enum.GetValues(fieldInfo.FieldType);
                List<int> values = new List<int>(enums.Length);
                for (int i = 0; i < enums.Length; ++i)
                {
                    values.Add((int)enums.GetValue(i));
                }

                foreach (var target in targets)
                {
                    VFXModel targetIte = target as VFXModel;

                    var filteredValues = targetIte.GetFilteredOutEnumerators(fieldInfo.Name);
                    if (filteredValues != null)
                        foreach (int val in filteredValues)
                            values.Remove(val);
                }
                enumNames = values.Select(t => new GUIContent(Enum.GetName(fieldInfo.FieldType, t))).ToArray();
                enumValues = values.ToArray();

                HeaderAttribute attr = fieldInfo.GetCustomAttributes<HeaderAttribute>().FirstOrDefault();

                if (attr != null)
                    GUILayout.Label(attr.header, EditorStyles.boldLabel);

                EditorGUILayout.IntPopup(prop.Value, enumNames, enumValues);
            }
            else if (fieldInfo.FieldType == typeof(int)
                        && rangeAttribute != null
                        && fieldInfo.GetCustomAttributes<DelayedAttribute>().Any())
            {
                //Workaround: Range & Delayed attribute are incompatible, avoid the slider usage to keep the delayed behavior
                var newValue = EditorGUILayout.DelayedIntField(ObjectNames.NicifyVariableName(prop.Value.name), prop.Value.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    newValue = Mathf.Clamp(newValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
                    prop.Value.intValue = newValue;
                    modifiedSetting = prop.Value;
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
            if (EditorGUI.EndChangeCheck())
            {
                modifiedSetting = prop.Value;
            }
        }

        return modifiedSetting;
    }

    IGizmoController m_CurrentController;

    static VFXSlotContainerEditor s_EffectUi;

    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
    class SceneViewVFXSlotContainerOverlay : IMGUIOverlay, ITransientOverlay
    {
        const string k_OverlayId = "Scene View/Visual Effect Model";
        const string k_DisplayName = "Visual Effect Model";

        public bool visible => s_EffectUi != null;

        public override void OnGUI()
        {
            if (s_EffectUi == null)
                return;
            s_EffectUi.SceneViewGUICallback();
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

                        m_CurrentController = controller;
                        if (controller != null)
                            controller.DrawGizmos(view.attachedComponent);
                    }
                    else
                    {
                        m_CurrentController = view.controller.GetNodeController(slotContainer, 0);
                    }
                    if (m_CurrentController != null)
                    {
                        m_CurrentController.DrawGizmos(view.attachedComponent);

                        if (m_CurrentController.gizmoables.Count > 0)
                        {
                            s_EffectUi = this;
                        }
                        else
                        {
                            s_EffectUi = null;
                        }
                    }
                }
                else
                {
                    m_CurrentController = null;
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

    internal virtual void SceneViewGUICallback()
    {
        if (m_CurrentController == null)
            return;

        var gizmoableAnchors = m_CurrentController.gizmoables;
        if (gizmoableAnchors.Count > 0)
        {
            int current = gizmoableAnchors.IndexOf(m_CurrentController.currentGizmoable);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUI.enabled = gizmoableAnchors.Count > 1;
            int result = EditorGUILayout.Popup(current, gizmoableAnchors.Select(t => t.name).ToArray());
            GUI.enabled = true;
            if (EditorGUI.EndChangeCheck() && result != current)
            {
                m_CurrentController.currentGizmoable = gizmoableAnchors[result];
            }
            var slotContainer = targets[0] as VFXModel;
            bool hasvfxViewOpened = VFXViewWindow.currentWindow != null && VFXViewWindow.currentWindow.graphView.controller != null && VFXViewWindow.currentWindow.graphView.controller.graph == slotContainer.GetGraph();


            if (m_CurrentController.gizmoIndeterminate)
            {
                GUILayout.Label(Contents.gizmoIndeterminateWarning, Styles.warningStyle, GUILayout.Width(19), GUILayout.Height(18));
            }
            else if (m_CurrentController.gizmoNeedsComponent && (!hasvfxViewOpened || VFXViewWindow.currentWindow.graphView.attachedComponent == null))
            {
                GUILayout.Label(Contents.gizmoLocalWarning, Styles.warningStyle, GUILayout.Width(19), GUILayout.Height(18));
            }
            else
            {
                if (GUILayout.Button(Contents.gizmoFrame, Styles.frameButtonStyle, GUILayout.Width(16), GUILayout.Height(16)))
                {
                    if (m_CurrentController != null && VFXViewWindow.currentWindow != null)
                    {
                        VFXView view = VFXViewWindow.currentWindow.graphView;
                        if (view.controller != null && view.controller.model && view.controller.graph == slotContainer.GetGraph())
                        {
                            Bounds b = m_CurrentController.GetGizmoBounds(view.attachedComponent);
                            var sceneView = SceneView.lastActiveSceneView;
                            if (b.size.sqrMagnitude > Mathf.Epsilon && sceneView)
                                sceneView.Frame(b, false);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var referenceModel = serializedObject.targetObject as VFXModel;

        var resource = referenceModel.GetResource();
        GUI.enabled = resource != null ? resource.IsAssetEditable() : true;

        SerializedProperty modifiedProperty = DoInspectorGUI();

        if (modifiedProperty != null && modifiedProperty.serializedObject.ApplyModifiedProperties())
        {
            foreach (VFXModel slotContainer in modifiedProperty.serializedObject.targetObjects)
            {
                // notify that something changed.
                slotContainer.OnSettingModified(slotContainer.GetSetting(modifiedProperty.propertyPath));
                slotContainer.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    public class Contents
    {
        public static GUIContent name = EditorGUIUtility.TrTextContent("Name");
        public static GUIContent type = EditorGUIUtility.TrTextContent("Type");
        public static GUIContent mode = EditorGUIUtility.TrTextContent("Mode");
        public static GUIContent gizmoLocalWarning = EditorGUIUtility.TrIconContent(EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "console.warnicon.sml.png"), "Local values require a target GameObject to display");
        public static GUIContent gizmoIndeterminateWarning = EditorGUIUtility.TrIconContent(EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "console.warnicon.sml.png"), "The gizmo value is indeterminate.");
        public static GUIContent gizmoFrame = EditorGUIUtility.TrTextContent("", "Frame Gizmo in scene");
    }

    public class Styles
    {
        public static GUIStyle header;
        public static GUIStyle cell;
        public static GUIStyle foldout;
        public static GUIStyle letter;
        public static GUIStyle warningStyle;
        public static GUIStyle frameButtonStyle;
        static Styles()
        {
            warningStyle = new GUIStyle(); // margin are steup so that the warning takes the same space as the frame button
            warningStyle.margin.top = 1;
            warningStyle.margin.bottom = 1;
            warningStyle.margin.left = 2;
            warningStyle.margin.right = 1;

            frameButtonStyle = new GUIStyle();
            frameButtonStyle.normal.background = EditorGUIUtility.LoadIconForSkin(EditorResources.iconsPath + "ViewToolZoom.png", EditorGUIUtility.skinIndex);
            frameButtonStyle.active.background = EditorGUIUtility.LoadIconForSkin(EditorResources.iconsPath + "ViewToolZoom On.png", EditorGUIUtility.skinIndex);
            frameButtonStyle.normal.background.filterMode = FilterMode.Trilinear;
            frameButtonStyle.active.background.filterMode = FilterMode.Trilinear;

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

            letter = new GUIStyle(GUI.skin.label);
            letter.fontSize = 36;
        }

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
            { VFXValueType.CameraBuffer, new Color32(250, 137, 137, 255) },
            { VFXValueType.Uint32, new Color32(125, 110, 191, 255) },
        };

        internal static void DataTypeLabel(Rect r, string Label, VFXValueType type, GUIStyle style)
        {
            Color backup = GUI.color;
            GUI.color = valueTypeColors[type];
            GUI.Label(r, Label, style);
            GUI.color = backup;
        }

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

            var c = new Color32(160, 160, 160, 255);
            if ((mode & VFXAttributeMode.Read) != 0)
                c.b = 255;
            if ((mode & VFXAttributeMode.Write) != 0)
                c.r = 255;
            if ((mode & VFXAttributeMode.ReadSource) != 0)
                c.g = 255;

            GUI.color = c;
            GUILayout.Label(Label, style, options);
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
