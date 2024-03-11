using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using Unity.Collections;
using Unity.Profiling;

#if UNITY_EDITOR
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Initialization parameters for the probe volume system.
    /// </summary>
    public struct ProbeVolumeSystemParameters
    {
        /// <summary>
        /// The memory budget determining the size of the textures containing SH data.
        /// </summary>
        public ProbeVolumeTextureMemoryBudget memoryBudget;
        /// <summary>
        /// The memory budget determining the size of the textures used for blending between scenarios.
        /// </summary>
        public ProbeVolumeBlendingTextureMemoryBudget blendingMemoryBudget;
        /// <summary>
        /// The shader used to visualize the probes in the debug view.
        /// </summary>
        public Shader probeDebugShader;
        /// <summary>
        /// The shader used to visualize the way probes are sampled for a single pixel in the debug view.
        /// </summary>
        public Shader probeSamplingDebugShader;
        /// <summary>
        /// The debug texture used to display probe weight in the debug view.
        /// </summary>
        public Texture probeSamplingDebugTexture;
        /// <summary>
        /// The debug mesh used to visualize the way probes are sampled for a single pixel in the debug view.
        /// </summary>
        public Mesh probeSamplingDebugMesh;
        /// <summary>
        /// The shader used to visualize probes virtual offset in the debug view.
        /// </summary>
        public Shader offsetDebugShader;
        /// <summary>
        /// The shader used to visualize APV fragmentation.
        /// </summary>
        public Shader fragmentationDebugShader;
        /// <summary>
        /// The compute shader used to interpolate between two lighting scenarios.
        /// Set to null if blending is not supported.
        /// </summary>
        public ComputeShader scenarioBlendingShader;
        /// <summary>
        /// The compute shader used to upload streamed data to the GPU.
        /// </summary>
        public ComputeShader streamingUploadShader;
        /// <summary>
        /// The <see cref="ProbeVolumeSceneData"/>
        /// </summary>
        public ProbeVolumeSceneData sceneData;
        /// <summary>
        /// The <see cref="ProbeVolumeSHBands"/>
        /// </summary>
        public ProbeVolumeSHBands shBands;
        /// <summary>True if APV is able to show runtime debug information.</summary>
        public bool supportsRuntimeDebug;
        /// <summary>True if APV should support lighting scenarios.</summary>
        public bool supportScenarios;
        /// <summary>True if APV should support streaming of cell data to the GPU.</summary>
        public bool supportGPUStreaming;
        /// <summary>True if APV should support streaming of cell data from the disk.</summary>
        public bool supportDiskStreaming;
    }

    /// <summary>
    /// Struct holiding the <see cref="ProbeVolume"/> shading parameters
    /// </summary>
    public struct ProbeVolumeShadingParameters
    {
        /// <summary>
        /// Normal bias to apply to the position used to sample probe volumes.
        /// </summary>
        public float normalBias;
        /// <summary>
        /// View bias to apply to the position used to sample probe volumes.
        /// </summary>
        public float viewBias;
        /// <summary>
        /// Whether to scale the biases with the minimum distance between probes.
        /// </summary>
        public bool scaleBiasByMinDistanceBetweenProbes;
        /// <summary>
        /// Noise to be applied to the sampling position. It can hide seams issues between subdivision levels, but introduces noise.
        /// </summary>
        public float samplingNoise;
        /// <summary>
        /// Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe.
        /// </summary>
        public float weight;
        /// <summary>
        /// Method used for leak reduction.
        /// </summary>
        public APVLeakReductionMode leakReductionMode;
        /// <summary>
        /// Contribution of leak reduction weights.
        /// </summary>
        public float occlusionWeightContribution;
        /// <summary>
        /// The minimum value that dot(N, vectorToProbe) need to have to be considered valid.
        /// </summary>
        public float minValidNormalWeight;
        /// <summary>
        /// The frame index to be used to animate the sampling noise if requested.
        /// </summary>
        public int frameIndexForNoise;
        /// <summary>
        /// Lower clamp value for reflection probe normalization.
        /// </summary>
        public float reflNormalizationLowerClamp;
        /// <summary>
        /// Upper clamp value for reflection probe normalization.
        /// </summary>
        public float reflNormalizationUpperClamp;

    }

    /// <summary>
    /// Possible values for the probe volume memory budget (determines the size of the textures used).
    /// </summary>
    [Serializable]
    public enum ProbeVolumeTextureMemoryBudget
    {
        /// <summary>Low Budget</summary>
        MemoryBudgetLow = 512,
        /// <summary>Medium Budget</summary>
        MemoryBudgetMedium = 1024,
        /// <summary>High Budget</summary>
        MemoryBudgetHigh = 2048,
    }

    /// <summary>
    /// Possible values for the probe volume scenario blending memory budget (determines the size of the textures used).
    /// </summary>
    [Serializable]
    public enum ProbeVolumeBlendingTextureMemoryBudget
    {
        /// <summary>Low Budget</summary>
        MemoryBudgetLow = 128,
        /// <summary>Medium Budget</summary>
        MemoryBudgetMedium = 256,
        /// <summary>High Budget</summary>
        MemoryBudgetHigh = 512,
    }

    /// <summary>
    /// Number of Spherical Harmonics bands that are used with Probe Volumes
    /// </summary>
    [Serializable]
    public enum ProbeVolumeSHBands
    {
        /// <summary>Up to the L1 band of Spherical Harmonics</summary>
        SphericalHarmonicsL1 = 1,
        /// <summary>Up to the L2 band of Spherical Harmonics</summary>
        SphericalHarmonicsL2 = 2,
    }

    /// <summary>
    /// The reference volume for the Probe Volume system. This defines the structure in which volume assets are loaded into. There must be only one, hence why it follow a singleton pattern.
    /// </summary>
    public partial class ProbeReferenceVolume
    {
        [Serializable]
        internal struct IndirectionEntryInfo
        {
            public Vector3Int positionInBricks;
            public int minSubdiv;
            public bool hasOnlyBiggerBricks; // True if it has only bricks that are bigger than the entry itself
        }

        [Serializable]
        internal class CellDesc
        {
            public Vector3Int position;
            public int index;
            public int probeCount;
            public int minSubdiv;
            public int indexChunkCount;
            public int shChunkCount;
            public int bricksCount;

            // This is data that is generated at bake time to not having to re-analyzing the content of the cell for the indirection buffer.
            // This is not technically part of the descriptor of the cell but it needs to be here because it's computed at bake time and needs
            // to be serialized with the rest of the cell.
            public IndirectionEntryInfo[] indirectionEntryInfo;

            public override string ToString()
            {
                return $"Index = {index} position = {position}";
            }
        }

        internal class CellData
        {
            // Shared Data
            public NativeArray<byte> validityNeighMaskData;

            // Scenario Data
            public struct PerScenarioData
            {
                // L0/L1 Data
                public NativeArray<ushort> shL0L1RxData;
                public NativeArray<byte> shL1GL1RyData;
                public NativeArray<byte> shL1BL1RzData;

                // Optional L2 Data
                public NativeArray<byte> shL2Data_0;
                public NativeArray<byte> shL2Data_1;
                public NativeArray<byte> shL2Data_2;
                public NativeArray<byte> shL2Data_3;
            }

            public Dictionary<string, PerScenarioData> scenarios = new Dictionary<string, PerScenarioData>();
            
            // Brick data.
            public NativeArray<Brick> bricks { get; internal set; }

            // Support Data
            public NativeArray<Vector3> probePositions { get; internal set; }
            public NativeArray<float> touchupVolumeInteraction { get; internal set; } // Only used by a specific debug view.
            public NativeArray<Vector3> offsetVectors { get; internal set; }
            public NativeArray<float> validity { get; internal set; }

            public void CleanupPerScenarioData(in PerScenarioData data)
            {
                if (data.shL0L1RxData.IsCreated)
                {
                    data.shL0L1RxData.Dispose();
                    data.shL1GL1RyData.Dispose();
                    data.shL1BL1RzData.Dispose();
                }

                if (data.shL2Data_0.IsCreated)
                {
                    data.shL2Data_0.Dispose();
                    data.shL2Data_1.Dispose();
                    data.shL2Data_2.Dispose();
                    data.shL2Data_3.Dispose();
                }
            }

            public void Cleanup(bool cleanScenarioList)
            {
                // GPU Data. Will not exist if disk streaming is enabled.
                if (validityNeighMaskData.IsCreated)
                {
                    validityNeighMaskData.Dispose();
                    validityNeighMaskData = default;

                    foreach (var scenario in scenarios.Values)
                        CleanupPerScenarioData(scenario);
                }

                // When using disk streaming, we don't want to clear this list as it's the only place where we know which scenarios are available for the cell
                // This is ok because the scenario data isn't instantiated here.
                if (cleanScenarioList)
                    scenarios.Clear();

                // Bricks and support data. May not exist with disk streaming.
                if (bricks.IsCreated)
                {
                    bricks.Dispose();
                    bricks = default;
                }

                if (probePositions.IsCreated)
                {
                    probePositions.Dispose();
                    probePositions = default;
                    touchupVolumeInteraction.Dispose();
                    touchupVolumeInteraction = default;
                    validity.Dispose();
                    validity = default;
                    if (offsetVectors.IsCreated)
                    {
                        offsetVectors.Dispose();
                        offsetVectors = default;
                    }
                }
            }
        }

        internal class CellPoolInfo
        {
            public List<Chunk> chunkList = new List<Chunk>();
            public int shChunkCount;

            public void Clear()
            {
                chunkList.Clear();
            }
        }

        internal class CellIndexInfo
        {
            public int[] flatIndicesInGlobalIndirection = null;
            public ProbeBrickIndex.CellIndexUpdateInfo updateInfo;
            public bool indexUpdated;
            public IndirectionEntryInfo[] indirectionEntryInfo;
            public int indexChunkCount;

            public void Clear()
            {
                flatIndicesInGlobalIndirection = null;
                updateInfo = default(ProbeBrickIndex.CellIndexUpdateInfo);
                indexUpdated = false;
                indirectionEntryInfo = null;
            }
        }

        internal class CellBlendingInfo
        {
            public List<Chunk> chunkList = new List<Chunk>();
            public float blendingScore;
            public float blendingFactor;
            public bool blending;

            public void MarkUpToDate() => blendingScore = float.MaxValue;
            public bool IsUpToDate() => blendingScore == float.MaxValue;
            public void ForceReupload() => blendingFactor = -1.0f;
            public bool ShouldReupload() => blendingFactor == -1.0f;
            public void Prioritize() => blendingFactor = -2.0f;
            public bool ShouldPrioritize() => blendingFactor == -2.0f;

            public void Clear()
            {
                chunkList.Clear();
                blendingScore = 0;
                blendingFactor = 0;
                blending = false;
            }
        }

        internal class CellStreamingInfo
        {
            public CellStreamingRequest request = null;
            public CellStreamingRequest blendingRequest0 = null;
            public CellStreamingRequest blendingRequest1 = null;
            public float streamingScore;

            public bool IsStreaming()
            {
                return request != null && request.IsStreaming();
            }

            public bool IsBlendingStreaming()
            {
                return blendingRequest0 != null && blendingRequest0.IsStreaming()
                    || blendingRequest1 != null && blendingRequest1.IsStreaming();
            }

            public void Clear()
            {
                request = null;
                blendingRequest0 = null;
                blendingRequest1 = null;
                streamingScore = 0;
            }
        }

        [DebuggerDisplay("Index = {desc.index} Loaded = {loaded}")]
        internal class Cell : IComparable<Cell>
        {
            // Baked data (cell descriptor and baked probe data read from disk).
            public CellDesc desc;
            public CellData data;
            // Runtime info.
            public CellPoolInfo poolInfo = new CellPoolInfo();
            public CellIndexInfo indexInfo = new CellIndexInfo();
            public CellBlendingInfo blendingInfo = new CellBlendingInfo();
            public CellStreamingInfo streamingInfo = new CellStreamingInfo();

            public int referenceCount = 0;
            public bool loaded; // "Loaded" means the streaming system decided the cell should be loaded. It does not mean it's ready for GPU consumption (because of blending or disk streaming)

            public CellData.PerScenarioData scenario0;
            public CellData.PerScenarioData scenario1;
            public bool hasTwoScenarios;

            public CellInstancedDebugProbes debugProbes;

            public int CompareTo(Cell other)
            {
                if (streamingInfo.streamingScore < other.streamingInfo.streamingScore)
                    return -1;
                else if (streamingInfo.streamingScore > other.streamingInfo.streamingScore)
                    return 1;
                else
                    return 0;
            }

            public bool UpdateCellScenarioData(string scenario0, string scenario1)
            {
                if(!data.scenarios.TryGetValue(scenario0, out this.scenario0))
                {
                    return false;
                }

                hasTwoScenarios = false;

                if (!string.IsNullOrEmpty(scenario1))
                {
                    if (data.scenarios.TryGetValue(scenario1, out this.scenario1))
                        hasTwoScenarios = true;
                }

                return true;
            }

            public void Clear()
            {
                desc = null;
                data = null;
                poolInfo.Clear();
                indexInfo.Clear();
                blendingInfo.Clear();
                streamingInfo.Clear();

                referenceCount = 0;
                loaded = false;
                scenario0 = default;
                scenario1 = default;
                hasTwoScenarios = false;

                debugProbes = null;
            }
        }

        internal struct Volume : IEquatable<Volume>
        {
            internal Vector3 corner;
            internal Vector3 X;   // the vectors are NOT normalized, their length determines the size of the box
            internal Vector3 Y;
            internal Vector3 Z;

            internal float maxSubdivisionMultiplier;
            internal float minSubdivisionMultiplier;

            public Volume(Matrix4x4 trs, float maxSubdivision, float minSubdivision)
            {
                X = trs.GetColumn(0);
                Y = trs.GetColumn(1);
                Z = trs.GetColumn(2);
                corner = (Vector3)trs.GetColumn(3) - X * 0.5f - Y * 0.5f - Z * 0.5f;
                this.maxSubdivisionMultiplier = maxSubdivision;
                this.minSubdivisionMultiplier = minSubdivision;
            }

            public Volume(Vector3 corner, Vector3 X, Vector3 Y, Vector3 Z, float maxSubdivision = 1, float minSubdivision = 0)
            {
                this.corner = corner;
                this.X = X;
                this.Y = Y;
                this.Z = Z;
                this.maxSubdivisionMultiplier = maxSubdivision;
                this.minSubdivisionMultiplier = minSubdivision;
            }

            public Volume(Volume copy)
            {
                X = copy.X;
                Y = copy.Y;
                Z = copy.Z;
                corner = copy.corner;
                maxSubdivisionMultiplier = copy.maxSubdivisionMultiplier;
                minSubdivisionMultiplier = copy.minSubdivisionMultiplier;
            }

            public Volume(Bounds bounds)
            {
                var size = bounds.size;
                corner = bounds.center - size * 0.5f;
                X = new Vector3(size.x, 0, 0);
                Y = new Vector3(0, size.y, 0);
                Z = new Vector3(0, 0, size.z);

                maxSubdivisionMultiplier = minSubdivisionMultiplier = 0;
            }

            public Bounds CalculateAABB()
            {
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 dir = new Vector3(x, y, z);

                            Vector3 pt = corner
                                + X * dir.x
                                + Y * dir.y
                                + Z * dir.z;

                            min = Vector3.Min(min, pt);
                            max = Vector3.Max(max, pt);
                        }
                    }
                }

                return new Bounds((min + max) / 2, max - min);
            }

            public void CalculateCenterAndSize(out Vector3 center, out Vector3 size)
            {
                size = new Vector3(X.magnitude, Y.magnitude, Z.magnitude);
                center = corner + X * 0.5f + Y * 0.5f + Z * 0.5f;
            }

            public void Transform(Matrix4x4 trs)
            {
                corner = trs.MultiplyPoint(corner);
                X = trs.MultiplyVector(X);
                Y = trs.MultiplyVector(Y);
                Z = trs.MultiplyVector(Z);
            }

            public override string ToString()
            {
                return $"Corner: {corner}, X: {X}, Y: {Y}, Z: {Z}, MaxSubdiv: {maxSubdivisionMultiplier}";
            }

            public bool Equals(Volume other)
            {
                return corner == other.corner
                    && X == other.X
                    && Y == other.Y
                    && Z == other.Z
                    && minSubdivisionMultiplier == other.minSubdivisionMultiplier
                    && maxSubdivisionMultiplier == other.maxSubdivisionMultiplier;
            }
        }

        internal struct RefVolTransform
        {
            public Vector3 posWS;
            public Quaternion rot;
            public float scale;
        }

        /// <summary>
        /// The resources that are bound to the runtime shaders for sampling Adaptive Probe Volume data.
        /// </summary>
        public struct RuntimeResources
        {
            /// <summary>
            /// Index data to fetch the correct location in the Texture3D.
            /// </summary>
            public ComputeBuffer index;
            /// <summary>
            /// Indices of the various index buffers for each cell.
            /// </summary>
            public ComputeBuffer cellIndices;
            /// <summary>
            /// Texture containing Spherical Harmonics L0 band data and first coefficient of L1_R.
            /// </summary>
            public RenderTexture L0_L1rx;
            /// <summary>
            /// Texture containing the second channel of Spherical Harmonics L1 band data and second coefficient of L1_R.
            /// </summary>
            public RenderTexture L1_G_ry;
            /// <summary>
            /// Texture containing the second channel of Spherical Harmonics L1 band data and third coefficient of L1_R.
            /// </summary>
            public RenderTexture L1_B_rz;
            /// <summary>
            /// Texture containing the first coefficient of Spherical Harmonics L2 band data and first channel of the fifth.
            /// </summary>
            public RenderTexture L2_0;
            /// <summary>
            /// Texture containing the second coefficient of Spherical Harmonics L2 band data and second channel of the fifth.
            /// </summary>
            public RenderTexture L2_1;
            /// <summary>
            /// Texture containing the third coefficient of Spherical Harmonics L2 band data and third channel of the fifth.
            /// </summary>
            public RenderTexture L2_2;
            /// <summary>
            /// Texture containing the fourth coefficient of Spherical Harmonics L2 band data.
            /// </summary>
            public RenderTexture L2_3;

            /// <summary>
            /// Texture containing packed validity binary data for the neighbourhood of each probe. Only used when L1. Otherwise this info is stored
            /// in the alpha channel of L2_3.
            /// </summary>
            public RenderTexture Validity;

        }

        bool m_IsInitialized = false;
        bool m_SupportScenarios = false;
        bool m_ForceNoDiskStreaming = false;
        bool m_SupportDiskStreaming = false;
        bool m_SupportGPUStreaming = false;
        RefVolTransform m_Transform;
        int m_MaxSubdivision;
        ProbeBrickPool m_Pool;
        ProbeBrickIndex m_Index;
        ProbeGlobalIndirection m_CellIndices;
        ProbeBrickBlendingPool m_BlendingPool;
        List<Chunk> m_TmpSrcChunks = new List<Chunk>();
        float[] m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        Bounds m_CurrGlobalBounds = new Bounds();

        internal Bounds globalBounds { get { return m_CurrGlobalBounds; } set { m_CurrGlobalBounds = value; } }

        internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();
        ObjectPool<Cell> m_CellPool = new ObjectPool<Cell>(x => x.Clear(), null, false);

        ProbeBrickPool.DataLocation m_TemporaryDataLocation;
        int m_TemporaryDataLocationMemCost;

        internal ProbeVolumeSceneData sceneData;

        // We need to keep track the area, in cells, that is currently loaded. The index buffer will cover even unloaded areas, but we want to avoid sampling outside those areas.
        Vector3Int minLoadedCellPos = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxLoadedCellPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        /// <summary>
        ///  The input to the retrieveExtraDataAction action.
        /// </summary>
        public struct ExtraDataActionInput
        {
            // Empty, but defined to make this future proof without having to change public API
        }

        /// <summary>
        ///  An action that is used by the SRP to retrieve extra data that was baked together with the bake
        /// </summary>
        public Action<ExtraDataActionInput> retrieveExtraDataAction;

        /// <summary>
        ///  An action that is used by the SRP to perform checks every frame during baking.
        /// </summary>
        public Action checksDuringBakeAction = null;

        // Information of the probe volume scenes that is being loaded (if one is pending)
        Dictionary<string, List<int>> m_PendingScenesToBeLoaded = new Dictionary<string, List<int>>();

        // Information on probes we need to remove.
        Dictionary<string, List<int>> m_PendingScenesToBeUnloaded = new Dictionary<string, List<int>>();
        // Information of the probe volume scenes that is being loaded (if one is pending)
        List<string> m_ActiveScenes = new List<string>();

        ProbeVolumeBakingSet m_CurrentBakingSet = null;

        bool m_NeedLoadAsset = false;
        bool m_ProbeReferenceVolumeInit = false;
        bool m_EnabledBySRP = false;
        bool m_VertexSampling = false;

        /// <summary>Is Probe Volume initialized.</summary>
        public bool isInitialized => m_ProbeReferenceVolumeInit;
        internal bool enabledBySRP => m_EnabledBySRP;
        internal bool vertexSampling => m_VertexSampling;

        internal bool hasUnloadedCells => m_ToBeLoadedCells.size != 0;

        internal bool supportLightingScenarios => m_SupportScenarios;
        internal bool enableScenarioBlending => m_SupportScenarios && m_BlendingMemoryBudget != 0 && ProbeBrickBlendingPool.isSupported;
        internal bool diskStreamingEnabled => m_SupportDiskStreaming && !m_ForceNoDiskStreaming;

        bool m_NeedsIndexRebuild = false;
        bool m_HasChangedIndex = false;

        int m_CBShaderID = Shader.PropertyToID("ShaderVariablesProbeVolumes");

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        ProbeVolumeBlendingTextureMemoryBudget m_BlendingMemoryBudget;
        ProbeVolumeSHBands m_SHBands;
        float m_ProbeVolumesWeight;

        /// <summary>
        /// The <see cref="ProbeVolumeSHBands"/>
        /// </summary>
        public ProbeVolumeSHBands shBands => m_SHBands;

        internal bool clearAssetsOnVolumeClear = false;

        /// <summary>The active baking set.</summary>
        public ProbeVolumeBakingSet currentBakingSet => m_CurrentBakingSet;

        /// <summary>The active lighting scenario.</summary>
        public string lightingScenario
        {
            get => m_CurrentBakingSet ? m_CurrentBakingSet.lightingScenario : null;
            set
            {
                SetActiveScenario(value);
            }
        }

        /// <summary>The lighting scenario APV is blending toward.</summary>
        public string otherScenario
        {
            get => m_CurrentBakingSet ? m_CurrentBakingSet.otherScenario : null;
        }

        /// <summary>The blending factor currently used to blend probe data. A value of 0 means blending is not active.</summary>
        public float scenarioBlendingFactor
        {
            get => m_CurrentBakingSet ? m_CurrentBakingSet.scenarioBlendingFactor : 0.0f;
            set
            {
                if (m_CurrentBakingSet != null)
                    m_CurrentBakingSet.BlendLightingScenario(m_CurrentBakingSet.otherScenario, value);
            }
        }

        internal void SetActiveScenario(string scenario, bool verbose = true)
        {
            if (m_CurrentBakingSet != null)
                m_CurrentBakingSet.SetActiveScenario(scenario, verbose);
        }

        /// <summary>Allows smooth transitions between two lighting scenarios. This only affects the runtime data used for lighting.</summary>
        /// <param name="otherScenario">The name of the scenario to load.</param>
        /// <param name="blendingFactor">The factor used to interpolate between the active scenario and otherScenario. Accepted values range from 0 to 1 and will progressively blend from the active scenario to otherScenario.</param>
        public void BlendLightingScenario(string otherScenario, float blendingFactor)
        {
            if (m_CurrentBakingSet != null)
                m_CurrentBakingSet.BlendLightingScenario(otherScenario, blendingFactor);
        }

        internal static string defaultLightingScenario = "Default";

        /// <summary>
        /// Get the memory budget for the Probe Volume system.
        /// </summary>
        public ProbeVolumeTextureMemoryBudget memoryBudget => m_MemoryBudget;

        /// <summary>
        /// Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe.
        /// </summary>
        public float probeVolumesWeight { get => m_ProbeVolumesWeight; set => m_ProbeVolumesWeight = Mathf.Clamp01(value); }

        static ProbeReferenceVolume _instance = new ProbeReferenceVolume();

        internal List<ProbeVolumePerSceneData> perSceneDataList { get; private set; } = new List<ProbeVolumePerSceneData>();
        internal List<ProbeVolumePerSceneData> perSceneDataMigrationList { get; private set; } = new List<ProbeVolumePerSceneData>();

        internal void RegisterPerSceneDataMigration(ProbeVolumePerSceneData data)
        {
            if (!perSceneDataMigrationList.Contains(data))
                perSceneDataMigrationList.Add(data);
        }

        internal void UnregisterPerSceneDataMigration(ProbeVolumePerSceneData data)
        {
            perSceneDataMigrationList.Remove(data);
        }

        internal void RegisterPerSceneData(ProbeVolumePerSceneData data)
        {
            if (!perSceneDataList.Contains(data))
                perSceneDataList.Add(data);
        }

        /// <summary>
        /// Set the currently active baking set.
        /// Can be used when loading additively two scenes belonging to different baking sets to control which one is active
        /// </summary>
        /// <param name="bakingSet">The baking set to load.</param>
        public void SetActiveBakingSet(ProbeVolumeBakingSet bakingSet)
        {
            if (m_CurrentBakingSet == bakingSet)
                return;

            foreach (var data in perSceneDataList)
                data.QueueSceneRemoval();

            UnloadBakingSet();
            SetBakingSetAsCurrent(bakingSet);

            foreach (var data in perSceneDataList)
                data.QueueSceneLoading();
        }

        void SetBakingSetAsCurrent(ProbeVolumeBakingSet bakingSet)
        {
            m_CurrentBakingSet = bakingSet;

            // Can happen when you have only one scene loaded and you remove it from any baking set.
            if (m_CurrentBakingSet != null)
            {
                m_CurrentBakingSet.Initialize();
                m_CurrGlobalBounds = m_CurrentBakingSet.globalBounds;
                SetMinBrickAndMaxSubdiv(bakingSet.minBrickSize, bakingSet.maxSubdivision);

                m_NeedsIndexRebuild = true;
            }
        }

        internal void RegisterBakingSet(ProbeVolumePerSceneData data)
        {
            if (m_CurrentBakingSet == null)
            {
                SetBakingSetAsCurrent(sceneData.GetBakingSetForScene(data.sceneGUID));
            }
        }
        
        internal void UnloadBakingSet()
        {
            // Need to make sure everything is unloaded before killing the baking set ref (we need it to unload cell CPU data).
            PerformPendingOperations();

            if (m_CurrentBakingSet != null)
                m_CurrentBakingSet.Cleanup();
            m_CurrentBakingSet = null;
            m_CurrGlobalBounds = new Bounds();

            // Restart pool from zero to avoid unnecessary memory consumption when going from a big to a small scene.
            if (m_ScratchBufferPool != null)
            {
                m_ScratchBufferPool.Cleanup();
                m_ScratchBufferPool = null;
            }
        }

        internal void UnregisterPerSceneData(ProbeVolumePerSceneData data)
        {
            perSceneDataList.Remove(data);
            if (perSceneDataList.Count == 0)
                UnloadBakingSet();
        }

        internal float indexFragmentationRate { get => m_Index.fragmentationRate; }

        /// <summary>
        /// Get the instance of the probe reference volume (singleton).
        /// </summary>
        public static ProbeReferenceVolume instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Initialize the Probe Volume system
        /// </summary>
        /// <param name="parameters">Initialization parameters.</param>
        public void Initialize(in ProbeVolumeSystemParameters parameters)
        {
            if (m_IsInitialized)
            {
                Debug.LogError("Probe Volume System has already been initialized.");
                return;
            }

            m_MemoryBudget = parameters.memoryBudget;
            m_BlendingMemoryBudget = parameters.blendingMemoryBudget;
            m_SupportScenarios = parameters.supportScenarios;
            m_SHBands = parameters.shBands;
            m_ProbeVolumesWeight = 1f;
            m_SupportGPUStreaming = parameters.supportGPUStreaming;
            m_SupportDiskStreaming = parameters.supportDiskStreaming && SystemInfo.supportsComputeShaders && m_SupportGPUStreaming; // GPU Streaming is required for Disk Streaming
            // For now this condition is redundant with m_SupportDiskStreaming but we plan to support disk streaming without compute in the future.
            // So we need to split the conditions to plan for that.
            m_DiskStreamingUseCompute = SystemInfo.supportsComputeShaders && parameters.streamingUploadShader != null;
            InitializeDebug(parameters);
            ProbeBrickPool.Initialize(parameters);
            ProbeBrickBlendingPool.Initialize(parameters);
            InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, m_SHBands);
            m_IsInitialized = true;
            m_NeedsIndexRebuild = true;
            sceneData = parameters.sceneData;

