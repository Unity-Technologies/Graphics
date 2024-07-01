using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Extensions for <see cref="BuildTarget"/>
    /// </summary>
    public static class BuildTargetExtensions
    {
        /// <summary>
        /// Obtains a list of the <see cref="RenderPipelineAsset"/> that are references into the settings either on <see cref="QualitySettings"/> or in <see cref="GraphicsSettings"/>
        /// </summary>
        /// <typeparam name="T">The type of <see cref="RenderPipelineAsset"/></typeparam>
        /// <param name="buildTarget">The <see cref="BuildTarget"/> to obtain the assets.</param>
        /// <param name="srpAssets">The output list of <see cref="RenderPipelineAsset"/> that are referenced by the platform.</param>
        /// <returns>false if there was an error fetching the <see cref="RenderPipelineAsset"/> for this <see cref="BuildTarget"/></returns>
        [MustUseReturnValue]
        public static bool TryGetRenderPipelineAssets<T>([DisallowNull] this BuildTarget buildTarget, List<T> srpAssets)
            where T : RenderPipelineAsset
        {
            if (srpAssets == null)
                return false;

            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);

            QualitySettings.GetRenderPipelineAssetsForPlatform<T>(namedBuildTarget.TargetName, out var buildPipelineAssets);
            srpAssets.AddRange(buildPipelineAssets);

            int count = QualitySettings.GetActiveQualityLevelsForPlatformCount(activeBuildTargetGroupName);
            var allQualityLevelsAreOverriden = buildPipelineAssets.Count == count;
            if (count == 0 || !allQualityLevelsAreOverriden)
            {
                // We need to check the fallback cases
                if (GraphicsSettings.defaultRenderPipeline is T srpAsset)
                    srpAssets.Add(srpAsset);
            }

            return true;
        }
    }
}
