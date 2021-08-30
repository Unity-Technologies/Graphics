using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Formats the provided descriptor into a linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    public class LightUnitSlider
    {
        protected SerializedObject m_SerializedObject;

        static class SliderConfig
        {
            public const float k_IconSeparator = 0;
            public const float k_MarkerWidth = 2;
            public const float k_MarkerHeight = 2;
            public const float k_MarkerTooltipScale = 4;
            public const float k_ThumbTooltipSize = 10;
            public const float k_KnobSize = 10;
        }

        protected static class SliderStyles
        {
            public static GUIStyle k_IconButton = new GUIStyle("IconButton");
            public static GUIStyle k_TemperatureBorder = new GUIStyle("ColorPickerSliderBackground");
            public static GUIStyle k_TemperatureThumb = new GUIStyle("ColorPickerHorizThumb");
        }

        protected readonly LightUnitSliderUIDescriptor m_Descriptor;

        public LightUnitSlider(LightUnitSliderUIDescriptor descriptor)
        {
            m_Descriptor = descriptor;
        }

        public void SetSerializedObject(SerializedObject serialized)
        {
            m_SerializedObject = serialized;
        }

        public virtual void Draw(Rect rect, SerializedProperty value, ref float floatValue)
        {
            BuildRects(rect, out var sliderRect, out var iconRect);

            if (m_Descriptor.clampValue)
                ClampValue(ref floatValue, m_Descriptor.sliderRange);

            var level = CurrentRange(floatValue);

            DoSlider(sliderRect, ref floatValue, m_Descriptor.sliderRange, level.value);

            if (m_Descriptor.hasMarkers)
            {
                foreach (var r in m_Descriptor.valueRanges)
                {
                    var markerValue = r.value.y;
                    var markerPosition = GetPositionOnSlider(markerValue, r.value);
                    var markerTooltip = r.content.tooltip;
                    DoSliderMarker(sliderRect, markerPosition, markerValue, markerTooltip);
                }
            }

            var levelIconContent = level.content;
            var levelRange = level.value;
            DoIcon(iconRect, levelIconContent, value, floatValue, levelRange.y);

            var thumbValue = floatValue;
            var thumbPosition = GetPositionOnSlider(thumbValue, level.value);
            var thumbTooltip = levelIconContent.tooltip;
            DoThumbTooltip(sliderRect, thumbPosition, thumbValue, thumbTooltip);
        }

        LightUnitSliderUIRange CurrentRange(float value)
        {
            foreach (var l in m_Descriptor.valueRanges)
            {
                if (value >= l.value.x && value <= l.value.y)
                {
                    return l;
                }
            }

            var cautionValue = value < m_Descriptor.sliderRange.x ? m_Descriptor.sliderRange.x : m_Descriptor.sliderRange.y;
            var cautionTooltip = value < m_Descriptor.sliderRange.x ? m_Descriptor.belowRangeTooltip : m_Descriptor.aboveRangeTooltip;
            return LightUnitSliderUIRange.CautionRange(cautionTooltip, cautionValue);
        }

        void BuildRects(Rect baseRect, out Rect sliderRect, out Rect iconRect)
        {
            sliderRect = baseRect;
            sliderRect.width -= EditorGUIUtility.singleLineHeight + SliderConfig.k_IconSeparator;

            iconRect = baseRect;
            iconRect.x += sliderRect.width + SliderConfig.k_IconSeparator;
            iconRect.width = EditorGUIUtility.singleLineHeight;
        }

        void ClampValue(ref float value, Vector2 range) =>
            value = Mathf.Clamp(value, range.x, range.y);

        private static Color k_DarkThemeColor = new Color32(153, 153, 153, 255);
        private static Color k_LiteThemeColor = new Color32(97, 97, 97, 255);
        static Color GetMarkerColor() => EditorGUIUtility.isProSkin ? k_DarkThemeColor : k_LiteThemeColor;

        void DoSliderMarker(Rect rect, float position, float value, string tooltip)
        {
            const float width = SliderConfig.k_MarkerWidth;
            const float height = SliderConfig.k_MarkerHeight;

            var markerRect = rect;
            markerRect.width = width;
            markerRect.height = height;

            // Vertically align with slider.
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;

            // Horizontally place on slider. We need to take into account the "knob" size when doing this, because position 0 and 1 starts
            // at the center of the knob when it's placed at the left and right corner respectively. We don't do this adjustment when placing
            // the marker at the corners (to avoid havind the slider slightly extend past the marker)
            float knobSize = (position > 0f && position < 1f) ? SliderConfig.k_KnobSize : 0f;
            float start = rect.x + knobSize / 2f;
            float range = rect.width - knobSize;
            markerRect.x = start + range * position;

            // Center the marker on value.
            const float halfWidth = width * 0.5f;
            markerRect.x -= halfWidth;

            // Clamp to the slider edges.
            float min = rect.x;
            float max = (rect.x + rect.width) - width;
            markerRect.x = Mathf.Clamp(markerRect.x, min, max);

            // Draw marker by manually drawing the rect, and an empty label with the tooltip.
            EditorGUI.DrawRect(markerRect, GetMarkerColor());

            // Scale the marker tooltip for easier discovery
            const float markerTooltipRectScale = SliderConfig.k_MarkerTooltipScale;
            var markerTooltipRect = markerRect;
            markerTooltipRect.width *= markerTooltipRectScale;
            markerTooltipRect.height *= markerTooltipRectScale;
            markerTooltipRect.x -= (markerTooltipRect.width * 0.5f) - 1;
            markerTooltipRect.y -= (markerTooltipRect.height * 0.5f) - 1;
            EditorGUI.LabelField(markerTooltipRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        void DoIcon(Rect rect, GUIContent icon, SerializedProperty value, float floatValue, float range)
        {
            // Draw the context menu feedback before the icon
            GUI.Box(rect, GUIContent.none, SliderStyles.k_IconButton);

            var oldColor = GUI.color;
            GUI.color = Color.clear;
            EditorGUI.DrawTextureTransparent(rect, icon.image);
            GUI.color = oldColor;

            EditorGUI.LabelField(rect, GetLightUnitTooltip(icon.tooltip, range, m_Descriptor.unitName));

            // Handle events for context menu
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (rect.Contains(e.mousePosition))
                {
                    var menuPosition = rect.position + rect.size;
                    DoContextMenu(menuPosition, value, floatValue);
                    e.Use();
                }
            }
        }

        void DoContextMenu(Vector2 pos, SerializedProperty value, float floatValue)
        {
            var menu = new GenericMenu();

            foreach (var preset in m_Descriptor.valueRanges)
            {
                // Indicate a checkmark if the value is within this preset range.
                var isInPreset = CurrentRange(floatValue).value == preset.value;

                menu.AddItem(EditorGUIUtility.TrTextContent(preset.content.tooltip), isInPreset, () => SetValueToPreset(value, preset));
            }

            menu.DropDown(new Rect(pos, Vector2.zero));
        }

        void DoThumbTooltip(Rect rect, float position, float value, string tooltip)
        {
            const float size = SliderConfig.k_ThumbTooltipSize;
            const float halfSize = SliderConfig.k_ThumbTooltipSize * 0.5f;

            var thumbMarkerRect = rect;
            thumbMarkerRect.width = size;
            thumbMarkerRect.height = size;

            // Vertically align with slider
            thumbMarkerRect.y += halfSize - 1f;

            // Horizontally place tooltip on the wheel,
            thumbMarkerRect.x = rect.x + (rect.width - size) * position;

            EditorGUI.LabelField(thumbMarkerRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        protected virtual void SetValueToPreset(SerializedProperty value, LightUnitSliderUIRange preset)
        {
            m_SerializedObject?.Update();

            // Set the value to the average of the preset range.
            value.floatValue = preset.presetValue;

            m_SerializedObject?.ApplyModifiedProperties();
        }

        protected virtual GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            var formatValue = value < 100 ? $"{value:n}" : $"{value:n0}";
            var tooltip = $"{baseTooltip} | {formatValue} {unit}";
            return new GUIContent(string.Empty, tooltip);
        }

        protected virtual void DoSlider(Rect rect, ref float value, Vector2 sliderRange, Vector2 valueRange)
        {
            DoSlider(rect, ref value, sliderRange);
        }

        /// <summary>
        /// Draws a linear slider mapped to the min/max value range. Override this for different slider behavior (texture background, power).
        /// </summary>
        protected virtual void DoSlider(Rect rect, ref float value, Vector2 sliderRange)
        {
            value = GUI.HorizontalSlider(rect, value, sliderRange.x, sliderRange.y);
        }

        // Remaps value in the domain { Min0, Max0 } to { Min1, Max1 } (by default, normalizes it to (0, 1).
        static float Remap(float v, float x0, float y0, float x1 = 0f, float y1 = 1f) => x1 + (v - x0) * (y1 - x1) / (y0 - x0);

        protected virtual float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            return GetPositionOnSlider(value);
        }

        /// <summary>
        /// Maps a light unit value onto the slider. Keeps in sync placement of markers and tooltips with the slider power.
        /// Override this in case of non-linear slider.
        /// </summary>
        protected virtual float GetPositionOnSlider(float value)
        {
            return Remap(value, m_Descriptor.sliderRange.x, m_Descriptor.sliderRange.y);
        }
    }
}