#if UNITY_EDITOR
            if (sceneData != null)
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += sceneData.OnSceneSaving;
#endif
            m_EnabledBySRP = true;

            if (sceneData != null)
            {
                foreach (var data in perSceneDataMigrationList)
                {
                    data.MigrateIfNeeded();
                    RegisterPerSceneData(data);
                }

                perSceneDataMigrationList.Clear();

                foreach (var data in instance.perSceneDataList)
                    data.Initialize();
            }
        }

        /// <summary>
        /// Communicate to the Probe Volume system whether the SRP enables Probe Volume.
        /// It is important to keep in mind that this is not used by the system for anything else but book-keeping,
        /// the SRP is still responsible to disable anything Probe volume related on SRP side.
        /// </summary>
        /// <param name="srpEnablesPV">The value of the new enabled</param>
        public void SetEnableStateFromSRP(bool srpEnablesPV)
        {
            m_EnabledBySRP = srpEnablesPV;
        }

        /// <summary>
        /// Communicate to the Probe Volume system whether the SRP uses per vertex sampling
        /// </summary>
        /// <param name="value">True for vertex sampling, false for pixel sampling</param>
        public void SetVertexSamplingEnabled(bool value)
        {
            m_VertexSampling = value;
        }

        // This is used for steps such as dilation that require the maximum order allowed to be loaded at all times. Should really never be used as a general purpose function.
        internal void ForceSHBand(ProbeVolumeSHBands shBands)
        {
            foreach (var data in perSceneDataList)
                data.QueueSceneRemoval();

            PerformPendingOperations();

            if (m_ProbeReferenceVolumeInit)
                CleanupLoadedData();
            m_SHBands = shBands;

            DeinitProbeReferenceVolume();
            InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, shBands);

            foreach (var data in perSceneDataList)
                data.QueueSceneLoading();

            PerformPendingOperations();
        }

        internal void ForceNoDiskStreaming(bool state)
        {
            m_ForceNoDiskStreaming = state;
        }

        /// <summary>
        /// Cleanup the Probe Volume system.
        /// </summary>
        public void Cleanup()
        {
            CoreUtils.SafeRelease(m_EmptyIndexBuffer);
            m_EmptyIndexBuffer = null;

            if (!m_ProbeReferenceVolumeInit) return;

#if UNITY_EDITOR
            if (sceneData != null)
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= sceneData.OnSceneSaving;
#endif

            if (!m_IsInitialized)
            {
                Debug.LogError("Probe Volume System has not been initialized first before calling cleanup.");
                return;
            }

            CleanupLoadedData();
            CleanupDebug();
            CleanupStreaming();
            m_IsInitialized = false;
        }

        /// <summary>
        /// Get approximate video memory impact, in bytes, of the system.
        /// </summary>
        /// <returns>An approximation of the video memory impact, in bytes, of the system</returns>
        public int GetVideoMemoryCost()
        {
            if (!m_ProbeReferenceVolumeInit)
                return 0;

            return m_Pool.estimatedVMemCost + m_Index.estimatedVMemCost + m_CellIndices.estimatedVMemCost + m_BlendingPool.estimatedVMemCost + m_TemporaryDataLocationMemCost;
        }

        void RemoveCell(int cellIndex)
        {
            if (cells.TryGetValue(cellIndex, out var cellInfo))
            {
                cellInfo.referenceCount--;
                if (cellInfo.referenceCount <= 0)
                {
                    cells.Remove(cellIndex);

                    if (cellInfo.loaded)
                    {
                        m_LoadedCells.Remove(cellInfo);
                        UnloadCell(cellInfo);
                    }
                    else
                    {
                        m_ToBeLoadedCells.Remove(cellInfo);
                    }

                    m_CurrentBakingSet.ReleaseCell(cellIndex);
                    m_CellPool.Release(cellInfo);
                }
            }
        }

        // This one is internal for baking purpose only.
        // Calling this from "outside" will not properly update Loaded/ToBeLoadedCells arrays and thus will break the state of streaming.
        internal void UnloadCell(Cell cell)
        {
            // Streaming might have never loaded the cell in the first place
            if (cell.loaded)
            {
                if (cell.blendingInfo.blending)
                {
                    m_LoadedBlendingCells.Remove(cell);
                    UnloadBlendingCell(cell);
                }
                else
                    m_ToBeLoadedBlendingCells.Remove(cell);

                if (cell.indexInfo.flatIndicesInGlobalIndirection != null)
                    m_CellIndices.MarkEntriesAsUnloaded(cell.indexInfo.flatIndicesInGlobalIndirection);

                if (diskStreamingEnabled)
                {
                    if (cell.streamingInfo.IsStreaming())
                    {
                        CancelStreamingRequest(cell);
                    }
                    else
                    {
                        ReleaseBricks(cell);
                        cell.data.Cleanup(!diskStreamingEnabled); // Release CPU data
                    }
                }
                else
                    ReleaseBricks(cell);

                cell.loaded = false;
                cell.debugProbes = null;
                
                ClearDebugData();
            }
        }

        internal void UnloadBlendingCell(Cell cell)
        {
            if (diskStreamingEnabled && cell.streamingInfo.IsBlendingStreaming())
                CancelBlendingStreamingRequest(cell);

            if (cell.blendingInfo.blending)
            {
                m_BlendingPool.Deallocate(cell.blendingInfo.chunkList);
                cell.blendingInfo.chunkList.Clear();
                cell.blendingInfo.blending = false;
            }
        }

        internal void UnloadAllCells()
        {
            for (int i = 0; i < m_LoadedCells.size; ++i)
                UnloadCell(m_LoadedCells[i]);

            m_ToBeLoadedCells.AddRange(m_LoadedCells);
            m_LoadedCells.Clear();
        }

        internal void UnloadAllBlendingCells()
        {
            for (int i = 0; i < m_LoadedBlendingCells.size; ++i)
                UnloadBlendingCell(m_LoadedBlendingCells[i]);

            m_ToBeLoadedBlendingCells.AddRange(m_LoadedBlendingCells);
            m_LoadedBlendingCells.Clear();
        }

        void AddCell(int cellIndex)
        {
            // The same cell can exist in more than one scene
            // Need to check existence because we don't want to add cells more than once to streaming structures
            // TODO: Check perf if relevant?
            if (!cells.TryGetValue(cellIndex, out var cell))
            {
                var cellDesc = m_CurrentBakingSet.GetCellDesc(cellIndex);

                // This can happen if a baking set was cleared and not all scene were loaded.
                // This results in stray ProbeVolumeAssets for unloaded scenes that contains cell indices not present in the baking set if it was rebaked partially.
                if (cellDesc != null)
                {
                    cell = m_CellPool.Get();
                    cell.desc = cellDesc;
                    cell.data = m_CurrentBakingSet.GetCellData(cellIndex);
                    cell.poolInfo.shChunkCount = cell.desc.shChunkCount;
                    cell.indexInfo.flatIndicesInGlobalIndirection = m_CellIndices.GetFlatIndicesForCell(cellDesc.position);
                    cell.indexInfo.indexChunkCount = cell.desc.indexChunkCount;
                    cell.indexInfo.indirectionEntryInfo = cell.desc.indirectionEntryInfo;
                    cell.indexInfo.updateInfo.entriesInfo = new ProbeBrickIndex.IndirectionEntryUpdateInfo[cellDesc.indirectionEntryInfo.Length];
                    cell.referenceCount = 1;

                    cells[cellIndex] = cell;

                    m_ToBeLoadedCells.Add(cell);
                }
            }
            else
            {
                cell.referenceCount++;
            }
        }

        // This one is internal for baking purpose only.
        // Calling this from "outside" will not properly update Loaded/ToBeLoadedCells arrays and thus will break the state of streaming.
        internal bool LoadCell(Cell cell, bool ignoreErrorLog = false)
        {
            // First try to allocate pool memory. This is what is most likely to fail.
            if (ReservePoolChunks(cell.desc.bricksCount, cell.poolInfo.chunkList, ignoreErrorLog))
            {
                int indirectionBufferEntries = cell.indexInfo.indirectionEntryInfo.Length;

                var indexInfo = cell.indexInfo;

                for (int entry = 0; entry < indirectionBufferEntries; ++entry)
                {
                    int brickCountAtResForEntry = GetNumberOfBricksAtSubdiv(cell.indexInfo.indirectionEntryInfo[entry], ref indexInfo.updateInfo.entriesInfo[entry]);
                    indexInfo.updateInfo.entriesInfo[entry].numberOfChunks = m_Index.GetNumberOfChunks(brickCountAtResForEntry);
                }

                bool canAllocateCell = m_Index.FindSlotsForEntries(ref indexInfo.updateInfo.entriesInfo);
                if (canAllocateCell)
                {
                    bool scenarioValid = cell.UpdateCellScenarioData(lightingScenario, otherScenario);

                    bool successfulReserve = m_Index.ReserveChunks(indexInfo.updateInfo.entriesInfo, ignoreErrorLog);
                    Debug.Assert(successfulReserve);

                    for (int entry = 0; entry < indirectionBufferEntries; ++entry)
                    {
                        indexInfo.updateInfo.entriesInfo[entry].entryPositionInBricksAtMaxRes = indexInfo.indirectionEntryInfo[entry].positionInBricks;
                        indexInfo.updateInfo.entriesInfo[entry].minSubdivInCell = indexInfo.indirectionEntryInfo[entry].minSubdiv;
                        indexInfo.updateInfo.entriesInfo[entry].hasOnlyBiggerBricks = indexInfo.indirectionEntryInfo[entry].hasOnlyBiggerBricks;
                    }
                    cell.loaded = true;

                    // Copy proper data inside index buffers and pool textures or kick off streaming request.
                    if (scenarioValid)
                        AddBricks(cell);

                    minLoadedCellPos = Vector3Int.Min(minLoadedCellPos, cell.desc.position);
                    maxLoadedCellPos = Vector3Int.Max(maxLoadedCellPos, cell.desc.position);

                    ClearDebugData();

                    return true;
                }
                else
                {
                    // Index allocation failed, we need to release the pool chunks.
                    ReleasePoolChunks(cell.poolInfo.chunkList);
                    // We know we should have the space (test done in TryLoadCell above) so it's because of fragmentation.
                    StartIndexDefragmentation();
                }

                return false;
            }

            return false;
        }

        // May not load all cells if there is not enough space given the current budget.
        internal void LoadAllCells()
        {
            int loadedCellsCount = m_LoadedCells.size;
            for (int i = 0; i < m_ToBeLoadedCells.size; ++i)
            {
                Cell cell = m_ToBeLoadedCells[i];
                if (LoadCell(cell, ignoreErrorLog: true))
                    m_LoadedCells.Add(cell);
            }

            for (int i = loadedCellsCount; i < m_LoadedCells.size; ++i)
            {
                m_ToBeLoadedCells.Remove(m_LoadedCells[i]);
            }
        }

        // This will compute the min/max position of loaded cells as well as the max number of SH chunk for a cell.
        void ComputeCellGlobalInfo()
        {
            minLoadedCellPos = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            maxLoadedCellPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            foreach (var cell in cells.Values)
            {
                if (cell.loaded)
                {
                    minLoadedCellPos = Vector3Int.Min(cell.desc.position, minLoadedCellPos);
                    maxLoadedCellPos = Vector3Int.Max(cell.desc.position, maxLoadedCellPos);
                }
            }
        }

        internal void AddPendingSceneLoading(string sceneGUID)
        {
            if (m_PendingScenesToBeLoaded.ContainsKey(sceneGUID))
            {
                m_PendingScenesToBeLoaded.Remove(sceneGUID);
            }

            var bakingSet = sceneData.GetBakingSetForScene(sceneGUID);

            // User might have loaded other scenes with probe volumes but not belonging to the "single scene" baking set.
            if (bakingSet == null && m_CurrentBakingSet != null && m_CurrentBakingSet.singleSceneMode)
                return;

            if (bakingSet.chunkSizeInBricks != ProbeBrickPool.GetChunkSizeInBrickCount())
            {
                Debug.LogError($"Trying to load Probe Volume data ({bakingSet.name}) baked with an older incompatible version of APV. Please rebake your data.");
                return;
            }

            if (m_CurrentBakingSet != null && bakingSet != m_CurrentBakingSet)
            {
                // Trying to load data for a scene from a different baking set than currently loaded ones.
                // This should not throw an error, but it's not supported
                return;
            }

            // If we don't have any loaded asset yet, we need to verify the other queued assets.
            if (m_PendingScenesToBeLoaded.Count != 0)
            {
                foreach(var scene in m_PendingScenesToBeLoaded.Keys)
                {
                    var toBeLoadedBakingSet = sceneData.GetBakingSetForScene(scene);
                    if (bakingSet != toBeLoadedBakingSet)
                        return;

                    // Only need to check one entry here, they should all have the same baking set by construction.
                    break;
                }
            }

            m_PendingScenesToBeLoaded.Add(sceneGUID, m_CurrentBakingSet.GetSceneCellIndexList(sceneGUID));
            m_NeedLoadAsset = true;
        }

        internal void AddPendingSceneRemoval(string sceneGUID)
        {
            if (m_PendingScenesToBeLoaded.ContainsKey(sceneGUID))
                m_PendingScenesToBeLoaded.Remove(sceneGUID);
            if (m_ActiveScenes.Contains(sceneGUID))
                m_PendingScenesToBeUnloaded.TryAdd(sceneGUID, m_CurrentBakingSet.GetSceneCellIndexList(sceneGUID));
        }

        internal void RemovePendingScene(string sceneGUID, List<int> cellList)
        {
            if (m_ActiveScenes.Contains(sceneGUID))
            {
                m_ActiveScenes.Remove(sceneGUID);
            }

            // Remove bricks and empty cells
            foreach (var cellIndex in cellList)
            {
                RemoveCell(cellIndex);
            }

            ClearDebugData();
            ComputeCellGlobalInfo();
        }

        void PerformPendingIndexChangeAndInit()
        {
            if (m_NeedsIndexRebuild)
            {
                CleanupLoadedData();
                InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, m_SHBands);
                InitializeGlobalIndirection();
                m_HasChangedIndex = true;
                m_NeedsIndexRebuild = false;
            }
            else
            {
                m_HasChangedIndex = false;
            }
        }

        internal void SetMinBrickAndMaxSubdiv(float minBrickSize, int maxSubdiv)
        {
            SetTRS(Vector3.zero, Quaternion.identity, minBrickSize);
            SetMaxSubdivision(maxSubdiv);
        }

        bool LoadScene(string sceneGUID, List<int> cellList)
        {
            if (m_CurrentBakingSet.ResolveCellData(sceneGUID))
            {
                ClearDebugData();

                // Add all the cells to the system.
                // They'll be streamed in later on.
                for (int i = 0; i < cellList.Count; ++i)
                {
                    AddCell(cellList[i]);
                }

                return true;
            }

            return false;
        }

        void PerformPendingLoading()
        {
            if ((m_PendingScenesToBeLoaded.Count == 0 && m_ActiveScenes.Count == 0) || !m_NeedLoadAsset || !m_ProbeReferenceVolumeInit)
                return;

            m_Pool.EnsureTextureValidity();
            m_BlendingPool.EnsureTextureValidity();

            // Load the ones that are already active but reload if we said we need to load
            if (m_HasChangedIndex)
            {
                foreach (var sceneGUID in m_ActiveScenes)
                {
                    LoadScene(sceneGUID, m_CurrentBakingSet.GetSceneCellIndexList(sceneGUID));
                }
            }

            foreach (var loadRequest in m_PendingScenesToBeLoaded)
            {
                var sceneGUID = loadRequest.Key;
                if (LoadScene(sceneGUID, loadRequest.Value) && !m_ActiveScenes.Contains(sceneGUID))
                {
                    m_ActiveScenes.Add(sceneGUID);
                }
            }

            m_PendingScenesToBeLoaded.Clear();

            // Mark the loading as done.
            m_NeedLoadAsset = false;
        }

        void PerformPendingDeletion()
        {
            foreach (var unloadRequest in m_PendingScenesToBeUnloaded)
            {
                RemovePendingScene(unloadRequest.Key, unloadRequest.Value);
            }

            m_PendingScenesToBeUnloaded.Clear();
        }

        internal int GetNumberOfBricksAtSubdiv(IndirectionEntryInfo entryInfo, ref ProbeBrickIndex.IndirectionEntryUpdateInfo indirectionEntryUpdateInfo)
        {
            // This is a special case that can be handled manually easily.
            if (entryInfo.hasOnlyBiggerBricks)
            {
                indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes = Vector3Int.zero;
                indirectionEntryUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne = Vector3Int.one * CellSize(GetEntrySubdivLevel()) + Vector3Int.one;
                indirectionEntryUpdateInfo.brickCount = 1;
                return indirectionEntryUpdateInfo.brickCount;
            }

            indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes = Vector3Int.zero;
            Vector3Int sizeOfValidIndicesAtMaxRes = Vector3Int.one;

            float entrySize = GetEntrySize();
            var posWS =  new Vector3(entryInfo.positionInBricks.x , entryInfo.positionInBricks.y, entryInfo.positionInBricks.z) * MinBrickSize();
            Bounds entryBounds = new Bounds();
            entryBounds.min = posWS;
            entryBounds.max = posWS + (Vector3.one * entrySize);

            Bounds intersectBound = new Bounds();
            intersectBound.min = Vector3.Max(entryBounds.min, m_CurrGlobalBounds.min);
            intersectBound.max = Vector3.Min(entryBounds.max, m_CurrGlobalBounds.max);

            var toStart = intersectBound.min - entryBounds.min;
            indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.x = Mathf.CeilToInt((toStart.x) / MinBrickSize());
            indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.y = Mathf.CeilToInt((toStart.y) / MinBrickSize());
            indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.z = Mathf.CeilToInt((toStart.z) / MinBrickSize());

            var toEnd = intersectBound.max - entryBounds.min;
            sizeOfValidIndicesAtMaxRes.x = Mathf.CeilToInt((toEnd.x) / MinBrickSize()) - indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.x + 1;
            sizeOfValidIndicesAtMaxRes.y = Mathf.CeilToInt((toEnd.y) / MinBrickSize()) - indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.y + 1;
            sizeOfValidIndicesAtMaxRes.z = Mathf.CeilToInt((toEnd.z) / MinBrickSize()) - indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes.z + 1;

            Vector3Int bricksForEntry = new Vector3Int();
            bricksForEntry = sizeOfValidIndicesAtMaxRes / CellSize(entryInfo.minSubdiv);

            indirectionEntryUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne = indirectionEntryUpdateInfo.minValidBrickIndexForCellAtMaxRes + sizeOfValidIndicesAtMaxRes;

            indirectionEntryUpdateInfo.brickCount = bricksForEntry.x * bricksForEntry.y * bricksForEntry.z;

            return indirectionEntryUpdateInfo.brickCount;
        }

        /// <summary>
        /// Perform all the operations that are relative to changing the content or characteristics of the probe reference volume.
        /// </summary>
        public void PerformPendingOperations()
        {
#if UNITY_EDITOR
            checksDuringBakeAction?.Invoke();
#endif
            PerformPendingDeletion();
            PerformPendingIndexChangeAndInit();
            PerformPendingLoading();
        }

        internal void InitializeGlobalIndirection()
        {
            // Current baking set can be null at init and we still need the buffers to valid.
            var minCellPosition = m_CurrentBakingSet ? m_CurrentBakingSet.minCellPosition : Vector3Int.zero;
            var maxCellPosition = m_CurrentBakingSet ? m_CurrentBakingSet.maxCellPosition : Vector3Int.zero;
            if (m_CellIndices != null)
                m_CellIndices.Cleanup();
            m_CellIndices = new ProbeGlobalIndirection(minCellPosition, maxCellPosition, Mathf.Max(1, (int)Mathf.Pow(3, m_MaxSubdivision - 1)));
            if (m_SupportGPUStreaming)
            {
                if (m_DefragCellIndices != null)
                    m_DefragCellIndices.Cleanup();
                m_DefragCellIndices = new ProbeGlobalIndirection(minCellPosition, maxCellPosition, Mathf.Max(1, (int)Mathf.Pow(3, m_MaxSubdivision - 1)));
            }
        }

        /// <summary>
        /// Initialize the reference volume.
        /// </summary>
        /// <param name ="allocationSize"> Size used for the chunk allocator that handles bricks.</param>
        /// <param name ="memoryBudget">Probe reference volume memory budget.</param>
        /// <param name ="shBands">Probe reference volume SH bands.</param>
        void InitProbeReferenceVolume(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeBlendingTextureMemoryBudget blendingMemoryBudget, ProbeVolumeSHBands shBands)
        {
            if (!m_ProbeReferenceVolumeInit)
            {
                Profiler.BeginSample("Initialize Reference Volume");
                m_Pool = new ProbeBrickPool(m_MemoryBudget, shBands);
                m_BlendingPool = new ProbeBrickBlendingPool(m_BlendingMemoryBudget, shBands);

                m_Index = new ProbeBrickIndex(memoryBudget);

                if (m_SupportGPUStreaming)
                {
                    m_DefragIndex = new ProbeBrickIndex(memoryBudget);
                }

                InitializeGlobalIndirection();

                m_TemporaryDataLocation = ProbeBrickPool.CreateDataLocation(ProbeBrickPool.GetChunkSizeInProbeCount(), compressed: false, m_SHBands, "APV_Intermediate", false, true, out m_TemporaryDataLocationMemCost);

                // initialize offsets
                m_PositionOffsets[0] = 0.0f;
                float probeDelta = 1.0f / ProbeBrickPool.kBrickCellCount;
                for (int i = 1; i < ProbeBrickPool.kBrickProbeCountPerDim - 1; i++)
                    m_PositionOffsets[i] = i * probeDelta;
                m_PositionOffsets[m_PositionOffsets.Length - 1] = 1.0f;

                Profiler.EndSample();

                m_ProbeReferenceVolumeInit = true;

                ClearDebugData();

                m_NeedLoadAsset = true;
            }
        }

