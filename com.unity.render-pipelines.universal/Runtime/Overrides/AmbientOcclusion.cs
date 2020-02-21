using UnityEngine.Rendering;
using System;

[VolumeComponentMenu("Lighting/Ambient Occlusion")]
class AmbientOcclusion : VolumeComponent
{
    public enum AOMode
    {
        SSAO
    }

    public enum SSAOQuality
    {
        Low,
        Medium,
        High
    }

    // Common Options
    public AOModeParameter type = new AOModeParameter(AOMode.SSAO, false);
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f, false);
    public ClampedFloatParameter radius = new ClampedFloatParameter(0.025f, 0.001f, 1.0f, false);

    //SSAO Options
    public FloatParameter area = new FloatParameter(50, false);
    public ClampedIntParameter sampleCount = new ClampedIntParameter(16, 2, 32, false);
    public SSAOQuality quality = SSAOQuality.Medium;

    [Serializable]
    public sealed class AOModeParameter : VolumeParameter<AOMode> { public AOModeParameter(AOMode value, bool overrideState = false) : base(value, overrideState) { } }

}
