using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Serialization;

using CellData = UnityEngine.Rendering.ProbeReferenceVolume.CellData;
using CellDesc = UnityEngine.Rendering.ProbeReferenceVolume.CellDesc;

namespace UnityEngine.Rendering
{
    internal class LogarithmicAttribute : PropertyAttribute
    {
        public int min;
        public int max;

        public LogarithmicAttribute(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed partial class ProbeVolumeBakingSet : ScriptableObject, ISerializationCallbackReceiver
    {
        internal enum Version
        {
            Initial,
            RemoveProbeVolumeSceneData
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

        internal bool hasDilation => settings.dilationSettings.enableDilation && settings.dilationSettings.dilationDistance > 0.0f;

        // We keep a separate list with only the guids for the sake of convenience when iterating from outside this class.
        [SerializeField] private List<string> m_SceneGUIDs = new List<string>();
        [SerializeField, Obsolete("This is now contained in the SceneBakeData structure"), FormerlySerializedAs("scenesToNotBake")] internal List<string> obsoleteScenesToNotBake = new List<string>();
        [SerializeField, FormerlySerializedAs("lightingScenarios")] internal List<string> m_LightingScenarios = new List<string>();

        /// <summary>The list of scene GUIDs.</summary>
        public IReadOnlyList<string> sceneGUIDs => m_SceneGUIDs;
        /// <summary>The list of lighting scenarios.</summary>
        public IReadOnlyList<string> lightingScenarios => m_LightingScenarios;

        // List of cell descriptors.
        [SerializeField] internal SerializedDictionary<int, CellDesc> cellDescs = new SerializedDictionary<int, CellDesc>();

        internal Dictionary<int, CellData> cellDataMap = new Dictionary<int, CellData>();
        List<int> m_TotalIndexList = new List<int>();

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
        [SerializeField] internal int bakedSkyOcclusionValue = -1;
        [SerializeField] internal int bakedSkyShadingDirectionValue = -1;
        [SerializeField] internal Vector3 bakedProbeOffset = Vector3.zero;
        [SerializeField] internal int bakedMaskCount = 1;
        [SerializeField] internal uint4 bakedLayerMasks;
        [SerializeField] internal int maxSHChunkCount = -1; // Maximum number of SH chunk for a cell in this set.
        [SerializeField] internal int L0ChunkSize;
        [SerializeField] internal int L1ChunkSize;
        [SerializeField] internal int L2TextureChunkSize; // Optional. Size of the chunk for one texture (4 textures for all data)
        [SerializeField] internal int sharedValidityMaskChunkSize; // Shared
        [SerializeField] internal int sharedSkyOcclusionL0L1ChunkSize; // Shared
        [SerializeField] internal int sharedSkyShadingDirectionIndicesChunkSize;
        [SerializeField] internal int sharedDataChunkSize;
        [SerializeField] internal int supportPositionChunkSize;
        [SerializeField] internal int supportValidityChunkSize;
        [SerializeField] internal int supportTouchupChunkSize;
        [SerializeField] internal int supportLayerMaskChunkSize;
        [SerializeField] internal int supportOffsetsChunkSize;
        [SerializeField] internal int supportDataChunkSize;
        
        internal bool bakedSkyOcclusion
        {
            get => bakedSkyOcclusionValue <= 0 ? false : true;
            set => bakedSkyOcclusionValue = value ? 1 : 0;
        }
        internal bool bakedSkyShadingDirection
        {
            get => bakedSkyShadingDirectionValue <= 0 ? false : true;
            set => bakedSkyShadingDirectionValue = value ? 1 : 0;
        }

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

        internal const int k_MaxSkyOcclusionBakingSamples = 8192;

        // Baking Profile
        [SerializeField]
        Version version = CoreUtils.GetLastEnumValue<Version>();

        [SerializeField]
        internal bool freezePlacement = false;

        /// <summary>
        /// Offset on world origin used during baking. Can be used to have cells on positions that are not multiples of the probe spacing.
        /// </summary>
        [SerializeField]
        public Vector3 probeOffset = Vector3.zero;

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

        /// <summary>
        /// Specifies whether the baking set will have sky handled dynamically.
        /// </summary>
        public bool skyOcclusion = false;

        /// <summary>
        /// Controls the number of samples per probe for dynamic sky baking.
        /// </summary>
        [Logarithmic(1, k_MaxSkyOcclusionBakingSamples)]
        public int skyOcclusionBakingSamples = 2048;

        /// <summary>
        /// Controls the number of bounces per light path for dynamic sky baking.
        /// </summary>
        [Range(0, 5)]
        public int skyOcclusionBakingBounces = 2;

        /// <summary>
        /// Average albedo for dynamic sky bounces
        /// </summary>
        [Range(0, 1)]
        public float skyOcclusionAverageAlbedo = 0.6f;

        /// <summary>
        /// Sky Occlusion backface culling
        /// </summary>
        public bool skyOcclusionBackFaceCulling = false;

        /// <summary>
        /// Bake sky shading direction.
        /// </summary>
        public bool skyOcclusionShadingDirection = false;
        
        [Serializable]
        internal struct ProbeLayerMask
        {
            public RenderingLayerMask mask;
            public string name;
        }

        [SerializeField]
        internal bool useRenderingLayers = false;
        [SerializeField]
        internal ProbeLayerMask[] renderingLayerMasks;

        internal uint4 ComputeRegionMasks()
        {
            uint4 masks = 0;
            if (!useRenderingLayers || renderingLayerMasks == null)
                masks.x = 0xFFFFFFFF;
            else
            {
                for (int i = 0; i < renderingLayerMasks.Length; i++)
                    masks[i] = renderingLayerMasks[i].mask;

            }
            return masks;
        }

        internal static int GetCellSizeInBricks(int simplificationLevels) => (int)Mathf.Pow(3, simplificationLevels);
        internal static int GetMaxSubdivision(int simplificationLevels) => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
        internal static float GetMinBrickSize(float minDistanceBetweenProbes) => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        bool m_HasSupportData = false;
        bool m_SharedDataIsValid = false;
        bool m_UseStreamingAsset = true;

        private void OnValidate()
        {
            singleSceneMode &= m_SceneGUIDs.Count <= 1;

            if (m_LightingScenarios.Count == 0)
                m_LightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };

            settings.Upgrade();
        }

        void OnEnable()
        {
            Migrate();

            m_HasSupportData = ComputeHasSupportData();
            m_SharedDataIsValid = ComputeHasValidSharedData();
        }

        internal void Migrate()
        {
            if (version != CoreUtils.GetLastEnumValue<Version>())
            {
#pragma warning disable 618 // Type or member is obsolete
                if (version < Version.RemoveProbeVolumeSceneData)
                {
#if UNITY_EDITOR
                    var sceneData = ProbeReferenceVolume.instance.sceneData;
                    if (sceneData == null)
                        return;

                    foreach (var scene in m_SceneGUIDs)
                    {
                        SceneBakeData newSceneData = new SceneBakeData();
                        sceneData.obsoleteSceneBounds.TryGetValue(scene, out newSceneData.bounds);
                        sceneData.obsoleteHasProbeVolumes.TryGetValue(scene, out newSceneData.hasProbeVolume);
                        newSceneData.bakeScene = !obsoleteScenesToNotBake.Contains(scene);
                        m_SceneBakeData.Add(scene, newSceneData);
                    }

                    version = Version.RemoveProbeVolumeSceneData;
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }

#pragma warning restore 618
            }

            if (sharedValidityMaskChunkSize == 0)
                sharedValidityMaskChunkSize = sizeof(byte) * ProbeBrickPool.GetChunkSizeInProbeCount();

            if (settings.virtualOffsetSettings.validityThreshold == 0.0f)
                settings.virtualOffsetSettings.validityThreshold = 0.25f;
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

            if (bakedSkyOcclusionValue == -1)
                bakedSkyOcclusion = false;

            if (bakedSkyShadingDirectionValue == -1)
                bakedSkyShadingDirection = false;


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

        internal void Initialize(bool useStreamingAsset)
        {
            // Would have been better in OnEnable but unfortunately, ProbeReferenceVolume.instance.shBands might not be initialized yet when it's called.
            foreach (var scenario in scenarios)
                scenario.Value.Initialize(ProbeReferenceVolume.instance.shBands);

            if (!useStreamingAsset)
            {
                m_UseStreamingAsset = false;
                m_TotalIndexList.Clear();
                foreach (var index in cellDescs.Keys)
                    m_TotalIndexList.Add(index);

                ResolveAllCellData();
            }

            // Reset blending.
            if (ProbeReferenceVolume.instance.supportScenarioBlending)
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

            if (ProbeReferenceVolume.instance.supportScenarioBlending)
            {
                // Trigger blending system to replace old cells with the one from the new active scenario.
                // Although we technically don't need blending for that, it is better than unloading all cells
                // because it will replace them progressively. There is no real performance cost to using blending
                // rather than regular load thanks to the bypassBlending branch in AddBlendingBricks.
                ProbeReferenceVolume.instance.ScenarioBlendingChanged(true);
            }
            else
                ProbeReferenceVolume.instance.UnloadAllCells();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal void BlendLightingScenario(string otherScenario, float blendingFactor)
        {
            if (!string.IsNullOrEmpty(otherScenario) && !ProbeReferenceVolume.instance.supportScenarioBlending)
            {
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
            if (!m_UseStreamingAsset)
            {
                // Only when not using Streaming Asset is this reference valid.
                Debug.Assert(asset.asset != null);
                return asset.asset.GetData<byte>().Reinterpret<T>(1);
            }
            else
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
        }

        void ReleaseStreamableAssetData<T>(NativeArray<T> buffer) where T : struct
        {
            if (m_UseStreamingAsset)
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

        bool ResolveAllCellData()
        {
            Debug.Assert(!m_UseStreamingAsset);
            Debug.Assert(m_TotalIndexList.Count != 0);

            if (ResolveSharedCellData(m_TotalIndexList))
                return ResolvePerScenarioCellData(m_TotalIndexList);
            else
                return false;
        }

        internal bool ResolveCellData(List<int> cellIndices)
        {
            // All cells should already be resolved in CPU memory.
            if (!m_UseStreamingAsset)
                return true;

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

        void ResolveSharedCellData(List<int> cellIndices, NativeArray<ProbeBrickIndex.Brick> bricksData, NativeArray<byte> cellSharedData, NativeArray<byte> cellSupportData)
        {
            var prv = ProbeReferenceVolume.instance;
            bool hasSupportData = cellSupportData.Length != 0;

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

                // When we use Streaming Assets, we can't keep a reference to the source file data so we create a copy of the native array.
                // When not using Streaming Assets, the file will always be alive so we can keep the reference on the file data.
                var sourceBricks = bricksData.GetSubArray(totalBricksCount, bricksCount);
                var sourceValidityNeightMaskData = cellSharedData.GetSubArray(sharedDataChunkOffset, sharedValidityMaskChunkSize * shChunkCount);
                sharedDataChunkOffset += sharedValidityMaskChunkSize * shChunkCount;

                cellData.bricks = m_UseStreamingAsset ? new NativeArray<ProbeBrickIndex.Brick>(sourceBricks, Allocator.Persistent) : sourceBricks;
                cellData.validityNeighMaskData = m_UseStreamingAsset ? new NativeArray<byte>(sourceValidityNeightMaskData, Allocator.Persistent) : sourceValidityNeightMaskData;

                // TODO save sky occlusion in a separate asset (see AdaptiveProbeVolumes WriteBakingCells)
                // And load it depending on ProbeReferenceVolume.instance.skyOcclusion
                if (bakedSkyOcclusion)
                {
                    if (prv.skyOcclusion)
                    {
                        var sourceSkyOcclusionDataL0L1 = cellSharedData.GetSubArray(sharedDataChunkOffset, sharedSkyOcclusionL0L1ChunkSize * shChunkCount).Reinterpret<ushort>(1);
                        cellData.skyOcclusionDataL0L1 = m_UseStreamingAsset ? new NativeArray<ushort>(sourceSkyOcclusionDataL0L1, Allocator.Persistent) : sourceSkyOcclusionDataL0L1;
                    }
                    sharedDataChunkOffset += sharedSkyOcclusionL0L1ChunkSize * shChunkCount;
                    if (bakedSkyShadingDirection)
                    {
                        if (prv.skyOcclusion && prv.skyOcclusionShadingDirection)
                        {
                            var sourceSkyShadingDirectionIndices = cellSharedData.GetSubArray(sharedDataChunkOffset, sharedSkyShadingDirectionIndicesChunkSize * shChunkCount);
                            cellData.skyShadingDirectionIndices = m_UseStreamingAsset ? new NativeArray<byte>(sourceSkyShadingDirectionIndices, Allocator.Persistent) : sourceSkyShadingDirectionIndices;
                        }
                        sharedDataChunkOffset += sharedSkyShadingDirectionIndicesChunkSize * shChunkCount;
                    }
                }

                if (hasSupportData)
                {
                    var sourcePositions = cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportPositionChunkSize).Reinterpret<Vector3>(1);
                    supportDataChunkOffset += shChunkCount * supportPositionChunkSize;
                    cellData.probePositions = m_UseStreamingAsset ? new NativeArray<Vector3>(sourcePositions, Allocator.Persistent) : sourcePositions;

                    var sourceValidity = cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportValidityChunkSize).Reinterpret<float>(1);
                    supportDataChunkOffset += shChunkCount * supportValidityChunkSize;
                    cellData.validity = m_UseStreamingAsset ? new NativeArray<float>(sourceValidity, Allocator.Persistent) : sourceValidity;

                    var sourceTouchup = cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportTouchupChunkSize).Reinterpret<float>(1);
                    supportDataChunkOffset += shChunkCount * supportTouchupChunkSize;
                    cellData.touchupVolumeInteraction = m_UseStreamingAsset ? new NativeArray<float>(sourceTouchup, Allocator.Persistent) : sourceTouchup;

                    if (supportLayerMaskChunkSize != 0)
                    {
                        var sourceLayer = cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportLayerMaskChunkSize).Reinterpret<byte>(1);
                        supportDataChunkOffset += shChunkCount * supportLayerMaskChunkSize;
                        cellData.layer = m_UseStreamingAsset ? new NativeArray<byte>(sourceLayer, Allocator.Persistent) : sourceLayer;
                    }

                    if (supportOffsetsChunkSize != 0)
                    {
                        var sourceOffsetVectors = cellSupportData.GetSubArray(supportDataChunkOffset, shChunkCount * supportOffsetsChunkSize).Reinterpret<Vector3>(1);
                        supportDataChunkOffset += shChunkCount * supportOffsetsChunkSize;
                        cellData.offsetVectors = m_UseStreamingAsset ? new NativeArray<Vector3>(sourceOffsetVectors, Allocator.Persistent) : sourceOffsetVectors;
                    }
                }

                cellDataMap.Add(cellIndex, cellData);
                totalBricksCount += bricksCount;
                totalSHChunkCount += shChunkCount;
            }
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

            ResolveSharedCellData(cellIndices, bricksData, cellSharedData, cellSupportData);

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

                var sourceShL0L1RxDataSource = cellData.GetSubArray(chunkOffsetL0L1, L0ChunkSize * shChunkCount).Reinterpret<ushort>(1);
                var sourceShL1GL1RyDataSource = cellData.GetSubArray(chunkOffsetL0L1 + L0ChunkSize * shChunkCount, L1ChunkSize * shChunkCount);
                var sourceShL1BL1RzDataSource = cellData.GetSubArray(chunkOffsetL0L1 + (L0ChunkSize + L1ChunkSize) * shChunkCount, L1ChunkSize * shChunkCount);

                cellState.shL0L1RxData = m_UseStreamingAsset ? new NativeArray<ushort>(sourceShL0L1RxDataSource, Allocator.Persistent) : sourceShL0L1RxDataSource;
                cellState.shL1GL1RyData = m_UseStreamingAsset ?new NativeArray<byte>(sourceShL1GL1RyDataSource, Allocator.Persistent) : sourceShL1GL1RyDataSource;
                cellState.shL1BL1RzData = m_UseStreamingAsset ? new NativeArray<byte>(sourceShL1BL1RzDataSource, Allocator.Persistent) : sourceShL1BL1RzDataSource;

                if (hasOptionalData)
                {
                    var L2DataSize = shChunkCount * L2TextureChunkSize;

                    var sourceShL2Data_0 = cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 0, L2DataSize);
                    var sourceShL2Data_1 = cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 1, L2DataSize);
                    var sourceShL2Data_2 = cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 2, L2DataSize);
                    var sourceShL2Data_3 = cellOptionalData.GetSubArray(chunkOffsetL2 + L2DataSize * 3, L2DataSize);

                    cellState.shL2Data_0 = m_UseStreamingAsset ? new NativeArray<byte>(sourceShL2Data_0, Allocator.Persistent) : sourceShL2Data_0;
                    cellState.shL2Data_1 = m_UseStreamingAsset ? new NativeArray<byte>(sourceShL2Data_1, Allocator.Persistent) : sourceShL2Data_1;
                    cellState.shL2Data_2 = m_UseStreamingAsset ? new NativeArray<byte>(sourceShL2Data_2, Allocator.Persistent) : sourceShL2Data_2;
                    cellState.shL2Data_3 = m_UseStreamingAsset ? new NativeArray<byte>(sourceShL2Data_3, Allocator.Persistent) : sourceShL2Data_3;
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
            // One L0 Chunk, Two L1 Chunks, 1 shared chunk which may contain sky occlusion
            int size = L0ChunkSize + 2 * L1ChunkSize + sharedDataChunkSize;

            // 4 Optional L2 Chunks
            if (shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                size += 4 * L2TextureChunkSize;

            return size;
        }
    }
}
