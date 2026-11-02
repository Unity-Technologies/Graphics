using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Global Settings")]
    class GlobalSettingsAssetAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0201 = nameof(URP0201);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0201,
            "URP: URP Global Settings Asset is not assigned",
            Areas.Quality,
            "The Universal Render Pipeline Global Settings asset is not assigned or is invalid. Global Settings contains important configuration such as rendering layer names and shader stripping settings. Without it, some URP features may not work correctly.",
            "Ensure URP Global Settings asset"
        )
        {
            DefaultSeverity = Severity.Major,
            Fixer = FixGlobalSettings,
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            var settings = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();

            // If settings is null, we're not in URP or settings haven't been initialized
            if (UniversalRenderPipelineGlobalSettings.instance == null)
                yield return new RenderingSettingsIssue(URP0201);
        }

        public static bool FixGlobalSettings(ReportItem issue, AnalysisParams context)
        {
            UniversalRenderPipelineGlobalSettings.Ensure();
            return true;
        }
    }
}
