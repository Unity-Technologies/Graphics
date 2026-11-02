using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("SRP and Static Batching")]
    class SRPBatcherAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0301 = nameof(URP0301);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0301,
            "URP: SRP Batcher should be enabled",
            Areas.GPU | Areas.Quality,
            "Ensures that the Scriptable Render Pipeline Batcher is enabled on the URP Assets configured on Graphics and Quality Settings. This improves rendering performance significantly in many projects.",
            "Enable the SRP Batcher on the used URP assets on Graphics and Quality Settings"
        )
        {
            Fixer = FixSRPBatcher,
            MessageFormat = "URP: SRPBatcher disabled in {0}.asset in {1}",
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset && !urpAsset.useSRPBatcher)
            {
                yield return new RenderingSettingsIssue(URP0301, assetName: urpAsset.name);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset && !urpAsset.useSRPBatcher)
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0301, level, name);
            }
        }

        public static bool FixSRPBatcher(ReportItem issue, AnalysisParams context)
        {
            bool fixedAny = false;

            // Fix default render pipeline in Graphics Settings
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultUrpAsset)
            {
                defaultUrpAsset.useSRPBatcher = true;
                EditorUtility.SetDirty(defaultUrpAsset);
                fixedAny = true;
            }

            // Fix all quality level render pipelines
            QualitySettings.ForEach(() =>
            {
                if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset)
                {
                    urpAsset.useSRPBatcher = true;
                    EditorUtility.SetDirty(urpAsset);
                    fixedAny = true;
                }
            });

            return fixedAny;
        }
    }
}
