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


    //[CustomEditor(typeof(VisualEffect))]
    public class VisualEffectEditor : Editor
    {
        protected SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;

        protected void OnEnable()
        {
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");
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

        protected const float overrideWidth = 16;

        void DisplayProperty(VFXParameterInfo parameter, SerializedProperty overrideProperty, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent nameContent = EditorGUIUtility.TextContent(parameter.name);

            //EditorGUI.showMixedValue = overrideProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool result = EditorGUILayout.Toggle(overrideProperty.hasMultipleDifferentValues ? false : overrideProperty.boolValue, overrideProperty.hasMultipleDifferentValues ? Styles.toggleMixedStyle : Styles.toggleStyle, GUILayout.Width(overrideWidth));
            if (EditorGUI.EndChangeCheck())
            {
                overrideProperty.boolValue = result;
            }
            //EditorGUI.showMixedValue = false;

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

        private void SceneViewGUICallback(UnityObject target, SceneView sceneView)
        {
            VisualEffect effect = ((VisualEffect)targets[0]);

            var buttonWidth = GUILayout.Width(50);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Stop), buttonWidth))
            {
                effect.ControlStop();
            }
            if (effect.pause)
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Play), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }
            else
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Pause), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }


            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Step), buttonWidth))
            {
                effect.ControlStep();
            }
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Restart), buttonWidth))
            {
                effect.ControlRestart();
            }
            GUILayout.EndHorizontal();

            float playRate = effect.playRate * VisualEffectControl.playRateToValue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Contents.playRate, GUILayout.Width(44));
            playRate = EditorGUILayout.PowerSlider("", playRate, VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, GUILayout.Width(138));
            effect.playRate = playRate * VisualEffectControl.valueToPlayRate;
            if (EditorGUILayout.DropdownButton(Contents.setPlayRate, FocusType.Passive, GUILayout.Width(36)))
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
            SceneViewOverlay.Window(Contents.headerPlayControls, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
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

        protected virtual void EditorModeInspectorButton()
        {
        }

        protected void ShowHeader(GUIContent nameContent)
        {
            float height = Styles.categoryHeader.CalcHeight(nameContent, 4000);
            Rect rect = GUILayoutUtility.GetRect(1, height + Styles.headerTopMargin + Styles.headerBottomMargin);

            rect.width += rect.x;
            rect.x = 0;
            rect.y += Styles.headerTopMargin;
            rect.height -= Styles.headerTopMargin + Styles.headerBottomMargin;
            if (Event.current.type == EventType.Repaint)
                Styles.categoryHeader.Draw(rect, nameContent, false, true, true, false);
        }

        protected virtual void AssetField()
        {
            var component = (VisualEffect)target;
            EditorGUILayout.PropertyField(m_VisualEffectAsset, Contents.assetPath);
        }

        protected virtual bool SeedField()
        {
            var component = (VisualEffect)target;
            //Seed
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(m_ReseedOnPlay.boolValue))
                {
                    EditorGUILayout.PropertyField(m_RandomSeed, Contents.randomSeed);
                    if (GUILayout.Button(Contents.setRandomSeed, EditorStyles.miniButton, Styles.MiniButtonWidth))
                    {
                        m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                        component.startSeed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                    }
                }
            }
            EditorGUILayout.PropertyField(m_ReseedOnPlay, Contents.reseedOnPlay);
            return EditorGUI.EndChangeCheck();
        }

        public override void OnInspectorGUI()
        {
            AssetField();
            bool reinit = SeedField();


            var component = (VisualEffect)target;
            //Display properties only if all the VisualEffects share the same graph
            VisualEffectAsset asset = component.visualEffectAsset;
            if (targets.Length > 1)
            {
                foreach (VisualEffect effect in targets)
                {
                    if (effect.visualEffectAsset != asset)
                    {
                        return;
                    }
                }
            }

            EditorModeInspectorButton();

            DrawParameters();

            serializedObject.ApplyModifiedProperties();
            if (reinit)
            {
                component.Reinit();
            }

            GUI.enabled = true;
        }

        protected virtual void DrawParameters()
        {
            var component = (VisualEffect)target;
            if (m_graph == null || m_asset != component.visualEffectAsset)
            {
                m_asset = component.visualEffectAsset;
                if (m_asset != null)
                {
                    m_graph = m_asset.GetResource().GetOrCreateGraph();
                }
            }

            GUI.enabled = true;

            if (m_graph != null)
            {
                if (m_graph.m_ParameterInfo == null)
                {
                    m_graph.BuildParameterInfo();
                }

                if (m_graph.m_ParameterInfo != null)
                {
                    ShowHeader(Contents.headerParameters);
                    List<int> stack = new List<int>();
                    int currentCount = m_graph.m_ParameterInfo.Length;
                    if (currentCount == 0)
                    {
                        GUILayout.Label("No Parameter exposed in the asset");
                    }

                    foreach (var parameter in m_graph.m_ParameterInfo)
                    {
                        --currentCount;

                        if (parameter.descendantCount > 0)
                        {
                            stack.Add(currentCount);
                            currentCount = parameter.descendantCount;
                        }

                        if (currentCount == 0 && stack.Count > 0)
                        {
                            do
                            {
                                currentCount = stack.Last();
                                stack.RemoveAt(stack.Count - 1);
                            }
                            while (currentCount == 0);
                        }


                        if (string.IsNullOrEmpty(parameter.sheetType))
                        {
                            if (!string.IsNullOrEmpty(parameter.name))
                            {
                                if (string.IsNullOrEmpty(parameter.realType)) // This is a category
                                {
                                    var nameContent = new GUIContent(parameter.name);
                                    ShowHeader(nameContent);
                                }
                                else
                                    EmptyLineControl(parameter.name, stack.Count);
                            }
                        }
                        else
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
        }

        protected static class Contents
        {
            public static readonly GUIContent headerPlayControls = EditorGUIUtility.TrTextContent("Play Controls");
            public static readonly GUIContent headerParameters = EditorGUIUtility.TrTextContent("Parameters");

            public static readonly GUIContent assetPath = EditorGUIUtility.TrTextContent("Asset Template");
            public static readonly GUIContent randomSeed = EditorGUIUtility.TrTextContent("Random Seed");
            public static readonly GUIContent reseedOnPlay = EditorGUIUtility.TrTextContent("Reseed on play");
            public static readonly GUIContent openEditor = EditorGUIUtility.TrTextContent("Edit");
            public static readonly GUIContent setRandomSeed = EditorGUIUtility.TrTextContent("Reseed");
            public static readonly GUIContent setPlayRate = EditorGUIUtility.TrTextContent("Set");
            public static readonly GUIContent playRate = EditorGUIUtility.TrTextContent("PlayRate");

            static readonly GUIContent[] m_Icons;

            public enum Icon
            {
                Pause,
                Play,
                Restart,
                Step,
                Stop
            }
            static Contents()
            {
                m_Icons = new GUIContent[1 + (int)Icon.Stop];
                for (int i = 0; i <= (int)Icon.Stop; ++i)
                {
                    Icon icon = (Icon)i;
                    string name = icon.ToString();

                    //TODO replace with editor default resource call when going to trunk
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFXEditor/Editor/SceneWindow/Textures/" + name + ".png");
                    if (texture == null)
                    {
                        Debug.LogError("Can't find icon for " + name + " in Styles");
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

        public static GUISkin GetCurrentSkin()
        {
            return EditorGUIUtility.isProSkin ? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene) : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        }

        protected static class Styles
        {
            public static readonly GUIStyle toggleStyle;
            public static readonly GUIStyle toggleMixedStyle;

            public static readonly GUIStyle categoryHeader;
            public const float headerTopMargin = 8;
            public const float headerBottomMargin = 4;

            public static readonly GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
            public static readonly GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

            static Styles()
            {
                var builtInSkin = GetCurrentSkin();
                toggleStyle = builtInSkin.GetStyle("ShurikenCheckMark");
                toggleMixedStyle = builtInSkin.GetStyle("ShurikenCheckMarkMixed");
                categoryHeader = new GUIStyle(builtInSkin.label);
                categoryHeader.fontStyle = FontStyle.Bold;
                categoryHeader.border.left = 2;
                categoryHeader.padding.left = 14;
                categoryHeader.border.right = 2;
                //TODO change to editor resources calls
                categoryHeader.normal.background = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "VFX/cat-background-dark" : "VFX/cat-background-light");
            }
        }
    }
}
