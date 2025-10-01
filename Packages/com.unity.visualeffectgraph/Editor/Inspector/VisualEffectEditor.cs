#define WORKAROUND_TIMELINE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

using UnityEditor.Overlays;
using UnityEditor.VFX.UI;

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
        const string kInstancingFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Instancing";
        const string kPropertyFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Properties";

        bool showGeneralCategory;
        bool showRendererCategory;
        bool showInstancingCategory;
        bool showPropertyCategory;

        protected SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_InitialEventName;
        SerializedProperty m_InitialEventNameOverriden;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;
        SerializedProperty m_AllowInstancing;

        RendererEditor m_RendererEditor;

        static SerializedObject s_FakeObjectSerializedCache;

        static readonly List<VisualEffectEditor> s_AllEditors = new List<VisualEffectEditor>();

        public static void RepaintAllEditors()
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
            showInstancingCategory = EditorPrefs.GetBool(kInstancingFoldoutStatePreferenceName, true);
            showGeneralCategory = EditorPrefs.GetBool(kGeneralFoldoutStatePreferenceName, true);

            m_OtherSerializedObjects = targets.Skip(1).Select(x => new SerializedObject(x)).ToArray();
            s_AllEditors.Add(this);
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_InitialEventName = serializedObject.FindProperty("m_InitialEventName");
            m_InitialEventNameOverriden = serializedObject.FindProperty("m_InitialEventNameOverriden");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = m_SingleSerializedObject.FindProperty("m_PropertySheet");
            m_AllowInstancing = serializedObject.FindProperty("m_AllowInstancing");

            var renderers = targets.Cast<Component>().Select(t => t.GetComponent<VFXRenderer>()).ToArray();
            m_RendererEditor = new RendererEditor(renderers);

            s_FakeObjectSerializedCache = new SerializedObject(targets[0]);
            s_EffectUi = this;
        }

        protected void OnDisable()
        {
            OnDisableWithoutResetting();
            if (s_EffectUi == this)
                s_EffectUi = null;
        }

        protected void OnDisableWithoutResetting()
        {
            if (s_EffectUi == this)
                s_EffectUi = null;
            s_AllEditors.Remove(this);
        }

        private static bool GenerateMultipleField(ref VFXParameterInfo parameter, SerializedProperty property)
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
            if (parameter.realType == typeof(Matrix4x4).Name
                || parameter.realType == typeof(GraphicsBuffer).Name)
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
            rect.xMin += Styles.overrideWidth + EditorGUI.indentLevel * 16;

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
                else if (parameter.enumValues != null && parameter.enumValues.Count > 0)
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
                    EditorGUI.BeginChangeCheck();
                    c = EditorGUI.ColorField(rect, nameContent, c, true, true, true);

                    if (EditorGUI.EndChangeCheck())
                        valueProperty.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
                }
                else if (parameter.realType == typeof(Gradient).Name)
                {
                    EditorGUI.BeginChangeCheck();
                    Gradient newGradient = EditorGUI.GradientField(rect, nameContent, valueProperty.gradientValue, true, ColorSpace.Linear);

                    if (EditorGUI.EndChangeCheck())
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
                        else if (parameter.realType == "SkinnedMeshRenderer")
                        {
                            objTyp = typeof(SkinnedMeshRenderer);
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
                            EditorGUI.BeginChangeCheck();
                            c = EditorGUI.ColorField(rect, nameContent, c, true, true, true);

                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
                                changed = true;
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            Vector4 result = EditorGUI.Vector4Field(rect, nameContent, Vector4.zero);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.vector4Value = result;
                                changed = true;
                            }
                        }
                        break;
                    case SerializedPropertyType.Vector3:
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 result = EditorGUI.Vector3Field(rect, nameContent, Vector3.zero);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueProperty.vector3Value = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.Vector2:
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector2 result = EditorGUI.Vector2Field(rect, nameContent, Vector2.zero);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueProperty.vector2Value = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.Boolean:
                        {
                            EditorGUI.BeginChangeCheck();
                            bool result = EditorGUI.Toggle(rect, nameContent, false);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.boolValue = result;
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
                        EditorGUI.BeginChangeCheck();
                        UnityObject result = EditorGUI.ObjectField(rect, nameContent, null, objTyp, false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueProperty.objectReferenceValue = result;
                            changed = true;
                        }
                    }
                    break;
                    case SerializedPropertyType.Float:
                        if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                        {
                            EditorGUI.BeginChangeCheck();
                            float value = EditorGUI.Slider(rect, nameContent, 0, parameter.min, parameter.max);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.floatValue = value;
                                changed = true;
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            float value = EditorGUI.FloatField(rect, nameContent, 0);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.floatValue = value;
                                changed = true;
                            }
                        }
                        break;
                    case SerializedPropertyType.Integer:
                        if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                        {
                            EditorGUI.BeginChangeCheck();
                            int value = EditorGUI.IntSlider(rect, nameContent, 0, (int)parameter.min, (int)parameter.max);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.intValue = value;
                                changed = true;
                            }
                        }
                        else if (parameter.enumValues != null && parameter.enumValues.Count > 0)
                        {
                            EditorGUI.BeginChangeCheck();
                            int newIndex = EditorGUI.Popup(rect, nameContent, (int)0, parameter.enumValues.ToArray());
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.intValue = newIndex;
                                changed = true;
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            int value = EditorGUI.IntField(rect, nameContent, 0);
                            if (EditorGUI.EndChangeCheck())
                            {
                                valueProperty.intValue = value;
                                changed = true;
                            }
                        }
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        EditorGUI.BeginChangeCheck();
                        AnimationCurve animationCurve = EditorGUI.CurveField(rect, nameContent, new AnimationCurve());
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueProperty.animationCurveValue = animationCurve;
                            changed = true;
                        }
                        break;
                    case SerializedPropertyType.Gradient:
                        EditorGUI.BeginChangeCheck();
                        Gradient newGradient = EditorGUI.GradientField(rect, nameContent, s_DefaultGradient, true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valueProperty.gradientValue = newGradient;
                            changed = true;
                        }
                        break;
                    default:
                        Debug.Assert(parameter.realType != typeof(Gradient).Name);
                        break;
                }
                EditorGUI.showMixedValue = false;
            }
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        static readonly Gradient s_DefaultGradient = new Gradient();

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

        protected virtual void SceneViewGUICallback()
        {
            var effects = targets
                .OfType<VisualEffect>()
                .Where(x => x != null)
                .ToList();
            if (effects.Count == 0)
                return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Stop), Contents.sceneViewButtonWidth))
            {
                effects.ForEach(x => x.ControlStop());
            }
            if (effects.All(x => x.pause))
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Play), Contents.sceneViewButtonWidth))
                {
                    effects.ForEach(x => x.ControlPlayPause());
                }
            }
            else
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Pause), Contents.sceneViewButtonWidth))
                {
                    effects.ForEach(x => x.ControlPlayPause());
                }
            }


            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Step), Contents.sceneViewButtonWidth))
            {
                effects.ForEach(x => x.ControlStep());
            }
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Restart), Contents.sceneViewButtonWidth))
            {
                effects.ForEach(x => x.ControlRestart());
            }
            GUILayout.EndHorizontal();

            float playRate = effects[0].playRate;
            bool mixedValues = false;
            for (int i = 1; i < effects.Count; i++)
            {
                if (Math.Abs(effects[i].playRate - playRate) > 1e-5)
                {
                    mixedValues = true;
                    break;
                }
            }

            float playRateValue = playRate * VisualEffectControl.playRateToValue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Contents.playRate, Contents.playRateWidth);
            EditorGUI.showMixedValue = mixedValues;
            EditorGUI.BeginChangeCheck();
            var newPlayRateVal = EditorGUILayout.PowerSlider("", (float)Math.Round(playRateValue), VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, Contents.powerSliderWidth);
            EditorGUI.showMixedValue = false;
            bool playRateChanged = EditorGUI.EndChangeCheck();
            if (playRateChanged && playRate >= 0)
            {
                effects.ForEach(x => x.playRate = newPlayRateVal * VisualEffectControl.valueToPlayRate);
            }

            var eventType = Event.current.type;
            if (EditorGUILayout.DropdownButton(Contents.setPlayRate, FocusType.Passive, Contents.playRateDropdownWidth))
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
            GUILayout.Label("Show Bounds", Contents.showToggleLabelsWidth);
            VisualEffectUtility.renderBounds = EditorGUILayout.Toggle(VisualEffectUtility.renderBounds, Contents.showToggleWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show Event Tester", Contents.showToggleLabelsWidth);
            VFXEventTesterWindow.visible = EditorGUILayout.Toggle(VFXEventTesterWindow.visible, Contents.showToggleWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Contents.play))
                effects.ForEach(x => x.Play());
            if (GUILayout.Button(Contents.stop))
                effects.ForEach(x => x.Stop());
            GUILayout.EndHorizontal();
        }

        void SetPlayRate(object value)
        {
            float rate = (int)value * VisualEffectControl.valueToPlayRate;
            foreach (var visualEffect in targets.OfType<VisualEffect>())
            {
                visualEffect.playRate = rate;
            }
        }

        static VisualEffectEditor s_EffectUi;

        [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
        class SceneViewVFXSlotContainerOverlay : IMGUIOverlay, ITransientOverlay
        {
            const string k_OverlayId = "Scene View/Visual Effect";
            const string k_DisplayName = "Visual Effect";

            public bool visible => s_EffectUi != null;

            public override void OnGUI()
            {
                if (s_EffectUi == null)
                    return;

                s_EffectUi.SceneViewGUICallback();
            }
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

        protected virtual void EmptyLineControl(string name, string tooltip, VFXSpace? space, int depth, VisualEffectResource resource)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Styles.overrideWidth);

            if (space != null && space != VFXSpace.None)
                name += $" (in {space} Space)";

            var guiContent = GetGUIContent(name, tooltip);
            EditorGUILayout.LabelField(guiContent, EditorStyles.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        protected virtual void EditorModeInspectorButton()
        {
        }

        public static bool ShowHeader(GUIContent nameContent, bool displayFoldout, bool foldoutState, string preferenceName = null)
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
                rect.width -= 2;
                result = EditorGUI.Toggle(rect, foldoutState, Styles.foldoutStyle);
            }

            EditorGUI.indentLevel = result ? 1 : 0;

            if (preferenceName != null && result != foldoutState)
            {
                EditorPrefs.SetBool(preferenceName, result);
            }

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
                toggleRect.width = Styles.overrideWidth;

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

                rect.xMin += Styles.overrideWidth;
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
            showGeneralCategory = ShowHeader(Contents.headerGeneral, true, showGeneralCategory, kGeneralFoldoutStatePreferenceName);

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
                DrawInstancingProperties();
                DrawParameters(resource);
            }
            EditorGUI.indentLevel = 0;
            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var window in VFXViewWindow.GetAllWindows())
                {
                    window.OnVisualEffectComponentChanged(targets.Cast<VisualEffect>());
                }
            }
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

                foreach (var sheetType in graph.m_ParameterInfo.Select(t => t.sheetType).Where(t => !string.IsNullOrEmpty(t)).Distinct())
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
                    showPropertyCategory = ShowHeader(Contents.headerProperties, true, showPropertyCategory, kPropertyFoldoutStatePreferenceName);

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
                                        EmptyLineControl(parameter.name, parameter.tooltip, parameter.spaceable ? parameter.space : (VFXSpace?)null, stack.Count, resource);
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

        protected virtual void PropertyOverrideChanged() { }

        private void DrawRendererProperties()
        {
            showRendererCategory = ShowHeader(Contents.headerRenderer, true, showRendererCategory, kRendererFoldoutStatePreferenceName);

            if (showRendererCategory)
                m_RendererEditor.OnInspectorGUI();
        }

        private void DrawInstancingProperties()
        {
            showInstancingCategory = ShowHeader(Contents.headerInstancing, true, showInstancingCategory, kInstancingFoldoutStatePreferenceName);

            if (showInstancingCategory)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AllowInstancing, Contents.allowInstancing);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    foreach (var visualEffect in targets.OfType<VisualEffect>())
                    {
                        visualEffect.RecreateData();
                    }
                }
            }
        }

        internal class RendererEditor
        {
            const string kRendererProbesFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Renderer.Probes";
            const string kRendererAdditionnalSettingsFoldoutStatePreferenceName = "VFX.VisualEffectEditor.Foldout.Renderer.AdditionnalSettings";

            private VFXRenderer[] m_Renderers;
            private SerializedObject m_SerializedRenderers;

            private SerializedProperty m_RendererPriority;
            private SerializedProperty m_SortingLayerID;
            private SerializedProperty m_SortingOrder;
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
                m_SortingOrder = m_SerializedRenderers.FindProperty("m_SortingOrder");
                m_SortingLayerID = m_SerializedRenderers.FindProperty("m_SortingLayerID");
                m_RenderingLayerMask = m_SerializedRenderers.FindProperty("m_RenderingLayerMask");
                m_LightProbeUsage = m_SerializedRenderers.FindProperty("m_LightProbeUsage");
                m_LightProbeVolumeOverride = m_SerializedRenderers.FindProperty("m_LightProbeVolumeOverride");
                m_ReflectionProbeUsage = m_SerializedRenderers.FindProperty("m_ReflectionProbeUsage");
                m_ProbeAnchor = m_SerializedRenderers.FindProperty("m_ProbeAnchor");
            }

            public SerializedObject SerializedRenderers => m_SerializedRenderers;

            public static readonly Action<GUIContent, SerializedProperty, GUIStyle, GUIStyle> s_fnGetSortingLayerField = GetSortingLayerField();

            private static Action<GUIContent, SerializedProperty, GUIStyle, GUIStyle> GetSortingLayerField()
            {
                //Find UnityEditor.EditorGUILayout.SortingLayerField by reflection to avoid any breakage due to an API change
                var type = typeof(EditorGUILayout);
                var function = type.GetMethods(BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(f => f.Name == "SortingLayerField" && f.GetParameters().Length == 4);
                if (function != null)
                {
                    var parameters = function.GetParameters();
                    if (parameters[0].ParameterType == typeof(GUIContent)
                        && parameters[1].ParameterType == typeof(SerializedProperty)
                        && parameters[2].ParameterType == typeof(GUIStyle)
                        && parameters[3].ParameterType == typeof(GUIStyle))
                    {
                        return delegate (GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
                        {
                            function.Invoke(null, new object[] { label, layerID, style, labelStyle });
                        };
                    }
                }
                return null;
            }

            static void SortingLayerField(GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
            {
                if (s_fnGetSortingLayerField == null)
                    return;
                s_fnGetSortingLayerField.Invoke(label, layerID, style, labelStyle);
            }

            static bool HasPrefabOverride(SerializedProperty property)
            {
                return property != null && property.serializedObject.targetObjectsCount == 1 && property.isInstantiatedPrefab && property.prefabOverride;
            }

            public void OnInspectorGUI()
            {
                m_SerializedRenderers.Update();

                EditorGUI.indentLevel += 1;
                // Ugly hack to indent the header group because "indentLevel" is not taken into account
                var x = EditorStyles.inspectorDefaultMargins.padding.left;
                EditorStyles.inspectorDefaultMargins.padding.left -= 24;
                bool showProbesCategory = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowProbesCategory, Contents.probeSettings);
                if (showProbesCategory != m_ShowProbesCategory)
                {
                    m_ShowProbesCategory = showProbesCategory;
                    EditorPrefs.SetBool(kRendererProbesFoldoutStatePreferenceName, m_ShowProbesCategory);
                }

                if (m_ShowProbesCategory)
                {
                    bool showReflectionProbeUsage = m_ReflectionProbeUsage != null && SupportedRenderingFeatures.active.reflectionProbes;

                    var srpAssetType = GraphicsSettings.currentRenderPipelineAssetType;
                    if (srpAssetType is not null && srpAssetType.ToString().Contains("UniversalRenderPipeline"))
                    {
                        //Reflection Probe Usage option has been removed in URP but the VFXRenderer uses ReflectionProbeUsage.Off by default
                        //We are temporarily letting this option reachable until the C++ doesn't change the default value
                        showReflectionProbeUsage = m_ReflectionProbeUsage != null;
                    }

                    if (showReflectionProbeUsage)
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
                    if (m_RenderingLayerMask != null && GraphicsSettings.isScriptableRenderPipelineEnabled)
                    {
                        var mask = m_Renderers[0].renderingLayerMask;

                        EditorGUI.BeginChangeCheck();
                        mask = EditorGUILayout.RenderingLayerMaskField(Contents.renderingLayerMaskStyle, mask);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(m_SerializedRenderers.targetObjects, "Set rendering layer mask");
                            for (var i = 0; i < m_SerializedRenderers.targetObjects.Length; i++)
                            {
                                var r = m_SerializedRenderers.targetObjects[i] as VFXRenderer;
                                if (r == null)
                                    continue;
                                r.renderingLayerMask = mask;
                                EditorUtility.SetDirty(r);
                            }
                        }
                    }

                    if (m_RendererPriority != null && SupportedRenderingFeatures.active.rendererPriority)
                    {
                        EditorGUILayout.PropertyField(m_RendererPriority, Contents.rendererPriorityStyle);
                    }

                    if (m_SortingOrder != null && m_SortingLayerID != null)
                    {
                        var hasPrefabOverride = HasPrefabOverride(m_SortingLayerID);
                        SortingLayerField(Contents.sortingLayerStyle, m_SortingLayerID, hasPrefabOverride ? Contents.boldPopupStyle : EditorStyles.popup, hasPrefabOverride ? EditorStyles.boldLabel : EditorStyles.label);
                        EditorGUILayout.PropertyField(m_SortingOrder, Contents.sortingOrderStyle);
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorStyles.inspectorDefaultMargins.padding.left = x;
                EditorGUI.indentLevel -= 1;

                m_SerializedRenderers.ApplyModifiedProperties();
            }

            private static class Contents
            {
                public static readonly GUIContent renderingLayerMaskStyle = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");
                public static readonly GUIContent rendererPriorityStyle = EditorGUIUtility.TrTextContent("Priority", "Priority used for sorting objects on top of material render queue.");
                public static readonly GUIContent lightProbeUsageStyle = EditorGUIUtility.TrTextContent("Light Probes", "Specifies how Light Probes will handle the interpolation of lighting and occlusion.");
                public static readonly GUIContent reflectionProbeUsageStyle = EditorGUIUtility.TrTextContent("Reflection Probes", "Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");
                public static readonly GUIContent lightProbeVolumeOverrideStyle = EditorGUIUtility.TrTextContent("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");
                public static readonly GUIContent lightProbeAnchorStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes and reflection probes.");
                public static readonly GUIContent lightProbeVolumeUnsupportedNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");

                public static readonly GUIContent probeSettings = EditorGUIUtility.TrTextContent("Probes");
                public static readonly GUIContent otherSettings = EditorGUIUtility.TrTextContent("Additional Settings");

                public static readonly GUIContent sortingLayerStyle = EditorGUIUtility.TrTextContent("Sorting Layer", "Name of the Renderer's sorting layer");
                public static readonly GUIContent sortingOrderStyle = EditorGUIUtility.TrTextContent("Order in Layer", "Renderer's order within a sorting layer");

                public static readonly GUIStyle boldPopupStyle = new GUIStyle(EditorStyles.popup) { fontStyle = FontStyle.Bold };
            }
        }

        protected static class Contents
        {
            public static readonly GUIContent headerPlayControls = EditorGUIUtility.TrTextContent("Play Controls");
            public static readonly GUIContent headerGeneral = EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent headerProperties = EditorGUIUtility.TrTextContent("Properties");
            public static readonly GUIContent headerRenderer = EditorGUIUtility.TrTextContent("Renderer");
            public static readonly GUIContent headerInstancing = EditorGUIUtility.TrTextContent("Instancing");

            public static readonly GUIContent assetPath = EditorGUIUtility.TrTextContent("Asset Template", "Sets the Visual Effect Graph asset to be used in this component.");
            public static readonly GUIContent randomSeed = EditorGUIUtility.TrTextContent("Random Seed", "Sets the value used when determining the randomness of the graph. Using the same seed will make the Visual Effect play identically each time.");
            public static readonly GUIContent reseedOnPlay = EditorGUIUtility.TrTextContent("Reseed on play", "When enabled, a new random seed value will be used each time the effect is played. Enable to randomize the look of this Visual Effect.");
            public static readonly GUIContent openEditor = EditorGUIUtility.TrTextContent("Edit", "Opens the currently assigned template for editing within the Visual Effect Graph window.");
            public static readonly GUIContent createAsset = EditorGUIUtility.TrTextContent("New", "Creates a new Visual Effect Graph and opens it for editing within the Visual Effect Graph window.");
            public static readonly GUIContent setRandomSeed = EditorGUIUtility.TrTextContent("Reseed", "When clicked, if ‘Reseed on play’ is disabled a new random seed will be generated.");
            public static readonly GUIContent resetInitialEvent = EditorGUIUtility.TrTextContent("Default");
            public static readonly GUIContent setPlayRate = EditorGUIUtility.TrTextContent("Set");
            public static readonly GUIContent playRate = EditorGUIUtility.TrTextContent("Rate");
            public static readonly GUILayoutOption playRateWidth = GUILayout.Width(46);
            public static readonly GUILayoutOption showToggleLabelsWidth = GUILayout.Width(192);
            public static readonly GUILayoutOption showToggleWidth = GUILayout.Width(18);
            public static readonly GUILayoutOption powerSliderWidth = GUILayout.Width(124);
            public static readonly GUILayoutOption sceneViewButtonWidth = GUILayout.Width(52);
            public static readonly GUILayoutOption playRateDropdownWidth = GUILayout.Width(40);

            public static readonly GUIContent allowInstancing = EditorGUIUtility.TrTextContent("Allow Instancing", "When enabled, the effect will try to be batched with other of the same type.");

            public static readonly GUIContent graphInBundle = EditorGUIUtility.TrTextContent("Exposed properties are hidden in the Inspector when Visual Effect Assets are stored in Asset Bundles.");
            public static readonly GUIContent play = new GUIContent("Play()");
            public static readonly GUIContent stop = new GUIContent("Stop()");

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
            public const float overrideWidth = 16;

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
                categoryHeader.normal.background = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + (EditorGUIUtility.isProSkin ? "/VFX/cat-background-dark.png" : "/VFX/cat-background-light.png"));
            }
        }
    }
}
