using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ProbeVolumeBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        const string kTempAPVStreamingAssetsPath = "TempAPVStreamingAssets";

        string GetTempAPVStreamingAssetsPath()
        {
            var libraryPath = Path.GetFullPath("Library");
            return Path.Combine(libraryPath, kTempAPVStreamingAssetsPath);
        }

        // Include an asset in the build. The mechanism for doing so depends on whether we are using StreamingAssets path.
        static void IncludeStreamableAsset(ProbeVolumeStreamableAsset asset, string basePath, bool useStreamingAsset)
        {
            if (useStreamingAsset)
            {
                asset.ClearAssetReferenceForBuild();
                CopyStreamableAsset(asset, basePath);
            }
            else
            {
                asset.EnsureAssetLoaded();
            }
        }

        // Ensure that an asset is not included in the build.
        static void StripStreambleAsset(ProbeVolumeStreamableAsset asset)
        {
            asset.ClearAssetReferenceForBuild();
        }

        static void CopyStreamableAsset(ProbeVolumeStreamableAsset asset, string basePath)
        {
            var assetPath = asset.GetAssetPath();
            if (!File.Exists(assetPath))
            {
                Debug.LogError($"Missing APV data asset {assetPath}. Please make sure that the lighting has been baked properly.");
                return;
            }
            File.Copy(assetPath, Path.Combine(basePath, asset.assetGUID + ".bytes"));
        }

        void GetProbeVolumeProjectSettings(BuildTarget target, out bool supportProbeVolume, out ProbeVolumeSHBands maxSHBands)
        {
            // Grab all assets used for the build.
            List<RenderPipelineAsset> srpAssets = new List<RenderPipelineAsset>();
            target.TryGetRenderPipelineAssets(srpAssets);

            maxSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1;
            supportProbeVolume = false;

            foreach (var asset in srpAssets)
            {
                var probeVolumeEnabledRenderPipeline = asset as IProbeVolumeEnabledRenderPipeline;
                // If at least one asset needs L2 then we can return.
                if (probeVolumeEnabledRenderPipeline != null)
                {
                    supportProbeVolume |= probeVolumeEnabledRenderPipeline.supportProbeVolume;

                    if (probeVolumeEnabledRenderPipeline.maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                        maxSHBands = ProbeVolumeSHBands.SphericalHarmonicsL2;
                }
            }
        }

        // Keep track of which assets we touched during the build, so we can restore them after the build.
        private static HashSet<ProbeVolumeBakingSet> s_BakingSetsProcessedLastBuild = new();

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            GetProbeVolumeProjectSettings(buildPlayerContext.BuildPlayerOptions.target, out bool supportProbeVolume, out var maxSHBands);

            if (!supportProbeVolume)
                return;

            // We need to make sure the baking set map is properly initialized.
            // Since it's done only at APV init, we call it explicitely here in case APV was not used yet in this session.
            ProbeVolumeBakingSet.SyncBakingSets();

            var tempStreamingAssetsPath = GetTempAPVStreamingAssetsPath();

            // Delete previously built data. This way we remove any data from scenes that are no longer in the build.
            if (Directory.Exists(tempStreamingAssetsPath))
            {
                try
                {
                    Directory.Delete(tempStreamingAssetsPath, recursive: true);
                }
                catch
                {
                    // This can happen if for example the folder is write protected
                    // This is not ideal but don't block project build
                }
            }

            Directory.CreateDirectory(tempStreamingAssetsPath);

            s_BakingSetsProcessedLastBuild.Clear();
            foreach (var scene in buildPlayerContext.BuildPlayerOptions.scenes)
            {
                var sceneGUID = AssetDatabase.AssetPathToGUID(scene);
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(sceneGUID);
                if (bakingSet != null)
                {
                    // Already processed (different scenes can belong to the same baking set).
                    if (s_BakingSetsProcessedLastBuild.Contains(bakingSet))
                        continue;

                    if (!bakingSet.cellSharedDataAsset.IsValid()) // Not baked
                        continue;

                    var bakingSetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(bakingSet));
                    var basePath = Path.Combine(tempStreamingAssetsPath, bakingSetGUID);

                    Directory.CreateDirectory(basePath);

                    bool useStreamingAsset = !GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeGlobalSettings>().probeVolumeDisableStreamingAssets;

                    IncludeStreamableAsset(bakingSet.cellSharedDataAsset, basePath, useStreamingAsset);
                    IncludeStreamableAsset(bakingSet.cellBricksDataAsset, basePath, useStreamingAsset);
                    // For now we always strip support data in build as it's mostly unsupported.
                    // Later we'll need a proper option to strip it or not.
                    bool stripSupportData = true;
                    if (stripSupportData)
                        StripStreambleAsset(bakingSet.cellSupportDataAsset);
                    else
                        IncludeStreamableAsset(bakingSet.cellSupportDataAsset, basePath, useStreamingAsset);

                    foreach (var scenario in bakingSet.scenarios)
                    {
                        IncludeStreamableAsset(scenario.Value.cellDataAsset, basePath, useStreamingAsset);
                        if (maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            IncludeStreamableAsset(scenario.Value.cellOptionalDataAsset, basePath, useStreamingAsset);
                        else
                            StripStreambleAsset(scenario.Value.cellOptionalDataAsset);

                        if (bakingSet.bakedProbeOcclusion)
                            IncludeStreamableAsset(scenario.Value.cellProbeOcclusionDataAsset, basePath, useStreamingAsset);
                        else
                            StripStreambleAsset(scenario.Value.cellProbeOcclusionDataAsset);
                    }

                    s_BakingSetsProcessedLastBuild.Add(bakingSet);
                }
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(tempStreamingAssetsPath, AdaptiveProbeVolumes.kAPVStreamingAssetsPath);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (s_BakingSetsProcessedLastBuild == null || s_BakingSetsProcessedLastBuild.Count == 0)
                return;

            // Go over each asset reference we touched during the last build, make sure asset references are intact.
            foreach (var bakingSet in s_BakingSetsProcessedLastBuild)
            {
                bakingSet.cellBricksDataAsset.EnsureAssetLoaded();
                bakingSet.cellSharedDataAsset.EnsureAssetLoaded();
                bakingSet.cellSupportDataAsset.EnsureAssetLoaded();
                foreach (var scenario in bakingSet.scenarios)
                {
                    scenario.Value.cellDataAsset.EnsureAssetLoaded();
                    scenario.Value.cellOptionalDataAsset.EnsureAssetLoaded();
                    scenario.Value.cellProbeOcclusionDataAsset.EnsureAssetLoaded();
                }
            }

            s_BakingSetsProcessedLastBuild.Clear();
        }
    }
}
