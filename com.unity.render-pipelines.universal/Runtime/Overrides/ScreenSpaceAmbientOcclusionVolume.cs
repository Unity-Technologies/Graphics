using UnityEngine.Rendering;
using System;

[VolumeComponentMenu("Lighting/Screen Space Ambient Occlusion")]
class ScreenSpaceAmbientOcclusionVolume : VolumeComponent
{
    public DepthSource depthSource = new DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource.Depth);
    public BoolParameter downSample = new BoolParameter(false);
    public ClampedIntParameter sampleCount = new ClampedIntParameter(16, 2, 32, false);
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 4f, false);
    public ClampedFloatParameter radius = new ClampedFloatParameter(0.05f, 0.001f, 1.0f, false);

    public bool IsActive() => intensity.value > 0f;

    [Serializable]
    public sealed class DepthSource : VolumeParameter<ScreenSpaceAmbientOcclusionFeature.DepthSource>
    {
        public DepthSource(ScreenSpaceAmbientOcclusionFeature.DepthSource value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
}