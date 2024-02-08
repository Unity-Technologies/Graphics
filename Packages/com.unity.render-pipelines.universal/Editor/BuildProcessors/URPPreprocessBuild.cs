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
    class URPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1;

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

                LogIncludedAssets(m_BuildData.renderPipelineAssets);
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
