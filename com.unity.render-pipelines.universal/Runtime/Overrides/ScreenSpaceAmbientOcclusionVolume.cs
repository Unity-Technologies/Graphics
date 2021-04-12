using UnityEngine.Rendering;
using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
    internal class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
    {
        // Public properties
        public BoolParameter Downsample = new BoolParameter(false);
        public BoolParameter AfterOpaque = new BoolParameter(false);
        public DepthSourceSetting Source = new DepthSourceSetting(ScreenSpaceAmbientOcclusion.DepthSource.DepthNormals);
        public NormalQualitySetting NormalSamples = new NormalQualitySetting(ScreenSpaceAmbientOcclusion.NormalQuality.Medium);
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(3.0f, 0.0f, 10.0f);
        public ClampedFloatParameter DirectLightingStrength = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public FloatParameter Radius = new FloatParameter(0.035f);
        public ClampedIntParameter SampleCount = new ClampedIntParameter(4, 4, 20);

        // Classes
        [Serializable]
        public sealed class NormalQualitySetting : VolumeParameter<ScreenSpaceAmbientOcclusion.NormalQuality>
        {
            public NormalQualitySetting(ScreenSpaceAmbientOcclusion.NormalQuality value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }

        // This will be used once we've exposed render feature requirements.
        [Serializable]
        public sealed class DepthSourceSetting : VolumeParameter<ScreenSpaceAmbientOcclusion.DepthSource>
        {
            public DepthSourceSetting(ScreenSpaceAmbientOcclusion.DepthSource value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }
    }
}
