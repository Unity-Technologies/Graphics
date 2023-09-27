using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPPreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static HDRPBuildData m_BuildData = null;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_BuildData?.Dispose();
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            m_BuildData = new HDRPBuildData(EditorUserBuildSettings.activeBuildTarget, isDevelopmentBuild);

            if (m_BuildData.buildingPlayerForHDRenderPipeline)
            {
                // Now that we know that we are on HDRP we need to make sure everything is correct, otherwise we break the build.
                if (!HDRPBuildDataValidator.IsProjectValidForBuilding(report, out var message))
                    throw new BuildFailedException(message);

                ConfigureMinimumMaxLoDValueForAllQualitySettings();

                LogIncludedAssets(m_BuildData.renderPipelineAssets);
            }
        }

        internal static void LogIncludedAssets(List<HDRenderPipelineAsset> assetsList)
        {
            using (GenericPool<StringBuilder>.Get(out var assetsIncluded))
            {
                assetsIncluded.Clear();

                assetsIncluded.Append($"{assetsList.Count} HDRP assets included in build");

                foreach (var hdrpAsset in assetsList)
                {
                    assetsIncluded.AppendLine($"- {hdrpAsset.name} - {AssetDatabase.GetAssetPath(hdrpAsset)}");
                }

                Debug.Log(assetsIncluded);
            }
        }

        internal static void ConfigureMinimumMaxLoDValueForAllQualitySettings()
        {
            int GetMinimumMaxLoDValue(HDRenderPipelineAsset asset)
            {
                int minimumMaxLoD = int.MaxValue;

                if (asset != null)
                {
                    var maxLoDs = asset.currentPlatformRenderPipelineSettings.maximumLODLevel;
                    var schema = ScalableSettingSchema.GetSchemaOrNull(maxLoDs.schemaId);
                    for (int lod = 0; lod < schema.levelCount; ++lod)
                    {
                        if (maxLoDs.TryGet(lod, out int maxLoD))
                            minimumMaxLoD = Mathf.Min(minimumMaxLoD, maxLoD);
                    }
                }

                return minimumMaxLoD != int.MaxValue ? minimumMaxLoD : 0;
            }

            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;

            // Update all quality levels with the right max lod so that meshes can be stripped.
            // We don't take lod bias into account because it can be overridden per camera.
            QualitySettings.ForEach((tier, name) =>
            {
                if (QualitySettings.renderPipeline is not HDRenderPipelineAsset renderPipeline)
                    renderPipeline = defaultRenderPipeline;

                QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(renderPipeline);
            });
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Clean up the build data once we have finishing building
            m_BuildData?.Dispose();
            m_BuildData = null;
        }
    }
}
