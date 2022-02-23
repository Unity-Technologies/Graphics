#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEditor;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class GrainConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.Grain);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldGrain = oldSettings as BIRPRendering.Grain;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.FilmGrain>(targetProfile);

            newVolumeComponent.active = oldGrain.active;

            oldGrain.intensity.Convert(newVolumeComponent.intensity, enabledState: oldGrain.enabled);
            oldGrain.lumContrib.Convert(newVolumeComponent.response);

            newVolumeComponent.type.overrideState = oldGrain.size.overrideState;
            if (oldGrain.size.value > 1.5f)
                newVolumeComponent.type.value = URPRendering.FilmGrainLookup.Medium3;
            else if (oldGrain.size.value > 1.25f)
                newVolumeComponent.type.value = URPRendering.FilmGrainLookup.Medium2;
            else if (oldGrain.size.value > 0.7f)
                newVolumeComponent.type.value = URPRendering.FilmGrainLookup.Thin2;
            else
                newVolumeComponent.type.value = URPRendering.FilmGrainLookup.Thin1;

            // TODO: No clear conversions for these?
            // oldGrain.colored
        }
    }
}
#endif
