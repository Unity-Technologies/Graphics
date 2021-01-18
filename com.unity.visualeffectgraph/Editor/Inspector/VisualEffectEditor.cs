#define WORKAROUND_TIMELINE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using EditMode = UnityEditorInternal.EditMode;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    static class VisualEffectControl
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


    class VisualEffectEditor : Editor
    {
        const string kGeneralFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.General";
        const string kRendererFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Renderer";
        const string kPropertyFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Properties";

        bool showGeneralCategory;

        bool showRendererCategory;
        bool showPropertyCategory;

        protected SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_InitialEventName;
        SerializedProperty m_InitialEventNameOverriden;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;

        RendererEditor m_RendererEditor;

        static SerializedObject s_FakeObjectSerializedCache;

        static List<VisualEffectEditor> s_AllEditors = new List<VisualEffectEditor>();

        static public void RepaintAllEditors()
        {
            foreach (var ed in s_AllEditors)
            {
                ed.Repaint();
            }
        }

        SerializedObject m_SingleSerializedObject;
        SerializedObject[] m_OtherSerializedObjects;

        protected void OnEnable()
        {
            m_SingleSerializedObject = targets.Length == 1 ? serializedObject : new SerializedObject(targets[0]);
            showPropertyCategory = EditorPrefs.GetBool(kPropertyFoldoutStatePreferenceName, true);
            showRendererCategory = EditorPrefs.GetBool(kRendererFoldoutStatePreferenceName, true);
            showGeneralCategory = EditorPrefs.GetBool(kGeneralFoldoutStatePreferenceName, true);

            if (targets.Length > 1)
            {
                m_OtherSerializedObjects = new SerializedObject[targets.Length - 1];
                for (int i = 1; i < targets.Length; ++i)
                {
                    m_OtherSerializedObjects[i - 1] = new SerializedObject(targets[i]);
                }
            }
            s_AllEditors.Add(this);
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_InitialEventName = serializedObject.FindProperty("m_InitialEventName");
            m_InitialEventNameOverriden = serializedObject.FindProperty("m_InitialEventNameOverriden");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = m_SingleSerializedObject.FindProperty("m_PropertySheet");

            var renderers = targets.Cast<Component>().Select(t => t.GetComponent<VFXRenderer>()).ToArray();
            m_RendererEditor = new RendererEditor(renderers);

            s_FakeObjectSerializedCache = new SerializedObject(targets[0]);
            SceneView.duringSceneGui += OnSceneViewGUI;
        }

        protected void OnDisable()
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            if (effect != null)
            {
                effect.pause = false;
                effect.playRate = 1.0f;
            }
            OnDisableWithoutResetting();
        }

        protected void OnDisableWithoutResetting()
        {
            SceneView.duringSceneGui -= OnSceneViewGUI;

            s_AllEditors.Remove(this);
        }

        protected const float overrideWidth = 16;

        static private bool GenerateMultipleField(ref VFXParameterInfo parameter, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Vector4 && parameter.realType != typeof(Color).Name)
            {
                return true;
            }
            else if (property.propertyType == SerializedPropertyType.Vector3 || property.propertyType == SerializedPropertyType.Vector2)
            {
                return true;
            }

            return false;
        }

        bool DisplayProperty(ref VFXParameterInfo parameter, GUIContent nameContent, SerializedProperty overridenProperty, SerializedProperty valueProperty, bool overrideMixed, bool valueMixed, out bool overriddenChanged)
        {
            if (parameter.realType == typeof(Matrix4x4).Name)
            {
                overriddenChanged = false;
                return false;
            }
            EditorGUILayout.BeginHorizontal();
            
            var height = 18f;
            if (!EditorGUIUtility.wideMode && GenerateMultipleField(ref parameter, valueProperty))
            {
                height *= 2.0f;
                height += 1;
            }

            var rect = EditorGUILayout.GetControlRect(false, height);

            var toggleRect = rect;
            toggleRect.x += EditorGUI.indentLevel * 16;
            toggleRect.yMin += 2.0f;
            toggleRect.width = 18;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(toggleRect, GUIContent.none, overridenProperty);
            bool newOverriden = EditorGUI.Toggle(toggleRect, overrideMixed ? false : overridenProperty.boolValue, overrideMixed ? Styles.toggleMixedStyle : Styles.toggleStyle);
            EditorGUI.EndProperty();
            overriddenChanged = EditorGUI.EndChangeCheck();
            if (overriddenChanged)
            {
                overridenProperty.boolValue = newOverriden;
            }
            rect.xMin += overrideWidth + EditorGUI.indentLevel * 16;

            int saveIndent = EditorGUI.indentLevel; // since we already applied the indentLevel to the rect reset it to zero.
            EditorGUI.indentLevel = 0;
            bool changed = false;
            if (!valueMixed)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginProperty(rect, nameContent, valueProperty);

                if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                {
                    if (valueProperty.propertyType == SerializedPropertyType.Float)
                        EditorGUI.Slider(rect, valueProperty, parameter.min, parameter.max, nameContent);
                    else
                        EditorGUI.IntSlider(rect, valueProperty, (int)parameter.min, (int)parameter.max, nameContent);
                }
                else if( parameter.enumValues != null && parameter.enumValues.Count > 0)
                {
                    long currentValue = valueProperty.longValue;
                    int newIndex = EditorGUI.Popup(rect, nameContent, (int)currentValue, parameter.enumValues.ToArray());
                    if (newIndex != currentValue)
                    {
                        valueProperty.longValue = newIndex;
                    }
                }
                else if (parameter.realType == typeof(Color).Name)
                {
                    Vector4 vVal = valueProperty.vector4Value;
                    Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                    c = EditorGUI.ColorField(rect, nameContent, c, true, true, true);

                    if (GUI.changed)
                        valueProperty.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
                }
                else if (parameter.realType == typeof(Gradient).Name)

                {
                    Gradient newGradient = EditorGUI.GradientField(rect, nameContent, valueProperty.gradientValue, true,ColorSpace.Linear);

                    if (GUI.changed)
                        valueProperty.gradientValue = newGradient;
                }
                else if (valueProperty.propertyType == SerializedPropertyType.Vector4)
                {
                    SerializedProperty copy = valueProperty.Copy();
                    copy.Next(true);
                    EditorGUI.MultiPropertyField(rect, new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") }, copy, nameContent);
                }
                else if (valueProperty.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Type objTyp = typeof(UnityObject);
                    if (!string.IsNullOrEmpty(parameter.realType))
                    {
                        if (parameter.realType.StartsWith("Texture") || parameter.realType.StartsWith("Cubemap"))
                        {
                            objTyp = typeof(Texture);
                        }
                        else if (parameter.realType == "Mesh")
                        {
                            objTyp = typeof(Mesh);
                        }
                    }
                    EditorGUI.ObjectField(rect, valueProperty, objTyp, nameContent);
                }
                else
                {
                    EditorGUI.PropertyField(rect, valueProperty, nameContent, true);
                }
                EditorGUI.indentLevel = saveIndent;
                EditorGUI.EndProperty();
                changed = EditorGUI.EndChangeCheck();
            }
            else
            {
                EditorGUI.showMixedValue = true;
                switch (valueProperty.propertyType)
                {
                    case SerializedPropertyType.Vector4:
                        if (parameter.realType == typeof(Color).Name)
                        {
                            Vector4 vVal = valueProperty.vector4Value;
                            Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                            c = EditorGUI.ColorField(rect, nameContent, c, true, true, true);

                            if (GUI.changed)
                            {
                                valueProperty.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
                                changed = true;
                            }
                        }
                        else
                        {
                            Vector4 result = EditorGUI.Vector4Field(rect, nameContent, Vector4.zero);
                            if (GUI.changed)
                            {
                                valueProperty.vector4Value = result;
                                changed = true;
                            }
                        }
                        break;
                    case SerializedPropertyType.Vector3:
                    {
                        Vector3 result = EditorGUI.Vector3Field(rect, nameContent, Vector3.zero);
                        if (GUI.changed)
                        {
                            valueProperty.vector3Value = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.Vector2:
                    {
                        Vector2 result = EditorGUI.Vector2Field(rect, nameContent, Vector2.zero);
                        if (GUI.changed)
                        {
                            valueProperty.vector2Value = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.ObjectReference:
                    {
                        Type objTyp = typeof(UnityObject);
                        if (!string.IsNullOrEmpty(parameter.realType))
                        {
                            if (parameter.realType.StartsWith("Texture") || parameter.realType.StartsWith("Cubemap"))
                            {
                                objTyp = typeof(Texture);
                            }
                            else if (parameter.realType == "Mesh")
                            {
                                objTyp = typeof(Mesh);
                            }
                        }
                        UnityObject result = EditorGUI.ObjectField(rect, nameContent, null, objTyp, false);
                        if (GUI.changed)
                        {
                            valueProperty.objectReferenceValue = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.Float:
                        if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                        {
                            float value = EditorGUI.Slider(rect, nameContent, 0, parameter.min, parameter.max);
                            if (GUI.changed)
                            {
                                valueProperty.floatValue = value;
                                changed = true;
                            }
                        }
                        else
                        {
                            float value = EditorGUI.FloatField(rect, nameContent, 0);
                            if (GUI.changed)
                            {
                                valueProperty.floatValue = value;
                                changed = true;
                            }
                        }
                        break;
                    case SerializedPropertyType.Integer:
                        if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                        {
                            int value = EditorGUI.IntSlider(rect, nameContent, 0, (int)parameter.min, (int)parameter.max);
                            if (GUI.changed)
                            {
                                valueProperty.intValue = value;
                                changed = true;
                            }
                        }
                        else if (parameter.enumValues != null && parameter.enumValues.Count > 0)
                        {
                            int newIndex = EditorGUI.Popup(rect, nameContent, (int)0, parameter.enumValues.ToArray());
                            if (GUI.changed)
                            {
                                valueProperty.intValue = newIndex;
                                changed = true;
                            }
                        }
                        else
                        {
                            int value = EditorGUI.IntField(rect, nameContent, 0);
                            if (GUI.changed)
                            {
                                valueProperty.intValue = value;
                                changed = true;
                            }
                        }
                        break;
                    default:
                        if (parameter.realType == typeof(Gradient).Name)
                        {
                            Gradient newGradient = EditorGUI.GradientField(rect, nameContent, s_DefaultGradient, true);

                            if (GUI.changed)
                            {
                                valueProperty.gradientValue = newGradient;
                                changed = true;
                            }
                        }
                        break;
                }
                EditorGUI.showMixedValue = false;
            }
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        static Gradient s_DefaultGradient = new Gradient();

        protected static object GetObjectValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Gradient:
                    return prop.gradientValue;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
            }
            return null;
        }

        protected static void SetObjectValue(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    prop.floatValue = (float)value;
                    return;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = (Vector3)value;
                    return;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = (Vector2)value;
                    return;
                case SerializedPropertyType.Vector4:
                    if (value is Color)
                        prop.vector4Value = (Vector4)(Color)value;
                    else
                        prop.vector4Value = (Vector4)value;
                    return;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = (UnityEngine.Object)value;
                    return;
                case SerializedPropertyType.Integer:
                    if (value is uint)
                        prop.longValue = (int)(uint)value;
                    else
                        prop.intValue = (int)value;
                    return;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = (bool)value;
                    return;
                case SerializedPropertyType.Gradient:
                    prop.gradientValue = (Gradient)value;
                    return;
                case SerializedPropertyType.AnimationCurve:
                    prop.animationCurveValue = (AnimationCurve)value;
                    return;
            }
        }

        protected virtual void SceneViewGUICallback(UnityObject target, SceneView sceneView)
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            if (effect == null)
                return;

            var buttonWidth = GUILayout.Width(52);
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
            GUILayout.Label(Contents.playRate, GUILayout.Width(46));
            playRate = EditorGUILayout.PowerSlider("", playRate, VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, GUILayout.Width(124));
            effect.playRate = playRate * VisualEffectControl.valueToPlayRate;

            var eventType = Event.current.type;
            if (EditorGUILayout.DropdownButton(Contents.setPlayRate, FocusType.Passive, GUILayout.Width(40)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var value in VisualEffectControl.setPlaybackValues)
                {
                    menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
                }
                var savedEventType = Event.current.type;
                Event.current.type = eventType;
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                Event.current.type = savedEventType;
                menu.DropDown(buttonRect);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show Bounds", GUILayout.Width(192));
            VisualEffectUtility.renderBounds = EditorGUILayout.Toggle(VisualEffectUtility.renderBounds, GUILayout.Width(18));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show Event Tester", GUILayout.Width(192));
            VFXEventTesterWindow.visible = EditorGUILayout.Toggle(VFXEventTesterWindow.visible, GUILayout.Width(18));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Play()")))
                effect.Play();
            if (GUILayout.Button(new GUIContent("Stop()")))
                effect.Stop();
            GUILayout.EndHorizontal();
        }

        void SetPlayRate(object value)
        {
            float rate = (float)((int)value)  * VisualEffectControl.valueToPlayRate;
            VisualEffect effect = ((VisualEffect)targets[0]);
            effect.playRate = rate;
        }

        protected virtual void OnSceneViewGUI(SceneView sv)
        {
            SceneViewOverlay.Window(Contents.headerPlayControls, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, target, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        private VFXGraph m_graph;

        protected struct NameNTooltip
        {
            public string name;
            public string tooltip;

            public override int GetHashCode()
            {
                if (name == null)
                    return 0;

                if (tooltip == null)
                    return name.GetHashCode();

                return name.GetHashCode() ^ (tooltip.GetHashCode() << sizeof(int) * 4);
            }
        }


        static Dictionary<NameNTooltip, GUIContent> s_ContentCache = new Dictionary<NameNTooltip, GUIContent>();


        protected GUIContent GetGUIContent(string name, string tooltip = null)
        {
            GUIContent result = null;
            var nnt = new NameNTooltip { name = name, tooltip = tooltip };
            if (!s_ContentCache.TryGetValue(nnt, out result))
            {
                s_ContentCache[nnt] = result = new GUIContent(name, tooltip);
            }

            return result;
        }

        protected virtual void EmptyLineControl(string name, string tooltip, int depth, VisualEffectResource resource)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(overrideWidth); // the 4 is so that Labels are aligned with elements having an override toggle.
            EditorGUILayout.LabelField(GetGUIContent(name, tooltip));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        protected virtual void EditorModeInspectorButton()
        {
        }

        public static bool ShowHeader(GUIContent nameContent, bool displayFoldout, bool foldoutState)
        {
            float height = Styles.categoryHeader.CalcHeight(nameContent, 4000) + 3;

            Rect rect = GUILayoutUtility.GetRect(1, height - 1);

            rect.width += rect.x;
            rect.x = 0;

            if (Event.current.type == EventType.Repaint)
                Styles.categoryHeader.Draw(rect, nameContent, false, true, true, false);

            bool result = false;
            if (displayFoldout)
            {
                rect.x += 14;
                rect.y += 2;
                rect.width -= 2;
                result = EditorGUI.Toggle(rect, foldoutState, Styles.foldoutStyle);
            }

            EditorGUI.indentLevel = result ? 1 : 0;

            return result;
        }

        protected virtual void AssetField(VisualEffectResource resource)
        {
            EditorGUILayout.PropertyField(m_VisualEffectAsset, Contents.assetPath);
        }

        void SeedField()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(m_ReseedOnPlay.boolValue || m_ReseedOnPlay.hasMultipleDifferentValues))
                {
                    EditorGUILayout.PropertyField(m_RandomSeed, Contents.randomSeed);
                    if (GUILayout.Button(Contents.setRandomSeed, EditorStyles.miniButton, Styles.MiniButtonWidth))
                    {
                        foreach (VisualEffect ve in targets)
                        {
                            var singleSerializedObject = new SerializedObject(ve);
                            var singleProperty = singleSerializedObject.FindProperty("m_StartSeed");
                            singleProperty.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                            singleSerializedObject.ApplyModifiedProperties();
                            ve.startSeed = (uint)singleProperty.intValue;
                        }
                        serializedObject.Update();
                    }
                }
            }
            EditorGUILayout.PropertyField(m_ReseedOnPlay, Contents.reseedOnPlay);
        }

        static readonly GUIContent exampleGUIContent = new GUIContent("Aq");

        void InitialEventField(VisualEffectResource resource)
        {
            if (m_InitialEventName == null)
                return;

            bool changed = false;
            using (new GUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect(false, GUI.skin.textField.CalcHeight(exampleGUIContent, 10000));
                var toggleRect = rect;
                toggleRect.yMin += 2.0f;
                toggleRect.width = overrideWidth;

                s_FakeObjectSerializedCache.Update();
                var fakeInitialEventNameField = s_FakeObjectSerializedCache.FindProperty("m_InitialEventName");
                fakeInitialEventNameField.stringValue = resource != null ? resource.initialEventName : "OnPlay";

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginProperty(toggleRect, GUIContent.none, m_InitialEventNameOverriden);
                bool resultOverriden = EditorGUI.Toggle(toggleRect, m_InitialEventNameOverriden.boolValue, Styles.toggleStyle);
                EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                {
                    m_InitialEventNameOverriden.boolValue = resultOverriden;
                    changed = true;
                }

                rect.xMin += overrideWidth;
                var save = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.BeginChangeCheck();

                SerializedProperty intialEventName = m_InitialEventNameOverriden.boolValue ? m_InitialEventName : fakeInitialEventNameField;

                EditorGUI.PropertyField(rect, intialEventName);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!m_InitialEventNameOverriden.boolValue)
                    {
                        m_InitialEventNameOverriden.boolValue = true;
                        s_FakeObjectSerializedCache.ApplyModifiedPropertiesWithoutUndo();
                        m_InitialEventName.stringValue = intialEventName.stringValue;
                    }
                    changed = true;
                }
                EditorGUI.indentLevel = save;
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        bool ShowCategory(GUIContent nameContent, bool foldoutState)
        {
            //bool currentState = EditorGUILayout.Toggle(GUIContent.none, prevState, Styles.foldoutStyle);

            float height = Styles.foldoutStyle.CalcHeight(nameContent, 4000) + 3;

            Rect rect = GUILayoutUtility.GetRect(1, height);

            rect.width += rect.x;
            rect.x += Styles.foldoutStyle.CalcSize(GUIContent.none).x;
            rect.y += 3;

            EditorGUI.LabelField(rect, nameContent, EditorStyles.boldLabel);

            bool result = false;
            rect.x = 14;

            rect.width -= 2;
            result = EditorGUI.Toggle(rect, foldoutState, Styles.foldoutStyle);

            return result;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(6);
            bool newShowGeneralCategory = ShowHeader(Contents.headerGeneral, true, showGeneralCategory);
            if (newShowGeneralCategory != showGeneralCategory)
            {
                EditorPrefs.SetBool(kGeneralFoldoutStatePreferenceName, newShowGeneralCategory);
                showGeneralCategory = newShowGeneralCategory;
            }
            m_SingleSerializedObject.Update();
            if (m_OtherSerializedObjects != null) // copy the set value to all multi selection by hand, because it might not be at the same array index or already present in the property sheet
            {
                foreach (var serobj in m_OtherSerializedObjects)
                {
                    serobj.Update();
                }
            }

            VisualEffectResource resource = null;
            if (!m_VisualEffectAsset.hasMultipleDifferentValues)
            {
                VisualEffect effect = ((VisualEffect)targets[0]);
                var asset = effect.visualEffectAsset;
                if (asset != null)
                {
                    resource = asset.GetResource(); //This resource could be null if asset is actually in an AssetBundle
                }
            }

            if (showGeneralCategory)
            {
                AssetField(resource);
                SeedField();
            }

            if (!m_VisualEffectAsset.hasMultipleDifferentValues)
            {
                if (showGeneralCategory)
                    InitialEventField(resource);

                DrawRendererProperties();
                DrawParameters(resource);
            }
            EditorGUI.indentLevel = 0;
            serializedObject.ApplyModifiedProperties();
        }


        Dictionary<string, Dictionary<string, SerializedProperty>> m_PropertyToProp = new Dictionary<string, Dictionary<string, SerializedProperty>>();

        protected virtual void DrawParameters(VisualEffectResource resource)
        {
            var component = (VisualEffect)target;
            VFXGraph graph = null;
            if (resource != null)
                graph = resource.GetOrCreateGraph();


            if (graph == null)
            {
                ShowHeader(Contents.headerProperties, true, showPropertyCategory);
                EditorGUILayout.HelpBox(Contents.graphInBundle.text.ToString(), MessageType.Info, true);
            }
            else
            {
                if (graph.m_ParameterInfo == null)
                {
                    graph.BuildParameterInfo();
                }

                m_PropertyToProp.Clear();

                foreach (var sheetType in graph.m_ParameterInfo.Select(t => t.sheetType).Where(t=>!string.IsNullOrEmpty(t)).Distinct())
                {
                    var nameToIndices = new Dictionary<string, SerializedProperty>();

                    var sourceVfxField = m_VFXPropertySheet.FindPropertyRelative(sheetType + ".m_Array");
                    for (int i = 0; i < sourceVfxField.arraySize; ++i)
                    {
                        SerializedProperty sourceProperty = sourceVfxField.GetArrayElementAtIndex(i);
                        var nameProperty = sourceProperty.FindPropertyRelative("m_Name").stringValue;

                        nameToIndices[nameProperty] = sourceProperty;
                    }
                    m_PropertyToProp[sheetType] = nameToIndices;
                }


                if (graph.m_ParameterInfo != null)
                {
                    bool newShowParameterCategory = ShowHeader(Contents.headerProperties, true, showPropertyCategory);
                    if (newShowParameterCategory != showPropertyCategory)
                    {
                        EditorPrefs.SetBool(kPropertyFoldoutStatePreferenceName, newShowParameterCategory);
                        showPropertyCategory = newShowParameterCategory;
                    }

                    if (showPropertyCategory)
                    {
                        var stack = new List<int>();
                        int currentCount = graph.m_ParameterInfo.Length;
                        if (currentCount == 0)
                        {
                            GUILayout.Label("No Property exposed in the Visual Effect Graph");
                        }
                        else
                        {
                            EditorModeInspectorButton();
                        }

                        bool ignoreUntilNextCat = false;

                        foreach (var param in graph.m_ParameterInfo)
                        {
                            EditorGUI.indentLevel = stack.Count;
                            --currentCount;

                            var parameter = param;
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
                                        bool wasIgnored = ignoreUntilNextCat;
                                        ignoreUntilNextCat = false;
                                        var nameContent = GetGUIContent(parameter.name);

                                        bool prevState = EditorPrefs.GetBool("VFX-category-" + parameter.name, true);
                                        bool currentState = ShowCategory(nameContent, prevState);

                                        if (currentState != prevState)
                                        {
                                            EditorPrefs.SetBool("VFX-category-" + parameter.name, currentState);
                                        }

                                        if (!currentState)
                                            ignoreUntilNextCat = true;
                                    }
                                    else if (!ignoreUntilNextCat)
                                        EmptyLineControl(parameter.name, parameter.tooltip, stack.Count, resource);
                                }
                            }
                            else if (!ignoreUntilNextCat)
                            {   
                                SerializedProperty sourceProperty = null;

                                m_PropertyToProp[parameter.sheetType].TryGetValue(parameter.path, out sourceProperty);

                                //< Prepare potential indirection
                                bool wasNewProperty = false;
                                bool wasNotOverriddenProperty = false;

                                SerializedProperty actualDisplayedPropertyValue = null;
                                SerializedProperty actualDisplayedPropertyOverridden = null;
                                if (sourceProperty == null)
                                {
                                    s_FakeObjectSerializedCache.Update();
                                    var fakeField = s_FakeObjectSerializedCache.FindProperty("m_PropertySheet." + parameter.sheetType + ".m_Array");
                                    fakeField.InsertArrayElementAtIndex(fakeField.arraySize);
                                    var newFakeEntry = fakeField.GetArrayElementAtIndex(fakeField.arraySize - 1);
                                    newFakeEntry.FindPropertyRelative("m_Name").stringValue = param.path;
                                    newFakeEntry.FindPropertyRelative("m_Overridden").boolValue = false;

                                    actualDisplayedPropertyOverridden = newFakeEntry.FindPropertyRelative("m_Overridden");
                                    actualDisplayedPropertyValue = newFakeEntry.FindPropertyRelative("m_Value");
                                    SetObjectValue(actualDisplayedPropertyValue, parameter.defaultValue.Get());

                                    wasNewProperty = true;
                                }
                                else
                                {
                                    actualDisplayedPropertyOverridden = sourceProperty.FindPropertyRelative("m_Overridden");
                                    actualDisplayedPropertyValue = sourceProperty.FindPropertyRelative("m_Value");
                                    if (!actualDisplayedPropertyOverridden.boolValue)
                                    {
                                        s_FakeObjectSerializedCache.Update();

                                        actualDisplayedPropertyOverridden = s_FakeObjectSerializedCache.FindProperty(actualDisplayedPropertyOverridden.propertyPath);
                                        actualDisplayedPropertyValue = s_FakeObjectSerializedCache.FindProperty(actualDisplayedPropertyValue.propertyPath);
                                        SetObjectValue(actualDisplayedPropertyValue, parameter.defaultValue.Get());

                                        wasNotOverriddenProperty = true;
                                    }
                                }

                                //< Actual display
                                GUIContent nameContent = GetGUIContent(parameter.name, parameter.tooltip);
                                EditorGUI.BeginChangeCheck();

                                bool wasOverriden = actualDisplayedPropertyOverridden.boolValue;

                                bool overrideMixed = false;
                                bool valueMixed = false;
                                if (m_OtherSerializedObjects != null) // copy the set value to all multi selection by hand, because it might not be at the same array index or already present in the property sheet
                                {
                                    foreach (var otherObject in m_OtherSerializedObjects)
                                    {
                                        var otherSourceVfxField = otherObject.FindProperty("m_PropertySheet." + parameter.sheetType + ".m_Array");
                                        SerializedProperty otherSourceProperty = null;
                                        for (int i = 0; i < otherSourceVfxField.arraySize; ++i)
                                        {
                                            otherSourceProperty = otherSourceVfxField.GetArrayElementAtIndex(i);
                                            var nameProperty = otherSourceProperty.FindPropertyRelative("m_Name").stringValue;
                                            if (nameProperty == parameter.path)
                                            {
                                                break;
                                            }
                                            otherSourceProperty = null;
                                        }

                                        if (otherSourceProperty != null)
                                        {
                                            overrideMixed = overrideMixed || (wasOverriden != otherSourceProperty.FindPropertyRelative("m_Overridden").boolValue);
                                        }
                                        else
                                        {
                                            overrideMixed = overrideMixed || wasOverriden;
                                        }
                                        if (overrideMixed)
                                            break;
                                    }

                                    if (overrideMixed)
                                        valueMixed = true;
                                    else
                                    {
                                        foreach (var otherObject in m_OtherSerializedObjects)
                                        {
                                            var otherSourceVfxField = otherObject.FindProperty("m_PropertySheet." + parameter.sheetType + ".m_Array");
                                            SerializedProperty otherSourceProperty = null;
                                            for (int i = 0; i < otherSourceVfxField.arraySize; ++i)
                                            {
                                                otherSourceProperty = otherSourceVfxField.GetArrayElementAtIndex(i);
                                                var nameProperty = otherSourceProperty.FindPropertyRelative("m_Name").stringValue;
                                                if (nameProperty == parameter.path)
                                                    break;
                                                otherSourceProperty = null;
                                            }

                                            if (otherSourceProperty != null)
                                            {
                                                var otherValue = GetObjectValue(otherSourceProperty.FindPropertyRelative("m_Value"));
                                                if (otherValue == null)
                                                    valueMixed = valueMixed || GetObjectValue(actualDisplayedPropertyValue) != null;
                                                else
                                                    valueMixed = valueMixed || !otherValue.Equals(GetObjectValue(actualDisplayedPropertyValue));
                                            }

                                            if (valueMixed)
                                                break;
                                        }
                                    }
                                }
                                bool overridenChanged = false;
                                if (DisplayProperty(ref parameter, nameContent, actualDisplayedPropertyOverridden, actualDisplayedPropertyValue, overrideMixed, valueMixed, out overridenChanged) || overridenChanged)
                                {
                                    if (!overridenChanged)  // the value has changed
                                    {
                                        if (m_OtherSerializedObjects != null) // copy the set value to all multi selection by hand, because it might not be at the same array index or already present in the property sheet
                                        {
                                            foreach (var otherObject in m_OtherSerializedObjects)
                                            {
                                                var singleSourceVfxField = otherObject.FindProperty("m_PropertySheet." + parameter.sheetType + ".m_Array");
                                                SerializedProperty singleSourceProperty = null;
                                                for (int i = 0; i < singleSourceVfxField.arraySize; ++i)
                                                {
                                                    singleSourceProperty = singleSourceVfxField.GetArrayElementAtIndex(i);
                                                    var nameProperty = singleSourceProperty.FindPropertyRelative("m_Name").stringValue;
                                                    if (nameProperty == parameter.path)
                                                    {
                                                        break;
                                                    }
                                                    singleSourceProperty = null;
                                                }
                                                if (singleSourceProperty == null)
                                                {
                                                    singleSourceVfxField.InsertArrayElementAtIndex(singleSourceVfxField.arraySize);
                                                    var newEntry = singleSourceVfxField.GetArrayElementAtIndex(singleSourceVfxField.arraySize - 1);

                                                    newEntry.FindPropertyRelative("m_Overridden").boolValue = true;
                                                    SetObjectValue(newEntry.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                                    newEntry.FindPropertyRelative("m_Name").stringValue = param.path;
                                                    PropertyOverrideChanged();
                                                }
                                                else
                                                {
                                                    singleSourceProperty.FindPropertyRelative("m_Overridden").boolValue = true;
                                                    SetObjectValue(singleSourceProperty.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                                }
                                                otherObject.ApplyModifiedProperties();
                                            }
                                        }
                                    }
                                    if (wasNewProperty)
                                    {
                                        var sourceVfxField = m_VFXPropertySheet.FindPropertyRelative(parameter.sheetType + ".m_Array");
                                        //We start editing a new exposed value which wasn't stored in this Visual Effect Component
                                        sourceVfxField.InsertArrayElementAtIndex(sourceVfxField.arraySize);
                                        var newEntry = sourceVfxField.GetArrayElementAtIndex(sourceVfxField.arraySize - 1);

                                        newEntry.FindPropertyRelative("m_Overridden").boolValue = true;
                                        SetObjectValue(newEntry.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                        newEntry.FindPropertyRelative("m_Name").stringValue = param.path;
                                        PropertyOverrideChanged();
                                    }
                                    else if (wasNotOverriddenProperty && !overridenChanged)
                                    {
                                        if (!actualDisplayedPropertyOverridden.boolValue)
                                        {
                                            //The value has been directly changed, change overridden state and recopy new value
                                            SetObjectValue(sourceProperty.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                        }
                                        sourceProperty.FindPropertyRelative("m_Overridden").boolValue = true;
                                        PropertyOverrideChanged();
                                    }
                                    else if (wasOverriden != actualDisplayedPropertyOverridden.boolValue)
                                    {
                                        sourceProperty.FindPropertyRelative("m_Overridden").boolValue = actualDisplayedPropertyOverridden.boolValue;
                                        if (m_OtherSerializedObjects != null) // copy the set value to all multi selection by hand, because it might not be at the same array index or already present in the property sheet
                                        {
                                            foreach (var otherObject in m_OtherSerializedObjects)
                                            {
                                                var otherSourceVfxField = otherObject.FindProperty("m_PropertySheet." + parameter.sheetType + ".m_Array");
                                                SerializedProperty otherSourceProperty = null;
                                                for (int i = 0; i < otherSourceVfxField.arraySize; ++i)
                                                {
                                                    otherSourceProperty = otherSourceVfxField.GetArrayElementAtIndex(i);
                                                    var nameProperty = otherSourceProperty.FindPropertyRelative("m_Name").stringValue;
                                                    if (nameProperty == parameter.path)
                                                    {
                                                        break;
                                                    }
                                                    otherSourceProperty = null;
                                                }
                                                if (otherSourceProperty == null)
                                                {
                                                    if (!wasOverriden)
                                                    {
                                                        otherSourceVfxField.InsertArrayElementAtIndex(otherSourceVfxField.arraySize);
                                                        var newEntry = otherSourceVfxField.GetArrayElementAtIndex(otherSourceVfxField.arraySize - 1);

                                                        newEntry.FindPropertyRelative("m_Overridden").boolValue = true;
                                                        SetObjectValue(newEntry.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                                        newEntry.FindPropertyRelative("m_Name").stringValue = param.path;
                                                        PropertyOverrideChanged();
                                                    }
                                                }
                                                else
                                                {
                                                    otherSourceProperty.FindPropertyRelative("m_Overridden").boolValue = !wasOverriden;
                                                    if (!wasOverriden)
                                                    {
                                                        SetObjectValue(otherSourceProperty.FindPropertyRelative("m_Value"), GetObjectValue(actualDisplayedPropertyValue));
                                                    }
                                                    PropertyOverrideChanged();
                                                }
                                                otherObject.ApplyModifiedProperties();
                                            }
                                        }

                                        PropertyOverrideChanged();
                                    }
                                    m_SingleSerializedObject.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.Space(1); // Space for the line if the last category is closed.
        }

        protected virtual void PropertyOverrideChanged() {}

        private void DrawRendererProperties()
        {
            bool newShowRendererCategory = ShowHeader(Contents.headerRenderer, true, showRendererCategory);
            if (newShowRendererCategory != showRendererCategory)
            {
                EditorPrefs.SetBool(kRendererFoldoutStatePreferenceName, newShowRendererCategory);
                showRendererCategory = newShowRendererCategory;
            }

            if (showRendererCategory)
                m_RendererEditor.OnInspectorGUI();
        }

        internal class RendererEditor
        {
            const string kRendererProbesFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Renderer.Probes";
            const string kRendererAdditionnalSettingsFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Renderer.AdditionnalSettings";

            private VFXRenderer[] m_Renderers;
            private SerializedObject m_SerializedRenderers;

            private SerializedProperty m_RendererPriority;
            private SerializedProperty m_RenderingLayerMask;
            private SerializedProperty m_LightProbeUsage;
            private SerializedProperty m_LightProbeVolumeOverride;
            private SerializedProperty m_ReflectionProbeUsage;
            private SerializedProperty m_ProbeAnchor;

            private bool m_ShowProbesCategory;
            private bool m_ShowAdditionnalCategory;

            public RendererEditor(params VFXRenderer[] renderers)
            {
                m_ShowProbesCategory = EditorPrefs.GetBool(kRendererProbesFoldoutStatePreferenceName, true);
                m_ShowAdditionnalCategory = EditorPrefs.GetBool(kRendererAdditionnalSettingsFoldoutStatePreferenceName, true);

                m_Renderers = renderers;
                m_SerializedRenderers = new SerializedObject(m_Renderers);

                m_RendererPriority = m_SerializedRenderers.FindProperty("m_RendererPriority");
                m_RenderingLayerMask = m_SerializedRenderers.FindProperty("m_RenderingLayerMask");
                m_LightProbeUsage = m_SerializedRenderers.FindProperty("m_LightProbeUsage");
                m_LightProbeVolumeOverride = m_SerializedRenderers.FindProperty("m_LightProbeVolumeOverride");
                m_ReflectionProbeUsage = m_SerializedRenderers.FindProperty("m_ReflectionProbeUsage");
                m_ProbeAnchor = m_SerializedRenderers.FindProperty("m_ProbeAnchor");
            }

            public static readonly string[] s_DefaultRenderingLayerNames = GetDefaultRenderingLayerNames();
            private static string[] GetDefaultRenderingLayerNames()
            {
                //Find UnityEditor.RendererEditorBase.defaultRenderingLayerNames by reflection to avoid any breakage due to an API change
                var type = Type.GetType("UnityEditor.RendererEditorBase, UnityEditor");
                if (type != null)
                {
                    var property = type.GetProperty("defaultRenderingLayerNames", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
                    if (property != null)
                    {
                        var invokeResult = property.GetMethod.Invoke(null, null);
                        if (invokeResult != null && invokeResult is string[])
                            return invokeResult as string[];
                    }
                }
                return null;
            }

            public void OnInspectorGUI()
            {
                m_SerializedRenderers.Update();

                bool showProbesCategory = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowProbesCategory, Contents.probeSettings);
                if (showProbesCategory != m_ShowProbesCategory)
                {
                    m_ShowProbesCategory = showProbesCategory;
                    EditorPrefs.SetBool(kRendererProbesFoldoutStatePreferenceName, m_ShowProbesCategory);
                }

                if (m_ShowProbesCategory)
                {
                    if (m_ReflectionProbeUsage != null && SupportedRenderingFeatures.active.reflectionProbes)
                    {
                        Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                        EditorGUI.BeginProperty(r, Contents.reflectionProbeUsageStyle, m_ReflectionProbeUsage);
                        EditorGUI.BeginChangeCheck();
                        var newValue = EditorGUI.EnumPopup(r, Contents.reflectionProbeUsageStyle, (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue);
                        if (EditorGUI.EndChangeCheck())
                            m_ReflectionProbeUsage.intValue = (int)(ReflectionProbeUsage)newValue;
                        EditorGUI.EndProperty();
                    }

                    if (m_LightProbeUsage != null)
                    {
                        Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                        EditorGUI.BeginProperty(r, Contents.lightProbeUsageStyle, m_LightProbeUsage);
                        EditorGUI.BeginChangeCheck();
                        var newValue = EditorGUI.EnumPopup(r, Contents.lightProbeUsageStyle, (LightProbeUsage)m_LightProbeUsage.intValue);
                        if (EditorGUI.EndChangeCheck())
                            m_LightProbeUsage.intValue = (int)(LightProbeUsage)newValue;
                        EditorGUI.EndProperty();

                        if (!m_LightProbeUsage.hasMultipleDifferentValues && m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume)
                        {
                            if (!LightProbeProxyVolume.isFeatureSupported || !SupportedRenderingFeatures.active.lightProbeProxyVolumes)
                                EditorGUILayout.HelpBox(Contents.lightProbeVolumeUnsupportedNote.text, MessageType.Warning);
                            EditorGUILayout.PropertyField(m_LightProbeVolumeOverride, Contents.lightProbeVolumeOverrideStyle);
                        }
                    }

                    bool useReflectionProbes = m_ReflectionProbeUsage != null && !m_ReflectionProbeUsage.hasMultipleDifferentValues && (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue != ReflectionProbeUsage.Off;
                    bool lightProbesEnabled = m_LightProbeUsage != null && !m_LightProbeUsage.hasMultipleDifferentValues && (LightProbeUsage)m_LightProbeUsage.intValue != LightProbeUsage.Off;
                    bool needsProbeAnchor = useReflectionProbes || lightProbesEnabled;

                    if (needsProbeAnchor)
                        EditorGUILayout.PropertyField(m_ProbeAnchor, Contents.lightProbeAnchorStyle);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                var showAdditionnalCategory = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowAdditionnalCategory, Contents.otherSettings);
                if (showAdditionnalCategory != m_ShowAdditionnalCategory)
                {
                    m_ShowAdditionnalCategory = showAdditionnalCategory;
                    EditorPrefs.SetBool(kRendererAdditionnalSettingsFoldoutStatePreferenceName, m_ShowAdditionnalCategory);
                }

                if (showAdditionnalCategory)
                {
                    if (m_RenderingLayerMask != null)
                    {
                        string[] layerNames = null;
                        var srpAsset = GraphicsSettings.currentRenderPipeline;
                        if (srpAsset != null)
                            layerNames = srpAsset.renderingLayerMaskNames;

                        if (layerNames == null)
                            layerNames = s_DefaultRenderingLayerNames;

                        if (layerNames != null)
                        {
                            var mask = (int)m_Renderers[0].renderingLayerMask;

                            var rect = EditorGUILayout.GetControlRect();
                            EditorGUI.showMixedValue = m_RenderingLayerMask.hasMultipleDifferentValues;
                            EditorGUI.BeginProperty(rect, Contents.renderingLayerMaskStyle, m_RenderingLayerMask);
                            EditorGUI.BeginChangeCheck();

                            mask = EditorGUI.MaskField(rect, Contents.renderingLayerMaskStyle, mask, layerNames);

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(m_SerializedRenderers.targetObjects, "Set rendering layer mask");
                                foreach (var t in m_SerializedRenderers.targetObjects)
                                {
                                    var r = t as VFXRenderer;
                                    if (r != null)
                                    {
                                        r.renderingLayerMask = (uint)mask;
                                        EditorUtility.SetDirty(t);
                                    }
                                }
                            }
                            EditorGUI.EndProperty();
                            EditorGUI.showMixedValue = false;
                        }
                    }

                    if (m_RendererPriority != null && SupportedRenderingFeatures.active.rendererPriority)
                    {
                        EditorGUILayout.PropertyField(m_RendererPriority, Contents.rendererPriorityStyle);
                    }

                    //As classic MeshRenderer, VisualEffect doesn't support the single mesh batching of UI.
                    //if (m_SortingOrder != null && m_SortingLayerID != null)
                    //{
                    //    SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);
                    //}
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                m_SerializedRenderers.ApplyModifiedProperties();
            }

            private static class Contents
            {
                public static readonly GUIContent renderingLayerMaskStyle =         EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");
                public static readonly GUIContent rendererPriorityStyle =           EditorGUIUtility.TrTextContent("Priority", "Priority used for sorting objects on top of material render queue.");
                public static readonly GUIContent lightProbeUsageStyle =            EditorGUIUtility.TrTextContent("Light Probes", "Specifies how Light Probes will handle the interpolation of lighting and occlusion.");
                public static readonly GUIContent reflectionProbeUsageStyle =       EditorGUIUtility.TrTextContent("Reflection Probes", "Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");
                public static readonly GUIContent lightProbeVolumeOverrideStyle =   EditorGUIUtility.TrTextContent("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");
                public static readonly GUIContent lightProbeAnchorStyle =           EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes and reflection probes.");
                public static readonly GUIContent lightProbeVolumeUnsupportedNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");

                public static readonly GUIContent probeSettings =                   EditorGUIUtility.TrTextContent("Probes");
                public static readonly GUIContent otherSettings =                   EditorGUIUtility.TrTextContent("Additional Settings");
            }
        }

        protected static class Contents
        {
            public static readonly GUIContent headerPlayControls =  EditorGUIUtility.TrTextContent("Play Controls");
            public static readonly GUIContent headerGeneral =       EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent headerProperties =    EditorGUIUtility.TrTextContent("Properties");
            public static readonly GUIContent headerRenderer =      EditorGUIUtility.TrTextContent("Renderer");

            public static readonly GUIContent assetPath =           EditorGUIUtility.TrTextContent("Asset Template", "Sets the Visual Effect Graph asset to be used in this component.");
            public static readonly GUIContent randomSeed =          EditorGUIUtility.TrTextContent("Random Seed", "Sets the value used when determining the randomness of the graph. Using the same seed will make the Visual Effect play identically each time.");
            public static readonly GUIContent reseedOnPlay =        EditorGUIUtility.TrTextContent("Reseed on play", "When enabled, a new random seed value will be used each time the effect is played. Enable to randomize the look of this Visual Effect.");
            public static readonly GUIContent openEditor =          EditorGUIUtility.TrTextContent("Edit", "Opens the currently assigned template for editing within the Visual Effect Graph window.");
            public static readonly GUIContent createAsset =         EditorGUIUtility.TrTextContent("New", "Creates a new Visual Effect Graph and opens it for editing within the Visual Effect Graph window.");
            public static readonly GUIContent setRandomSeed =       EditorGUIUtility.TrTextContent("Reseed", "When clicked, if ‘Reseed on play’ is disabled a new random seed will be generated.");
            public static readonly GUIContent resetInitialEvent =   EditorGUIUtility.TrTextContent("Default");
            public static readonly GUIContent setPlayRate =         EditorGUIUtility.TrTextContent("Set");
            public static readonly GUIContent playRate =            EditorGUIUtility.TrTextContent("Rate");

            public static readonly GUIContent graphInBundle =       EditorGUIUtility.TrTextContent("Exposed properties are hidden in the Inspector when Visual Effect Assets are stored in Asset Bundles.");

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
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectGraphPackageInfo.assetPackagePath + "/Editor/SceneWindow/Textures/" + name + ".png");
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
            public static readonly GUIStyle foldoutStyle;
            public static readonly GUIStyle toggleStyle;
            public static readonly GUIStyle toggleMixedStyle;

            public static readonly GUIStyle categoryHeader;

            public static readonly GUILayoutOption MiniButtonWidth = GUILayout.Width(56);
            public static readonly GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

            static Styles()
            {
                var builtInSkin = GetCurrentSkin();
                foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;

                toggleStyle = new GUIStyle(builtInSkin.GetStyle("ShurikenToggle"));

                toggleMixedStyle = new GUIStyle(builtInSkin.GetStyle("ShurikenCheckMarkMixed"));
                categoryHeader = new GUIStyle(builtInSkin.label);
                categoryHeader.fontStyle = FontStyle.Bold;
                categoryHeader.border.left = 2;
                categoryHeader.padding.left = 32;
                categoryHeader.padding.top = 2;
                categoryHeader.border.right = 2;

                //TODO change to editor resources calls
                categoryHeader.normal.background = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectGraphPackageInfo.assetPackagePath + "/Editor Default Resources/" + (EditorGUIUtility.isProSkin ? "VFX/cat-background-dark.png" : "VFX/cat-background-light.png"));
            }
        }
    }
}
