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
    class MSAAValidation : IRenderingSettingsAnalyzer
    {
        internal const string URP0005 = nameof(URP0005);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0005,
            "URP: MSAA is set to 4x or 8x",
            Areas.GPU | Areas.Quality,
            "<b>Anti Aliasing (MSAA)</b> is set to <b>4x</b> or <b>8x</b> in a URP Asset for mobile platforms. MSAA 4x/8x rendering can be intensive on low-end mobile GPUs.",
            "Decrease <b>Anti Aliasing (MSAA)</b> value to <b>2x</b> in the URP Asset."
        )
        {
            Platforms = new SerializableEnum<BuildTarget>[] {
                new(BuildTarget.Android),
                new(BuildTarget.iOS),
                new(BuildTarget.Switch)
            },
            MessageFormat = "URP: MSAA is set to 4x or 8x in {0}.asset in {1}",
            Fixer = FixMSAA
        };

        internal static int GetMsaaSampleCountSetting(RenderPipelineAsset renderPipeline)
        {
            return renderPipeline is UniversalRenderPipelineAsset urpAsset ? urpAsset.msaaSampleCount : -1;
        }

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GetMsaaSampleCountSetting(GraphicsSettings.defaultRenderPipeline) >= 4)
            {
                yield return new RenderingSettingsIssue(URP0005);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (GetMsaaSampleCountSetting(QualitySettings.renderPipeline) >= 4)
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0005, level, name);
            }
        }

        public static bool FixMSAA(ReportItem issue, AnalysisParams context)
        {
            bool fixedAny = false;

            if (GetMsaaSampleCountSetting(GraphicsSettings.defaultRenderPipeline) >= 4)
            {
                (GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset).msaaSampleCount = 2;
                EditorUtility.SetDirty(GraphicsSettings.defaultRenderPipeline);
                fixedAny = true;
            }

            QualitySettings.ForEach(() =>
            {
                if (GetMsaaSampleCountSetting(QualitySettings.renderPipeline) >= 4)
                {
                    (QualitySettings.renderPipeline as UniversalRenderPipelineAsset).msaaSampleCount = 2;
                    EditorUtility.SetDirty(QualitySettings.renderPipeline);
                    fixedAny = true;
                }
            });

            return fixedAny;
        }
    }
}
