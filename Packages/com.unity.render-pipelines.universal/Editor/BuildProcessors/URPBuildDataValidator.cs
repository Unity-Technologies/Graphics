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
        
#if !URP_COMPATIBILITY_MODE
        private static void ValidateCompatibilityMode(UniversalRenderPipelineGlobalSettings globalSettingsInstance, StringBuilder failures)
        {
            if (globalSettingsInstance == null)
                return; //error already covered in ValidateRenderPipelineGlobalSettings

            if (!GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphSettings>(out var settings))
            {
                failures.AppendLine($"- The {nameof(RenderGraphSettings)} of the project are missing. Is the {nameof(UniversalRenderPipelineGlobalSettings)} missing?");
                return;
            }

            if (settings.GetSerializedCompatibilityModeForBuildCheck())
            {
                failures.AppendLine($"- Compatibility Mode is enabled in Project Settings, but this feature is deprecated from Unity 6.0, and the setting is hidden in Unity 6.3. To enable Compatibility Mode, go to Edit > Project Settings > Player and add URP_COMPATIBILITY_MODE to the Scripting Define Symbols.");

                //It can be complicated to fix it manually:
                // - Add the URP_COMPATIBILITY_MODE define
                // - Change back the checkbox to false in Project Settings > Graphics
                // - Remove the URP_COMPATIBILITY_MODE define
                //So this helpbox propose to fix it for user wanting to adopt Render Graph
                EditorApplication.delayCall += () => {
                    EditorApplication.delayCall += () =>
                    {
                        int answear = EditorUtility.DisplayDialogComplex("Universal Render Pipeline's Compatibility Mode",
                            "Unity can't build your project because Compatibility Mode (Render Graph disabled) is active. This feature is deprecated.\n\n" +
                            "Select \"Use Render Graph\" (Recommended) to update Project Settings. You may need to update scripts and assets.\n\n" +
                            "Select \"Keep Compatibility Mode\" to add URP_COMPATIBILITY_MODE to Player Settings for this build target. Warning: Compatibility Mode will be removed in a future release.",
                            "Use Render Graph",
                            "Cancel",
                            "Keep Compatibility Mode");
                        switch (answear)
                        {
                            case 0: settings.SetCompatibilityModeFromUpgrade(false); break;
                            case 2: settings.AddCompatibilityModeDefineForCurrentPlateform(); break;
                        };
                    };
                };
            }
        }
#endif

        public static bool IsProjectValidForBuilding(BuildReport report, out string message)
        {
            using (GenericPool<StringBuilder>.Get(out var failures))
            {
                failures.Clear();

                ValidateRenderPipelineAssetsAreAtLastVersion(URPBuildData.instance.renderPipelineAssets, failures);
                ValidateRenderPipelineGlobalSettings(UniversalRenderPipelineGlobalSettings.Ensure(), failures);
                ValidateDynamicBatchingSettings(URPBuildData.instance.renderPipelineAssets);
#if !URP_COMPATIBILITY_MODE
                ValidateCompatibilityMode(UniversalRenderPipelineGlobalSettings.Ensure(), failures);
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
