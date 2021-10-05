using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Formats the provided descriptor into a punctual light unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class HDPunctualLightUnitSlider : HDPiecewiseLightUnitSlider
    {
        public HDPunctualLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor) { }

        private SerializedHDLight m_Light;
        private Editor m_Editor;
        private LightUnit m_Unit;
        private bool m_SpotReflectorEnabled;

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

            // Cache the spot reflector state as we will need to revert back to it after treating the slider as point light.
            m_SpotReflectorEnabled = light.enableSpotReflector.boolValue;
        }

        public override void Draw(Rect rect, SerializedProperty value, ref float floatValue)
        {
            // Convert the incoming unit value into Lumen as the punctual slider is always in these terms (internally)
            float convertedValue = UnitToLumen(floatValue);

            EditorGUI.BeginChangeCheck();
            base.Draw(rect, value, ref convertedValue);
            if (EditorGUI.EndChangeCheck())
                floatValue = LumenToUnit(convertedValue);
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

            // Punctual slider currently does not have any regard for spot shape/reflector.
            // Conversions need to happen as if light is a point, and this is the only setting that influences that.
            m_Light.enableSpotReflector.boolValue = false;

            return HDLightUI.ConvertLightIntensity(m_Unit, LightUnit.Lumen, m_Light, m_Editor, value);
        }

        float LumenToUnit(float value)
        {
            if (m_Unit == LightUnit.Lumen)
                return value;

            // Once again temporarily disable reflector in case we called this for tooltip or context menu preset.
            m_Light.enableSpotReflector.boolValue = false;

            value = HDLightUI.ConvertLightIntensity(LightUnit.Lumen, m_Unit, m_Light, m_Editor, value);

            // Restore the state of spot reflector on the light.
            m_Light.enableSpotReflector.boolValue = m_SpotReflectorEnabled;

            return value;
        }

        protected override void SetValueToPreset(SerializedProperty value, LightUnitSliderUIRange preset)
        {
            m_Light?.Update();

            // Convert to the actual unit value.
            value.floatValue = LumenToUnit(preset.presetValue);

            m_Light?.Apply();
        }
    }
}
