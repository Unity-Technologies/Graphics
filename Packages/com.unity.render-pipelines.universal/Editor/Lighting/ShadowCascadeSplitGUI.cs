using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal static class ShadowCascadeSplitGUI
    {
        const float k_SplitBias = 0.001f;

        internal static Vector4 SortSplits(Vector4 splits, int splitCount)
        {
            Debug.Assert(splitCount >= 0 && splitCount < 4);

            splits[splitCount] = 1f;
            for (int i = 0; i < splitCount; ++i)
                splits[i] = Mathf.Max(splits[i], i == 0 ? 0f : splits[i - 1] + k_SplitBias);
            for (int i = splitCount - 1; i >= 0; --i)
                splits[i] = Mathf.Min(splits[i], splits[i + 1] - k_SplitBias);

            return splits;
        }

        internal static float SplitScale(bool useMetric, float maxDistance)
        {
            return useMetric ? maxDistance : 100f;
        }

        internal static float SplitFromSliderValue(float scaledValue, Vector4 splits, int index, int splitCount, float scale)
        {
            float minimum = index == 0 ? 0f : splits[index - 1] + k_SplitBias;
            float maximum = index == splitCount - 1 ? 1f : splits[index + 1] - k_SplitBias;
            float invScale = scale == 0f ? 0f : 1f / scale;
            return Mathf.Clamp(scaledValue * invScale, minimum, maximum);
        }

        internal static float LastBorderScale(Vector4 splits, int splitCount, bool useMetric, float maxDistance)
        {
            if (!useMetric)
                return 100f;

            return splitCount == 0 ? maxDistance : (1f - splits[splitCount - 1]) * maxDistance;
        }

        internal static float LastBorderFromSliderValue(float scaledValue, float scale)
        {
            return scale == 0f ? 0f : scaledValue / scale;
        }

        internal static ShadowCascadeGUI.Cascade[] ToCascades(Vector4 splits, float border, int cascadeCount, bool[] splitHandlesEnabled, bool borderHandleEnabled)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            float previousSplit = 0f;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = splits[i] - previousSplit,
                    borderSize = 0f,
                    cascadeHandleState = splitHandlesEnabled[i] ? ShadowCascadeGUI.HandleState.Enabled : ShadowCascadeGUI.HandleState.Disabled,
                    borderHandleState = ShadowCascadeGUI.HandleState.Hidden,
                };
                previousSplit = splits[i];
            }

            cascades[cascadeCount - 1] = new ShadowCascadeGUI.Cascade()
            {
                size = 1f - previousSplit,
                borderSize = border,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = borderHandleEnabled ? ShadowCascadeGUI.HandleState.Enabled : ShadowCascadeGUI.HandleState.Disabled,
            };

            return cascades;
        }

        internal static void FromCascades(ShadowCascadeGUI.Cascade[] cascades, ref Vector4 splits, out float border)
        {
            int lastCascade = cascades.Length - 1;

            float split = 0f;
            for (int i = 0; i < lastCascade; ++i)
            {
                split += cascades[i].size;
                splits[i] = split;
            }
            border = cascades[lastCascade].borderSize;
        }

        internal static bool SplitSlider(Rect rect, GUIContent label, ref Vector4 splits, int index, int splitCount, bool useMetric, float maxDistance)
        {
            float scale = SplitScale(useMetric, maxDistance);

            EditorGUI.BeginChangeCheck();
            float scaledValue = EditorGUI.Slider(rect, label, splits[index] * scale, 0f, scale);
            if (!EditorGUI.EndChangeCheck())
                return false;

            splits[index] = SplitFromSliderValue(scaledValue, splits, index, splitCount, scale);
            return true;
        }

        internal static bool LastBorderSlider(Rect rect, GUIContent label, ref float border, Vector4 splits, int splitCount, bool useMetric, float maxDistance)
        {
            float scale = LastBorderScale(splits, splitCount, useMetric, maxDistance);

            EditorGUI.BeginChangeCheck();
            float scaledValue = EditorGUI.Slider(rect, label, border * scale, 0f, scale);
            if (!EditorGUI.EndChangeCheck())
                return false;

            border = LastBorderFromSliderValue(scaledValue, scale);
            return true;
        }

        internal static bool DrawCascades(ref Vector4 splits, ref float border, int cascadeCount, bool useMetric, float maxDistance, bool[] splitHandlesEnabled, bool borderHandleEnabled)
        {
            var cascades = ToCascades(splits, border, cascadeCount, splitHandlesEnabled, borderHandleEnabled);

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, maxDistance);
            if (!EditorGUI.EndChangeCheck())
                return false;

            FromCascades(cascades, ref splits, out border);
            return true;
        }
    }
}
