using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreEditorUtils
    {
        // GUIContent cache utilities
        static Dictionary<string, GUIContent> s_GUIContentCache = new Dictionary<string, GUIContent>();

        public static GUIContent GetContent(string textAndTooltip)
        {
            if (string.IsNullOrEmpty(textAndTooltip))
                return GUIContent.none;

            GUIContent content;

            if (!s_GUIContentCache.TryGetValue(textAndTooltip, out content))
            {
                var s = textAndTooltip.Split('|');
                content = new GUIContent(s[0]);

                if (s.Length > 1 && !string.IsNullOrEmpty(s[1]))
                    content.tooltip = s[1];

                s_GUIContentCache.Add(textAndTooltip, content);
            }

            return content;
        }

        // Serialization helpers
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
                members.Add(me.Member.Name);
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
        public static void DrawSplitter()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static void DrawHeader(string title)
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

        public static bool DrawHeaderFoldout(string title, bool state)
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

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }

        public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var toggleRect = backgroundRect;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, GetContent(title), EditorStyles.boldLabel);

            // Active checkbox
            activeField.serializedObject.Update();
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            activeField.serializedObject.ApplyModifiedProperties();

            // Context menu
            var menuIcon = EditorGUIUtility.isProSkin
                ? CoreEditorStyles.paneOptionsIconDark
                : CoreEditorStyles.paneOptionsIconLight;

            var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);

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

        public static void RemoveMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
        }

        public static T[] GetAdditionalData<T>(params UnityEngine.Object[] targets)
            where T : Component
        {
            // Handles multi-selection
            var data = targets.Cast<Component>()
                .Select(t => t.GetComponent<T>())
                .ToArray();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == null)
                    data[i] = Undo.AddComponent<T>(((Component)targets[i]).gameObject);
            }

            return data;
        }
    }
}
