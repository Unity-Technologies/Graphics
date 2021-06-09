#if PPV2_EXISTS
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;
using URPRendering = UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Converters
{
    public abstract class PostProcessEffectSettingsConverter : ScriptableObject
    {
        protected abstract Type OldSettingsType { get; }

        public void AddConvertedProfileSettingsToProfile(
            BIRPRendering.PostProcessEffectSettings oldSettings,
            VolumeProfile targetProfile)
        {
            if (oldSettings == null || oldSettings.GetType() != OldSettingsType) return;
            if (targetProfile == null || targetProfile.Has(OldSettingsType)) return;

            ConvertToTarget(oldSettings, targetProfile);
        }

        protected abstract void ConvertToTarget(BIRPRendering.PostProcessEffectSettings oldBloom,
            VolumeProfile targetProfile);

        protected T AddVolumeComponentToAsset<T>(VolumeProfile targetProfileAsset) where T : VolumeComponent
        {
            if (!targetProfileAsset) return null;

            var profilePath = AssetDatabase.GetAssetPath(targetProfileAsset);

            if (string.IsNullOrEmpty(profilePath)) return null;

            var newVolumeComponent = targetProfileAsset.Add<T>();
            AssetDatabase.AddObjectToAsset(newVolumeComponent, targetProfileAsset);

            return newVolumeComponent;
        }
    }
}
#endif
