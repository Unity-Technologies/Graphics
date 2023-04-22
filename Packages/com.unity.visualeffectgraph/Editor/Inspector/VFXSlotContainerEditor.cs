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
                enumNames = values.Select(t => new GUIContent(ObjectNames.NicifyVariableName(Enum.GetName(fieldInfo.FieldType, t)))).ToArray();
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
                var tooltipAttribute = fieldInfo.GetCustomAttributes<TooltipAttribute>().FirstOrDefault();
                GUIContent guiContent;
                if (tooltipAttribute != null)
                    guiContent = new GUIContent(ObjectNames.NicifyVariableName(prop.Value.name),
                        tooltipAttribute.tooltip);
                else
                    guiContent = new GUIContent(ObjectNames.NicifyVariableName(prop.Value.name));

                var newValue = EditorGUILayout.DelayedIntField(guiContent, prop.Value.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    newValue = Mathf.Clamp(newValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
                    prop.Value.intValue = newValue;
                    modifiedSetting = prop.Value;
                }

                continue;
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
    internal class SceneViewVFXSlotContainerOverlay : IMGUIOverlay, ITransientOverlay
    {
        const string k_OverlayId = "Scene View/Visual Effect Model";
        const string k_DisplayName = "Visual Effect Model";

        static readonly Dictionary<IGizmoController, VFXView> s_ControllersMap = new();
        static bool s_HasGizmos;

        private IGizmoController selectedController;

        public static void UpdateFromVFXView(VFXView vfxView, IEnumerable<IGizmoController> controllers)
        {
            var viewControllers = s_ControllersMap
                .Where(x => x.Value == vfxView)
                .Select(x => x.Key)
                .ToArray();

            viewControllers.Except(controllers).ToList().ForEach(x => s_ControllersMap.Remove(x));
            controllers.Except(viewControllers).ToList().ForEach(x => s_ControllersMap[x] = vfxView);

            s_HasGizmos = s_ControllersMap.Any(x => x.Key.gizmoables.Any());
        }

        public bool visible => s_HasGizmos;

        public override void OnGUI()
        {
            if (s_ControllersMap.Any())
            {
                GUILayout.BeginHorizontal();
                try
                {
                    var gizmosData = s_ControllersMap
                        .SelectMany(x => x.Key.gizmoables.Select(y => new { View = x.Value, Controller = x.Key, Gizmo = y }))
                        .ToArray();

                    if (gizmosData.Length == 0)
                    {
                        return;
                    }

                    var entries = gizmosData
                        .Select(x => $"{x.View.controller.name}, {(string.IsNullOrEmpty(x.Gizmo.name) ? ((VFXController<VFXModel>)x.Controller).name : x.Gizmo.name)}")
                        .ToArray();

                    var currentIndex = selectedController != null && s_ControllersMap.Keys.Contains(selectedController) ? gizmosData.TakeWhile(x => x.Gizmo != selectedController.currentGizmoable).Count() : 0;

                    GUI.enabled = true;
                    var index = EditorGUILayout.Popup(currentIndex, entries);
                    var selection = gizmosData[index];
                    selectedController = selection.Controller;
                    selectedController.currentGizmoable = selection.Gizmo;
                    var vfxView = selection.View;

                    var component = vfxView.attachedComponent;
                    var gizmoError = selectedController.GetGizmoError(component);
                    if (gizmoError != GizmoError.None)
                    {
                        var content = Contents.GetGizmoErrorContent(gizmoError);
                        GUILayout.Label(content, Styles.warningStyle, GUILayout.Width(19), GUILayout.Height(18));
                    }
                    else
                    {
                        if (GUILayout.Button(Contents.gizmoFrame, Styles.frameButtonStyle, GUILayout.Width(16), GUILayout.Height(16)))
                        {
                            Bounds b = selectedController.GetGizmoBounds(vfxView.attachedComponent);
                            var sceneView = SceneView.lastActiveSceneView;
                            if (b.size.sqrMagnitude > Mathf.Epsilon && sceneView)
                                sceneView.Frame(b, false);
                        }
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
            }
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

        private static Texture2D warningIcon = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "console.warnicon.sml.png");
        public static GUIContent gizmoWarningDefault = EditorGUIUtility.TrIconContent(warningIcon, "The gizmo value is indeterminate.");
        public static GUIContent gizmoWarningHasLinkIndeterminate = EditorGUIUtility.TrIconContent(warningIcon, "The gizmo state is indeterminate because the value relies on an indeterminate evaluation.");
        public static GUIContent gizmoWarningNeedComponent = EditorGUIUtility.TrIconContent(warningIcon, "Local values require a target GameObject to display");
        public static GUIContent gizmoWarningNeedExplicitSpace = EditorGUIUtility.TrIconContent(warningIcon, "The gizmo value needs an explicit Local or World space.");
        public static GUIContent gizmoWarningNotAvailable = EditorGUIUtility.TrIconContent(warningIcon, "There isn't any gizmo available.");
        public static GUIContent gizmoFrame = EditorGUIUtility.TrTextContent("", "Frame Gizmo in scene");

        public static GUIContent GetGizmoErrorContent(GizmoError gizmoError)
        {
            var content = Contents.gizmoWarningDefault;
            if (gizmoError.HasFlag(GizmoError.HasLinkIndeterminate))
            {
                content = Contents.gizmoWarningHasLinkIndeterminate;
            }
            else if (gizmoError.HasFlag(GizmoError.NeedComponent))
            {
                content = Contents.gizmoWarningNeedComponent;
            }
            else if (gizmoError.HasFlag(GizmoError.NeedExplicitSpace))
            {
                content = Contents.gizmoWarningNeedExplicitSpace;
            }
            else if (gizmoError.HasFlag(GizmoError.NotAvailable))
            {
                content = Contents.gizmoWarningNotAvailable;
            }

            return content;
        }
    }

    public class Styles
    {
        public static GUIStyle header;
        public static GUIStyle cell;
        public static GUIStyle foldout;
        public static GUIStyle spawnStyle;
        public static GUIStyle particleStyle;
        public static GUIStyle particleStripeStyle;
        public static GUIStyle meshStyle;
        public static GUIStyle warningStyle;
        public static GUIStyle frameButtonStyle;
        static Styles()
        {
            warningStyle = new GUIStyle(); // margin are steup so that the warning takes the same space as the frame button
            warningStyle.margin.top = 1;
            warningStyle.margin.bottom = 1;
            warningStyle.margin.left = 2;
            warningStyle.margin.right = 1;
            warningStyle.alignment = TextAnchor.MiddleLeft;

            frameButtonStyle = new GUIStyle();
            frameButtonStyle.normal.background = EditorGUIUtility.LoadIconForSkin(EditorResources.iconsPath + "ViewToolZoom.png", EditorGUIUtility.skinIndex);
            frameButtonStyle.active.background = EditorGUIUtility.LoadIconForSkin(EditorResources.iconsPath + "ViewToolZoom On.png", EditorGUIUtility.skinIndex);
            frameButtonStyle.normal.background.filterMode = FilterMode.Trilinear;
            frameButtonStyle.active.background.filterMode = FilterMode.Trilinear;
            frameButtonStyle.alignment = TextAnchor.MiddleLeft;

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

            spawnStyle = new GUIStyle(GUI.skin.label);
            spawnStyle.fontSize = 20;
            spawnStyle.normal.textColor = new Color(0f, 1f, 0.5607843f);
            spawnStyle.hover.textColor = spawnStyle.normal.textColor;

            particleStyle = new GUIStyle(spawnStyle);
            particleStyle.normal.textColor = new Color(1f, 0.7372549f, 0.1294118f);
            particleStyle.hover.textColor = particleStyle.normal.textColor;

            particleStripeStyle = new GUIStyle(spawnStyle);
            particleStripeStyle.normal.textColor = new Color(1f, 0.6666667f, 0.4196078f);
            particleStripeStyle.hover.textColor = particleStripeStyle.normal.textColor;

            meshStyle = new GUIStyle(spawnStyle);
            meshStyle.normal.textColor = new Color(0.231f, 0.369f, 0.573f);
            meshStyle.hover.textColor = meshStyle.normal.textColor;
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
