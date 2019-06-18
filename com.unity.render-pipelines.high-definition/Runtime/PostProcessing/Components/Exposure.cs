using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Exposure")]
    public sealed class Exposure : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the method that HDRP uses to process exposure.")]
        public ExposureModeParameter mode = new ExposureModeParameter(ExposureMode.Fixed);
        [Tooltip("Specifies the metering method that HDRP uses the filter the luminance source.")]
        public MeteringModeParameter meteringMode = new MeteringModeParameter(MeteringMode.CenterWeighted);
        [Tooltip("Specifies the luminance source that HDRP uses to calculate the current Scene exposure.")]
        public LuminanceSourceParameter luminanceSource = new LuminanceSourceParameter(LuminanceSource.ColorBuffer);

        [Tooltip("Sets a static exposure value for Cameras in this Volume.")]
        public FloatParameter fixedExposure = new FloatParameter(0f);
        [Tooltip("Sets the compensation that the Camera applies to the calculated exposure value.")]
        public FloatParameter compensation = new FloatParameter(0f);
        [Tooltip("Sets the minimum value that the Scene exposure can be set to.")]
        public FloatParameter limitMin = new FloatParameter(-10f);
        [Tooltip("Sets the maximum value that the Scene exposure can be set to.")]
        public FloatParameter limitMax = new FloatParameter(20f);
        [Tooltip("Specifies a curve that remaps the Scene exposure on the x-axis to the exposure you want on the y-axis.")]
        public AnimationCurveParameter curveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 20f, 20f)); // TODO: Use TextureCurve instead?

        [Tooltip("Specifies the method that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa.")]
        public AdaptationModeParameter adaptationMode = new AdaptationModeParameter(AdaptationMode.Progressive);
        [Tooltip("Sets the speed at which the exposure changes when the Camera moves from a dark area to a bright area.")]
        public MinFloatParameter adaptationSpeedDarkToLight = new MinFloatParameter(3f, 0.001f);
        [Tooltip("Sets the speed at which the exposure changes when the Camera moves from a bright area to a dark area.")]
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
