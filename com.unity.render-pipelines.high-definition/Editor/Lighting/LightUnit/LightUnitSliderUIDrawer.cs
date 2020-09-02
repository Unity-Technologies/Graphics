using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class LightUnitSliderUIDrawer
    {
        class LightUnitSlider
        {
            LightUnit m_Unit;
            LightUnitUILevel[] m_Levels;
            string m_CautionTooltip;
            Vector2 m_SliderValueRange;
            GUIContent s_MarkerContent;

            public LightUnitSlider(LightUnit unit, LightUnitUILevel[] levels, string cautionTooltip)
            {
                m_Unit = unit;
                m_Levels = levels;
                m_CautionTooltip = cautionTooltip;

                // Set slider range
                m_SliderValueRange = new Vector2(
                    m_Levels.Min(x => x.range.x),
                    m_Levels.Max(x => x.range.y)
                );

                s_MarkerContent = new GUIContent(string.Empty);
            }

            public void Draw(Rect rect, SerializedProperty value)
            {
                // Fetch the rects
                GetRects(rect, out var sliderRect, out var iconRect);

                // Slider
                DoSlider(sliderRect, value, m_SliderValueRange);

                // Markers
                foreach (var l in m_Levels)
                {
                    DoSliderMarker(sliderRect, l, m_SliderValueRange.y);
                }

                // Fetch the current level
                var level = CurrentLevel(value.floatValue);
                var levelIconContent = level.content;
                var levelRange = level.range;

                // Icon
                DoIcon(iconRect, levelIconContent, levelRange.y);

                // Place tooltip on slider thumb.
                DoThumbTooltip(sliderRect, value.floatValue, value.floatValue / m_SliderValueRange.y, levelIconContent.tooltip);
            }

            LightUnitUILevel CurrentLevel(float value)
            {
                foreach (var l in m_Levels)
                {
                    if (value >= l.range.x && value <= l.range.y)
                    {
                        return l;
                    }
                }

                // If value out of range, indicate caution. (For now assume caution feedback is last)
                return LightUnitUILevel.CautionLevel(m_CautionTooltip, value);
            }

            void DoSliderMarker(Rect rect, LightUnitUILevel level, float rangeMax)
            {
                const float width  = 4f;
                const float height = 2f;

                float normalizedValue = level.range.y / rangeMax;

                var markerRect = rect;
                markerRect.width  = width;
                markerRect.height = height;

                // Vertically align with slider.
                markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;

                // Horizontally place on slider.
                const float halfWidth = width * 0.5f;
                markerRect.x = rect.x + rect.width * normalizedValue;

                // Center the marker on value.
                markerRect.x -= halfWidth;

                // Clamp to the slider edges.
                float min = rect.x;
                float max = (rect.x + rect.width) - width;
                markerRect.x = Mathf.Clamp(markerRect.x, min, max);

                // Draw marker by manually drawing the rect, and an empty label with the tooltip.
                EditorGUI.DrawRect(markerRect, Color.white);

                s_MarkerContent.tooltip = FormatTooltip(m_Unit, level.content.tooltip, level.range.y);

                // Scale the marker tooltip for easier discovery
                const float markerTooltipRectScale = 4f;
                var markerTooltipRect = markerRect;
                markerTooltipRect.width  *= markerTooltipRectScale;
                markerTooltipRect.height *= markerTooltipRectScale;
                markerTooltipRect.x      -= (markerTooltipRect.width  * 0.5f) - 1;
                markerTooltipRect.y      -= (markerTooltipRect.height * 0.5f) - 1;
                EditorGUI.LabelField(markerTooltipRect, s_MarkerContent);
            }

            void DoThumbTooltip(Rect rect, float value, float normalizedValue, string tooltip)
            {
                const float size = 10f;
                const float halfSize = size * 0.5f;

                var thumbMarkerRect = rect;
                thumbMarkerRect.width  = size;
                thumbMarkerRect.height = size;

                // Vertically align with slider
                thumbMarkerRect.y += halfSize - 1f;

                // Horizontally place tooltip on the wheel,
                thumbMarkerRect.x  = rect.x + (rect.width - size) * normalizedValue;

                s_MarkerContent.tooltip = FormatTooltip(m_Unit, tooltip, value);
                EditorGUI.LabelField(thumbMarkerRect, s_MarkerContent);
            }

            void DoIcon(Rect rect, GUIContent icon, float range)
            {
                var oldColor = GUI.color;
                GUI.color = Color.clear;
                EditorGUI.DrawTextureTransparent(rect, icon.image);
                GUI.color = oldColor;

                EditorGUI.LabelField(rect, new GUIContent(string.Empty, FormatTooltip(m_Unit, icon.tooltip, range)));
            }
        }

        static readonly Dictionary<LightUnit, LightUnitSlider> s_LightUnitSliderMap = new Dictionary<LightUnit, LightUnitSlider>();

        static LightUnitSliderUIDrawer()
        {
            var luxSlider = new LightUnitSlider(LightUnit.Lux, LightUnitValuesTable.k_LuxValueTable, "Higher than Sunlight");
            s_LightUnitSliderMap.Add(LightUnit.Lux, luxSlider);

            var lumenSlider = new LightUnitSlider(LightUnit.Lumen, LightUnitValuesTable.k_LumenValueTable, "Very High Intensity Light");
            s_LightUnitSliderMap.Add(LightUnit.Lumen, lumenSlider);
        }

        public void OnGUI(LightUnit unit, SerializedProperty value)
        {
            OnGUI(unit, value, EditorGUILayout.GetControlRect());
        }

        public void OnGUI(LightUnit unit, SerializedProperty value, Rect rect)
        {
            if (!s_LightUnitSliderMap.TryGetValue(unit, out var lightUnitSlider))
                return;

            // Disable indentation (breaks tooltips otherwise).
            var prevIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Draw
            lightUnitSlider.Draw(rect, value);

            // Restore indentation
            EditorGUI.indentLevel = prevIndentLevel;
        }

        static void GetRects(Rect baseRect, out Rect sliderRect, out Rect iconRect)
        {
            const int k_IconSeparator = 6;

            sliderRect = baseRect;
            sliderRect.width -= EditorGUIUtility.singleLineHeight + k_IconSeparator;

            iconRect = baseRect;
            iconRect.x += sliderRect.width + k_IconSeparator;
            iconRect.width = EditorGUIUtility.singleLineHeight;
        }

        static void DoSlider(Rect rect, SerializedProperty value, Vector2 range)
        {
            // value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, range.x, range.y, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            value.floatValue = ExponentialSlider(rect, value.floatValue, range.x, 20000f, range.y);
        }

        // Note: Could use the internal PowerSlider, but that comes with a textfield, and we can't define the f(0.5).
        // ref: https://stackoverflow.com/a/17102320
        static float ExponentialSlider(Rect rect, float value, float lo, float mi, float hi)
        {
            float x = lo;
            float y = mi;
            float z = hi;

            // https://www.desmos.com/calculator/yx2yf4huia
            float a = ((x * z) - (y * y)) / (x - (2 * y) + z);
            float b = ((y - x) * (y - x)) / (x - (2 * y) + z);
            float c = 2 * Mathf.Log((z - y) / (y - x));

            float ValueToSlider(float x) => Mathf.Log((x - a) / b) / c;
            float SliderToValue(float x) => a + b * Mathf.Exp(c * x);

            float internalValue = GUI.HorizontalSlider(rect, ValueToSlider(value), 0f, 1f);

            return SliderToValue(internalValue);
        }

        static string FormatTooltip(LightUnit unit, string baseTooltip, float value)
        {
            string formatValue;

            // Massage the value for readability (with respect to the UX request).
            if (value >= Single.PositiveInfinity)
                formatValue = "###K";
            else if (value >= 100000)
                formatValue = (value / 1000).ToString("#,0K");
            else if (value >= 10000)
                formatValue = (value / 1000).ToString("0.#") + "K";
            else
                formatValue = value.ToString("#.0");

            return baseTooltip + " - " + formatValue + " " + unit;
        }
    }
}
