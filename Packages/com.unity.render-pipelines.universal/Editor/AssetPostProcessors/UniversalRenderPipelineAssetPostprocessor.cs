using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class UniversalRenderPipelineAssetPostprocessor : AssetPostprocessor
    {
        [RunAfterClass(typeof(UniversalRenderPipelineGlobalSettingsPostprocessor))]
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<UniversalRenderPipelineAsset> urpAssets = null;
            foreach (var path in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(UniversalRenderPipelineAsset))
                    continue;

                var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (urpAsset != null)
                    (urpAssets ??= new List<UniversalRenderPipelineAsset>()).Add(urpAsset);
            }

            if (urpAssets != null)
                UniversalRenderPipelineGlobalSettings.MigratePendingShadowSettingsToVolumeProfiles(urpAssets);
        }
    }
}
