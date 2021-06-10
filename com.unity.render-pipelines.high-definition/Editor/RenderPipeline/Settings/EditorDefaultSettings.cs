using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [Obsolete("Please use HDRenderPipelineGlobalSettings.instance.* instead.", false)]
    static class EditorDefaultSettings
    {
        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile()
        {
            return HDRenderPipelineGlobalSettings.instance.GetOrCreateDefaultVolumeProfile();
        }

        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            return GetOrAssignDefaultVolumeProfile();
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile()
        {
            return GetOrAssignLookDevVolumeProfile(HDRenderPipeline.currentAsset);
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            return HDRenderPipelineGlobalSettings.instance.GetOrAssignLookDevVolumeProfile();
        }
    }
}
