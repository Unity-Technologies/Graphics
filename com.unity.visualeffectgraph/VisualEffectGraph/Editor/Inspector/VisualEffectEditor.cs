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
using UnityEditor.Experimental.UIElements.GraphView;
using EditMode = UnityEditorInternal.EditMode;
using UnityObject = UnityEngine.Object;
using System.Reflection;
namespace UnityEditor.VFX
{
    public static class VisualEffectControl
    {
        public static void ControlStop(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = true;
        }

        public static void ControlPlayPause(this VisualEffect effect)
        {
            effect.pause = !effect.pause;
        }

        public static void ControlStep(this VisualEffect effect)
        {
            effect.pause = true;
            effect.AdvanceOneFrame();
        }

        public static void ControlRestart(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = false;
        }

        public const float minSlider = 1;
        public const float maxSlider = 4000;

        public const float playRateToValue = 100.0f;
        public const float valueToPlayRate = 1.0f / playRateToValue;


        public const float sliderPower = 10;

        public static readonly int[] setPlaybackValues = new int[] { 1, 10, 50, 100, 200, 500, 1000, 4000 };
    }

    static class VisualEffectEditorStyles
    {
        static GUIContent[] m_Icons;

        public enum Icon
        {
            Pause,
            Play,
            Restart,
            Step,
            Stop
        }

        public static readonly GUIStyle toggleStyle;

        static VisualEffectEditorStyles()
        {
            toggleStyle = new GUIStyle("ShurikenCheckMark");
            m_Icons = new GUIContent[1 + (int)Icon.Stop];
            for (int i = 0; i <= (int)Icon.Stop; ++i)
            {
                Icon icon = (Icon)i;
                string name = icon.ToString();

                //TODO replace with editor default resource call when going to trunk
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFXEditor/Editor/SceneWindow/Textures/" + name + ".png");
                if (texture == null)
                {
                    Debug.LogError("Can't find icon for " + name + " in VisualEffectEditorStyles");
                    continue;
                }
                m_Icons[i] = new GUIContent(texture);
            }
        }

        public static GUIContent GetIcon(Icon icon)
        {
            return m_Icons[(int)icon];
        }
    }
    public class VisualEffectEditor : Editor
    {
        public static bool CanSetOverride = false;

        SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;
        bool m_useNewSerializedField = false;

        private Contents m_Contents;
        private Styles m_Styles;
        private bool m_ShowDebugStats = false;

        protected void OnEnable()
        {
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");

            m_Infos.Clear();
        }

        protected void OnDisable()
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            if (effect != null)
            {
                effect.pause = false;
                effect.playRate = 1.0f;
            }
        }

        struct Infos
        {
            public VFXPropertyIM propertyIM;
            public Type type;
        }

        Dictionary<VFXParameterNodeController, Infos> m_Infos = new Dictionary<VFXParameterNodeController, Infos>();

        struct FieldData
        {
            public System.Type type;
            public string exposedName;
            public string fieldName;
            public RangeAttribute rangeAttribute;
        }

        protected const float overrideWidth = 16;