#if UNITY_EDITOR
        internal static Func<LightingSettings> _GetLightingSettingsOrDefaultsFallback;
#endif

        ProbeReferenceVolume()
        {
            m_Transform.posWS = Vector3.zero;
            m_Transform.rot = Quaternion.identity;
            m_Transform.scale = 1f;

#if UNITY_EDITOR
            Type lightMappingType = typeof(Lightmapping);
            var getLightingSettingsOrDefaultsFallbackInfo = lightMappingType.GetMethod("GetLightingSettingsOrDefaultsFallback", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightingSettingsOrDefaultsFallbackLambda = Expression.Lambda<Func<LightingSettings>>(Expression.Call(null, getLightingSettingsOrDefaultsFallbackInfo));
            _GetLightingSettingsOrDefaultsFallback = getLightingSettingsOrDefaultsFallbackLambda.Compile();
#endif
        }

#if UNITY_EDITOR
        internal void EnsureCurrentBakingSet(ProbeVolumeBakingSet bakingSet)
        {
            //Ensure that all currently loaded scenes belong to the same set.
            foreach (var data in perSceneDataList)
            {
                if (sceneData.GetBakingSetForScene(data.gameObject.scene))
                    Debug.Assert(sceneData.GetBakingSetForScene(data.sceneGUID) == bakingSet);
            }

            SetBakingSetAsCurrent(bakingSet);
        }
#endif

        /// <summary>
        /// Get the resources that are bound to the runtime shaders for sampling Adaptive Probe Volume data.
        /// </summary>
        /// <returns>The resources to bind to runtime shaders.</returns>
        public RuntimeResources GetRuntimeResources()
        {
            if (!m_ProbeReferenceVolumeInit)
                return default(RuntimeResources);

            RuntimeResources rr = new RuntimeResources();
            m_Index.GetRuntimeResources(ref rr);
            m_CellIndices.GetRuntimeResources(ref rr);
            m_Pool.GetRuntimeResources(ref rr);
            return rr;
        }

        internal void SetTRS(Vector3 position, Quaternion rotation, float minBrickSize)
        {
            m_Transform.posWS = position;
            m_Transform.rot = rotation;
            m_Transform.scale = minBrickSize;
        }

        internal void SetMaxSubdivision(int maxSubdivision)
        {
            int newValue = Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);
            if (newValue != m_MaxSubdivision)
            {
                m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);
                if (m_CellIndices != null)
                {
                    m_CellIndices.Cleanup();
                }
                if (m_SupportGPUStreaming && m_DefragCellIndices != null)
                {
                    m_DefragCellIndices.Cleanup();
                }
                InitializeGlobalIndirection();
            }
        }

        internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);
        internal float BrickSize(int subdivisionLevel) => m_Transform.scale * CellSize(subdivisionLevel);
        internal float MinBrickSize() => m_Transform.scale;
        internal float MaxBrickSize() => BrickSize(m_MaxSubdivision - 1);
        internal RefVolTransform GetTransform() => m_Transform;
        internal int GetMaxSubdivision() => m_MaxSubdivision;
        internal int GetMaxSubdivision(float multiplier) => Mathf.CeilToInt(m_MaxSubdivision * multiplier);
        internal float GetDistanceBetweenProbes(int subdivisionLevel) => BrickSize(subdivisionLevel) / 3.0f;
        internal float MinDistanceBetweenProbes() => GetDistanceBetweenProbes(0);

        // IMPORTANT! IF THIS VALUE CHANGES DATA NEEDS TO BE REBAKED.
        internal int GetGlobalIndirectionEntryMaxSubdiv() => ProbeGlobalIndirection.kEntryMaxSubdivLevel;

        internal int GetEntrySubdivLevel() => Mathf.Min(ProbeGlobalIndirection.kEntryMaxSubdivLevel, m_MaxSubdivision - 1);
        internal float GetEntrySize() => BrickSize(GetEntrySubdivLevel());
        /// <summary>
        /// Returns whether any brick data has been loaded.
        /// </summary>
        /// <returns></returns>
        public bool DataHasBeenLoaded() => m_LoadedCells.size != 0;

        internal void Clear()
        {
            if (m_ProbeReferenceVolumeInit)
            {
                try
                {
                    // Need to do that first because some assets may be in the process of being removed.
                    PerformPendingOperations();
                }
                finally
                {
                    UnloadAllCells();
                    m_Pool.Clear();
                    m_BlendingPool.Clear();
                    m_Index.Clear();
                    cells.Clear();

                    Debug.Assert(m_ToBeLoadedCells.size == 0);
                    Debug.Assert(m_LoadedCells.size == 0);
                }
            }

            if (clearAssetsOnVolumeClear)
            {
                m_PendingScenesToBeLoaded.Clear();
                m_ActiveScenes.Clear();
            }
        }

        // Currently only used for 1 chunk at a time but kept in case we need more in the future.
        List<Chunk> GetSourceLocations(int count, int chunkSize, ProbeBrickPool.DataLocation dataLoc)
        {
            var c = new Chunk();
            m_TmpSrcChunks.Clear();
            m_TmpSrcChunks.Add(c);

            // currently this code assumes that the texture width is a multiple of the allocation chunk size
            for (int j = 1; j < count; j++)
            {
                c.x += chunkSize * ProbeBrickPool.kBrickProbeCountPerDim;
                if (c.x >= dataLoc.width)
                {
                    c.x = 0;
                    c.y += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (c.y >= dataLoc.height)
                    {
                        c.y = 0;
                        c.z += ProbeBrickPool.kBrickProbeCountPerDim;
                    }
                }
                m_TmpSrcChunks.Add(c);
            }

            return m_TmpSrcChunks;
        }

        void UpdateDataLocationTexture<T>(Texture output, NativeArray<T> input) where T : struct
        {
            var outputNativeArray = (output as Texture3D).GetPixelData<T>(0);
            Debug.Assert(outputNativeArray.Length >= input.Length);
            outputNativeArray.GetSubArray(0, input.Length).CopyFrom(input);
            (output as Texture3D).Apply();
        }

        void UpdatePool(List<Chunk> chunkList, CellData.PerScenarioData data, NativeArray<byte> validityNeighMaskData, int chunkIndex, int poolIndex)
        {
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            UpdateDataLocationTexture(m_TemporaryDataLocation.TexL0_L1rx, data.shL0L1RxData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
            UpdateDataLocationTexture(m_TemporaryDataLocation.TexL1_G_ry, data.shL1GL1RyData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
            UpdateDataLocationTexture(m_TemporaryDataLocation.TexL1_B_rz, data.shL1BL1RzData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));

            if (poolIndex == -1) // validity data doesn't need to be updated per scenario
            {
                UpdateDataLocationTexture(m_TemporaryDataLocation.TexValidity, validityNeighMaskData.GetSubArray(chunkIndex * chunkSizeInProbes, chunkSizeInProbes));
            }

            if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_0, data.shL2Data_0.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
                UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_1, data.shL2Data_1.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
                UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_2, data.shL2Data_2.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
                UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_3, data.shL2Data_3.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
            }

            // New data format only uploads one chunk at a time (we need predictable chunk size)
            var srcChunks = GetSourceLocations(1, ProbeBrickPool.GetChunkSizeInBrickCount(), m_TemporaryDataLocation);

            // Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
            if (poolIndex == -1)
                m_Pool.Update(m_TemporaryDataLocation, srcChunks, chunkList, chunkIndex, m_SHBands);
            else
                m_BlendingPool.Update(m_TemporaryDataLocation, srcChunks, chunkList, chunkIndex, m_SHBands, poolIndex);
        }

        void UpdatePool(CommandBuffer cmd, List<Chunk> chunkList, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout, int poolIndex)
        {
            // Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
            if (poolIndex == -1)
                m_Pool.Update(cmd, dataBuffer, layout, chunkList, updateSharedData: true, m_Pool.GetValidityTexture(), m_SHBands);
            else
                m_BlendingPool.Update(cmd, dataBuffer, layout, chunkList, m_SHBands, poolIndex, m_Pool.GetValidityTexture());
        }

        void UpdatePoolValidity(List<Chunk> chunkList, NativeArray<byte> validityNeighMaskData, int chunkIndex)
        {
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInBrickCount() * ProbeBrickPool.kBrickProbeCountTotal;

            UpdateDataLocationTexture(m_TemporaryDataLocation.TexValidity, validityNeighMaskData.GetSubArray(chunkIndex * chunkSizeInProbes, chunkSizeInProbes));

            var srcChunks = GetSourceLocations(1, ProbeBrickPool.GetChunkSizeInBrickCount(), m_TemporaryDataLocation);

            m_Pool.UpdateValidity(m_TemporaryDataLocation, srcChunks, chunkList, chunkIndex);
        }

        // Runtime API starts here
        bool AddBlendingBricks(Cell cell)
        {
            Debug.Assert(cell.loaded);

            using var pm = new ProfilerMarker("AddBlendingBricks").Auto();

            Debug.Assert(cell.blendingInfo.chunkList.Count == 0);

            // If no blending is needed, bypass the blending pool and directly update uploaded cells
            bool bypassBlending = m_CurrentBakingSet.otherScenario == null || !cell.hasTwoScenarios;

            // Try to allocate texture space
            if (!bypassBlending && !m_BlendingPool.Allocate(cell.poolInfo.shChunkCount, cell.blendingInfo.chunkList))
                return false;

            if (diskStreamingEnabled)
            {
                if (bypassBlending)
                {
                    if (cell.blendingInfo.blendingFactor != scenarioBlendingFactor)
                        PushDiskStreamingRequest(cell, lightingScenario, -1, OnStreamingComplete);

                    // As we bypass blending, we don't load the blending data so we want to avoid trying to blend them later on.
                    cell.blendingInfo.MarkUpToDate();
                }
                else
                {
                    PushDiskStreamingRequest(cell, lightingScenario, 0, OnBlendingStreamingComplete);
                    PushDiskStreamingRequest(cell, otherScenario, 1, OnBlendingStreamingComplete);
                }
            }
            else
            {
                // Now that we are sure probe data will be uploaded, we can register the cell in the pool
                if (!cell.indexInfo.indexUpdated)
                {
                    // Update the cell index
                    UpdateCellIndex(cell);
                    // Upload validity data directly to main pool - constant per scenario, will not need blending, therefore we use the cellInfo chunk list.
                    var chunkList = cell.poolInfo.chunkList;
                    for (int chunkIndex = 0; chunkIndex < chunkList.Count; ++chunkIndex)
                        UpdatePoolValidity(chunkList, cell.data.validityNeighMaskData, chunkIndex);
                }

                if (bypassBlending)
                {
                    if (cell.blendingInfo.blendingFactor != scenarioBlendingFactor)
                    {
                        var chunkList = cell.poolInfo.chunkList;
                        for (int chunkIndex = 0; chunkIndex < chunkList.Count; ++chunkIndex)
                        {
                            // No blending so do the same operation as AddBricks would do. But because cell is already loaded,
                            // no index or chunk data must change, so only probe values need to be updated
                            UpdatePool(chunkList, cell.scenario0, cell.data.validityNeighMaskData, chunkIndex, -1);
                        }
                    }

                    // As we bypass blending, we don't load the blending data so we want to avoid trying to blend them later on.
                    cell.blendingInfo.MarkUpToDate();
                }
                else
                {
                    var chunkList = cell.blendingInfo.chunkList;
                    for (int chunkIndex = 0; chunkIndex < chunkList.Count; ++chunkIndex)
                    {
                        UpdatePool(chunkList, cell.scenario0, cell.data.validityNeighMaskData, chunkIndex, 0);
                        UpdatePool(chunkList, cell.scenario1, cell.data.validityNeighMaskData, chunkIndex, 1);
                    }
                }
            }

            cell.blendingInfo.blending = true;

            return true;
        }

        bool ReservePoolChunks(int brickCount, List<Chunk> chunkList, bool ignoreErrorLog)
        {
            // calculate the number of chunks necessary
            int brickChunksCount = ProbeBrickPool.GetChunkCount(brickCount);
            chunkList.Clear();

            // Try to allocate texture space
            return m_Pool.Allocate(brickChunksCount, chunkList, ignoreErrorLog);
        }

        void ReleasePoolChunks(List<Chunk> chunkList)
        {
            m_Pool.Deallocate(chunkList);
            chunkList.Clear();
        }

        void UpdatePoolAndIndex(Cell cell, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout, int poolIndex, CommandBuffer cmd)
        {
            if (diskStreamingEnabled && m_DiskStreamingUseCompute)
            {
                Debug.Assert(dataBuffer.buffer != null);
                UpdatePool(cmd, cell.poolInfo.chunkList, dataBuffer, layout, poolIndex);
            }
            else
            {
                // In order not to pre-allocate for the worse case, we update the texture by smaller chunks with a preallocated DataLoc
                for (int chunkIndex = 0; chunkIndex < cell.poolInfo.chunkList.Count; ++chunkIndex)
                    UpdatePool(cell.poolInfo.chunkList, cell.scenario0, cell.data.validityNeighMaskData, chunkIndex, poolIndex);
            }

            // Index may already be updated when simply switching scenarios.
            if (!cell.indexInfo.indexUpdated)
                UpdateCellIndex(cell);
        }

        bool AddBricks(Cell cell)
        {
            using var pm = new ProfilerMarker("AddBricks").Auto();

            if (enableScenarioBlending) // Register this cell for blending system
                m_ToBeLoadedBlendingCells.Add(cell);

            // If blending is enabled, we rely on it to upload data already blended to avoid popping
            // If enabled but blending factor is 0, upload here in case blending pool is not already allocated
            if (!enableScenarioBlending || scenarioBlendingFactor == 0.0f || !cell.hasTwoScenarios)
            {
                if (diskStreamingEnabled)
                {
                    PushDiskStreamingRequest(cell, m_CurrentBakingSet.lightingScenario, -1, OnStreamingComplete);
                }
                else
                {
                    UpdatePoolAndIndex(cell, null, default, -1, null);
                }

                cell.blendingInfo.blendingFactor = 0.0f;
            }
            else if (enableScenarioBlending)
            {
                cell.blendingInfo.Prioritize();
                // Cell index update is delayed until probe data is loaded
                cell.indexInfo.indexUpdated = false;
            }

            cell.loaded = true;
            ClearDebugData();

            return true;
        }

        void UpdateCellIndex(Cell cell)
        {
            cell.indexInfo.indexUpdated = true;

            // Build index
            var bricks = cell.data.bricks;
            m_Index.AddBricks(cell.indexInfo, bricks, cell.poolInfo.chunkList, ProbeBrickPool.GetChunkSizeInBrickCount(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());

            // Update indirection buffer
            m_CellIndices.UpdateCell(cell.indexInfo);
        }

        void ReleaseBricks(Cell cell)
        {
            if (cell.poolInfo.chunkList.Count == 0)
            {
                Debug.Log("Tried to release bricks from an empty Cell.");
                return;
            }

            // clean up the index
            m_Index.RemoveBricks(cell.indexInfo);
            cell.indexInfo.indexUpdated = false;

            // clean up the pool
            m_Pool.Deallocate(cell.poolInfo.chunkList);

            cell.poolInfo.chunkList.Clear();
        }

        internal void UpdateConstantBuffer(CommandBuffer cmd, ProbeVolumeShadingParameters parameters)
        {
            float normalBias = parameters.normalBias;
            float viewBias = parameters.viewBias;

            if (parameters.scaleBiasByMinDistanceBetweenProbes)
            {
                normalBias *= MinDistanceBetweenProbes();
                viewBias *= MinDistanceBetweenProbes();
            }

            var indexDim = m_CellIndices.GetGlobalIndirectionDimension();
            var poolDim = m_Pool.GetPoolDimensions();
            m_CellIndices.GetMinMaxEntry(out Vector3Int minEntry, out Vector3Int maxEntry);
            var entriesPerCell = m_CellIndices.entriesPerCellDimension;

            ShaderVariablesProbeVolumes shaderVars;
            shaderVars._Biases_CellInMinBrick_MinBrickSize = new Vector4(normalBias, viewBias, (int)Mathf.Pow(3, m_MaxSubdivision - 1), MinBrickSize());
            shaderVars._IndicesDim_IndexChunkSize = new Vector4(indexDim.x, indexDim.y, indexDim.z, ProbeBrickIndex.kIndexChunkSize);
            shaderVars._MinEntryPos_Noise = new Vector4(minEntry.x, minEntry.y, minEntry.z, parameters.samplingNoise); // TODO_FCC! IMPORTANT! RENAME!
            shaderVars._PoolDim_CellInMeters = new Vector4(poolDim.x, poolDim.y, poolDim.z, MaxBrickSize());
            shaderVars._RcpPoolDim_Padding = new Vector4(1.0f / poolDim.x, 1.0f / poolDim.y, 1.0f / poolDim.z, 1.0f / (poolDim.x * poolDim.y));
            shaderVars._Weight_MinLoadedCellInEntries = new Vector4(parameters.weight, minLoadedCellPos.x * entriesPerCell, minLoadedCellPos.y * entriesPerCell, minLoadedCellPos.z * entriesPerCell);
            shaderVars._MaxLoadedCellInEntries_FrameIndex = new Vector4((maxLoadedCellPos.x + 1) * entriesPerCell - 1, (maxLoadedCellPos.y + 1) * entriesPerCell - 1, (maxLoadedCellPos.z + 1) * entriesPerCell - 1, parameters.frameIndexForNoise);
            shaderVars._LeakReductionParams = new Vector4((int)parameters.leakReductionMode, parameters.occlusionWeightContribution, parameters.minValidNormalWeight, 0.0f);

            // TODO: Expose this somewhere UX visible? To discuss.
            shaderVars._NormalizationClamp_IndirectionEntryDim_Padding = new Vector4(parameters.reflNormalizationLowerClamp, parameters.reflNormalizationUpperClamp, GetEntrySize(), 0);

            ConstantBuffer.PushGlobal(cmd, shaderVars, m_CBShaderID);
        }

        void DeinitProbeReferenceVolume()
        {
            if (m_ProbeReferenceVolumeInit)
            {
                // We have to store the indices before the loop has it modifes the dictionary
                var cellIndices = new List<int>(cells.Keys);
                foreach (int cellIdx in cellIndices)
                    RemoveCell(cellIdx);
                Debug.Assert(cells.Count == 0);

                m_Index.Cleanup();
                m_CellIndices.Cleanup();

                if (m_SupportGPUStreaming)
                {
                    m_DefragIndex.Cleanup();
                    m_DefragCellIndices.Cleanup();
                }

                if (m_Pool != null)
                {
                    m_Pool.Cleanup();
                    m_BlendingPool.Cleanup();
                }

                m_TemporaryDataLocation.Cleanup();
                m_ProbeReferenceVolumeInit = false;
            }

            ClearDebugData();
            m_PendingScenesToBeUnloaded.Clear();
            m_ActiveScenes.Clear();

            Debug.Assert(m_ToBeLoadedCells.size == 0);
            Debug.Assert(m_LoadedCells.size == 0);
        }

        /// <summary>
        /// Cleanup loaded data.
        /// </summary>
        void CleanupLoadedData()
        {
            UnloadAllCells();
            DeinitProbeReferenceVolume();
        }
    }
}
