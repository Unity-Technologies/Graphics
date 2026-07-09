using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class URPBuildDataValidator
    {
        private static void ValidateRenderPipelineAssetsAreAtLastVersion(List<UniversalRenderPipelineAsset> renderPipelineAssets, StringBuilder failures)
        {
            // Validate all included assets are at last version
            foreach (var urpPipelineAsset in renderPipelineAssets)
            {
                if (!urpPipelineAsset.IsAtLastVersion())
                {
                    failures.AppendLine(
                        $"- The {nameof(UniversalRenderPipelineAsset)} with '{urpPipelineAsset.name}({AssetDatabase.GetAssetPath(urpPipelineAsset)})' is not at last version.");
                }
            }
        }

        private static void ValidateRenderPipelineGlobalSettings(UniversalRenderPipelineGlobalSettings globalSettingsInstance, StringBuilder failures)
        {
            if (globalSettingsInstance == null)
                failures.AppendLine($"- The {nameof(UniversalRenderPipelineGlobalSettings)} of the project are missing.");
            else
            {
                if (!globalSettingsInstance.IsAtLastVersion())
                {
                    failures.AppendLine(
                        $"- The {nameof(UniversalRenderPipelineGlobalSettings)} with '{globalSettingsInstance.name}({AssetDatabase.GetAssetPath(globalSettingsInstance)})' is not at last version.");
                }
            }
        }

#if SURFACE_CACHE
        private static void ValidateSurfaceCacheSupportedOnTarget(BuildReport report, StringBuilder failures)
        {
            // GetAssemblies() only answers for the active target, so validate only that one.
            if (report.summary.platform != EditorUserBuildSettings.activeBuildTarget)
                return;
            if (SurfaceCacheGISupport.IsSupportedByActiveBuildTarget())
                return;
            if (!SurfaceCacheStripperUtility.IsSurfaceCacheEnabledForBuild())
                return;

            failures.AppendLine(
                $"- Surface Cache GI is enabled in a Universal Renderer but is not supported on the active build target '{EditorUserBuildSettings.activeBuildTarget}'. " +
                "Remove the Surface Cache GI renderer feature, or build with a Renderer that does not use it.");
        }
#endif

        public static bool IsProjectValidForBuilding(BuildReport report, out string message)
        {
            using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var failures))
            {
                failures.Clear();

                ValidateRenderPipelineAssetsAreAtLastVersion(URPBuildData.instance.renderPipelineAssets, failures);
                ValidateRenderPipelineGlobalSettings(UniversalRenderPipelineGlobalSettings.Ensure(), failures);
#if SURFACE_CACHE
                ValidateSurfaceCacheSupportedOnTarget(report, failures);
#endif

                string allFailures = failures.ToString();

                if (!string.IsNullOrEmpty(allFailures))
                {
                    message =
                        $"Please fix the following errors before building:{Environment.NewLine}{allFailures}. {Environment.NewLine}";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }
    }
}
