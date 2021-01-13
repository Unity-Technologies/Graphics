using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Helper class for drawing shadow cascade with GUI.
    /// </summary>
    public static class ShadowCascadeGUI
    {
        private const float kSliderbarMargin = 2.0f;
        private const float kSliderbarHeight = 25.0f;

        private static readonly Color[] kCascadeColors =
        {
            new Color(0.5f, 0.5f, 0.6f, 1.0f),
            new Color(0.5f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.5f, 0.5f, 1.0f),
        };
        private static readonly Color kDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); //works with both personal and pro skin

        private static GUIStyle s_UpSwatch = "Grad Up Swatch";
        private static GUIStyle s_DownSwatch = "Grad Down Swatch";
        private static GUIStyle s_CascadeSliderBG = "LODSliderRange"; // using a LODGroup skin
        private static readonly GUIStyle s_TextCenteredStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        /// <summary>
        /// Represents the state of the cascade handle.
        /// </summary>
        public enum HandleState
        {
            /// <summary>
            /// Handle will not be drawn.
            /// </summary>
            Hidden,
            /// <summary>
            /// Handle will be disabled.
            /// </summary>
            Disabled,
            /// <summary>
            /// Handle will be enabled.
            /// </summary>
            Enabled,
        }

        /// <summary>
        /// Data of single cascade for drawing in GUI.
        /// </summary>
        public struct Cascade
        {
            /// <summary>
            /// Cascade normalized size that ranges from 0 to 1.
            /// Sum of all cascade sizes can not exceed 1.
            /// </summary>
            public float size;

            /// <summary>
            /// Cascade border size that ranges from 0 to 1.
            /// Border represents the width of shadow blend.
            /// Where 0 value result in no blend and 1 will blend from cascade beginning.
            /// </summary>
            public float borderSize;

            /// <summary>
            /// Current state of cascade handle that will be used for drawing it.
            /// </summary>
            public HandleState cascadeHandleState;

            /// <summary>
            /// Current state of border handle that will be used for drawing it.
            /// </summary>
            public HandleState borderHandleState;
        }

        /// <summary>
        /// Draw cascades using editor GUI. This also includes handles
        /// </summary>
        /// <param name="cascades">Array of cascade data.</param>
        /// <param name="useMetric">True if numbers should be presented with metric system, otherwise procentage.</param>
        /// <param name="baseMetric">The base of the metric system. In most cases it is maximum shadow distance.</param>
        public static void DrawCascades(ref Cascade[] cascades, bool useMetric, float baseMetric)
        {
            EditorGUILayout.BeginVertical();

            // Space for cascade handles
            GUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();

            // Correctly handle indents
            GUILayout.Space(EditorGUI.indentLevel * 15f);

            var sliderRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                s_CascadeSliderBG,
                GUILayout.Height(kSliderbarMargin + kSliderbarHeight + kSliderbarMargin),
                GUILayout.ExpandWidth(true));
            DrawBackgroundBoxGUI(sliderRect, Color.gray);

            var formatSymbol = useMetric ? 'm' : '%';
            var usableRect = new Rect(sliderRect.x + kSliderbarMargin, sliderRect.y + kSliderbarMargin, sliderRect.width - kSliderbarMargin * 2, sliderRect.height - kSliderbarMargin * 2);
            var partitionWidth = 2.0f / EditorGUIUtility.pixelsPerPoint;
            var partitionHalfWidth = partitionWidth * 0.5f;

            // Calculate pixel perfect cascade widths
            float widthForCascades = usableRect.width;
            float[] cascadeWidths = new float[cascades.Length];
            float sumOfCascadeWidthsWithoutLast = 0;
            float startX = 0;
            for (int i = 0; i < cascades.Length - 1; ++i)
            {
                float endX = startX + cascades[i].size * widthForCascades;

                float pixelPerfectStartX = Mathf.Round(startX * EditorGUIUtility.pixelsPerPoint) / EditorGUIUtility.pixelsPerPoint;
                float pixelPerfectEndX = Mathf.Round(endX * EditorGUIUtility.pixelsPerPoint) / EditorGUIUtility.pixelsPerPoint;
                float pixelPerfectCascadeWidth = pixelPerfectEndX - pixelPerfectStartX;

                cascadeWidths[i] = pixelPerfectCascadeWidth;
                sumOfCascadeWidthsWithoutLast += cascadeWidths[i];

                startX = endX;
            }
            cascadeWidths[cascades.Length - 1] = widthForCascades - sumOfCascadeWidthsWithoutLast;

            float currentX = usableRect.x;
            for (int i = 0; i < cascades.Length; ++i)
            {
                ref var cascade = ref cascades[i];
                var cascadeWidth = cascadeWidths[i];

                bool isLastCascade = (i == cascades.Length - 1);

                // Split cascade into cascade without border and border
                float borderValue;
                float cascadeValue;
                float borderWidth;
                float cascadeWithoutBorderWidth;
                if (cascade.borderHandleState != HandleState.Hidden)
                {
                    borderValue = cascade.size * cascade.borderSize;
                    cascadeValue = cascade.size - borderValue;
                    var cascadeWidthWithoutPartition = cascadeWidth;
                    cascadeWithoutBorderWidth = Mathf.Round(cascadeWidthWithoutPartition * (1 - cascade.borderSize) * EditorGUIUtility.pixelsPerPoint) / EditorGUIUtility.pixelsPerPoint;
                    borderWidth = cascadeWidth - cascadeWithoutBorderWidth;
                }
                else
                {
                    borderValue = 0;
                    cascadeValue = cascade.size;
                    borderWidth = 0;
                    cascadeWithoutBorderWidth = cascadeWidth;
                }

                // Draw cascade
                var cascadeRect = new Rect(currentX, usableRect.y, cascadeWithoutBorderWidth, usableRect.height);
                currentX += DrawBoxGUI(cascadeRect, kCascadeColors[i]);

                // Draw cascade text
                float cascadeValueForText = useMetric ? cascadeValue * baseMetric : cascadeValue * 100;
                string cascadeText = $"{i}\n{cascadeValueForText:F1}{formatSymbol}";
                DrawLabelGUI(cascadeRect, cascadeText, Color.white);

                if (cascade.borderHandleState != HandleState.Hidden)
                {
                    // As we are rounding everything against pixel per point and subtracting from total it might result in fractions for the last cascade border
                    if (isLastCascade && cascade.borderSize == 0.0)
                        borderWidth = 0;

                    // Draw border snatch handle
                    var borderPartitionHandleRect = new Rect(
                        currentX - 5 - partitionHalfWidth,
                        usableRect.y + usableRect.height - 3,
                        10,
                        15);
                    var enabled = cascade.borderHandleState == HandleState.Enabled;
                    var borderPartitionColor = enabled ? kCascadeColors[i] : kDisabledColor; ;
                    var delta = DrawSnatchWithHandle(borderPartitionHandleRect, cascadeWidth, borderPartitionColor, s_UpSwatch, enabled);
                    cascade.borderSize = Mathf.Clamp01(cascade.borderSize - delta);

                    // Draw border partition
                    DrawBoxGUI(new Rect(currentX - partitionWidth, usableRect.y, partitionWidth, usableRect.height), Color.black);

                    // Draw border
                    var borderRect = new Rect(currentX, usableRect.y, borderWidth, usableRect.height);
                    var gradientLeftColor = kCascadeColors[i];
                    var gradientRightColor = isLastCascade ? Color.black : kCascadeColors[i + 1];
                    currentX += DrawGradientBoxGUI(borderRect, gradientLeftColor, gradientRightColor);

                    // Draw border text
                    float borderValueForText = useMetric ? borderValue * baseMetric : borderValue * 100;
                    string borderText;
                    if (isLastCascade)
                    {
                        string fallbackText = (borderWidth < 57) ? "F." : "Fallback";
                        borderText = $"{i}\u2192{fallbackText}\n{borderValueForText:F1}{formatSymbol}";
                    }
                    else
                    {
                        borderText = $"{i}\u2192{i + 1}\n{borderValueForText:F1}{formatSymbol}";
                    }
                    DrawLabelGUI(borderRect, borderText, Color.white);
                }

                if (!isLastCascade) // Don't draw partition for last cascade
                {
                    if (cascade.cascadeHandleState != HandleState.Hidden)
                    {
                        // Draw cascade partition snatch handle
                        var cascadeHandleRect = new Rect(
                            currentX - 5 - partitionHalfWidth,
                            usableRect.y - 15 + 1,
                            10,
                            15);
                        var enabled = cascade.cascadeHandleState == HandleState.Enabled;
                        var cascadePartitionColor = enabled ? kCascadeColors[i + 1] : kDisabledColor;
                        var delta = DrawSnatchWithHandle(cascadeHandleRect, usableRect.width, cascadePartitionColor, s_DownSwatch, enabled);
                        
                        if (delta != 0)
                        {
                            ref var nextCascade = ref cascades[i + 1];

                            // We want to resize only the current cascade and next cascade
                            // Lets convert this problem to the slider
                            var sliderMinimum = 0;
                            var sliderMaximum = cascade.size + nextCascade.size;
                            var sliderPosition = cascade.size + delta;

                            // Force minimum cascade size and prevent cascade going out of bounds
                            var cascadeMinimumSize = 0.001f;
                            var sliderPositionPixelPerfectClamped = Mathf.Clamp(sliderPosition,
                                sliderMinimum + cascadeMinimumSize, sliderMaximum - cascadeMinimumSize);

                            cascade.size = sliderPositionPixelPerfectClamped;
                            nextCascade.size = sliderMaximum - sliderPositionPixelPerfectClamped;
                        }
                    }

                    // Draw cascade partition
                    DrawBoxGUI(new Rect(currentX - partitionWidth, usableRect.y, partitionWidth, usableRect.height), Color.black);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Space for border handles
            GUILayout.Space(10f);

            EditorGUILayout.EndVertical();
        }

        private static float DrawBackgroundBoxGUI(Rect rect, Color color)
        {
            var cachedColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none);
            GUI.backgroundColor = cachedColor;
            return rect.width;
        }

        private const string kPathToGradientWhiteTexture = "Packages/com.unity.render-pipelines.core/Editor/Resources/HorizontalGradient.png";
        private static Texture2D s_GradientWhiteTexture;
        private static float DrawGradientBoxGUI(Rect rect, Color leftColor, Color rightColor)
        {
            if (s_GradientWhiteTexture == null)
            {
                s_GradientWhiteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(kPathToGradientWhiteTexture);
                Debug.Assert(s_GradientWhiteTexture != null);
            }

            // Draw right color as background
            var cachedColor = GUI.backgroundColor;
            GUI.backgroundColor = rightColor;
            GUI.Box(rect, GUIContent.none, s_CascadeSliderBG);
            GUI.backgroundColor = cachedColor;

            // Draw left color as gradient overlay
            cachedColor = GUI.color;
            GUI.color = leftColor;
            GUI.DrawTexture(rect, s_GradientWhiteTexture);
            GUI.color = cachedColor;

            return rect.width;
        }

        private static float DrawBoxGUI(Rect rect, Color color)
        {
            var cachedColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none, s_CascadeSliderBG);
            GUI.backgroundColor = cachedColor;
            var realRect = new Rect(
                rect.x * EditorGUIUtility.pixelsPerPoint,
                rect.y * EditorGUIUtility.pixelsPerPoint,
                rect.width * EditorGUIUtility.pixelsPerPoint,
                rect.height * EditorGUIUtility.pixelsPerPoint
                );
            //Debug.Log(realRect);
            return rect.width;
        }

        private static float DrawLabelGUI(Rect rect, string text, Color color)
        {
            var cachedColor = GUI.backgroundColor;
            GUI.color = color;
            GUI.Label(rect, text, s_TextCenteredStyle);
            GUI.backgroundColor = cachedColor;
            return rect.width;
        }

        private static Vector2 s_DragLastMousePosition;
        private static readonly int s_CascadeSliderId = "s_CascadeSliderId".GetHashCode();
        private static float DrawSnatchWithHandle(Rect rect, float distance, Color color, GUIStyle snatch, bool enabled = true)
        {
            // check for user input on any of the partition handles
            // this mechanism gets the current event in the queue... make sure that the mouse is over our control before consuming the event
            int sliderControlId = GUIUtility.GetControlID(FocusType.Passive, rect);
            Event currentEvent = Event.current;
            EventType eventType = currentEvent.GetTypeForControl(sliderControlId);

            if (eventType == EventType.Repaint)
            {
                var cachedColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
                snatch.Draw(rect, true, false, false, false);
                GUI.backgroundColor = cachedColor;
            }

            float delta = 0;

            if (enabled)
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal, sliderControlId);

                switch (eventType)
                {
                    case EventType.MouseDown:
                        if (!rect.Contains(currentEvent.mousePosition))
                            break;

                        // We do not consume event on purpose.
                        // In case there is overlapping snatch, this way the last one will be hot control

                        GUIUtility.hotControl = sliderControlId;

                        s_DragLastMousePosition = currentEvent.mousePosition;
                        break;

                    case EventType.MouseUp:
                        // mouseUp event anywhere should release the hotcontrol (if it belongs to us), drags (if any)
                        if (GUIUtility.hotControl == sliderControlId)
                        {
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl != sliderControlId)
                            break;

                        delta = (currentEvent.mousePosition - s_DragLastMousePosition).x / (distance);

                        GUI.changed = true;

                        s_DragLastMousePosition = currentEvent.mousePosition;
                        currentEvent.Use();
                        break;
                }
            }

            return delta;
        }
    }
}
