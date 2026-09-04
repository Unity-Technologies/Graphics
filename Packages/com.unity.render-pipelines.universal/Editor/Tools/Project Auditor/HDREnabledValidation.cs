using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class HDREnabledValidation : IRenderingSettingsAnalyzer
    {
        internal const string URP0004 = nameof(URP0004);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0004,
            "URP: HDR is enabled",
            Areas.GPU | Areas.Quality,
            "<b>HDR</b> (High Dynamic Range) is enabled in a URP Asset for mobile platforms. HDR rendering can be very intensive on low-end mobile GPUs.",
            "Disable <b>HDR</b> in the URP Asset."
        )
        {
            Platforms = new SerializableEnum<BuildTarget>[] {
                new(BuildTarget.Android),
                new(BuildTarget.iOS),
                new(BuildTarget.Switch)
            },
            MessageFormat = "URP: HDR is enabled in {0}.asset in {1}",
            Fixer = FixHDR
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset.supportsHDR)
            {
                yield return new RenderingSettingsIssue(URP0004, assetName: urpAsset.name);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset && qualityUrpAsset.supportsHDR)
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0004, level, name);
            }
        }

        public static bool FixHDR(ReportItem issue, AnalysisParams context)
        {
            bool fixedAny = false;

            var configuredAssets = GraphicsSettings.allConfiguredRenderPipelines;

            foreach (var asset in configuredAssets)
            {
                if (asset is UniversalRenderPipelineAsset urpAsset && urpAsset.supportsHDR)
                {
                    urpAsset.supportsHDR = false;
                    EditorUtility.SetDirty(urpAsset);
                    fixedAny = true;
                }
            }

            return fixedAny;
        }
    }
}
