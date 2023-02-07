using System;
using System.IO;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using CellData = UnityEngine.Rendering.ProbeReferenceVolume.CellData;
using CellDesc = UnityEngine.Rendering.ProbeReferenceVolume.CellDesc;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeVolumeBakingSet : ScriptableObject, ISerializationCallbackReceiver
    {
        internal enum Version
        {
            Initial,
        }

        // A StreamableAsset is an asset that is converted to a Streaming Asset for builds.
        // assetGUID is used in editor to handle the asset and streamableAssetPath is updated at build time and is used at runtime.
        [Serializable]
        internal class StreamableAsset
        {
            [Serializable]
            public struct StreamableCellDesc
            {
                public int offset; // Offset of the cell within the file.
                public int elementCount; // Number of elements in the cell (can be data chunks, bricks, debug info, etc)
            }

            public string assetGUID = ""; // In the editor, allows us to load the asset through the AssetDatabase.
            public string streamableAssetPath = ""; // At runtime, path of the asset within the StreamingAssets data folder.
            
            public SerializedDictionary<int, StreamableCellDesc> streamableCellDescs = new SerializedDictionary<int, StreamableCellDesc>();
            public int elementSize; // Size of an element. Can be a data chunk, a brick, etc.

            FileHandle m_AssetFileHandle;

            public string GetAssetPath()
            {
#if UNITY_EDITOR
                return AssetDatabase.GUIDToAssetPath(assetGUID);
#else
                return Path.Combine(Application.streamingAssetsPath, streamableAssetPath);
#endif
            }

            unsafe public bool FileExists()
            {
#if UNITY_EDITOR
                return File.Exists(GetAssetPath());
#else
                FileInfoResult result;
                AsyncReadManager.GetFileInfo(GetAssetPath(), &result).JobHandle.Complete();
                return result.FileState == FileState.Exists;
#endif
            }

            public FileHandle OpenFile()
            {
                if (m_AssetFileHandle.IsValid())
                    return m_AssetFileHandle;

                m_AssetFileHandle = AsyncReadManager.OpenFileAsync(GetAssetPath());
                return m_AssetFileHandle;
            }

            public void CloseFile()
            {
                if (m_AssetFileHandle.IsValid() && m_AssetFileHandle.JobHandle.IsCompleted)
                    m_AssetFileHandle.Close().Complete();

                m_AssetFileHandle = default(FileHandle);
            }

            public StreamableAsset(string apvStreamingAssetsPath, SerializedDictionary<int, StreamableCellDesc> cellDescs, int elementSize, string bakingSetGUID, string assetGUID)
            {
                this.assetGUID = assetGUID;
                this.streamableCellDescs = cellDescs;
                this.elementSize = elementSize;
                streamableAssetPath = Path.Combine(Path.Combine(apvStreamingAssetsPath, bakingSetGUID), assetGUID + ".bytes");
            }

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(assetGUID);
            }

            public void Dispose()
            {
                if (m_AssetFileHandle.IsValid())
                {
                    m_AssetFileHandle.Close().Complete();
                    m_AssetFileHandle = default(FileHandle);
                }
            }
        }

        [Serializable]
        internal struct PerScenarioDataInfo
        {
            public bool IsValid()
            {
                return cellDataAsset != null && !string.IsNullOrEmpty(cellDataAsset.assetGUID); // if cellDataAsset is valid optional data (if available) should always be valid.
            }

            public int sceneHash;
            public StreamableAsset cellDataAsset; // Contains L0 L1 SH data
            public StreamableAsset cellOptionalDataAsset; // Contains L2 SH data
        }

        [Serializable]
        internal struct CellCounts
        {
            public int bricksCount;
            public int chunksCount;

            public void Add(CellCounts o)
            {
                bricksCount += o.bricksCount;
                chunksCount += o.chunksCount;
            }
        }

        // Baking Set Data
        [SerializeField] internal bool singleSceneMode = true;
        [SerializeField] internal ProbeVolumeBakingProcessSettings settings;

        [SerializeField] private List<string> m_SceneGUIDs = new List<string>();
        [SerializeField] internal List<string> scenesToNotBake = new List<string>();
        [SerializeField] internal List<string> lightingScenarios = new List<string>();

        // List of cell descriptors.
        [SerializeField] internal SerializedDictionary<int, CellDesc> cellDescs = new SerializedDictionary<int, CellDesc>();
        [SerializeField] internal SerializedDictionary<int, CellCounts> cellCounts = new SerializedDictionary<int, CellCounts>();

        internal Dictionary<int, CellData> cellDataMap = new Dictionary<int, CellData>();

        [Serializable]
        struct SerializedPerSceneCellList
        {
            public string sceneGUID;
            public List<int> cellList;
        }
        [SerializeField] List<SerializedPerSceneCellList> m_SerializedPerSceneCellList;

        // Can't use SerializedDictionary here because we can't serialize a List of List T_T
        internal Dictionary<string, List<int>> perSceneCellLists = new Dictionary<string, List<int>>();

        // Assets containing actual cell data (SH, Validity, etc)
        // This data will be streamed from disk to the GPU.
        [SerializeField] internal StreamableAsset cellSharedDataAsset = null; // Contains validity data
        [SerializeField] internal SerializedDictionary<string, PerScenarioDataInfo> scenarios = new SerializedDictionary<string, PerScenarioDataInfo>();
        // This data will be streamed from disk but is only needed in CPU memory.
        [SerializeField] internal StreamableAsset cellBricksDataAsset; // Contains bricks data
        [SerializeField] internal StreamableAsset cellSupportDataAsset = null; // Contains debug data

        [SerializeField] internal int chunkSizeInBricks;
        [SerializeField] internal Vector3Int maxCellPosition;
        [SerializeField] internal Vector3Int minCellPosition;
        [SerializeField] internal Bounds globalBounds;
        [SerializeField] internal int bakedSimplificationLevels = -1;
        [SerializeField] internal float bakedMinDistanceBetweenProbes = -1.0f;

        [SerializeField] internal int L0ChunkSize;
        [SerializeField] internal int L1ChunkSize;
        [SerializeField] internal int L2TextureChunkSize; // Optional. Size of the chunk for one texture (4 textures for all data)
        [SerializeField] internal int validityMaskChunkSize; // Shared
        [SerializeField] internal int supportPositionChunkSize;
        [SerializeField] internal int supportValidityChunkSize;
        [SerializeField] internal int supportTouchupChunkSize;
        [SerializeField] internal int supportOffsetsChunkSize;
        [SerializeField] internal int supportDataChunkSize;

        [SerializeField] internal CellCounts totalCellCounts;


        [SerializeField] internal string lightingScenario = ProbeReferenceVolume.defaultLightingScenario;
        string m_OtherScenario = null;
        float m_ScenarioBlendingFactor = 0.0f;

        internal string otherScenario => m_OtherScenario;
        internal float scenarioBlendingFactor => m_ScenarioBlendingFactor;

        ReadCommandArray m_ReadCommandArray = new ReadCommandArray();
        NativeArray<ReadCommand> m_ReadCommandBuffer = new NativeArray<ReadCommand>();
        Stack<NativeArray<byte>> m_ReadOperationScratchBuffers = new Stack<NativeArray<byte>>();
        List<int> m_PrunedIndexList = new List<int>();
        List<int> m_PrunedScenarioIndexList = new List<int>();

        internal IReadOnlyList<string> sceneGUIDs => m_SceneGUIDs;

        // Baking Profile

        [SerializeField]
        Version version = CoreUtils.GetLastEnumValue<Version>();

        // TODO: This is here just to find a place where to serialize it. It might not be the best spot.
        [SerializeField]
        internal bool freezePlacement = false;

        /// <summary>
        /// How many levels contains the probes hierarchical structure.
        /// </summary>
        [Range(2, 5)]
        public int simplificationLevels = 3;

        /// <summary>
        /// The size of a Cell in number of bricks.
        /// </summary>
        public int cellSizeInBricks => GetCellSizeInBricks(bakedSimplificationLevels);

        /// <summary>
        /// The minimum distance between two probes in meters.
        /// </summary>
        [Min(0.1f)]
        public float minDistanceBetweenProbes = 1.0f;

        /// <summary>
        /// Maximum subdivision in the structure.
        /// </summary>
        public int maxSubdivision => GetMaxSubdivision(bakedSimplificationLevels);

        /// <summary>
        /// Minimum size of a brick in meters.
        /// </summary>
        public float minBrickSize => GetMinBrickSize(bakedMinDistanceBetweenProbes);

        /// <summary>
        /// Size of the cell in meters.
        /// </summary>
        public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;

        /// <summary>
        /// Layer mask filter for all renderers.
        /// </summary>
        public LayerMask renderersLayerMask = -1;

        /// <summary>
        /// Specifies the minimum bounding box volume of renderers to consider placing probes around.
        /// </summary>
        [Min(0)]
        public float minRendererVolumeSize = 0.1f;

        internal static int GetCellSizeInBricks(int simplificationLevels) => (int)Mathf.Pow(3, simplificationLevels);
        internal static int GetMaxSubdivision(int simplificationLevels) => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
        internal static float GetMinBrickSize(float minDistanceBetweenProbes) => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);


        private void OnValidate()
        {
            singleSceneMode &= m_SceneGUIDs.Count <= 1;

            ProbeReferenceVolume.instance.sceneData?.SyncBakingSets();

            if (lightingScenarios.Count == 0)
                lightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };

            if (version != CoreUtils.GetLastEnumValue<Version>())
            {
                // Migration code
            }

            settings.Upgrade();
        }

        public void OnAfterDeserialize()
        {
            if (!lightingScenarios.Contains(lightingScenario))
            {
                if (lightingScenarios.Count != 0)
                    lightingScenario = lightingScenarios[0];
                else
                    lightingScenario = ProbeReferenceVolume.defaultLightingScenario;
            }

            perSceneCellLists.Clear();
            foreach(var scene in m_SerializedPerSceneCellList)
            {
                perSceneCellLists.Add(scene.sceneGUID, scene.cellList);
            }

            if (m_OtherScenario == "")
                m_OtherScenario = null;

            if (bakedSimplificationLevels == -1)
            {
                bakedSimplificationLevels = simplificationLevels;
                bakedMinDistanceBetweenProbes = minDistanceBetweenProbes;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPerSceneCellList = new List<SerializedPerSceneCellList>();
            foreach (var kvp in perSceneCellLists)
            {
                m_SerializedPerSceneCellList.Add(new SerializedPerSceneCellList { sceneGUID = kvp.Key, cellList = kvp.Value });
            }
        }

        internal void Initialize()
        {
            // Reset blending.
            if (ProbeReferenceVolume.instance.enableScenarioBlending)
                BlendLightingScenario(null, 0.0f);
        }

        internal void Cleanup()
        {
            if (cellSharedDataAsset != null)
            {
                cellSharedDataAsset.Dispose();
                foreach (var scenario in scenarios)
                {
                    if (scenario.Value.IsValid())
                    {
                        scenario.Value.cellDataAsset.Dispose();
                        scenario.Value.cellOptionalDataAsset.Dispose();
                    }
                }
            }

            if (m_ReadCommandBuffer.IsCreated)
                m_ReadCommandBuffer.Dispose();

            foreach(var buffer in m_ReadOperationScratchBuffers)
                buffer.Dispose();

            m_ReadOperationScratchBuffers.Clear();
        }

        internal void Migrate(ProbeVolumeSceneData.BakingSet set)
        {
            singleSceneMode = false;
            settings = set.settings;
            m_SceneGUIDs = set.sceneGUIDs;
            lightingScenarios = set.lightingScenarios;
            bakedMinDistanceBetweenProbes = set.profile.minDistanceBetweenProbes;
            bakedSimplificationLevels = set.profile.simplificationLevels;
        }

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeVolumeBakingSet otherProfile)
        {
            return minDistanceBetweenProbes == otherProfile.minDistanceBetweenProbes &&
                cellSizeInMeters == otherProfile.cellSizeInMeters &&
                simplificationLevels == otherProfile.simplificationLevels &&
                renderersLayerMask == otherProfile.renderersLayerMask;
        }

        internal void RemoveScene(string guid)
        {
            m_SceneGUIDs.Remove(guid);
            scenesToNotBake.Remove(guid);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet.Remove(guid);
        }

        internal void AddScene(string guid)
        {
            m_SceneGUIDs.Add(guid);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet[guid] = this;
        }

        internal void SetScene(string guid, int index)
        {
            scenesToNotBake.Remove(m_SceneGUIDs[index]);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet.Remove(m_SceneGUIDs[index]);
            m_SceneGUIDs[index] = guid;
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet[guid] = this;
        }

        internal string CreateScenario(string name)
        {
            if (lightingScenarios.Contains(name))
            {
                string renamed;
                int index = 1;
                do
                    renamed = $"{name} ({index++})";
                while (lightingScenarios.Contains(renamed));
                name = renamed;
            }
            lightingScenarios.Add(name);
            return name;
        }

        internal bool RemoveScenario(string name)
        {
#if UNITY_EDITOR
            if (scenarios.TryGetValue(name, out var scenarioData))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(scenarioData.cellDataAsset.assetGUID));
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(scenarioData.cellOptionalDataAsset.assetGUID));
                EditorUtility.SetDirty(this);
            }
