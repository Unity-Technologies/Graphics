using UnityEngine.Rendering;
using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
    internal class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
    {
        // Public properties
        public BoolParameter Downsample = new BoolParameter(false, true);
        public DepthSourceSetting Source = new DepthSourceSetting(ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals, true);
        public NormalQualitySetting NormalSamples = new NormalQualitySetting(ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium, true);
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(3.0f, 0.0f, 10.0f, true);
        public ClampedFloatParameter DirectLightingStrength = new ClampedFloatParameter(0.5f, 0.0f, 1.0f, true);
        public FloatParameter Radius = new FloatParameter(0.035f, true);
        public ClampedIntParameter SampleCount = new ClampedIntParameter(4, 4, 20, true);

        // Classes
        [Serializable]
        public sealed class NormalQualitySetting : VolumeParameter<ScreenSpaceAmbientOcclusionSettings.NormalQuality>
        {
            public NormalQualitySetting(ScreenSpaceAmbientOcclusionSettings.NormalQuality value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }

        // This will be used once we've exposed render feature requirements.
        [Serializable]
        public sealed class DepthSourceSetting : VolumeParameter<ScreenSpaceAmbientOcclusionSettings.DepthSource>
        {
            public DepthSourceSetting(ScreenSpaceAmbientOcclusionSettings.DepthSource value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }
    }
}
