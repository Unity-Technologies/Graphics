using System;
using UnityEngine;
using System.Linq;
using UnityEditor.Rendering;

namespace UnityEditor
{
    static class ShadowCascadeGUI
    {
        private const int kSliderbarTopMargin = 2;
        private const int kSliderbarHeight = 25;
        private const int kSliderbarBottomMargin = 2;
        private const int kPartitionHandleWidth = 2;
        private const int kPartitionHandleExtraHitAreaWidth = 2;
        private static GUIStyle s_UpSwatch = "Grad Up Swatch";
        private static GUIStyle s_DownSwatch = "Grad Down Swatch";
        private static int s_BlendHandleSelected = -1;

        private static readonly GUIContent s_Text = new GUIContent();
        private static GUIContent TempGUIContent(string label, string tooltip)
        {
            s_Text.text = label;
            s_Text.tooltip = tooltip;
            return s_Text;
        }

        private static readonly Color[] kCascadeColors =
        {
            new Color(0.5f, 0.5f, 0.6f, 1.0f),
            new Color(0.5f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.5f, 0.5f, 1.0f),
        };
        private static readonly Color kDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); //works with both personal and pro skin

        class LazyTextureArray
        {
            Texture2D[] values = new[]
            {
                new Texture2D(1, 1),
                new Texture2D(1, 1),
                new Texture2D(1, 1),
                new Texture2D(1, 1),
            };
            public Texture2D this[int index]
            {
                get
                {
                    if (index < 0 || 3 < index)
                        throw new IndexOutOfRangeException();

                    if (values.Length != 4)
                    {
                        values = new[]
                        {
                            new Texture2D(1, 1),
                            new Texture2D(1, 1),
                            new Texture2D(1, 1),
                            new Texture2D(1, 1),
                        };
                    }
                    var value = values[index];
                    if (value == null || value.Equals(null))
                        value = values[index] = new Texture2D(1, 1);
                    return value;
                }
            }
        }
        private static readonly Lazy<LazyTextureArray> kBorderBlends = new Lazy<LazyTextureArray>();

        // using a LODGroup skin
        private static readonly GUIStyle s_CascadeSliderBG = "LODSliderRange";
        private static readonly GUIStyle s_TextCenteredStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        // Internal struct to bundle drag information
        private class DragCache
        {
            public int activePartition;          // the cascade partition that we are currently dragging/resizing
            public float normalizedPartitionSize;  // the normalized size of the partition (0.0f < size < 1.0f)
            public float endBlendAreaPercent;
            public Vector2 lastCachedMousePosition;  // mouse position the last time we registered a drag or mouse down.
            public bool isEndBlendArea;

            public DragCache(int activePartition, float normalizedPartitionSize, float endBlendAreaPercent, Vector2 currentMousePos, bool isEndBlendArea)
            {
                this.activePartition = activePartition;
                this.normalizedPartitionSize = normalizedPartitionSize;
                this.endBlendAreaPercent = endBlendAreaPercent;
                this.isEndBlendArea = isEndBlendArea;
                lastCachedMousePosition = currentMousePos;
            }
        };
        private static DragCache s_DragCache;

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
        static void HandleCascadeSliderGUI(ref float[] normalizedCascadePartitions, ref float[] endPartitionBordersPercent, bool[] enabledCascadePartitionHandles, bool[] enabledEndPartitionBorderHandles, bool drawEndPartitionHandles, bool useMetric, float baseMetric)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            // get the inspector width since we need it while drawing the partition rects.
            // Only way currently is to reserve the block in the layout using GetRect(), and then immediately drawing the empty box
            // to match the call to GetRect.
            // From this point on, we move to non-layout based code.
            var sliderRect = GUILayoutUtility.GetRect(GUIContent.none
                , s_CascadeSliderBG
                , GUILayout.Height(kSliderbarTopMargin + kSliderbarHeight + kSliderbarBottomMargin)
                , GUILayout.ExpandWidth(true));
            GUI.Box(sliderRect, GUIContent.none);

            float currentX = sliderRect.x + 3;
            float cascadeBoxStartY = sliderRect.y + kSliderbarTopMargin;
            int borderAdjustment = (3 - normalizedCascadePartitions.Length) * 2;
            float cascadeSliderWidth = sliderRect.width - (normalizedCascadePartitions.Length * kPartitionHandleWidth) - borderAdjustment;
            Color origTextColor = GUI.color;
            Color origBackgroundColor = GUI.backgroundColor;
            int colorIndex = -1;