#endif
            foreach (var cellData in cellDataMap.Values)
            {
                if (cellData.scenarios.TryGetValue(name, out var cellScenarioData))
                {
                    cellData.CleanupPerScenarioData(cellScenarioData);
                    cellData.scenarios.Remove(name);
                }
            }

            scenarios.Remove(name);
            return lightingScenarios.Remove(name);
        }

        internal ProbeVolumeBakingSet Clone()
        {
            var newSet = Instantiate(this);
            newSet.m_SceneGUIDs.Clear();
            newSet.scenesToNotBake.Clear();
            return newSet;
        }


        internal void SetActiveScenario(string scenario, bool verbose = true)
        {
            // This ensure that we invalidate the other scenario.
            // This is necessary for some cases in the context of baking.
            m_OtherScenario = null;

            if (lightingScenario == scenario && m_ScenarioBlendingFactor == 0.0f)
                return;

            if (!lightingScenarios.Contains(scenario))
            {
                if (verbose)
                    Debug.LogError($"Scenario '{scenario}' does not exist.");
                return;
            }

            if (!scenarios.ContainsKey(scenario))
            {
                // We don't return here as it's still valid to enable a scenario that wasn't baked in the editor.
                if (verbose)
                    Debug.LogError($"Scenario '{scenario}' has not been baked.");
            }

            lightingScenario = scenario;
            m_ScenarioBlendingFactor = 0.0f;

            if (ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                // Trigger blending system to replace old cells with the one from the new active scenario.
                // Although we technically don't need blending for that, it is better than unloading all cells
                // because it will replace them progressively. There is no real performance cost to using blending
                // rather than regular load thanks to the bypassBlending branch in AddBlendingBricks.
                ProbeReferenceVolume.instance.ScenarioBlendingChanged(true);
            }
            else
                ProbeReferenceVolume.instance.UnloadAllCells();
        }

        internal void BlendLightingScenario(string otherScenario, float blendingFactor)
        {
            if (!ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                if (!ProbeBrickBlendingPool.isSupported)
                    Debug.LogError("Blending between lighting scenarios is not supported by this render pipeline.");
                else
                    Debug.LogError("Blending between lighting scenarios is disabled in the render pipeline settings.");
                return;
            }

            // null scenario is valid in order to reset blending.
            if (otherScenario != null && !lightingScenarios.Contains(otherScenario))
            {
                Debug.LogError($"Scenario '{otherScenario}' does not exist.");
                return;
            }

            if (otherScenario != null && !scenarios.ContainsKey(otherScenario))
            {
                Debug.LogError($"Scenario '{otherScenario}' has not been baked.");
                return;
            }

            blendingFactor = Mathf.Clamp01(blendingFactor);

            if (otherScenario == lightingScenario || string.IsNullOrEmpty(otherScenario))
                otherScenario = null;
            if (otherScenario == null)
                blendingFactor = 0.0f;
            if (otherScenario == m_OtherScenario && Mathf.Approximately(blendingFactor, m_ScenarioBlendingFactor))
                return;

            bool scenarioChanged = otherScenario != m_OtherScenario;
            m_OtherScenario = otherScenario;
            m_ScenarioBlendingFactor = blendingFactor;

            ProbeReferenceVolume.instance.ScenarioBlendingChanged(scenarioChanged);
        }

        internal int GetBakingHashCode()
        {
            int hash = maxCellPosition.GetHashCode();
            hash = hash * 23 + minCellPosition.GetHashCode();
            hash = hash * 23 + globalBounds.GetHashCode();
            hash = hash * 23 + cellSizeInBricks.GetHashCode();
            hash = hash * 23 + simplificationLevels.GetHashCode();
            hash = hash * 23 + minDistanceBetweenProbes.GetHashCode();

            return hash;
        }

        internal void Clear()
        {
#if UNITY_EDITOR
            try
            {
                AssetDatabase.StartAssetEditing();
                if (cellBricksDataAsset != null)
                {
                    DeleteAsset(cellBricksDataAsset.assetGUID);
                    DeleteAsset(cellSharedDataAsset.assetGUID);
                    DeleteAsset(cellSupportDataAsset.assetGUID);
                    cellBricksDataAsset = null;
                    cellSharedDataAsset = null;
                    cellSupportDataAsset = null;
                }
                foreach (var scenarioData in scenarios.Values)
                {
                    if (scenarioData.IsValid())
                    {
                        DeleteAsset(scenarioData.cellDataAsset.assetGUID);
                        DeleteAsset(scenarioData.cellOptionalDataAsset.assetGUID);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);
            }
#endif
            cellDescs.Clear();
            cellCounts.Clear();
            scenarios.Clear();

            // All cells should have been released through unloading the scenes first.
            Debug.Assert(cellDataMap.Count == 0);

            perSceneCellLists.Clear();
            foreach (var sceneGUID in sceneGUIDs)
                perSceneCellLists.Add(sceneGUID, new List<int>());
        }

        internal void RenameScenario(string scenario, string newName)
        {
            if (!lightingScenarios.Contains(scenario))
                return;

            lightingScenarios.Remove(scenario);
            lightingScenarios.Add(newName);

            // If the scenario was not baked at least once, this does not exist.
            if (scenarios.TryGetValue(scenario, out var data))
            {
                scenarios.Remove(scenario);
                scenarios.Add(newName, data);

                foreach(var cellData in cellDataMap.Values)
                {
                    if (cellData.scenarios.TryGetValue(scenario, out var cellScenarioData))
                    {
                        cellData.scenarios.Add(newName, cellScenarioData);
                        cellData.scenarios.Remove(scenario);
                    }
                }

#if UNITY_EDITOR
                var baseName = name + "-" + newName;

                GetCellDataFileNames(name, newName, out string cellDataFileName, out string cellOptionalDataFileName);
                RenameAsset(data.cellDataAsset.assetGUID, cellDataFileName);
                RenameAsset(data.cellOptionalDataAsset.assetGUID, cellOptionalDataFileName);
#endif
            }
        }


        static int AlignUp16(int count)
        {
            var alignment = 16;
            var remainder = count % alignment;
            return count + (remainder == 0 ? 0 : alignment - remainder);
        }

        NativeArray<T> GetSubArray<T>(NativeArray<byte> input, int count, ref int offset) where T : struct
        {
            var size = count * UnsafeUtility.SizeOf<T>();
            if (offset + size > input.Length)
                return default;

            var result = input.GetSubArray(offset, size).Reinterpret<T>(1);
            offset = AlignUp16(offset + size);
            return result;
        }

        NativeArray<byte> RequestScratchBuffer(int size)
        {
            if (m_ReadOperationScratchBuffers.Count == 0)
            {
                return new NativeArray<byte>(size, Allocator.Persistent);
            }
            else
            {
                var buffer = m_ReadOperationScratchBuffers.Pop();
                Debug.Assert(buffer.IsCreated);
                if (buffer.Length < size)
                {
                    buffer.Dispose();
                    return new NativeArray<byte>(size, Allocator.Persistent);
                }
                else
                {
                    return buffer;
                }
            }
        }

        // Load from disk all data related to the required cells only.
        // This allows us to avoid loading the whole file in memory which could be a huge spike for multi scene setups.
        unsafe NativeArray<T> LoadStreambleAssetData<T>(StreamableAsset asset, List<int> cellIndices) where T : struct
        {
            // Prepare read commands.

            // Reallocate read commands buffer if needed.
            if (!m_ReadCommandBuffer.IsCreated || m_ReadCommandBuffer.Length < cellIndices.Count)
            {
                if (m_ReadCommandBuffer.IsCreated)
                    m_ReadCommandBuffer.Dispose();
                m_ReadCommandBuffer = new NativeArray<ReadCommand>(cellIndices.Count, Allocator.Persistent);
            }

            // Compute total size and fill read command offsets/sizes
            int totalSize = 0;
            int commandIndex = 0;
            foreach (int cellIndex in cellIndices)
            {
                var cell = cellDescs[cellIndex];
                var streamableCellDesc = asset.streamableCellDescs[cellIndex];
                ReadCommand command = new ReadCommand();
                command.Offset = streamableCellDesc.offset;
                command.Size = streamableCellDesc.elementCount * asset.elementSize;
                command.Buffer = null;
                m_ReadCommandBuffer[commandIndex++] = command;

                totalSize += (int)command.Size;
            }

            var scratchBuffer = RequestScratchBuffer(totalSize);

            // Update output buffer pointers
            commandIndex = 0;
            long outputOffset = 0;
            byte* scratchPtr = (byte*)scratchBuffer.GetUnsafePtr();
            foreach (int cellIndex in cellIndices)
            {
                // Stupid C# and no ref returns by default...
                var command = m_ReadCommandBuffer[commandIndex];
                command.Buffer = scratchPtr + outputOffset;
                outputOffset += command.Size;
                m_ReadCommandBuffer[commandIndex++] = command;
            }

            m_ReadCommandArray.CommandCount = cellIndices.Count;
            m_ReadCommandArray.ReadCommands = (ReadCommand*)m_ReadCommandBuffer.GetUnsafePtr();

            // We don't need async read here but only partial read to avoid loading up the whole file for ever.
            // So we just wait for the result.
            var readHandle = AsyncReadManager.Read(asset.OpenFile(), m_ReadCommandArray);
            readHandle.JobHandle.Complete();
            Debug.Assert(readHandle.Status == ReadStatus.Complete);
            asset.CloseFile();
            readHandle.Dispose();

            return scratchBuffer.Reinterpret<T>(1);
        }

        void ReleaseStreamableAssetData<T>(NativeArray<T> buffer) where T : struct
        {
            m_ReadOperationScratchBuffers.Push(buffer.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()));
        }

        void PruneCellIndexList(List<int> cellIndices, List<int> prunedIndexList)
        {
            prunedIndexList.Clear();
            foreach (var cellIndex in cellIndices)
            {
                // When clearing data only partially (ie: not all scenes are loaded), there can be left over indices here but no cells in the set.
                if (!cellDataMap.ContainsKey(cellIndex))
                {
                    prunedIndexList.Add(cellIndex);
                }
            }
        }

        void PruneCellIndexListForScenario(List<int> cellIndices, PerScenarioDataInfo scenarioData, List<int> prunedIndexList)
        {
            prunedIndexList.Clear();
            foreach (var cellIndex in cellIndices)
            {
                // When partially (in terms of scenes) baking for different scenarios with different list of scenes on after the other,
                // not all scenarios contains all cells, so we need to remove the cells that are not available.
                if (scenarioData.cellDataAsset.streamableCellDescs.ContainsKey(cellIndex))
                {
                    prunedIndexList.Add(cellIndex);
                }
            }
        }

        internal List<int> GetSceneCellIndexList(string sceneGUID)
        {
            if (perSceneCellLists.TryGetValue(sceneGUID, out var indexList))
                return indexList;
            else
                return null;
        }

        bool CheckSharedDataIntegrity()
        {
            return cellSharedDataAsset.FileExists() && cellBricksDataAsset.FileExists();
        }

        internal bool ResolveCellData(string sceneGUID)
        {
            var cellIndices = GetSceneCellIndexList(sceneGUID);

            if (cellIndices == null)
                return false;

            // Prune index list (some cells might already be resolved from another scene).
            PruneCellIndexList(cellIndices, m_PrunedIndexList);
            if (ResolveSharedCellData(m_PrunedIndexList))
            {
                return ResolvePerScenarioCellData(m_PrunedIndexList);
            }

            return false;
        }

        internal bool ResolveSharedCellData(List<int> cellIndices)
        {
            // When streaming is enabled, we should never resolve Cell data in CPU memory.
            // The streaming system will upload them directly to the GPU.
            if (ProbeReferenceVolume.instance.diskStreamingEnabled)
                return true;

            // Set not baked
            if (cellSharedDataAsset == null || !cellSharedDataAsset.IsValid())
                return false;

            if (!CheckSharedDataIntegrity())
            {
                Debug.LogError($"One or more data file missing for baking set {name}. Cannot load shared data.");
                return false;
            }

            // Load needed cells from disk.
            var cellSharedData = LoadStreambleAssetData<byte>(cellSharedDataAsset, cellIndices);
            var bricksData = LoadStreambleAssetData<ProbeBrickIndex.Brick>(cellBricksDataAsset, cellIndices);

            var hasSupportData = cellSupportDataAsset != null && !string.IsNullOrEmpty(cellSupportDataAsset.assetGUID) && cellSupportDataAsset.FileExists();
            var cellSupportData = hasSupportData ? LoadStreambleAssetData<byte>(cellSupportDataAsset, cellIndices) : default;

            // Resolve per cell
            var sharedDataChunkOffset = 0;
            var supportDataChunkOffset = 0;
            var startCounts = new CellCounts();
            for (var i = 0; i < cellIndices.Count; ++i)
            {
                int cellIndex = cellIndices[i];
                var cellData = new CellData();
                var counts = cellCounts[cellIndex];

                Debug.Assert(!cellDataMap.ContainsKey(cellIndex)); // Don't resolve the same cell twice.

                cellData.bricks = new NativeArray<ProbeBrickIndex.Brick>(bricksData.GetSubArray(startCounts.bricksCount, counts.bricksCount), Allocator.Persistent);
                cellData.validityNeighMaskData = new NativeArray<byte>(cellSharedData.GetSubArray(sharedDataChunkOffset, validityMaskChunkSize * counts.chunksCount), Allocator.Persistent);

                if (hasSupportData)
                {
                    cellData.probePositions = new NativeArray<Vector3>(cellSupportData.GetSubArray(supportDataChunkOffset, counts.chunksCount * supportPositionChunkSize).Reinterpret<Vector3>(1), Allocator.Persistent);
                    supportDataChunkOffset += counts.chunksCount * supportPositionChunkSize;
                    cellData.validity = new NativeArray<float>(cellSupportData.GetSubArray(supportDataChunkOffset, counts.chunksCount * supportValidityChunkSize).Reinterpret<float>(1), Allocator.Persistent);
                    supportDataChunkOffset += counts.chunksCount * supportValidityChunkSize;
                    cellData.touchupVolumeInteraction = new NativeArray<float>(cellSupportData.GetSubArray(supportDataChunkOffset, counts.chunksCount * supportTouchupChunkSize).Reinterpret<float>(1), Allocator.Persistent);
                    supportDataChunkOffset += counts.chunksCount * supportTouchupChunkSize;
                    if (supportOffsetsChunkSize != 0)
                        cellData.offsetVectors = new NativeArray<Vector3>(cellSupportData.GetSubArray(supportDataChunkOffset, counts.chunksCount * supportOffsetsChunkSize).Reinterpret<Vector3>(1), Allocator.Persistent);
                    else
                        cellData.offsetVectors = default;
                    supportDataChunkOffset += counts.chunksCount * supportOffsetsChunkSize;
                }

                sharedDataChunkOffset += validityMaskChunkSize * counts.chunksCount;
                cellDataMap.Add(cellIndex, cellData);
                startCounts.Add(counts);
            }

            ReleaseStreamableAssetData(cellSharedData);
            ReleaseStreamableAssetData(bricksData);
            if (hasSupportData)
                ReleaseStreamableAssetData(cellSupportData);

            return true;
        }

        bool CheckPerScenarioDataIntegrity(in PerScenarioDataInfo data, ProbeVolumeSHBands shBands)
        {
            return data.cellDataAsset.FileExists() && (shBands == ProbeVolumeSHBands.SphericalHarmonicsL1 || data.cellOptionalDataAsset.FileExists());
        }

        internal bool ResolvePerScenarioCellData(List<int> cellIndices)
        {
            if (ProbeReferenceVolume.instance.diskStreamingEnabled)
                return true;

            bool shUseL2 = ProbeReferenceVolume.instance.shBands == ProbeVolumeSHBands.SphericalHarmonicsL2;

            foreach (var scenario in scenarios)
            {
                var name = scenario.Key;
                var data = scenario.Value;

                PruneCellIndexListForScenario(cellIndices, data, m_PrunedScenarioIndexList);

                if (!CheckPerScenarioDataIntegrity(data, ProbeReferenceVolume.instance.shBands))
                {
                    Debug.LogError($"One or more data file missing for baking set {name} scenario {lightingScenario}. Cannot load scenario data.");
                    return false;
                }

                var cellData = LoadStreambleAssetData<byte>(data.cellDataAsset, m_PrunedScenarioIndexList);
                var cellOptionalData = shUseL2 ? LoadStreambleAssetData<byte>(data.cellOptionalDataAsset, m_PrunedScenarioIndexList) : default;
                if (!ResolvePerScenarioCellData(cellData, cellOptionalData, name, m_PrunedScenarioIndexList))
                {
                    Debug.LogError($"Baked data for scenario '{name}' cannot be loaded.");
                    return false;
                }

                ReleaseStreamableAssetData(cellData);
                if (shUseL2)
                    ReleaseStreamableAssetData(cellOptionalData);
            }

            return true;
        }

        internal bool ResolvePerScenarioCellData(NativeArray<byte> cellData, NativeArray<byte> cellOptionalData, string scenario, List<int> cellIndices)
        {
            Debug.Assert(!ProbeReferenceVolume.instance.diskStreamingEnabled);

            if (!cellData.IsCreated)
                return false;

            // Optional L2 data
            var hasOptionalData = cellOptionalData.IsCreated;

            var chunkOffsetL0L1 = 0;
            var chunkOffsetL2 = 0;

            for (var i = 0; i < cellIndices.Count; ++i)
            {
                var cellIndex = cellIndices[i];
                var counts = cellCounts[cellIndex];
                var cell = cellDataMap[cellIndex];
                var cellState = new CellData.PerScenarioData();

                cellState.shL0L1RxData = new NativeArray<ushort>(cellData.GetSubArray(chunkOffsetL0L1, L0ChunkSize * counts.chunksCount).Reinterpret<ushort>(1), Allocator.Persistent);
                cellState.shL1GL1RyData = new NativeArray<byte>(cellData.GetSubArray(chunkOffsetL0L1 + L0ChunkSize * counts.chunksCount, L1ChunkSize * counts.chunksCount), Allocator.Persistent);
                cellState.shL1BL1RzData = new NativeArray<byte>(cellData.GetSubArray(chunkOffsetL0L1 + (L0ChunkSize + L1ChunkSize) * counts.chunksCount, L1ChunkSize * counts.chunksCount), Allocator.Persistent);

                if (hasOptionalData)
                {
                    var L2DataSize = counts.chunksCount * L2TextureChunkSize;
                    cellState.shL2Data_0 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 0, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_1 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 1, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_2 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 2, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_3 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 3, L2DataSize), Allocator.Persistent);
                }

                chunkOffsetL0L1 += (L0ChunkSize + 2 * L1ChunkSize) * counts.chunksCount;
                chunkOffsetL2 += (L2TextureChunkSize * 4) * counts.chunksCount;

                cell.scenarios.Add(scenario, cellState);
            }

            return true;
        }

        internal void ReleaseCell(int cellIndex)
        {
            var cellData = cellDataMap[cellIndex];
            cellData.Cleanup();
            cellDataMap.Remove(cellIndex);
        }

        internal CellDesc GetCellDesc(int cellIndex)
        {
            if (cellDescs.TryGetValue(cellIndex, out var cellDesc))
                return cellDesc;
            else
                return null;
        }

        internal CellData GetCellData(int cellIndex)
        {
            if (cellDataMap.TryGetValue(cellIndex, out var cellData))
                return cellData;
            else
                return null;
        }

