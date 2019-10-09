using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection")]
    public class ScreenSpaceReflection : VolumeComponent
    {
        public BoolParameter rayTracing = new BoolParameter(false);

        // Shared Data
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public BoolParameter reflectSky = new BoolParameter(true);

        // SSR Data
        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        public IntParameter rayMaxIterations = new IntParameter(32);

        public LayerMaskParameter layerMask = new LayerMaskParameter(-1);
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0.001f, 50f);
        public ClampedFloatParameter clampValue = new ClampedFloatParameter(1.0f, 0.001f, 10.0f);
        public BoolParameter denoise = new BoolParameter(false);
        public ClampedIntParameter denoiserRadius = new ClampedIntParameter(16, 1, 32);

        // Tier 1 code
        public IntParameter upscaleRadius = new ClampedIntParameter(4, 2, 6);
        public BoolParameter fullResolution = new BoolParameter(false);
        public BoolParameter deferredMode = new BoolParameter(false);
        public BoolParameter rayBinning = new BoolParameter(false);

        // Tier 2 code
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 32);
        public ClampedIntParameter bounceCount = new ClampedIntParameter(1, 1, 31);
    }
}
