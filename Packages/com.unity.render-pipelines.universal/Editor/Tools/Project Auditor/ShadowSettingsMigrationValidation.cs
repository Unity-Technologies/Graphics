using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class ShadowSettingsMigrationValidation : IRenderingSettingsAnalyzer
    {
        internal const string URP0006 = nameof(URP0006);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0006,
            "URP: Shadow settings are not migrated to a Shadows volume override",
            Areas.Quality | Areas.Upgrade,
            "One or more URP Assets have shadow distance and cascade settings that were not moved to a <b>Shadows</b> volume override, for example because version control could not check out the asset during the upgrade. URP no longer uses these settings, so shadows use the values from the volume profiles instead.",
            "Make the URP Asset editable, then fix the issue to move its settings to a <b>Shadows</b> volume override."
        )
        {
            DefaultSeverity = Severity.Major,
            MessageFormat = "URP: Shadow settings are not migrated in {0} in {1}",
            Fixer = FixShadowSettings
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset
                && UniversalRenderPipelineGlobalSettings.HasUnmigratedShadowSettings(urpAsset, inReadOnlyPackage: false))
            {
                yield return new RenderingSettingsIssue(URP0006, assetName: AssetDatabase.GetAssetPath(urpAsset));
            }

            using (UnityEngine.Pool.ListPool<(int level, string assetPath)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset
                        && UniversalRenderPipelineGlobalSettings.HasUnmigratedShadowSettings(qualityUrpAsset, inReadOnlyPackage: false))
                    {
                        tmp.Add((level, AssetDatabase.GetAssetPath(qualityUrpAsset)));
                    }
                });

                foreach (var (level, assetPath) in tmp)
                    yield return new RenderingSettingsIssue(URP0006, level, assetPath);
            }
        }

        public static bool FixShadowSettings(ReportItem issue, AnalysisParams context)
        {
            string qualityLevel = issue.CustomProperties?.Length > 0 ? issue.CustomProperties[0] : null;
            var issueAsset = string.IsNullOrEmpty(qualityLevel)
                ? GraphicsSettings.defaultRenderPipeline
                : QualitySettings.GetRenderPipelineAssetAt(int.Parse(qualityLevel));
            return FixShadowSettings(issueAsset as UniversalRenderPipelineAsset);
        }

        internal static bool FixShadowSettings(UniversalRenderPipelineAsset issueAsset)
        {
            var urpAssets = new List<UniversalRenderPipelineAsset>();
            foreach (var asset in GraphicsSettings.allConfiguredRenderPipelines)
            {
                if (asset is UniversalRenderPipelineAsset urpAsset && UniversalRenderPipelineGlobalSettings.HasUnmigratedShadowSettings(urpAsset, inReadOnlyPackage: false))
                    urpAssets.Add(urpAsset);
            }

            UniversalRenderPipelineGlobalSettings.MigratePendingShadowSettingsToVolumeProfiles(urpAssets);
            return issueAsset != null && !issueAsset.m_ShadowSettingsMigrationPending;
        }
    }
}
