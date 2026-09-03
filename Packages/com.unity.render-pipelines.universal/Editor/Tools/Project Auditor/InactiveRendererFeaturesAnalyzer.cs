using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Renderer")]
    class InactiveRendererFeaturesAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0104 = nameof(URP0104);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0104,
            "URP: Inactive renderer features detected",
            Areas.Quality | Areas.Memory,
            "One or more renderer data assets contain inactive renderer features. Consider removing them if they are not needed.",
            "Remove inactive renderer features or activate them if they should be used"
        );

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            var rendererDataAssets = URPProjectAuditorUtilities.GetAllRendererDataAssets();

            foreach (var rendererData in rendererDataAssets)
            {
                if (rendererData == null || rendererData.rendererFeatures == null)
                    continue;

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature != null && !feature.isActive)
                        yield return new RenderingSettingsIssue(URP0104);
                }
            }
        }
    }
}
