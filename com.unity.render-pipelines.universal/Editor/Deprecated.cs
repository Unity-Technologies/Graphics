using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.LWRP
{
    // This is to keep the namespace UnityEditor.Rendering.LWRP alive,
    // so that user scripts that have "using UnityEditor.Rendering.LWRP" in them still compile.
    internal class Deprecated
    {
    }
}

namespace UnityEditor.Rendering.Universal
{
    static partial class EditorUtils
    {
        [Obsolete("This is obsolete, please use DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit, float distance, int cascadeCount, Unit unit) instead.", false)]
        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;
            Type type = typeof(T);
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
    }
    static partial class ShadowCascadeSplitGUI
    {
        [Obsolete("This is obsolete, please use HandleCascadeSliderGUI(ref float[] normalizedCascadePartitions, float distance, EditorUtils.Unit unit) instead.", false)]
        public static void HandleCascadeSliderGUI(ref float[] normalizedCascadePartitions)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            // get the inspector width since we need it while drawing the partition rects.
            // Only way currently is to reserve the block in the layout using GetRect(), and then immediately drawing the empty box
            // to match the call to GetRect.
            // From this point on, we move to non-layout based code.

            var sliderRect = GUILayoutUtility.GetRect(GUIContent.none
                , s_CascadeSliderBG
                , GUILayout.Height(kSliderbarTopMargin + kSliderbarHeight + kSliderbarBottomMargin)
                , GUILayout.ExpandWidth(true));
            GUI.Box(sliderRect, GUIContent.none);

            EditorGUILayout.EndHorizontal();

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
#if UNITY_2019_1_OR_NEWER
                                s_OldSceneLightingMode = s_RestoreSceneView.sceneLighting;
#else
                                s_OldSceneLightingMode = s_RestoreSceneView.m_SceneLighting;
#endif
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
#if UNITY_2019_1_OR_NEWER
                        s_RestoreSceneView.sceneLighting = s_OldSceneLightingMode;
#else
                        s_RestoreSceneView.m_SceneLighting = s_OldSceneLightingMode;
#endif
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
