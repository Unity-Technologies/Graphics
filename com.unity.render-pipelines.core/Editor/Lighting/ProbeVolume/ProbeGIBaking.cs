using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEditor;

using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using CellInfo = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.CellInfo;
using Cell = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.Cell;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    struct BakingCell
    {
        public Vector3Int position;
        public int index;

        public Brick[] bricks;
        public Vector3[] probePositions;
        public SphericalHarmonicsL2[] sh;
        public float[] validity;
        public Vector3[] offsetVectors;

        public int minSubdiv;
        public int indexChunkCount;
        public int shChunkCount;

        public int[] probeIndices;

        internal int GetBakingHashCode()
        {
            int hash = position.GetHashCode();
            hash = hash * 23 + minSubdiv.GetHashCode();
            hash = hash * 23 + indexChunkCount.GetHashCode();
            hash = hash * 23 + shChunkCount.GetHashCode();

            foreach (var brick in bricks)
            {
                hash = hash * 23 + brick.position.GetHashCode();
                hash = hash * 23 + brick.subdivisionLevel.GetHashCode();
            }
            return hash;
        }
    }

    class BakingBatch
    {
        public int index;
        public Dictionary<int, List<Scene>> cellIndex2SceneReferences = new Dictionary<int, List<Scene>>();
        public List<BakingCell> cells = new List<BakingCell>();
        public Dictionary<Vector3, int> uniquePositions = new Dictionary<Vector3, int>();
        public Vector3[] virtualOffsets;
        // Allow to get a mapping to subdiv level with the unique positions. It stores the minimum subdiv level found for a given position.
        // Can be probably done cleaner.
        public Dictionary<Vector3, int> uniqueBrickSubdiv = new Dictionary<Vector3, int>();

        private BakingBatch() { }

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

        static Dictionary<Vector3Int, int> m_CellPosToIndex = new Dictionary<Vector3Int, int>();
        static Dictionary<int, BakingCell> m_BakedCells = new Dictionary<int, BakingCell>();

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
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.Clear();

            ProbeReferenceVolume.instance.Clear();

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
                probeVolume.OnLightingDataAssetCleared();
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
            for (int i = 0; i < openedScenesCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
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

        static void SetBakingContext(List<ProbeVolumePerSceneData> perSceneData)
        {
            // We need to make sure all scenes we are baking have the same profile. The same should be done for the baking settings, but we check only profile.
            // TODO: This should be ensured by the controlling panel, until we have that we need to assert.

            // To check what are  the scenes that have probe volume enabled we checks the ProbeVolumePerSceneData. We are guaranteed to have only one per scene.
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(scene);
                Debug.Assert(profile != null, "Trying to bake a scene without a profile properly set.");

                data.SetBakingState(ProbeReferenceVolume.instance.bakingState);

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
            if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return;

            var pvList = GameObject.FindObjectsOfType<ProbeVolume>();
            if (pvList.Length == 0) return; // We have no probe volumes.

            FindWorldBounds(out bool hasFoundInvalidSetup);
            if (hasFoundInvalidSetup) return;

            SetBakingContext(ProbeReferenceVolume.instance.perSceneDataList);

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
                if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return;
                SetBakingContext(ProbeReferenceVolume.instance.perSceneDataList);
            }

            var dilationSettings = m_BakingSettings.dilationSettings;
            var blackProbe = new SphericalHarmonicsL2();

            foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
            {
                var cell = cellInfo.cell;
                for (int i = 0; i < cell.validity.Length; ++i)
                {
                    if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && cell.validity[i] > dilationSettings.dilationValidityThreshold)
                    {
                        WriteToShaderCoeffsL0L1(blackProbe, cell.shL0L1Data, i * ProbeVolumeAsset.kL0L1ScalarCoefficientsCount);

                        if (cell.shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            WriteToShaderCoeffsL2(blackProbe, cell.shL2Data, i * ProbeVolumeAsset.kL2ScalarCoefficientsCount);
                    }
                }
            }
        }

        // Can definitively be optimized later on.
        // Also note that all the bookkeeping of all the reference volumes will likely need to change when we move to
        // proper UX.
        internal static void PerformDilation()
        {
            var perSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
            if (perSceneDataList.Count == 0) return;

            Dictionary<int, List<string>> cell2Assets = new Dictionary<int, List<string>>();
            List<CellInfo> tempLoadedCells = new List<CellInfo>();

            var prv = ProbeReferenceVolume.instance;

            SetBakingContext(perSceneDataList);

            foreach (var sceneData in perSceneDataList)
            {
                var asset = sceneData.asset;
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
                // Make sure all assets are loaded before performing dilation.
                prv.PerformPendingOperations();

                // Force maximum sh bands to perform dilation, we need to store what sh bands was selected from the settings as we need to restore
                // post dilation.
                var prevSHBands = prv.shBands;
                prv.ForceSHBand(ProbeVolumeSHBands.SphericalHarmonicsL2);

                // TODO: This loop is very naive, can be optimized, but let's first verify if we indeed want this or not.
                for (int iterations = 0; iterations < dilationSettings.dilationIterations; ++iterations)
                {
                    // Try to load all available cells to the GPU. Might not succeed depending on the memory budget.
                    prv.LoadAllCells();

                    // Dilate all cells
                    List<ProbeReferenceVolume.Cell> dilatedCells = new List<ProbeReferenceVolume.Cell>(prv.cells.Values.Count);
                    bool everythingLoaded = !prv.hasUnloadedCells;

                    if (everythingLoaded)
                    {
                        foreach (var cellInfo in prv.cells.Values)
                        {
                            var cell = cellInfo.cell;
                            PerformDilation(cell, dilationSettings);
                            dilatedCells.Add(cell);
                        }
                    }
                    else
                    {
                        // When everything does not fit in memory, we are going to dilate one cell at a time.
                        // To do so, we load the cell and all its neighbours and then dilate.
                        // This is an inefficient use of memory but for now most of the time is spent in reading back the result anyway so it does not introduce any performance regression.

                        // Free All memory to make room for each cell and its neighbors for dilation.
                        prv.UnloadAllCells();

                        foreach (var cellInfo in prv.cells.Values)
                        {
                            tempLoadedCells.Clear();

                            var cell = cellInfo.cell;
                            var cellPos = cell.position;
                            // Load the cell and all its neighbors before doing dilation.
                            for (int x = -1; x <= 1; ++x)
                                for (int y = -1; y <= 1; ++y)
                                    for (int z = -1; z <= 1; ++z)
                                    {
                                        Vector3Int pos = cellPos + new Vector3Int(x, y, z);
                                        if (m_CellPosToIndex.TryGetValue(pos, out var cellToLoadIndex))
                                        {
                                            if (prv.cells.TryGetValue(cellToLoadIndex, out var cellToLoad))
                                            {
                                                if (prv.LoadCell(cellToLoad))
                                                {
                                                    tempLoadedCells.Add(cellToLoad);
                                                }
                                                else
                                                    Debug.LogError($"Not enough memory to perform dilation for cell {cell.index}");
                                            }
                                        }
                                    }

                            PerformDilation(cell, dilationSettings);
                            dilatedCells.Add(cell);

                            // Free memory again.
                            foreach (var cellToUnload in tempLoadedCells)
                                prv.UnloadCell(cellToUnload);
                        }
                    }

                    foreach (var sceneData in perSceneDataList)
                        prv.AddPendingAssetRemoval(sceneData.asset);

                    // Make sure unloading happens.
                    prv.PerformPendingOperations();

                    // Commit cell changes
                    HashSet<string> assetsCommitted = new HashSet<string>();
                    foreach (var cell in dilatedCells)
                    {
                        foreach (var sceneData in perSceneDataList)
                        {
                            var assetPath = sceneData.asset.GetSerializedFullPath();
                            if (cell2Assets[cell.index].Contains(assetPath))
                            {
                                if (!assetsCommitted.Contains(assetPath))
                                {
                                    WritebackModifiedCellsData(sceneData);
                                    assetsCommitted.Add(assetPath);
                                }
                            }
                        }
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    foreach (var sceneData in perSceneDataList)
                    {
                        sceneData.QueueAssetLoading();
                    }
                }

                // Need to restore the original sh bands
                prv.ForceSHBand(prevSHBands);
            }
        }

        static void OnAdditionalProbesBakeCompleted()
        {
            using var pm = new ProfilerMarker("OnAdditionalProbesBakeCompleted").Auto();

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;

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

            m_CellPosToIndex.Clear();
            m_BakedCells.Clear();

            // Clear baked data
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.QueueAssetRemoval();
            ProbeReferenceVolume.instance.Clear();

            // Make sure all pending operations are done (needs to be after the Clear to unload all previous scenes)
            probeRefVolume.PerformPendingOperations();

            // Use the globalBounds we just computed, as the one in probeRefVolume doesn't include scenes that have never been baked
            probeRefVolume.globalBounds = globalBounds;

            onAdditionalProbesBakeCompletedCalled = true;

            var dilationSettings = m_BakingSettings.dilationSettings;
            var virtualOffsets = m_BakingBatch.virtualOffsets;

            // Fetch results of all cells
            for (int c = 0; c < numCells; ++c)
            {
                var cell = bakingCells[c];

                m_CellPosToIndex.Add(cell.position, cell.index);

                if (cell.probePositions == null)
                    continue;

                int numProbes = cell.probePositions.Length;
                Debug.Assert(numProbes > 0);

                cell.sh = new SphericalHarmonicsL2[numProbes];
                cell.validity = new float[numProbes];
                cell.offsetVectors = new Vector3[virtualOffsets != null ? numProbes : 0];
                cell.minSubdiv = probeRefVolume.GetMaxSubdivision();

                for (int i = 0; i < numProbes; ++i)
                {
                    int j = cell.probeIndices[i];

                    if (virtualOffsets != null)
                        cell.offsetVectors[i] = virtualOffsets[j];

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

                cell.indexChunkCount = probeRefVolume.GetNumberOfBricksAtSubdiv(cell.position, cell.minSubdiv, out _, out _) / ProbeBrickIndex.kIndexChunkSize;
                cell.shChunkCount = ProbeBrickPool.GetChunkCount(cell.bricks.Length);

                m_BakedCells[cell.index] = cell;
            }

            m_BakingBatchIndex = 0;

            // Reset index
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);

            // Map from each scene to its per scene data, and create a new asset for each scene
            var scene2Data = new Dictionary<Scene, ProbeVolumePerSceneData>();
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                data.asset = ProbeVolumeAsset.CreateAsset(data);
                data.states.TryAdd(ProbeReferenceVolume.instance.bakingState, default);
                scene2Data[data.gameObject.scene] = data;
            }

            // Allocate cells to the respective assets
            var data2BakingCells = new Dictionary<ProbeVolumePerSceneData, List<BakingCell>>();
            foreach (var cell in m_BakedCells.Values)
            {
                foreach (var scene in m_BakingBatch.cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a reference volume authoring component in it?
                    if (scene2Data.TryGetValue(scene, out var data))
                    {
                        if (!data2BakingCells.TryGetValue(data, out var bakingCellsList))
                            bakingCellsList = data2BakingCells[data] = new();

                        bakingCellsList.Add(cell);

                        var asset = data.asset;
                        var profile = probeRefVolume.sceneData.GetProfileForScene(scene);
                        asset.StoreProfileData(profile);
                        CellCountInDirections(out asset.minCellPosition, out asset.maxCellPosition, profile.cellSizeInMeters);
                        asset.globalBounds = globalBounds;

                        EditorUtility.SetDirty(asset);
                    }
                }
            }

            // Convert baking cells to runtime cells
            foreach ((var data, var bakingCellsList) in data2BakingCells)
            {
                // NOTE: Right now we always write out both L0L1 and L2 data, regardless of which Probe Volume SH Bands lighting setting
                //       happens to be active at the time of baking (probeRefVolume.shBands).
                //
                // TODO: Explicitly add an option for storing L2 data to bake sets. Freely mixing cells with different bands
                //       availability is already supported by runtime.
                //
                data.asset.bands = ProbeVolumeSHBands.SphericalHarmonicsL2;
                WriteBakingCells(data, bakingCellsList);
                data.ResolveCells();
            }

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.Iterative)
                {
                    EditorUtility.SetDirty(data);
                    EditorUtility.SetDirty(data.asset);
                }
            }

            var probeVolumes = GameObject.FindObjectsOfType<ProbeVolume>();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnBakeCompleted();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            probeRefVolume.clearAssetsOnVolumeClear = false;

            m_BakingBatch = null;

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.QueueAssetLoading();

            // ---- Perform dilation ---
            PerformDilation();

            // Mark old bakes as out of date if needed
            if (EditorWindow.HasOpenInstances<ProbeVolumeBakingWindow>())
            {
                var window = (ProbeVolumeBakingWindow)EditorWindow.GetWindow(typeof(ProbeVolumeBakingWindow));
                window.UpdateBakingStatesStatuses(ProbeReferenceVolume.instance.bakingState);
            }
        }

        static void OnLightingDataCleared()
        {
            Clear();
        }

        static ushort SHFloatToHalf(float value)
        {
            return Mathf.FloatToHalf(value);
        }

        static byte SHFloatToByte(float value)
        {
            return (byte)(Mathf.Clamp(value, 0.0f, 1.0f) * 255.0f);
        }

        static void WriteToShaderCoeffsL0L1(in SphericalHarmonicsL2 sh, NativeArray<float> shaderCoeffsL0L1, int offset)
        {
            shaderCoeffsL0L1[offset + 0] = sh[0, 0]; shaderCoeffsL0L1[offset + 1] = sh[1, 0]; shaderCoeffsL0L1[offset + 2] = sh[2, 0]; shaderCoeffsL0L1[offset + 3] = sh[0, 1];
            shaderCoeffsL0L1[offset + 4] = sh[1, 1]; shaderCoeffsL0L1[offset + 5] = sh[1, 2]; shaderCoeffsL0L1[offset + 6] = sh[1, 3]; shaderCoeffsL0L1[offset + 7] = sh[0, 2];
            shaderCoeffsL0L1[offset + 8] = sh[2, 1]; shaderCoeffsL0L1[offset + 9] = sh[2, 2]; shaderCoeffsL0L1[offset + 10] = sh[2, 3]; shaderCoeffsL0L1[offset + 11] = sh[0, 3];
        }

        static void WriteToShaderCoeffsL0L1(in SphericalHarmonicsL2 sh, NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz, int offset)
        {
            shaderCoeffsL0L1Rx[offset + 0] = SHFloatToHalf(sh[0, 0]); shaderCoeffsL0L1Rx[offset + 1] = SHFloatToHalf(sh[1, 0]); shaderCoeffsL0L1Rx[offset + 2] = SHFloatToHalf(sh[2, 0]); shaderCoeffsL0L1Rx[offset + 3] = SHFloatToHalf(sh[0, 1]);
            shaderCoeffsL1GL1Ry[offset + 0] = SHFloatToByte(sh[1, 1]); shaderCoeffsL1GL1Ry[offset + 1] = SHFloatToByte(sh[1, 2]); shaderCoeffsL1GL1Ry[offset + 2] = SHFloatToByte(sh[1, 3]); shaderCoeffsL1GL1Ry[offset + 3] = SHFloatToByte(sh[0, 2]);
            shaderCoeffsL1BL1Rz[offset + 0] = SHFloatToByte(sh[2, 1]); shaderCoeffsL1BL1Rz[offset + 1] = SHFloatToByte(sh[2, 2]); shaderCoeffsL1BL1Rz[offset + 2] = SHFloatToByte(sh[2, 3]); shaderCoeffsL1BL1Rz[offset + 3] = SHFloatToByte(sh[0, 3]);
        }

        static void WriteToShaderCoeffsL2(in SphericalHarmonicsL2 sh, NativeArray<float> shaderCoeffsL2, int offset)
        {
            shaderCoeffsL2[offset + 0] = sh[0, 4]; shaderCoeffsL2[offset + 1] = sh[0, 5]; shaderCoeffsL2[offset + 2] = sh[0, 6]; shaderCoeffsL2[offset + 3] = sh[0, 7];
            shaderCoeffsL2[offset + 4] = sh[1, 4]; shaderCoeffsL2[offset + 5] = sh[1, 5]; shaderCoeffsL2[offset + 6] = sh[1, 6]; shaderCoeffsL2[offset + 7] = sh[1, 7];
            shaderCoeffsL2[offset + 8] = sh[2, 4]; shaderCoeffsL2[offset + 9] = sh[2, 5]; shaderCoeffsL2[offset + 10] = sh[2, 6]; shaderCoeffsL2[offset + 11] = sh[2, 7];
            shaderCoeffsL2[offset + 12] = sh[0, 8]; shaderCoeffsL2[offset + 13] = sh[1, 8]; shaderCoeffsL2[offset + 14] = sh[2, 8];
        }

        static void WriteToShaderCoeffsL2(in SphericalHarmonicsL2 sh, NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3, int offset)
        {
            shaderCoeffsL2_0[offset + 0] = SHFloatToByte(sh[0, 4]); shaderCoeffsL2_0[offset + 1] = SHFloatToByte(sh[0, 5]); shaderCoeffsL2_0[offset + 2] = SHFloatToByte(sh[0, 6]); shaderCoeffsL2_0[offset + 3] = SHFloatToByte(sh[0, 7]);
            shaderCoeffsL2_1[offset + 0] = SHFloatToByte(sh[1, 4]); shaderCoeffsL2_1[offset + 1] = SHFloatToByte(sh[1, 5]); shaderCoeffsL2_1[offset + 2] = SHFloatToByte(sh[1, 6]); shaderCoeffsL2_1[offset + 3] = SHFloatToByte(sh[1, 7]);
            shaderCoeffsL2_2[offset + 0] = SHFloatToByte(sh[2, 4]); shaderCoeffsL2_2[offset + 1] = SHFloatToByte(sh[2, 5]); shaderCoeffsL2_2[offset + 2] = SHFloatToByte(sh[2, 6]); shaderCoeffsL2_2[offset + 3] = SHFloatToByte(sh[2, 7]);
            shaderCoeffsL2_3[offset + 0] = SHFloatToByte(sh[0, 8]); shaderCoeffsL2_3[offset + 1] = SHFloatToByte(sh[1, 8]); shaderCoeffsL2_3[offset + 2] = SHFloatToByte(sh[2, 8]);
        }

        static void ReadFromShaderCoeffsL0L1(ref SphericalHarmonicsL2 sh, NativeArray<float> shaderCoeffsL0L1, int offset)
        {
            sh[0, 0] = shaderCoeffsL0L1[offset + 0]; sh[1, 0] = shaderCoeffsL0L1[offset + 1]; sh[2, 0] = shaderCoeffsL0L1[offset + 2]; sh[0, 1] = shaderCoeffsL0L1[offset + 3];
            sh[1, 1] = shaderCoeffsL0L1[offset + 4]; sh[1, 2] = shaderCoeffsL0L1[offset + 5]; sh[1, 3] = shaderCoeffsL0L1[offset + 6]; sh[0, 2] = shaderCoeffsL0L1[offset + 7];
            sh[2, 1] = shaderCoeffsL0L1[offset + 8]; sh[2, 2] = shaderCoeffsL0L1[offset + 9]; sh[2, 3] = shaderCoeffsL0L1[offset + 10]; sh[0, 3] = shaderCoeffsL0L1[offset + 11];
        }

        static void ReadFromShaderCoeffsL2(ref SphericalHarmonicsL2 sh, NativeArray<float> shaderCoeffsL2, int offset)
        {
            sh[0, 4] = shaderCoeffsL2[offset + 0]; sh[0, 5] = shaderCoeffsL2[offset + 1]; sh[0, 6] = shaderCoeffsL2[offset + 2]; sh[0, 7] = shaderCoeffsL2[offset + 3];
            sh[1, 4] = shaderCoeffsL2[offset + 4]; sh[1, 5] = shaderCoeffsL2[offset + 5]; sh[1, 6] = shaderCoeffsL2[offset + 6]; sh[1, 7] = shaderCoeffsL2[offset + 7];
            sh[2, 4] = shaderCoeffsL2[offset + 8]; sh[2, 5] = shaderCoeffsL2[offset + 9]; sh[2, 6] = shaderCoeffsL2[offset + 10]; sh[2, 7] = shaderCoeffsL2[offset + 11];
            sh[0, 8] = shaderCoeffsL2[offset + 12]; sh[1, 8] = shaderCoeffsL2[offset + 13]; sh[2, 8] = shaderCoeffsL2[offset + 14];
        }

        unsafe static int PackValidity(float* validity)
        {
            int outputByte = 0;
            for (int i = 0; i < 8; ++i)
            {
                int val = (validity[i] > 0.05f) ? 0 : 1;
                outputByte |= (val << i);
            }
            return outputByte;
        }

        static Vector3Int GetSampleOffset(int i)
        {
            return new Vector3Int(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        }

        /// <summary>
        /// This method converts a list of baking cells into 5 separate assets:
        ///  2 assets per baking state:
        ///   CellData: a binary flat file containing L0L1 probes data
        ///   CellOptionalData: a binary flat file containing L2 probe data (when present)
        ///  3 assets shared between states:
        ///   ProbeVolumeAsset: a Scriptable Object which currently contains book-keeping data, runtime cells, and references to flattened data
        ///   CellSharedData: a binary flat file containing bricks data
        ///   CellSupportData: a binary flat file containing debug data (stripped from player builds if building without debug shaders)
        /// </summary>
        unsafe static void WriteBakingCells(ProbeVolumePerSceneData data, List<BakingCell> bakingCells)
        {
            var asset = data.asset;
            asset.cells = new Cell[bakingCells.Count];
            asset.cellCounts = new ProbeVolumeAsset.CellCounts[bakingCells.Count];
            asset.totalCellCounts = new ProbeVolumeAsset.CellCounts();
            asset.chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();

            for (var i = 0; i < bakingCells.Count; ++i)
            {
                var bakingCell = bakingCells[i];

                asset.cells[i] = new Cell
                {
                    position = bakingCell.position,
                    index = bakingCell.index,
                    minSubdiv = bakingCell.minSubdiv,
                    indexChunkCount = bakingCell.indexChunkCount,
                    shChunkCount = bakingCell.shChunkCount,
                    shBands = asset.bands,
                };

                var cellCounts = new ProbeVolumeAsset.CellCounts
                {
                    bricksCount = bakingCell.bricks.Length,
                    probesCount = bakingCell.probePositions.Length,
                    offsetsCount = bakingCell.offsetVectors.Length,
                    chunksCount = bakingCell.shChunkCount
                };
                asset.cellCounts[i] = cellCounts;
                asset.totalCellCounts.Add(cellCounts);
            }

            // CellData
            // Need full chunks so we don't use totalCellCounts.probesCount
            var count = asset.totalCellCounts.chunksCount * ProbeBrickPool.GetChunkSizeInProbeCount() * 4; // 4 component per probe per texture.
            using var probesL0L1Rx = new NativeArray<ushort>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1GL1Ry = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1BL1Rz = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            using var validityOld = new NativeArray<float>(asset.totalCellCounts.probesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            using var packedValidity = new NativeArray<byte>(asset.totalCellCounts.chunksCount * ProbeBrickPool.GetChunkSizeInProbeCount(), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellOptionalData
            count = asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? count : 0;
            using var probesL2_0 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_1 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_2 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_3 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // OLD DATA
            using var probesL0L1 = new NativeArray<float>(asset.totalCellCounts.probesCount * ProbeVolumeAsset.kL0L1ScalarCoefficientsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var probesL2ScalarPaddedCount = asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? asset.totalCellCounts.probesCount * ProbeVolumeAsset.kL2ScalarCoefficientsCount + 3 : 0;
            using var probesL2 = new NativeArray<float>(probesL2ScalarPaddedCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            // OLD DATA

            // CellSharedData
            using var bricks = new NativeArray<Brick>(asset.totalCellCounts.bricksCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSupportData
            using var positions = new NativeArray<Vector3>(asset.totalCellCounts.probesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var offsets = new NativeArray<Vector3>(asset.totalCellCounts.offsetsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sceneStateHash = asset.GetBakingHashCode();
            var startCounts = new ProbeVolumeAsset.CellCounts();

            int chunkOffset = 0;
            var chunkSize = ProbeBrickPool.GetChunkSizeInProbeCount() * 4;

            int validityChunkOffset = 0;
            var validityChunkSize = ProbeBrickPool.GetChunkSizeInProbeCount();
            var tempValidityArray = new DynamicArray<float>(asset.totalCellCounts.chunksCount * ProbeBrickPool.GetChunkSizeInProbeCount());

            // Init SH with values that will resolve to black
            var blackSH = new SphericalHarmonicsL2();
            for (int channel = 0; channel < 3; ++channel)
            {
                blackSH[channel, 0] = 0.0f;
                for (int coeff = 1; coeff < 9; ++coeff)
                    blackSH[channel, coeff] = 0.5f;
            }

            for (var i = 0; i < bakingCells.Count; ++i)
            {
                var bakingCell = bakingCells[i];
                var cellCounts = asset.cellCounts[i];

                sceneStateHash = sceneStateHash * 23 + bakingCell.GetBakingHashCode();
                bricks.GetSubArray(startCounts.bricksCount, cellCounts.bricksCount).CopyFrom(bakingCell.bricks);

                // Each
                var inputProbesCount = cellCounts.probesCount;
                // Size of the DataLocation used to do the copy texture at runtime. Used to generate the right layout for the 3D texture.
                Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());

                int shidx = 0;

                var probesTargetL0L1 = probesL0L1.GetSubArray(startCounts.probesCount * ProbeVolumeAsset.kL0L1ScalarCoefficientsCount, cellCounts.probesCount * ProbeVolumeAsset.kL0L1ScalarCoefficientsCount);
                int oldDataOffsetL0L1 = 0;
                NativeArray<float> probesTargetL2 = default(NativeArray<float>);
                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    probesTargetL2 = probesL2.GetSubArray(startCounts.probesCount * ProbeVolumeAsset.kL2ScalarCoefficientsCount, cellCounts.probesCount * ProbeVolumeAsset.kL2ScalarCoefficientsCount);
                int oldDataOffsetL2 = 0;

                // Here we directly map each chunk to the layout of the 3D textures in order to be able to copy the data directly to the GPU.
                // The granularity at runtime is one chunk at a time currently so the temporary data loc used is sized accordingly.
                for (int chunkIndex = 0; chunkIndex < cellCounts.chunksCount; ++chunkIndex)
                {
                    var probesTargetL0L1Rx = probesL0L1Rx.GetSubArray(chunkOffset, chunkSize);
                    var probesTargetL1GL1Ry = probesL1GL1Ry.GetSubArray(chunkOffset, chunkSize);
                    var probesTargetL1BL1Rz = probesL1BL1Rz.GetSubArray(chunkOffset, chunkSize);
                    var packedValidityChunkTarget = packedValidity.GetSubArray(validityChunkOffset, validityChunkSize);

                    NativeArray<byte> probesTargetL2_0 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_1 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_2 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_3 = default(NativeArray<byte>);

                    if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        probesTargetL2_0 = probesL2_0.GetSubArray(chunkOffset, chunkSize);
                        probesTargetL2_1 = probesL2_1.GetSubArray(chunkOffset, chunkSize);
                        probesTargetL2_2 = probesL2_2.GetSubArray(chunkOffset, chunkSize);
                        probesTargetL2_3 = probesL2_3.GetSubArray(chunkOffset, chunkSize);
                    }

                    int bx = 0, by = 0, bz = 0;

                    for (int brickIndex = 0; brickIndex < asset.chunkSizeInBricks; brickIndex++)
                    {
                        for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                        {
                            for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                            {
                                for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                                {
                                    int ix = bx + x;
                                    int iy = by + y;
                                    int iz = bz + z;

                                    int index = ix + locSize.x * (iy + locSize.y * iz);

                                    // We are processing chunks at a time.
                                    // So in practice we can go over the number of SH we have in the input list.
                                    // We fill with encoded black to avoid copying garbage in the final atlas.
                                    if (shidx >= inputProbesCount)
                                    {
                                        WriteToShaderCoeffsL0L1(blackSH, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);

                                        tempValidityArray[index] = 1.0f;
                                        packedValidityChunkTarget[index] = 0;

                                        if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                            WriteToShaderCoeffsL2(blackSH, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);
                                    }
                                    else
                                    {
                                        ref var sh = ref bakingCell.sh[shidx];

                                        WriteToShaderCoeffsL0L1(sh, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);
                                        WriteToShaderCoeffsL0L1(sh, probesTargetL0L1, oldDataOffsetL0L1);

                                        tempValidityArray[index] = bakingCell.validity[shidx];

                                        if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                        {
                                            WriteToShaderCoeffsL2(sh, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);
                                            WriteToShaderCoeffsL2(sh, probesTargetL2, oldDataOffsetL2);
                                        }
                                    }
                                    shidx++;
                                    oldDataOffsetL0L1 += ProbeVolumeAsset.kL0L1ScalarCoefficientsCount;
                                    oldDataOffsetL2 += ProbeVolumeAsset.kL2ScalarCoefficientsCount;
                                }
                            }
                        }

                        // update the pool index
                        bx += ProbeBrickPool.kBrickProbeCountPerDim;
                        if (bx >= locSize.x)
                        {
                            bx = 0;
                            by += ProbeBrickPool.kBrickProbeCountPerDim;
                            if (by >= locSize.y)
                            {
                                by = 0;
                                bz += ProbeBrickPool.kBrickProbeCountPerDim;
                            }
                        }
                    }

                    float* validities = stackalloc float[8];

                    // This can be optimized later.
                    for (int x = 0; x < locSize.x; ++x)
                    {
                        for (int y = 0; y < locSize.y; ++y)
                        {
                            for (int z = 0; z < locSize.z; ++z)
                            {
                                int index = x + locSize.x * (y + locSize.y * z);

                                for (int o = 0; o < 8; ++o)
                                {
                                    Vector3Int off = GetSampleOffset(o);
                                    Vector3Int samplePos = new Vector3Int(Mathf.Clamp(x + off.x, 0, locSize.x - 1),
                                                                          Mathf.Clamp(y + off.y, 0, locSize.y - 1),
                                                                          Mathf.Clamp(z + off.z, 0, ProbeBrickPool.kBrickProbeCountPerDim - 1));
                                    int validityIndex = samplePos.x + locSize.x * (samplePos.y + locSize.y * samplePos.z);
                                    validities[o] = tempValidityArray[validityIndex];
                                }

                                packedValidityChunkTarget[index] = Convert.ToByte(PackValidity(validities));
                            }
                        }
                    }

                    chunkOffset += chunkSize;
                    validityChunkOffset += validityChunkSize;
                }

                validityOld.GetSubArray(startCounts.probesCount, cellCounts.probesCount).CopyFrom(bakingCell.validity);
                positions.GetSubArray(startCounts.probesCount, cellCounts.probesCount).CopyFrom(bakingCell.probePositions);
                offsets.GetSubArray(startCounts.offsetsCount, cellCounts.offsetsCount).CopyFrom(bakingCell.offsetVectors);

                startCounts.Add(cellCounts);
            }

            // Need to save here because the forced import below discards the changes.
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            data.GetBlobFileNames(out var cellDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            unsafe
            {
                static long AlignRemainder16(long count) => count % 16L;

                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    fs.Write(new ReadOnlySpan<byte>(probesL0L1.GetUnsafeReadOnlyPtr(), probesL0L1.Length * UnsafeUtility.SizeOf<float>()));

                    fs.Write(new ReadOnlySpan<byte>(probesL0L1Rx.GetUnsafeReadOnlyPtr(), probesL0L1Rx.Length * UnsafeUtility.SizeOf<ushort>()));
                    fs.Write(new ReadOnlySpan<byte>(probesL1GL1Ry.GetUnsafeReadOnlyPtr(), probesL1GL1Ry.Length * UnsafeUtility.SizeOf<byte>()));
                    fs.Write(new ReadOnlySpan<byte>(probesL1BL1Rz.GetUnsafeReadOnlyPtr(), probesL1BL1Rz.Length * UnsafeUtility.SizeOf<byte>()));

                    fs.Write(new byte[AlignRemainder16(fs.Position)]);
                    fs.Write(new ReadOnlySpan<byte>(validityOld.GetUnsafeReadOnlyPtr(), validityOld.Length * UnsafeUtility.SizeOf<float>()));

                    fs.Write(new byte[AlignRemainder16(fs.Position)]);
                    fs.Write(new ReadOnlySpan<byte>(packedValidity.GetUnsafeReadOnlyPtr(), packedValidity.Length * UnsafeUtility.SizeOf<byte>()));
                }
                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        fs.Write(new ReadOnlySpan<byte>(probesL2.GetUnsafeReadOnlyPtr(), probesL2.Length * UnsafeUtility.SizeOf<float>()));

                        fs.Write(new ReadOnlySpan<byte>(probesL2_0.GetUnsafeReadOnlyPtr(), probesL2_0.Length * UnsafeUtility.SizeOf<byte>()));
                        fs.Write(new ReadOnlySpan<byte>(probesL2_1.GetUnsafeReadOnlyPtr(), probesL2_1.Length * UnsafeUtility.SizeOf<byte>()));
                        fs.Write(new ReadOnlySpan<byte>(probesL2_2.GetUnsafeReadOnlyPtr(), probesL2_2.Length * UnsafeUtility.SizeOf<byte>()));
                        fs.Write(new ReadOnlySpan<byte>(probesL2_3.GetUnsafeReadOnlyPtr(), probesL2_3.Length * UnsafeUtility.SizeOf<byte>()));
                    }
                }
                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    fs.Write(new ReadOnlySpan<byte>(bricks.GetUnsafeReadOnlyPtr(), bricks.Length * UnsafeUtility.SizeOf<Brick>()));
                using (var fs = new System.IO.FileStream(cellSupportDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    fs.Write(new ReadOnlySpan<byte>(positions.GetUnsafeReadOnlyPtr(), positions.Length * UnsafeUtility.SizeOf<Vector3>()));
                    fs.Write(new byte[AlignRemainder16(fs.Position)]);
                    fs.Write(new ReadOnlySpan<byte>(offsets.GetUnsafeReadOnlyPtr(), offsets.Length * UnsafeUtility.SizeOf<Vector3>()));
                }
            }

            AssetDatabase.ImportAsset(cellDataFilename);

            if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                AssetDatabase.ImportAsset(cellOptionalDataFilename);
            else
                AssetDatabase.DeleteAsset(cellOptionalDataFilename);

            AssetDatabase.ImportAsset(cellSharedDataFilename);
            AssetDatabase.ImportAsset(cellSupportDataFilename);

            data.states[ProbeReferenceVolume.instance.bakingState] = new ProbeVolumePerSceneData.PerStateData
            {
                sceneHash = sceneStateHash,
                cellDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellDataFilename),
                cellOptionalDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellOptionalDataFilename),
            };
            data.cellSharedDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellSharedDataFilename);
            data.cellSupportDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellSupportDataFilename);
            EditorUtility.SetDirty(data);
        }

        static void WritebackModifiedCellsData(ProbeVolumePerSceneData data)
        {
            var asset = data.asset;
            var stateData = data.states[ProbeReferenceVolume.instance.bakingState];
            data.GetBlobFileNames(out var cellDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    var cellData = stateData.cellDataAsset.GetData<byte>();
                    fs.Write(new ReadOnlySpan<byte>(cellData.GetUnsafeReadOnlyPtr(), cellData.Length));
                }
                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        var cellOptionalData = stateData.cellOptionalDataAsset.GetData<byte>();
                        fs.Write(new ReadOnlySpan<byte>(cellOptionalData.GetUnsafeReadOnlyPtr(), cellOptionalData.Length));
                    }
                }
                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    var cellSharedData = data.cellSharedDataAsset.GetData<byte>();
                    fs.Write(new ReadOnlySpan<byte>(cellSharedData.GetUnsafeReadOnlyPtr(), cellSharedData.Length));
                }
                using (var fs = new System.IO.FileStream(cellSupportDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    var cellSupportData = data.cellSupportDataAsset.GetData<byte>();
                    fs.Write(new ReadOnlySpan<byte>(cellSupportData.GetUnsafeReadOnlyPtr(), cellSupportData.Length));
                }
            }
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
                if (m_BakingSettings.virtualOffsetSettings.useVirtualOffset)
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
                if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return ctx;
                SetBakingContext(ProbeReferenceVolume.instance.perSceneDataList);
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
                        renderer.volume.CalculateCenterAndSize(out _, out var rendererBoundsSize);
                        float rendererBoundsVolume = rendererBoundsSize.x * rendererBoundsSize.y * rendererBoundsSize.z;

                        foreach (var probeVolume in overlappingProbeVolumes)
                        {
                            // Skip renderers that have a smaller volume than the min volume size from the profile or probe volume component
                            float minRendererBoundingBoxSize = ctx.profile.minRendererVolumeSize;
                            if (probeVolume.component.overrideRendererFilters)
                                minRendererBoundingBoxSize = probeVolume.component.minRendererVolumeSize;
                            if (rendererBoundsVolume < minRendererBoundingBoxSize)
                                continue;

                            if (ProbeVolumePositioning.OBBIntersect(renderer.volume, probeVolume.volume)
                                && ProbeVolumePositioning.OBBIntersect(renderer.volume, cell.volume))
                            {
                                var layerMask = ctx.profile.renderersLayerMask;

                                if (probeVolume.component.overrideRendererFilters)
                                    layerMask = probeVolume.component.objectLayerMask;

                                // Check if the renderer has a matching layer with probe volume
                                if ((layerMask & rendererLayerMask) != 0)
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

                if (bricks.Count == 0)
                    continue;

                BakingCell cell = new BakingCell();
                cell.position = cellPos;
                cell.index = index++;

                // Convert bricks to positions
                var probePositionsArr = new Vector3[bricks.Count * ProbeBrickPool.kBrickProbeCountTotal];
                var brickSubdivLevels = new int[bricks.Count * ProbeBrickPool.kBrickProbeCountTotal];
                ConvertBricksToPositions(bricks, probePositionsArr, refToWS, brickSubdivLevels);

                DeduplicateProbePositions(in probePositionsArr, in brickSubdivLevels, m_BakingBatch.uniquePositions, m_BakingBatch.uniqueBrickSubdiv, out var indices);

                cell.probePositions = probePositionsArr;
                cell.bricks = bricks.ToArray();

                cell.probeIndices = indices;

                m_BakingBatch.cells.Add(cell);
                m_BakingBatch.cellIndex2SceneReferences[cell.index] = results.scenesPerCells[cellPos].ToList();
            }

            // Virtually offset positions before passing them to lightmapper
            var positions = m_BakingBatch.uniquePositions.Keys.ToArray();
            ApplyVirtualOffsets(positions, out m_BakingBatch.virtualOffsets);

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, positions);
        }
    }
}
