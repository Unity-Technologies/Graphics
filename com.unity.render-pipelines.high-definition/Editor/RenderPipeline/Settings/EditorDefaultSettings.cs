using System;
using System.IO;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    static class EditorDefaultSettings
    {
        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile()
        {
            if (!(GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset hdrpAsset))
                return null;

            return GetOrAssignDefaultVolumeProfile(hdrpAsset);
        }

        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            if (hdrpAsset.defaultVolumeProfile == null || hdrpAsset.defaultVolumeProfile.Equals(null))
            {
                hdrpAsset.defaultVolumeProfile =
                    hdrpAsset.renderPipelineEditorResources.defaultSettingsVolumeProfile;
                EditorUtility.SetDirty(hdrpAsset);
            }

            return hdrpAsset.defaultVolumeProfile;
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile()
        {
            if (!(GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset hdrpAsset))
                return null;

            return GetOrAssignLookDevVolumeProfile(hdrpAsset);
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            if (hdrpAsset.defaultLookDevProfile == null || hdrpAsset.defaultLookDevProfile.Equals(null))
                hdrpAsset.defaultLookDevProfile =
                    hdrpAsset.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;

            return hdrpAsset.defaultLookDevProfile;
        }
    }
}
