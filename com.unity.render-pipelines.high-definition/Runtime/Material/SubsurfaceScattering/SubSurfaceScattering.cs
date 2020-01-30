using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/SubSurface Scattering (Preview)")]
    public sealed class SubSurfaceScattering : VolumeComponent
    {
        [Tooltip("Enable ray traced sub-surface scattering.")]
        public BoolParameter rayTracing = new BoolParameter(false);

        [Tooltip("Number of samples for sub-surface scattering.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);
    }
}
