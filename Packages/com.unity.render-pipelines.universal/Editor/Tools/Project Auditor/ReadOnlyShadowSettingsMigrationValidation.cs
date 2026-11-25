using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class ReadOnlyShadowSettingsMigrationValidation : IRenderingSettingsAnalyzer
    {
        internal const string URP0007 = nameof(URP0007);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0007,
            "URP: Shadow settings in a read-only package are not migrated to a Shadows volume override",
            Areas.Quality | Areas.Upgrade,
            "One or more URP Assets in read-only packages have shadow distance and cascade settings that could not be moved to a <b>Shadows</b> volume override. URP no longer uses these settings, so shadows use the values from the volume profiles instead.",
            "Update the package, or copy the URP Asset into the Assets folder and use the copy."
        )
        {
            DefaultSeverity = Severity.Major,
            MessageFormat = "URP: Shadow settings are not migrated in read-only {0} in {1}",
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset
                && UniversalRenderPipelineGlobalSettings.HasUnmigratedShadowSettings(urpAsset, inReadOnlyPackage: true))
            {
                yield return new RenderingSettingsIssue(URP0007, assetName: AssetDatabase.GetAssetPath(urpAsset));
            }

            using (UnityEngine.Pool.ListPool<(int level, string assetPath)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset
                        && UniversalRenderPipelineGlobalSettings.HasUnmigratedShadowSettings(qualityUrpAsset, inReadOnlyPackage: true))
                    {
                        tmp.Add((level, AssetDatabase.GetAssetPath(qualityUrpAsset)));
                    }
                });

                foreach (var (level, assetPath) in tmp)
                    yield return new RenderingSettingsIssue(URP0007, level, assetPath);
            }
        }
    }
}
