#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEngine;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class ColorGradingConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.ColorGrading);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldColorGrading = oldSettings as BIRPRendering.ColorGrading;

            var newTonemapping = AddVolumeComponentToAsset<URPRendering.Tonemapping>(targetProfile); // was: Tonemapping
            var newWhiteBalance =
                AddVolumeComponentToAsset<URPRendering.WhiteBalance>(targetProfile); // was: White Balance
            var newColorAdjustments =
                AddVolumeComponentToAsset<URPRendering.ColorAdjustments>(targetProfile); // was: Tone
            var newTargetProfile =
                AddVolumeComponentToAsset<URPRendering.ChannelMixer>(targetProfile); // was: Channel Mixer
            var newLiftGammaGain =
                AddVolumeComponentToAsset<URPRendering.LiftGammaGain>(targetProfile); // was: Trackballs
            var newColorCurves =
                AddVolumeComponentToAsset<URPRendering.ColorCurves>(targetProfile); // was: Grading Curves

            // Tonemapping
            newTonemapping.active = oldColorGrading.active;

            ConvertTonemapper(oldColorGrading.tonemapper, newTonemapping.mode, oldColorGrading.enabled);

            // White Balance
            newWhiteBalance.active = oldColorGrading.active;

            oldColorGrading.temperature.Convert(newWhiteBalance.temperature, enabledState: oldColorGrading.enabled);
            oldColorGrading.tint.Convert(newWhiteBalance.tint, enabledState: oldColorGrading.enabled);

            // Tone -> ColorAdjustments
            newColorAdjustments.active = oldColorGrading.active;

            oldColorGrading.postExposure.Convert(newColorAdjustments.postExposure,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.colorFilter.Convert(newColorAdjustments.colorFilter, oldColorGrading.enabled,
                disabledColor: Color.white);
            oldColorGrading.hueShift.Convert(newColorAdjustments.hueShift, enabledState: oldColorGrading.enabled);
            oldColorGrading.saturation.Convert(newColorAdjustments.saturation, enabledState: oldColorGrading.enabled);
            oldColorGrading.contrast.Convert(newColorAdjustments.contrast, enabledState: oldColorGrading.enabled);

            // Channel Mixer
            newTargetProfile.active = oldColorGrading.active;

            oldColorGrading.mixerRedOutRedIn.Convert(newTargetProfile.redOutRedIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerRedOutGreenIn.Convert(newTargetProfile.redOutGreenIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerRedOutBlueIn.Convert(newTargetProfile.redOutBlueIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerGreenOutRedIn.Convert(newTargetProfile.greenOutRedIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerGreenOutGreenIn.Convert(newTargetProfile.greenOutGreenIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerGreenOutBlueIn.Convert(newTargetProfile.greenOutBlueIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerBlueOutRedIn.Convert(newTargetProfile.blueOutRedIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerBlueOutGreenIn.Convert(newTargetProfile.blueOutGreenIn,
                enabledState: oldColorGrading.enabled);
            oldColorGrading.mixerBlueOutBlueIn.Convert(newTargetProfile.blueOutBlueIn,
                enabledState: oldColorGrading.enabled);

            // Trackballs -> LiftGammaGain
            newLiftGammaGain.active = oldColorGrading.active;

            // Note: URP always does color grading in HDR values (as it should),
            //       which means the non-HDR modes no longer have valid conversion targets.
            //       So, these values are left at defaults (neutral) when not previously using HDR.
            if (oldColorGrading.gradingMode.value == BIRPRendering.GradingMode.HighDefinitionRange)
            {
                oldColorGrading.lift.Convert(newLiftGammaGain.lift, oldColorGrading.enabled);
                oldColorGrading.gamma.Convert(newLiftGammaGain.gamma, oldColorGrading.enabled);
                oldColorGrading.gain.Convert(newLiftGammaGain.gain, oldColorGrading.enabled);
            }

            // Grading Curves -> ColorCurves
            newColorCurves.active = oldColorGrading.active;

            oldColorGrading.masterCurve.Convert(newColorCurves.master, oldColorGrading.enabled);
            oldColorGrading.redCurve.Convert(newColorCurves.red, oldColorGrading.enabled);
            oldColorGrading.greenCurve.Convert(newColorCurves.green, oldColorGrading.enabled);
            oldColorGrading.blueCurve.Convert(newColorCurves.blue, oldColorGrading.enabled);
            oldColorGrading.hueVsHueCurve.Convert(newColorCurves.hueVsHue, oldColorGrading.enabled);
            oldColorGrading.hueVsSatCurve.Convert(newColorCurves.hueVsSat, oldColorGrading.enabled);
            oldColorGrading.satVsSatCurve.Convert(newColorCurves.satVsSat, oldColorGrading.enabled);
            oldColorGrading.lumVsSatCurve.Convert(newColorCurves.lumVsSat, oldColorGrading.enabled);
        }

        private void ConvertTonemapper(BIRPRendering.TonemapperParameter birpSource,
            URPRendering.TonemappingModeParameter target, bool enabledState)
        {
            if (target == null) return;

            switch (birpSource.value)
            {
                case BIRPRendering.Tonemapper.Neutral:
                    target.value = URPRendering.TonemappingMode.Neutral;
                    break;
                case BIRPRendering.Tonemapper.ACES:
                    target.value = URPRendering.TonemappingMode.ACES;
                    break;
                default:
                    target.value = URPRendering.TonemappingMode.None;
                    break;
            }

            if (!enabledState)
            {
                target.value = URPRendering.TonemappingMode.None;
            }

            target.overrideState = birpSource.overrideState;
        }
    }
}

#endif
