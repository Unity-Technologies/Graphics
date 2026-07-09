using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Pool;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("SRP and Static Batching")]
    class StaticBatchingWithSRPBatcherAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0302 = nameof(URP0302);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0302,
            "URP: Static Batching conflicts with SRP Batcher",
            Areas.GPU | Areas.Quality,
            "Static Batching is enabled while SRP Batcher is also enabled. Static Batching prevents the SRP Batcher from working efficiently, reducing performance. When using SRP Batcher, Static Batching should be disabled.",
            "Disable Static Batching in Project Settings > Player > Other Settings > Static Batching"
        )
        {
            Fixer = FixStaticBatching,
            DefaultSeverity = Severity.Warning,
            MessageFormat = "URP: Static Batching conflicts with SRP Batcher enabled in {0}.asset in {1}",
        };


        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            bool staticBatchingEnabled = PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget);
            if (!staticBatchingEnabled)
                yield break; // Static Batching is disabled, no issue

            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset.useSRPBatcher)
            {
                yield return new RenderingSettingsIssue(URP0302, assetName: urpAsset.name);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset.useSRPBatcher)
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0302, level, name);
            }
        }

        public static bool FixStaticBatching(ReportItem issue, AnalysisParams context)
        {
            PlayerSettings.SetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, false);
            return true;
        }
    }
}