            // setup the array locally with the last partition
            float[] adjustedCascadePartitions = new float[normalizedCascadePartitions.Length + 1];
            Array.Copy(normalizedCascadePartitions, adjustedCascadePartitions, normalizedCascadePartitions.Length);
            adjustedCascadePartitions[adjustedCascadePartitions.Length - 1] = 1.0f - normalizedCascadePartitions.Sum();


            // check for user input on any of the partition handles
            // this mechanism gets the current event in the queue... make sure that the mouse is over our control before consuming the event
            int sliderControlId = GUIUtility.GetControlID(s_CascadeSliderId, FocusType.Passive);
            Event currentEvent = Event.current;
            int hotPartitionHandleIndex = -1; // the index of any partition handle that we are hovering over or dragging

            EventType eventType = currentEvent.GetTypeForControl(sliderControlId);

            // draw each cascade partition
            for (int i = 0; i < adjustedCascadePartitions.Length; ++i)
            {
                float currentPartition = adjustedCascadePartitions[i];

                colorIndex = (colorIndex + 1) % kCascadeColors.Length;
                GUI.backgroundColor = kCascadeColors[colorIndex];
                float boxLength = Mathf.RoundToInt(cascadeSliderWidth * currentPartition);

                // main cascade box
                Rect partitionRect = new Rect(currentX, cascadeBoxStartY, boxLength, kSliderbarHeight);
                GUI.Box(partitionRect, GUIContent.none, s_CascadeSliderBG);
                currentX += boxLength;

                // cascade box texts preparation
                Rect fullCascadeText = partitionRect;
                Rect blendCascadeText = partitionRect;
                blendCascadeText.x += partitionRect.width;
                blendCascadeText.width = 0f;
                float fullCascadeValue = currentPartition * 100.0f;
                float blendCascadeValue = 0f;

                Rect separationRect = partitionRect;
                if (i < endPartitionBordersPercent.Length)
                {
                    // partition blend background and separators
                    GUI.backgroundColor = Color.black;
                    separationRect.width = Mathf.Max(kPartitionHandleWidth, Mathf.CeilToInt(endPartitionBordersPercent[i] * partitionRect.width));
                    separationRect.x = Mathf.CeilToInt(partitionRect.x + partitionRect.width - separationRect.width);
                    GUI.Box(separationRect, GUIContent.none, s_CascadeSliderBG);

                    //update cascade box texts update
                    blendCascadeValue = endPartitionBordersPercent[i] * currentPartition * 100f;
                    fullCascadeValue -= blendCascadeValue;
                    blendCascadeText.x -= separationRect.width - 1; //remove left border
                    blendCascadeText.width = endPartitionBordersPercent[i] * boxLength;
                    fullCascadeText.width -= separationRect.width;
                    blendCascadeText.width -= 3; //remove right border
                }

                // full cascade box text
                GUI.color = Color.white;
                float textValue = fullCascadeValue;
                if (useMetric)
                    textValue *= baseMetric / 100;
                var cascadeText = String.Format(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}\n{1:F1}{2}", i, textValue, useMetric ? 'm' : '%');
                GUI.Label(fullCascadeText, TempGUIContent(cascadeText, cascadeText), s_TextCenteredStyle);

                if (i >= endPartitionBordersPercent.Length)
                    break;

                // partition blend gradient
                Rect gradientRect = separationRect;
                gradientRect.x += 1;
                gradientRect.width -= 3;
                if (gradientRect.width > 0)
                {
                    kBorderBlends.Value[i].Resize((int)gradientRect.width, 1);
                    FillWithGradient(kBorderBlends.Value[i], kCascadeColors[i], i < adjustedCascadePartitions.Length - 1 ? kCascadeColors[i + 1] : Color.black);
                    GUI.DrawTexture(gradientRect, kBorderBlends.Value[i]);
                }

                // blend cascade box text
                textValue = blendCascadeValue;
                if (useMetric)
                    textValue *= baseMetric / 100;
                if (i == normalizedCascadePartitions.Length)
                {
                    cascadeText = String.Format(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}\u2192{1}\n{2:F1}{3}", i, blendCascadeText.width < 57 ? "F." : "Fallback", textValue, useMetric ? 'm' : '%');
                    string cascadeToolTip = String.Format(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}\u2192{1}\n{2:F1}{3}", i, "Fallback", textValue, useMetric ? 'm' : '%');
                    GUI.Label(blendCascadeText, TempGUIContent(cascadeText, cascadeToolTip), s_TextCenteredStyle);
                }
                else
                {
                    cascadeText = String.Format(System.Globalization.CultureInfo.InvariantCulture.NumberFormat, "{0}\u2192{1}\n{2:F1}{3}", i, i + 1, textValue, useMetric ? 'm' : '%');
                    GUI.Label(blendCascadeText, TempGUIContent(cascadeText, cascadeText), s_TextCenteredStyle);
                }

                // init rect for Swatches
                Rect cascadeHandleRect = default;
                if (i < normalizedCascadePartitions.Length)
                {
                    cascadeHandleRect = separationRect;
                    cascadeHandleRect.x += separationRect.width - 6f;
                    cascadeHandleRect.width = enabledCascadePartitionHandles[i] ? 10 : 0;
                    cascadeHandleRect.y -= 14;
                    cascadeHandleRect.height = 15;
                }
                Rect blendHandleRect = default;
                if (drawEndPartitionHandles)
                {
                    blendHandleRect = separationRect;
                    blendHandleRect.x -= 5f;
                    blendHandleRect.width = enabledEndPartitionBorderHandles[i] ? 10 : 0;
                    blendHandleRect.y += 22;
                    blendHandleRect.height = 15;
                }

                if (eventType == EventType.Repaint) //Can only draw the snatch in repaint event
                {
                    // Add handle to change end of cascade
                    if (i < normalizedCascadePartitions.Length)
                    {
                        GUI.backgroundColor = enabledCascadePartitionHandles[i] ? kCascadeColors[colorIndex + 1] : kDisabledColor;
                        s_DownSwatch.Draw(cascadeHandleRect, false, false, s_BlendHandleSelected == i, false);
                    }

                    if (drawEndPartitionHandles)
                    {
                        GUI.backgroundColor = enabledEndPartitionBorderHandles[i] ? kCascadeColors[colorIndex] : kDisabledColor;
                        s_UpSwatch.Draw(blendHandleRect, false, false, s_BlendHandleSelected == i + 100, false);
                    }
                }

                if (cascadeHandleRect.Contains(currentEvent.mousePosition))
                    hotPartitionHandleIndex = i;

                if (blendHandleRect.Contains(currentEvent.mousePosition))
                    hotPartitionHandleIndex = i + 100;

                // add regions to slider where the cursor changes to Resize-Horizontal
                EditorGUIUtility.AddCursorRect(cascadeHandleRect, MouseCursor.ResizeHorizontal, sliderControlId);
                EditorGUIUtility.AddCursorRect(blendHandleRect, MouseCursor.ResizeHorizontal, sliderControlId);
            }

