using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This defines which ray casting technique should be used.
    /// </summary>
    public enum RayCastingMode
    {
        /// <summary>
        /// When selected, ray marching is used to evaluate ray intersections.
        /// </summary>
        [InspectorName("Ray Marching")]
        RayMarching = 1 << 0,
        /// <summary>
        /// When selected, ray tracing is used to evaluate ray intersections.
        /// </summary>
        [InspectorName("Ray Tracing (Preview)")]
        RayTracing = 1 << 1,
        /// <summary>
        /// When selected, both ray marching and ray tracing are used to evaluate ray intersections.
        /// </summary>
        [InspectorName("Mixed (Preview)")]
        Mixed = 1 << 2,
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="RayCastingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class RayCastingModeParameter : VolumeParameter<RayCastingMode>
    {
        /// <summary>
        /// Creates a new <see cref="RayCastingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RayCastingModeParameter(RayCastingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
