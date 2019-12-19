using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Reflection")]
    public class ScreenSpaceReflection : VolumeComponentWithQuality
    {
        public BoolParameter rayTracing = new BoolParameter(false);

        // Shared Data
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public BoolParameter reflectSky = new BoolParameter(true);

        // SSR Data
        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

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

        public int rayMaxIterations
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_RayMaxIterations.value;
                else
                    return GetLightingQualitySettings().SSRMaxRaySteps[(int)quality.value];
            }
            set { m_RayMaxIterations.value = value; }
        }

        [SerializeField, FormerlySerializedAs("rayMaxIterations")]
        private IntParameter m_RayMaxIterations = new IntParameter(32);
    }
}
