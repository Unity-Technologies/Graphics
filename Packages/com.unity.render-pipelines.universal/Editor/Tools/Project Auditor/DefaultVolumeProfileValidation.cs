using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Global Settings")]
    class DefaultVolumeProfileAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0202 = nameof(URP0202);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0202,
            "URP: Default Volume Profile is not assigned",
            Areas.Quality,
            "The default Volume Profile is not assigned in Graphics Settings. Without a default volume profile, post-processing and other volume-based effects may not work correctly. This setting is configured in Project Settings > Graphics > URP.",
            "Assign a default Volume Profile in Project Settings > Graphics > URP section"
        );

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            var settings = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();

            // If settings is null, we're not in URP or settings haven't been initialized
            if (settings != null && settings.volumeProfile == null)
                yield return new RenderingSettingsIssue(URP0202);
        }
    }
}
