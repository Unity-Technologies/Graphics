using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDRPBuildData : IDisposable
    {
        static HDRPBuildData m_Instance = null;
        public static HDRPBuildData instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new(EditorUserBuildSettings.activeBuildTarget);

                return m_Instance;
            }
        }

        public bool buildingPlayerForHDRenderPipeline { get; private set; }

        public List<HDRenderPipelineAsset> renderPipelineAssets { get; private set; } = new List<HDRenderPipelineAsset>();
        public bool playerNeedRaytracing { get; private set; }
        public Dictionary<int, ComputeShader> rayTracingComputeShaderCache { get; private set; } = new();
        public Dictionary<int, ComputeShader> computeShaderCache { get; private set; } = new();

        public HDRPBuildData()
        {

        }

        public HDRPBuildData(BuildTarget buildTarget)
        {
            buildingPlayerForHDRenderPipeline = false;

            if (TryGetAllValidHDRPAssets(buildTarget, renderPipelineAssets))
            {
                foreach (var hdrpAsset in renderPipelineAssets)
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing)
                    {
                        playerNeedRaytracing = true;
                        break;
                    }
                }

                var hdrpGlobalSettingsInstance = HDRenderPipelineGlobalSettings.instance;

                var rtxResources = hdrpGlobalSettingsInstance.renderPipelineRayTracingResources;
                if (rtxResources != null)
                    rtxResources.ForEachFieldOfType<ComputeShader>(computeShader => rayTracingComputeShaderCache.Add(computeShader.GetInstanceID(), computeShader));

                var runtimeShaderResources = hdrpGlobalSettingsInstance.renderPipelineResources.shaders;
                runtimeShaderResources?.ForEachFieldOfType<ComputeShader>(computeShader => computeShaderCache.Add(computeShader.GetInstanceID(), computeShader));

                buildingPlayerForHDRenderPipeline = true;
            }

            m_Instance = this;
        }

        public void Dispose()
        {
            renderPipelineAssets?.Clear();
            rayTracingComputeShaderCache?.Clear();
            computeShaderCache?.Clear();
            playerNeedRaytracing = false;
            buildingPlayerForHDRenderPipeline = false;
            m_Instance = null;
        }

        internal static void AddAdditionalHDRenderPipelineAssetsIncludedForBuild(List<HDRenderPipelineAsset> assetsList)
        {
            using (ListPool<string>.Get(out var scenesPaths))
            {
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                    {
                        scenesPaths.Add(scene.path);
                    }
                }

                // Get all enabled scenes path in the build settings.
                HashSet<string> depsHash = new HashSet<string>(AssetDatabase.GetDependencies(scenesPaths.ToArray()));

                var guidRenderPipelineAssets = AssetDatabase.FindAssets("t:HDRenderPipelineAsset");

                for (int i = 0; i < guidRenderPipelineAssets.Length; ++i)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guidRenderPipelineAssets[i]);
                    if (depsHash.Contains(assetPath))
                    {
                        assetsList.Add(AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(assetPath));
                    }
                    else
                    {
                        // Add the HDRP assets that are labeled to be included
                        var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(assetPath);
                        if (HDEditorUtils.NeedsToBeIncludedInBuild(asset))
                        {
                            assetsList.Add(asset);
                        }
                    }
                }
            }
        }

        internal static void RemoveDuplicateAssets(List<HDRenderPipelineAsset> assetsList)
        {
            var uniques = new HashSet<HDRenderPipelineAsset>(assetsList);
            assetsList.Clear();
            assetsList.AddRange(uniques);
        }

        private static bool TryGetAllValidHDRPAssets(BuildTarget buildTarget, List<HDRenderPipelineAsset> assetsList)
        {
            // If the user has not selected HDRP in the ProjectSettings that means that we are not building for HDRP
            // Do not gather any other kind of HDRP asset from scenes or labeled to be included.
            bool hdrpConfiguredInSettings = buildTarget.TryGetRenderPipelineAssets<HDRenderPipelineAsset>(assetsList);
            if (hdrpConfiguredInSettings)
            {
                // Now that we know that we are building for HDRP, make sure that we add the other assets that must exist on the player
                AddAdditionalHDRenderPipelineAssetsIncludedForBuild(assetsList);

                // Avoid duplicates and return a clean list
                RemoveDuplicateAssets(assetsList);
            }

            return hdrpConfiguredInSettings;
        }
    }
}
