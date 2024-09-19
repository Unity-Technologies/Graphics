using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering
{
    class ProbeVolumeBuildProcessor : BuildPlayerProcessor, IProcessSceneWithReport
    {
        const string kTempAPVStreamingAssetsPath = "TempAPVStreamingAssets";

        string GetTempAPVStreamingAssetsPath()
        {
            var libraryPath = Path.GetFullPath("Library");
            return Path.Combine(libraryPath, kTempAPVStreamingAssetsPath);
        }

        void PrepareStreamableAsset(ProbeVolumeStreamableAsset asset, string basePath, bool useStreamingAsset)
        {
            asset.UpdateAssetReference(useStreamingAsset);

            if (useStreamingAsset)
                CopyStreamableAsset(asset, basePath);
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

            HashSet<ProbeVolumeBakingSet> processedBakingSets = new HashSet<ProbeVolumeBakingSet>();

            foreach (var scene in buildPlayerContext.BuildPlayerOptions.scenes)
            {
                var sceneGUID = AssetDatabase.AssetPathToGUID(scene);
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(sceneGUID);
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

                    bool useStreamingAsset = !GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeGlobalSettings>().probeVolumeDisableStreamingAssets;

                    PrepareStreamableAsset(bakingSet.cellSharedDataAsset, basePath, useStreamingAsset);
                    PrepareStreamableAsset(bakingSet.cellBricksDataAsset, basePath, useStreamingAsset);
                    // For now we always strip support data in build as it's mostly unsupported.
                    // Later we'll need a proper option to strip it or not.
                    bool stripSupportData = true;
                    if (!stripSupportData)
                        PrepareStreamableAsset(bakingSet.cellSupportDataAsset, basePath, useStreamingAsset);

                    foreach (var scenario in bakingSet.scenarios)
                    {
                        PrepareStreamableAsset(scenario.Value.cellDataAsset, basePath, useStreamingAsset);
                        if (maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            PrepareStreamableAsset(scenario.Value.cellOptionalDataAsset, basePath, useStreamingAsset);
                        PrepareStreamableAsset(scenario.Value.cellProbeOcclusionDataAsset, basePath, useStreamingAsset);
                    }

                    processedBakingSets.Add(bakingSet);
                }
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(tempStreamingAssetsPath, AdaptiveProbeVolumes.kAPVStreamingAssetsPath);
        }

        private static bool IsBundleBuild(BuildReport report, bool isPlaying)
        {
            // We are entering playmode, so not building a bundle.
            if (isPlaying)
                return false;

            // Addressable builds do not provide a BuildReport. Because the Addressables package
            // only supports AssetBundle builds, we infer that this is not a player build.
            if (report == null)
                return true;

            return report.summary.buildType == BuildType.AssetBundle;
        }

        // This codepath handles the case of building asset bundles, i.e. not a full player build. It updates the references
        // to individual data assets in the baking sets for each scene, such that the assets are included in the bundle.
        public override int callbackOrder => 1;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Only run for bundle builds.
            if (!IsBundleBuild(report, Application.isPlaying))
                return;

            // Only run when APV is enabled.
            GetProbeVolumeProjectSettings(EditorUserBuildSettings.activeBuildTarget, out bool supportProbeVolume, out var maxSHBands);
            if (!supportProbeVolume)
                return;

            // Reload the map from scene to baking set if we couldn't find the specific baking set.
            if (ProbeVolumeBakingSet.sceneToBakingSet == null || ProbeVolumeBakingSet.sceneToBakingSet.Count == 0)
                ProbeVolumeBakingSet.SyncBakingSets();

            // Get the baking set for the scene.
            var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(scene.GetGUID());
            if (bakingSet == null || !bakingSet.cellSharedDataAsset.IsValid())
                return;

            bool useStreamingAsset = !GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeGlobalSettings>().probeVolumeDisableStreamingAssets;
            if (useStreamingAsset)
            {
                Debug.LogWarning(
                    "Attempted to build an Asset Bundle containing Adaptive Probe Volume data, but streaming assets are enabled. This is unsupported. " +
                    "To use Adaptive Probe Volumes with Asset Bundles, please check 'Probe Volume Disable Streaming Assets' under Graphics Settings.");
            }

            // Update all the asset references.
            bakingSet.cellSharedDataAsset.UpdateAssetReference(useStreamingAsset);
            bakingSet.cellBricksDataAsset.UpdateAssetReference(useStreamingAsset);

            bool stripSupportData = true;
            if (!stripSupportData)
                bakingSet.cellSupportDataAsset.UpdateAssetReference(false);

            foreach (var scenario in bakingSet.scenarios)
            {
                scenario.Value.cellDataAsset.UpdateAssetReference(useStreamingAsset);
                if (maxSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    scenario.Value.cellOptionalDataAsset.UpdateAssetReference(useStreamingAsset);
                scenario.Value.cellProbeOcclusionDataAsset.UpdateAssetReference(useStreamingAsset);
            }
        }
    }
}