#if UNITY_EDITOR

        string GetOrCreateFileName(StreamableAsset asset, string filePath)
        {
            string res = "";
            if (asset != null && asset.IsValid())
                res = AssetDatabase.GUIDToAssetPath(asset.assetGUID);
            if (string.IsNullOrEmpty(res))
                res = filePath;
            return res;
        }

        internal void EnsureScenarioAssetNameConsistencyForUndo()
        {
            foreach(var scenario in scenarios)
            {
                var scenarioName = scenario.Key;
                var scenarioData = scenario.Value;

                GetCellDataFileNames(name, scenarioName, out string cellDataFileName, out string cellOptionalDataFileName);

                if (!scenarioData.cellDataAsset.GetAssetPath().Contains(cellDataFileName))
                {
                    RenameAsset(scenarioData.cellDataAsset.assetGUID, cellDataFileName);
                    RenameAsset(scenarioData.cellOptionalDataAsset.assetGUID, cellOptionalDataFileName);
                }
            }
        }

        internal void GetCellDataFileNames(string basePath, string scenario, out string cellDataFileName, out string cellOptionalDataFileName)
        {
            cellDataFileName = basePath + "-" + scenario + ".CellData.bytes";
            cellOptionalDataFileName = basePath + "-" + scenario + ".CellOptionalData.bytes";
        }

        internal void GetBlobFileNames(string scenario, out string cellDataFilename, out string cellBricksDataFilename, out string cellOptionalDataFilename, out string cellSharedDataFilename, out string cellSupportDataFilename)
        {
            string baseDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

            string basePath = Path.Combine(baseDir, name);

            GetCellDataFileNames(basePath, scenario, out string dataFile, out string optionalDataFile);

            cellDataFilename = GetOrCreateFileName(scenarios[scenario].cellDataAsset, dataFile);
            cellOptionalDataFilename = GetOrCreateFileName(scenarios[scenario].cellOptionalDataAsset, optionalDataFile);
            cellBricksDataFilename = GetOrCreateFileName(cellBricksDataAsset, basePath + ".CellBricksData.bytes");
            cellSharedDataFilename = GetOrCreateFileName(cellSharedDataAsset, basePath + ".CellSharedData.bytes");
            cellSupportDataFilename = GetOrCreateFileName(cellSupportDataAsset, basePath + ".CellSupportData.bytes");
        }

        // Returns the file size in bytes
        long GetFileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

        internal long GetDiskSizeOfSharedData()
        {
            if (cellSharedDataAsset == null || !cellSharedDataAsset.IsValid())
                return 0;

            return GetFileSize(AssetDatabase.GUIDToAssetPath(cellBricksDataAsset.assetGUID)) + GetFileSize(AssetDatabase.GUIDToAssetPath(cellSharedDataAsset.assetGUID)) + GetFileSize(AssetDatabase.GUIDToAssetPath(cellSupportDataAsset.assetGUID));
        }

        internal long GetDiskSizeOfScenarioData(string scenario)
        {
            if (scenario == null || !scenarios.TryGetValue(scenario, out var data) || !data.IsValid())
                return 0;

            return GetFileSize(AssetDatabase.GUIDToAssetPath(data.cellDataAsset.assetGUID)) + GetFileSize(AssetDatabase.GUIDToAssetPath(data.cellOptionalDataAsset.assetGUID));
        }

        internal void SanitizeScenes()
        {
            // Remove entries in the list pointing to deleted scenes
            for (int i = m_SceneGUIDs.Count - 1; i >= 0; i--)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(m_SceneGUIDs[i]);
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) == null)
                {
                    ProbeReferenceVolume.instance.sceneData.OnSceneRemovedFromSet(m_SceneGUIDs[i]);
                    UnityEditor.EditorUtility.SetDirty(this);
                    m_SceneGUIDs.RemoveAt(i);
                }
            }
            for (int i = scenesToNotBake.Count - 1; i >= 0; i--)
            {
                if (ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(scenesToNotBake[i]) != this)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    scenesToNotBake.RemoveAt(i);
                }
            }
        }

        void RenameAsset(string assetGUID, string newName)
        {
            var oldPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            AssetDatabase.RenameAsset(oldPath, newName);
        }

        void DeleteAsset(string assetGUID)
        {
            if (assetGUID == "")
                return;

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.DeleteAsset(assetPath);
        }

        internal bool HasBeenBaked()
        {
            return !string.IsNullOrEmpty(cellSharedDataAsset.assetGUID);
        }

        public static string GetDirectory(string scenePath, string sceneName)
        {
            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (!AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.CreateFolder(sceneDir, sceneName);

            return assetPath;
        }
#endif
    }
}
