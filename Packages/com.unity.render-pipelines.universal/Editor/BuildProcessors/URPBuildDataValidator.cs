using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
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

        // This check iterates over all pipeline assets and enables the built-in DynamicBatching flag if at least one of them has dynamic batching enabled. We need to do this to ensure meshCompression is set correctly.
        private static void ValidateDynamicBatchingSettings(List<UniversalRenderPipelineAsset> renderPipelineAssets)
        {
            bool supportsDynamicBatching = false;
            foreach (var urpPipelineAsset in renderPipelineAssets)
            {
                if (urpPipelineAsset.supportsDynamicBatching)
                {
                    supportsDynamicBatching = true;
                    break;
                }
            }

            PlayerSettings.SetDynamicBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, supportsDynamicBatching);
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

        public static bool IsProjectValidForBuilding(BuildReport report, out string message)
        {
            using (GenericPool<StringBuilder>.Get(out var failures))
            {
                failures.Clear();

                ValidateRenderPipelineAssetsAreAtLastVersion(URPBuildData.instance.renderPipelineAssets, failures);
                ValidateRenderPipelineGlobalSettings(UniversalRenderPipelineGlobalSettings.Ensure(), failures);
                ValidateDynamicBatchingSettings(URPBuildData.instance.renderPipelineAssets);

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
