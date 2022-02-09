#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEditor;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class ChromaticAberrationConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.ChromaticAberration);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldChromaticAberration = oldSettings as BIRPRendering.ChromaticAberration;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.ChromaticAberration>(targetProfile);

            newVolumeComponent.active = oldChromaticAberration.active;

            // TODO: Verify that these are 1:1 conversions for visual parity
            oldChromaticAberration.intensity.Convert(newVolumeComponent.intensity,
                enabledState: oldChromaticAberration.enabled);

            // TODO: No clear conversions for these?
            // oldChromaticAberration.spectralLut
            // oldChromaticAberration.fastMode
        }
    }
}
#endif
