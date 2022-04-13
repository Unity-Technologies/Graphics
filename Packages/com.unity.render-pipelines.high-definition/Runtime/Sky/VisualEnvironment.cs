using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Visual Environment Volume Component.
    /// This component setups the sky used for rendering as well as the way ambient probe should be computed.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Visual Environment", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Visual-Environment")]
    public sealed class VisualEnvironment : VolumeComponent
    {
        /// <summary>Type of sky that should be used for rendering.</summary>
        [Header("Sky")]
        public NoInterpIntParameter skyType = new NoInterpIntParameter(0);
        /// <summary>Type of clouds that should be used for rendering.</summary>
        public NoInterpIntParameter cloudType = new NoInterpIntParameter(0);
        /// <summary>Defines the way the ambient probe should be computed.</summary>
        public SkyAmbientModeParameter skyAmbientMode = new SkyAmbientModeParameter(SkyAmbientMode.Dynamic);

        /// <summary>Controls the global orientation of the wind relative to the X world vector.</summary>
        [Header("Wind")]
        public ClampedFloatParameter windOrientation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Controls the global wind speed in kilometers per hour.</summary>
        public FloatParameter windSpeed = new FloatParameter(0.0f);

        // Deprecated, kept for migration
        [SerializeField]
        internal FogTypeParameter fogType = new FogTypeParameter(FogType.None);
    }

    /// <summary>
    /// Informative enumeration containing SkyUniqeIDs already used by HDRP.
    /// When users write their own sky type, they can use any ID not present in this enumeration or in their project.
    /// </summary>
    public enum SkyType
    {
        /// <summary>HDRI Sky Unique ID.</summary>
        HDRI = 1,
        /// <summary>Procedural Sky Unique ID.</summary>
        Procedural = 2,
        /// <summary>Gradient Sky Unique ID.</summary>
        Gradient = 3,
        /// <summary>Physically Based Sky Unique ID.</summary>
        PhysicallyBased = 4,
    }

    /// <summary>
    /// Informative enumeration containing CloudUniqeIDs already used by HDRP.
    /// When users write their own cloud type, they can use any ID not present in this enumeration or in their project.
    /// </summary>
    public enum CloudType
    {
        /// <summary>Cloud Layer Unique ID.</summary>
        CloudLayer = 1,
    }

    /// <summary>
    /// Sky Ambient Mode.
    /// </summary>
    public enum SkyAmbientMode
    {
        /// <summary>HDRP will use the static lighting sky setup in the lighting panel to compute the global ambient probe.</summary>
        Static,
        /// <summary>HDRP will use the current sky used for lighting (either the one setup in the Visual Environment component or the Sky Lighting Override) to compute the global ambient probe.</summary>
        Dynamic,
    }

    /// <summary>
    /// Sky Ambient Mode volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyAmbientModeParameter : VolumeParameter<SkyAmbientMode>
    {
        /// <summary>
        /// Sky Ambient Mode volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Ambient Mode parameter.</param>
        /// <param name="overrideState">Initial override value.</param>
        public SkyAmbientModeParameter(SkyAmbientMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Generic wind volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public abstract class WindParameter : VolumeParameter<WindParameter.WindParamaterValue>
    {
        /// <summary>Parameter override mode.</summary>
        public enum WindOverrideMode
        {
            /// <summary>Custom value.</summary>
            Custom,
            /// <summary>Use the value from the Visual Environment.</summary>
            Global,
            /// <summary>Add a custom amount to the value from the Visual Environment.</summary>
            Additive,
            /// <summary>Multiply the value from the Visual Environment by a custom factor.</summary>
            Multiply
        }

        /// <summary>Wind parameter value.</summary>
        [Serializable]
        public struct WindParamaterValue
        {
            /// <summary>Override mode.</summary>
            public WindOverrideMode mode;
            /// <summary>Value for the Custom mode.</summary>
            public float customValue;
            /// <summary>Value for the Additive mode.</summary>
            public float additiveValue;
            /// <summary>Value for the Multiply mode.</summary>
            public float multiplyValue;


            /// <summary>Returns a string that represents the current object.</summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                if (mode == WindOverrideMode.Global)
                    return mode.ToString();
                string str = null;
                if (mode == WindOverrideMode.Custom)
                    str = customValue.ToString();
                if (mode == WindOverrideMode.Additive)
                    str = additiveValue.ToString();
                if (mode == WindOverrideMode.Multiply)
                    str = multiplyValue.ToString();
                return str + " (" + mode.ToString() + ")";
            }
        }

        /// <summary>Wind volume parameter constructor.</summary>
        /// <param name="value">Initial value.</param>
        /// <param name="mode">Initial override mode.</param>
        /// <param name="overrideState">Initial override state.</param>
        public WindParameter(float value = 0.0f, WindOverrideMode mode = WindOverrideMode.Global, bool overrideState = false)
            : base(default, overrideState)
        {
            this.value = new WindParamaterValue
            {
                mode = mode,
                customValue = mode <= WindOverrideMode.Global ? value : 0.0f,
                additiveValue = mode == WindOverrideMode.Additive ? value : 0.0f,
                multiplyValue = mode == WindOverrideMode.Multiply ? value : 1.0f,
            };
        }

        /// <summary>Interpolates between two values.</summary>
        /// <param name="from">The start value</param>
        /// <param name="to">The end value</param>
        /// <param name="t">The interpolation factor in range [0,1]</param>
        public override void Interp(WindParamaterValue from, WindParamaterValue to, float t)
        {
            m_Value.mode = t > 0f ? to.mode : from.mode;
            m_Value.customValue = from.customValue + (to.customValue - from.customValue) * t;
            m_Value.additiveValue = from.additiveValue + (to.additiveValue - from.additiveValue) * t;
            m_Value.multiplyValue = from.multiplyValue + (to.multiplyValue - from.multiplyValue) * t;
        }

        /// <summary>Returns a hash code for the current object.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + value.mode.GetHashCode();
                hash = hash * 23 + value.customValue.GetHashCode();
                hash = hash * 23 + value.additiveValue.GetHashCode();
                hash = hash * 23 + value.multiplyValue.GetHashCode();

                return hash;
            }
        }

        /// <summary>Returns interpolated value from the visual environment.</summary>
        /// <param name="camera">The camera containing the volume stack to evaluate</param>
        /// <returns>The value for this parameter.</returns>
        public virtual float GetValue(HDCamera camera)
        {
            if (value.mode == WindOverrideMode.Custom)
                return value.customValue;
            float globalValue = GetGlobalValue(camera);
            if (value.mode == WindOverrideMode.Additive)
                return globalValue + value.additiveValue;
            if (value.mode == WindOverrideMode.Multiply)
                return globalValue * value.multiplyValue;
            return globalValue;
        }

        /// <summary>Returns the value stored in the volume.</summary>
        /// <param name="camera">The camera containing the volume stack to evaluate</param>
        /// <returns>The value for this parameter.</returns>
        protected abstract float GetGlobalValue(HDCamera camera);
    }

    /// <summary>
    /// Wind Orientation parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class WindOrientationParameter : WindParameter
    {
        /// <summary>
        /// Wind orientation volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Ambient Mode parameter.</param>
        /// <param name="mode">Initial override mode.</param>
        /// <param name="overrideState">Initial override value.</param>
        public WindOrientationParameter(float value = 0.0f, WindOverrideMode mode = WindOverrideMode.Global, bool overrideState = false)
            : base(value, mode, overrideState) { }

        /// <summary>Returns the value stored in the volume.</summary>
        /// <param name="camera">The camera containing the volume stack to evaluate</param>
        /// <returns>The value for this parameter.</returns>
        protected override float GetGlobalValue(HDCamera camera) =>
            camera.volumeStack.GetComponent<VisualEnvironment>().windOrientation.value;

        /// <summary>Interpolates between two values.</summary>
        /// <param name="from">The start value</param>
        /// <param name="to">The end value</param>
        /// <param name="t">The interpolation factor in range [0,1]</param>
        public override void Interp(WindParamaterValue from, WindParamaterValue to, float t)
        {
            // These are not used
            m_Value.multiplyValue = 0;

            // Binary state that cannot be blended
            m_Value.mode = t > 0f ? to.mode : from.mode;

            // Override the orientation specific values
            m_Value.additiveValue = from.additiveValue + (to.additiveValue - from.additiveValue) * t;
            m_Value.customValue = HDUtils.InterpolateOrientation(from.customValue, to.customValue, t);
        }

        /// <summary>Returns interpolated value from the visual environment.</summary>
        /// <param name="camera">The camera containing the volume stack to evaluate</param>
        /// <returns>The value for this parameter.</returns>
        public override float GetValue(HDCamera camera)
        {
            // Multiply mode is not supported for wind orientation
            if (value.mode == WindOverrideMode.Multiply)
                throw new NotSupportedException("Texture format not supported");

            // Otherwise we want the base behavior
            return base.GetValue(camera);
        }
    }

    /// <summary>
    /// Wind speed parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class WindSpeedParameter : WindParameter
    {
        /// <summary>
        /// Wind speed volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Ambient Mode parameter.</param>
        /// <param name="mode">Initial override mode.</param>
        /// <param name="overrideState">Initial override value.</param>
        public WindSpeedParameter(float value = 100.0f, WindOverrideMode mode = WindOverrideMode.Global, bool overrideState = false)
            : base(value, mode, overrideState) { }

        /// <summary>Returns the value stored in the volume.</summary>
        /// <param name="camera">The camera containing the volume stack to evaluate</param>
        /// <returns>The value for this parameter.</returns>
        protected override float GetGlobalValue(HDCamera camera) =>
            camera.volumeStack.GetComponent<VisualEnvironment>().windSpeed.value;
    }
}
