﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            public const float k_MarkerWidth        = 2;
            public const float k_MarkerHeight       = 2;
            public const float k_MarkerTooltipScale = 4;
            public const float k_ThumbTooltipSize   = 10;
        }

        protected readonly LightUnitSliderUIDescriptor m_Descriptor;

        public LightUnitSlider(LightUnitSliderUIDescriptor descriptor)
        {
            m_Descriptor = descriptor;
        }

        public virtual void Draw(Rect rect, SerializedProperty value)
        {
            BuildRects(rect, out var sliderRect, out var iconRect);

            if (m_Descriptor.clampValue)
                ClampValue(value, m_Descriptor.sliderRange);

            var level = CurrentRange(value.floatValue);

            DoSlider(sliderRect, value, m_Descriptor.sliderRange, level.value);

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
            DoIcon(iconRect, levelIconContent, levelRange.y);

            var thumbValue = value.floatValue;
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

            var cautionValue = m_Descriptor.sliderRange.y;
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

        void ClampValue(SerializedProperty value, Vector2 range) =>
            value.floatValue = Mathf.Clamp(value.floatValue, range.x, range.y);

        private static Color k_DarkThemeColor = new Color32(153, 153, 153, 255);
        private static Color k_LiteThemeColor = new Color32(97, 97, 97, 255);
        static Color GetMarkerColor() => EditorGUIUtility.isProSkin ? k_DarkThemeColor : k_LiteThemeColor;

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
            EditorGUI.DrawRect(markerRect, GetMarkerColor());

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

        protected virtual GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            var formatValue = value < 100 ? $"{value:n}" : $"{value:n0}";
            var tooltip = $"{baseTooltip} | {formatValue} {unit}";
            return new GUIContent(string.Empty, tooltip);
        }

        protected virtual void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange, Vector2 valueRange)
        {
            DoSlider(rect, value, sliderRange);
        }

        /// <summary>
        /// Draws a linear slider mapped to the min/max value range. Override this for different slider behavior (texture background, power).
        /// </summary>
        protected virtual void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, sliderRange.x, sliderRange.y);
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

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            value.floatValue = ExponentialSlider(rect, value.floatValue);
        }

        float ExponentialSlider(Rect rect, float value)
        {
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(value), 0f, 1f);
            return SliderToValue(internalValue);
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a piece-wise linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class PiecewiseLightUnitSlider : LightUnitSlider
    {
        struct Piece
        {
            public Vector2 domain;
            public Vector2 range;

            public float directM;
            public float directB;
            public float inverseM;
            public float inverseB;
        }

        // Piecewise function indexed by value ranges.
        private readonly Dictionary<Vector2, Piece> m_PiecewiseFunctionMap = new Dictionary<Vector2, Piece>();

        static void ComputeTransformationParameters(float x0, float x1, float y0, float y1, out float m, out float b)
        {
            m = (y0 - y1) / (x0 - x1);
            b = (m * -x0) + y0;
        }

        static float DoTransformation(in float x, in float m, in float b) => (m * x) + b;

        // Ensure clamping to (0,1) as sometimes the function evaluates to slightly below 0 (breaking the handle).
        static float ValueToSlider(Piece p, float x) => Mathf.Clamp01(DoTransformation(x, p.inverseM, p.inverseB));
        static float SliderToValue(Piece p, float x) => DoTransformation(x, p.directM, p.directB);

        // Ideally we want a continuous, monotonically increasing function, but this is useful as we can easily fit a
        // distribution to a set of (huge) value ranges onto a slider.
        public PiecewiseLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            // Sort the ranges into ascending order
            var sortedRanges = m_Descriptor.valueRanges.OrderBy(x => x.value.x).ToArray();
            var sliderDistribution = m_Descriptor.sliderDistribution;

            // Compute the transformation for each value range.
            for (int i = 0; i < sortedRanges.Length; i++)
            {
                var r = sortedRanges[i].value;

                var x0 = sliderDistribution[i + 0];
                var x1 = sliderDistribution[i + 1];
                var y0 = r.x;
                var y1 = r.y;

                Piece piece;
                piece.domain = new Vector2(x0, x1);
                piece.range  = new Vector2(y0, y1);

                ComputeTransformationParameters(x0, x1, y0, y1, out piece.directM, out piece.directB);

                // Compute the inverse
                ComputeTransformationParameters(y0, y1, x0, x1, out piece.inverseM, out piece.inverseB);

                m_PiecewiseFunctionMap.Add(sortedRanges[i].value, piece);
            }
        }

        protected override float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piecewise))
                return -1f;

            return ValueToSlider(piecewise, value);
        }

        // Search for the corresponding piece-wise function to a value on the domain and update the input piece to it.
        // Returns true if search was successful and an update was made, false otherwise.
        bool UpdatePiece(ref Piece piece, float x)
        {
            foreach (var pair in m_PiecewiseFunctionMap)
            {
                var p = pair.Value;

                if (x >= p.domain.x && x <= p.domain.y)
                {
                    piece = p;

                    return true;
                }
            }

            return false;
        }

        void SliderOutOfBounds(Rect rect, SerializedProperty value)
        {
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, value.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Piece p = new Piece();
                UpdatePiece(ref p, internalValue);
                value.floatValue = SliderToValue(p, internalValue);
            }
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange, Vector2 valueRange)
        {
            // Map the internal slider value to the current piecewise function
            if (!m_PiecewiseFunctionMap.TryGetValue(valueRange, out var piece))
            {
                // Assume that if the piece is not found, that means the unit value is out of bounds.
                SliderOutOfBounds(rect, value);
                return;
            }

            // Maintain an internal value to support a single linear continuous function
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(piece, value.floatValue), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure that the current function piece is being used to transform the value
                UpdatePiece(ref piece, internalValue);
                value.floatValue = SliderToValue(piece, internalValue);
            }
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a punctual light unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class PunctualLightUnitSlider : PiecewiseLightUnitSlider
    {
        public PunctualLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor) {}

        private SerializedHDLight m_Light;
        private Editor m_Editor;
        private LightUnit m_Unit;

        // Note: these should be in sync with LightUnit
        private static string[] k_UnitNames =
        {
            "Lumen",
            "Candela",
            "Lux",
            "Nits",
            "EV",
        };

        public void Setup(LightUnit unit, SerializedHDLight light, Editor owner)
        {
            m_Unit = unit;
            m_Light = light;
            m_Editor = owner;
        }

        public override void Draw(Rect rect, SerializedProperty value)
        {
            // Convert the incoming unit value into Lumen as the punctual slider is always in these terms (internally)
            value.floatValue = UnitToLumen(value.floatValue);

            base.Draw(rect, value);

            value.floatValue = LumenToUnit(value.floatValue);
        }

        protected override GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            // Convert the internal lumens into the actual light unit value
            value = LumenToUnit(value);
            unit = k_UnitNames[(int)m_Unit];

            return base.GetLightUnitTooltip(baseTooltip, value, unit);
        }

        float UnitToLumen(float value)
        {
            if (m_Unit == LightUnit.Lumen)
                return value;

            return HDLightUI.ConvertLightIntensity(m_Unit, LightUnit.Lumen, m_Light, m_Editor, value);
        }

        float LumenToUnit(float value)
        {
            if (m_Unit == LightUnit.Lumen)
                return value;

            if (m_Light.type == HDLightType.Spot &&
                m_Unit != LightUnit.Lumen &&
                m_Light.enableSpotReflector.boolValue)
            {
                // Note: When reflector is flagged, the util conversion for Lumen -> Candela will actually force re-use
                // the serialized intensity, which is not what we want in this case. Because of this, we re-use the formulation
                // in HDAdditionalLightData to correctly compute the intensity for spot reflector.
                return ConvertLightIntensitySpotAngleLumenToUnit(value);
            }

            return HDLightUI.ConvertLightIntensity(LightUnit.Lumen, m_Unit, m_Light, m_Editor, value);
        }

        // This code is re-used from HDAdditionalLightData
        float ConvertLightIntensitySpotAngleLumenToUnit(float value)
        {
            var spotLightShape = m_Light.spotLightShape.GetEnumValue<SpotLightShape>();
            var spotAngle = m_Light.settings.spotAngle.floatValue;
            var aspectRatio = m_Light.aspectRatio.floatValue;

            // If reflector is enabled all the lighting from the sphere is focus inside the solid angle of current shape
            if (spotLightShape == SpotLightShape.Cone)
            {
                value = LightUtils.ConvertSpotLightLumenToCandela(value, spotAngle * Mathf.Deg2Rad, true);
            }
            else if (spotLightShape == SpotLightShape.Pyramid)
            {
                float angleA, angleB;
                LightUtils.CalculateAnglesForPyramid(aspectRatio, spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                value = LightUtils.ConvertFrustrumLightLumenToCandela(value, angleA, angleB);
            }
            else // Box shape, fallback to punctual light.
            {
                value = LightUtils.ConvertPointLightLumenToCandela(value);
            }

            // Units so far are in Candela now, last step is to get it in terms of the actual unit.
            return HDLightUI.ConvertLightIntensity(LightUnit.Candela, m_Unit, m_Light, m_Editor, value);
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

        public void Setup(LightEditor.Settings settings)
        {
            m_Settings = settings;
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            SliderWithTextureNoTextField(rect, value, sliderRange, m_Settings);
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
        static PiecewiseLightUnitSlider k_DirectionalLightUnitSlider;
        static PunctualLightUnitSlider  k_PunctualLightUnitSlider;
        static PiecewiseLightUnitSlider k_ExposureSlider;
        static TemperatureSlider        k_TemperatureSlider;

        static LightUnitSliderUIDrawer()
        {
            // Maintain a unique slider for directional/lux.
            k_DirectionalLightUnitSlider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.LuxDescriptor);

            // Internally, slider is always in terms of lumens, so that the slider is uniform for all light units.
            k_PunctualLightUnitSlider = new PunctualLightUnitSlider(LightUnitSliderDescriptors.LumenDescriptor);

            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);

            // Kelvin is not classified internally as a light unit so we handle it independently as well.
            k_TemperatureSlider = new TemperatureSlider(LightUnitSliderDescriptors.TemperatureDescriptor);
        }

        public void Draw(HDLightType type, LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                if (type == HDLightType.Directional)
                    DrawDirectionalUnitSlider(value, rect);
                else
                    DrawPunctualLightUnitSlider(lightUnit, value, rect, light, owner);
            }
        }

        void DrawDirectionalUnitSlider(SerializedProperty value, Rect rect)
        {
            k_DirectionalLightUnitSlider.Draw(rect, value);
        }

        void DrawPunctualLightUnitSlider(LightUnit lightUnit, SerializedProperty value, Rect rect, SerializedHDLight light, Editor owner)
        {
            k_PunctualLightUnitSlider.Setup(lightUnit, light, owner);
            k_PunctualLightUnitSlider.Draw(rect, value);
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
                k_TemperatureSlider.Setup(settings);
                k_TemperatureSlider.Draw(rect, value);
            }
        }
    }
}
