using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ProbeVolumeBuildProcessor : BuildPlayerProcessor
    {
        const string kTempAPVStreamingAssetsPath = "TempAPVStreamingAssets";

        string GetTempAPVStreamingAssetsPath()
        {
            var libraryPath = Path.GetFullPath("Library");
            return Path.Combine(libraryPath, kTempAPVStreamingAssetsPath);
        }

        void CopyStreamableAsset(ProbeVolumeStreamableAsset asset, string basePath)
        {
            var assetPath = asset.GetAssetPath();
            if (!File.Exists(assetPath))
            {
                Debug.LogError($"Missing APV data asset {assetPath}. Please make sure that the lighting has been baked properly.");
                return;
            }
            File.Copy(assetPath, Path.Combine(basePath, asset.assetGUID + ".bytes"));
        }

        void GetProbeVolumeProjectSettings(BuildPlayerContext buildPlayerContext, out ProbeVolumeSHBands maxSHBands, out ProbeVolumeSceneData sceneData)
        {
            // Grab all assets used for the build.
            List<RenderPipelineAsset> srpAssets = new List<RenderPipelineAsset>();
            buildPlayerContext.BuildPlayerOptions.target.TryGetRenderPipelineAssets(srpAssets);

            maxSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1;
            sceneData = null;

            foreach (var asset in srpAssets)
            {
                var probeVolumeEnabledRenderPipeline = asset as IProbeVolumeEnabledRenderPipeline;
                // If at least one asset needs L2 then we can return.
                if (probeVolumeEnabledRenderPipeline != null)
                {
                    if (probeVolumeEnabledRenderPipeline.maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        maxSHBands = ProbeVolumeSHBands.SphericalHarmonicsL2;

                    sceneData = probeVolumeEnabledRenderPipeline.probeVolumeSceneData;
                }
            }
        }

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            GetProbeVolumeProjectSettings(buildPlayerContext, out var maxSHBands, out var probeVolumeSceneData);

            // No APV in the project.
            if (probeVolumeSceneData == null)
                return;

            var tempStreamingAssetsPath = GetTempAPVStreamingAssetsPath();

            // Delete previously built data. This way we remove any data from scenes that are no longer in the build.
            if (Directory.Exists(tempStreamingAssetsPath))
                Directory.Delete(tempStreamingAssetsPath, recursive: true);

            Directory.CreateDirectory(tempStreamingAssetsPath);

            HashSet<ProbeVolumeBakingSet> processedBakingSets = new HashSet<ProbeVolumeBakingSet>();

            foreach (var scene in buildPlayerContext.BuildPlayerOptions.scenes)
            {
                var sceneGUID = AssetDatabase.AssetPathToGUID(scene);
                var bakingSet = probeVolumeSceneData.GetBakingSetForScene(sceneGUID);
                if (bakingSet != null)
                {
                    // Already processed (different scenes can belong to the same baking set).
                    if (processedBakingSets.Contains(bakingSet))
                        continue;

                    if (!bakingSet.cellSharedDataAsset.IsValid()) // Not baked
                        continue;

                    var bakingSetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(bakingSet));
                    var basePath = Path.Combine(tempStreamingAssetsPath, bakingSetGUID);

                    Directory.CreateDirectory(basePath);

                    CopyStreamableAsset(bakingSet.cellSharedDataAsset, basePath);
                    CopyStreamableAsset(bakingSet.cellBricksDataAsset, basePath);
                    // For now we always strip support data in build as it's mostly unsupported.
                    // Later we'll need a proper option to strip it or not.
                    bool stripSupportData = true;
                    if (!stripSupportData)
                        CopyStreamableAsset(bakingSet.cellSupportDataAsset, basePath);

                    foreach (var scenario in bakingSet.scenarios)
                    {
                        CopyStreamableAsset(scenario.Value.cellDataAsset, basePath);
                        if (maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            CopyStreamableAsset(scenario.Value.cellOptionalDataAsset, basePath);
                    }

                    processedBakingSets.Add(bakingSet);
                }
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(tempStreamingAssetsPath, ProbeGIBaking.kAPVStreamingAssetsPath);
        }
    }
}
