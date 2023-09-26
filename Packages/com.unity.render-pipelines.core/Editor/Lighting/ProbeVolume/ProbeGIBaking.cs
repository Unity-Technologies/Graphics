using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using Cell = UnityEngine.Rendering.ProbeReferenceVolume.Cell;
using IndirectionEntryInfo = UnityEngine.Rendering.ProbeReferenceVolume.IndirectionEntryInfo;
using CellInfo = UnityEngine.Rendering.ProbeReferenceVolume.CellInfo;

namespace UnityEngine.Rendering
{
    struct BakingCell
    {
        public Vector3Int position;
        public int index;

        public Brick[] bricks;
        public Vector3[] probePositions;
        public SphericalHarmonicsL2[] sh;
        public byte[] validityNeighbourMask;
        public float[] validity;
        public Vector3[] offsetVectors;
        public float[] touchupVolumeInteraction;

        public int minSubdiv;
        public int indexChunkCount;
        public int shChunkCount;
        public IndirectionEntryInfo[] indirectionEntryInfo;

        public int[] probeIndices;

        public Bounds bounds;

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
        public Dictionary<int, HashSet<Scene>> cellIndex2SceneReferences = new Dictionary<int, HashSet<Scene>>();
        public List<BakingCell> cells = new List<BakingCell>();
        public Vector3[] virtualOffsets;
        // Allow to get a mapping to subdiv level with the unique positions. It stores the minimum subdiv level found for a given position.
        // Can be probably done cleaner.
        public Dictionary<int, int> uniqueBrickSubdiv = new ();
        // Mapping for explicit invalidation, whether it comes from the auto finding of occluders or from the touch up volumes
        // TODO: This is not used yet. Will soon.
        public Dictionary<Vector3, bool> invalidatedPositions = new Dictionary<Vector3, bool>();
        // Utilities to compute unique probe position hash
        Vector3Int maxBrickCount;
        float inverseScale;

        private BakingBatch() { }

        public BakingBatch(int index, Vector3Int cellCount)
        {
            this.index = index;

            maxBrickCount = cellCount * ProbeReferenceVolume.CellSize(ProbeReferenceVolume.instance.GetMaxSubdivision());
            inverseScale = ProbeBrickPool.kBrickCellCount / ProbeReferenceVolume.instance.MinBrickSize();
        }

        public void Clear()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(index, null);
            cells.Clear();
            cellIndex2SceneReferences.Clear();
        }

        public int GetProbePositionHash(Vector3 position)
        {
            var brickPosition = Vector3Int.RoundToInt(position * inverseScale);
            return brickPosition.x + brickPosition.y * maxBrickCount.x + brickPosition.z * maxBrickCount.x * maxBrickCount.y;
        }

        public int GetSubdivLevelAt(Vector3 position) => uniqueBrickSubdiv[GetProbePositionHash(position)];

