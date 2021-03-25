using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This defines which version of an effect should be used.
    /// </summary>
    public enum RayTracingMode
    {
        /// <summary>
        /// When selected, choices are made to reduce execution time of the effect.
        /// </summary>
        Performance = 1 << 0,
        /// <summary>
        /// When selected, choices are made to increase the visual quality of the effect.
        /// </summary>
        Quality = 1 << 1
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="RayTracingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class RayTracingModeParameter : VolumeParameter<RayTracingMode>
    {
        /// <summary>
        /// Creates a new <see cref="RayTracingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RayTracingModeParameter(RayTracingMode value, bool overrideState = false) : base(value, overrideState) {}
    }
}
