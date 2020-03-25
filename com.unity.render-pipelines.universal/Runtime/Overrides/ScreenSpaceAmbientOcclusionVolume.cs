using UnityEngine.Rendering;
using System;

[VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
{
    // Public properties
    public BoolParameter DownScale = new BoolParameter(false);
    public ClampedIntParameter SampleCount = new ClampedIntParameter(10, 2, 32, false);
    public ClampedFloatParameter Intensity = new ClampedFloatParameter(0f, 0f, 10f, false);
    public ClampedFloatParameter Radius = new ClampedFloatParameter(0.05f, 0.001f, 10.0f, false);
    public QualitySettingNormals NormalQuality = new QualitySettingNormals(ScreenSpaceAmbientOcclusionFeature.QualityOptions.Medium);

    // Classes
    [Serializable]
    public sealed class QualitySettingNormals : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.QualityOptions>
    {
        public QualitySettingNormals(ScreenSpaceAmbientOcclusionFeature.QualityOptions value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }

    // This will be used once we've exposed render feature requirements.
    //public DepthSource depthSource = new DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource.Depth);
    //[Serializable]
    //public sealed class DepthSource : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.DepthSource>
    //{
    //    public DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource value, bool overrideState = false) : base(value, overrideState)
    //    {
    //    }
    //}
}
