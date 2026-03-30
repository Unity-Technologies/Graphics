using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class URPPreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue + 100;

        private static URPBuildData m_BuildData = null;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_BuildData?.Dispose();
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            m_BuildData = new URPBuildData(EditorUserBuildSettings.activeBuildTarget, isDevelopmentBuild);

            if (m_BuildData.buildingPlayerForUniversalRenderPipeline)
            {
                // Now that we know that we are on URP we need to make sure everything is correct, otherwise we break the build.
                if (!URPBuildDataValidator.IsProjectValidForBuilding(report, out var message))
                    throw new BuildFailedException(message);

                EnsureVolumeProfile();

                LogIncludedAssets(m_BuildData.renderPipelineAssets);
            }
        }

        static void EnsureVolumeProfile()
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPDefaultVolumeProfileSettings>(out var volumeProfileSettings))
            {
                var initState = volumeProfileSettings.volumeProfile;
                volumeProfileSettings.volumeProfile = UniversalRenderPipelineGlobalSettings.GetOrCreateDefaultVolumeProfile(volumeProfileSettings.volumeProfile);

                if (initState != volumeProfileSettings.volumeProfile)
                {
                    Debug.Log($"Default Volume Profile was missing, one has been created automatically at '{AssetDatabase.GetAssetPath(volumeProfileSettings.volumeProfile)}'.");
                }

                if (VolumeProfileUtils.TryEnsureAllOverridesForDefaultProfile(volumeProfileSettings.volumeProfile))
                {
                    Debug.Log("Default Volume Profile has been modified to ensure all overrides are present. This is required to avoid missing overrides at runtime which can lead to unexpected rendering issues. Please save these changes to avoid this message in the future.");
                }
            }
        }

        internal static void LogIncludedAssets(List<UniversalRenderPipelineAsset> assetsList)
        {
            using (GenericPool<StringBuilder>.Get(out var assetsIncluded))
            {
                assetsIncluded.Clear();

                assetsIncluded.AppendLine($"{assetsList.Count} URP assets included in build");

                foreach (var urpAsset in assetsList)
                {
                    assetsIncluded.AppendLine($"- {urpAsset.name} - {AssetDatabase.GetAssetPath(urpAsset)}");
                    foreach (var rendererData in urpAsset.m_RendererDataList)
                    {
                        if (rendererData != null)
                            assetsIncluded.AppendLine($"\t - {rendererData.name} - {AssetDatabase.GetAssetPath(rendererData)} - {rendererData.GetType()}");
                    }
                }

                Debug.Log(assetsIncluded);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Clean up the build data once we have finishing building
            m_BuildData?.Dispose();
            m_BuildData = null;
        }
    }
}
