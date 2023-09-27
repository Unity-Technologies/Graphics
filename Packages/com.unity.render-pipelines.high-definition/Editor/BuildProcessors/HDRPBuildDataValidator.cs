using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPBuildDataValidator
    {
        internal static void ValidateRenderPipelineAssetsAreAtLastVersion(List<HDRenderPipelineAsset> renderPipelineAssets, StringBuilder failures)
        {
            // Validate all included assets are at last version
            foreach (var hdPipelineAsset in renderPipelineAssets)
            {
                if (!(hdPipelineAsset as IMigratableAsset).IsAtLastVersion())
                {
                    failures.AppendLine(
                        $"- The {nameof(HDRenderPipelineAsset)} with '{hdPipelineAsset.name}({AssetDatabase.GetAssetPath(hdPipelineAsset)})' is not at last version.");
                }
            }
        }

        internal static void ValidateRenderPipelineGlobalSettings(HDRenderPipelineGlobalSettings globalSettingsInstance, StringBuilder failures)
        {
            if (globalSettingsInstance == null)
                failures.AppendLine($"- The {nameof(HDRenderPipelineGlobalSettings)} of the project are missing.");
            else
            {
                if (!(globalSettingsInstance as IMigratableAsset).IsAtLastVersion())
                {
                    failures.AppendLine(
                        $"- The {nameof(HDRenderPipelineGlobalSettings)} with '{globalSettingsInstance.name}({AssetDatabase.GetAssetPath(globalSettingsInstance)})' is not at last version.");
                }
            }
        }

        internal static void ValidatePlatform(UnityEditor.BuildTarget activeBuildTarget, StringBuilder failures)
        {
            // If platform is not supported, throw an exception to stop the build
            if (!HDUtils.IsSupportedBuildTargetAndDevice(activeBuildTarget, out GraphicsDeviceType deviceType))
                failures.AppendLine(HDUtils.GetUnsupportedAPIMessage(deviceType.ToString()));
        }

        public static bool IsProjectValidForBuilding(BuildReport report, out string message)
        {
            using (GenericPool<StringBuilder>.Get(out var failures))
            {
                failures.Clear();

                ValidateRenderPipelineAssetsAreAtLastVersion(HDRPBuildData.instance.renderPipelineAssets, failures);
                ValidateRenderPipelineGlobalSettings(HDRenderPipelineGlobalSettings.Ensure(), failures);
                ValidatePlatform(report.summary.platform, failures);

                string allFailures = failures.ToString();

                if (!string.IsNullOrEmpty(allFailures))
                {
                    message =
                        $"Please fix the following errors before building:{Environment.NewLine}{allFailures}. {Environment.NewLine}You can use HDRP Wizard to fix them";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }
    }
}