        void DisplayProperty(VFXGraph.ParameterInfo parameter, SerializedProperty overrideProperty, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent nameContent = EditorGUIUtility.TextContent(parameter.name);

            if (!overrideProperty.hasMultipleDifferentValues)
            {
                bool result = EditorGUILayout.Toggle(overrideProperty.boolValue, VisualEffectEditorStyles.toggleStyle, GUILayout.Width(overrideWidth));

                if (overrideProperty.boolValue != result)
                {
                    overrideProperty.boolValue = result;
                }
            }
            else
            {
                //TODO what to do with multiple value
            }

            EditorGUI.BeginChangeCheck();
            if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                    EditorGUILayout.Slider(property, parameter.min, parameter.max, EditorGUIUtility.TextContent(parameter.name));
                else
                    EditorGUILayout.IntSlider(property, (int)parameter.min, (int)parameter.max, EditorGUIUtility.TextContent(parameter.name));
            }
            else if (parameter.realType == typeof(Color).Name)
            {
                Vector4 vVal = property.vector4Value;
                Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                c = EditorGUILayout.ColorField(nameContent, c, true, true, true);

                if (c.r != vVal.x || c.g != vVal.y || c.b != vVal.z || c.a != vVal.w)
                    property.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                var oldVal = property.vector4Value;
                var newVal = EditorGUILayout.Vector4Field(nameContent, oldVal);
                if (oldVal.x != newVal.x || oldVal.y != newVal.y || oldVal.z != newVal.z || oldVal.w != newVal.w)
                {
                    property.vector4Value = newVal;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(property, nameContent, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                overrideProperty.boolValue = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        public void InitializeGUI()
        {
            if (m_Contents == null)
                m_Contents = new Contents();

            if (m_Styles == null)
                m_Styles = new Styles();
        }

        private void SceneViewGUICallback(UnityObject target, SceneView sceneView)
        {
            VisualEffect effect = ((VisualEffect)targets[0]);

            var buttonWidth = GUILayout.Width(50);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Stop), buttonWidth))
            {
                effect.ControlStop();
            }
            if (effect.pause)
            {
                if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Play), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }
            else
            {
                if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Pause), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }


            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Step), buttonWidth))
            {
                effect.ControlStep();
            }
            if (GUILayout.Button(VisualEffectEditorStyles.GetIcon(VisualEffectEditorStyles.Icon.Restart), buttonWidth))
            {
                effect.ControlRestart();
            }
            GUILayout.EndHorizontal();

            float playRate = effect.playRate * VisualEffectControl.playRateToValue;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Playback Rate", GUILayout.Width(84));
            playRate = EditorGUILayout.PowerSlider("", playRate, VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, GUILayout.Width(138));
            effect.playRate = playRate * VisualEffectControl.valueToPlayRate;
            if (EditorGUILayout.DropdownButton(EditorGUIUtility.TextContent("Set"), FocusType.Passive, GUILayout.Width(36)))
            {
                GenericMenu menu = new GenericMenu();
                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                foreach (var value in VisualEffectControl.setPlaybackValues)
                {
                    menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
                }
                menu.DropDown(buttonRect);
            }
            GUILayout.EndHorizontal();
        }

        void SetPlayRate(object value)
        {
            float rate = (float)((int)value)  * VisualEffectControl.valueToPlayRate;
            VisualEffect effect = ((VisualEffect)targets[0]);
            effect.playRate = rate;
        }

        protected virtual void OnSceneGUI()
        {
            SceneViewOverlay.Window(ParticleSystemInspector.playBackTitle, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        private VisualEffectAsset m_asset;
        private VFXGraph m_graph;


        protected virtual void EmptyLineControl(string name, int depth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(overrideWidth + 4); // the 4 is so that Labels are aligned with elements having an override toggle.
            EditorGUILayout.LabelField(name);
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            InitializeGUI();

            var component = (VisualEffect)target;

            //Asset
            GUILayout.Label(m_Contents.HeaderMain, m_Styles.InspectorHeader);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(m_VisualEffectAsset, m_Contents.AssetPath);

                GUI.enabled = component.visualEffectAsset != null; // Enabled state will be kept for all content until the end of the inspectorGUI.
                if (GUILayout.Button(m_Contents.OpenEditor, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                {
                    VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

                    window.LoadAsset(component.visualEffectAsset);
                }
            }

            //Seed
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(m_ReseedOnPlay.boolValue))
                {
                    EditorGUILayout.PropertyField(m_RandomSeed, m_Contents.RandomSeed);
                    if (GUILayout.Button(m_Contents.SetRandomSeed, EditorStyles.miniButton, m_Styles.MiniButtonWidth))
                    {
                        m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                        component.startSeed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                    }
                }
            }
            EditorGUILayout.PropertyField(m_ReseedOnPlay, m_Contents.ReseedOnPlay);
            bool reinit = EditorGUI.EndChangeCheck();

            //Field
            GUILayout.Label(m_Contents.HeaderParameters, m_Styles.InspectorHeader);

            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Show Parameters",
                EditorGUIUtility.IconContent("EditCollider"),
                this
                );

            if (m_graph == null || m_asset != component.visualEffectAsset)
            {
                m_asset = component.visualEffectAsset;
                if (m_asset != null)
                {
                    m_graph = m_asset.GetOrCreateGraph();
                }
            }

            if (m_graph != null)
            {
                if (m_graph.m_ParameterInfo == null)
                {
                    m_graph.BuildParameterInfo();
                }
                if (m_graph.m_ParameterInfo != null)
                {
                    List<int> stack = new List<int>();
                    int currentCount = m_graph.m_ParameterInfo.Length;
                    foreach (var parameter in m_graph.m_ParameterInfo)
                    {
                        --currentCount;
                        if (currentCount == 0 && stack.Count > 0)
                        {
                            currentCount = stack.Last();
                            stack.RemoveAt(stack.Count - 1);
                        }
                        if (parameter.descendantCount > 0)
                        {
                            stack.Add(currentCount);
                            currentCount = parameter.descendantCount;
                        }

                        if (string.IsNullOrEmpty(parameter.sheetType))
                        {
                            if (parameter.name != null)
                            {
                                EmptyLineControl(parameter.name, stack.Count);
                            }
                        }
                        else if (parameter.sheetType != null)
                        {
                            var vfxField = m_VFXPropertySheet.FindPropertyRelative(parameter.sheetType + ".m_Array");
                            SerializedProperty property = null;
                            if (vfxField != null)
                            {
                                for (int i = 0; i < vfxField.arraySize; ++i)
                                {
                                    property = vfxField.GetArrayElementAtIndex(i);
                                    var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                                    if (nameProperty == parameter.path)
                                    {
                                        break;
                                    }
                                    property = null;
                                }
                            }
                            if (property != null)
                            {
                                SerializedProperty overrideProperty = property.FindPropertyRelative("m_Overridden");
                                property = property.FindPropertyRelative("m_Value");
                                string firstpropName = property.name;

                                Color previousColor = GUI.color;
                                var animated = AnimationMode.IsPropertyAnimated(target, property.propertyPath);
                                if (animated)
                                {
                                    GUI.color = AnimationMode.animatedPropertyColor;
                                }

                                DisplayProperty(parameter, overrideProperty, property);

                                if (animated)
                                {
                                    GUI.color = previousColor;
                                }
                            }
                        }
                        EditorGUI.indentLevel = stack.Count;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (reinit)
            {
                component.Reinit();
            }

            GUI.enabled = true;
        }

        private class Styles
        {
            public GUIStyle InspectorHeader;
            public GUIStyle ToggleGizmo;

            public GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
            public GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

            public Styles()
            {
                InspectorHeader = new GUIStyle("ShurikenModuleTitle");
                InspectorHeader.fontSize = 12;
                InspectorHeader.fontStyle = FontStyle.Bold;
                InspectorHeader.contentOffset = new Vector2(2, -2);
                InspectorHeader.border = new RectOffset(4, 4, 4, 4);
                InspectorHeader.overflow = new RectOffset(4, 4, 4, 4);
                InspectorHeader.margin = new RectOffset(4, 4, 16, 8);

                Texture2D showIcon = EditorGUIUtility.Load("VisibilityIcon.png") as Texture2D;
                Texture2D hideIcon = EditorGUIUtility.Load("VisibilityIconDisabled.png") as Texture2D;

                ToggleGizmo = new GUIStyle();
                ToggleGizmo.margin = new RectOffset(0, 0, 4, 0);
                ToggleGizmo.active.background = hideIcon;
                ToggleGizmo.onActive.background = showIcon;
                ToggleGizmo.normal.background = hideIcon;
                ToggleGizmo.onNormal.background = showIcon;
                ToggleGizmo.focused.background = hideIcon;
                ToggleGizmo.onFocused.background = showIcon;
                ToggleGizmo.hover.background = hideIcon;
                ToggleGizmo.onHover.background = showIcon;
            }
        }

        private class Contents
        {
            public GUIContent HeaderMain = new GUIContent("VFX Asset");
            public GUIContent HeaderPlayControls = new GUIContent("Play Controls");
            public GUIContent HeaderParameters = new GUIContent("Parameters");

            public GUIContent AssetPath = new GUIContent("Asset Template");
            public GUIContent RandomSeed = new GUIContent("Random Seed");
            public GUIContent ReseedOnPlay = new GUIContent("Reseed on play");
            public GUIContent OpenEditor = new GUIContent("Edit");
            public GUIContent SetRandomSeed = new GUIContent("Reseed");
            public GUIContent SetPlayRate = new GUIContent("Set");
            public GUIContent PlayRate = new GUIContent("PlayRate");
            public GUIContent ResetOverrides = new GUIContent("Reset");

            public GUIContent ButtonRestart = new GUIContent();
            public GUIContent ButtonPlay = new GUIContent();
            public GUIContent ButtonPause = new GUIContent();
            public GUIContent ButtonStop = new GUIContent();
            public GUIContent ButtonFrameAdvance = new GUIContent();

            public GUIContent ToggleWidget = new GUIContent();

            public GUIContent infoButton = new GUIContent("Debug", EditorGUIUtility.IconContent("console.infoicon").image);
        }
    }
}
