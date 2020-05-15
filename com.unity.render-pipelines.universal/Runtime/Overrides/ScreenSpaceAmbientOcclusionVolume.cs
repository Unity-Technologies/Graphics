using UnityEngine.Rendering;
using System;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
    class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
    {
        // Public properties
        //public DepthSourceSetting DepthSource = new DepthSourceSetting(ScreenSpaceAmbientOcclusionFeature.DepthSource.Depth);
        public QualitySetting Quality = new QualitySetting(ScreenSpaceAmbientOcclusion.k_QualityDefault, true);
        public NormalSamplesSetting NormalSamples = new NormalSamplesSetting(ScreenSpaceAmbientOcclusion.k_NormalSamplesDefault);
        public BoolParameter Downsample = new BoolParameter(ScreenSpaceAmbientOcclusion.k_DownsampleDefault);
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(ScreenSpaceAmbientOcclusion.k_IntensityDefault, ScreenSpaceAmbientOcclusion.k_IntensityMin, ScreenSpaceAmbientOcclusion.k_IntensityMax);
        public ClampedFloatParameter Radius = new ClampedFloatParameter(ScreenSpaceAmbientOcclusion.k_RadiusDefault, ScreenSpaceAmbientOcclusion.k_RadiusMin, ScreenSpaceAmbientOcclusion.k_RadiusMax, false);
        public ClampedIntParameter SampleCount = new ClampedIntParameter(ScreenSpaceAmbientOcclusion.k_SampleCountDefault, ScreenSpaceAmbientOcclusion.k_SampleCountMin, ScreenSpaceAmbientOcclusion.k_SampleCountMax, false);
        public ClampedIntParameter BlurPasses = new ClampedIntParameter(ScreenSpaceAmbientOcclusion.k_BlurPassesDefault, ScreenSpaceAmbientOcclusion.k_BlurPassesMin, ScreenSpaceAmbientOcclusion.k_BlurPassesMax, false);

        // Classes
        [Serializable]
        public sealed class QualitySetting : VolumeParameter<ScreenSpaceAmbientOcclusion.Quality>
        {
            public QualitySetting(ScreenSpaceAmbientOcclusion.Quality value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }

        [Serializable]
        public sealed class NormalSamplesSetting : VolumeParameter<ScreenSpaceAmbientOcclusion.NormalSamples>
        {
            public NormalSamplesSetting(ScreenSpaceAmbientOcclusion.NormalSamples value, bool overrideState = false) : base(value, overrideState)
            {
            }
        }

        // This will be used once we've exposed render feature requirements.
        //[Serializable]
        //public sealed class DepthSourceSetting : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.DepthSource>
        //{
        //    public DepthSourceSetting(ScreenSpaceAmbientOcclusionFeature.DepthSource value, bool overrideState = false) : base(value, overrideState)
        //    {
        //    }
        //}
    }
}
