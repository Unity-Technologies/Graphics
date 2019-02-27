using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Exposure")]
    public sealed class Exposure : VolumeComponent, IPostProcessComponent
    {
        public ExposureModeParameter mode = new ExposureModeParameter(ExposureMode.Fixed);
        public MeteringModeParameter meteringMode = new MeteringModeParameter(MeteringMode.CenterWeighted);
        public LuminanceSourceParameter luminanceSource = new LuminanceSourceParameter(LuminanceSource.ColorBuffer);

        public FloatParameter fixedExposure = new FloatParameter(0f);
        public FloatParameter compensation = new FloatParameter(0f);
        public FloatParameter limitMin = new FloatParameter(-10f);
        public FloatParameter limitMax = new FloatParameter(20f);
        public AnimationCurveParameter curveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 20f, 20f)); // TODO: Use TextureCurve instead?

        public AdaptationModeParameter adaptationMode = new AdaptationModeParameter(AdaptationMode.Progressive);
        public MinFloatParameter adaptationSpeedDarkToLight = new MinFloatParameter(3f, 0.001f);
        public MinFloatParameter adaptationSpeedLightToDark = new MinFloatParameter(1f, 0.001f);

        public bool IsActive()
        {
            return true;
        }
    }

    public enum ExposureMode
    {
        Fixed,
        Automatic,
        CurveMapping,
        UsePhysicalCamera
    }

    public enum MeteringMode
    {
        Average,
        Spot,
        CenterWeighted
    }

    public enum LuminanceSource
    {
        LightingBuffer,
        ColorBuffer
    }

    public enum AdaptationMode
    {
        Fixed,
        Progressive
    }

    [Serializable]
    public sealed class ExposureModeParameter : VolumeParameter<ExposureMode> { public ExposureModeParameter(ExposureMode value, bool overrideState = false) : base(value, overrideState) {} }

    [Serializable]
    public sealed class MeteringModeParameter : VolumeParameter<MeteringMode> { public MeteringModeParameter(MeteringMode value, bool overrideState = false) : base(value, overrideState) { } }
    
    [Serializable]
    public sealed class LuminanceSourceParameter : VolumeParameter<LuminanceSource> { public LuminanceSourceParameter(LuminanceSource value, bool overrideState = false) : base(value, overrideState) {} }

    [Serializable]
    public sealed class AdaptationModeParameter : VolumeParameter<AdaptationMode> { public AdaptationModeParameter(AdaptationMode value, bool overrideState = false) : base(value, overrideState) {} }
}
