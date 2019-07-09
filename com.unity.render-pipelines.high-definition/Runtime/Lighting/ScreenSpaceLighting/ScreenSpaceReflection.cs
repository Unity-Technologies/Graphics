using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection")]
    public class ScreenSpaceReflection : VolumeComponent
    {
        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public BoolParameter         reflectSky          = new BoolParameter(true);

        public IntParameter rayMaxIterations = new IntParameter(32);

        [Tooltip("Enable ray traced reflections")]
        public BoolParameter enableRaytracing = new BoolParameter(false);

        [Tooltip("Controls the length of reflection rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0.001f, 50f);

        [Tooltip("Controls the clamp of intensity.")]
        public ClampedFloatParameter clampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        [Tooltip("Enable Filtering on the ray traced reflections.")]
        public BoolParameter enableFilter = new BoolParameter(false);

        [Tooltip("Controls the size of the filter radius.")]
        public ClampedIntParameter filterRadius = new ClampedIntParameter(16, 1, 32);

        // Tier 1 code
        [Tooltip("Controls the size of the upscale radius")]
        public IntParameter spatialFilterRadius = new ClampedIntParameter(4, 2, 6);

        [Tooltip("Enables full resolution mode")]
        public BoolParameter fullResolution = new BoolParameter(false);

        [Tooltip("Enables deferred mode")]
        public BoolParameter deferredMode = new BoolParameter(false);

        [Tooltip("Enables ray binning")]
        public BoolParameter rayBinning = new BoolParameter(false);

        // Tier 2 code
        [Tooltip("Number of samples for reflections.")]
        public ClampedIntParameter numSamples = new ClampedIntParameter(1, 1, 32);

        [Tooltip("Number of bounces for reflections.")]
        public ClampedIntParameter numBounces = new ClampedIntParameter(1, 1, 31);
    }
}
