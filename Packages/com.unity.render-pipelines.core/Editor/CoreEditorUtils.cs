using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;

namespace UnityEditor.Rendering
{
    using UnityObject = UnityEngine.Object;

    /// <summary>Utility class for Editor</summary>
    public static class CoreEditorUtils
    {
        static GraphicsDeviceType[] m_BuildTargets;

        /// <summary>Build targets</summary>
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
        /// <typeparam name="T">Entry type of expr</typeparam>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="expr">Expression returning the value seeked</param>
        /// <returns>serialization path of the seeked property</returns>
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
                    : "m_" + me.Member.Name.Substring(0, 1).ToUpperInvariant() + me.Member.Name.Substring(1);
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

        /// <summary>Creates a 1x1 <see cref="Texture2D"/> with a plain <see cref="Color"/></summary>
        /// <param name="color">The color to fill the texture</param>
        /// <param name="textureName">The name of the texture</param>
        /// <returns>a <see cref="Texture2D"/></returns>
        public static Texture2D CreateColoredTexture2D(Color color, string textureName)
        {
            Texture2D tex2 = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = textureName
            };
            tex2.SetPixel(1, 1, color);
            tex2.Apply();
            return tex2;
        }

        const float k_IndentMargin = 15.0f;
        const float k_HighlightDuration = 2.0f;

        static float s_HighlightStart = -1.0f;
        static Texture2D s_HighlightBackground;
        static object s_View;

