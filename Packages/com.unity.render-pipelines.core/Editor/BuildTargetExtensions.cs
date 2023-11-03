using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Extensions for <see cref="BuildTarget"/>
    /// </summary>
    public static class BuildTargetExtensions
    {
        static bool NeedsToBeIncludedInBuildBylabel(RenderPipelineAsset asset, string label)
        {
            var labelList = AssetDatabase.GetLabels(asset);
            foreach (string item in labelList)
            {
                if (item == label)
                    return true;
            }
            return false;
        }

        static void AddAdditionalRenderPipelineAssetsIncludedForBuild<T>(HashSet<T> assetsList)
            where T : RenderPipelineAsset
        {
            var includer = GraphicsSettings.GetRenderPipelineSettings<IncludeAdditionalRPAssets>();
            if (includer == null)
                return;

            bool includeSceneDependencies = includer.includeReferencedInScenes;
            bool includeAssetsWithLabel = includer.includeAssetsByLabel;
            string labelToInclude = includer.labelToInclude;

            if (!includeSceneDependencies && !includeAssetsWithLabel)
                return;

            using (ListPool<string>.Get(out var assetsPaths))
            {
                assetsPaths.AddRange(AssetDatabaseHelper.FindAssetPaths<T>(".asset"));

                if (includeSceneDependencies)
                {
                    using (ListPool<string>.Get(out var scenesPaths))
                    {
                        foreach (var scene in EditorBuildSettings.scenes)
                            if (scene.enabled)
                                scenesPaths.Add(scene.path);
                    
                        // Get all enabled scenes path in the build settings.
                        HashSet<string> depsHash = new HashSet<string>(AssetDatabase.GetDependencies(scenesPaths.ToArray()));
                        for (int i = 0; i < assetsPaths.Count; ++i)
                        {
                            var assetPath = assetsPaths[i];
                            if (depsHash.Contains(assetPath))
                                assetsList.Add(AssetDatabase.LoadAssetAtPath<T>(assetPath));
                        }
                    }
                }

                if (includeAssetsWithLabel)
                {
                    for (int i = 0; i < assetsPaths.Count; ++i)
                    {
                        // Add the assets that are labeled to be included
                        var asset = AssetDatabase.LoadAssetAtPath<T>(assetsPaths[i]);
                        if (NeedsToBeIncludedInBuildBylabel(asset, labelToInclude))
                            assetsList.Add(asset);
                    }
                }
            }
        }

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

            QualitySettings.GetRenderPipelineAssetsForPlatform<T>(activeBuildTargetGroupName, out var buildPipelineAssets, out var allQualityLevelsAreOverriden);

            bool noQualityLevels = QualitySettings.GetActiveQualityLevelsForPlatformCount(activeBuildTargetGroupName) == 0;
            if (noQualityLevels || !allQualityLevelsAreOverriden)
            {
                // We need to check the fallback cases
                if (GraphicsSettings.defaultRenderPipeline is T srpAsset)
                    buildPipelineAssets.Add(srpAsset);
            }

            if (buildPipelineAssets.Count != 0)
                AddAdditionalRenderPipelineAssetsIncludedForBuild(buildPipelineAssets);
            
            srpAssets.AddRange(buildPipelineAssets);
            return srpAssets.Count != 0;
        }
    }
}