        public int uniqueProbeCount => uniqueBrickSubdiv.Count;
    }

    [InitializeOnLoad]
    partial class ProbeGIBaking
    {
        enum BakingStage
        {
            NotStarted,
            Started,
            PlacementDone,
            OnBakeCompletedStarted,
            OnBakeCompletedFinished
        }

        public abstract class BakingProfiling<T> where T : Enum
        {
            protected virtual string LogFile => null; // Override in child classes to write profiling data to disk

            protected T prevStage;
            bool disposed = false;
            static float globalProgress = 0.0f;

            public float GetProgress(T stage) => (int)(object)stage / (float)(int)(object)GetLastStep();
            void UpdateProgressBar(T stage)
            {
                if (EqualityComparer<T>.Default.Equals(stage, GetLastStep()))
                {
                    globalProgress = 0.0f;
                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    globalProgress = Mathf.Max(GetProgress(stage), globalProgress); // prevent progress from going back
                    EditorUtility.DisplayProgressBar("Baking Probe Volumes", stage.ToString(), globalProgress);
                }
            }

            public abstract T GetLastStep();

            public BakingProfiling(T stage, ref T currentStage)
            {
                if (LogFile != null && EqualityComparer<T>.Default.Equals(currentStage, GetLastStep()))
                {
                    Profiling.Profiler.logFile = LogFile;
                    Profiling.Profiler.enableBinaryLog = true;
                    Profiling.Profiler.enabled = true;
                }

                prevStage = currentStage;
                currentStage = stage;
                UpdateProgressBar(stage);

                Profiling.Profiler.BeginSample(stage.ToString());
            }

            public void OnDispose(ref T currentStage)
            {
                if (disposed) return;
                disposed = true;

                Profiling.Profiler.EndSample();

                UpdateProgressBar(prevStage);
                currentStage = prevStage;

                if (LogFile != null && EqualityComparer<T>.Default.Equals(currentStage, GetLastStep()))
                {
                    Profiling.Profiler.enabled = false;
                    Profiling.Profiler.logFile = null;
                }
            }
        }
        public class BakingSetupProfiling : BakingProfiling<BakingSetupProfiling.Stages>, IDisposable
        {
            //protected override string LogFile => "OnBakeStarted";

            public enum Stages
            {
                PrepareWorldSubdivision,
                EnsurePerSceneDataInOpenScenes,
                FindWorldBounds,
                PlaceProbes,
                BakeBricks,
                ApplySubdivisionResults,
                ApplyVirtualOffsets,
                None
            }

            static Stages currentStage = Stages.None;
            public BakingSetupProfiling(Stages stage) : base(stage, ref currentStage) { }
            public override Stages GetLastStep() => Stages.None;
            public static void GetProgressRange(out float progress0, out float progress1) { float s = 1 / (float)Stages.None; progress0 = (float)currentStage * s; progress1 = progress0 + s; }
            public void Dispose() { OnDispose(ref currentStage); }
        }
        public class BakingCompleteProfiling : BakingProfiling<BakingCompleteProfiling.Stages>, IDisposable
        {
            //protected override string LogFile => "OnAdditionalProbesBakeCompleted";

            public enum Stages
            {
                FinalizingBake,
                AddOccluders,
                FetchResults,
                WriteBakedData,
                PerformDilation,
                None
            }

            static Stages currentStage = Stages.None;
            public BakingCompleteProfiling(Stages stage) : base(stage, ref currentStage) { }
            public override Stages GetLastStep() => Stages.None;
            public static void GetProgressRange(out float progress0, out float progress1) { float s = 1 / (float)Stages.None; progress0 = (float)currentStage * s; progress1 = progress0 + s; }
            public void Dispose() { OnDispose(ref currentStage); }
        }


        static bool m_IsInit = false;
        static BakingBatch m_BakingBatch;
        static ProbeVolumeBakingSet m_BakingProfile = null;
        static ProbeVolumeBakingProcessSettings m_BakingSettings;

        static int m_BakingBatchIndex = 0;

        static Bounds globalBounds = new Bounds();
        static bool hasFoundBounds = false;
        static Vector3Int minCellPosition = Vector3Int.one * int.MaxValue;
        static Vector3Int maxCellPosition = Vector3Int.one * int.MinValue;
        static Vector3Int cellCount = Vector3Int.zero;

        static BakingStage currentBakingState = BakingStage.NotStarted;
        static int pvHashesAtBakeStart = -1;

        static Dictionary<Vector3Int, int> m_CellPosToIndex = new Dictionary<Vector3Int, int>();
        static Dictionary<int, BakingCell> m_BakedCells = new Dictionary<int, BakingCell>();

        internal static List<string> partialBakeSceneList = null;
        internal static bool isBakingSceneSubset => partialBakeSceneList != null;
        internal static bool isFreezingPlacement = false;

        // This is needed only for isBakingOnlyActiveScene when we have some cells extracted from assets into m_BakedCells
        static HashSet<int> m_NewlyBakedCells = new HashSet<int>();

        internal static List<ProbeVolumePerSceneData> GetPerSceneDataList()
        {
            var fullPerSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
            if (!isBakingSceneSubset)
                return fullPerSceneDataList;

            List<ProbeVolumePerSceneData> usedPerSceneDataList = new ();
            foreach (var sceneData in fullPerSceneDataList)
            {
                if (partialBakeSceneList.Contains(ProbeVolumeSceneData.GetSceneGUID(sceneData.gameObject.scene)))
                    usedPerSceneDataList.Add(sceneData);
            }
            return usedPerSceneDataList;
        }

        internal static List<ProbeVolume> GetProbeVolumeList()
        {
            var fullPvList = GameObject.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            List<ProbeVolume> usedPVList;

            if (isBakingSceneSubset)
            {
                usedPVList = new List<ProbeVolume>();
                foreach (var pv in fullPvList)
                {
                    if (pv.isActiveAndEnabled && partialBakeSceneList.Contains(ProbeVolumeSceneData.GetSceneGUID(pv.gameObject.scene)))
                        usedPVList.Add(pv);
                }
            }
            else
            {
                usedPVList = new List<ProbeVolume>(fullPvList);
            }

            return usedPVList;
        }

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

            var probeVolumes = GameObject.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            foreach (var probeVolume in probeVolumes)
                probeVolume.OnLightingDataAssetCleared();
        }

        public static bool CanFreezePlacement()
        {
            if (!ProbeReferenceVolume.instance.supportLightingScenarios)
                return false;

            // Check if all the scene datas in the scene have an asset, if  not then we cannot enable this option.
            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0)
                return false;

            foreach (var sceneData in sceneDataList)
            {
                if (sceneData.asset == null)
                    return false;
            }

            return true;
        }

        public static void FindWorldBounds()
        {
            ProbeReferenceVolume.instance.clearAssetsOnVolumeClear = true;

            var sceneData = ProbeReferenceVolume.instance.sceneData;
            HashSet<string> scenesToConsider = new HashSet<string>();

            var activeScene = SceneManager.GetActiveScene();
            var activeSet = sceneData.GetBakingSetForScene(activeScene);

            hasFoundBounds = false;

            foreach (var sceneGUID in activeSet.sceneGUIDs)
            {
                if (sceneData.hasProbeVolumes.TryGetValue(sceneGUID, out bool hasProbeVolumes))
                {
                    if (hasProbeVolumes)
                    {
                        if (sceneData.sceneBounds.TryGetValue(sceneGUID, out var localBound))
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
            }

            ProbeReferenceVolume.instance.globalBounds = globalBounds;
        }

        static bool SetBakingContext(List<ProbeVolumePerSceneData> perSceneData)
        {
            // We need to make sure all scenes we are baking have the same profile. The same should be done for the baking settings, but we check only profile.
            // TODO: This should be ensured by the controlling panel, until we have that we need to assert.

            // To check what are the scenes that have probe volume enabled we checks the ProbeVolumePerSceneData. We are guaranteed to have only one per scene.
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var profile = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(scene);

                if (profile == null)
                {
                    Debug.LogError($"Scene '{scene.name}' does not belong to any Baking Set. Please add it to a Baking Set in the Probe Volumes tab of the Lighting Window.");
                    return false;
                }

                // In case a scene is duplicated, we need to clear references to original asset
                if (data.sceneGUID != data.gameObject.scene.GetGUID())
                {
                    if (!string.IsNullOrEmpty(data.sceneGUID))
                        data.ClearBakedDataReferences();
                    data.sceneGUID = data.gameObject.scene.GetGUID();
                }

                if (i == 0)
                {
                    m_BakingProfile = profile;
                    m_BakingSettings = profile.settings;
                }
                else if (!m_BakingProfile.IsEquivalent(profile))
                    return false;
            }

            return true;
        }

        static void EnsurePerSceneDataInOpenScenes()
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            var activeScene = SceneManager.GetActiveScene();
            var activeSet = sceneData.GetBakingSetForScene(activeScene);

            // We assume that all the per scene data for all the scenes in the set have been set with the scene been saved at least once. However we also update the scenes that are currently loaded anyway for security.
            // and to have a new trigger to update the bounds we have.
            int openedScenesCount = SceneManager.sceneCount;
            for (int i = 0; i < openedScenesCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                sceneData.OnSceneSaving(scene); // We need to perform the same actions we do when the scene is saved.
                if (sceneData.GetBakingSetForScene(scene) != activeSet && sceneData.SceneHasProbeVolumes(scene))
                {
                    Debug.LogError($"Scene at {scene.path} is loaded and has probe volumes, but not part of the same baking set as the active scene. This will result in an error. Please make sure all loaded scenes are part of the same baking sets.");
                }
            }

            // Make sure there are no remaining per scene data in scenes where probe volume was deleted
            // iterate in reverse order because destroy will pop element from the array
            for (int i = ProbeReferenceVolume.instance.perSceneDataList.Count - 1; i >= 0; i--)
            {
                var perSceneData = ProbeReferenceVolume.instance.perSceneDataList[i];
                if (!sceneData.SceneHasProbeVolumes(perSceneData.gameObject.scene))
                    CoreUtils.Destroy(perSceneData.gameObject);
            }
        }

        static void CachePVHashes(List<ProbeVolume> probeVolumes)
        {
            pvHashesAtBakeStart = 0;
            foreach (var pv in probeVolumes)
            {
                pvHashesAtBakeStart += pvHashesAtBakeStart * 23 + pv.GetHashCode();
            }
        }

        static void CheckPVChanges()
        {
            // If we have baking in flight.
            if (Lightmapping.isRunning && currentBakingState > BakingStage.Started && (GUIUtility.hotControl == 0))
            {
                var pvList = GetProbeVolumeList();
                int currHash = 0;
                foreach (var pv in pvList)
                {
                    currHash += currHash * 23 + pv.GetHashCode();
                }

                if (currHash != pvHashesAtBakeStart)
                {
                    // Need to force stop the light baking and start it again.
                    Lightmapping.Cancel();
                    OnLightingDataCleared();
                    OnBakeCompletedCleanup();
                    Lightmapping.BakeAsync();
                }
            }
        }

        static void OnBakeStarted()
        {
            if (ProbeVolumeLightingTab.instance?.PrepareAPVBake() == false) return;
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP) return;

            using var scope = new BakingSetupProfiling(BakingSetupProfiling.Stages.PrepareWorldSubdivision);

            // Verify to make sure we can still do it. Shortcircuting so that we don't run CanFreezePlacement unless is needed.
            isFreezingPlacement = isFreezingPlacement && CanFreezePlacement();
            if (!isFreezingPlacement)
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.EnsurePerSceneDataInOpenScenes))
                    EnsurePerSceneDataInOpenScenes();
            }

            if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return;

            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0) return;

            var pvList = GetProbeVolumeList();
            if (pvList.Count == 0) return; // We have no probe volumes.

            CachePVHashes(pvList);

            currentBakingState = BakingStage.Started;

            if (!SetBakingContext(sceneDataList))
                return;

            if (isFreezingPlacement)
            {
                ModifyProfileFromLoadedData(m_BakingProfile);
            }
            else
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.FindWorldBounds))
                    FindWorldBounds();
            }

            // Get min/max
            CellCountInDirections(out minCellPosition, out maxCellPosition, m_BakingProfile.cellSizeInMeters);
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            GeneratePhysicsComponentToModList();

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.Initialize();

            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.PlaceProbes))
                RunPlacement();

            currentBakingState = BakingStage.PlacementDone;
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

            maxCellPositionXYZ.x = Mathf.CeilToInt(centeredMax.x / cellSizeInMeters) - 1;
            maxCellPositionXYZ.y = Mathf.CeilToInt(centeredMax.y / cellSizeInMeters) - 1;
            maxCellPositionXYZ.z = Mathf.CeilToInt(centeredMax.z / cellSizeInMeters) - 1;
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
                        WriteToShaderCoeffsL0L1(blackProbe, cell.bakingScenario.shL0L1RxData, cell.bakingScenario.shL1GL1RyData, cell.bakingScenario.shL1BL1RzData, i * 4);

                        if (cell.shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            WriteToShaderCoeffsL2(blackProbe, cell.bakingScenario.shL2Data_0, cell.bakingScenario.shL2Data_1, cell.bakingScenario.shL2Data_2, cell.bakingScenario.shL2Data_3, i * 4);
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

                if (asset == null) continue; // Can happen if only the active scene is baked and the data for the rest is not available.

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
                            if (isBakingSceneSubset && !m_NewlyBakedCells.Contains(cell.index))
                            {
                                dilatedCells.Add(cell);
                            }
                            else
                            {
                                PerformDilation(cell, dilationSettings);
                                dilatedCells.Add(cell);
                            }
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

                            if (isBakingSceneSubset && !m_NewlyBakedCells.Contains(cell.index))
                            {
                                dilatedCells.Add(cell);
                            }
                            else
                            {
                                PerformDilation(cell, dilationSettings);
                                dilatedCells.Add(cell);
                            }

                            // Free memory again.
                            foreach (var cellToUnload in tempLoadedCells)
                                prv.UnloadCell(cellToUnload);
                        }
                    }

                    foreach (var sceneData in perSceneDataList)
                    {
                        if (sceneData.asset == null) continue; // Can happen if only the active scene is baked and the data for the rest is not available.
                        prv.AddPendingAssetRemoval(sceneData.asset);
                    }

                    // Make sure unloading happens.
                    prv.PerformPendingOperations();

                    Dictionary<ProbeVolumePerSceneData, List<Cell>> sceneDataToCells = new Dictionary<ProbeVolumePerSceneData, List<Cell>>();
                    foreach (var cell in dilatedCells)
                    {
                        foreach (var sceneData in perSceneDataList)
                        {
                            if (sceneData.asset == null) continue; // Can happen if only the active scene is baked and the data for the rest is not available.

                            var assetPath = sceneData.asset.GetSerializedFullPath();
                            if (cell2Assets[cell.index].Contains(assetPath))
                            {
                                if (!sceneDataToCells.ContainsKey(sceneData))
                                    sceneDataToCells[sceneData] = new List<Cell>();

                                sceneDataToCells[sceneData].Add(cell);
                            }
                        }
                    }

                    // Now write back the assets.
                    foreach (var data2Cell in sceneDataToCells)
                    {
                        WriteDilatedCells(data2Cell.Key, data2Cell.Value);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    foreach (var sceneData in perSceneDataList)
                        sceneData.QueueAssetLoading();
                }

                // Need to restore the original sh bands
                prv.ForceSHBand(prevSHBands);
            }
        }

        static void OnAdditionalProbesBakeCompleted()
        {
            if (currentBakingState != BakingStage.PlacementDone)
            {
                // This can happen if a baking job is canceled and a phantom call to OnAdditionalProbesBakeCompleted cannot be dequeued.
                // TODO: Investigate with the lighting team if we have a cleaner way.
                return;
            }
            currentBakingState = BakingStage.OnBakeCompletedStarted;

            using var scope = new BakingCompleteProfiling(BakingCompleteProfiling.Stages.FinalizingBake);

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
            s_ForceInvalidatedProbesAndTouchupVols.Clear();
            s_CustomDilationThresh.Clear();

            var probeRefVolume = ProbeReferenceVolume.instance;
            var bakingCells = m_BakingBatch.cells;
            var numCells = bakingCells.Count;

            var fullSceneDataList = probeRefVolume.perSceneDataList;
            int numUniqueProbes = m_BakingBatch.uniqueProbeCount;

            var sh = new NativeArray<SphericalHarmonicsL2>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(numUniqueProbes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (numUniqueProbes != 0)
            {
                var bakedProbeOctahedralDepth = new NativeArray<float>(numUniqueProbes * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                bool validBakedProbes = UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(m_BakingBatch.index, sh, validity, bakedProbeOctahedralDepth);
                bakedProbeOctahedralDepth.Dispose();

                if (!validBakedProbes)
                {
                    Debug.LogError("Lightmapper failed to produce valid probe data.  Please consider clearing lighting data and rebake.");
                    return;
                }
            }

            m_CellPosToIndex.Clear();
            m_BakedCells.Clear();

            // Clear baked data
            foreach (var data in probeRefVolume.perSceneDataList)
                data.QueueAssetRemoval();
            probeRefVolume.Clear();

            // Make sure all pending operations are done (needs to be after the Clear to unload all previous scenes)
            probeRefVolume.PerformPendingOperations();
            probeRefVolume.SetMinBrickAndMaxSubdiv(m_BakingProfile.minBrickSize, m_BakingProfile.maxSubdivision);

            // Use the globalBounds we just computed, as the one in probeRefVolume doesn't include scenes that have never been baked
            probeRefVolume.globalBounds = globalBounds;

            var dilationSettings = m_BakingSettings.dilationSettings;
            var virtualOffsets = m_BakingBatch.virtualOffsets;

            // This is slow, but we should have very little amount of touchup volumes.
            var touchupVolumes = GameObject.FindObjectsByType<ProbeTouchupVolume>(FindObjectsSortMode.InstanceID);
            var touchupVolumesAndBounds = new List<(ProbeReferenceVolume.Volume obb, Bounds aabb, ProbeTouchupVolume touchupVolume)>(touchupVolumes.Length);
            foreach (var touchup in touchupVolumes)
            {
                if (touchup.isActiveAndEnabled)
                {
                    touchup.GetOBBandAABB(out var obb, out var aabb);
                    touchupVolumesAndBounds.Add((obb, aabb, touchup));
                }
            }

            // If we did not use virtual offset, we did not have occluders spawned.
            if (!m_BakingSettings.virtualOffsetSettings.useVirtualOffset)
            {
                using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.AddOccluders))
                    AddOccluders();
            }

            ModifyPhysicsComponentsForBaking();

            // Fetch results of all cells
            using var fetchScope = new BakingCompleteProfiling(BakingCompleteProfiling.Stages.FetchResults);
            BakingCompleteProfiling.GetProgressRange(out float progress0, out float progress1);
            for (int c = 0; c < numCells; ++c)
            {
                var cell = bakingCells[c];

                m_CellPosToIndex.Add(cell.position, cell.index);

                if (cell.probePositions == null)
                    continue;

                int numProbes = cell.probePositions.Length;
                Debug.Assert(numProbes > 0);

                if (c % 10 == 0)
                    EditorUtility.DisplayProgressBar("Baking Probe Volumes", $"({c} of {numCells}) Read Cell Probes", Mathf.Lerp(progress0, progress1, c / (float)numCells));

                cell.sh = new SphericalHarmonicsL2[numProbes];
                cell.validity = new float[numProbes];
                cell.validityNeighbourMask = new byte[numProbes];
                cell.offsetVectors = new Vector3[virtualOffsets != null ? numProbes : 0];
                cell.touchupVolumeInteraction = new float[numProbes];
                cell.minSubdiv = probeRefVolume.GetMaxSubdivision();

                // Find the subset of touchup volumes that will be considered for this cell.
                // Capacity of the list to cover the worst case.
                var localTouchupVolumes = new List<(ProbeReferenceVolume.Volume obb, Bounds aabb, ProbeTouchupVolume touchupVolume)>(touchupVolumes.Length);
                foreach (var touchup in touchupVolumesAndBounds)
                {
                    if (touchup.aabb.Intersects(cell.bounds))
                        localTouchupVolumes.Add(touchup);
                }

                for (int i = 0; i < numProbes; ++i)
                {
                    int j = cell.probeIndices[i];

                    if (virtualOffsets != null)
                        cell.offsetVectors[i] = virtualOffsets[j];

                    SphericalHarmonicsL2 shv = sh[j];
                    float valid = validity[j];

                    int brickIdx = i / 64;
                    int subdivLevel = cell.bricks[brickIdx].subdivisionLevel;
                    cell.minSubdiv = Mathf.Min(cell.minSubdiv, subdivLevel);

                    bool invalidatedProbe = false;
                    float intensityScale = 1.0f;

                    foreach (var touchup in localTouchupVolumes)
                    {
                        var touchupBound = touchup.aabb;
                        var touchupVolume = touchup.touchupVolume;

                        // We check a small box around the probe to give some leniency (a couple of centimeters).
                        var probeBounds = new Bounds(cell.probePositions[i], new Vector3(0.02f, 0.02f, 0.02f));
                        if (touchupVolume.IntersectsVolume(touchup.obb, touchup.aabb, probeBounds))
                        {
                            if (touchupVolume.mode == ProbeTouchupVolume.Mode.InvalidateProbes)
                            {
                                invalidatedProbe = true;

                                if (valid < 0.05f) // We just want to add probes that were not already invalid or close to.
                                {
                                    // We check as below 1 but bigger than 0 in the debug shader, so any value <1 will do to signify touched up.
                                    cell.touchupVolumeInteraction[i] = 0.5f;

                                    s_ForceInvalidatedProbesAndTouchupVols[cell.probePositions[i]] = touchupBound;
                                }
                                break;
                            }
                            else if (touchupVolume.mode == ProbeTouchupVolume.Mode.OverrideValidityThreshold)
                            {
                                float thresh = (1.0f - touchupVolume.overriddenDilationThreshold);
                                // The 1.0f + is used to determine the action (debug shader tests above 1), then we add the threshold to be able to retrieve it in debug phase.
                                cell.touchupVolumeInteraction[i] = 1.0f + thresh;
                                s_CustomDilationThresh[(cell.index, i)] = thresh;
                            }

                            if (touchupVolume.mode == ProbeTouchupVolume.Mode.IntensityScale)
                                intensityScale = touchupVolume.intensityScale;
                            if (intensityScale != 1.0f)
                                cell.touchupVolumeInteraction[i] = 2.0f + intensityScale;
                        }
                    }

                    if (valid < 0.05f && m_BakingBatch.invalidatedPositions.ContainsKey(cell.probePositions[i]) && m_BakingBatch.invalidatedPositions[cell.probePositions[i]])
                    {
                        if (!s_ForceInvalidatedProbesAndTouchupVols.ContainsKey(cell.probePositions[i]))
                            s_ForceInvalidatedProbesAndTouchupVols.Add(cell.probePositions[i], new Bounds());

                        invalidatedProbe = true;
                    }

                    // Compress the range of all coefficients but the DC component to [0..1]
                    // Upper bounds taken from http://ppsloan.org/publications/Sig20_Advances.pptx
                    // Divide each coefficient by DC*f to get to [-1,1] where f is from slide 33
                    for (int rgb = 0; rgb < 3; ++rgb)
                    {
                        for (int k = 0; k < 9; ++k)
                            shv[rgb, k] *= intensityScale;

                        var l0 = shv[rgb, 0];

                        if (l0 == 0.0f)
                        {
                            shv[rgb, 0] = 0.0f;
                            for (int k = 1; k < 9; ++k)
                                shv[rgb, k] = 0.5f;
                        }
                        else if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && valid > dilationSettings.dilationValidityThreshold)
                        {
                            for (int k = 0; k < 9; ++k)
                                shv[rgb, k] = 0.0f;
                        }
                        else
                        {
                            // TODO: We're working on irradiance instead of radiance coefficients
                            //       Add safety margin 2 to avoid out-of-bounds values
                            float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
                            float l2scale = 3.5777088f; // 4/sqrt(5) * 2

                            // L_1^m
                            shv[rgb, 1] = shv[rgb, 1] / (l0 * l1scale * 2.0f) + 0.5f;
                            shv[rgb, 2] = shv[rgb, 2] / (l0 * l1scale * 2.0f) + 0.5f;
                            shv[rgb, 3] = shv[rgb, 3] / (l0 * l1scale * 2.0f) + 0.5f;

                            // L_2^-2
                            shv[rgb, 4] = shv[rgb, 4] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 5] = shv[rgb, 5] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 6] = shv[rgb, 6] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 7] = shv[rgb, 7] / (l0 * l2scale * 2.0f) + 0.5f;
                            shv[rgb, 8] = shv[rgb, 8] / (l0 * l2scale * 2.0f) + 0.5f;

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

                    float currValidity = invalidatedProbe ? 1.0f : valid;
                    byte currValidityNeighbourMask = 255;
                    cell.validity[i] = currValidity;
                    cell.validityNeighbourMask[i] = currValidityNeighbourMask;
                }

                cell.shChunkCount = ProbeBrickPool.GetChunkCount(cell.bricks.Length);

                ComputeValidityMasks(cell);

                m_BakedCells[cell.index] = cell;
            }
            fetchScope.Dispose();

            RestorePhysicsComponentsAfterBaking();
            CleanupOccluders();

            m_BakingBatchIndex = 0;

            // Reset index
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);

            // Extract baking cell if we are baking only active scene.
            if (isBakingSceneSubset)
            {
                ExtractBakingCells();
            }

            using var writeScope = new BakingCompleteProfiling(BakingCompleteProfiling.Stages.WriteBakedData);

            // Map from each scene to its per scene data, and create a new asset for each scene
            var scene2Data = new Dictionary<Scene, ProbeVolumePerSceneData>();
            foreach (var data in fullSceneDataList)
            {
                Debug.Assert(ProbeReferenceVolume.instance.sceneData.SceneHasProbeVolumes(data.gameObject.scene));

                data.asset = ProbeVolumeAsset.CreateAsset(data);
                data.scenarios.TryAdd(ProbeReferenceVolume.instance.lightingScenario, default);
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
                            bakingCellsList = data2BakingCells[data] = new ();

                        bakingCellsList.Add(cell);
                    }
                }
            }

            // Convert baking cells to runtime cells
            foreach (var data in fullSceneDataList)
            {
                // NOTE: Right now we always write out both L0L1 and L2 data, regardless of which Probe Volume SH Bands lighting setting
                //       happens to be active at the time of baking (probeRefVolume.shBands).
                //
                // TODO: Explicitly add an option for storing L2 data to bake sets. Freely mixing cells with different bands
                //       availability is already supported by runtime.

                var asset = data.asset;
                var sceneOfOrigin = data.gameObject.scene;
                var profile = probeRefVolume.sceneData.GetBakingSetForScene(sceneOfOrigin);

                // We might have a scene in the list that doesn't have a baking set, this can happen if we are baking in single scene mode with 2 scene loaded, one of which never baked. 
                if (profile == null)
                    continue;

                asset.StoreProfileData(profile);
                asset.minCellPosition = minCellPosition;
                asset.maxCellPosition = maxCellPosition;
                asset.globalBounds = globalBounds;
                asset.bands = ProbeVolumeSHBands.SphericalHarmonicsL2;

                if (!data2BakingCells.TryGetValue(data, out var bakingCellsList))
                    bakingCellsList = new (); // Can happen if no cell is baked for the scene

                WriteBakingCells(data, bakingCellsList);
                data.ResolveSharedCellData();
            }

            writeScope.Dispose();

            var probeVolumes = GetProbeVolumeList();
            foreach (var probeVolume in probeVolumes)
            {
                probeVolume.OnBakeCompleted();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            probeRefVolume.clearAssetsOnVolumeClear = false;

            m_BakingBatch = null;
            InitDilationShaders(); // Do it now otherwise it messes the loading bar

            foreach (var data in fullSceneDataList)
            {
                // We might have a scene in the list that doesn't have a baking set, this can happen if we are baking in single scene mode with 2 scene loaded, one of which never baked. 
                if (probeRefVolume.sceneData.GetBakingSetForScene(data.gameObject.scene) == null)
                    continue;

                data.QueueAssetLoading();
            }

            // ---- Perform dilation ---
            using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.PerformDilation))
                PerformDilation();

            // Mark old bakes as out of date if needed
            ProbeVolumeLightingTab.instance?.UpdateScenarioStatuses(ProbeReferenceVolume.instance.lightingScenario);

            currentBakingState = BakingStage.OnBakeCompletedFinished;
        }

        static void AnalyzeBrickForIndirectionEntries(ref BakingCell cell)
        {
            var prv = ProbeReferenceVolume.instance;
            int cellSizeInBricks = m_BakingProfile.cellSizeInBricks;
            int entrySubdivLevel = Mathf.Min(m_BakingProfile.simplificationLevels, prv.GetGlobalIndirectionEntryMaxSubdiv());
            int indirectionEntrySizeInBricks = ProbeReferenceVolume.CellSize(entrySubdivLevel);
            int numOfIndirectionEntriesPerCellDim = cellSizeInBricks / indirectionEntrySizeInBricks;

            int numOfEntries = numOfIndirectionEntriesPerCellDim * numOfIndirectionEntriesPerCellDim * numOfIndirectionEntriesPerCellDim;
            cell.indirectionEntryInfo = new IndirectionEntryInfo[numOfEntries];

            // This is fairly naive now, if we need optimization this is the place to be.

            Vector3Int cellPosInEntries = cell.position * numOfIndirectionEntriesPerCellDim;
            Vector3Int cellPosInBricks = cell.position * cellSizeInBricks;

            int totalIndexChunks = 0;
            int i = 0;
            for (int x = 0; x < numOfIndirectionEntriesPerCellDim; ++x)
            {
                for (int y = 0; y < numOfIndirectionEntriesPerCellDim; ++y)
                {
                    for (int z = 0; z < numOfIndirectionEntriesPerCellDim; ++z)
                    {
                        Vector3Int entryPositionInBricks = cellPosInBricks + new Vector3Int(x, y, z) * indirectionEntrySizeInBricks;
                        Bounds entryBoundsInBricks = new Bounds();
                        entryBoundsInBricks.min = entryPositionInBricks;
                        entryBoundsInBricks.max = entryPositionInBricks + new Vector3Int(indirectionEntrySizeInBricks, indirectionEntrySizeInBricks, indirectionEntrySizeInBricks);

                        int minSubdiv = m_BakingProfile.maxSubdivision;
                        bool touchedBrick = false;
                        foreach (Brick b in cell.bricks)
                        {
                            if (b.subdivisionLevel < minSubdiv)
                            {
                                if (b.IntersectArea(entryBoundsInBricks))
                                {
                                    touchedBrick = true;
                                    minSubdiv = b.subdivisionLevel;
                                    if (minSubdiv == 0) break;
                                }
                            }
                        }

                        cell.indirectionEntryInfo[i].minSubdiv = minSubdiv;
                        cell.indirectionEntryInfo[i].positionInBricks = cellPosInBricks + new Vector3Int(x, y, z) * indirectionEntrySizeInBricks;
                        cell.indirectionEntryInfo[i].hasOnlyBiggerBricks = minSubdiv > entrySubdivLevel && touchedBrick;

                        ProbeBrickIndex.IndirectionEntryUpdateInfo unused = new ProbeBrickIndex.IndirectionEntryUpdateInfo();
                        int brickCount = ProbeReferenceVolume.instance.GetNumberOfBricksAtSubdiv(cell.indirectionEntryInfo[i], ref unused);

                        totalIndexChunks += Mathf.CeilToInt((float)brickCount / ProbeBrickIndex.kIndexChunkSize);

                        i++;
                    }
                }
            }

            // Chunk count.
            cell.indexChunkCount = totalIndexChunks;
        }

        static void OnLightingDataCleared()
        {
            Clear();
        }

        // Mathf.HalfToFloat(Mathf.FloatToHalf(float.MaxValue)) returns +inf, so clamp manually to avoid that
        static float s_MaxSHValue = 65504; // IEEE max half

        static ushort SHFloatToHalf(float value)
        {
            return Mathf.FloatToHalf(Mathf.Min(value, s_MaxSHValue));
        }

        static float SHHalfToFloat(ushort value)
        {
            return Mathf.HalfToFloat(value);
        }

        static byte SHFloatToByte(float value)
        {
            return (byte)(Mathf.Clamp(value, 0.0f, 1.0f) * 255.0f);
        }

        static float SHByteToFloat(byte value)
        {
            return value / 255.0f;
        }

        static void WriteToShaderCoeffsL0L1(in SphericalHarmonicsL2 sh, NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz, int offset)
        {
            shaderCoeffsL0L1Rx[offset + 0] = SHFloatToHalf(sh[0, 0]); shaderCoeffsL0L1Rx[offset + 1] = SHFloatToHalf(sh[1, 0]); shaderCoeffsL0L1Rx[offset + 2] = SHFloatToHalf(sh[2, 0]); shaderCoeffsL0L1Rx[offset + 3] = SHFloatToHalf(sh[0, 1]);
            shaderCoeffsL1GL1Ry[offset + 0] = SHFloatToByte(sh[1, 1]); shaderCoeffsL1GL1Ry[offset + 1] = SHFloatToByte(sh[1, 2]); shaderCoeffsL1GL1Ry[offset + 2] = SHFloatToByte(sh[1, 3]); shaderCoeffsL1GL1Ry[offset + 3] = SHFloatToByte(sh[0, 2]);
            shaderCoeffsL1BL1Rz[offset + 0] = SHFloatToByte(sh[2, 1]); shaderCoeffsL1BL1Rz[offset + 1] = SHFloatToByte(sh[2, 2]); shaderCoeffsL1BL1Rz[offset + 2] = SHFloatToByte(sh[2, 3]); shaderCoeffsL1BL1Rz[offset + 3] = SHFloatToByte(sh[0, 3]);
        }

        static void WriteToShaderCoeffsL2(in SphericalHarmonicsL2 sh, NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3, int offset)
        {
            shaderCoeffsL2_0[offset + 0] = SHFloatToByte(sh[0, 4]); shaderCoeffsL2_0[offset + 1] = SHFloatToByte(sh[0, 5]); shaderCoeffsL2_0[offset + 2] = SHFloatToByte(sh[0, 6]); shaderCoeffsL2_0[offset + 3] = SHFloatToByte(sh[0, 7]);
            shaderCoeffsL2_1[offset + 0] = SHFloatToByte(sh[1, 4]); shaderCoeffsL2_1[offset + 1] = SHFloatToByte(sh[1, 5]); shaderCoeffsL2_1[offset + 2] = SHFloatToByte(sh[1, 6]); shaderCoeffsL2_1[offset + 3] = SHFloatToByte(sh[1, 7]);
            shaderCoeffsL2_2[offset + 0] = SHFloatToByte(sh[2, 4]); shaderCoeffsL2_2[offset + 1] = SHFloatToByte(sh[2, 5]); shaderCoeffsL2_2[offset + 2] = SHFloatToByte(sh[2, 6]); shaderCoeffsL2_2[offset + 3] = SHFloatToByte(sh[2, 7]);
            shaderCoeffsL2_3[offset + 0] = SHFloatToByte(sh[0, 8]); shaderCoeffsL2_3[offset + 1] = SHFloatToByte(sh[1, 8]); shaderCoeffsL2_3[offset + 2] = SHFloatToByte(sh[2, 8]);
        }

        static void ReadFromShaderCoeffsL0L1(ref SphericalHarmonicsL2 sh, NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz, int offset)
        {
            sh[0, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 0]); sh[1, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 1]); sh[2, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 2]); sh[0, 1] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 3]);
            sh[1, 1] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 0]); sh[1, 2] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 1]); sh[1, 3] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 2]); sh[0, 2] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 3]);
            sh[2, 1] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 0]); sh[2, 2] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 1]); sh[2, 3] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 2]); sh[0, 3] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 3]);
        }

        static void ReadFromShaderCoeffsL2(ref SphericalHarmonicsL2 sh, NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3, int offset)
        {
            sh[0, 4] = SHByteToFloat(shaderCoeffsL2_0[offset + 0]); sh[0, 5] = SHByteToFloat(shaderCoeffsL2_0[offset + 1]); sh[0, 6] = SHByteToFloat(shaderCoeffsL2_0[offset + 2]); sh[0, 7] = SHByteToFloat(shaderCoeffsL2_0[offset + 3]);
            sh[1, 4] = SHByteToFloat(shaderCoeffsL2_1[offset + 0]); sh[1, 5] = SHByteToFloat(shaderCoeffsL2_1[offset + 1]); sh[1, 6] = SHByteToFloat(shaderCoeffsL2_1[offset + 2]); sh[1, 7] = SHByteToFloat(shaderCoeffsL2_1[offset + 3]);
            sh[2, 4] = SHByteToFloat(shaderCoeffsL2_2[offset + 0]); sh[2, 5] = SHByteToFloat(shaderCoeffsL2_2[offset + 1]); sh[2, 6] = SHByteToFloat(shaderCoeffsL2_2[offset + 2]); sh[2, 7] = SHByteToFloat(shaderCoeffsL2_2[offset + 3]);
            sh[0, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 0]); sh[1, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 1]); sh[2, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 2]);
        }

        static void ReadFullFromShaderCoeffsL0L1L2(ref SphericalHarmonicsL2 sh,
            NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz,
            NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3,
            int probeIdx)
        {
            ReadFromShaderCoeffsL0L1(ref sh, shaderCoeffsL0L1Rx, shaderCoeffsL1GL1Ry, shaderCoeffsL1BL1Rz, probeIdx * 4);
            ReadFromShaderCoeffsL2(ref sh, shaderCoeffsL2_0, shaderCoeffsL2_1, shaderCoeffsL2_2, shaderCoeffsL2_3, probeIdx * 4);

        }

        // Returns index in the GPU layout of probe of coordinate (x, y, z) in the brick at brickIndex for a DataLocation of size locSize
        static int GetProbeGPUIndex(int brickIndex, int x, int y, int z, Vector3Int locSize)
        {
            Vector3Int locSizeInBrick = locSize / ProbeBrickPool.kBrickProbeCountPerDim;

            int bx = brickIndex % locSizeInBrick.x;
            int by = (brickIndex / locSizeInBrick.x) % locSizeInBrick.y;
            int bz = ((brickIndex / locSizeInBrick.x) / locSizeInBrick.y) % locSizeInBrick.z;

            // In probes
            int ix = bx * ProbeBrickPool.kBrickProbeCountPerDim + x;
            int iy = by * ProbeBrickPool.kBrickProbeCountPerDim + y;
            int iz = bz * ProbeBrickPool.kBrickProbeCountPerDim + z;

            return ix + locSize.x * (iy + locSize.y * iz);
        }

        static BakingCell ConvertCellToBakingCell(Cell cell)
        {
            BakingCell bc = new BakingCell
            {
                position = cell.position,
                index = cell.index,
                bricks = cell.bricks.ToArray(),
                minSubdiv = cell.minSubdiv,
                indexChunkCount = cell.indexChunkCount,
                shChunkCount = cell.shChunkCount,
                probeIndices = null, // Not needed for this conversion.
                indirectionEntryInfo = cell.indirectionEntryInfo,
            };

            // Runtime Cell arrays may contain padding to match chunk size
            // so we use the actual probe count for these arrays.
            int probeCount = cell.probeCount;
            bc.probePositions = new Vector3[probeCount];
            bc.validity = new float[probeCount];
            bc.touchupVolumeInteraction = new float[probeCount];
            bc.validityNeighbourMask = new byte[probeCount];
            bc.offsetVectors = new Vector3[probeCount];
            bc.sh = new SphericalHarmonicsL2[probeCount];

            // Runtime data layout is for GPU consumption.
            // We need to convert it back to a linear layout for the baking cell.
            int brickCount = probeCount / ProbeBrickPool.kBrickProbeCountTotal;
            int probeIndex = 0;
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(chunkSizeInProbes);

            for (int brickIndex = 0; brickIndex < brickCount; ++brickIndex)
            {
                int chunkIndex = brickIndex / ProbeBrickPool.GetChunkSizeInBrickCount();

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            var remappedIndex = chunkIndex * chunkSizeInProbes +  GetProbeGPUIndex(brickIndex, x, y, z, locSize);

                            ReadFullFromShaderCoeffsL0L1L2(ref bc.sh[probeIndex],
                                cell.bakingScenario.shL0L1RxData, cell.bakingScenario.shL1GL1RyData, cell.bakingScenario.shL1BL1RzData,
                                cell.bakingScenario.shL2Data_0, cell.bakingScenario.shL2Data_1, cell.bakingScenario.shL2Data_2, cell.bakingScenario.shL2Data_3, remappedIndex);

                            bc.probePositions[probeIndex] = cell.probePositions[remappedIndex];
                            bc.validity[probeIndex] = cell.validity[remappedIndex];
                            bc.validityNeighbourMask[probeIndex] = cell.validityNeighMaskData[remappedIndex];
                            bc.touchupVolumeInteraction[probeIndex] = cell.touchupVolumeInteraction[remappedIndex];
                            bc.offsetVectors[probeIndex] = cell.offsetVectors[remappedIndex];

                            probeIndex++;
                        }
                    }
                }
            }

            return bc;
        }

        // This is slow, but artists wanted this... This can be optimized later.
        static BakingCell MergeCells(BakingCell dst, BakingCell srcCell)
        {
            int maxSubdiv = Math.Max(dst.bricks[0].subdivisionLevel, srcCell.bricks[0].subdivisionLevel);

            List<(Brick, int, int)> consolidatedBricks = new List<(Brick, int, int)>();
            HashSet<(Vector3Int, int)> addedBricks = new HashSet<(Vector3Int, int)>();

            for (int b = 0; b < dst.bricks.Length; ++b)
            {
                var brick = dst.bricks[b];
                addedBricks.Add((brick.position, brick.subdivisionLevel));
                consolidatedBricks.Add((brick, b, 0));
            }

            // Now with lower priority we grab from src.
            for (int b = 0; b < srcCell.bricks.Length; ++b)
            {
                var brick = srcCell.bricks[b];

                if (!addedBricks.Contains((brick.position, brick.subdivisionLevel)))
                {
                    consolidatedBricks.Add((brick, b, 1));
                }
            }

            // And finally we sort. We don't need to check for anything but brick as we don't have duplicates.
            consolidatedBricks.Sort(((Brick, int, int) lhs, (Brick, int, int) rhs) =>
            {
                if (lhs.Item1.subdivisionLevel != rhs.Item1.subdivisionLevel)
                    return lhs.Item1.subdivisionLevel > rhs.Item1.subdivisionLevel ? -1 : 1;
                if (lhs.Item1.position.z != rhs.Item1.position.z)
                    return lhs.Item1.position.z < rhs.Item1.position.z ? -1 : 1;
                if (lhs.Item1.position.y != rhs.Item1.position.y)
                    return lhs.Item1.position.y < rhs.Item1.position.y ? -1 : 1;
                if (lhs.Item1.position.x != rhs.Item1.position.x)
                    return lhs.Item1.position.x < rhs.Item1.position.x ? -1 : 1;

                return 0;
            });

            BakingCell outCell = new BakingCell();

            int numberOfProbes = consolidatedBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;
            outCell.index = dst.index;
            outCell.position = dst.position;
            outCell.bricks = new Brick[consolidatedBricks.Count];
            outCell.probePositions = new Vector3[numberOfProbes];
            outCell.minSubdiv = Math.Min(dst.minSubdiv, srcCell.minSubdiv);
            outCell.sh = new SphericalHarmonicsL2[numberOfProbes];
            outCell.validity = new float[numberOfProbes];
            outCell.validityNeighbourMask = new byte[numberOfProbes];
            outCell.offsetVectors = new Vector3[numberOfProbes];
            outCell.touchupVolumeInteraction = new float[numberOfProbes];
            outCell.shChunkCount = ProbeBrickPool.GetChunkCount(outCell.bricks.Length);
            // We don't need to analyse here, it will be done upon writing back.
            outCell.indirectionEntryInfo = new IndirectionEntryInfo[srcCell.indirectionEntryInfo.Length];

            BakingCell[] consideredCells = { dst, srcCell };

            for (int i = 0; i < consolidatedBricks.Count; ++i)
            {
                var b = consolidatedBricks[i];
                int brickIndexInSource = b.Item2;

                outCell.bricks[i] = consideredCells[b.Item3].bricks[brickIndexInSource];

                for (int p = 0; p < ProbeBrickPool.kBrickProbeCountTotal; ++p)
                {
                    outCell.probePositions[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].probePositions[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.sh[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].sh[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.validity[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].validity[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.validityNeighbourMask[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].validityNeighbourMask[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.offsetVectors[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].offsetVectors[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.touchupVolumeInteraction[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].touchupVolumeInteraction[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                }
            }
            return outCell;
        }

        static void ExtractBakingCells()
        {
            foreach (var cellIndex in m_BakedCells.Keys)
            {
                m_NewlyBakedCells.Add(cellIndex);
            }

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                var asset = data.asset;
                if (asset == null || asset.cells == null) continue;

                var numberOfCells = asset.cells.Length;

                for (int i = 0; i < numberOfCells; ++i)
                {
                    var cell = asset.cells[i];

                    BakingCell bc = ConvertCellToBakingCell(cell);

                    if (m_NewlyBakedCells.Contains(cell.index) && m_BakedCells.ContainsKey(cell.index))
                    {
                        bc = MergeCells(m_BakedCells[cell.index], bc);
                    }

                    if (!m_BakingBatch.cellIndex2SceneReferences.ContainsKey(cell.index))
                    {
                        m_BakingBatch.cellIndex2SceneReferences.Add(cell.index, new HashSet<Scene>());
                    }

                    m_BakingBatch.cellIndex2SceneReferences[cell.index].Add(data.gameObject.scene);
                    m_BakedCells[cell.index] = bc;
                }
            }
        }

        static long AlignRemainder16(long count) => count % 16L;

        static void WriteNativeArray<T>(System.IO.FileStream fs, NativeArray<T> array) where T : struct
        {
            unsafe
            {
                fs.Write(new ReadOnlySpan<byte>(array.GetUnsafeReadOnlyPtr(), array.Length * UnsafeUtility.SizeOf<T>()));
                fs.Write(new byte[AlignRemainder16(fs.Position)]);
            }
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
            data.GetBlobFileNames(out var cellDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            var asset = data.asset;
            asset.cells = new Cell[bakingCells.Count];
            asset.cellCounts = new ProbeVolumeAsset.CellCounts[bakingCells.Count];
            asset.totalCellCounts = new ProbeVolumeAsset.CellCounts();
            asset.chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();

            for (var i = 0; i < bakingCells.Count; ++i)
            {
                var bakingCell = bakingCells[i];

                AnalyzeBrickForIndirectionEntries(ref bakingCell);

                asset.cells[i] = new Cell
                {
                    position = bakingCell.position,
                    index = bakingCell.index,
                    probeCount = bakingCell.probePositions.Length,
                    minSubdiv = bakingCell.minSubdiv,
                    indexChunkCount = bakingCell.indexChunkCount,
                    shChunkCount = bakingCell.shChunkCount,
                    shBands = asset.bands,
                    indirectionEntryInfo = bakingCell.indirectionEntryInfo,
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
            var probeCountPadded = asset.totalCellCounts.chunksCount * ProbeBrickPool.GetChunkSizeInProbeCount(); // Padded to a multiple of chunk size.
            var count = probeCountPadded * 4; // 4 component per probe per texture.
            using var probesL0L1Rx = new NativeArray<ushort>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1GL1Ry = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1BL1Rz = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellOptionalData
            count = asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? count : 0;
            using var probesL2_0 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_1 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_2 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_3 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSharedData
            using var bricks = new NativeArray<Brick>(asset.totalCellCounts.bricksCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var validityNeighbourMask = new NativeArray<byte>(probeCountPadded, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSupportData
            using var positions = new NativeArray<Vector3>(probeCountPadded, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var touchupVolumeInteraction = new NativeArray<float>(probeCountPadded, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var validity = new NativeArray<float>(probeCountPadded, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var offsets = new NativeArray<Vector3>(probeCountPadded, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sceneStateHash = asset.GetBakingHashCode();
            var startCounts = new ProbeVolumeAsset.CellCounts();

            int chunkOffsetInProbes = 0;
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            int shChunkOffset = 0;
            var shChunkSize = chunkSizeInProbes * 4;

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

                var inputProbesCount = cellCounts.probesCount;
                // Size of the DataLocation used to do the copy texture at runtime. Used to generate the right layout for the 3D texture.
                Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());

                int shidx = 0;

                // Here we directly map each chunk to the layout of the 3D textures in order to be able to copy the data directly to the GPU.
                // The granularity at runtime is one chunk at a time currently so the temporary data loc used is sized accordingly.
                for (int chunkIndex = 0; chunkIndex < cellCounts.chunksCount; ++chunkIndex)
                {
                    var probesTargetL0L1Rx = probesL0L1Rx.GetSubArray(shChunkOffset, shChunkSize);
                    var probesTargetL1GL1Ry = probesL1GL1Ry.GetSubArray(shChunkOffset, shChunkSize);
                    var probesTargetL1BL1Rz = probesL1BL1Rz.GetSubArray(shChunkOffset, shChunkSize);
                    var validityNeighboorMaskChunkTarget = validityNeighbourMask.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);
                    var positionsChunkTarget = positions.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);
                    var validityChunkTarget = validity.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);
                    var offsetChunkTarget = offsets.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);
                    var touchupVolumeInteractionChunkTarget = touchupVolumeInteraction.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);

                    NativeArray<byte> probesTargetL2_0 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_1 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_2 = default(NativeArray<byte>);
                    NativeArray<byte> probesTargetL2_3 = default(NativeArray<byte>);

                    if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        probesTargetL2_0 = probesL2_0.GetSubArray(shChunkOffset, shChunkSize);
                        probesTargetL2_1 = probesL2_1.GetSubArray(shChunkOffset, shChunkSize);
                        probesTargetL2_2 = probesL2_2.GetSubArray(shChunkOffset, shChunkSize);
                        probesTargetL2_3 = probesL2_3.GetSubArray(shChunkOffset, shChunkSize);
                    }

                    for (int brickIndex = 0; brickIndex < asset.chunkSizeInBricks; brickIndex++)
                    {
                        for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                        {
                            for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                            {
                                for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                                {
                                    int index = GetProbeGPUIndex(brickIndex, x, y, z, locSize);

                                    // We are processing chunks at a time.
                                    // So in practice we can go over the number of SH we have in the input list.
                                    // We fill with encoded black to avoid copying garbage in the final atlas.
                                    if (shidx >= inputProbesCount)
                                    {
                                        WriteToShaderCoeffsL0L1(blackSH, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);

                                        validityNeighboorMaskChunkTarget[index] = 0;
                                        validityChunkTarget[index] = 0.0f;
                                        positionsChunkTarget[index] = Vector3.zero;
                                        offsetChunkTarget[index] = Vector3.zero;
                                        touchupVolumeInteractionChunkTarget[index] = 0.0f;

                                        if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                            WriteToShaderCoeffsL2(blackSH, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);
                                    }
                                    else
                                    {
                                        ref var sh = ref bakingCell.sh[shidx];

                                        WriteToShaderCoeffsL0L1(sh, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);

                                        validityChunkTarget[index] = bakingCell.validity[shidx];
                                        validityNeighboorMaskChunkTarget[index] = bakingCell.validityNeighbourMask[shidx];
                                        positionsChunkTarget[index] = bakingCell.probePositions[shidx];
                                        // TODO for Julien: Just skip if we don't have the offsets; no need to store the data.
                                        // Shouldn't be hard, but currently it is expecting it will always be there.
                                        offsetChunkTarget[index] = shidx < bakingCell.offsetVectors.Length ? bakingCell.offsetVectors[shidx] : Vector3.zero;
                                        touchupVolumeInteractionChunkTarget[index] = bakingCell.touchupVolumeInteraction[shidx];

                                        if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                        {
                                            WriteToShaderCoeffsL2(sh, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);
                                        }
                                    }
                                    shidx++;
                                }
                            }
                        }
                    }

                    shChunkOffset += shChunkSize;
                    chunkOffsetInProbes += chunkSizeInProbes;
                }

                bricks.GetSubArray(startCounts.bricksCount, cellCounts.bricksCount).CopyFrom(bakingCell.bricks);

                startCounts.Add(cellCounts);
            }

            // Need to save here because the forced import below discards the changes.
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellSharedDataFilename, cellSupportDataFilename });
            if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2) outputPaths.Add(cellOptionalDataFilename);
            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL0L1Rx);
                    WriteNativeArray(fs, probesL1GL1Ry);
                    WriteNativeArray(fs, probesL1BL1Rz);
                }

                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        WriteNativeArray(fs, probesL2_0);
                        WriteNativeArray(fs, probesL2_1);
                        WriteNativeArray(fs, probesL2_2);
                        WriteNativeArray(fs, probesL2_3);
                    }
                }

                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, bricks);
                    WriteNativeArray(fs, validityNeighbourMask);
                }
                using (var fs = new System.IO.FileStream(cellSupportDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, positions);
                    WriteNativeArray(fs, touchupVolumeInteraction);
                    WriteNativeArray(fs, validity);
                    WriteNativeArray(fs, offsets);
                }
            }

            AssetDatabase.ImportAsset(cellDataFilename);

            if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                AssetDatabase.ImportAsset(cellOptionalDataFilename);
            else
                AssetDatabase.DeleteAsset(cellOptionalDataFilename);

            AssetDatabase.ImportAsset(cellSharedDataFilename);
            AssetDatabase.ImportAsset(cellSupportDataFilename);

            data.scenarios[ProbeReferenceVolume.instance.lightingScenario] = new ProbeVolumePerSceneData.PerScenarioData
            {
                sceneHash = sceneStateHash,
                cellDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellDataFilename),
                cellOptionalDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellOptionalDataFilename),
            };
            data.cellSharedDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellSharedDataFilename);
            data.cellSupportDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(cellSupportDataFilename);
            EditorUtility.SetDirty(data);
        }

        unsafe static void WriteDilatedCells(ProbeVolumePerSceneData data, List<Cell> cells)
        {
            data.GetBlobFileNames(out var cellDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            var asset = data.asset;

            // CellData
            // Need full chunks so we don't use totalCellCounts.probesCount
            var probeCountPadded = asset.totalCellCounts.chunksCount * ProbeBrickPool.GetChunkSizeInProbeCount(); // Padded to a multiple of chunk size.
            var count = probeCountPadded * 4; // 4 component per probe per texture.
            using var probesL0L1Rx = new NativeArray<ushort>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1GL1Ry = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL1BL1Rz = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellOptionalData
            count = asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? count : 0;
            using var probesL2_0 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_1 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_2 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using var probesL2_3 = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int currCellOffset = 0;

            for (var i = 0; i < cells.Count; ++i)
            {
                var srcCell = cells[i];

                // Find the cell in asset to grab the right indices.
                int assetCellIdx = 0;
                for (; assetCellIdx < asset.cells.Length; ++assetCellIdx)
                {
                    if (asset.cells[assetCellIdx].index == srcCell.index)
                        break;
                }

                var cellCounts = asset.cellCounts[assetCellIdx];

                // IMPORTANT!! We are assuming the size is all the same (4 entries per probe per array). Change if required.
                var inputSHDataSize = srcCell.bakingScenario.shL0L1RxData.Length;

                // Only SH data is touched by dilation, no need to update the rest.
                probesL0L1Rx.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL0L1RxData);
                probesL1GL1Ry.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL1GL1RyData);
                probesL1BL1Rz.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL1BL1RzData);

                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    probesL2_0.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL2Data_0);
                    probesL2_1.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL2Data_1);
                    probesL2_2.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL2Data_2);
                    probesL2_3.GetSubArray(currCellOffset, inputSHDataSize).CopyFrom(srcCell.bakingScenario.shL2Data_3);
                }

                currCellOffset += inputSHDataSize;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellSharedDataFilename, cellSupportDataFilename });
            if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2) outputPaths.Add(cellOptionalDataFilename);
            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL0L1Rx);
                    WriteNativeArray(fs, probesL1GL1Ry);
                    WriteNativeArray(fs, probesL1BL1Rz);
                }

                if (asset.bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        WriteNativeArray(fs, probesL2_0);
                        WriteNativeArray(fs, probesL2_1);
                        WriteNativeArray(fs, probesL2_2);
                        WriteNativeArray(fs, probesL2_3);
                    }
                }
            }
        }

        private static void DeduplicateProbePositions(in Vector3[] probePositions, in int[] brickSubdivLevel, Dictionary<int, int> positionToIndex, BakingBatch batch,
            List<Vector3> positionList, out int[] indices)
        {
            indices = new int[probePositions.Length];
            int uniqueIndex = positionToIndex.Count;

            for (int i = 0; i < probePositions.Length; i++)
            {
                var pos = probePositions[i];
                var brickSubdiv = brickSubdivLevel[i];
                int probeHash = batch.GetProbePositionHash(pos);

                if (positionToIndex.TryGetValue(probeHash, out var index))
                {
                    indices[i] = index;
                    int oldBrickLevel = batch.uniqueBrickSubdiv[probeHash];
                    if (brickSubdiv < oldBrickLevel)
                        batch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                }
                else
                {
                    positionToIndex[probeHash] = uniqueIndex;
                    indices[i] = uniqueIndex;
                    batch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                    positionList.Add(pos);
                    uniqueIndex++;
                }
            }
        }

        public static void OnBakeCompletedCleanup()
        {
            Lightmapping.bakeCompleted -= OnBakeCompletedCleanup;

            ProbeReferenceVolume.instance.checksDuringBakeAction = null;
            partialBakeSceneList = null;

            if (currentBakingState != BakingStage.OnBakeCompletedFinished && currentBakingState != BakingStage.OnBakeCompletedStarted)
            {
                if (m_BakingBatch != null && m_BakingBatch.uniqueProbeCount == 0)
                    OnAdditionalProbesBakeCompleted();
                else
                {
                    // Dequeue the call if something has failed.
                    UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
                    if (m_BakingBatch != null)
                        UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, null);

                    RestorePhysicsComponentsAfterBaking();
                    CleanupOccluders();
                }
            }

            s_ExcludedColliders = null;
            s_ExcludedRigidBodies = null;
            s_AddedOccluders = null;

            // We need to reset that view
            ProbeReferenceVolume.instance.ResetDebugViewToMaxSubdiv();
        }

        public static void RunPlacement()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;
            ProbeReferenceVolume.instance.checksDuringBakeAction = CheckPVChanges;
            AdditionalGIBakeRequestsManager.instance.AddRequestsToLightmapper();
            Lightmapping.bakeCompleted += OnBakeCompletedCleanup;

            ClearBakingBatch();


            ProbeSubdivisionResult result;

            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();
            // Overwrite loaded settings with data from profile. Note that the m_BakingProfile is already patched up if isFreezingPlacement
            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(m_BakingProfile.minBrickSize, m_BakingProfile.maxSubdivision);

            if (isFreezingPlacement)
            {
                result = GetBricksFromLoaded();
            }
            else
            {
                var ctx = PrepareProbeSubdivisionContext();

                // Subdivide the scene and place the bricks
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.BakeBricks))
                    result = BakeBricks(ctx);
            }

            // Compute probe positions and send them to the Lightmapper
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.ApplySubdivisionResults))
            {
                float brickSize = m_BakingProfile.minBrickSize;
                Matrix4x4 newRefToWS = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(brickSize, brickSize, brickSize));
                ApplySubdivisionResults(result, newRefToWS);
            }

            // Restore loaded asset settings
            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(prevBrickSize, prevMaxSubdiv);
        }

        public static ProbeSubdivisionContext PrepareProbeSubdivisionContext()
        {
            ProbeSubdivisionContext ctx = new ProbeSubdivisionContext();

            // Prepare all the information in the scene for baking GI.
            Vector3 refVolOrigin = Vector3.zero; // TODO: This will need to be center of the world bounds.
            var perSceneDataList = GetPerSceneDataList();

            if (m_BakingProfile == null)
            {
                if (perSceneDataList.Count == 0) return ctx;
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

            using (var gpuResources = ProbePlacement.AllocateGPUResources(ctx.probeVolumes.Count, ctx.profile))
            {
                // subdivide all the cells and generate brick positions
                foreach (var cell in ctx.cells)
                {
                    var scenesInCell = new HashSet<Scene>();

                    // Calculate overlaping probe volumes to avoid unnecessary work
                    var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)>();
                    foreach (var probeVolume in ctx.probeVolumes)
                    {
                        if (ProbeVolumePositioning.OBBAABBIntersect(probeVolume.volume, cell.bounds, probeVolume.bounds))
                        {
                            overlappingProbeVolumes.Add(probeVolume);
                            scenesInCell.Add(probeVolume.component.gameObject.scene);
                        }
                    }

                    // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                    var filteredContributors = ctx.contributors.Filter(ctx.profile, cell.bounds, overlappingProbeVolumes);

                    if (overlappingProbeVolumes.Count == 0 && filteredContributors.Count == 0)
                        continue;

                    var bricks = ProbePlacement.SubdivideCell(cell.bounds, ctx, gpuResources, filteredContributors, overlappingProbeVolumes);
                    if (bricks.Length == 0)
                        continue;

                    foreach (var renderer in filteredContributors.renderers)
                        scenesInCell.Add(renderer.component.gameObject.scene);
                    foreach (var terrain in filteredContributors.terrains)
                        scenesInCell.Add(terrain.component.gameObject.scene);

                    result.cells.Add((cell.position, cell.bounds, bricks));
                    result.scenesPerCells[cell.position] = scenesInCell;
                }
            }

            return result;
        }

        public static ProbeSubdivisionResult GetBricksFromLoaded()
        {
            var dataList = GetPerSceneDataList();
            var result = new ProbeSubdivisionResult();

            foreach (var data in dataList)
            {
                var asset = data.asset;
                var cellSize = asset.minDistanceBetweenProbes * 3.0f * asset.cellSizeInBricks;
                Vector3 cellDimensions = new Vector3(cellSize, cellSize, cellSize);

                // Loop through cells in asset, we need to be careful as there'll be duplicates.
                // As we go through the cells we fill ProbeSubdivisionResult as we go.
                foreach (var cell in asset.cells)
                {
                    var cellPos = cell.position;

                    if (!result.scenesPerCells.ContainsKey(cellPos))
                    {
                        result.scenesPerCells[cellPos] = new HashSet<Scene>();

                        var center = new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, (cellPos.z + 0.5f) * cellSize);
                        result.cells.Add((cellPos, new Bounds(center, cellDimensions), cell.bricks.ToArray()));
                    }
                    result.scenesPerCells[cellPos].Add(data.gameObject.scene);
                }
            }

            return result;
        }

        public static bool ModifyProfileFromLoadedData(ProbeVolumeBakingSet profile)
        {
            var dataList = GetPerSceneDataList();
            if (dataList.Count > 0)
            {
                var data = dataList[0];

                profile.simplificationLevels = (int)Mathf.Log(data.asset.cellSizeInBricks, 3);
                profile.minDistanceBetweenProbes = data.asset.minDistanceBetweenProbes;
                globalBounds = data.asset.globalBounds;
                return true;
            }

            return false;
        }

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        internal static void ConvertBricksToPositions(ref BakingCell cell, Brick[] bricks, Vector3[] outProbePositions, int[] outBrickSubdiv)
        {
            int posIdx = 0;
            float scale = ProbeReferenceVolume.instance.MinBrickSize() / ProbeBrickPool.kBrickCellCount;

            foreach (var b in bricks)
            {
                int brickSize = ProbeReferenceVolume.CellSize(b.subdivisionLevel);
                Vector3Int brickOffset = b.position * ProbeBrickPool.kBrickCellCount;

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            var probeOffset = brickOffset + new Vector3Int(x, y, z) * brickSize;

                            outProbePositions[posIdx] = (Vector3)probeOffset * scale;
                            outBrickSubdiv[posIdx] = b.subdivisionLevel;
                            posIdx++;
                        }
                    }
                }
            }
        }

        static int PosToIndex(Vector3Int pos)
        {
            Vector3Int normalizedPos = pos - minCellPosition;
            return normalizedPos.z * (cellCount.x * cellCount.y) + normalizedPos.y * cellCount.x + normalizedPos.x;
        }

        public static void ApplySubdivisionResults(ProbeSubdivisionResult results, Matrix4x4 refToWS)
        {
            // For now we just have one baking batch. Later we'll have more than one for a set of scenes.
            // All probes need to be baked only once for the whole batch and not once per cell
            // The reason is that the baker is not deterministic so the same probe position baked in two different cells may have different values causing seams artefacts.
            m_BakingBatch = new BakingBatch(m_BakingBatchIndex++, cellCount);

            int i = 0;
            BakingSetupProfiling.GetProgressRange(out float progress0, out float progress1);

            List <Vector3> positionList = new();
            Dictionary<int, int> positionToIndex = new();
            foreach ((var position, var bounds, var bricks) in results.cells)
            {
                if (++i % 10 == 0)
                    EditorUtility.DisplayProgressBar("Baking Probe Volumes", $"({i} of {results.cells.Count}) Subdivide Cell", Mathf.Lerp(progress0, progress1, i / (float)results.cells.Count));

                var probePositions = new Vector3[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];
                var brickSubdivLevels = new int[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];

                BakingCell cell = new BakingCell()
                {
                    index = PosToIndex(position),
                    position = position,
                    bounds = bounds,
                    bricks = bricks,
                    probePositions = probePositions,
                };

                ConvertBricksToPositions(ref cell, bricks, probePositions, brickSubdivLevels);
                DeduplicateProbePositions(in probePositions, in brickSubdivLevels, positionToIndex, m_BakingBatch, positionList, out cell.probeIndices);

                m_BakingBatch.cells.Add(cell);
                m_BakingBatch.cellIndex2SceneReferences[cell.index] = new HashSet<Scene>(results.scenesPerCells[cell.position]);
            }

            var positions = positionList.ToArray();

            // Virtually offset positions before passing them to lightmapper
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.ApplyVirtualOffsets))
                ApplyVirtualOffsets(positions, out m_BakingBatch.virtualOffsets);

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, positions);
        }
    }
}
