using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Unity.ProjectAuditor.Editor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Renderer")]
    class DuplicateRendererFeaturesAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0103 = nameof(URP0103);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0103,
            "URP: Duplicate features with DisallowMultipleRendererFeature",
            Areas.Quality,
            "Some Renderer Features are marked with [DisallowMultipleRendererFeature] attribute but appear multiple times in a renderer. This can cause unexpected behavior or rendering issues.",
            "Remove duplicate renderer features that are marked with DisallowMultipleRendererFeature attribute"
        )
        {
            DefaultSeverity = Severity.Major,
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            var rendererDataAssets = URPProjectAuditorUtilities.GetAllRendererDataAssets();

            foreach (var rendererData in rendererDataAssets)
            {
                if (rendererData == null || rendererData.rendererFeatures == null)
                    continue;

                var featureTypeCounts = new Dictionary<Type, int>();

                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature == null)
                        continue;

                    var featureType = feature.GetType();
                    var attribute = featureType.GetCustomAttribute<DisallowMultipleRendererFeature>();

                    if (attribute != null)
                    {
                        if (!featureTypeCounts.ContainsKey(featureType))
                            featureTypeCounts[featureType] = 0;

                        featureTypeCounts[featureType]++;

                        if (featureTypeCounts[featureType] > 1)
                            yield return new RenderingSettingsIssue(URP0103); // Found duplicate
                    }
                }
            }  
        }
    }
}
