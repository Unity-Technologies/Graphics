#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class DepthOfFieldConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.DepthOfField);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldDepthOfField = oldSettings as BIRPRendering.DepthOfField;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.DepthOfField>(targetProfile);

            newVolumeComponent.active = oldDepthOfField.active;

            // Always use Bokeh mode, because it has parity with the PPv2 approach
            newVolumeComponent.mode.value = oldDepthOfField.enabled
                ? URPRendering.DepthOfFieldMode.Bokeh
                : URPRendering.DepthOfFieldMode.Off;
            newVolumeComponent.mode.overrideState = true;

            oldDepthOfField.focusDistance.Convert(newVolumeComponent.focusDistance, enabledState: oldSettings.enabled);
            oldDepthOfField.focalLength.Convert(newVolumeComponent.focalLength, enabledState: oldSettings.enabled);
            oldDepthOfField.aperture.Convert(newVolumeComponent.aperture, enabledState: oldSettings.enabled);

            // TODO: No clear conversions for these?
            // oldDepthOfField.kernelSize
        }
    }
}
#endif
