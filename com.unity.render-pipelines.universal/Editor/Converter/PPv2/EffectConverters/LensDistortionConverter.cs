#if PPV2_EXISTS
using System;
using BIRPToURPConversionExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Converters
{
    public class LensDistortionConverter : PostProcessEffectSettingsConverter
    {
        protected override Type OldSettingsType { get; } = typeof(BIRPRendering.LensDistortion);

        protected override void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            var oldLensDistortion = oldSettings as BIRPRendering.LensDistortion;

            var newVolumeComponent = AddVolumeComponentToAsset<URPRendering.LensDistortion>(targetProfile);

            newVolumeComponent.active = oldLensDistortion.active;

            oldLensDistortion.intensity.Convert(newVolumeComponent.intensity,
                scale: 0.01f,
                enabledState: oldLensDistortion.enabled);
            oldLensDistortion.intensityX.Convert(newVolumeComponent.xMultiplier);
            oldLensDistortion.intensityY.Convert(newVolumeComponent.yMultiplier);
            oldLensDistortion.scale.Convert(newVolumeComponent.scale);

            newVolumeComponent.center.overrideState =
                oldLensDistortion.centerX.overrideState || oldLensDistortion.centerY.overrideState;
            newVolumeComponent.center.value =
                new Vector2(oldLensDistortion.centerX.value, oldLensDistortion.centerY.value);
        }
    }
}
#endif
