#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Converters
{
    public class VignetteConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.Vignette);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldVignette = oldSettings as BIRPRendering.Vignette;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.Vignette>(targetProfile);

            newVolumeComponent.active = oldVignette.active;

            oldVignette.color.Convert(newVolumeComponent.color);

            if (oldVignette.mode.value == BIRPRendering.VignetteMode.Masked)
            {
                // There's not much we can do with the Masked mode at present,
                // so we just assume the old opacity should be used as intensity,
                // and leave all other settings at default values.
                oldVignette.opacity.Convert(newVolumeComponent.intensity, enabledState: oldSettings.enabled);
            }
            else
            {
                oldVignette.intensity.Convert(newVolumeComponent.intensity, enabledState: oldSettings.enabled);

                oldVignette.center.Convert(newVolumeComponent.center);
                oldVignette.rounded.Convert(newVolumeComponent.rounded);
                oldVignette.smoothness.Convert(newVolumeComponent.smoothness);
            }

            // TODO: No clear conversions for these?
            // oldVignette.mask
            // oldVignette.roundness
        }
    }
}
#endif
