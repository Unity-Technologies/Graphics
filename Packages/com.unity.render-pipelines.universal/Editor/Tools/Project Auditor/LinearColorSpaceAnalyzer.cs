using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Other")]
    class LinearColorSpaceAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0401 = nameof(URP0401);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0401,
            "URP: Linear Color Space should be used",
            Areas.Quality,
            "The project is using Gamma color space. Linear color space is required for physically accurate rendering in URP. Gamma color space can cause incorrect lighting calculations and color artifacts.",
            "Change Color Space to Linear in Project Settings > Player > Other Settings > Color Space"
        )
        {
            Fixer = FixColorSpace,
            DefaultSeverity = Severity.Major,
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                yield return new RenderingSettingsIssue(URP0401);
        }

        public static bool FixColorSpace(ReportItem issue, AnalysisParams context)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            return true;
        }
    }
}
