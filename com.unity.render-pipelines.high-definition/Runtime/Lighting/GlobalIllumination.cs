using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Global Illumination")]
    public sealed class GlobalIllumination : VolumeComponent
    {
        [Tooltip("Enable ray traced global illumination.")]
        public BoolParameter rayTracing = new BoolParameter(false);

        [Tooltip("Defines the layers that GI should include.")]
        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);

        [Tooltip("Controls the length of GI rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0.001f, 50f);

        [Tooltip("Controls the clamp of intensity.")]
        public ClampedFloatParameter clampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);

        // Tier 1
        [Tooltip("Enables deferred mode")]
        public BoolParameter deferredMode = new BoolParameter(false);
        
        [Tooltip("Enables ray binning")]
        public BoolParameter rayBinning = new BoolParameter(false);

        [Tooltip("Full Resolution")]
        public BoolParameter fullResolution = new BoolParameter(false);

        [Tooltip("Upscale Radius")]
        public ClampedIntParameter upscaleRadius = new ClampedIntParameter(2, 2, 4);

        // Tier 2
        [Tooltip("Number of samples for GI.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);

        [Tooltip("Number of bounces for GI.")]
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 31);

        // Filtering
        [Tooltip("Denoise the raytraced GI.")]
        public BoolParameter denoise = new BoolParameter(false);

        [Tooltip("Use a half resolution denoiser.")]
        public BoolParameter halfResolutionDenoiser = new BoolParameter(false);

        [Tooltip("Controls the radius of GI denoiser (First Pass).")]
        public ClampedFloatParameter denoiserRadius = new ClampedFloatParameter(0.6f, 0.001f, 1.0f);

        [Tooltip("Enable second denoising pass.")]
        public BoolParameter secondDenoiserPass = new BoolParameter(false);

        [Tooltip("Controls the radius of GI denoiser (Second Pass).")]
        public ClampedFloatParameter secondDenoiserRadius = new ClampedFloatParameter(0.3f, 0.001f, 0.5f);


    }
}