        static readonly FieldInfo k_ViewInfo = typeof(Highlighter).GetField("s_View", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo k_HighlightStyleInfo = typeof(Highlighter).GetField("s_HighlightStyle", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo k_WindowBackendInfo = Type.GetType("UnityEditor.GUIView,UnityEditor").GetField("m_WindowBackend", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly EventInfo k_GUIHandlerInfo = Type.GetType("UnityEditor.UIElements.DefaultEditorWindowBackend,UnityEditor").GetEvent("overlayGUIHandler", (BindingFlags)(-1));
        static readonly MethodInfo k_Repaint = Type.GetType("UnityEditor.GUIView,UnityEditor").GetMethod("Repaint", (BindingFlags)(-1));

        static void HighlightTimeout()
        {
            if (!Highlighter.active)
            {
                if (s_HighlightBackground != null)
                    (k_HighlightStyleInfo.GetValue(null) as GUIStyle).normal.background = s_HighlightBackground;
                s_HighlightBackground = null;

                EditorApplication.update -= HighlightTimeout;
                s_HighlightStart = -1.0f;
                return;
            }

            // Item is in view for the first time, register highlight drawer delegate
            if (Highlighter.activeVisible && s_HighlightStart <= 0.0f)
            {
                s_HighlightStart = Time.realtimeSinceStartup;

                s_View = k_ViewInfo.GetValue(null);
                if (s_View != null)
                {
                    var windowBackend = k_WindowBackendInfo.GetValue(s_View);
                    k_GUIHandlerInfo.AddEventHandler(windowBackend, (Action)ControlHighlightGUI);
                    var style = k_HighlightStyleInfo.GetValue(null) as GUIStyle;
                    s_HighlightBackground = style.normal.background;
                    style.normal.background = null;
                }
                else
                {
                    Highlighter.Stop();
                }
            }

            if (s_HighlightStart > 0.0f)
            {
                if (Time.realtimeSinceStartup - s_HighlightStart > k_HighlightDuration)
                {
                    Highlighter.Stop();

                    var windowBackend = k_WindowBackendInfo.GetValue(s_View);
                    k_GUIHandlerInfo.RemoveEventHandler(windowBackend, (Action)ControlHighlightGUI);
                }
            }
        }

        static void ControlHighlightGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var color = CoreEditorStyles.backgroundHighlightColor;
            color.a = Mathf.Min(1.0f - (Time.realtimeSinceStartup - s_HighlightStart) / k_HighlightDuration, 0.8f);

            EditorGUI.DrawRect(GUIUtility.ScreenToGUIRect(Highlighter.activeRect), color);
        }

        /// <summary>Highlights an element in the editor for a short period of time.</summary>
        /// <param name="windowTitle">The title of the window the element is inside.</param>
        /// <param name="text">The text to identify the element with.</param>
        /// <param name="mode">Optional mode to specify how to search for the element.</param>
        public static void Highlight(string windowTitle, string text, HighlightSearchMode mode = HighlightSearchMode.Auto)
        {
            if (s_HighlightStart >= 0.0f)
                return;

            s_HighlightStart = 0.0f;
            Highlighter.Highlight(windowTitle, text, mode);
            EditorApplication.update += HighlightTimeout;
        }

        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message text.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(string message, Action action)
        {
            DrawFixMeBox(message, MessageType.Warning, "Fix", action);
        }

        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(string message, MessageType messageType, Action action)
        {
            DrawFixMeBox(EditorGUIUtility.TrTextContentWithIcon(message, CoreEditorStyles.GetMessageTypeIcon(messageType)), "Fix", action);
        }

        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(string message, MessageType messageType, string buttonLabel, Action action)
        {
            DrawFixMeBox(EditorGUIUtility.TrTextContentWithIcon(message, CoreEditorStyles.GetMessageTypeIcon(messageType)), buttonLabel, action);
        }

        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message with icon if needed.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(GUIContent message, Action action)
        {
            DrawFixMeBox(message, "Fix", action);
        }

        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message with icon if needed.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(GUIContent message, string buttonLabel, Action action)
        {
            EditorGUILayout.BeginHorizontal();

            float indent = EditorGUI.indentLevel * k_IndentMargin - EditorStyles.helpBox.margin.left;
            GUILayoutUtility.GetRect(indent, EditorGUIUtility.singleLineHeight, EditorStyles.helpBox, GUILayout.ExpandWidth(false));

            Rect leftRect = GUILayoutUtility.GetRect(new GUIContent(buttonLabel), EditorStyles.miniButton, GUILayout.MinWidth(60), GUILayout.ExpandWidth(false));
            Rect rect = GUILayoutUtility.GetRect(message, EditorStyles.helpBox);
            Rect boxRect = new Rect(leftRect.x, rect.y, rect.xMax - leftRect.xMin, rect.height);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (Event.current.type == EventType.Repaint)
                EditorStyles.helpBox.Draw(boxRect, false, false, false, false);

            Rect labelRect = new Rect(boxRect.x + 4, boxRect.y + 3, rect.width - 8, rect.height);
            EditorGUI.LabelField(labelRect, message, CoreEditorStyles.helpBox);

            var buttonRect = leftRect;
            buttonRect.x += rect.width - 2;
            buttonRect.y = rect.yMin + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            bool clicked = GUI.Button(buttonRect, buttonLabel);

            EditorGUI.indentLevel = oldIndent;
            EditorGUILayout.EndHorizontal();

            if (clicked)
                action();
        }

        /// <summary>
        /// Draw a multiple field property
        /// </summary>
        /// <param name="label">Label of the whole</param>
        /// <param name="ppts">Properties</param>
        /// <param name="labels">Sub-labels</param>
        public static void DrawMultipleFields(string label, SerializedProperty[] ppts, GUIContent[] labels)
            => DrawMultipleFields(EditorGUIUtility.TrTextContent(label), ppts, labels);

        private static float GetLongestLabelWidth(GUIContent[] labels)
        {
            float labelWidth = 0.0f;
            for (var i = 0; i < labels.Length; ++i)
                labelWidth = Mathf.Max(EditorStyles.label.CalcSize(labels[i]).x, labelWidth);
            return labelWidth;
        }

        /// <summary>
        /// Draws an <see cref="EditorGUI.EnumPopup"/> for the given property
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="rect">The rect where the drop down will be drawn</param>
        /// <param name="label">The label for the drop down</param>
        /// <param name="serializedProperty">The <see cref="SerializedProperty"/> to modify</param>
        public static void DrawEnumPopup<TEnum>(Rect rect, GUIContent label, SerializedProperty serializedProperty)
            where TEnum : Enum
        {
            using (new EditorGUI.MixedValueScope(serializedProperty.hasMultipleDifferentValues))
            {
                EditorGUI.BeginChangeCheck();
                var newValue = (TEnum)EditorGUI.EnumPopup(rect, label, serializedProperty.GetEnumValue<TEnum>());
                if (EditorGUI.EndChangeCheck())
                    serializedProperty.SetEnumValue(newValue);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draw a multiple field property
        /// </summary>
        /// <param name="label">Label of the whole</param>
        /// <param name="ppts">Properties</param>
        /// <param name="labels">Sub-labels</param>
        public static void DrawMultipleFields(GUIContent label, SerializedProperty[] ppts, GUIContent[] labels)
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUIUtility.labelWidth = GetLongestLabelWidth(labels) + CoreEditorConstants.standardHorizontalSpacing;
                    int oldIndentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    for (var i = 0; i < ppts.Length; ++i)
                        EditorGUILayout.PropertyField(ppts[i], labels[i]);
                    EditorGUI.indentLevel = oldIndentLevel;
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }
        /// <summary>
        /// Draw a multiple field property
        /// </summary>
        /// <typeparam name="T">A valid <see cref="struct"/></typeparam>
        /// <param name="label">Label of the whole</param>
        /// <param name="labels">The labels mapping the values</param>
        /// <param name="values">The values to be displayed</param>
        public static void DrawMultipleFields<T>(GUIContent label, GUIContent[] labels, T[] values)
            where T : struct
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUIUtility.labelWidth = GetLongestLabelWidth(labels) + CoreEditorConstants.standardHorizontalSpacing;
                    int oldIndentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    for (var i = 0; i < values.Length; ++i)
                    {
                        // Draw the right field depending on its type.
                        if (typeof(T) == typeof(int))
                            values[i] = (T)(object)EditorGUILayout.DelayedIntField(labels[i], (int)(object)values[i]);
                        else if (typeof(T) == typeof(bool))
                            values[i] = (T)(object)EditorGUILayout.Toggle(labels[i], (bool)(object)values[i]);
                        else if (typeof(T) == typeof(float))
                            values[i] = (T)(object)EditorGUILayout.FloatField(labels[i], (float)(object)values[i]);
                        else if (typeof(T).IsEnum)
                            values[i] = (T)(object)EditorGUILayout.EnumPopup(labels[i], (Enum)(object)values[i]);
                        else
                            throw new ArgumentOutOfRangeException($"<{typeof(T)}> is not a supported type for multi field");
                    }
                    EditorGUI.indentLevel = oldIndentLevel;
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            if (!isBoxed)
            {
                rect = ToFullWidth(rect);
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        /// <summary>Draw a splitter separator which is used after drawing a fouldout header.</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        internal static void DrawFoldoutEndSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            if (!isBoxed)
            {
                rect = ToFullWidth(rect);
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.73f, 0.73f, 0.73f, 1.333f)
                : new Color(0.19f, 0.19f, 0.19f, 1.333f));
        }

        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
        public static void DrawHeader(string title)
            => DrawHeader(EditorGUIUtility.TrTextContent(title));

        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
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
            backgroundRect = ToFullWidth(backgroundRect);

            DrawBackground(backgroundRect, false);

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the header contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOption"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <param name="isTitleHeader"> [optional] is this a title header, this setting controls the color used for the foldout </param>
        /// <param name="documentationURL">[optional] The URL that the Unity Editor opens when the user presses the help button on the header.</param>
        /// <param name="contextAction">[optional] The callback that the Unity Editor executes when the user presses the burger menu on the header.</param>
        /// <param name="customMenuContextAction">[optional] Delegate which adds items to a generic menu when the user presses the burger menu on the header.</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOption = null, bool isTitleHeader = false, string documentationURL = "", Action<Vector2> contextAction = null, Action<GenericMenu> customMenuContextAction = null)
            => DrawHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, hasMoreOptions, toggleMoreOption, isTitleHeader, documentationURL, contextAction, customMenuContextAction);



        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <param name="isTitleHeader"> [optional] is this a title header, this setting controls the color used for the foldout </param>
        /// <param name="documentationURL">[optional] The URL that the Unity Editor opens when the user presses the help button on the header.</param>
        /// <param name="contextAction">[optional] The callback that the Unity Editor executes when the user presses the burger menu on the header.</param>
        /// <param name="customMenuContextAction">[optional] Delegate which adds items to a generic menu when the user presses the burger menu on the header.</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null, bool isTitleHeader = false, string documentationURL = "", Action<Vector2> contextAction = null, Action<GenericMenu> customMenuContextAction = null)
        {
            const float height = 17f;
            const float iconRectSize = 13f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);
            if (backgroundRect.xMin != 0) // Fix for material editor
                backgroundRect.xMin = 1 + 15f * (EditorGUI.indentLevel + 1);
            float xMin = backgroundRect.xMin;

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = iconRectSize;
            foldoutRect.height = iconRectSize;
            foldoutRect.x = labelRect.xMin + k_IndentMargin * (EditorGUI.indentLevel - 1); //fix for presset

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
            }
            else
            {
                // Background rect should be full-width
                backgroundRect = ToFullWidth(backgroundRect);

            }

