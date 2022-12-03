using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Formats the provided descriptor into a temperature unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class TemperatureSlider : LightUnitSlider
    {
        private Vector3 m_ExponentialConstraints;

        private LightEditor.Settings m_Settings;

        private static Texture2D s_KelvinGradientTexture;

        /// <summary>
        /// Exponential slider modeled to set a f(0.5) value.
        /// ref: https://stackoverflow.com/a/17102320
        /// </summary>
        void PrepareExponentialConstraints(float lo, float mi, float hi)
        {
            // float x = lo;
            // float y = mi;
            // float z = hi;
            //
            // // https://www.desmos.com/calculator/yx2yf4huia
            // m_ExponentialConstraints.x = ((x * z) - (y * y)) / (x - (2 * y) + z);
            // m_ExponentialConstraints.y = ((y - x) * (y - x)) / (x - (2 * y) + z);
            // m_ExponentialConstraints.z = 2 * Mathf.Log((z - y) / (y - x));

            // Warning: These are the coefficients for a system of equation fit for a continuous, monotonic curve that fits a f(0.44) value.
            // f(0.44) is required instead of f(0.5) due to the location of the white in the temperature gradient texture.
            // The equation is solved to get the coefficient for the following constraint for low, mid, hi:
            // f(0)    = 1500
            // f(0.44) = 6500
            // f(1.0)  = 20000
            // If for any reason the constraints are changed, then the function must be refit and the new coefficients found.
            // Note that we can't re-use the original PowerSlider instead due to how it forces a text field, which we don't want in this case.
            m_ExponentialConstraints.x = -3935.53965427f;
            m_ExponentialConstraints.y = 5435.53965427f;
            m_ExponentialConstraints.z = 1.48240556f;
        }

        protected float ValueToSlider(float x) => Mathf.Log((x - m_ExponentialConstraints.x) / m_ExponentialConstraints.y) / m_ExponentialConstraints.z;
        protected float SliderToValue(float x) => m_ExponentialConstraints.x + m_ExponentialConstraints.y * Mathf.Exp(m_ExponentialConstraints.z * x);

        protected override float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            return ValueToSlider(value);
        }

        static Texture2D GetKelvinGradientTexture(LightEditor.Settings settings)
        {
            if (s_KelvinGradientTexture == null)
            {
                var kelvinTexture = (Texture2D)typeof(LightEditor.Settings).GetField("m_KelvinGradientTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(settings);

                // This seems to be the only way to gamma-correct the internal gradient tex (aside from drawing it manually).
                var kelvinTextureLinear = new Texture2D(kelvinTexture.width, kelvinTexture.height, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.MipChain);
                kelvinTextureLinear.SetPixels(kelvinTexture.GetPixels());
                kelvinTextureLinear.Apply();

                s_KelvinGradientTexture = kelvinTextureLinear;
            }

            return s_KelvinGradientTexture;
        }

        /// <summary>
        /// Constructs the temperature slider
        /// </summary>
        /// <param name="descriptor">The descriptor</param>
        public TemperatureSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            var halfValue = 6500;
            PrepareExponentialConstraints(m_Descriptor.sliderRange.x, halfValue, m_Descriptor.sliderRange.y);
        }

        /// <summary>
        /// Setups the light editor
        /// </summary>
        /// <param name="settings">The light editor from the light</param>
        public void Setup(LightEditor.Settings settings)
        {
            m_Settings = settings;
        }

        /// <summary>
        /// The serialized property for color temperature is stored in the build-in light editor, and we need to use this object to apply the update.
        /// </summary>
        /// <param name="value">The value to update</param>
        /// <param name="preset">The preset range</param>
        protected override void SetValueToPreset(SerializedProperty value, LightUnitSliderUIRange preset)
        {
            m_Settings.Update();

            base.SetValueToPreset(value, preset);

            m_Settings.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the slider
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/> to draw the slider.</param>
        /// <param name="value">The current value, and also returns the modified value.</param>
        /// <param name="sliderRange">The ranges of the slider.</param>
        protected override void DoSlider(Rect rect, ref float value, Vector2 sliderRange)
        {
            SliderWithTextureNoTextField(rect, ref value, sliderRange, m_Settings);
        }

        // Note: We could use the internal SliderWithTexture, however: the internal slider func forces a text-field (and no ability to opt-out of it).
        void SliderWithTextureNoTextField(Rect rect, ref float value, Vector2 range, LightEditor.Settings settings)
        {
            GUI.DrawTexture(rect, GetKelvinGradientTexture(settings));

            EditorGUI.BeginChangeCheck();

            // Draw the exponential slider that fits 6500K to the white point on the gradient texture.
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(value), 0f, 1f, SliderStyles.k_TemperatureBorder, SliderStyles.k_TemperatureThumb);

            // Round to nearest since so much precision is not necessary for kelvin while sliding.
            if (EditorGUI.EndChangeCheck())
            {
                // Map the value back into kelvin.
                value = SliderToValue(internalValue);

                value = Mathf.Round(value);
            }
        }
    }

    /// <summary>
    /// Helper to draw a temperature slider on the inspector
    /// </summary>
    public class TemperatureSliderUIDrawer
    {
        static TemperatureSlider k_TemperatureSlider;

        static TemperatureSliderUIDrawer()
        {
            // Kelvin is not classified internally as a light unit so we handle it independently as well.
            k_TemperatureSlider = new TemperatureSlider(LightUnitSliderDescriptors.TemperatureDescriptor);
        }

        /// <summary>
        /// Draws a temperature slider
        /// </summary>
        /// <param name="settings">The light settings</param>
        /// <param name="serializedObject">The serialized object</param>
        /// <param name="value">The serialized property</param>
        /// <param name="rect">The rect where the slider will be drawn</param>
        public static void Draw(LightEditor.Settings settings, SerializedObject serializedObject, SerializedProperty value, Rect rect)
        {
            k_TemperatureSlider.SetSerializedObject(serializedObject);
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                k_TemperatureSlider.Setup(settings);

                float val = value.floatValue;
                k_TemperatureSlider.Draw(rect, value, ref val);
                if (val != value.floatValue)
                    value.floatValue = val;
            }
        }

        /// <summary>
        /// Clamp to the authorized range of the temperature slider
        /// </summary>
        /// <param name="value">The serialized property</param>
        public static void ClampValue(SerializedProperty value)
        {
            value.floatValue = k_TemperatureSlider.ClampValue(value.floatValue);
        }
    }
}
