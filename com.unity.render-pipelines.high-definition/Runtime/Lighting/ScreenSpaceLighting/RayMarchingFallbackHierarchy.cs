using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This defines the order in which the fall backs are used if a screen space global illumination ray misses.
    /// </summary>
    [GenerateHLSL]
    public enum RayMarchingFallbackHierarchy
    {
        /// <summary>
        /// When selected, ray tracing will fall back on reflection probes (if any) then on the sky.
        /// </summary>
        [InspectorName("Reflection Probes and Sky")]
        ReflectionProbesAndSky = 0x03,
        /// <summary>
        /// When selected, ray tracing will fall back on reflection probes (if any).
        /// </summary>
        [InspectorName("Reflection Probes")]
        ReflectionProbes = 0x02,
        /// <summary>
        /// When selected, ray tracing will fall back on the sky.
        /// </summary>
        [InspectorName("Sky")]
        Sky = 0x01,
        /// <summary>
        /// When selected, ray tracing will return a black color.
        /// </summary>
        [InspectorName("None")]
        None = 0x00,
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds
    /// <see cref="RayMarchingFallbackHierarchy"/> value.
    /// </summary>
    [Serializable]
    public sealed class RayMarchingFallbackHierarchyParameter : VolumeParameter<RayMarchingFallbackHierarchy>
    {
        /// <summary>
        /// Creates a new <see cref="RayMarchingFallbackHierarchyParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RayMarchingFallbackHierarchyParameter(RayMarchingFallbackHierarchy value, bool overrideState = false) : base(value, overrideState) { }
    }
}
