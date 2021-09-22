#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    struct BakingCell
    {
        public ProbeReferenceVolume.Cell cell;
        public int[] probeIndices;
    }

    class BakingBatch
    {
        public int index;
        public Dictionary<int, List<Scene>> cellIndex2SceneReferences = new Dictionary<int, List<Scene>>();
        public List<BakingCell> cells = new List<BakingCell>();
        public Dictionary<Vector3, int> uniquePositions = new Dictionary<Vector3, int>();
        // Allow to get a mapping to subdiv level with the unique positions. It stores the minimum subdiv level found for a given position.
        // Can be probably done cleaner.
        public Dictionary<Vector3, int> uniqueBrickSubdiv = new Dictionary<Vector3, int>();

        private BakingBatch() {}

        public BakingBatch(int index)
        {
            this.index = index;
        }

        public void Clear()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(index, null);
            cells.Clear();
            cellIndex2SceneReferences.Clear();
        }

        public int uniqueProbeCount => uniquePositions.Keys.Count;
    }

    [InitializeOnLoad]
    partial class ProbeGIBaking
    {
        static bool m_IsInit = false;
        static BakingBatch m_BakingBatch;
        static ProbeReferenceVolumeProfile m_BakingProfile = null;
        static ProbeVolumeBakingProcessSettings m_BakingSettings;

        static int m_BakingBatchIndex = 0;

        static Bounds globalBounds = new Bounds();
        static bool hasFoundBounds = false;

        static bool onAdditionalProbesBakeCompletedCalled = false;

        static ProbeGIBaking()
        {
            Init();
        }

        public static void Init()
        {
            if (!m_IsInit)
            {
                m_IsInit = true;
                Lightmapping.lightingDataCleared += OnLightingDataCleared;
                Lightmapping.bakeStarted += OnBakeStarted;
            }
        }

        static void ClearBakingBatch()
        {
            if (m_BakingBatch != null)
                m_BakingBatch.Clear();

            m_BakingBatchIndex = 0;
        }

        static public void Clear()
        {
            var perSceneData = GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
            foreach (var data in perSceneData)
            {
                data.InvalidateAllAssets();
                var refVol = ProbeReferenceVolume.instance;
                refVol.Clear();
            }

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnLightingDataAssetCleared();
            }
        }

        public static void FindWorldBounds(out bool hasFoundInvalidSetup)
        {
            ProbeReferenceVolume.instance.clearAssetsOnVolumeClear = true;
            hasFoundInvalidSetup = false;

            var sceneData = ProbeReferenceVolume.instance.sceneData;
            HashSet<string> scenesToConsider = new HashSet<string>();

            var activeScene = SceneManager.GetActiveScene();
            var activeSet = sceneData.GetBakingSetForScene(activeScene);

            // We assume that all the bounds for all the scenes in the set have been set. However we also update the scenes that are currently loaded anyway for security.
            // and to have a new trigger to update the bounds we have.
            int openedScenesCount = SceneManager.sceneCount;
            for (int i=0; i<openedScenesCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                sceneData.OnSceneSaved(scene); // We need to perform the same actions we do when the scene is saved.
                if (sceneData.GetBakingSetForScene(scene) != activeSet && sceneData.SceneHasProbeVolumes(scene))
                {
                    Debug.LogError($"Scene at {scene.path} is loaded and has probe volumes, but not part of the same baking set as the active scene. This will result in an error. Please make sure all loaded scenes are part of the same baking sets.");
                }
            }


            hasFoundBounds = false;

            foreach (var sceneGUID in activeSet.sceneGUIDs)
            {
                bool hasProbeVolumes = false;
                if (sceneData.hasProbeVolumes.TryGetValue(sceneGUID, out hasProbeVolumes))
                {
                    if (hasProbeVolumes)
                    {
                        Bounds localBound;
                        if (sceneData.sceneBounds.TryGetValue(sceneGUID, out localBound))
                        {
                            if (hasFoundBounds)
                            {
                                globalBounds.Encapsulate(localBound);
                            }
                            else
                            {
                                globalBounds = localBound;
                                hasFoundBounds = true;
                            }
                        }
                    }
                }
                else // we need to open the scene to test.
                {
                    Debug.Log("The probe volume system couldn't find data for all the scenes in the baking set. Consider opening the scenes in the set and save them. Alternatively, bake the full set.");
                    hasFoundInvalidSetup = true;
                }
            }
        }

        static void SetBakingContext(ProbeVolumePerSceneData[] perSceneData)
        {
            // We need to make sure all scenes we are baking have the same profile. The same should be done for the baking settings, but we check only profile.
            // TODO: This should be ensured by the controlling panel, until we have that we need to assert.

            // To check what are  the scenes that have probe volume enabled we checks the ProbeVolumePerSceneData. We are guaranteed to have only one per scene.
            for (int i=0; i<perSceneData.Length; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(scene);
                Debug.Assert(profile != null, "Trying to bake a scene without a profile properly set.");

                if (i == 0)
                {
                    m_BakingProfile = profile;
                    Debug.Assert(ProbeReferenceVolume.instance.sceneData.BakeSettingsDefinedForScene(scene));
                    m_BakingSettings = ProbeReferenceVolume.instance.sceneData.GetBakeSettingsForScene(scene);
                }
                else
                {
                    Debug.Assert(m_BakingProfile.IsEquivalent(profile));
                }
            }
        }

        static void OnBakeStarted()
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            var pvList = GameObject.FindObjectsOfType<ProbeVolume>();
            if (pvList.Length == 0) return; // We have no probe volumes.

            FindWorldBounds(out bool hasFoundInvalidSetup);
            var perSceneDataList = GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
            if (perSceneDataList.Length == 0 || hasFoundInvalidSetup) return;

            SetBakingContext(perSceneDataList);

            if (m_BakingSettings.virtualOffsetSettings.useVirtualOffset)
                AddOccluders();

            RunPlacement();
        }

        static void CellCountInDirections(out Vector3Int minCellPositionXYZ, out Vector3Int maxCellPositionXYZ, float cellSizeInMeters)
        {
            minCellPositionXYZ = Vector3Int.zero;
            maxCellPositionXYZ = Vector3Int.zero;

            Vector3 center = Vector3.zero;
            var centeredMin = globalBounds.min - center;
            var centeredMax = globalBounds.max - center;

            minCellPositionXYZ.x = Mathf.FloorToInt(centeredMin.x / cellSizeInMeters);
            minCellPositionXYZ.y = Mathf.FloorToInt(centeredMin.y / cellSizeInMeters);
            minCellPositionXYZ.z = Mathf.FloorToInt(centeredMin.z / cellSizeInMeters);

            maxCellPositionXYZ.x = Mathf.CeilToInt(centeredMax.x / cellSizeInMeters);
            maxCellPositionXYZ.y = Mathf.CeilToInt(centeredMax.y / cellSizeInMeters);
            maxCellPositionXYZ.z = Mathf.CeilToInt(centeredMax.z / cellSizeInMeters);
        }

        static void BrickCountInDirections(out Vector3Int cellsInXYZ, float brickSizeInMeter)
        {
            cellsInXYZ = Vector3Int.zero;

            Vector3 center = Vector3.zero;
            var centeredMin = globalBounds.min - center;
            var centeredMax = globalBounds.max - center;

            cellsInXYZ.x = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.x / brickSizeInMeter)), Mathf.CeilToInt(Mathf.Abs(centeredMax.x / brickSizeInMeter))) * 2;
            cellsInXYZ.y = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.y / brickSizeInMeter)), Mathf.CeilToInt(Mathf.Abs(centeredMax.y / brickSizeInMeter))) * 2;
            cellsInXYZ.z = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(centeredMin.z / brickSizeInMeter)), Mathf.CeilToInt(Mathf.Abs(centeredMax.z / brickSizeInMeter))) * 2;
        }

        // NOTE: This is somewhat hacky and is going to likely be slow (or at least slower than it could).
        // It is only a first iteration of the concept that won't be as impactful on memory as other options.
        internal static void RevertDilation()
        {
            if (m_BakingProfile == null)
            {
                var perSceneDataList = GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
                if (perSceneDataList.Length == 0) return;
                SetBakingContext(perSceneDataList);
            }

            var dilationSettings = m_BakingSettings.dilationSettings;

            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                for (int i = 0; i < cell.validity.Length; ++i)
                {
                    if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && cell.validity[i] > dilationSettings.dilationValidityThreshold)
                    {
                        for (int k = 0; k < 9; ++k)
                        {
                            cell.sh[i][0, k] = 0.0f;
                            cell.sh[i][1, k] = 0.0f;
                            cell.sh[i][2, k] = 0.0f;
                        }
                    }
                }
            }
        }

        // Can definitively be optimized later on.
        // Also note that all the bookkeeping of all the reference volumes will likely need to change when we move to
        // proper UX.
        internal static void PerformDilation()
        {
            Dictionary<int, List<string>> cell2Assets = new Dictionary<int, List<string>>();
            var perSceneDataList = GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
            if (perSceneDataList.Length == 0) return;

            SetBakingContext(perSceneDataList);

            foreach (var sceneData in perSceneDataList)
            {
                var asset = sceneData.GetCurrentStateAsset();
                string assetPath = asset.GetSerializedFullPath();
                foreach (var cell in asset.cells)
                {
                    if (!cell2Assets.ContainsKey(cell.index))
                    {
                        cell2Assets.Add(cell.index, new List<string>());
                    }

                    cell2Assets[cell.index].Add(assetPath);
                }
                //// We need to queue the asset loading to make sure all is fine when calling refresh.
                //sceneData.QueueAssetLoading();
            }

            var dilationSettings = m_BakingSettings.dilationSettings;

            if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f)
            {
                // Force maximum sh bands to perform dilation, we need to store what sh bands was selected from the settings as we need to restore
                // post dilation.
                var prevSHBands = ProbeReferenceVolume.instance.shBands;
                ProbeReferenceVolume.instance.ForceSHBand(ProbeVolumeSHBands.SphericalHarmonicsL2);

                // TODO: This loop is very naive, can be optimized, but let's first verify if we indeed want this or not.
                for (int iterations = 0; iterations < dilationSettings.dilationIterations; ++iterations)
                {
                    // Make sure all is loaded before performing dilation.
                    ProbeReferenceVolume.instance.PerformPendingOperations(loadAllCells: true);

                    // Dilate all cells
                    List<ProbeReferenceVolume.Cell> dilatedCells = new List<ProbeReferenceVolume.Cell>(ProbeReferenceVolume.instance.cells.Values.Count);

                    foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                    {
                        PerformDilation(cell, dilationSettings);
                        dilatedCells.Add(cell);
                    }

                    foreach (var sceneData in perSceneDataList)
                    {
                        var asset = sceneData.GetCurrentStateAsset();
                        string assetPath = asset.GetSerializedFullPath();
                        if (asset != null)
                        {
                            ProbeReferenceVolume.instance.AddPendingAssetRemoval(asset);
                        }
                    }

                    // Make sure unloading happens.
                    ProbeReferenceVolume.instance.PerformPendingOperations();

                    Dictionary<string, bool> assetCleared = new Dictionary<string, bool>();
                    // Put back cells
                    foreach (var cell in dilatedCells)
                    {
                        foreach (var sceneData in perSceneDataList)
                        {
                            var asset = sceneData.GetCurrentStateAsset();

                            if (asset == null) continue;

                            var assetPath = asset.GetSerializedFullPath();
                            bool valueFound = false;
                            if (!assetCleared.TryGetValue(assetPath, out valueFound))
                            {
                                asset.cells.Clear();
                                assetCleared.Add(asset.GetSerializedFullPath(), true);
                                UnityEditor.EditorUtility.SetDirty(asset);
                            }

                            if (cell2Assets[cell.index].Contains(assetPath))
                            {
                                asset.cells.Add(cell);
                            }
                        }
                    }
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();

                    foreach (var sceneData in perSceneDataList)
                    {
                        sceneData.QueueAssetLoading();
                    }
                }

                // Need to restore the original sh bands
                ProbeReferenceVolume.instance.ForceSHBand(prevSHBands);
            }

        }

        static void OnAdditionalProbesBakeCompleted()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
            UnityEngine.Profiling.Profiler.BeginSample("OnAdditionalProbesBakeCompleted");

            var probeRefVolume = ProbeReferenceVolume.instance;
            var bakingCells = m_BakingBatch.cells;
            var numCells = bakingCells.Count;

            int numUniqueProbes = m_BakingBatch.uniqueProbeCount;

            var sh = new NativeArray<SphericalHarmonicsL2>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bakedProbeOctahedralDepth = new NativeArray<float>(numUniqueProbes * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bool validBakedProbes = UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(m_BakingBatch.index, sh, validity, bakedProbeOctahedralDepth);

            if (!validBakedProbes)
            {
                Debug.LogError("Lightmapper failed to produce valid probe data.  Please consider clearing lighting data and rebake.");
                return;
            }

            // Clear baked data
            Clear();

            // Make sure all pending operations are done (needs to be after the Clear to unload all previous scenes)
            probeRefVolume.PerformPendingOperations();

            onAdditionalProbesBakeCompletedCalled = true;

            var dilationSettings = m_BakingSettings.dilationSettings;
            // Fetch results of all cells
            for (int c = 0; c < numCells; ++c)
            {
                var cell = bakingCells[c].cell;

                if (cell.probePositions == null)
                    continue;

                int numProbes = cell.probePositions.Length;
                Debug.Assert(numProbes > 0);

                cell.sh = new SphericalHarmonicsL2[numProbes];
                cell.validity = new float[numProbes];
                cell.minSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();

                for (int i = 0; i < numProbes; ++i)
                {
                    int j = bakingCells[c].probeIndices[i];
                    SphericalHarmonicsL2 shv = sh[j];

                    int brickIdx = i / 64;
                    cell.minSubdiv = Mathf.Min(cell.minSubdiv, cell.bricks[brickIdx].subdivisionLevel);

                    // Compress the range of all coefficients but the DC component to [0..1]
                    // Upper bounds taken from http://ppsloan.org/publications/Sig20_Advances.pptx
                    // Divide each coefficient by DC*f to get to [-1,1] where f is from slide 33
                    for (int rgb = 0; rgb < 3; ++rgb)
                    {
                        var l0 = sh[j][rgb, 0];

                        if (l0 == 0.0f)
                            continue;

                        if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && validity[j] > dilationSettings.dilationValidityThreshold)
                        {
                            for (int k = 0; k < 9; ++k)
                            {
                                shv[rgb, k] = 0.0f;
                            }
                        }
                        else
                        {
                            // TODO: We're working on irradiance instead of radiance coefficients
                            //       Add safety margin 2 to avoid out-of-bounds values
                            float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
                            float l2scale = 3.5777088f; // 4/sqrt(5) * 2

                            // L_1^m
                            shv[rgb, 1] = sh[j][rgb, 1] / (l0 * l1scale * 2.0f) + 0.5f;
                            shv[rgb, 2] = sh[j][rgb, 2] / (l0 * l1scale * 2.0f) + 0.5f;
                            shv[rgb, 3] = sh[j][rgb, 3] / (l0 * l1scale * 2.0f) + 0.5f;

                            // L_2^-2
                            shv[rgb, 4] = sh[j][rgb, 4] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 5] = sh[j][rgb, 5] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 6] = sh[j][rgb, 6] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 7] = sh[j][rgb, 7] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 8] = sh[j][rgb, 8] / (l0 * l2scale * 2.0f) + 0.5f;

                            for (int coeff = 1; coeff < 9; ++coeff)
                                Debug.Assert(shv[rgb, coeff] >= 0.0f && shv[rgb, coeff] <= 1.0f);
                        }
                    }

                    SphericalHarmonicsL2Utils.SetL0(ref cell.sh[i], new Vector3(shv[0, 0], shv[1, 0], shv[2, 0]));
                    SphericalHarmonicsL2Utils.SetL1R(ref cell.sh[i], new Vector3(shv[0, 3], shv[0, 1], shv[0, 2]));
                    SphericalHarmonicsL2Utils.SetL1G(ref cell.sh[i], new Vector3(shv[1, 3], shv[1, 1], shv[1, 2]));
                    SphericalHarmonicsL2Utils.SetL1B(ref cell.sh[i], new Vector3(shv[2, 3], shv[2, 1], shv[2, 2]));

                    SphericalHarmonicsL2Utils.SetCoefficient(ref cell.sh[i], 4, new Vector3(shv[0, 4], shv[1, 4], shv[2, 4]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref cell.sh[i], 5, new Vector3(shv[0, 5], shv[1, 5], shv[2, 5]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref cell.sh[i], 6, new Vector3(shv[0, 6], shv[1, 6], shv[2, 6]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref cell.sh[i], 7, new Vector3(shv[0, 7], shv[1, 7], shv[2, 7]));
                    SphericalHarmonicsL2Utils.SetCoefficient(ref cell.sh[i], 8, new Vector3(shv[0, 8], shv[1, 8], shv[2, 8]));

                    cell.validity[i] = validity[j];
                }

                probeRefVolume.cells[cell.index] = cell;
                UnityEngine.Profiling.Profiler.EndSample();
            }

            m_BakingBatchIndex = 0;

            // Reset index
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);

            // Map from each scene to an existing reference volume
            var scene2Data = new Dictionary<Scene, ProbeVolumePerSceneData>();
            foreach (var data in GameObject.FindObjectsOfType<ProbeVolumePerSceneData>())
                scene2Data[data.gameObject.scene] = data;

            // Map from each reference volume to its asset
            var data2Asset = new Dictionary<ProbeVolumePerSceneData, ProbeVolumeAsset>();
            foreach (var data in scene2Data.Values)
            {
                data2Asset[data] = ProbeVolumeAsset.CreateAsset(data.gameObject.scene);
            }

            // Put cells into the respective assets
            foreach (var cell in probeRefVolume.cells.Values)
            {
                foreach (var scene in m_BakingBatch.cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a reference volume authoring component in it?
                    ProbeVolumePerSceneData data = null;
                    if (scene2Data.TryGetValue(scene, out data))
                    {
                        var asset = data2Asset[data];
                        asset.cells.Add(cell);
                        var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(scene);
                        asset.StoreProfileData(profile);
                        Debug.Assert(profile != null);
                        CellCountInDirections(out asset.minCellPosition, out asset.maxCellPosition, profile.cellSizeInMeters);
                        asset.globalBounds = globalBounds;
                    }
                }
            }

            // Connect the assets to their components
            foreach (var pair in data2Asset)
            {
                var data = pair.Key;
                var asset = pair.Value;

                // TODO: This will need to use the proper state, not default, when we have them.
                data.StoreAssetForState(ProbeVolumeState.Default, asset);

                if (UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.Iterative)
                {
                    UnityEditor.EditorUtility.SetDirty(data);
                    UnityEditor.EditorUtility.SetDirty(asset);
                }
            }

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnBakeCompleted();
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            probeRefVolume.clearAssetsOnVolumeClear = false;

            foreach (var data in data2Asset.Keys)
            {
                data.QueueAssetLoading();
            }

            // ---- Perform dilation ---
            PerformDilation();
        }

        static void OnLightingDataCleared()
        {
            Clear();
        }

        private static void DeduplicateProbePositions(in Vector3[] probePositions, in int[] brickSubdivLevel, Dictionary<Vector3, int> uniquePositions,
            Dictionary<Vector3, int> uniqueBrickSubdiv, out int[] indices)
        {
            indices = new int[probePositions.Length];
            int uniqueIndex = uniquePositions.Count;

            for (int i = 0; i < probePositions.Length; i++)
            {
                var pos = probePositions[i];
                var brickSubdiv = brickSubdivLevel[i];

                if (uniquePositions.TryGetValue(pos, out var index))
                {
                    indices[i] = index;
                    int oldBrickLevel = uniqueBrickSubdiv[pos];
                    int newBrickLevel = Math.Min(oldBrickLevel, brickSubdiv);
                    uniqueBrickSubdiv[pos] = newBrickLevel;
                }
                else
                {
                    uniquePositions[pos] = uniqueIndex;
                    indices[i] = uniqueIndex;
                    uniqueBrickSubdiv[pos] = brickSubdiv;
                    uniqueIndex++;
                }
            }
        }

        public static void OnBakeCompletedCleanup()
        {
            if (!onAdditionalProbesBakeCompletedCalled)
            {
                // Dequeue the call if something has failed.
                UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);
                CleanupOccluders();
            }
        }

        public static void RunPlacement()
        {
            onAdditionalProbesBakeCompletedCalled = false;
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;
            AdditionalGIBakeRequestsManager.instance.AddRequestsToLightmapper();
            UnityEditor.Lightmapping.bakeCompleted += OnBakeCompletedCleanup;

            ClearBakingBatch();

            // Subdivide the scene and place the bricks
            var ctx = PrepareProbeSubdivisionContext();
            var result = BakeBricks(ctx);

            // Compute probe positions and send them to the Lightmapper
            float brickSize = m_BakingProfile.minBrickSize;
            Matrix4x4 newRefToWS = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(brickSize, brickSize, brickSize));
            ApplySubdivisionResults(result, newRefToWS);
        }

        public static ProbeSubdivisionContext PrepareProbeSubdivisionContext()
        {
            ProbeSubdivisionContext ctx = new ProbeSubdivisionContext();

            // Prepare all the information in the scene for baking GI.
            Vector3 refVolOrigin = Vector3.zero; // TODO: This will need to be center of the world bounds.
            if (m_BakingProfile == null)
            {
                var perSceneDataList = GameObject.FindObjectsOfType<ProbeVolumePerSceneData>();
                if (perSceneDataList.Length == 0) return ctx;
                SetBakingContext(perSceneDataList);
            }
            ctx.Initialize(m_BakingProfile, refVolOrigin);

            return ctx;
        }

        public static ProbeSubdivisionResult BakeBricks(ProbeSubdivisionContext ctx)
        {
            var result = new ProbeSubdivisionResult();

            if (ctx.probeVolumes.Count == 0)
                return result;

            using (var gpuResources = ProbePlacement.AllocateGPUResources(ctx.probeVolumes.Count, ctx.profile.maxSubdivision))
            {
                // subdivide all the cells and generate brick positions
                foreach (var cell in ctx.cells)
                {
                    var scenesInCell = new HashSet<Scene>();

                    // Calculate overlaping probe volumes to avoid unnecessary work
                    var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)>();
                    foreach (var probeVolume in ctx.probeVolumes)
                    {
                        if (ProbeVolumePositioning.OBBIntersect(probeVolume.volume, cell.volume))
                        {
                            overlappingProbeVolumes.Add(probeVolume);
                            scenesInCell.Add(probeVolume.component.gameObject.scene);
                        }
                    }

                    // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                    var validRenderers = new List<(Renderer component, ProbeReferenceVolume.Volume volume)>();
                    foreach (var renderer in ctx.renderers)
                    {
                        var go = renderer.component.gameObject;
                        int rendererLayerMask = 1 << go.layer;

                        foreach (var probeVolume in overlappingProbeVolumes)
                        {
                            if (ProbeVolumePositioning.OBBIntersect(renderer.volume, probeVolume.volume)
                                && ProbeVolumePositioning.OBBIntersect(renderer.volume, cell.volume))
                            {
                                // Check if the renderer has a matching layer with probe volume
                                if ((probeVolume.component.objectLayerMask & rendererLayerMask) != 0)
                                {
                                    validRenderers.Add(renderer);
                                    scenesInCell.Add(go.scene);
                                }
                            }
                        }
                    }

                    // Skip empty cells
                    if (validRenderers.Count == 0 && overlappingProbeVolumes.Count == 0)
                        continue;

                    var bricks = ProbePlacement.SubdivideCell(cell.volume, ctx, gpuResources, validRenderers, overlappingProbeVolumes);

                    result.cellPositions.Add(cell.position);
                    result.bricksPerCells[cell.position] = bricks;
                    result.scenesPerCells[cell.position] = scenesInCell;
                }
            }

            return result;
        }

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        internal static void ConvertBricksToPositions(List<Brick> bricks, Vector3[] outProbePositions, Matrix4x4 refToWS, int[] outBrickSubdiv)
        {
            Matrix4x4 m = refToWS;
            int posIdx = 0;

            float[] ProbeOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
            ProbeOffsets[0] = 0.0f;
            float probeDelta = 1.0f / ProbeBrickPool.kBrickCellCount;
            for (int i = 1; i < ProbeBrickPool.kBrickProbeCountPerDim - 1; i++)
                ProbeOffsets[i] = i * probeDelta;
            ProbeOffsets[ProbeBrickPool.kBrickProbeCountPerDim - 1] = 1.0f;

            float minDist = ProbeReferenceVolume.instance.MinDistanceBetweenProbes();

            foreach (var b in bricks)
            {
                Vector3 offset = b.position;
                offset = m.MultiplyPoint(offset);
                float scale = ProbeReferenceVolume.CellSize(b.subdivisionLevel);
                Vector3 X = m.GetColumn(0) * scale;
                Vector3 Y = m.GetColumn(1) * scale;
                Vector3 Z = m.GetColumn(2) * scale;


                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    float zoff = ProbeOffsets[z];
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        float yoff = ProbeOffsets[y];
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            float xoff = ProbeOffsets[x];
                            Vector3 probePosition = offset + xoff * X + yoff * Y + zoff * Z;
                            // We need to round positions to the nearest multiple of the min distance between probes.
                            // Otherwise, the deduplication could fail because of floating point precision issue.
                            // This can lead to probes at the same position having different SH values, causing seams and other similar issues.
                            Vector3 roundedPosition = new Vector3(Mathf.Round(probePosition.x / minDist) * minDist,
                                Mathf.Round(probePosition.y / minDist) * minDist,
                                Mathf.Round(probePosition.z / minDist) * minDist);
                            outProbePositions[posIdx] = roundedPosition;
                            outBrickSubdiv[posIdx] = b.subdivisionLevel;
                            posIdx++;
                        }
                    }
                }
            }
        }

        public static void ApplySubdivisionResults(ProbeSubdivisionResult results, Matrix4x4 refToWS)
        {
            int index = 0;
            // For now we just have one baking batch. Later we'll have more than one for a set of scenes.
            // All probes need to be baked only once for the whole batch and not once per cell
            // The reason is that the baker is not deterministic so the same probe position baked in two different cells may have different values causing seams artefacts.
            m_BakingBatch = new BakingBatch(m_BakingBatchIndex++);

            foreach (var cellPos in results.cellPositions)
            {
                var bricks = results.bricksPerCells[cellPos];
                var cell = new ProbeReferenceVolume.Cell();

                cell.position = cellPos;
                cell.index = index++;
                if (bricks.Count > 0)
                {
                    // Convert bricks to positions
                    var probePositionsArr = new Vector3[bricks.Count * ProbeBrickPool.kBrickProbeCountTotal];
                    var brickSubdivLevels = new int[bricks.Count * ProbeBrickPool.kBrickProbeCountTotal];
                    ConvertBricksToPositions(bricks, probePositionsArr, refToWS, brickSubdivLevels);

                    int[] indices = null;
                    DeduplicateProbePositions(in probePositionsArr, in brickSubdivLevels, m_BakingBatch.uniquePositions, m_BakingBatch.uniqueBrickSubdiv, out indices);

                    cell.probePositions = probePositionsArr;
                    cell.bricks = bricks;

                    BakingCell bakingCell = new BakingCell();
                    bakingCell.cell = cell;
                    bakingCell.probeIndices = indices;

                    m_BakingBatch.cells.Add(bakingCell);
                    m_BakingBatch.cellIndex2SceneReferences[cell.index] = results.scenesPerCells[cellPos].ToList();
                }
            }


            // Move positions before sending them
            var positions = m_BakingBatch.uniquePositions.Keys.ToArray();
            VirtualOffsetSettings voSettings = m_BakingSettings.virtualOffsetSettings;
            if (voSettings.useVirtualOffset)
            {
                for (int i = 0; i < positions.Length; ++i)
                {
                    int subdivLevel = 0;
                    m_BakingBatch.uniqueBrickSubdiv.TryGetValue(positions[i], out subdivLevel);
                    float brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                    float searchDistance = (brickSize * m_BakingProfile.minBrickSize) / ProbeBrickPool.kBrickCellCount;

                    float scaleForSearchDist = voSettings.searchMultiplier;
                    positions[i] = PushPositionOutOfGeometry(positions[i], scaleForSearchDist * searchDistance, voSettings.outOfGeoOffset);
                }
                CleanupOccluders();
            }

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, positions);
        }
    }
}

#endif
