using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    using UnityObject = UnityEngine.Object;

    public static class CoreEditorUtils
    {
        [Obsolete("Use EditorGUIUtility.TrTextContent(<title>, <tooltip>) instead.")]
        public static GUIContent GetContent(string textAndTooltip)
        {
            if (textAndTooltip == null)
                return GUIContent.none; //done in TrTextContent but here we need to split...

            var s = textAndTooltip.Split('|');
            if (s.Length > 1)
                return EditorGUIUtility.TrTextContent(s[0], s[1]);
            else
                return EditorGUIUtility.TrTextContent(s[0]);
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
        {
            DrawMultipleFields(EditorGUIUtility.TrTextContent(label), ppts, lbls);
        }

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
        {
            DrawHeader(EditorGUIUtility.TrTextContent(title));
        }

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
        /// <param name="isAdvanced"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="switchAdvanced"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> isAdvanced = null, Action switchAdvanced = null)
        {
            return DrawHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, isAdvanced, switchAdvanced);
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="isAdvanced"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="switchAdvanced"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> isAdvanced = null, Action switchAdvanced = null)
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
            
            var advancedRect = new Rect();
            if (isAdvanced != null)
            {
                advancedRect = backgroundRect;
                advancedRect.x += advancedRect.width - 16 - 1;
                advancedRect.y -= 2;
                advancedRect.height = 16;
                advancedRect.width = 16;

                GUIStyle styleAdvanced = new GUIStyle(GUI.skin.toggle);
                styleAdvanced.normal.background = isAdvanced()
                    ? Resources.Load<Texture2D>("Advanced_Pressed_mini")
                    : Resources.Load<Texture2D>("Advanced_UnPressed_mini");
                styleAdvanced.onActive.background = styleAdvanced.normal.background;
                styleAdvanced.onFocused.background = styleAdvanced.normal.background;
                styleAdvanced.onNormal.background = styleAdvanced.normal.background;
                styleAdvanced.onHover.background = styleAdvanced.normal.background;
                styleAdvanced.active.background = styleAdvanced.normal.background;
                styleAdvanced.focused.background = styleAdvanced.normal.background;
                styleAdvanced.hover.background = styleAdvanced.normal.background;
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(advancedRect, isAdvanced(), GUIContent.none, styleAdvanced);
                if(EditorGUI.EndChangeCheck() && switchAdvanced != null)
                {
                    switchAdvanced();
                }
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

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="isAdvanced"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="switchAdvanced"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawSubHeaderFoldout(string title, bool state, bool isBoxed = false, Func<bool> isAdvanced = null, Action switchAdvanced = null)
        {
            return DrawSubHeaderFoldout(EditorGUIUtility.TrTextContent(title), state, isBoxed, isAdvanced, switchAdvanced);
        }

        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="isAdvanced"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="switchAdvanced"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        public static bool DrawSubHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> isAdvanced = null, Action switchAdvanced = null)
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

            var advancedRect = new Rect();
            if (isAdvanced != null)
            {
                advancedRect = backgroundRect;
                advancedRect.x += advancedRect.width - 16 - 1;
                advancedRect.y -= 2;
                advancedRect.height = 16;
                advancedRect.width = 16;

                GUIStyle styleAdvanced = new GUIStyle(GUI.skin.toggle);
                styleAdvanced.normal.background = isAdvanced()
                    ? Resources.Load<Texture2D>("Advanced_Pressed_mini")
                    : Resources.Load<Texture2D>("Advanced_UnPressed_mini");
                styleAdvanced.onActive.background = styleAdvanced.normal.background;
                styleAdvanced.onFocused.background = styleAdvanced.normal.background;
                styleAdvanced.onNormal.background = styleAdvanced.normal.background;
                styleAdvanced.onHover.background = styleAdvanced.normal.background;
                styleAdvanced.active.background = styleAdvanced.normal.background;
                styleAdvanced.focused.background = styleAdvanced.normal.background;
                styleAdvanced.hover.background = styleAdvanced.normal.background;
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(advancedRect, isAdvanced(), GUIContent.none, styleAdvanced);
                if (EditorGUI.EndChangeCheck() && switchAdvanced != null)
                {
                    switchAdvanced();
                }
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

        public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null)
        {
            return DrawHeaderToggle(EditorGUIUtility.TrTextContent(title), group, activeField, contextAction);
        }

        public static bool DrawHeaderToggle(GUIContent title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x += 16f;
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
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Foldout
            group.serializedObject.Update();
            group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
            group.serializedObject.ApplyModifiedProperties();

            // Active checkbox
            activeField.serializedObject.Update();
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            activeField.serializedObject.ApplyModifiedProperties();

            // Context menu
            var menuIcon = CoreEditorStyles.paneOptionsIcon;
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

        static readonly GUIContent[] k_DrawVector6_Label =
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z"),
        };
        const int k_DrawVector6Slider_LabelSize = 60;
        const int k_DrawVector6Slider_FieldSize = 80;

        public static void DrawVector6(GUIContent label, ref Vector3 positive, ref Vector3 negative, Vector3 min, Vector3 max, Color[] colors = null)
        {
            if (colors != null && (colors.Length != 6))
                    throw new System.ArgumentException("Colors must be a 6 element array. [+X, +Y, +X, -X, -Y, -Z]");

            GUILayout.BeginVertical();
            Rect rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            if (label != GUIContent.none)
            {
                var labelRect = rect;
                labelRect.x -= 11f * EditorGUI.indentLevel;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.x += EditorGUIUtility.labelWidth - 1f - 11f * EditorGUI.indentLevel;
                rect.width -= EditorGUIUtility.labelWidth - 1f - 11f * EditorGUI.indentLevel;
            }
            
            var v = positive;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3(rect, k_DrawVector6_Label, v, min, max, false, colors == null ? null : new Color[] { colors[0], colors[1], colors[2] });
            if (EditorGUI.EndChangeCheck())
                positive = v;

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            rect.x += EditorGUIUtility.labelWidth - 1f - 11f * EditorGUI.indentLevel;
            rect.width -= EditorGUIUtility.labelWidth - 1f - 11f * EditorGUI.indentLevel;
            v = negative;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3(rect, k_DrawVector6_Label, v, min, max, true, colors == null ? null : new Color[] { colors[3], colors[4], colors[5] });
            if (EditorGUI.EndChangeCheck())
                negative = v;
            GUILayout.EndVertical();
        }

        static Vector3 DrawVector3(Rect rect, GUIContent[] labels, Vector3 value, Vector3 min, Vector3 max, bool addMinusPrefix, Color[] colors)
        {
            float[] multifloat = new float[] { value.x, value.y, value.z };
            //rect = EditorGUI.IndentedRect(rect);
            float fieldWidth = rect.width / 3f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, labels, multifloat);
            if(EditorGUI.EndChangeCheck())
            {
                value.x = Mathf.Max(Mathf.Min(multifloat[0], max.x), min.x);
                value.y = Mathf.Max(Mathf.Min(multifloat[1], max.y), min.y);
                value.z = Mathf.Max(Mathf.Min(multifloat[2], max.z), min.z);
            }

            //Suffix is a hack as sublabel only work with 1 character
            if(addMinusPrefix)
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
            return value;
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

        public static void RemoveMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
        }

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
        {
            return ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);
        }

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
    }
}
