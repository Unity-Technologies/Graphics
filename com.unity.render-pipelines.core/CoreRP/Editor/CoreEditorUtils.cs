using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        public static void DrawMultipleFields(string label, SerializedProperty[] ppts, GUIContent[] lbls)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(GetContent(label));
            GUILayout.BeginVertical();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 45;
            for (var i = 0; i < ppts.Length; ++i)
                EditorGUILayout.PropertyField(ppts[i], lbls[i]);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
        }

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

        static readonly GUIContent[] k_DrawVector6Slider_LabelPositives =
        {
            new GUIContent("+X"),
            new GUIContent("+Y"),
            new GUIContent("+Z"),
        };
        static readonly GUIContent[] k_DrawVector6Slider_LabelNegatives =
        {
            new GUIContent("-X"),
            new GUIContent("-Y"),
            new GUIContent("-Z"),
        };
        const int k_DrawVector6Slider_LabelSize = 60;
        const int k_DrawVector6Slider_FieldSize = 80;
        public static void DrawVector6Slider(GUIContent label, SerializedProperty positive, SerializedProperty negative, Vector3 min, Vector3 max)
        {
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField(label);
            ++EditorGUI.indentLevel;

            var rect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var v = positive.vector3Value;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3Slider(rect, k_DrawVector6Slider_LabelPositives, v, min, max);
            if (EditorGUI.EndChangeCheck())
                positive.vector3Value = v;

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            rect = GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            v = negative.vector3Value;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3Slider(rect, k_DrawVector6Slider_LabelNegatives, v, min, max);
            if (EditorGUI.EndChangeCheck())
                negative.vector3Value = v;
            --EditorGUI.indentLevel;
            GUILayout.EndVertical();
        }

        static Vector3 DrawVector3Slider(Rect rect, GUIContent[] labels, Vector3 value, Vector3 min, Vector3 max)
        {
            // Use a corrected width due to the hacks used for layouting the slider properly below
            rect.width -= 20;
            var fieldWidth = rect.width / 3f;

            for (var i = 0; i < 3; ++i)
            {
                var c = new Rect(rect.x + fieldWidth * i, rect.y, fieldWidth, rect.height);
                var labelRect = new Rect(c.x, c.y, k_DrawVector6Slider_LabelSize, c.height);
                var sliderRect = new Rect(labelRect.x + labelRect.width, c.y, c.width - k_DrawVector6Slider_LabelSize - k_DrawVector6Slider_FieldSize + 45, c.height);
                var fieldRect = new Rect(sliderRect.x + sliderRect.width - 25, c.y, k_DrawVector6Slider_FieldSize, c.height);
                EditorGUI.LabelField(labelRect, labels[i]);
                value[i] = GUI.HorizontalSlider(sliderRect, value[i], min[i], max[i]);
                value[i] = EditorGUI.FloatField(fieldRect, value[i]);
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
            {
                Undo.RecordObject(property.objectReferenceValue, property.name);
                property.intValue = mode;
            }
        }

        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;

            System.Type type = typeof(T);
            if (type == typeof(float))
            {
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            }
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
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
    }

    static class ShadowCascadeSplitGUI
    {
        private const int kSliderbarTopMargin = 2;
        private const int kSliderbarHeight = 24;
        private const int kSliderbarBottomMargin = 2;
        private const int kPartitionHandleWidth = 2;
        private const int kPartitionHandleExtraHitAreaWidth = 2;

        private static readonly Color[] kCascadeColors =
        {
            new Color(0.5f, 0.5f, 0.6f, 1.0f),
            new Color(0.5f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.5f, 0.5f, 1.0f),
        };

        // using a LODGroup skin
        private static readonly GUIStyle s_CascadeSliderBG = "LODSliderRange";
        private static readonly GUIStyle s_TextCenteredStyle =  new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        // Internal struct to bundle drag information
        private class DragCache
        {
            public int      m_ActivePartition;          // the cascade partition that we are currently dragging/resizing
            public float    m_NormalizedPartitionSize;  // the normalized size of the partition (0.0f < size < 1.0f)
            public Vector2  m_LastCachedMousePosition;  // mouse position the last time we registered a drag or mouse down.

            public DragCache(int activePartition, float normalizedPartitionSize, Vector2 currentMousePos)
            {
                m_ActivePartition = activePartition;
                m_NormalizedPartitionSize = normalizedPartitionSize;
                m_LastCachedMousePosition = currentMousePos;
            }
        };
        private static DragCache  s_DragCache;

        private static readonly int s_CascadeSliderId = "s_CascadeSliderId".GetHashCode();

        private static SceneView s_RestoreSceneView;
        private static SceneView.CameraMode s_OldSceneDrawMode;
        private static bool s_OldSceneLightingMode;


        /**
            *  Static function to handle the GUI and User input related to the cascade slider.
            *
            *  @param  normalizedCascadePartition      The array of partition sizes in the range 0.0f - 1.0f; expects ONE entry if cascades = 2, and THREE if cascades=4
            *                                          The last entry will be automatically determined by summing up the array, and doing 1.0f - sum
            */
        public static void HandleCascadeSliderGUI(ref float[] normalizedCascadePartitions)
        {
            EditorGUILayout.LabelField("Cascade splits");

            // get the inspector width since we need it while drawing the partition rects.
            // Only way currently is to reserve the block in the layout using GetRect(), and then immediately drawing the empty box
            // to match the call to GetRect.
            // From this point on, we move to non-layout based code.
            var sliderRect = GUILayoutUtility.GetRect(GUIContent.none
                    , s_CascadeSliderBG
                    , GUILayout.Height(kSliderbarTopMargin + kSliderbarHeight + kSliderbarBottomMargin)
                    , GUILayout.ExpandWidth(true));
            GUI.Box(sliderRect, GUIContent.none);

            float currentX = sliderRect.x;
            float cascadeBoxStartY = sliderRect.y + kSliderbarTopMargin;
            float cascadeSliderWidth = sliderRect.width - (normalizedCascadePartitions.Length * kPartitionHandleWidth);
            Color origTextColor = GUI.color;
            Color origBackgroundColor = GUI.backgroundColor;
            int colorIndex = -1;

            // setup the array locally with the last partition
            float[] adjustedCascadePartitions = new float[normalizedCascadePartitions.Length + 1];
            System.Array.Copy(normalizedCascadePartitions, adjustedCascadePartitions, normalizedCascadePartitions.Length);
            adjustedCascadePartitions[adjustedCascadePartitions.Length - 1] = 1.0f - normalizedCascadePartitions.Sum();


            // check for user input on any of the partition handles
            // this mechanism gets the current event in the queue... make sure that the mouse is over our control before consuming the event
            int sliderControlId = GUIUtility.GetControlID(s_CascadeSliderId, FocusType.Passive);
            Event currentEvent = Event.current;
            int hotPartitionHandleIndex = -1; // the index of any partition handle that we are hovering over or dragging

            // draw each cascade partition
            for (int i = 0; i < adjustedCascadePartitions.Length; ++i)
            {
                float currentPartition = adjustedCascadePartitions[i];

                colorIndex = (colorIndex + 1) % kCascadeColors.Length;
                GUI.backgroundColor = kCascadeColors[colorIndex];
                float boxLength = (cascadeSliderWidth * currentPartition);

                // main cascade box
                Rect partitionRect = new Rect(currentX, cascadeBoxStartY, boxLength, kSliderbarHeight);
                GUI.Box(partitionRect, GUIContent.none, s_CascadeSliderBG);
                currentX += boxLength;

                // cascade box percentage text
                GUI.color = Color.white;
                Rect textRect = partitionRect;
                var cascadeText = string.Format("{0}\n{1:F1}%", i, currentPartition * 100.0f);

                GUI.Label(textRect, cascadeText, s_TextCenteredStyle);

                // no need to draw the partition handle for last box
                if (i == adjustedCascadePartitions.Length - 1)
                    break;

                // partition handle
                GUI.backgroundColor = Color.black;
                Rect handleRect = partitionRect;
                handleRect.x = currentX;
                handleRect.width = kPartitionHandleWidth;
                GUI.Box(handleRect, GUIContent.none, s_CascadeSliderBG);
                // we want a thin handle visually (since wide black bar looks bad), but a slightly larger
                // hit area for easier manipulation
                Rect handleHitRect = handleRect;
                handleHitRect.xMin -= kPartitionHandleExtraHitAreaWidth;
                handleHitRect.xMax += kPartitionHandleExtraHitAreaWidth;
                if (handleHitRect.Contains(currentEvent.mousePosition))
                    hotPartitionHandleIndex = i;

                // add regions to slider where the cursor changes to Resize-Horizontal
                if (s_DragCache == null)
                {
                    EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.ResizeHorizontal, sliderControlId);
                }

                currentX += kPartitionHandleWidth;
            }

            GUI.color = origTextColor;
            GUI.backgroundColor = origBackgroundColor;

            EventType eventType = currentEvent.GetTypeForControl(sliderControlId);
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (hotPartitionHandleIndex >= 0)
                    {
                        s_DragCache = new DragCache(hotPartitionHandleIndex, normalizedCascadePartitions[hotPartitionHandleIndex], currentEvent.mousePosition);
                        if (GUIUtility.hotControl == 0)
                            GUIUtility.hotControl = sliderControlId;
                        currentEvent.Use();

                        // Switch active scene view into shadow cascades visualization mode, once we start
                        // tweaking cascade splits.
                        if (s_RestoreSceneView == null)
                        {
                            s_RestoreSceneView = SceneView.lastActiveSceneView;
                            if (s_RestoreSceneView != null)
                            {
                                s_OldSceneDrawMode = s_RestoreSceneView.cameraMode;
                                s_OldSceneLightingMode = s_RestoreSceneView.m_SceneLighting;
                                s_RestoreSceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.ShadowCascades);
                            }
                        }
                    }
                    break;

                case EventType.MouseUp:
                    // mouseUp event anywhere should release the hotcontrol (if it belongs to us), drags (if any)
                    if (GUIUtility.hotControl == sliderControlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    s_DragCache = null;

                    // Restore previous scene view drawing mode once we stop tweaking cascade splits.
                    if (s_RestoreSceneView != null)
                    {
                        s_RestoreSceneView.cameraMode = s_OldSceneDrawMode;
                        s_RestoreSceneView.m_SceneLighting = s_OldSceneLightingMode;
                        s_RestoreSceneView = null;
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != sliderControlId)
                        break;

                    // convert the mouse movement to normalized cascade width. Make sure that we are safe to apply the delta before using it.
                    float delta = (currentEvent.mousePosition - s_DragCache.m_LastCachedMousePosition).x / cascadeSliderWidth;
                    bool isLeftPartitionHappy = ((adjustedCascadePartitions[s_DragCache.m_ActivePartition] + delta) > 0.0f);
                    bool isRightPartitionHappy = ((adjustedCascadePartitions[s_DragCache.m_ActivePartition + 1] - delta) > 0.0f);
                    if (isLeftPartitionHappy && isRightPartitionHappy)
                    {
                        s_DragCache.m_NormalizedPartitionSize += delta;
                        normalizedCascadePartitions[s_DragCache.m_ActivePartition] = s_DragCache.m_NormalizedPartitionSize;
                        if (s_DragCache.m_ActivePartition < normalizedCascadePartitions.Length - 1)
                            normalizedCascadePartitions[s_DragCache.m_ActivePartition + 1] -= delta;
                        GUI.changed = true;
                    }
                    s_DragCache.m_LastCachedMousePosition = currentEvent.mousePosition;
                    currentEvent.Use();
                    break;
            }
        }
    }
}
