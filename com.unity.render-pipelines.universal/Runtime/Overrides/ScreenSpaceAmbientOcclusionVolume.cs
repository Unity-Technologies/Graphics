using UnityEngine.Rendering;
using System;

[VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
{
    // Public properties
    //public DepthSource DepthSource = new DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource.Depth);
    public QualitySettingNormals NormalQuality = new QualitySettingNormals(ScreenSpaceAmbientOcclusionFeature.QualityOptions.Medium);
    public BoolParameter Downsample            = new BoolParameter(true);
    public ClampedFloatParameter Intensity     = new ClampedFloatParameter(0f, 0f, 10f, false);
    public ClampedFloatParameter Radius        = new ClampedFloatParameter(0.05f, 0f, 10.0f, false);
    public ClampedIntParameter SampleCount     = new ClampedIntParameter(4, 0, 12, false);
    public ClampedIntParameter BlurPassesCount = new ClampedIntParameter(3, 0, 12, false);

    // Classes
    [Serializable]
    public sealed class QualitySettingNormals : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.QualityOptions>
    {
        public QualitySettingNormals(ScreenSpaceAmbientOcclusionFeature.QualityOptions value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }

    // This will be used once we've exposed render feature requirements.
    //[Serializable]
    //public sealed class DepthSource : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.DepthSource>
    //{
    //    public DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource value, bool overrideState = false) : base(value, overrideState)
    //    {
    //    }
    //}
}
