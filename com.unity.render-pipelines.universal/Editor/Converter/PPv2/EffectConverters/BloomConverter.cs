#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEditor;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Converters
{
    public class BloomConverter : PostProcessEffectSettingsConverter
    {
#if PPV2_EXISTS
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.Bloom);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings, VolumeProfile targetProfile)
        {
            var oldBloom = oldSettings as BIRPRendering.Bloom;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.Bloom>(targetProfile);

            newVolumeComponent.active = oldBloom.active;

            oldBloom.clamp.Convert(newVolumeComponent.clamp);
            oldBloom.diffusion.Convert(newVolumeComponent.scatter, scale: 0.05f);
            oldBloom.intensity.Convert(newVolumeComponent.intensity, enabledState: oldBloom.enabled);
            oldBloom.threshold.Convert(newVolumeComponent.threshold);
            oldBloom.color.Convert(newVolumeComponent.tint);
            oldBloom.dirtIntensity.Convert(newVolumeComponent.dirtIntensity);
            oldBloom.dirtTexture.Convert(newVolumeComponent.dirtTexture);
            oldBloom.fastMode.Convert(newVolumeComponent.highQualityFiltering, invertValue: true);

            // TODO: No clear conversions for these?
            // newVolumeComponent.skipIterations = oldBloom.???;
            // newVolumeComponent.??? = oldBloom.anamorphicRatio;
            // newVolumeComponent.??? = oldBloom.softKnee;
        }

#endif
    }
}
#endif
