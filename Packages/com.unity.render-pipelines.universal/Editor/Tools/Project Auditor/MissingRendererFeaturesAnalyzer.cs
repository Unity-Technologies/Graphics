using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Renderer")]
    class MissingRendererFeaturesAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0102 = nameof(URP0102);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0102,
            "URP: Missing or null renderer features",
            Areas.Quality,
            "One or more renderer data assets contain null or missing renderer features. This typically happens when scripts are deleted or there are compilation errors. This can cause rendering issues.",
            "Open the renderer data asset and remove the missing features, or restore the missing scripts"
        )
        {
            DefaultSeverity = Severity.Major,
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (!CheckForMissingFeatures())
                yield return new RenderingSettingsIssue(URP0102);
        }

        static bool CheckForMissingFeatures()
        {
            var rendererDataAssets = URPProjectAuditorUtilities.GetAllRendererDataAssets();

            foreach (var rendererData in rendererDataAssets)
            {
                if (rendererData == null)
                    continue;

                if (rendererData.rendererFeatures != null && rendererData.rendererFeatures.Contains(null))
                    return false; // Found missing features
            }

            return true; // No missing features
        }
    }
}
