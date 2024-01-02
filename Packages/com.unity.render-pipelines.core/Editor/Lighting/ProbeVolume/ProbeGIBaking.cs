using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SceneManagement;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using Cell = UnityEngine.Rendering.ProbeReferenceVolume.Cell;
using CellDesc = UnityEngine.Rendering.ProbeReferenceVolume.CellDesc;
using CellData = UnityEngine.Rendering.ProbeReferenceVolume.CellData;
using IndirectionEntryInfo = UnityEngine.Rendering.ProbeReferenceVolume.IndirectionEntryInfo;
using StreamableCellDesc = UnityEngine.Rendering.ProbeVolumeStreamableAsset.StreamableCellDesc;

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
        public Dictionary<int, HashSet<string>> cellIndex2SceneReferences = new Dictionary<int, HashSet<string>>();
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

    class ProbeVolumeProfileInfo
    {
        public int simplificationLevels;
        public float minDistanceBetweenProbes;

        public int maxSubdivision => ProbeVolumeBakingSet.GetMaxSubdivision(simplificationLevels);
        public float minBrickSize => ProbeVolumeBakingSet.GetMinBrickSize(minDistanceBetweenProbes);
        public int cellSizeInBricks => ProbeVolumeBakingSet.GetCellSizeInBricks(simplificationLevels);
        public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;
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

                if (LogFile != null)
                    Profiling.Profiler.BeginSample(stage.ToString());
            }

            public void OnDispose(ref T currentStage)
            {
                if (disposed) return;
                disposed = true;

                if (LogFile != null)
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
                OnBakeStarted,
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

        struct CellCounts
        {
            public int bricksCount;
            public int chunksCount;

            public void Add(CellCounts o)
            {
                bricksCount += o.bricksCount;
                chunksCount += o.chunksCount;
            }
        }

        struct CellChunkData
        {
            public bool scenarioValid;

            public NativeArray<ushort> shL0L1RxData;
            public NativeArray<byte> shL1GL1RyData;
            public NativeArray<byte> shL1BL1RzData;

            // Optional L2 Data
            public NativeArray<byte> shL2Data_0;
            public NativeArray<byte> shL2Data_1;
            public NativeArray<byte> shL2Data_2;
            public NativeArray<byte> shL2Data_3;

            public NativeArray<byte> validityNeighMaskData;
        }

        public const string kAPVStreamingAssetsPath = "APVStreamingAssets";

        static bool m_IsInit = false;
        static BakingBatch m_BakingBatch;
        static ProbeVolumeBakingSet m_BakingSet = null;
        static CellCounts m_TotalCellCounts;

        static internal ProbeVolumeProfileInfo m_ProfileInfo = null;

        static int m_BakingBatchIndex = 0;

        static Bounds globalBounds = new Bounds();
        static Vector3Int minCellPosition = Vector3Int.one * int.MaxValue;
        static Vector3Int maxCellPosition = Vector3Int.one * int.MinValue;
        static Vector3Int cellCount = Vector3Int.zero;

        static BakingStage currentBakingState = BakingStage.NotStarted;
        static int pvHashesAtBakeStart = -1;

        static Dictionary<Vector3Int, int> m_CellPosToIndex = new Dictionary<Vector3Int, int>();
        static Dictionary<int, BakingCell> m_BakedCells = new Dictionary<int, BakingCell>();
        // We need to keep the original list of cells that were actually baked to feed it to the dilation process.
        // This is because during partial bake we only want to dilate those cells.
        static Dictionary<int, BakingCell> m_CellsToDilate = new Dictionary<int, BakingCell>();

        internal static List<string> partialBakeSceneList = null;
        internal static bool isBakingSceneSubset => partialBakeSceneList != null;
        internal static bool isFreezingPlacement = false;

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

        static SphericalHarmonicsL2 s_BlackSH;
        static bool s_BlackSHInitialized = false;

        static SphericalHarmonicsL2 GetBlackSH()
        {
            if (!s_BlackSHInitialized)
            {
                // Init SH with values that will resolve to black
                s_BlackSH = new SphericalHarmonicsL2();
                for (int channel = 0; channel < 3; ++channel)
                {
                    s_BlackSH[channel, 0] = 0.0f;
                    for (int coeff = 1; coeff < 9; ++coeff)
                        s_BlackSH[channel, coeff] = 0.5f;
                }
            }

            return s_BlackSH;
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
            var activeSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(SceneManager.GetActiveScene());

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.Clear();

            ProbeReferenceVolume.instance.Clear();

            if (activeSet != null)
                activeSet.Clear();

            var probeVolumes = GameObject.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.InstanceID);
            foreach (var probeVolume in probeVolumes)
                probeVolume.OnLightingDataAssetCleared();
        }

        public static bool CanFreezePlacement()
        {
            if (!ProbeReferenceVolume.instance.supportLightingScenarios)
                return false;

            // Check if all the scene datas in the scene have a baking set, if  not then we cannot enable this option.
            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0)
                return false;

            foreach (var sceneData in sceneDataList)
            {
                if (sceneData.bakingSet == null || sceneData.bakingSet.GetSceneCellIndexList(sceneData.sceneGUID) == null)
                    return false;
            }

            return true;
        }

        internal static void GetProbeAndChunkIndex(int globalProbeIndex, out int chunkIndex, out int chunkProbeIndex)
        {
            var chunkSizeInProbeCount = ProbeBrickPool.GetChunkSizeInProbeCount();
            chunkIndex = globalProbeIndex / chunkSizeInProbeCount;
            chunkProbeIndex = globalProbeIndex - chunkIndex * chunkSizeInProbeCount;
        }

        public static void FindWorldBounds()
        {
            ProbeReferenceVolume.instance.clearAssetsOnVolumeClear = true;

            var sceneData = ProbeReferenceVolume.instance.sceneData;
            HashSet<string> scenesToConsider = new HashSet<string>();

            var activeScene = SceneManager.GetActiveScene();
            var activeSet = sceneData.GetBakingSetForScene(activeScene);

            bool hasFoundBounds = false;

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
            bool isBakingSingleScene = false;
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var bakingSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(scene);
                if (bakingSet != null && bakingSet.singleSceneMode)
                {
                    isBakingSingleScene = true;
                    break;
                }
            }

            // We need to make sure all scenes we are baking are from the same baking set.
            // TODO: This should be ensured by the controlling panel, until we have that we need to assert.
            for (int i = 0; i < perSceneData.Count; ++i)
            {
                var data = perSceneData[i];
                var scene = data.gameObject.scene;
                var bakingSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(scene);

                if (bakingSet == null)
                {
                    if (isBakingSingleScene)
                        continue;

                    Debug.LogError($"Scene '{scene.name}' does not belong to any Baking Set. Please add it to a Baking Set in the Probe Volumes tab of the Lighting Window.");
                    return false;
                }

                bakingSet.SetActiveScenario(bakingSet.lightingScenario, verbose: false); // Ensure we are not blending any other scenario.
                bakingSet.BlendLightingScenario(null, 0.0f);

                // In case a scene is duplicated, we need to store the right scene GUID
                if (data.sceneGUID != data.gameObject.scene.GetGUID())
                {
                    data.sceneGUID = data.gameObject.scene.GetGUID();
                }

                if (i == 0)
                {
                    m_BakingSet = bakingSet;
                }
                else if (!m_BakingSet.IsEquivalent(bakingSet))
                    return false;
            }

            return true;
        }

        static void EnsurePerSceneDataInOpenScenes()
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            var activeScene = SceneManager.GetActiveScene();

            // We assume that all the per scene data for all the scenes in the set have been set with the scene been saved at least once. However we also update the scenes that are currently loaded anyway for security.
            // and to have a new trigger to update the bounds we have.
            int openedScenesCount = SceneManager.sceneCount;
            for (int i = 0; i < openedScenesCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                sceneData.OnSceneSaving(scene); // We need to perform the same actions we do when the scene is saved.
                var activeSet = sceneData.GetBakingSetForScene(activeScene); // Must be done after OnSceneSaved because it can put the set in the default baking set if needed.
                var sceneBakingSet = sceneData.GetBakingSetForScene(scene);
                if (sceneBakingSet != null && sceneBakingSet != activeSet && sceneData.SceneHasProbeVolumes(scene))
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

        static ProbeVolumeProfileInfo GetProfileInfoFromBakingSet(ProbeVolumeBakingSet set)
        {
            var result = new ProbeVolumeProfileInfo();
            result.minDistanceBetweenProbes = set.minDistanceBetweenProbes;
            result.simplificationLevels = set.simplificationLevels;
            return result;
        }

        static public bool InitializeBake()
        {
            if (ProbeVolumeLightingTab.instance?.PrepareAPVBake() == false) return false;
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP) return false;

            using var scope = new BakingSetupProfiling(BakingSetupProfiling.Stages.PrepareWorldSubdivision);

            // Verify to make sure we can still do it. Shortcircuting so that we don't run CanFreezePlacement unless is needed.
            isFreezingPlacement = isFreezingPlacement && CanFreezePlacement();
            if (!isFreezingPlacement)
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.EnsurePerSceneDataInOpenScenes))
                    EnsurePerSceneDataInOpenScenes();
            }

            if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return false;

            var sceneDataList = GetPerSceneDataList();
            if (sceneDataList.Count == 0) return false;

            var pvList = GetProbeVolumeList();
            if (pvList.Count == 0) return false; // We have no probe volumes.

            CachePVHashes(pvList);

            currentBakingState = BakingStage.Started;

            if (!SetBakingContext(sceneDataList))
                return false;

            m_TotalCellCounts = new CellCounts();
            m_ProfileInfo = GetProfileInfoFromBakingSet(m_BakingSet);

            if (isFreezingPlacement)
            {
                ModifyProfileFromLoadedData(m_BakingSet);
            }
            else
            {
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.FindWorldBounds))
                    FindWorldBounds();
            }

            // Get min/max
            CellCountInDirections(out minCellPosition, out maxCellPosition, m_ProfileInfo.cellSizeInMeters);
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            ProbeReferenceVolume.instance.EnsureCurrentBakingSet(m_BakingSet);

            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
            {
                // It can be null if the scene was never added to a baking set and we are baking in single scene mode, in that case we don't have a baking set for it yet and we need to skip 
                if (ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(data.gameObject.scene))
                    data.Initialize();
            }

            return true;
        }

        static void OnBakeStarted()
        {
            using var scope = new BakingSetupProfiling(BakingSetupProfiling.Stages.OnBakeStarted);

            if (!InitializeBake())
                return;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;
            ProbeReferenceVolume.instance.checksDuringBakeAction = CheckPVChanges;
            AdditionalGIBakeRequestsManager.instance.AddRequestsToLightmapper();
            Lightmapping.bakeCompleted += OnBakeCompletedCleanup;

            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.PlaceProbes))
            {

                Vector3[] positions = RunPlacement();
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(m_BakingBatch.index, positions);
            }

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

        static CellChunkData GetCellChunkData(CellData cellData, int chunkIndex)
        {
            var result = new CellChunkData();

            int chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            int chunkOffset = chunkSizeInProbes * chunkIndex;

            if (m_BakingSet != null)
            {
                result.scenarioValid = cellData.scenarios.TryGetValue(m_BakingSet.lightingScenario, out var scenarioData);

                if (result.scenarioValid)
                {
                    result.shL0L1RxData = scenarioData.shL0L1RxData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    result.shL1GL1RyData = scenarioData.shL1GL1RyData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    result.shL1BL1RzData = scenarioData.shL1BL1RzData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);

                    if (scenarioData.shL2Data_0.Length > 0) // we might have no L2 if we are not during baking but during touchup interaction
                    {
                        result.shL2Data_0 = scenarioData.shL2Data_0.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_1 = scenarioData.shL2Data_1.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_2 = scenarioData.shL2Data_2.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_3 = scenarioData.shL2Data_3.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    }
                }
            }

            result.validityNeighMaskData = cellData.validityNeighMaskData.GetSubArray(chunkOffset, chunkSizeInProbes);

            return result;
        }

        // NOTE: This is somewhat hacky and is going to likely be slow (or at least slower than it could).
        // It is only a first iteration of the concept that won't be as impactful on memory as other options.
        internal static void RevertDilation()
        {
            if (m_BakingSet == null)
            {
                if (ProbeReferenceVolume.instance.perSceneDataList.Count == 0) return;
                SetBakingContext(ProbeReferenceVolume.instance.perSceneDataList);
            }

            var dilationSettings = m_BakingSet.settings.dilationSettings;
            var blackProbe = new SphericalHarmonicsL2();

            int chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                for (int i = 0; i < cell.data.validity.Length; ++i)
                {
                    if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && cell.data.validity[i] > dilationSettings.dilationValidityThreshold)
                    {
                        GetProbeAndChunkIndex(i, out var chunkIndex, out var index);

                        var cellChunkData = GetCellChunkData(cell.data, chunkIndex);

                        WriteToShaderCoeffsL0L1(blackProbe, cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData, index * 4);
                        WriteToShaderCoeffsL2(blackProbe, cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, index * 4);
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

            List<Cell> tempLoadedCells = new List<Cell>();

            var prv = ProbeReferenceVolume.instance;

            SetBakingContext(perSceneDataList);

            var dilationSettings = m_BakingSet.settings.dilationSettings;

            if (dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f)
            {
                // Make sure all assets are loaded.
                prv.PerformPendingOperations();

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
                        foreach (var cell in prv.cells.Values)
                        {
                            if (m_CellsToDilate.ContainsKey(cell.desc.index))
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

                        foreach (var cell in prv.cells.Values)
                        {
                            tempLoadedCells.Clear();

                            var cellPos = cell.desc.position;
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
                                                    Debug.LogError($"Not enough memory to perform dilation for cell {cell.desc.index}");
                                            }
                                        }
                                    }

                            if (m_CellsToDilate.ContainsKey(cell.desc.index))
                            {
                                PerformDilation(cell, dilationSettings);
                                dilatedCells.Add(cell);
                            }

                            // Free memory again.
                            foreach (var cellToUnload in tempLoadedCells)
                                prv.UnloadCell(cellToUnload);
                        }
                    }


                    // Now write back the assets.
                    WriteDilatedCells(dilatedCells);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // Reload data
                    foreach (var sceneData in perSceneDataList)
                    {
                        sceneData.QueueSceneRemoval();
                        sceneData.QueueSceneLoading();
                    }
                    prv.PerformPendingOperations();
                }
            }
        }

        static Dictionary<int, int> RemapBakedCells(bool isBakingSubset)
        {
            // When baking a baking set. It is possible that cells layout has changed (min and max position of cells in the set).
            // If this is the case then the cell index for a given position will change.
            // Because of this, when doing partial bakes, we need to generate a remapping table of the old cells to the new layout in order to be able to update existing data.
            Dictionary<int, int> oldToNewCellRemapping = new Dictionary<int, int>();

            if (isBakingSubset)
            {
                // Layout has changed but is still compatible. Remap all cells that are not part of the bake.
                if (minCellPosition != m_BakingSet.minCellPosition && maxCellPosition != m_BakingSet.maxCellPosition)
                {
                    var alreadyBakedCells = m_BakingSet.cellDescs;
                    var newCells = new SerializedDictionary<int, CellDesc>();

                    // Generate remapping for all cells baked the last time.
                    foreach (var cellKvP in alreadyBakedCells)
                    {
                        var cell = cellKvP.Value;
                        int oldIndex = cell.index;
                        int remappedIndex = PosToIndex(cell.position);
                        oldToNewCellRemapping.Add(oldIndex, remappedIndex);

                        cell.index = remappedIndex;
                        newCells.Add(oldIndex, cell);
                    }
                }
            }

            return oldToNewCellRemapping;
        }

        static void GenerateScenesCellLists(List<ProbeVolumePerSceneData> bakedSceneDataList, Dictionary<int, int> cellRemapTable)
        {
            bool needRemap = cellRemapTable.Count != 0;

            // Build lists of scene GUIDs and assign baking set to the PerSceneData.
            var bakedSceneGUIDList = new List<string>();
            foreach (var data in bakedSceneDataList)
            {
                Debug.Assert(ProbeReferenceVolume.instance.sceneData.SceneHasProbeVolumes(data.sceneGUID));
                bakedSceneGUIDList.Add(data.sceneGUID);

                data.bakingSet = m_BakingSet;
                EditorUtility.SetDirty(data);
            }

            var currentPerSceneCellList = m_BakingSet.perSceneCellLists; // Cell lists from last baking.
            m_BakingSet.perSceneCellLists = new SerializedDictionary<string, List<int>>();

            // Partial baking: Copy over scene cell lists for scenes not being baked.
            // Layout change: Remap indices.
            foreach (var scene in currentPerSceneCellList)
            {
                // Scene is not baked. Remap if needed or add it back to the baking set.
                if (!bakedSceneGUIDList.Contains(scene.Key))
                {
                    if (needRemap)
                    {
                        var newCellList = new List<int>();
                        foreach (var cell in scene.Value)
                            newCellList.Add(cellRemapTable[cell]);

                        m_BakingSet.perSceneCellLists.Add(scene.Key, newCellList);
                    }
                    else
                    {
                        m_BakingSet.perSceneCellLists.Add(scene.Key, scene.Value);
                    }
                }
            }

            // Allocate baked cells to the relevant scenes cell list.
            foreach (var cell in m_BakedCells.Values)
            {
                foreach (var scene in m_BakingBatch.cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a probe volume in it?
                    if (bakedSceneGUIDList.Contains(scene))
                    {
                        List<int> indexList;
                        if (!m_BakingSet.perSceneCellLists.TryGetValue(scene, out indexList))
                        {
                            indexList = new List<int>();
                            m_BakingSet.perSceneCellLists.Add(scene, indexList);
                        }

                        indexList.Add(cell.index);
                    }
                }
            }

            EditorUtility.SetDirty(m_BakingSet);
        }

        static void PrepareCellsForWriting(bool isBakingSubset)
        {
            // Remap if needed existing Cell descriptors in the baking set.
            var cellRemapTable = RemapBakedCells(isBakingSubset);

            // Generate list of cells for all cells being baked and remap untouched existing scenes if needed.
            GenerateScenesCellLists(GetPerSceneDataList(), cellRemapTable);

            if (isBakingSubset)
            {
                // Resolve all unloaded scene cells in CPU memory. This will allow us to extract them into BakingCells in order to have the full list for writing.
                // Other cells should already be in the baked cells list.
                var loadedSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
                foreach(var sceneGUID in m_BakingSet.sceneGUIDs)
                {
                    // If a scene was baked
                    if (m_BakingSet.perSceneCellLists.TryGetValue(sceneGUID, out var cellList))
                    {
                        // And the scene is not loaded
                        if (!loadedSceneDataList.Exists((x) => x.sceneGUID == sceneGUID) && cellList.Count != 0)
                        {
                            // Resolve its data in CPU memory.
                            bool resolved = m_BakingSet.ResolveCellData(sceneGUID);
                            Debug.Assert(resolved, "Could not resolve unloaded scene data");
                        }
                    }
                }

                // Extract all cells that weren't baked into baking cells.
                // Merge existing data of cells belonging both to the baking scene list and to scenes not being baked (prevents losing placement data for those).
                // This way we have a full cell list to provide to WriteBakingCells
                ExtractBakingCells();
            }
        }

        static public void ApplyPostBakeOperations(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            var probeRefVolume = ProbeReferenceVolume.instance;
            var bakingCells = m_BakingBatch.cells;
            var numCells = bakingCells.Count;

            m_CellPosToIndex.Clear();
            m_BakedCells.Clear();
            m_CellsToDilate.Clear();

            // Clear baked data
            foreach (var data in probeRefVolume.perSceneDataList)
                data.QueueSceneRemoval();
            probeRefVolume.Clear();

            // Make sure all pending operations are done (needs to be after the Clear to unload all previous scenes)
            probeRefVolume.PerformPendingOperations();
            probeRefVolume.SetMinBrickAndMaxSubdiv(m_ProfileInfo.minBrickSize, m_ProfileInfo.maxSubdivision);

            // Use the globalBounds we just computed, as the one in probeRefVolume doesn't include scenes that have never been baked
            probeRefVolume.globalBounds = globalBounds;

            var dilationSettings = m_BakingSet.settings.dilationSettings;
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
                m_CellsToDilate[cell.index] = cell;
            }
            fetchScope.Dispose();

            m_BakingBatchIndex = 0;
            
            PrepareCellsForWriting(isBakingSceneSubset);

            using var writeScope = new BakingCompleteProfiling(BakingCompleteProfiling.Stages.WriteBakedData);

            var fullSceneDataList = probeRefVolume.perSceneDataList;

            m_BakingSet.chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();
            m_BakingSet.minCellPosition = minCellPosition;
            m_BakingSet.maxCellPosition = maxCellPosition;
            m_BakingSet.globalBounds = globalBounds;
            m_BakingSet.maxSHChunkCount = -1;

            m_BakingSet.scenarios.TryAdd(m_BakingSet.lightingScenario, new ProbeVolumeBakingSet.PerScenarioDataInfo());

            // Convert baking cells to runtime cells
            WriteBakingCells(m_BakedCells.Values.ToArray());

            // Reset internal structures depending on current bake.
            probeRefVolume.EnsureCurrentBakingSet(m_BakingSet);

            // This subsequent block needs to happen AFTER we call WriteBakingCells.
            // Otherwise in cases where we change the spacing between probes, we end up loading cells with a certain layout in ForceSHBand
            // And then we unload cells using the wrong layout in PerformDilation (after WriteBakingCells updates the baking set object) which leads to a broken internal state.

            // Don't use Disk streaming to avoid having to wait for it when doing dilation.
            ProbeReferenceVolume.instance.ForceNoDiskStreaming(true);
            // Force maximum sh bands to perform baking, we need to store what sh bands was selected from the settings as we need to restore it after.
            var prevSHBands = ProbeReferenceVolume.instance.shBands;
            ProbeReferenceVolume.instance.ForceSHBand(ProbeVolumeSHBands.SphericalHarmonicsL2);

            // TODO Discuss: Not nice to do this here, shouldn't reloading the asset also resolve cell data?
            // Would still need to reload common shared data like bricks as they are separately handled by the baking set itself.
            // Load common shared data (bricks + debug)
            // Load streamable shared data.
            foreach (var data in ProbeReferenceVolume.instance.perSceneDataList)
                data.Initialize();

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
                data.QueueSceneLoading();

            // ---- Perform dilation ---
            using (new BakingCompleteProfiling(BakingCompleteProfiling.Stages.PerformDilation))
                PerformDilation();

            // Need to restore the original sh bands
            ProbeReferenceVolume.instance.ForceSHBand(prevSHBands);
            ProbeReferenceVolume.instance.ForceNoDiskStreaming(false);

            // Mark old bakes as out of date if needed
            ProbeVolumeLightingTab.instance?.UpdateScenarioStatuses(ProbeReferenceVolume.instance.lightingScenario);

            currentBakingState = BakingStage.OnBakeCompletedFinished;
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

            ApplyPostBakeOperations(sh, validity);
        }

        static void AnalyzeBrickForIndirectionEntries(ref BakingCell cell)
        {
            var prv = ProbeReferenceVolume.instance;
            int cellSizeInBricks = m_ProfileInfo.cellSizeInBricks;
            int entrySubdivLevel = Mathf.Min(m_ProfileInfo.simplificationLevels, prv.GetGlobalIndirectionEntryMaxSubdiv());
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

                        int minSubdiv = m_ProfileInfo.maxSubdivision;
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
            if (ProbeReferenceVolume.instance == null)
                return;
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
                return;

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
            if (shaderCoeffsL2_0.Length > 0)
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

        static BakingCell ConvertCellToBakingCell(CellDesc cellDesc, CellData cellData)
        {
            BakingCell bc = new BakingCell
            {
                position = cellDesc.position,
                index = cellDesc.index,
                bricks = cellData.bricks.ToArray(),
                minSubdiv = cellDesc.minSubdiv,
                indexChunkCount = cellDesc.indexChunkCount,
                shChunkCount = cellDesc.shChunkCount,
                probeIndices = null, // Not needed for this conversion.
                indirectionEntryInfo = cellDesc.indirectionEntryInfo,
            };

            bool hasVirtualOffsets = cellData.offsetVectors.Length > 0;

            // Runtime Cell arrays may contain padding to match chunk size
            // so we use the actual probe count for these arrays.
            int probeCount = cellDesc.probeCount;
            bc.probePositions = new Vector3[probeCount];
            bc.validity = new float[probeCount];
            bc.touchupVolumeInteraction = new float[probeCount];
            bc.validityNeighbourMask = new byte[probeCount];
            bc.offsetVectors = hasVirtualOffsets ? new Vector3[probeCount] : null;
            bc.sh = new SphericalHarmonicsL2[probeCount];

            // Runtime data layout is for GPU consumption.
            // We need to convert it back to a linear layout for the baking cell.
            int probeIndex = 0;
            int chunkOffsetInProbes = 0;
            var chunksCount = cellDesc.shChunkCount;
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(chunkSizeInProbes);

            var blackSH = GetBlackSH();

            for (int chunkIndex = 0; chunkIndex < chunksCount; ++chunkIndex)
            {
                var cellChunkData = GetCellChunkData(cellData, chunkIndex);

                for (int brickIndex = 0; brickIndex < m_BakingSet.chunkSizeInBricks; ++brickIndex)
                {
                    if (probeIndex >= probeCount)
                        break;

                    for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                    {
                        for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                        {
                            for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                            {
                                var remappedIndex = GetProbeGPUIndex(brickIndex, x, y, z, locSize);

                                // Scenario data can be invalid due to partially baking the set.
                                if (cellChunkData.scenarioValid)
                                    ReadFullFromShaderCoeffsL0L1L2(ref bc.sh[probeIndex], cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData,
                                        cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, remappedIndex);
                                else
                                    bc.sh[probeIndex] = blackSH;

                                bc.validityNeighbourMask[probeIndex] = cellChunkData.validityNeighMaskData[remappedIndex];

                                remappedIndex += chunkOffsetInProbes;
                                bc.probePositions[probeIndex] = cellData.probePositions[remappedIndex];
                                bc.validity[probeIndex] = cellData.validity[remappedIndex];
                                bc.touchupVolumeInteraction[probeIndex] = cellData.touchupVolumeInteraction[remappedIndex];
                                if (hasVirtualOffsets)
                                    bc.offsetVectors[probeIndex] = cellData.offsetVectors[remappedIndex];

                                probeIndex++;
                            }
                        }
                    }
                }

                chunkOffsetInProbes += chunkSizeInProbes;
            }

            return bc;
        }

        // This is slow, but artists wanted this... This can be optimized later.
        static BakingCell MergeCells(BakingCell dst, BakingCell srcCell)
        {
            int maxSubdiv = Math.Max(dst.bricks[0].subdivisionLevel, srcCell.bricks[0].subdivisionLevel);
            bool hasVirtualOffsets = m_BakingBatch.virtualOffsets != null;

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
            outCell.offsetVectors = hasVirtualOffsets ? new Vector3[numberOfProbes] : null;
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
                    if (hasVirtualOffsets)
                    outCell.offsetVectors[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].offsetVectors[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                    outCell.touchupVolumeInteraction[i * ProbeBrickPool.kBrickProbeCountTotal + p] = consideredCells[b.Item3].touchupVolumeInteraction[brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p];
                }
            }
            return outCell;
        }

        static void ExtractBakingCells()
        {
            // For cells that are being baked, this loop will merge existing baked data with newly baked data to not lose data.
            var loadedSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
            foreach (var data in loadedSceneDataList)
            {
                var cells = m_BakingSet.GetSceneCellIndexList(data.sceneGUID);

                var numberOfCells = cells.Count;

                for (int i = 0; i < numberOfCells; ++i)
                {
                    if (m_BakedCells.ContainsKey(cells[i]))
                    {
                        var cell = m_BakingSet.GetCellDesc(cells[i]);

                        // This can happen if doing a partial bake before ever doing a full bake.
                        if (cell == null)
                            continue;

                        var cellData = m_BakingSet.GetCellData(cells[i]);

                        // When doing partial baking some cells might not have any already baked data.
                        if (cellData == null || !cellData.scenarios.ContainsKey(m_BakingSet.lightingScenario))
                            continue;

                        BakingCell bc = ConvertCellToBakingCell(cell, cellData);
                        bc = MergeCells(m_BakedCells[cell.index], bc);
                        m_BakedCells[cell.index] = bc;
                    }
                }
            }

            // Here we convert to baking cells all cells that were not already baked.
            // This allows us to have the full set of cells ready for writing all at once.
            foreach (var cell in m_BakingSet.cellDescs.Values)
            {
                if (!m_BakedCells.ContainsKey(cell.index))
                {
                    var cellData = m_BakingSet.GetCellData(cell.index);
                    m_BakedCells.Add(cell.index, ConvertCellToBakingCell(cell, cellData));
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
        unsafe static void WriteBakingCells(BakingCell[] bakingCells)
        {
            m_BakingSet.GetBlobFileNames(m_BakingSet.lightingScenario, out var cellDataFilename, out var cellBricksDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            m_BakingSet.cellDescs = new SerializedDictionary<int, CellDesc>();
            m_BakingSet.bakedMinDistanceBetweenProbes = m_ProfileInfo.minDistanceBetweenProbes;
            m_BakingSet.bakedSimplificationLevels = m_ProfileInfo.simplificationLevels;

            var cellSharedDataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellL0L1DataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellL2DataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellBricksDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellSupportDescs = new SerializedDictionary<int, StreamableCellDesc>();

            var voSettings = m_BakingSet.settings.virtualOffsetSettings;
            bool hasVirtualOffsets = voSettings.useVirtualOffset;

            for (var i = 0; i < bakingCells.Length; ++i)
            {
                AnalyzeBrickForIndirectionEntries(ref bakingCells[i]);
                var bakingCell = bakingCells[i];

                m_BakingSet.cellDescs.Add(bakingCell.index, new CellDesc
                {
                    position = bakingCell.position,
                    index = bakingCell.index,
                    probeCount = bakingCell.probePositions.Length,
                    minSubdiv = bakingCell.minSubdiv,
                    indexChunkCount = bakingCell.indexChunkCount,
                    shChunkCount = bakingCell.shChunkCount,
                    indirectionEntryInfo = bakingCell.indirectionEntryInfo,
                    bricksCount = bakingCell.bricks.Length,
                });

                m_BakingSet.maxSHChunkCount = Mathf.Max(m_BakingSet.maxSHChunkCount, bakingCell.shChunkCount);

                m_TotalCellCounts.Add(new CellCounts
                {
                    bricksCount = bakingCell.bricks.Length,
                    chunksCount = bakingCell.shChunkCount
                });
            }

            // All per probe data is stored per chunk and contiguously for each cell.
            // This is done so that we can stream from disk one cell at a time by group of chunks.

            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            // CellData
            // L0 and L1 Data: 12 Coeffs stored in 3 textures. L0 (rgb) and R1x as ushort in one texture, the rest as byte in two 4 component textures.
            var L0L1R1xChunkSize = sizeof(ushort) * 4 * chunkSizeInProbes; // 4 ushort components per probe
            var L1ChunkSize = sizeof(byte) * 4 * chunkSizeInProbes; // 4 components per probe
            var L0L1ChunkSize = L0L1R1xChunkSize + 2 * L1ChunkSize;
            var L0L1TotalSize = m_TotalCellCounts.chunksCount * L0L1ChunkSize;
            using var probesL0L1 = new NativeArray<byte>(L0L1TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.L0ChunkSize = L0L1R1xChunkSize;
            m_BakingSet.L1ChunkSize = L1ChunkSize;

            // CellOptionalData
            // L2 Data: 15 Coeffs stored in 4 byte4 textures.
            var L2TextureChunkSize = 4 * sizeof(byte) * chunkSizeInProbes; // 4 byte component per probe
            var L2ChunkSize = L2TextureChunkSize * 4; // 4 Textures for all L2 data.
            var L2TotalSize = m_TotalCellCounts.chunksCount * L2ChunkSize; // 4 textures
            using var probesL2 = new NativeArray<byte>(L2TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.L2TextureChunkSize = L2TextureChunkSize;

            // CellSharedData (only validity for now)
            var validityMaskChunkSize = sizeof(byte) * chunkSizeInProbes;
            var sharedDataChunkSize = validityMaskChunkSize;
            var sharedDataTotalSize = m_TotalCellCounts.chunksCount * sharedDataChunkSize;
            using var sharedData = new NativeArray<byte>(sharedDataTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.validityMaskChunkSize = validityMaskChunkSize;


            // Brick data
            using var bricks = new NativeArray<Brick>(m_TotalCellCounts.bricksCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSupportData
            m_BakingSet.supportPositionChunkSize = sizeof(Vector3) * chunkSizeInProbes;
            m_BakingSet.supportValidityChunkSize = sizeof(float) * chunkSizeInProbes;
            m_BakingSet.supportOffsetsChunkSize = hasVirtualOffsets ? sizeof(Vector3) * chunkSizeInProbes : 0;
            m_BakingSet.supportTouchupChunkSize = sizeof(float) * chunkSizeInProbes;

            m_BakingSet.supportDataChunkSize = m_BakingSet.supportPositionChunkSize + m_BakingSet.supportValidityChunkSize + m_BakingSet.supportOffsetsChunkSize + m_BakingSet.supportTouchupChunkSize;
            var supportDataTotalSize = m_TotalCellCounts.chunksCount * m_BakingSet.supportDataChunkSize;
            using var supportData = new NativeArray<byte>(supportDataTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sceneStateHash = m_BakingSet.GetBakingHashCode();
            var startCounts = new CellCounts();

            int chunkOffsetInProbes = 0;

            int shL0L1ChunkOffset = 0;
            int shL2ChunkOffset = 0;
            int supportChunkOffset = 0;

            var blackSH = GetBlackSH();

            // Size of the DataLocation used to do the copy texture at runtime. Used to generate the right layout for the 3D texture.
            Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());

            for (var i = 0; i < bakingCells.Length; ++i)
            {
                var bakingCell = bakingCells[i];
                var cellDesc = m_BakingSet.cellDescs[bakingCell.index];
                var chunksCount = cellDesc.shChunkCount;

                cellSharedDataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * sharedDataChunkSize, elementCount = chunksCount });
                cellL0L1DataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * L0L1ChunkSize, elementCount = chunksCount });
                cellL2DataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * L2ChunkSize, elementCount = chunksCount });
                cellBricksDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.bricksCount * sizeof(Brick), elementCount = cellDesc.bricksCount });
                cellSupportDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * m_BakingSet.supportDataChunkSize, elementCount = chunksCount });

                sceneStateHash = sceneStateHash * 23 + bakingCell.GetBakingHashCode();

                var inputProbesCount = bakingCell.probePositions.Length;

                int shidx = 0;

                // Cell base offsets for each data streams
                int cellL0R1xOffset = shL0L1ChunkOffset;
                int cellL1GL1RyOffset = cellL0R1xOffset + chunksCount * L0L1R1xChunkSize;
                int cellL1BL1RzOffset = cellL1GL1RyOffset + chunksCount * L1ChunkSize;

                int positionOffset = supportChunkOffset;
                int validityOffset = positionOffset + chunksCount * m_BakingSet.supportPositionChunkSize;
                int touchupOffset = validityOffset + chunksCount * m_BakingSet.supportValidityChunkSize;
                int offsetsOffset = touchupOffset + chunksCount * m_BakingSet.supportTouchupChunkSize; // Keep last as it's optional.

                // Here we directly map each chunk to the layout of the 3D textures in order to be able to copy the data directly to the GPU.
                // The granularity at runtime is one chunk at a time currently so the temporary data loc used is sized accordingly.
                for (int chunkIndex = 0; chunkIndex < chunksCount; ++chunkIndex)
                {
                    NativeArray<ushort> probesTargetL0L1Rx = probesL0L1.GetSubArray(cellL0R1xOffset + chunkIndex * L0L1R1xChunkSize, L0L1R1xChunkSize).Reinterpret<ushort>(1);
                    NativeArray<byte> probesTargetL1GL1Ry = probesL0L1.GetSubArray(cellL1GL1RyOffset + chunkIndex * L1ChunkSize, L1ChunkSize);
                    NativeArray<byte> probesTargetL1BL1Rz = probesL0L1.GetSubArray(cellL1BL1RzOffset + chunkIndex * L1ChunkSize, L1ChunkSize);

                    NativeArray<byte> validityNeighboorMaskChunkTarget = sharedData.GetSubArray(chunkOffsetInProbes, chunkSizeInProbes);

                    NativeArray<Vector3> positionsChunkTarget = supportData.GetSubArray(positionOffset + chunkIndex * m_BakingSet.supportPositionChunkSize, m_BakingSet.supportPositionChunkSize).Reinterpret<Vector3>(1);
                    NativeArray<float> validityChunkTarget = supportData.GetSubArray(validityOffset + chunkIndex * m_BakingSet.supportValidityChunkSize, m_BakingSet.supportValidityChunkSize).Reinterpret<float>(1);
                    NativeArray<float> touchupVolumeInteractionChunkTarget = supportData.GetSubArray(touchupOffset + chunkIndex * m_BakingSet.supportTouchupChunkSize, m_BakingSet.supportTouchupChunkSize).Reinterpret<float>(1);
                    NativeArray<Vector3> offsetChunkTarget = supportData.GetSubArray(offsetsOffset + chunkIndex * m_BakingSet.supportOffsetsChunkSize, m_BakingSet.supportOffsetsChunkSize).Reinterpret<Vector3>(1);

                    NativeArray<byte> probesTargetL2_0 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 0 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_1 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 1 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_2 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 2 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_3 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 3 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);

                    for (int brickIndex = 0; brickIndex < m_BakingSet.chunkSizeInBricks; brickIndex++)
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
                                        WriteToShaderCoeffsL2(blackSH, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);

                                        validityNeighboorMaskChunkTarget[index] = 0;
                                        validityChunkTarget[index] = 0.0f;
                                        positionsChunkTarget[index] = Vector3.zero;
                                        touchupVolumeInteractionChunkTarget[index] = 0.0f;
                                        if (hasVirtualOffsets)
                                            offsetChunkTarget[index] = Vector3.zero;
                                    }
                                    else
                                    {
                                        ref var sh = ref bakingCell.sh[shidx];

                                        WriteToShaderCoeffsL0L1(sh, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);
                                        WriteToShaderCoeffsL2(sh, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);

                                        validityChunkTarget[index] = bakingCell.validity[shidx];
                                        validityNeighboorMaskChunkTarget[index] = bakingCell.validityNeighbourMask[shidx];
                                        positionsChunkTarget[index] = bakingCell.probePositions[shidx];
                                        touchupVolumeInteractionChunkTarget[index] = bakingCell.touchupVolumeInteraction[shidx];
                                        if (hasVirtualOffsets)
                                            offsetChunkTarget[index] = bakingCell.offsetVectors[shidx];
                                    }
                                    shidx++;
                                }
                            }
                        }
                    }

                    chunkOffsetInProbes += chunkSizeInProbes;
                }

                shL0L1ChunkOffset += (chunksCount * L0L1ChunkSize);
                shL2ChunkOffset += (chunksCount * L2ChunkSize);
                supportChunkOffset += (chunksCount * m_BakingSet.supportDataChunkSize);

                bricks.GetSubArray(startCounts.bricksCount, cellDesc.bricksCount).CopyFrom(bakingCell.bricks);

                startCounts.Add(new CellCounts()
                {
                    bricksCount = cellDesc.bricksCount,
                    chunksCount = cellDesc.shChunkCount
                });
            }

            // Need to save here because the forced import below discards the changes.
            EditorUtility.SetDirty(m_BakingSet);
            AssetDatabase.SaveAssets();

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellBricksDataFilename, cellSharedDataFilename, cellSupportDataFilename, cellOptionalDataFilename });

            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL0L1);
                }
                using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL2);
                }
                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, sharedData);
                }
                using (var fs = new System.IO.FileStream(cellBricksDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, bricks);
                }
                using (var fs = new System.IO.FileStream(cellSupportDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, supportData);
                }
            }

            AssetDatabase.ImportAsset(cellDataFilename);
            AssetDatabase.ImportAsset(cellOptionalDataFilename);
            AssetDatabase.ImportAsset(cellBricksDataFilename);
            AssetDatabase.ImportAsset(cellSharedDataFilename);
            AssetDatabase.ImportAsset(cellSupportDataFilename);

            var bakingSetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_BakingSet));

            m_BakingSet.scenarios[ProbeReferenceVolume.instance.lightingScenario] = new ProbeVolumeBakingSet.PerScenarioDataInfo
            {
                sceneHash = sceneStateHash,
                cellDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellL0L1DataDescs, L0L1ChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellDataFilename)),
                cellOptionalDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellL2DataDescs, L2ChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellOptionalDataFilename)),
            };
            m_BakingSet.cellSharedDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellSharedDataDescs, sharedDataChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellSharedDataFilename));
            m_BakingSet.cellBricksDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellBricksDescs, sizeof(Brick), bakingSetGUID, AssetDatabase.AssetPathToGUID(cellBricksDataFilename));
            m_BakingSet.cellSupportDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellSupportDescs, m_BakingSet.supportDataChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellSupportDataFilename));

            EditorUtility.SetDirty(m_BakingSet);
        }

        unsafe static void WriteDilatedCells(List<Cell> cells)
        {
            m_BakingSet.GetBlobFileNames(m_BakingSet.lightingScenario, out var cellDataFilename, out var cellBricksDataFilename, out var cellOptionalDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            // CellData
            // L0 and L1 Data: 12 Coeffs stored in 3 textures. L0 (rgb) and R1x as ushort in one texture, the rest as byte in two 4 component textures.
            var L0L1R1xChunkSize = sizeof(ushort) * 4 * chunkSizeInProbes; // 4 ushort components per probe
            var L1ChunkSize = sizeof(byte) * 4 * chunkSizeInProbes; // 4 components per probe
            var L0L1ChunkSize = L0L1R1xChunkSize + 2 * L1ChunkSize;
            var L0L1TotalSize = m_TotalCellCounts.chunksCount * L0L1ChunkSize;
            using var probesL0L1 = new NativeArray<byte>(L0L1TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);


            // CellOptionalData
            // L2 Data: 15 Coeffs stored in 4 byte4 textures.
            var L2ChunkSize = 4 * sizeof(byte) * chunkSizeInProbes; // 4 byte component per probe
            var L2TotalSize = m_TotalCellCounts.chunksCount * L2ChunkSize * 4; // 4 textures
            using var probesL2 = new NativeArray<byte>(L2TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // When baking with partially loaded scenes, the list of cells being dilated might be smaller than the full list of cells in the bake.
            // In this case, in order not to destroy the rest of the data, we need to load it back before writing.
            if (cells.Count != m_BakingSet.cellDescs.Count)
            {
                probesL0L1.CopyFrom(System.IO.File.ReadAllBytes(cellDataFilename));
                probesL2.CopyFrom(System.IO.File.ReadAllBytes(cellOptionalDataFilename));
            }

            var lightingScenario = ProbeReferenceVolume.instance.lightingScenario;
            Debug.Assert(m_BakingSet.scenarios.ContainsKey(lightingScenario));
            var scenarioDataInfo = m_BakingSet.scenarios[lightingScenario];

            for (var i = 0; i < cells.Count; ++i)
            {
                var srcCell = cells[i];

                var srcCellDesc = srcCell.desc;
                var scenarioData = srcCell.data.scenarios[lightingScenario];

                var L0L1chunkBaseOffset = scenarioDataInfo.cellDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var L2chunkBaseOffset = scenarioDataInfo.cellOptionalDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var shChunksCount = srcCellDesc.shChunkCount;

                NativeArray<ushort> probesTargetL0L1Rx = probesL0L1.GetSubArray(L0L1chunkBaseOffset, L0L1R1xChunkSize * shChunksCount).Reinterpret<ushort>(1);
                NativeArray<byte> probesTargetL1GL1Ry = probesL0L1.GetSubArray(L0L1chunkBaseOffset + shChunksCount * L0L1R1xChunkSize, L1ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL1BL1Rz = probesL0L1.GetSubArray(L0L1chunkBaseOffset + shChunksCount * (L0L1R1xChunkSize + L1ChunkSize), L1ChunkSize * shChunksCount);

                probesTargetL0L1Rx.CopyFrom(scenarioData.shL0L1RxData);
                probesTargetL1GL1Ry.CopyFrom(scenarioData.shL1GL1RyData);
                probesTargetL1BL1Rz.CopyFrom(scenarioData.shL1BL1RzData);

                NativeArray<byte> probesTargetL2_0 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 0, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_1 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 1, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_2 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 2, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_3 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 3, L2ChunkSize * shChunksCount);

                probesTargetL2_0.CopyFrom(scenarioData.shL2Data_0);
                probesTargetL2_1.CopyFrom(scenarioData.shL2Data_1);
                probesTargetL2_2.CopyFrom(scenarioData.shL2Data_2);
                probesTargetL2_3.CopyFrom(scenarioData.shL2Data_3);
            }

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellSharedDataFilename, cellSupportDataFilename, cellOptionalDataFilename });

            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL0L1);
                }

                using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    WriteNativeArray(fs, probesL2);
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
                }
            }

            // We need to reset that view
            ProbeReferenceVolume.instance.ResetDebugViewToMaxSubdiv();
        }

        public static Vector3[] RunPlacement()
        {
            ClearBakingBatch();

            ProbeSubdivisionResult result;
            GIContributors? contributors = null;

            float prevBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            int prevMaxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision();
            // Overwrite loaded settings with data from profile. Note that the m_BakingSet.profile is already patched up if isFreezingPlacement
            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(m_ProfileInfo.minBrickSize, m_ProfileInfo.maxSubdivision);

            if (isFreezingPlacement)
            {
                result = GetBricksFromLoaded();
            }
            else
            {
                var ctx = PrepareProbeSubdivisionContext();
                contributors = ctx.contributors;

                // Subdivide the scene and place the bricks
                using (new BakingSetupProfiling(BakingSetupProfiling.Stages.BakeBricks))
                    result = BakeBricks(ctx);
            }

            // Compute probe positions
            Vector3[] positions;
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.ApplySubdivisionResults))
            {
                float brickSize = m_ProfileInfo.minBrickSize;
                Matrix4x4 newRefToWS = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(brickSize, brickSize, brickSize));
                ApplySubdivisionResults(result, contributors, newRefToWS, out positions);
            }

            // Restore loaded asset settings
            ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(prevBrickSize, prevMaxSubdiv);

            return positions;
        }

        public static ProbeSubdivisionContext PrepareProbeSubdivisionContext(bool liveContext = false)
        {
            ProbeSubdivisionContext ctx = new ProbeSubdivisionContext();

            // Prepare all the information in the scene for baking GI.
            Vector3 refVolOrigin = Vector3.zero; // TODO: This will need to be center of the world bounds.
            var perSceneDataList = GetPerSceneDataList();

            if (m_BakingSet == null)
            {
                if (perSceneDataList.Count == 0) return ctx;
                SetBakingContext(perSceneDataList);
            }

            var profileInfo = m_ProfileInfo;
            if (liveContext || m_ProfileInfo == null)
                profileInfo = GetProfileInfoFromBakingSet(m_BakingSet);

            ctx.Initialize(m_BakingSet, profileInfo, refVolOrigin);
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
                    var scenesInCell = new HashSet<string>();

                    // Calculate overlaping probe volumes to avoid unnecessary work
                    var overlappingProbeVolumes = new List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)>();
                    foreach (var probeVolume in ctx.probeVolumes)
                    {
                        if (ProbeVolumePositioning.OBBAABBIntersect(probeVolume.volume, cell.bounds, probeVolume.bounds))
                        {
                            overlappingProbeVolumes.Add(probeVolume);
                            scenesInCell.Add(ProbeVolumeSceneData.GetSceneGUID(probeVolume.component.gameObject.scene));
                        }
                    }

                    // Calculate valid renderers to avoid unnecessary work (a renderer needs to overlap a probe volume and match the layer)
                    var filteredContributors = ctx.contributors.Filter(ctx.bakingSet, cell.bounds, overlappingProbeVolumes);

                    if (filteredContributors.Count == 0 && !overlappingProbeVolumes.Any(v => v.component.fillEmptySpaces))
                        continue;

                    var bricks = ProbePlacement.SubdivideCell(cell.bounds, ctx, gpuResources, filteredContributors, overlappingProbeVolumes);
                    if (bricks.Length == 0)
                        continue;

                    foreach (var renderer in filteredContributors.renderers)
                        scenesInCell.Add(ProbeVolumeSceneData.GetSceneGUID(renderer.component.gameObject.scene));
                    foreach (var terrain in filteredContributors.terrains)
                        scenesInCell.Add(ProbeVolumeSceneData.GetSceneGUID(terrain.component.gameObject.scene));

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
                var cellSize = m_ProfileInfo.minDistanceBetweenProbes * 3.0f * m_ProfileInfo.cellSizeInBricks;
                Vector3 cellDimensions = new Vector3(cellSize, cellSize, cellSize);

                // Loop through cells in asset, we need to be careful as there'll be duplicates.
                // As we go through the cells we fill ProbeSubdivisionResult as we go.
                var cells = m_BakingSet.GetSceneCellIndexList(data.sceneGUID);
                foreach (var cellIndex in cells)
                {
                    var cellDesc = m_BakingSet.GetCellDesc(cellIndex);
                    var cellData = m_BakingSet.GetCellData(cellIndex);
                    var cellPos = cellDesc.position;

                    if (!result.scenesPerCells.ContainsKey(cellPos))
                    {
                        result.scenesPerCells[cellPos] = new HashSet<string>();

                        var center = new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, (cellPos.z + 0.5f) * cellSize);
                        result.cells.Add((cellPos, new Bounds(center, cellDimensions), cellData.bricks.ToArray()));
                    }
                    result.scenesPerCells[cellPos].Add(data.sceneGUID);
                }
            }

            return result;
        }

        public static void ModifyProfileFromLoadedData(ProbeVolumeBakingSet bakingSet)
        {
            m_ProfileInfo.simplificationLevels = bakingSet.bakedSimplificationLevels;
            m_ProfileInfo.minDistanceBetweenProbes = bakingSet.bakedMinDistanceBetweenProbes;
            globalBounds = bakingSet.globalBounds;
        }

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        internal static void ConvertBricksToPositions(Brick[] bricks, out Vector3[] outProbePositions, out int[] outBrickSubdiv)
        {
            int posIdx = 0;
            float scale = ProbeReferenceVolume.instance.MinBrickSize() / ProbeBrickPool.kBrickCellCount;

            outProbePositions = new Vector3[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];
            outBrickSubdiv = new int[bricks.Length * ProbeBrickPool.kBrickProbeCountTotal];

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

        public static void ApplySubdivisionResults(ProbeSubdivisionResult results, GIContributors? contributors, Matrix4x4 refToWS, out Vector3[] positions)
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

                ConvertBricksToPositions(bricks, out var probePositions, out var brickSubdivLevels);
                DeduplicateProbePositions(in probePositions, in brickSubdivLevels, positionToIndex, m_BakingBatch, positionList, out var probeIndices);

                BakingCell cell = new BakingCell()
                {
                    index = PosToIndex(position),
                    position = position,
                    bounds = bounds,
                    bricks = bricks,
                    probePositions = probePositions,
                    probeIndices = probeIndices,
                };

                m_BakingBatch.cells.Add(cell);
                m_BakingBatch.cellIndex2SceneReferences[cell.index] = new HashSet<string>(results.scenesPerCells[cell.position]);
            }

            positions = positionList.ToArray();

            // Virtually offset positions before passing them to lightmapper
            using (new BakingSetupProfiling(BakingSetupProfiling.Stages.ApplyVirtualOffsets))
                ApplyVirtualOffsets(contributors, positions, out m_BakingBatch.virtualOffsets);
        }
    }
}
