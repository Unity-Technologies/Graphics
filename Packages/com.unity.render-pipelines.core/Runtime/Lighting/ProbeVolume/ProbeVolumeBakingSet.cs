using System;
using System.IO;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

using CellData = UnityEngine.Rendering.ProbeReferenceVolume.CellData;
using CellDesc = UnityEngine.Rendering.ProbeReferenceVolume.CellDesc;

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

        [Serializable]
        internal class PerScenarioDataInfo
        {
            public void Initialize(ProbeVolumeSHBands shBands)
            {
                m_HasValidData = ComputeHasValidData(shBands);
            }

            public bool IsValid()
            {
                return cellDataAsset != null && cellDataAsset.IsValid(); // if cellDataAsset is valid optional data (if available) should always be valid.
            }

            public bool HasValidData(ProbeVolumeSHBands shBands)
            {
#if UNITY_EDITOR
                return ComputeHasValidData(shBands);
#else
                return m_HasValidData;
#endif
            }

            public bool ComputeHasValidData(ProbeVolumeSHBands shBands)
            {
                return cellDataAsset.FileExists() && (shBands == ProbeVolumeSHBands.SphericalHarmonicsL1 || cellOptionalDataAsset.FileExists());
            }

            public int sceneHash;
            public ProbeVolumeStreamableAsset cellDataAsset; // Contains L0 L1 SH data
            public ProbeVolumeStreamableAsset cellOptionalDataAsset; // Contains L2 SH data

            bool m_HasValidData;
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
        [SerializeField] internal bool dialogNoProbeVolumeInSetShown = false;
        [SerializeField] internal ProbeVolumeBakingProcessSettings settings;

        [SerializeField] private List<string> m_SceneGUIDs = new List<string>();
        [SerializeField] internal List<string> scenesToNotBake = new List<string>();
        [SerializeField, FormerlySerializedAs("lightingScenarios")] internal List<string> m_LightingScenarios = new List<string>();

        /// <summary>The list of scene GUIDs.</summary>
        public IReadOnlyList<string> sceneGUIDs => m_SceneGUIDs;
        /// <summary>The list of lighting scenarios.</summary>
        public IReadOnlyList<string> lightingScenarios => m_LightingScenarios;

        // List of cell descriptors.
        [SerializeField] internal SerializedDictionary<int, CellDesc> cellDescs = new SerializedDictionary<int, CellDesc>();

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
        [SerializeField] internal ProbeVolumeStreamableAsset cellSharedDataAsset = null; // Contains validity data
        [SerializeField] internal SerializedDictionary<string, PerScenarioDataInfo> scenarios = new SerializedDictionary<string, PerScenarioDataInfo>();
        // This data will be streamed from disk but is only needed in CPU memory.
        [SerializeField] internal ProbeVolumeStreamableAsset cellBricksDataAsset; // Contains bricks data
        [SerializeField] internal ProbeVolumeStreamableAsset cellSupportDataAsset = null; // Contains debug data

        [SerializeField] internal int chunkSizeInBricks;
        [SerializeField] internal Vector3Int maxCellPosition;
        [SerializeField] internal Vector3Int minCellPosition;
        [SerializeField] internal Bounds globalBounds;
        [SerializeField] internal int bakedSimplificationLevels = -1;
        [SerializeField] internal float bakedMinDistanceBetweenProbes = -1.0f;

        [SerializeField] internal int maxSHChunkCount = -1; // Maximum number of SH chunk for a cell in this set.
        [SerializeField] internal int L0ChunkSize;
        [SerializeField] internal int L1ChunkSize;
        [SerializeField] internal int L2TextureChunkSize; // Optional. Size of the chunk for one texture (4 textures for all data)
        [SerializeField] internal int validityMaskChunkSize; // Shared
        [SerializeField] internal int supportPositionChunkSize;
        [SerializeField] internal int supportValidityChunkSize;
        [SerializeField] internal int supportTouchupChunkSize;
        [SerializeField] internal int supportOffsetsChunkSize;
        [SerializeField] internal int supportDataChunkSize;

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

        bool m_HasSupportData = false;
        bool m_SharedDataIsValid = false;

        private void OnValidate()
        {
            singleSceneMode &= m_SceneGUIDs.Count <= 1;

            ProbeReferenceVolume.instance.sceneData?.SyncBakingSets();

            if (m_LightingScenarios.Count == 0)
                m_LightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };

            if (version != CoreUtils.GetLastEnumValue<Version>())
            {
                // Migration code
            }

            settings.Upgrade();
        }

        void OnEnable()
        {
            m_HasSupportData = ComputeHasSupportData();
            m_SharedDataIsValid = ComputeHasValidSharedData();
        }

        // For functions below:
        // In editor users can delete asset at any moment, so we need to compute the result from scratch all the time.
        // In builds however, we want to avoid the expensive I/O operations.
        bool ComputeHasValidSharedData()
        {
            return cellSharedDataAsset != null && cellSharedDataAsset.FileExists() && cellBricksDataAsset.FileExists();
        }

        internal bool HasValidSharedData()
        {
#if UNITY_EDITOR
            return ComputeHasValidSharedData();
#else
            return m_SharedDataIsValid;
#endif
        }

        bool ComputeHasSupportData()
        {
            return cellSupportDataAsset != null && cellSupportDataAsset.IsValid() && cellSupportDataAsset.FileExists();
        }

        internal bool HasSupportData()
        {
#if UNITY_EDITOR
            return ComputeHasSupportData();
#else
            return m_HasSupportData;
#endif
        }

        /// <summary>Called after deserializing</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!m_LightingScenarios.Contains(lightingScenario))
            {
                if (m_LightingScenarios.Count != 0)
                    lightingScenario = m_LightingScenarios[0];
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

            // Hack T_T
            // Added the new bricksCount in Disk Streaming PR to have everything ready in the serialized desc but old data does not have it so we need to recompute it...
            // Might as well not serialize it but it's bad to have non-serialized data in the serialized desc.
            if (cellDescs.Count != 0)
            {
                // Check first cell to see if we need to recompute for all cells.
                var enumerator = cellDescs.Values.GetEnumerator();
                enumerator.MoveNext();
                var cellDesc = enumerator.Current;
                if (cellDesc.bricksCount == 0)
                {
                    foreach(var value in cellDescs.Values)
                        value.bricksCount = value.probeCount / ProbeBrickPool.kBrickProbeCountTotal;
                }
            }
        }

        /// <summary>Called before serializing</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_SerializedPerSceneCellList = new List<SerializedPerSceneCellList>();
            foreach (var kvp in perSceneCellLists)
            {
                m_SerializedPerSceneCellList.Add(new SerializedPerSceneCellList { sceneGUID = kvp.Key, cellList = kvp.Value });
            }
        }

        internal void Initialize()
        {
            // Would have been better in OnEnable but unfortunately, ProbeReferenceVolume.instance.shBands might not be initialized yet when it's called.
            foreach (var scenario in scenarios)
                scenario.Value.Initialize(ProbeReferenceVolume.instance.shBands);

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
            m_LightingScenarios = set.lightingScenarios;
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

        /// <summary>
        /// Removes a scene from the baking set.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to remove.</param>
        public void RemoveScene(string guid)
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            m_SceneGUIDs.Remove(guid);
            scenesToNotBake.Remove(guid);
            sceneData.sceneToBakingSet.Remove(guid);
#if UNITY_EDITOR
            EditorUtility.SetDirty(sceneData.parentAsset);
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Tries to add a scene to the baking set.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to add.</param>
        /// <returns>Whether the scene was successfull added to the baking set.</returns>
        public bool TryAddScene(string guid)
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            var sceneSet = sceneData.GetBakingSetForScene(guid);
            if (sceneSet != null)
                return false;
            AddScene(guid);
            return true;
        }

        internal void AddScene(string guid)
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            m_SceneGUIDs.Add(guid);
            sceneData.sceneToBakingSet[guid] = this;
