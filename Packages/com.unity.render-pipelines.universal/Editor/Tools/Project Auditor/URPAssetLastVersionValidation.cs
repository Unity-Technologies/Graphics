using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class URPAssetLastVersionValidation : IRenderingSettingsAnalyzer
    {
        internal const string URP0003 = nameof(URP0003);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0003,
            "URP: URP Asset are not at latest version",
            Areas.Quality,
            "One or more Universal Render Pipeline assets have no been upgraded to latest version.",
            "Open the asset in the inspector to upgrade them to latest version."
        )
        {
            DefaultSeverity = Severity.Major,
            MessageFormat = "URP: URP Asset is not at latest version in {0}.asset in {1}",
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset && !urpAsset.IsAtLastVersion())
            {
                yield return new RenderingSettingsIssue(URP0003);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset && !urpAsset.IsAtLastVersion())
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0003, level, name);
            }
        }
    }
}
