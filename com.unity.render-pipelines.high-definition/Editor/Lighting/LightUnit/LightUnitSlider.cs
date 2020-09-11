using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Formats the provided descriptor into a linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class LightUnitSlider
    {
        static class SliderConfig
        {
            public const float k_IconSeparator      = 6;
            public const float k_MarkerWidth        = 4;
            public const float k_MarkerHeight       = 2;
            public const float k_MarkerTooltipScale = 4;
            public const float k_ThumbTooltipSize   = 10;
        }

        protected readonly LightUnitSliderUIDescriptor m_Descriptor;

        public LightUnitSlider(LightUnitSliderUIDescriptor descriptor)
        {
            m_Descriptor = descriptor;
        }

        public void Draw(Rect rect, SerializedProperty value)
        {
            BuildRects(rect, out var sliderRect, out var iconRect);

            DoSlider(sliderRect, value, m_Descriptor.sliderRange);

            if (m_Descriptor.hasMarkers)
            {
                foreach (var r in m_Descriptor.valueRanges)
                {
                    var markerValue = r.value.y;
                    var markerPosition = GetPositionOnSlider(markerValue);
                    var markerTooltip = r.content.tooltip;
                    DoSliderMarker(sliderRect, markerPosition, markerValue, markerTooltip);
                }
            }

            var level = CurrentRange(value.floatValue);
            var levelIconContent = level.content;
            var levelRange = level.value;
            DoIcon(iconRect, levelIconContent, levelRange.y);

            var thumbValue = value.floatValue;
            var thumbPosition = GetPositionOnSlider(thumbValue);
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

            return LightUnitSliderUIRange.CautionRange(m_Descriptor.cautionTooltip, value);
        }

        void BuildRects(Rect baseRect, out Rect sliderRect, out Rect iconRect)
        {
            sliderRect = baseRect;
            sliderRect.width -= EditorGUIUtility.singleLineHeight + SliderConfig.k_IconSeparator;

            iconRect = baseRect;
            iconRect.x += sliderRect.width + SliderConfig.k_IconSeparator;
            iconRect.width = EditorGUIUtility.singleLineHeight;
        }

        void DoSliderMarker(Rect rect, float position, float value, string tooltip)
        {
            const float width  = SliderConfig.k_MarkerWidth;
            const float height = SliderConfig.k_MarkerHeight;

            var markerRect = rect;
            markerRect.width  = width;
            markerRect.height = height;

            // Vertically align with slider.
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;

            // Horizontally place on slider.
            const float halfWidth = width * 0.5f;
            markerRect.x = rect.x + rect.width * position;

            // Center the marker on value.
            markerRect.x -= halfWidth;

            // Clamp to the slider edges.
            float min = rect.x;
            float max = (rect.x + rect.width) - width;
            markerRect.x = Mathf.Clamp(markerRect.x, min, max);

            // Draw marker by manually drawing the rect, and an empty label with the tooltip.
            EditorGUI.DrawRect(markerRect, Color.white);

            // Scale the marker tooltip for easier discovery
            const float markerTooltipRectScale = SliderConfig.k_MarkerTooltipScale;
            var markerTooltipRect = markerRect;
            markerTooltipRect.width  *= markerTooltipRectScale;
            markerTooltipRect.height *= markerTooltipRectScale;
            markerTooltipRect.x      -= (markerTooltipRect.width  * 0.5f) - 1;
            markerTooltipRect.y      -= (markerTooltipRect.height * 0.5f) - 1;
            EditorGUI.LabelField(markerTooltipRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        void DoIcon(Rect rect, GUIContent icon, float range)
        {
            var oldColor = GUI.color;
            GUI.color = Color.clear;
            EditorGUI.DrawTextureTransparent(rect, icon.image);
            GUI.color = oldColor;

            EditorGUI.LabelField(rect, GetLightUnitTooltip(icon.tooltip, range, m_Descriptor.unitName));
        }

        void DoThumbTooltip(Rect rect, float position, float value, string tooltip)
        {
            const float size = SliderConfig.k_ThumbTooltipSize;
            const float halfSize = SliderConfig.k_ThumbTooltipSize * 0.5f;

            var thumbMarkerRect = rect;
            thumbMarkerRect.width  = size;
            thumbMarkerRect.height = size;

            // Vertically align with slider
            thumbMarkerRect.y += halfSize - 1f;

            // Horizontally place tooltip on the wheel,
            thumbMarkerRect.x  = rect.x + (rect.width - size) * position;

            EditorGUI.LabelField(thumbMarkerRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        static GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            string formatValue;

            if (value >= 100000)
                formatValue = (value / 1000).ToString("#,0K");
            else if (value >= 10000)
                formatValue = (value / 1000).ToString("0.#") + "K";
            else
                formatValue = value.ToString("#.0");

            string tooltip = baseTooltip + " - " + formatValue + " " + unit;

            return new GUIContent(string.Empty, tooltip);
        }

        /// <summary>
        /// Draws a linear slider mapped to the min/max value range. Override this for different slider behavior (texture background, power).
        /// </summary>
        protected virtual void DoSlider(Rect rect, SerializedProperty value, Vector2 range)
        {
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, range.x, range.y);
        }

        // Remaps value in the domain { Min0, Max0 } to { Min1, Max1 } (by default, normalizes it to (0, 1).
        static float Remap(float v, float x0, float y0, float x1 = 0f, float y1 = 1f) => x1 + (v - x0) * (y1 - x1) / (y0 - x0);

        /// <summary>
        /// Maps a light unit value onto the slider. Keeps in sync placement of markers and tooltips with the slider power.
        /// Override this in case of non-linear slider.
        /// </summary>
        protected virtual float GetPositionOnSlider(float value)
        {
            return Remap(value, m_Descriptor.sliderRange.x, m_Descriptor.sliderRange.y);
        }
    }

    /// <summary>
    /// Formats the provided descriptor into an exponential slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class ExponentialLightUnitSlider : LightUnitSlider
    {
        private Vector3 m_ExponentialConstraints;

        /// <summary>
        /// Exponential slider modeled to set a f(0.5) value.
        /// ref: https://stackoverflow.com/a/17102320
        /// </summary>
        void PrepareExponentialConstraints(float lo, float mi, float hi)
        {
            float x = lo;
            float y = mi;
            float z = hi;

            // https://www.desmos.com/calculator/yx2yf4huia
            m_ExponentialConstraints.x = ((x * z) - (y * y)) / (x - (2 * y) + z);
            m_ExponentialConstraints.y = ((y - x) * (y - x)) / (x - (2 * y) + z);
            m_ExponentialConstraints.z = 2 * Mathf.Log((z - y) / (y - x));
        }

        float ValueToSlider(float x) => Mathf.Log((x - m_ExponentialConstraints.x) / m_ExponentialConstraints.y) / m_ExponentialConstraints.z;
        float SliderToValue(float x) => m_ExponentialConstraints.x + m_ExponentialConstraints.y * Mathf.Exp(m_ExponentialConstraints.z * x);

        public ExponentialLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            var halfValue = 300; // TODO: Compute the median
            PrepareExponentialConstraints(m_Descriptor.sliderRange.x, halfValue, m_Descriptor.sliderRange.y);
        }

        protected override float GetPositionOnSlider(float value)
        {
            return ValueToSlider(value);
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 range)
        {
            value.floatValue = ExponentialSlider(rect, value.floatValue);
        }

        float ExponentialSlider(Rect rect, float value)
        {
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(value), 0f, 1f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);

            return SliderToValue(internalValue);
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a temperature unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class TemperatureSlider : LightUnitSlider
    {
        private LightEditor.Settings m_Settings;

        private static Texture2D s_KelvinGradientTexture;

        static Texture2D GetKelvinGradientTexture(LightEditor.Settings settings)
        {
            if (s_KelvinGradientTexture == null)
            {
                var kelvinTexture = (Texture2D)typeof(LightEditor.Settings).GetField("m_KelvinGradientTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(settings);

                // This seems to be the only way to gamma-correct the internal gradient tex (aside from drawing it manually).
                var kelvinTextureLinear = new Texture2D(kelvinTexture.width, kelvinTexture.height, TextureFormat.RGBA32, true);
                kelvinTextureLinear.SetPixels(kelvinTexture.GetPixels());
                kelvinTextureLinear.Apply();

                s_KelvinGradientTexture = kelvinTextureLinear;
            }

            return s_KelvinGradientTexture;
        }

        public TemperatureSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor) {}

        public void SetLightSettings(LightEditor.Settings settings)
        {
            m_Settings = settings;
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 range)
        {
            SliderWithTextureNoTextField(rect, value, range, m_Settings);
        }

        // Note: We could use the internal SliderWithTexture, however: the internal slider func forces a text-field (and no ability to opt-out of it).
        void SliderWithTextureNoTextField(Rect rect, SerializedProperty value, Vector2 range, LightEditor.Settings settings)
        {
            GUI.DrawTexture(rect, GetKelvinGradientTexture(settings));

            var sliderBorder = new GUIStyle("ColorPickerSliderBackground");
            var sliderThumb = new GUIStyle("ColorPickerHorizThumb");
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, range.x, range.y, sliderBorder, sliderThumb);
        }
    }

    internal class LightUnitSliderUIDrawer
    {
        static Dictionary<LightUnit, LightUnitSlider> k_LightUnitSliderMap;
        static LightUnitSlider k_ExposureSlider;
        static TemperatureSlider k_TemperatureSlider;

        static LightUnitSliderUIDrawer()
        {
            k_LightUnitSliderMap = new Dictionary<LightUnit, LightUnitSlider>
            {
                { LightUnit.Lux,     new ExponentialLightUnitSlider(LightUnitSliderDescriptors.LuxDescriptor)     },
                { LightUnit.Lumen,   new ExponentialLightUnitSlider(LightUnitSliderDescriptors.LumenDescriptor)   },
                { LightUnit.Candela, new ExponentialLightUnitSlider(LightUnitSliderDescriptors.CandelaDescriptor) },
                { LightUnit.Ev100,   new ExponentialLightUnitSlider(LightUnitSliderDescriptors.EV100Descriptor)   },
                { LightUnit.Nits,    new ExponentialLightUnitSlider(LightUnitSliderDescriptors.NitsDescriptor)    },
            };

            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new LightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);

            // Kelvin is not classified internally as a light unit so we handle it independently as well.
            k_TemperatureSlider = new TemperatureSlider(LightUnitSliderDescriptors.TemperatureDescriptor);
        }

        public void Draw(LightUnit unit, SerializedProperty value, Rect rect)
        {
            if (!k_LightUnitSliderMap.TryGetValue(unit, out var lightUnitSlider))
                return;

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                lightUnitSlider.Draw(rect, value);
            }
        }

        public void DrawExposureSlider(SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                k_ExposureSlider.Draw(rect, value);
            }
        }

        public void DrawTemperatureSlider(LightEditor.Settings settings, SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                k_TemperatureSlider.SetLightSettings(settings);
                k_TemperatureSlider.Draw(rect, value);
            }
        }
    }
}
