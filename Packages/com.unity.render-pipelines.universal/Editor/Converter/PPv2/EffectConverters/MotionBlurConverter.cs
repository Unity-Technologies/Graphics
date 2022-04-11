#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEditor;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class MotionBlurConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.MotionBlur);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings, VolumeProfile targetProfile)
        {
            var oldMotionBlur = oldSettings as BIRPRendering.MotionBlur;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.MotionBlur>(targetProfile);

            newVolumeComponent.active = oldMotionBlur.active;

            // Note: These settings cannot provide visual parity,
            // but this scale factor provides a good starting point.
            oldMotionBlur.shutterAngle.Convert(newVolumeComponent.intensity, scale: 1f / 360f, oldMotionBlur.enabled);

            newVolumeComponent.quality.overrideState = oldMotionBlur.sampleCount.overrideState;
            if (oldMotionBlur.sampleCount >= 24)
                newVolumeComponent.quality.value = URPRendering.MotionBlurQuality.High;
            else if (oldMotionBlur.sampleCount > 12)
                newVolumeComponent.quality.value = URPRendering.MotionBlurQuality.Medium;
            else
                newVolumeComponent.quality.value = URPRendering.MotionBlurQuality.Low;
        }
    }
}
#endif
