using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    using UnityObject = UnityEngine.Object;

    public static class CoreEditorUtils
    {
        class Styles
        {
            static readonly Color k_Normal_AllTheme = new Color32(0, 0, 0, 0);
            //static readonly Color k_Hover_Dark = new Color32(70, 70, 70, 255);
            //static readonly Color k_Hover = new Color32(193, 193, 193, 255);
            static readonly Color k_Active_Dark = new Color32(80, 80, 80, 255);
            static readonly Color k_Active = new Color32(216, 216, 216, 255);

            static readonly int s_MoreOptionsHash = "MoreOptions".GetHashCode();

            static public GUIContent moreOptionsLabel { get; private set; }
            static public GUIStyle moreOptionsStyle { get; private set; }
            static public GUIStyle moreOptionsLabelStyle { get; private set; }

            static Styles()
            {
                moreOptionsLabel = EditorGUIUtility.TrIconContent("MoreOptions", "More Options");

                moreOptionsStyle = new GUIStyle(GUI.skin.toggle);
                Texture2D normalColor = new Texture2D(1, 1);
                normalColor.SetPixel(1, 1, k_Normal_AllTheme);
                moreOptionsStyle.normal.background = normalColor;
                moreOptionsStyle.onActive.background = normalColor;
                moreOptionsStyle.onFocused.background = normalColor;
                moreOptionsStyle.onNormal.background = normalColor;
                moreOptionsStyle.onHover.background = normalColor;
                moreOptionsStyle.active.background = normalColor;
                moreOptionsStyle.focused.background = normalColor;
                moreOptionsStyle.hover.background = normalColor;
                
                moreOptionsLabelStyle = new GUIStyle(GUI.skin.label);
                moreOptionsLabelStyle.padding = new RectOffset(0, 0, 0, -1);
            }

            //Note:
            // - GUIStyle seams to be broken: all states have same state than normal light theme
            // - Hover with event will not be updated right when we enter the rect
            //-> Removing hover for now. Keep theme color for refactoring with UIElement later
            static public bool DrawMoreOptions(Rect rect, bool active)
            {
                int id = GUIUtility.GetControlID(s_MoreOptionsHash, FocusType.Passive, rect);
                var evt = Event.current;
                switch (evt.type)
                {
                    case EventType.Repaint:
                        Color background = k_Normal_AllTheme;
                        if (active)
                            background = EditorGUIUtility.isProSkin ? k_Active_Dark : k_Active;
                        EditorGUI.DrawRect(rect, background);
                        GUI.Label(rect, moreOptionsLabel, moreOptionsLabelStyle);
                        break;
                    case EventType.KeyDown:
                        bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);
                        if ((evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !anyModifiers && GUIUtility.keyboardControl == id)
                        {
                            evt.Use();
                            GUI.changed = true;
                            return !active;
                        }
                        break;
                    case EventType.MouseDown:
                        if (rect.Contains(evt.mousePosition))
                        {
                            GrabMouseControl(id);
                            evt.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (HasMouseControl(id))
                        {
                            ReleaseMouseControl();
                            evt.Use();
                            if (rect.Contains(evt.mousePosition))
                            {
                                GUI.changed = true;
                                return !active;
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        if (HasMouseControl(id))
                            evt.Use();
                        break;
                }
                
                return active;
            }

            static int s_GrabbedID = -1;
            static void GrabMouseControl(int id) => s_GrabbedID = id;
            static void ReleaseMouseControl() => s_GrabbedID = -1;
            static bool HasMouseControl(int id) => s_GrabbedID == id;
        }

        static GraphicsDeviceType[] m_BuildTargets;
        public static GraphicsDeviceType[] buildTargets => m_BuildTargets ?? (m_BuildTargets = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget));

        static CoreEditorUtils()
        {
            LoadSkinAndIconMethods();
        }
        
        // Serialization helpers
        /// <summary>
        /// To use with extreme caution. It not really get the property but try to find a field with similar name
        /// Hence inheritance override of property is not supported.
        /// Also variable rename will silently break the search.
        /// </summary>
        public static string FindProperty<T, TValue>(Expression<Func<T, TValue>> expr)
        {
            // Get the field path as a string
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                // For field, get the field name
                // For properties, get the name of the backing field
                var name = me.Member is FieldInfo
                    ? me.Member.Name
                    : "m_" + me.Member.Name.Substring(0, 1).ToUpper() + me.Member.Name.Substring(1);
                members.Add(name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        // UI Helpers
        public static void DrawFixMeBox(string text, Action action)
        {
            EditorGUILayout.HelpBox(text, MessageType.Warning);

            GUILayout.Space(-32);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Fix", GUILayout.Width(60)))
                    action();

                GUILayout.Space(8);
            }
            GUILayout.Space(11);
        }

        public static void DrawMultipleFields(string label, SerializedProperty[] ppts, GUIContent[] lbls)
            => DrawMultipleFields(EditorGUIUtility.TrTextContent(label), ppts, lbls);

        public static void DrawMultipleFields(GUIContent label, SerializedProperty[] ppts, GUIContent[] lbls)
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUIUtility.labelWidth = 40;
                    EditorGUI.indentLevel--;
                    for (var i = 0; i < ppts.Length; ++i)
                        EditorGUILayout.PropertyField(ppts[i], lbls[i]);
                    EditorGUI.indentLevel++;
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;
            
            if (isBoxed)
            {
                rect.xMin = EditorGUIUtility.singleLineHeight - 2;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static void DrawHeader(string title)
            => DrawHeader(EditorGUIUtility.TrTextContent(title));

        public static void DrawHeader(GUIContent title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOption"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOption = null)
            => DrawHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, hasMoreOptions, toggleMoreOption);

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // More options 1/2
            var moreOptionsRect = new Rect();
            if (hasMoreOptions != null)
            {
                moreOptionsRect = backgroundRect;
                moreOptionsRect.x += moreOptionsRect.width - 16 - 1;
                moreOptionsRect.height = 15;
                moreOptionsRect.width = 16;
            }

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 3;
            }

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // More options 2/2
            if (hasMoreOptions != null)
            {
                EditorGUI.BeginChangeCheck();
                Styles.DrawMoreOptions(moreOptionsRect, hasMoreOptions());
                if (EditorGUI.EndChangeCheck() && toggleMoreOptions != null)
                {
                    toggleMoreOptions();
                }
            }

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && !moreOptionsRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }
            
            return state;
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOption"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawSubHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
            => DrawSubHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, hasMoreOptions, toggleMoreOptions);

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawSubHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.x += 15 * EditorGUI.indentLevel; //GUI do not handle indent. Handle it here
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // More options
            var advancedRect = new Rect();
            if (hasMoreOptions != null)
            {
                advancedRect = backgroundRect;
                advancedRect.x += advancedRect.width - 16 - 1;
                advancedRect.height = 16;
                advancedRect.width = 16;

                bool moreOptions = hasMoreOptions();
                bool newMoreOptions = Styles.DrawMoreOptions(advancedRect, moreOptions);
                if (moreOptions ^ newMoreOptions)
                    toggleMoreOptions?.Invoke();
            }

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 3;
            }

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && !advancedRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }

        public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
            => DrawHeaderToggle(EditorGUIUtility.TrTextContent(title), group, activeField, contextAction, hasMoreOptions, toggleMoreOptions);

        public static bool DrawHeaderToggle(GUIContent title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f + 16 + 5;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            // More options 1/2
            var moreOptionsRect = new Rect();
            if (hasMoreOptions != null)
            {
                moreOptionsRect = backgroundRect;
                moreOptionsRect.x += moreOptionsRect.width - 16 - 1 - 16 - 5;
                moreOptionsRect.height = 15;
                moreOptionsRect.width = 16;
            }

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Foldout
            group.serializedObject.Update();
            group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
            group.serializedObject.ApplyModifiedProperties();

            // Active checkbox
            activeField.serializedObject.Update();
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            activeField.serializedObject.ApplyModifiedProperties();

            // More options 2/2
            if (hasMoreOptions != null)
            {
                bool moreOptions = hasMoreOptions();
                bool newMoreOptions = Styles.DrawMoreOptions(moreOptionsRect, moreOptions);
                if (moreOptions ^ newMoreOptions)
                    toggleMoreOptions?.Invoke();
            }

            // Context menu
            var menuIcon = CoreEditorStyles.paneOptionsIcon;
            var menuRect = new Rect(labelRect.xMax + 3f + 16 + 5 , labelRect.y + 1f, menuIcon.width, menuIcon.height);

            if (contextAction != null)
                GUI.DrawTexture(menuRect, menuIcon);

            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (contextAction != null && menuRect.Contains(e.mousePosition))
                {
                    contextAction(new Vector2(menuRect.x, menuRect.yMax));
                    e.Use();
                }
                else if (labelRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                        group.isExpanded = !group.isExpanded;
                    else if (contextAction != null)
                        contextAction(e.mousePosition);

                    e.Use();
                }
            }

            return group.isExpanded;
        }

        static readonly GUIContent[] k_DrawVector6_Label =
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z"),
        };
        const int k_DrawVector6Slider_LabelSize = 60;
        const int k_DrawVector6Slider_FieldSize = 80;

        public static void DrawVector6(GUIContent label, SerializedProperty positive, SerializedProperty negative, Vector3 min, Vector3 max, Color[] colors = null, SerializedProperty multiplicator = null)
        {
            if (colors != null && (colors.Length != 6))
                    throw new System.ArgumentException("Colors must be a 6 element array. [+X, +Y, +X, -X, -Y, -Z]");

            GUILayout.BeginVertical();
            Rect rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            if (label != GUIContent.none)
            {
                var labelRect = rect;
                labelRect.x -= 15f * EditorGUI.indentLevel;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.x += EditorGUIUtility.labelWidth - 1f - 15f * EditorGUI.indentLevel;
                rect.width -= EditorGUIUtility.labelWidth - 1f - 15f * EditorGUI.indentLevel;
            }
            DrawVector3(rect, k_DrawVector6_Label, positive, min, max, false, colors == null ? null : new Color[] { colors[0], colors[1], colors[2] }, multiplicator);

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            rect.x += EditorGUIUtility.labelWidth - 1f - 15f * EditorGUI.indentLevel;
            rect.width -= EditorGUIUtility.labelWidth - 1f - 15f * EditorGUI.indentLevel;
            DrawVector3(rect, k_DrawVector6_Label, negative, min, max, true, colors == null ? null : new Color[] { colors[3], colors[4], colors[5] }, multiplicator);

            GUILayout.EndVertical();
        }

        static void DrawVector3(Rect rect, GUIContent[] labels, SerializedProperty value, Vector3 min, Vector3 max, bool addMinusPrefix, Color[] colors, SerializedProperty multiplicator = null)
        {
            float[] multifloat = multiplicator == null
                ? new float[] { value.vector3Value.x, value.vector3Value.y, value.vector3Value.z }
                : new float[] { value.vector3Value.x * multiplicator.vector3Value.x, value.vector3Value.y * multiplicator.vector3Value.y, value.vector3Value.z * multiplicator.vector3Value.z };

            float fieldWidth = rect.width / 3f;
            EditorGUI.showMixedValue = value.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, labels, multifloat);
            if(EditorGUI.EndChangeCheck())
            {
                value.vector3Value = multiplicator == null
                    ? new Vector3(
                        Mathf.Clamp(multifloat[0], min.x, max.x),
                        Mathf.Clamp(multifloat[1], min.y, max.y),
                        Mathf.Clamp(multifloat[2], min.z, max.z)
                        )
                    : new Vector3(
                        Mathf.Clamp((multiplicator.vector3Value.x < -0.00001 || 0.00001 < multiplicator.vector3Value.x) ? multifloat[0] / multiplicator.vector3Value.x : 0f, min.x, max.x),
                        Mathf.Clamp((multiplicator.vector3Value.y < -0.00001 || 0.00001 < multiplicator.vector3Value.y) ? multifloat[1] / multiplicator.vector3Value.y : 0f, min.y, max.y),
                        Mathf.Clamp((multiplicator.vector3Value.z < -0.00001 || 0.00001 < multiplicator.vector3Value.z) ? multifloat[2] / multiplicator.vector3Value.z : 0f, min.z, max.z)
                        );
            }
            EditorGUI.showMixedValue = false;

            //Suffix is a hack as sublabel only work with 1 character
            if (addMinusPrefix)
            {
                Rect suffixRect = new Rect(rect.x - 4 - 15 * EditorGUI.indentLevel, rect.y, 100, rect.height);
                for(int i = 0; i < 3; ++i)
                {
                    EditorGUI.LabelField(suffixRect, "-");
                    suffixRect.x += fieldWidth + .66f;
                }
            }

            //Color is a hack as nothing is done to handle this at the moment
            if(colors != null)
            {
                if (colors.Length != 3)
                    throw new System.ArgumentException("colors must have 3 elements.");

                Rect suffixRect = new Rect(rect.x + 7 - 15 * EditorGUI.indentLevel, rect.y, 100, rect.height);
                GUIStyle colorMark = new GUIStyle(EditorStyles.label);
                colorMark.normal.textColor = colors[0];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth  - .5f;
                colorMark.normal.textColor = colors[1];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth;
                colorMark.normal.textColor = colors[2];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
            }
        }

        public static void DrawPopup(GUIContent label, SerializedProperty property, string[] options)
        {
            var mode = property.intValue;
            EditorGUI.BeginChangeCheck();

            if (mode >= options.Length)
                Debug.LogError(string.Format("Invalid option while trying to set {0}", label.text));

            mode = EditorGUILayout.Popup(label, mode, options);
            if (EditorGUI.EndChangeCheck())
                property.intValue = mode;
        }

        /// <summary>
        /// Draw an EnumPopup handling multiEdition
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <param name="label"></param>
        public static void DrawEnumPopup(SerializedProperty property, System.Type type, GUIContent label = null)
        {
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var name = System.Enum.GetName(type, property.intValue);
            var index = System.Array.FindIndex(System.Enum.GetNames(type), n => n == name);
            var input = (System.Enum)System.Enum.GetValues(type).GetValue(index);
            var rawResult = EditorGUILayout.EnumPopup(label ?? EditorGUIUtility.TrTextContent(ObjectNames.NicifyVariableName(property.name)), input);
            var result = ((System.IConvertible)rawResult).ToInt32(System.Globalization.CultureInfo.CurrentCulture);
            if (EditorGUI.EndChangeCheck())
                property.intValue = result;
            EditorGUI.showMixedValue = false;
        }

        public static void RemoveMaterialKeywords(Material material)
            => material.shaderKeywords = null;

        public static T[] GetAdditionalData<T>(UnityEngine.Object[] targets, Action<T> initDefault = null)
            where T : Component
        {
            // Handles multi-selection
            var data = targets.Cast<Component>()
                .Select(t => t.GetComponent<T>())
                .ToArray();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == null)
                {
                    data[i] = Undo.AddComponent<T>(((Component)targets[i]).gameObject);
                    if (initDefault != null)
                    {
                        initDefault(data[i]);
                    }
                }
            }

            return data;
        }

        static public GameObject CreateGameObject(GameObject parent, string name, params Type[] types)
            => ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);

        static public string GetCurrentProjectVersion()
        {
            string[] readText = File.ReadAllLines("ProjectSettings/ProjectVersion.txt");
            // format is m_EditorVersion: 2018.2.0b7
            string[] versionText = readText[0].Split(' ');
            return versionText[1];
        }

        static public void CheckOutFile(bool VCSEnabled, UnityObject mat)
        {
            if (VCSEnabled)
            {
                UnityEditor.VersionControl.Task task = UnityEditor.VersionControl.Provider.Checkout(mat, UnityEditor.VersionControl.CheckoutMode.Both);

                if (!task.success)
                {
                    Debug.Log(task.text + " " + task.resultCode);
                }
            }
        }


        #region IconAndSkin

        internal enum Skin
        {
            Auto,
            Personnal,
            Professional,
        }
        
        static Func<int> GetInternalSkinIndex;
        static Func<float> GetGUIStatePixelsPerPoint;
        static Func<Texture2D, float> GetTexturePixelPerPoint;
        static Action<Texture2D, float> SetTexturePixelPerPoint;

        static void LoadSkinAndIconMethods()
        {
            var internalSkinIndexInfo = typeof(EditorGUIUtility).GetProperty("skinIndex", BindingFlags.NonPublic | BindingFlags.Static);
            var internalSkinIndexLambda = Expression.Lambda<Func<int>>(Expression.Property(null, internalSkinIndexInfo));
            GetInternalSkinIndex = internalSkinIndexLambda.Compile();

            var guiStatePixelsPerPointInfo = typeof(GUIUtility).GetProperty("pixelsPerPoint", BindingFlags.NonPublic | BindingFlags.Static);
            var guiStatePixelsPerPointLambda = Expression.Lambda<Func<float>>(Expression.Property(null, guiStatePixelsPerPointInfo));
            GetGUIStatePixelsPerPoint = guiStatePixelsPerPointLambda.Compile();

            var pixelPerPointParam = Expression.Parameter(typeof(float), "pixelPerPoint");
            var texture2DProperty = Expression.Parameter(typeof(Texture2D), "texture2D");
            var texture2DPixelsPerPointInfo = typeof(Texture2D).GetProperty("pixelsPerPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            var texture2DPixelsPerPointProperty = Expression.Property(texture2DProperty, texture2DPixelsPerPointInfo);
            var texture2DGetPixelsPerPointLambda = Expression.Lambda<Func<Texture2D, float>>(texture2DPixelsPerPointProperty, texture2DProperty);
            GetTexturePixelPerPoint = texture2DGetPixelsPerPointLambda.Compile();
            var texture2DSetPixelsPerPointLambda = Expression.Lambda<Action<Texture2D, float>>(Expression.Assign(texture2DPixelsPerPointProperty, pixelPerPointParam), texture2DProperty, pixelPerPointParam);
            SetTexturePixelPerPoint = texture2DSetPixelsPerPointLambda.Compile();
        }

        /// <summary>Get the skin currently in use</summary>
        static Skin currentSkin
            => GetInternalSkinIndex() == 0 ? Skin.Personnal : Skin.Professional;


        // /!\ UIElement do not support well pixel per point at the moment. For this, use the hack forceLowRes
        /// <summary>
        /// Load an icon regarding skin and editor resolution.
        /// Icon should be stored as legacy icon resources:
        /// - "d_" prefix for Professional theme
        /// - "@2x" suffix for high resolution
        /// </summary>
        /// <param name="path">Path to seek the icon from Assets/ folder</param>
        /// <param name="name">Icon name without suffix, prefix or extention</param>
        /// <param name="extention">[Optional] Extention of file (png per default)</param>
        /// <param name="skin">[Optional] Load icon for this skin (Auto per default take current skin)</param>
        internal static Texture2D LoadIcon(string path, string name, string extention = ".png", Skin skin = Skin.Auto, bool forceLowRes = false)
        {
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name))
                return null;

            string prefix = "";

            if (skin == Skin.Auto)
                skin = currentSkin;

            if (skin == Skin.Professional)
                prefix = "d_";
            
            Texture2D icon = null;
            float pixelsPerPoint = GetGUIStatePixelsPerPoint();
            if (pixelsPerPoint > 1.0f && !forceLowRes)
            {
                icon = EditorGUIUtility.Load(String.Format("{0}/{1}{2}@2x{3}", path, prefix, name, extention)) as Texture2D;
                if (icon == null && !string.IsNullOrEmpty(prefix))
                    icon = EditorGUIUtility.Load(String.Format("{0}/{1}@2x{2}", path, name, extention)) as Texture2D;
                if (icon != null)
                    SetTexturePixelPerPoint(icon, 2.0f);
            }

            if (icon == null)
                icon = EditorGUIUtility.Load(String.Format("{0}/{1}{2}{3}", path, prefix, name, extention)) as Texture2D;

            if (icon == null && !string.IsNullOrEmpty(prefix))
                icon = EditorGUIUtility.Load(String.Format("{0}/{1}{2}", path, name, extention)) as Texture2D;

            if (icon != null &&
                !Mathf.Approximately(GetTexturePixelPerPoint(icon), pixelsPerPoint) && //scaling are different
                !Mathf.Approximately(pixelsPerPoint % 1, 0)) //screen scaling is non-integer
            {
                icon.filterMode = FilterMode.Bilinear;
            }

            return icon;
        }

        #endregion
    }
}
