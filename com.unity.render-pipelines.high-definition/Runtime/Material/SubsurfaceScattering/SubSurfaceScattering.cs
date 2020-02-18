using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Subsurface scattering volume component.
    /// This component setups subsurface scattering for ray-tracing.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/SubSurface Scattering (Preview)")]
    public sealed class SubSurfaceScattering : VolumeComponent
    {
        /// <summary>
        /// Enable ray traced sub-surface scattering.
        /// </summary>
        [Tooltip("Enable ray traced sub-surface scattering.")]
        public BoolParameter rayTracing = new BoolParameter(false);

        /// <summary>
        /// Number of samples for sub-surface scattering.
        /// </summary>
        [Tooltip("Number of samples for sub-surface scattering.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);
    }
}