            DrawBackground(backgroundRect, isTitleHeader);

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            // Context menu
            var menuRect = new Rect(labelRect.xMax + 3f, labelRect.y + 1f, 16, 16);

            contextAction = CreateMenuContextAction(contextAction, hasMoreOptions, toggleMoreOptions, customMenuContextAction);

            CreateContextMenu(menuRect, contextAction);

            // Documentation button
            ShowHelpButton(menuRect, documentationURL, title);

            state = HandleEvent(state, backgroundRect, contextAction);

            return state;
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <returns>return the state of the sub foldout header</returns>
        [Obsolete("'More Options' versions of DrawSubHeaderFoldout are obsolete. Please use DrawSubHeaderFoldout without 'More Options'")]
        public static bool DrawSubHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
            => DrawSubHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed);

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <returns>return the state of the foldout header</returns>
        [Obsolete("'More Options' versions of DrawSubHeaderFoldout are obsolete. Please use DrawSubHeaderFoldout without 'More Options'")]
        public static bool DrawSubHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
            => DrawSubHeaderFoldout(title, state, isBoxed);

        /// <summary>
        /// Draw a foldout sub header
        /// </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <returns>return the state of the sub foldout header</returns>
        public static bool DrawSubHeaderFoldout(string title, bool state, bool isBoxed = false)
            => DrawSubHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed);

        /// <summary>
        /// Draw a foldout sub header
        /// </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <returns>return the state of the sub foldout header</returns>
        public static bool DrawSubHeaderFoldout(GUIContent title, bool state, bool isBoxed = false)
        {
            const float height = 17f;
            const float iconRectSize = 13f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.x += k_IndentMargin * EditorGUI.indentLevel; //GUI do not handle indent. Handle it here
            foldoutRect.width = iconRectSize;
            foldoutRect.height = iconRectSize;


            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
            }
            else
            {
                backgroundRect = ToFullWidth(backgroundRect);
            }

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            state = HandleEvent(state, backgroundRect, null);

            return state;
        }


        /// <summary>Draw a header toggle like in Volumes</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="group"> The group of the header </param>
        /// <param name="activeField">The active field</param>
        /// <param name="contextAction">The context action</param>
        /// <param name="hasMoreOptions">Delegate saying if we have MoreOptions</param>
        /// <param name="toggleMoreOptions">Callback called when the MoreOptions is toggled</param>
        /// <param name="documentationURL">Documentation URL</param>
        /// <param name="customMenuContextAction">Delegate which adds items to a generic menu.</param>
        /// <param name="isBoxed">States if the header toggle should be boxed.</param>
        /// <param name="isTitleHeader"> [optional] is this a title header, this setting controls the color used for the foldout </param>
        /// <param name="shouldUpdate">States if the group and active field should update before usage and apply changes to them.</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null, string documentationURL = null, Action<GenericMenu> customMenuContextAction = null, bool isBoxed = false, bool isTitleHeader = false, bool shouldUpdate = true)
            => DrawHeaderToggle(EditorGUIUtility.TrTextContent(title), group, activeField, contextAction, hasMoreOptions, toggleMoreOptions, documentationURL, customMenuContextAction, isBoxed, isTitleHeader, shouldUpdate);

        private static void GetHeaderToggleRects(bool isBoxed, out Rect labelRect, out Rect foldoutRect, out Rect toggleRect, out Rect backgroundRect)
        {
            backgroundRect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(1f, 17f));

            labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f + 16 + 5;

            foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            if (!isBoxed)
            {
                // Background rect should be full-width
                backgroundRect = ToFullWidth(backgroundRect);
            }
        }

        /// <summary>Draw a header toggle like in Volumes</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="group"> The group of the header </param>
        /// <param name="activeField">The active field</param>
        /// <param name="contextAction">The context action</param>
        /// <param name="hasMoreOptions">Delegate saying if we have MoreOptions</param>
        /// <param name="toggleMoreOptions">Callback called when the MoreOptions is toggled</param>
        /// <param name="documentationURL">Documentation URL</param>
        /// <param name="customMenuContextAction">Delegate which adds items to a generic menu.</param>
        /// <param name="isBoxed">States if the header toggle should be boxed.</param>
        /// <param name="isTitleHeader"> [optional] is this a title header, this setting controls the color used for the foldout </param>
        /// <param name="shouldUpdate">States if the group and active field should update before usage and apply changes to them.</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderToggle(GUIContent title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null, string documentationURL = null, Action<GenericMenu> customMenuContextAction = null, bool isBoxed = false, bool isTitleHeader = false, bool shouldUpdate = true)
        {
            GetHeaderToggleRects(isBoxed, out Rect labelRect, out Rect foldoutRect, out Rect toggleRect, out Rect backgroundRect);
            DrawBackground(backgroundRect, isTitleHeader);

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            if (shouldUpdate)
            {
                // Foldout
                group.serializedObject.Update();
                group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
                group.serializedObject.ApplyModifiedProperties();

                // Active checkbox
                activeField.serializedObject.Update();
                activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
                activeField.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
                activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            }

            contextAction = ContextMenu(title, contextAction, hasMoreOptions, toggleMoreOptions, documentationURL, labelRect);
            group.isExpanded = HandleEvents(contextAction, backgroundRect, group.isExpanded);

            return group.isExpanded;
        }

        private static void DrawBackground(Rect backgroundRect)
        {
            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));
        }

        /// <summary>Draw a header toggle like in Volumes</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="foldoutExpanded">If the foldout is expanded</param>
        /// <param name="toogleProperty">The property to bind the toggle</param>
        /// <param name="contextAction">The context action</param>
        /// <param name="hasMoreOptions">Delegate saying if we have MoreOptions</param>
        /// <param name="toggleMoreOptions">Callback called when the MoreOptions is toggled</param>
        /// <param name="documentationURL">Documentation URL</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderToggleFoldout(GUIContent title, bool foldoutExpanded, SerializedProperty toogleProperty, Action<Vector2> contextAction, Func<bool> hasMoreOptions, Action toggleMoreOptions, string documentationURL)
        {
            GetHeaderToggleRects(false, out Rect labelRect, out Rect foldoutRect, out Rect toggleRect, out Rect backgroundRect);

            DrawBackground(backgroundRect);

            // Title
            using (new EditorGUI.DisabledScope(!toogleProperty.boolValue))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Foldout
            bool expanded = GUI.Toggle(foldoutRect, foldoutExpanded, GUIContent.none, EditorStyles.foldout);

            // Active checkbox
            toogleProperty.serializedObject.Update();
            toogleProperty.boolValue = GUI.Toggle(toggleRect, toogleProperty.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            toogleProperty.serializedObject.ApplyModifiedProperties();

            contextAction = ContextMenu(title, contextAction, hasMoreOptions, toggleMoreOptions, documentationURL, labelRect);
            expanded = HandleEvents(contextAction, backgroundRect, expanded);

            return expanded;
        }

        private static bool HandleEvents(Action<Vector2> contextAction, Rect backgroundRect, bool expanded)
        {
            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    // Left click: Expand/Collapse
                    if (e.button == 0)
                        expanded = !expanded;
                    // Right click: Context menu
                    else if (contextAction != null)
                        contextAction(e.mousePosition);

                    e.Use();
                }
            }

            return expanded;
        }

        private static Action<Vector2> ContextMenu(GUIContent title, Action<Vector2> contextAction, Func<bool> hasMoreOptions, Action toggleMoreOptions, string documentationURL, Rect labelRect)
        {
            const float menuRectSize = 16f;

            // Context menu
            var contextMenuRect = new Rect(labelRect.xMax + 3f + 16 + 5, labelRect.y + 1f, menuRectSize, menuRectSize);

            contextAction = CreateMenuContextAction(contextAction, hasMoreOptions, toggleMoreOptions, null);

            CreateContextMenu(contextMenuRect, contextAction);

            // Documentation button
            ShowHelpButton(contextMenuRect, documentationURL, title);
            return contextAction;
        }

        /// <summary>Draw a header section like in Global Settings</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="documentationURL">Documentation URL</param>
        /// <param name="contextAction">The context action</param>
        /// <param name="hasMoreOptions">Delegate saying if we have MoreOptions</param>
        /// <param name="toggleMoreOptions">Callback called when the MoreOptions is toggled</param>
        public static void DrawSectionHeader(GUIContent title, string documentationURL = null, Action<Vector2> contextAction = null, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null)
        {
            const float height = 17f;
            const float menuRectSize = 16f;
            var backgroundRect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(1f, height));

            var contextMenuRect = new Rect(backgroundRect.xMax - (menuRectSize + 5), backgroundRect.y + menuRectSize + 8f, menuRectSize, menuRectSize);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, CoreEditorStyles.sectionHeaderStyle);

                CreateContextMenu(contextMenuRect,  contextAction);
                ShowHelpButton(contextMenuRect, documentationURL, title);
            }

            HandleEvent(false, contextMenuRect, contextAction);
        }

        static void DrawBackground(Rect backgroundRect, bool isTitleHeader)
        {
            // Background
            float backgroundTint = isTitleHeader ? (EditorGUIUtility.isProSkin ? 0.24f : 0.78f) : (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, isTitleHeader ? 1f : 0.2f));
        }

        static Rect ToFullWidth(Rect rect)
        {
            rect.xMin = 0f;
            rect.width += 4f;
            return rect;
        }

        static Action<Vector2> CreateMenuContextAction(Action<Vector2> contextAction, Func<bool> hasMoreOptions, Action toggleMoreOptions, Action<GenericMenu> customMenuContextAction)
        {
            if (contextAction == null && (hasMoreOptions != null || customMenuContextAction != null))
            {
                // If no contextual menu add one for the additional properties.
                contextAction = pos =>
                {
                    var menu = new GenericMenu();
                    if (customMenuContextAction != null)
                        customMenuContextAction(menu);
                    if (hasMoreOptions != null)
                        AddAdditionalPropertiesContext(menu, hasMoreOptions, toggleMoreOptions);
                    menu.DropDown(new Rect(pos, Vector2.zero));
                };
            }
            return contextAction;
        }

        static void CreateContextMenu(Rect contextMenuRect, Action<Vector2> contextAction)
        {
            // Context menu
            if (contextAction != null)
            {
                if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                    contextAction(new Vector2(contextMenuRect.x, contextMenuRect.yMax));
            }
        }

        static bool HandleEvent(bool state, Rect activationRect, Action<Vector2> contextAction)
        {
            var e = Event.current;

            if (e.type == EventType.MouseDown && activationRect.Contains(e.mousePosition))
            {
                // Left click: Expand/Collapse
                if (e.button == 0)
                    state = !state;
                // Right click: Context menu
                else if (contextAction != null)
                    contextAction(e.mousePosition);
                e.Use();
            }
            return state;
        }

        static void ShowHelpButton(Rect contextMenuRect, string documentationURL, GUIContent title)
        {
            if (string.IsNullOrEmpty(documentationURL))
                return;

            var documentationRect = contextMenuRect;
            documentationRect.x -= 16 + 2;

            var documentationIcon = new GUIContent(CoreEditorStyles.iconHelp, $"Open Reference for {title.text}.");

            if (GUI.Button(documentationRect, documentationIcon, CoreEditorStyles.iconHelpStyle))
                Help.BrowseURL(documentationURL);
        }

        static void AddAdditionalPropertiesContext(GenericMenu menu, Func<bool> hasMoreOptions, Action toggleMoreOptions)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Additional Properties"), hasMoreOptions.Invoke(), () => toggleMoreOptions.Invoke());
            menu.AddItem(EditorGUIUtility.TrTextContent("Show All Additional Properties..."), false, () => CoreRenderPipelinePreferences.Open());
        }

        static readonly GUIContent[][] k_DrawVector6_Label =
        {
            new[]
            {
                new GUIContent(" X"),
                new GUIContent(" Y"),
                new GUIContent(" Z"),
            },
            new[]
            {
                new GUIContent("-X"),
                new GUIContent("-Y"),
                new GUIContent("-Z"),
            },
        };
        const int k_DrawVector6Slider_LabelSize = 60;
        const int k_DrawVector6Slider_FieldSize = 80;

        /// <summary>
        /// Draw a Vector6 field
        /// </summary>
        /// <param name="label">The label</param>
        /// <param name="positive">The data for +X, +Y and +Z</param>
        /// <param name="negative">The data for -X, -Y and -Z</param>
        /// <param name="min">Min clamping value along axis</param>
        /// <param name="max">Max clamping value along axis</param>
        /// <param name="colors">[Optional] Color marks to use</param>
        /// <param name="multiplicator">[Optional] multiplicator on the datas</param>
        /// <param name="allowIntersection">[Optional] Allow the face positive values to be smaller than negative ones and vice versa</param>
        public static void DrawVector6(GUIContent label, SerializedProperty positive, SerializedProperty negative, Vector3 min, Vector3 max, Color[] colors = null, SerializedProperty multiplicator = null, bool allowIntersection = true)
        {
            if (colors != null && (colors.Length != 6))
                throw new System.ArgumentException("Colors must be a 6 element array. [+X, +Y, +X, -X, -Y, -Z]");

            GUILayout.BeginVertical();

            const int interline = 2;
            const int fixAlignSubVector3Labels = -1;
            Rect wholeArea = EditorGUILayout.GetControlRect(true, 2 * EditorGUIUtility.singleLineHeight + interline);
            Rect firstLineRect = wholeArea;
            firstLineRect.height = EditorGUIUtility.singleLineHeight;
            Rect secondLineRect = firstLineRect;
            secondLineRect.y += firstLineRect.height + interline;
            Rect labelRect = firstLineRect;
            labelRect.width = EditorGUIUtility.labelWidth;
            Rect firstVectorValueRect = firstLineRect;
            firstVectorValueRect.xMin += labelRect.width + fixAlignSubVector3Labels;

            EditorGUI.BeginProperty(wholeArea, label, positive);
            EditorGUI.BeginProperty(wholeArea, label, negative);
            {
                EditorGUI.LabelField(labelRect, label);
            }
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            if (!allowIntersection)
            {
                max = negative.vector3Value;
                max.x = 1 - max.x;
                max.y = 1 - max.y;
                max.z = 1 - max.z;
            }

            DrawVector3(firstVectorValueRect, k_DrawVector6_Label[0], positive, min, max, false, colors == null ? null : new Color[] { colors[0], colors[1], colors[2] }, multiplicator);

            Rect secondVectorValueRect = secondLineRect;
            secondVectorValueRect.xMin = firstVectorValueRect.xMin;
            secondVectorValueRect.xMax = firstVectorValueRect.xMax;

            if (!allowIntersection)
            {
                max = positive.vector3Value;
                max.x = 1 - max.x;
                max.y = 1 - max.y;
                max.z = 1 - max.z;
            }

            DrawVector3(secondVectorValueRect, k_DrawVector6_Label[1], negative, min, max, true, colors == null ? null : new Color[] { colors[3], colors[4], colors[5] }, multiplicator);

            GUILayout.EndVertical();
        }

        static void DrawVector3(Rect rect, GUIContent[] labels, SerializedProperty value, Vector3 min, Vector3 max, bool minusPrefix, Color[] colors, SerializedProperty multiplicator = null)
        {
            float[] multifloat = multiplicator == null
                ? new float[] { value.vector3Value.x, value.vector3Value.y, value.vector3Value.z }
            : new float[] { value.vector3Value.x * multiplicator.vector3Value.x, value.vector3Value.y * multiplicator.vector3Value.y, value.vector3Value.z * multiplicator.vector3Value.z };

            float fieldWidth = rect.width / 3f;
            const int subLabelWidth = 13;
            const int colorWidth = 2;
            const int colorStartDecal = 1;
            const int valuesSeparator = 2;

            SerializedProperty[] values = new[]
            {
                value.FindPropertyRelative("x"),
                value.FindPropertyRelative("y"),
                value.FindPropertyRelative("z"),
            };

            int oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = subLabelWidth;

            for (int i = 0; i < 3; ++i)
            {
                Rect localRect = rect;
                localRect.xMin += i * fieldWidth;// + (i > 0 ? valuesSeparator : 0);
                localRect.xMax -= (2 - i) * fieldWidth + (i < 2 ? valuesSeparator : 0);
                Rect colorRect = localRect;
                colorRect.x = localRect.x + subLabelWidth + colorStartDecal;
                colorRect.width = colorWidth;
                colorRect.yMin += 2;
                colorRect.yMax -= 2;
                if (minusPrefix)
                {
                    localRect.xMin -= 3;
                    EditorGUIUtility.labelWidth = subLabelWidth + 3;
                }
                else
                    EditorGUIUtility.labelWidth = subLabelWidth;


                if (multiplicator == null)
                {
                    EditorGUI.BeginProperty(localRect, labels[i], values[i]);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(localRect, values[i], labels[i]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        values[i].floatValue = Mathf.Clamp(values[i].floatValue, min[i], max[i]);
                    }
                    EditorGUI.EndProperty();
                }
                else
                {
                    EditorGUI.BeginProperty(localRect, labels[i], values[i]);
                    EditorGUI.BeginChangeCheck();
                    float localMultiplicator = multiplicator.vector3Value[i];
                    float multipliedValue = values[i].floatValue * localMultiplicator;
                    multipliedValue = EditorGUI.FloatField(localRect, labels[i], multipliedValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        values[i].floatValue = Mathf.Clamp((localMultiplicator < -0.00001 || 0.00001 < localMultiplicator) ? multipliedValue / localMultiplicator : 0f, min[i], max[i]);
                    }
                    EditorGUI.EndProperty();
                }

                EditorGUI.DrawRect(colorRect, colors[i]);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUI.indentLevel = oldIndentLevel;
        }

        static void DrawVector3_(Rect rect, GUIContent[] labels, SerializedProperty value, Vector3 min, Vector3 max, bool addMinusPrefix, Color[] colors, SerializedProperty multiplicator = null)
        {
            float[] multifloat = multiplicator == null
                ? new float[] { value.vector3Value.x, value.vector3Value.y, value.vector3Value.z }
            : new float[] { value.vector3Value.x * multiplicator.vector3Value.x, value.vector3Value.y * multiplicator.vector3Value.y, value.vector3Value.z * multiplicator.vector3Value.z };

            float fieldWidth = rect.width / 3f;
            EditorGUI.showMixedValue = value.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, labels, multifloat);
            if (EditorGUI.EndChangeCheck())
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
                Rect suffixRect = new Rect(rect.x - 4 - k_IndentMargin * EditorGUI.indentLevel, rect.y, 100, rect.height);
                for (int i = 0; i < 3; ++i)
                {
                    EditorGUI.LabelField(suffixRect, "-");
                    suffixRect.x += fieldWidth + .33f;
                }
            }

            //Color is a hack as nothing is done to handle this at the moment
            if (colors != null)
            {
                if (colors.Length != 3)
                    throw new System.ArgumentException("colors must have 3 elements.");

                Rect suffixRect = new Rect(rect.x + 7 - k_IndentMargin * EditorGUI.indentLevel, rect.y, 100, rect.height);
                GUIStyle colorMark = new GUIStyle(EditorStyles.label);
                colorMark.normal.textColor = colors[0];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth + 0.33f;
                colorMark.normal.textColor = colors[1];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth + .33f;
                colorMark.normal.textColor = colors[2];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
            }
        }

        /// <summary>Draw a popup</summary>
        /// <param name="label">the label</param>
        /// <param name="property">The data displayed</param>
        /// <param name="options">Options of the dropdown</param>
        public static void DrawPopup(GUIContent label, SerializedProperty property, string[] options)
        {
            var mode = property.intValue;
            if (mode >= options.Length)
                Debug.LogError($"Invalid option while trying to set {label.text}");

            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.MixedValueScope(property.hasMultipleDifferentValues))
            {
                mode = EditorGUILayout.Popup(label, mode, options);
            }
            if (EditorGUI.EndChangeCheck())
                property.intValue = mode;
        }

        /// <summary>
        /// Draw an EnumPopup handling multiEdition
        /// </summary>
        /// <param name="property">The data displayed</param>
        /// <param name="type">Type of the property</param>
        /// <param name="label">The label</param>
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

        /// <summary>Remove the keywords on the given materials</summary>
        /// <param name="material">The material to edit</param>
        public static void RemoveMaterialKeywords(Material material)
            => material.shaderKeywords = null;

        /// <summary>Get the AdditionalData of the given component </summary>
        /// <typeparam name="T">The type of the AdditionalData component</typeparam>
        /// <param name="targets">The object to seek for AdditionalData</param>
        /// <param name="initDefault">[Optional] The default value to use if there is no AdditionalData</param>
        /// <returns>return an AdditionalData component</returns>
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

        /// <summary>Add the appropriate AdditionalData to the given GameObject and its children containing the original component</summary>
        /// <typeparam name="T">The type of the original component</typeparam>
        /// <typeparam name="AdditionalT">The type of the AdditionalData component</typeparam>
        /// <param name="go">The root object to update</param>
        /// <param name="initDefault">[Optional] The default value to use if there is no AdditionalData</param>
        public static void AddAdditionalData<T, AdditionalT>(GameObject go, Action<AdditionalT> initDefault = null)
            where T : Component
            where AdditionalT : Component
        {
            var components = go.GetComponentsInChildren(typeof(T), true);
            foreach (var c in components)
            {
                if (!c.TryGetComponent<AdditionalT>(out _))
                {
                    var hd = c.gameObject.AddComponent<AdditionalT>();
                    if (initDefault != null)
                        initDefault(hd);
                }
            }
        }

        /// <summary>Create a game object</summary>
        /// <param name="parent">The parent</param>
        /// <param name="name">The wanted name (can be updated with a number if a sibling with same name exist</param>
        /// <param name="types">Required component on this object in addition to Transform</param>
        /// <returns>The created object</returns>
        public static GameObject CreateGameObject(GameObject parent, string name, params Type[] types)
            => ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);

        /// <summary>
        /// Creates a new GameObject and set it's position to the current view
        /// </summary>
        /// <param name="name">the name of the new gameobject</param>
        /// <param name="context">the parent of the gameobject</param>
        /// <returns>the created GameObject</returns>
        public static GameObject CreateGameObject(string name, UnityEngine.Object context)
        {
            var parent = context as GameObject;
            var go = CoreEditorUtils.CreateGameObject(parent, name);
            GameObjectUtility.SetParentAndAlign(go, context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            if (parent != null)
                go.transform.localPosition = Vector3.zero;
            else
            {
                if (EditorPrefs.GetBool("Create3DObject.PlaceAtWorldOrigin", false))
                    go.transform.localPosition = Vector3.zero;
                else
                    EditorApplication.ExecuteMenuItem("GameObject/Move To View");
            }
            return go;
        }

        /// <summary>Parse and return current project version</summary>
        /// <returns>The version</returns>
        static public string GetCurrentProjectVersion()
        {
            string[] readText = File.ReadAllLines("ProjectSettings/ProjectVersion.txt");
            // format is m_EditorVersion: 2018.2.0b7
            string[] versionText = readText[0].Split(' ');
            return versionText[1];
        }

        /// <summary></summary>
        /// <param name="VCSEnabled"></param>
        /// <param name="mat"></param>
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
        /// <returns>The loaded texture</returns>
        public static Texture2D LoadIcon(string path, string name, string extention = ".png")
            => LoadIcon(path, name, extention, false);

        //forceLowRes should be deprecated as soon as this is fixed in UIElement
        internal static Texture2D LoadIcon(string path, string name, string extention = ".png", bool forceLowRes = false)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
                return null;

            string prefix = "";

            var skin = currentSkin;
            if (skin == Skin.Professional)
                prefix = "d_";

            Texture2D icon = null;
            float pixelsPerPoint = GetGUIStatePixelsPerPoint();
            if (pixelsPerPoint > 1.0f && !forceLowRes)
            {
                icon = EditorGUIUtility.Load($"{path}/{prefix}{name}@2x{extention}") as Texture2D;
                if (icon == null && !string.IsNullOrEmpty(prefix))
                    icon = EditorGUIUtility.Load($"{path}/{name}@2x{extention}") as Texture2D;
                if (icon != null)
                    SetTexturePixelPerPoint(icon, 2.0f);
            }

            if (icon == null)
                icon = EditorGUIUtility.Load($"{path}/{prefix}{name}{extention}") as Texture2D;

            if (icon == null && !string.IsNullOrEmpty(prefix))
                icon = EditorGUIUtility.Load($"{path}/{name}{extention}") as Texture2D;

            TryToFixFilterMode(pixelsPerPoint, icon);

            return icon;
        }

        internal static Texture2D FindTexture(string name)
        {
            float pixelsPerPoint = GetGUIStatePixelsPerPoint();
            Texture2D icon = pixelsPerPoint > 1.0f
                ? EditorGUIUtility.FindTexture($"{name}@2x")
                : EditorGUIUtility.FindTexture(name);

            TryToFixFilterMode(pixelsPerPoint, icon);

            return icon;
        }

        internal static void TryToFixFilterMode(float pixelsPerPoint, Texture2D icon)
        {
            if (icon != null &&
                !Mathf.Approximately(GetTexturePixelPerPoint(icon), pixelsPerPoint) && //scaling are different
                !Mathf.Approximately(pixelsPerPoint % 1, 0)) //screen scaling is non-integer
            {
                icon.filterMode = FilterMode.Bilinear;
            }
        }

        #endregion

        internal static void BeginAdditionalPropertiesHighlight(AnimFloat animation)
        {
            var oldColor = GUI.color;
            GUI.color = Color.Lerp(CoreEditorStyles.backgroundColor * oldColor, CoreEditorStyles.backgroundHighlightColor, animation.value);
            EditorGUILayout.BeginVertical(CoreEditorStyles.additionalPropertiesHighlightStyle);
            GUI.color = oldColor;
        }

        internal static void EndAdditionalPropertiesHighlight()
        {
            EditorGUILayout.EndVertical();
        }

        internal static T CreateAssetAt<T>(Scene scene, string targetName) where T : ScriptableObject
        {
            string path;

            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name;
                var profilePath = scenePath + Path.DirectorySeparatorChar + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                {
                    var directories = profilePath.Split(Path.DirectorySeparatorChar);
                    string rootPath = "";
                    foreach (var directory in directories)
                    {
                        var newPath = rootPath + directory;
                        if (!AssetDatabase.IsValidFolder(newPath))
                            AssetDatabase.CreateFolder(rootPath.TrimEnd(Path.DirectorySeparatorChar), directory);
                        rootPath = newPath + Path.DirectorySeparatorChar;
                    }
                }

                path = profilePath + Path.DirectorySeparatorChar;
            }

            path += targetName.ReplaceInvalidFileNameCharacters() + ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        internal static bool IsAssetInReadOnlyPackage(string path)
        {
            Assert.IsNotNull(path);
            var info = PackageManager.PackageInfo.FindForAssetPath(path);
            return info != null && (info.source != PackageSource.Local && info.source != PackageSource.Embedded);
        }
    }
}
