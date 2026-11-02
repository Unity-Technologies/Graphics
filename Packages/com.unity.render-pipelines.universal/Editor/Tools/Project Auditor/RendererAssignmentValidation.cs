using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("URP Assets")]
    class NoRendererAssignedAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0002 = nameof(URP0002);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0002,
            "URP: URP Asset has no renderer assigned",
            Areas.Quality,
            "One or more Universal Render Pipeline assets have no renderers assigned. Without a renderer, the pipeline cannot render anything and will result in a blank screen or errors.",
            "Assign at least one renderer to each URP asset in Graphics and Quality Settings"
        )
        {
            DefaultSeverity = Severity.Major,
        };

        private bool CheckRenderers(UniversalRenderPipelineAsset urpAsset)
        {
            bool allHaveRenderers = true;
            if (urpAsset.m_RendererDataList == null || urpAsset.m_RendererDataList.Length == 0)
            {
                allHaveRenderers = false;
            }
            else
            {
                // Check if all entries are null
                bool hasValidRenderer = false;
                foreach (var renderer in urpAsset.m_RendererDataList)
                {
                    if (renderer != null)
                    {
                        hasValidRenderer = true;
                        break;
                    }
                }
                if (!hasValidRenderer)
                    allHaveRenderers = false;
            }

            return allHaveRenderers;
        }

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                if (!CheckRenderers(urpAsset))
                    yield return new RenderingSettingsIssue(URP0002);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset)
                    {
                        if (!CheckRenderers(qualityUrpAsset))
                            tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0002, level, name);
            }
        }
    }
}
