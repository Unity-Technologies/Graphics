using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class MissingAssignedRenderPipeline : IRenderingSettingsAnalyzer
    {
        internal const string URP0001 = nameof(URP0001);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0001,
            "URP: Missing URP asset assigned",
            Areas.Quality,
            "Missing URP asset assigned. When a quality level has no render pipeline asset set, it will fall back to the default Graphics Settings asset. This can cause inconsistent rendering behavior across quality levels or fallback to Built-in Render Pipeline",
            "Assign a URP asset to all quality levels in Project Settings > Quality and a Default Render Pipeline in Project Settings > Graphics."
        )
        {
            MessageFormat = "URP: Missing URP asset assigned {0} in {1}"
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is not UniversalRenderPipelineAsset urpAsset)
            {
                yield return new RenderingSettingsIssue(URP0001);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is not UniversalRenderPipelineAsset qualityUrpAsset)
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0001, level, name);
            }
        }
    }
}
