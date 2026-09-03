using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Renderer")]
    class InvalidRendererIndexAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0101 = nameof(URP0101);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0101,
            "URP: Invalid renderer index assigned",
            Areas.Quality,
            "One or more URP assets have a default renderer index that is out of bounds for their renderer list. This will cause rendering failures as the pipeline cannot find a valid renderer to use.",
            "Check the default renderer index in each URP asset and ensure it's within the valid range of available renderers"
        )
        {
            DefaultSeverity = Severity.Major,
            MessageFormat = "URP: Invalid renderer index assigned in {0}.asset in {1}",
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Check default render pipeline
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset && !IsRendererIndexValid(urpAsset))
            {
                if (!IsRendererIndexValid(urpAsset))
                    yield return new RenderingSettingsIssue(URP0101, assetName:urpAsset.name);
            }

            using (UnityEngine.Pool.ListPool<(int level, string name)>.Get(out var tmp))
            {
                QualitySettings.ForEach((level, name) =>
                {
                    if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrpAsset && !IsRendererIndexValid(qualityUrpAsset))
                    {
                        tmp.Add((level, name));
                    }
                });

                foreach (var (level, name) in tmp)
                    yield return new RenderingSettingsIssue(URP0101, level, name);
            }
        }

        static bool IsRendererIndexValid(UniversalRenderPipelineAsset urpAsset)
        {
            if (urpAsset == null)
                return true; // Nothing to check

            // Get the renderer data list
            var rendererDataList = urpAsset.m_RendererDataList;
            if (rendererDataList == null || rendererDataList.Length == 0)
            {
                // If there are no renderers, any index is invalid
                return false;
            }

            // Get the default renderer index
            int defaultRendererIndex = urpAsset.m_DefaultRendererIndex;

            // Check if the index is out of bounds
            if (defaultRendererIndex < 0 || defaultRendererIndex >= rendererDataList.Length)
            {
                return false;
            }

            // Check if the renderer at that index is null
            if (rendererDataList[defaultRendererIndex] == null)
            {
                return false;
            }

            return true;
        }
    }
}