#if UNITY_EDITOR
            EditorUtility.SetDirty(sceneData.parentAsset);
            EditorUtility.SetDirty(this);
#endif
        }

        internal void SetScene(string guid, int index)
        {
            var sceneData = ProbeReferenceVolume.instance.sceneData;
            scenesToNotBake.Remove(m_SceneGUIDs[index]);
            sceneData.sceneToBakingSet.Remove(m_SceneGUIDs[index]);
            m_SceneGUIDs[index] = guid;
            sceneData.sceneToBakingSet[guid] = this;
#if UNITY_EDITOR
            EditorUtility.SetDirty(sceneData.parentAsset);
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Changes the baking status of a scene. Objects in scenes disabled for baking will still contribute to
        /// lighting for other scenes.
        /// </summary>
        /// <param name ="guid">The GUID of the scene to remove.</param>
        /// <param name ="enableForBaking">Wheter or not this scene should be included when baking lighting.</param>
        public void SetSceneBaking(string guid, bool enableForBaking)
        {
            if (enableForBaking)
                scenesToNotBake.Remove(guid);
            else if (m_SceneGUIDs.Contains(guid))
                scenesToNotBake.Add(guid);
        }

        /// <summary>
        /// Tries to add a lighting scenario to the baking set.
        /// </summary>
        /// <param name ="name">The name of the scenario to add.</param>
        /// <returns>Whether the scenario was successfully created.</returns>
        public bool TryAddScenario(string name)
        {
            if (m_LightingScenarios.Contains(name))
                return false;
            m_LightingScenarios.Add(name);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return true;
        }

        internal string CreateScenario(string name)
        {
            int index = 1;
            string renamed = name;
            while (!TryAddScenario(renamed))
                renamed = $"{name} ({index++})";

            return renamed;
        }

        internal bool RemoveScenario(string name)
        {
#if UNITY_EDITOR
            if (scenarios.TryGetValue(name, out var scenarioData))
            {
                AssetDatabase.DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                AssetDatabase.DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
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
            return m_LightingScenarios.Remove(name);
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
            if (lightingScenario == scenario)
                return;

            if (!m_LightingScenarios.Contains(scenario))
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
            if (!string.IsNullOrEmpty(otherScenario) && !ProbeReferenceVolume.instance.enableScenarioBlending)
            {
                if (!ProbeBrickBlendingPool.isSupported)
                    Debug.LogError("Blending between lighting scenarios is not supported by this render pipeline.");
                else
                    Debug.LogError("Blending between lighting scenarios is disabled in the render pipeline settings.");
                return;
            }

            // null scenario is valid in order to reset blending.
            if (otherScenario != null && !m_LightingScenarios.Contains(otherScenario))
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
                    DeleteAsset(cellBricksDataAsset.GetAssetPath());
                    DeleteAsset(cellSharedDataAsset.GetAssetPath());
                    DeleteAsset(cellSupportDataAsset.GetAssetPath());
                    cellBricksDataAsset = null;
                    cellSharedDataAsset = null;
                    cellSupportDataAsset = null;
                }
                foreach (var scenarioData in scenarios.Values)
                {
                    if (scenarioData.IsValid())
                    {
                        DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                        DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
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
            scenarios.Clear();

            // All cells should have been released through unloading the scenes first.
            Debug.Assert(cellDataMap.Count == 0);

            perSceneCellLists.Clear();
            foreach (var sceneGUID in sceneGUIDs)
                perSceneCellLists.Add(sceneGUID, new List<int>());
        }

        internal string RenameScenario(string scenario, string newName)
        {
            if (!m_LightingScenarios.Contains(scenario))
                return newName;

            m_LightingScenarios.Remove(scenario);
            newName = CreateScenario(newName);

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
                data.cellDataAsset.RenameAsset(cellDataFileName);
                data.cellOptionalDataAsset.RenameAsset(cellOptionalDataFileName);
#endif
            }

            return newName;
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
        unsafe NativeArray<T> LoadStreambleAssetData<T>(ProbeVolumeStreamableAsset asset, List<int> cellIndices) where T : struct
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

        internal bool ResolveCellData(string sceneGUID)
        {
            var cellIndices = GetSceneCellIndexList(sceneGUID);

            if (cellIndices == null)
                return false;

            // Prune index list (some cells might already be resolved from another scene).
            PruneCellIndexList(cellIndices, m_PrunedIndexList);

            // When disk streaming is enabled, we should never resolve Cell data in CPU memory.
            // The streaming system will upload them directly to the GPU.
            if (ProbeReferenceVolume.instance.diskStreamingEnabled)
            {
                // Prepare data structures.
                // GPU data will stay empty but CPU data (bricks, support) will be streamed here.
                foreach (var cell in m_PrunedIndexList)
                {
                    Debug.Assert(!cellDataMap.ContainsKey(cell));
                    // Not ideal.
                    // When streaming and blending, we still need to have a valid list of scenario per CellData.
                    var newCellData = new CellData();
                    foreach (var scenario in scenarios)
                        newCellData.scenarios.Add(scenario.Key, default);

                    cellDataMap.Add(cell, newCellData);
                }

                return true;
            }
            else
            {
                if (ResolveSharedCellData(m_PrunedIndexList))
                {
                    return ResolvePerScenarioCellData(m_PrunedIndexList);
                }
            }

            return false;
        }

        internal bool ResolveSharedCellData(List<int> cellIndices)
        {
            Debug.Assert(!ProbeReferenceVolume.instance.diskStreamingEnabled);

            // Set not baked
            if (cellSharedDataAsset == null || !cellSharedDataAsset.IsValid())
                return false;

            if (!HasValidSharedData())
            {
                Debug.LogError($"One or more data file missing for baking set {name}. Cannot load shared data.");
                return false;
            }

            // Load needed cells from disk.
            var cellSharedData = LoadStreambleAssetData<byte>(cellSharedDataAsset, cellIndices);
            var bricksData = LoadStreambleAssetData<ProbeBrickIndex.Brick>(cellBricksDataAsset, cellIndices);

            bool hasSupportData = HasSupportData();
            var cellSupportData = hasSupportData ? LoadStreambleAssetData<byte>(cellSupportDataAsset, cellIndices) : default;

            // Resolve per cell
            var sharedDataChunkOffset = 0;
            var supportDataChunkOffset = 0;
            int totalBricksCount = 0;
            int totalSHChunkCount = 0;
            for (var i = 0; i < cellIndices.Count; ++i)
            {
                int cellIndex = cellIndices[i];
                var cellData = new CellData();
                var cellDesc = cellDescs[cellIndex];
                int bricksCount = cellDesc.bricksCount;
                int shChunkCount = cellDesc.shChunkCount;

                Debug.Assert(!cellDataMap.ContainsKey(cellIndex)); // Don't resolve the same cell twice.

                cellData.bricks = new NativeArray<ProbeBrickIndex.Brick>(bricksData.GetSubArray(totalBricksCount, bricksCount), Allocator.Persistent);
                cellData.validityNeighMaskData = new NativeArray<byte>(cellSharedData.GetSubArray(sharedDataChunkOffset, validityMaskChunkSize * shChunkCount), Allocator.Persistent);

                if (hasSupportData)
                {
                    cellData.probePositions = new NativeArray<Vector3>(cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportPositionChunkSize).Reinterpret<Vector3>(1), Allocator.Persistent);
                    supportDataChunkOffset += shChunkCount * supportPositionChunkSize;
                    cellData.validity = new NativeArray<float>(cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportValidityChunkSize).Reinterpret<float>(1), Allocator.Persistent);
                    supportDataChunkOffset += shChunkCount * supportValidityChunkSize;
                    cellData.touchupVolumeInteraction = new NativeArray<float>(cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportTouchupChunkSize).Reinterpret<float>(1), Allocator.Persistent);
                    supportDataChunkOffset += shChunkCount * supportTouchupChunkSize;
                    if (supportOffsetsChunkSize != 0)
                        cellData.offsetVectors = new NativeArray<Vector3>(cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportOffsetsChunkSize).Reinterpret<Vector3>(1), Allocator.Persistent);
                    else
                        cellData.offsetVectors = default;
                    supportDataChunkOffset += shChunkCount * supportOffsetsChunkSize;
                }

                sharedDataChunkOffset += validityMaskChunkSize * shChunkCount;
                cellDataMap.Add(cellIndex, cellData);
                totalBricksCount += bricksCount;
                totalSHChunkCount += shChunkCount;
            }

            ReleaseStreamableAssetData(cellSharedData);
            ReleaseStreamableAssetData(bricksData);
            if (hasSupportData)
                ReleaseStreamableAssetData(cellSupportData);

            return true;
        }

        internal bool ResolvePerScenarioCellData(List<int> cellIndices)
        {
            Debug.Assert(!ProbeReferenceVolume.instance.diskStreamingEnabled);

            bool shUseL2 = ProbeReferenceVolume.instance.shBands == ProbeVolumeSHBands.SphericalHarmonicsL2;

            foreach (var scenario in scenarios)
            {
                var name = scenario.Key;
                var data = scenario.Value;

                PruneCellIndexListForScenario(cellIndices, data, m_PrunedScenarioIndexList);

                if (!data.HasValidData(ProbeReferenceVolume.instance.shBands))
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
                var cell = cellDataMap[cellIndex];
                var cellDesc = cellDescs[cellIndex];
                var cellState = new CellData.PerScenarioData();

                var shChunkCount = cellDesc.shChunkCount;

                cellState.shL0L1RxData = new NativeArray<ushort>(cellData.GetSubArray(chunkOffsetL0L1, L0ChunkSize * shChunkCount).Reinterpret<ushort>(1), Allocator.Persistent);
                cellState.shL1GL1RyData = new NativeArray<byte>(cellData.GetSubArray(chunkOffsetL0L1 + L0ChunkSize * shChunkCount, L1ChunkSize * shChunkCount), Allocator.Persistent);
                cellState.shL1BL1RzData = new NativeArray<byte>(cellData.GetSubArray(chunkOffsetL0L1 + (L0ChunkSize + L1ChunkSize) * shChunkCount, L1ChunkSize * shChunkCount), Allocator.Persistent);

                if (hasOptionalData)
                {
                    var L2DataSize = shChunkCount * L2TextureChunkSize;
                    cellState.shL2Data_0 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 0, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_1 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 1, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_2 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 2, L2DataSize), Allocator.Persistent);
                    cellState.shL2Data_3 = new NativeArray<byte>(cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 3, L2DataSize), Allocator.Persistent);
                }

                chunkOffsetL0L1 += (L0ChunkSize + 2 * L1ChunkSize) * shChunkCount;
                chunkOffsetL2 += (L2TextureChunkSize * 4) * shChunkCount;

                cell.scenarios.Add(scenario, cellState);
            }

            return true;
        }

        internal void ReleaseCell(int cellIndex)
        {
            var cellData = cellDataMap[cellIndex];
            cellData.Cleanup(true);
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

        internal int GetChunkGPUMemory(ProbeVolumeSHBands shBands)
        {
            // One L0 Chunk, Two L1 Chunks, 1 byte of validity per probe.
            int size = L0ChunkSize + 2 * L1ChunkSize + validityMaskChunkSize;
            // 4 Optional L2 Chunks
            if (shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                size += 4 * L2TextureChunkSize;
            return size;
        }

#if UNITY_EDITOR
        internal void SetDefaults()
        {
            settings.SetDefaults();
            m_LightingScenarios = new List<string> { ProbeReferenceVolume.defaultLightingScenario };

            // We have to initialize that to not trigger a warning on new baking sets
            chunkSizeInBricks = ProbeBrickPool.GetChunkSizeInBrickCount();

        }

        string GetOrCreateFileName(ProbeVolumeStreamableAsset asset, string filePath)
        {
            string res = "";
            if (asset != null && asset.IsValid())
                res = asset.GetAssetPath();
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
                    scenarioData.cellDataAsset.RenameAsset(cellDataFileName);
                    scenarioData.cellOptionalDataAsset.RenameAsset(cellOptionalDataFileName);
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

            return GetFileSize(cellBricksDataAsset.GetAssetPath()) + GetFileSize(cellSharedDataAsset.GetAssetPath()) + GetFileSize(cellSupportDataAsset.GetAssetPath());
        }

        internal long GetDiskSizeOfScenarioData(string scenario)
        {
            if (scenario == null || !scenarios.TryGetValue(scenario, out var data) || !data.IsValid())
                return 0;

            return GetFileSize(data.cellDataAsset.GetAssetPath()) + GetFileSize(data.cellOptionalDataAsset.GetAssetPath());
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

        void DeleteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

                AssetDatabase.DeleteAsset(assetPath);
        }

        internal bool HasBeenBaked()
        {
            return cellSharedDataAsset.IsValid();
        }

        public static string GetDirectory(string scenePath, string sceneName)
        {
            string sceneDir = Path.GetDirectoryName(scenePath);
            string assetPath = Path.Combine(sceneDir, sceneName);
            if (!AssetDatabase.IsValidFolder(assetPath))
                AssetDatabase.CreateFolder(sceneDir, sceneName);

            return assetPath;
        }

        public bool DialogNoProbeVolumeInSetShown()
        {
            return dialogNoProbeVolumeInSetShown;
        }

        public void SetDialogNoProbeVolumeInSetShown(bool value)
        {
            dialogNoProbeVolumeInSetShown = value;
        }
#endif
    }
}