            GUI.color = origTextColor;
            GUI.backgroundColor = origBackgroundColor;

            switch (eventType)
            {
                case EventType.MouseDown:
                    if (hotPartitionHandleIndex >= 0)
                    {
                        if (hotPartitionHandleIndex < 100)
                            s_DragCache = new DragCache(hotPartitionHandleIndex, normalizedCascadePartitions[hotPartitionHandleIndex], hotPartitionHandleIndex >= endPartitionBordersPercent.Length  ? 0f : endPartitionBordersPercent[hotPartitionHandleIndex], currentEvent.mousePosition, isEndBlendArea: false);
                        else
                        {
                            int endIndex = hotPartitionHandleIndex - 100;
                            s_DragCache = new DragCache(endIndex, adjustedCascadePartitions[endIndex], endPartitionBordersPercent[endIndex], currentEvent.mousePosition, isEndBlendArea: true);
                        }
                        if (GUIUtility.hotControl == 0)
                            GUIUtility.hotControl = sliderControlId;
                        currentEvent.Use();
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
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != sliderControlId)
                        break;

                    // convert the mouse movement to normalized cascade width. Make sure that we are safe to apply the delta before using it.
                    if (s_DragCache.isEndBlendArea)
                    {
                        float delta = (currentEvent.mousePosition - s_DragCache.lastCachedMousePosition).x / (cascadeSliderWidth * adjustedCascadePartitions[s_DragCache.activePartition]);
                        s_DragCache.endBlendAreaPercent = Mathf.Clamp01(s_DragCache.endBlendAreaPercent - delta);
                        endPartitionBordersPercent[s_DragCache.activePartition] = s_DragCache.endBlendAreaPercent;
                        GUI.changed = true;
                    }
                    else
                    {
                        float delta = (currentEvent.mousePosition - s_DragCache.lastCachedMousePosition).x / cascadeSliderWidth;
                        bool isLeftPartitionPositive = ((adjustedCascadePartitions[s_DragCache.activePartition] + delta) > 0.0f);
                        bool isRightPartitionPositive = ((adjustedCascadePartitions[s_DragCache.activePartition + 1] - delta) > 0.0f);
                        if (isLeftPartitionPositive && isRightPartitionPositive)
                        {
                            s_DragCache.normalizedPartitionSize += delta;
                            normalizedCascadePartitions[s_DragCache.activePartition] = s_DragCache.normalizedPartitionSize;
                            if (s_DragCache.activePartition < normalizedCascadePartitions.Length - 1)
                                normalizedCascadePartitions[s_DragCache.activePartition + 1] -= delta;
                            GUI.changed = true;
                        }
                    }
                    s_DragCache.lastCachedMousePosition = currentEvent.mousePosition;
                    currentEvent.Use();
                    break;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        public static void FillWithGradient(Texture2D tex, Color left, Color right)
        {
            if (tex == null || tex.height != 1 || tex.width < 1)
                throw new ArgumentException("The given texture must be initialized with only one pixel in height");

            int width = tex.width;
            Color[] colours = new Color[width];
            for (int i = 0; i < width; ++i)
                colours[i] = Color.Lerp(left, right, i / (float)(width - 1));
            tex.SetPixels(colours);
            tex.Apply();
        }

        public static void DrawCascadeSplitGUI(SerializedDataParameter[] splits, SerializedDataParameter[] borders, uint cascadeCount, bool blendLastCascade = false, bool useMetric = false, float baseMetric = 10f)
        {
            if (cascadeCount <= 0)
                throw new ArgumentException("Cascade amount must be strictly positive");

            uint splitCount = cascadeCount - 1;

            if (splitCount > splits.Length)
                throw new ArgumentException("Cannot use more splits than provided.");

            float[] cascadePartitionSizes = new float[splitCount]; //does not handle remaining (last partition)
            float[] cascadeEndBlendPercent = new float[blendLastCascade ? cascadeCount : splitCount];
            bool[] enabledPartitionHandles = new bool[splitCount];
            for (int i = 0; i < splitCount; ++i)
                enabledPartitionHandles[i] = splits[i].overrideState.boolValue;
            bool[] enabledEndPartitionHandles = new bool[cascadeEndBlendPercent.Length];
            for (int i = 0; i < cascadeEndBlendPercent.Length; ++i)
                enabledEndPartitionHandles[i] = borders == null || borders.Length <= i ? false : borders[i].overrideState.boolValue;

            if (splitCount > 0)
            {
                cascadePartitionSizes[0] = Mathf.Max(0f, splits[0].value.floatValue);
                cascadeEndBlendPercent[0] = borders == null || borders.Length <= 0 ? 0f : Mathf.Clamp01(borders[0].value.floatValue);
            }
            for (int index = 1; index < splitCount; ++index)
            {
                cascadePartitionSizes[index] = Mathf.Max(0f, splits[index].value.floatValue - splits[index - 1].value.floatValue);
                cascadeEndBlendPercent[index] = borders == null || borders.Length <= index ? 0f : Mathf.Clamp01(borders[index].value.floatValue);
            }
            if (blendLastCascade && borders != null && borders.Length > splitCount)
                cascadeEndBlendPercent[splitCount] = Mathf.Clamp01(borders[splitCount].value.floatValue);

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                HandleCascadeSliderGUI(ref cascadePartitionSizes, ref cascadeEndBlendPercent, enabledPartitionHandles, enabledEndPartitionHandles, drawEndPartitionHandles: borders != null, useMetric, baseMetric);
                if (EditorGUI.EndChangeCheck())
                {
                    if (splitCount > 0)
                    {
                        splits[0].value.floatValue = Mathf.Max(0f, cascadePartitionSizes[0]);
                        if (borders != null)
                            borders[0].value.floatValue = Mathf.Clamp01(cascadeEndBlendPercent[0]);
                    }
                    for (int index = 1; index < splitCount; ++index)
                    {
                        splits[index].value.floatValue = splits[index - 1].value.floatValue + Mathf.Max(0f, cascadePartitionSizes[index]);
                        if (borders != null)
                            borders[index].value.floatValue = Mathf.Clamp01(cascadeEndBlendPercent[index]);
                    }
                    if (blendLastCascade && borders != null)
                        borders[splitCount].value.floatValue = Mathf.Clamp01(cascadeEndBlendPercent[splitCount]);
                }
            }
        }
    }
}
