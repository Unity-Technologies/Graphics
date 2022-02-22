using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using Unity.Collections;
#if UNITY_EDITOR
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
#if UNITY_EDITOR

    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public class AdditionalGIBakeRequestsManager
    {
        // The baking ID for the extra requests
        // TODO: Need to ensure this never conflicts with bake IDs from others interacting with the API.
        // In our project, this is ProbeVolumes.
        internal static readonly int s_BakingID = 912345678;

        private static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();
        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        internal void Init()
        {
            SubscribeOnBakeStarted();
        }

        internal void Cleanup()
        {
            UnsubscribeOnBakeStarted();
        }

        const float kInvalidSH = 1f;
        const float kValidSH = 0f;

        private static List<SphericalHarmonicsL2> m_SHCoefficients = new List<SphericalHarmonicsL2>();
        private static List<float> m_SHValidity = new List<float>();
        private static List<Vector3> m_RequestPositions = new List<Vector3>();
        private static int m_FreelistHead = -1;

        private static readonly Vector2 s_FreelistSentinel = new Vector2(float.MaxValue, float.MaxValue);

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="capturePosition"> The position at which a probe is baked.</param>
        /// <returns>An ID that can be used to retrieve the data once it has been computed</returns>
        public int EnqueueRequest(Vector3 capturePosition)
        {
            Debug.Assert(ComputeCapturePositionIsValid(capturePosition));

            if (m_FreelistHead >= 0)
            {
                int requestID = m_FreelistHead;
                Debug.Assert(requestID < m_RequestPositions.Count);
                m_FreelistHead = ComputeFreelistNext(m_RequestPositions[requestID]);
                m_RequestPositions[requestID] = capturePosition;
                m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
                m_SHValidity[requestID] = kInvalidSH;
                return requestID;
            }
            else
            {
                int requestID = m_RequestPositions.Count;
                m_RequestPositions.Add(capturePosition);
                m_SHCoefficients.Add(new SphericalHarmonicsL2());
                m_SHValidity.Add(kInvalidSH);
                return requestID;
            }
        }

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="requestID"> An ID that can be used to retrieve the data once it has been computed</param>
        /// <returns>An ID that can be used to retrieve the data once it has been computed</returns>
        public void DequeueRequest(int requestID)
        {
            Debug.Assert(requestID >= 0 && requestID < m_RequestPositions.Count);

            m_RequestPositions[requestID] = new Vector3(s_FreelistSentinel.x, s_FreelistSentinel.y, m_FreelistHead);
            m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
            m_SHValidity[requestID] = kInvalidSH;
            m_FreelistHead = requestID;
        }

        private bool ComputeCapturePositionIsValid(Vector3 capturePosition)
        {
            return !((capturePosition.x == s_FreelistSentinel.x) && (capturePosition.y == s_FreelistSentinel.y));
        }

        private int ComputeFreelistNext(Vector3 capturePosition)
        {
            Debug.Assert(ComputeRequestIsFree(capturePosition));

            int freelistNext = (int)capturePosition.z;
            Debug.Assert(freelistNext >= -1 && freelistNext < m_RequestPositions.Count);
            return freelistNext;
        }

        private bool ComputeRequestIsFree(int requestID)
        {
            Debug.Assert(requestID >= 0 && requestID < m_RequestPositions.Count);
            Vector3 requestPosition = m_RequestPositions[requestID];
            return ComputeRequestIsFree(requestPosition);
        }

        private bool ComputeRequestIsFree(Vector3 capturePosition)
        {
            return (capturePosition.x == s_FreelistSentinel.x) && (capturePosition.y == s_FreelistSentinel.y);
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request has not been fulfilled yet or the request ID is invalid.
        /// </summary>
        /// <param name ="requestID"> The request ID that has been given by the manager through a previous EnqueueRequest.</param>
        /// <param name ="sh"> The output SH coefficients that have been computed.</param>
        /// <param name ="pos"> The position for which the computed SH coefficients are valid.</param>
        /// <returns>Whether the request for light probe rendering has been fulfilled and sh is valid.</returns>
        public bool RetrieveProbeSH(int requestID, out SphericalHarmonicsL2 sh, out Vector3 pos)
        {
            if (requestID >= 0 && requestID < m_SHCoefficients.Count && ComputeCapturePositionIsValid(m_RequestPositions[requestID]))
            {
                sh = m_SHCoefficients[requestID];
                pos = m_RequestPositions[requestID];
                return m_SHValidity[requestID] == kValidSH;
            }

            sh = new SphericalHarmonicsL2();
            pos = Vector3.negativeInfinity;
            return false;
        }

        /// <summary>
        /// Update the capture location for the probe request.
        /// </summary>
        /// <param name ="requestID"> The request ID that has been given by the manager through a previous EnqueueRequest.</param>
        /// <param name ="newPositionnewPosition"> The position at which a probe is baked.</param>
        public int UpdatePositionForRequest(int requestID, Vector3 newPosition)
        {
            if (requestID >= 0 && requestID < m_RequestPositions.Count)
            {
                Debug.Assert(ComputeCapturePositionIsValid(newPosition));
                m_RequestPositions[requestID] = newPosition;
                m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
                m_SHValidity[requestID] = kInvalidSH;
                return requestID;
            }
            else
            {
                return EnqueueRequest(newPosition);
            }
        }

        private void SubscribeOnBakeStarted()
        {
            UnsubscribeOnBakeStarted();
            Lightmapping.bakeStarted += AddRequestsToLightmapper;
        }

        private void UnsubscribeOnBakeStarted()
        {
            Lightmapping.bakeStarted -= AddRequestsToLightmapper;
            RemoveRequestsFromLightmapper();
        }

        internal void AddRequestsToLightmapper()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, m_RequestPositions.ToArray());

            Lightmapping.bakeCompleted -= OnAdditionalProbesBakeCompleted;
            Lightmapping.bakeCompleted += OnAdditionalProbesBakeCompleted;
        }

        private void RemoveRequestsFromLightmapper()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, null);
        }

        private void OnAdditionalProbesBakeCompleted()
        {
            Lightmapping.bakeCompleted -= OnAdditionalProbesBakeCompleted;

            if (m_RequestPositions.Count == 0) return;

            var sh = new NativeArray<SphericalHarmonicsL2>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bakedProbeOctahedralDepth = new NativeArray<float>(m_RequestPositions.Count * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(s_BakingID, sh, validity, bakedProbeOctahedralDepth))
            {
                SetSHCoefficients(sh, validity);
            }
            else
            {
                Debug.LogWarning($"Failed to collect results for additional probes. (Bake Id {s_BakingID})");
                ClearSHCoefficients();
            }

            ProbeReferenceVolume.instance.retrieveExtraDataAction?.Invoke(new ProbeReferenceVolume.ExtraDataActionInput());

            sh.Dispose();
            validity.Dispose();
            bakedProbeOctahedralDepth.Dispose();
        }

        private void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            Debug.Assert(sh.Length == m_SHCoefficients.Count);
            Debug.Assert(sh.Length == validity.Length);
            for (int i = 0; i < sh.Length; ++i)
            {
                var v = validity[i];
                var s = sh[i];

                if (v == kValidSH)
                {
                    var hasNonZeroValue = false;
                    for (var r = 0; r < 3; ++r)
                    {
                        for (var c = 0; c < 9; ++c)
                        {
                            if (s[r, c] != 0f)
                            {
                                hasNonZeroValue = true;
                                goto doubleBreak;
                            }
                        }
                    }
                    doubleBreak:

                    if (!hasNonZeroValue)
                    {
                        // Use max value as a sentinel to explicitly pass coefficients to light loop that cancel out reflection probe contribution
                        const float k = float.MaxValue;
                        s.AddAmbientLight(new Color(k, k, k));
                    }
                }

                m_SHCoefficients[i] = s;
                m_SHValidity[i] = v;
            }
        }

        private void ClearSHCoefficients()
        {
            for (int i = 0; i < m_SHCoefficients.Count; ++i)
            {
                m_SHCoefficients[i] = default;
                m_SHValidity[i] = kInvalidSH;
            }
        }
    }
#endif

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
        /// The debug mesh used to draw probes in the debug view.
        /// </summary>
        public Mesh probeDebugMesh;
        /// <summary>
        /// The shader used to visualize the probes in the debug view.
        /// </summary>
        public Shader probeDebugShader;
        /// <summary>
        /// The debug mesh used to visualize probes virtual offset in the debug view.
        /// </summary>
        public Mesh offsetDebugMesh;
        /// <summary>
        /// The shader used to visualize probes virtual offset in the debug view.
        /// </summary>
        public Shader offsetDebugShader;
        /// <summary>
        /// The compute shader used to interpolate between two lighting scenarios.
        /// Set to null if blending is not supported.
        /// </summary>
        public ComputeShader scenarioBlendingShader;
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
        /// <summary>True if APV should support streaming of cell data.</summary>
        public bool supportStreaming;
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
        internal const int kTemporaryDataLocChunkCount = 8;

        [Serializable]
        internal class Cell
        {
            public Vector3Int position;
            public int index;
            public int minSubdiv;
            public int indexChunkCount;
            public int shChunkCount;

            public bool hasTwoScenarios;

            public ProbeVolumeSHBands shBands;

            public NativeArray<Brick> bricks { get; internal set; }
            public NativeArray<uint> validity { get; internal set; }
            public NativeArray<Vector3> probePositions { get; internal set; }
            public NativeArray<float> touchupVolumeInteraction { get; internal set; } // Only used by a specific debug view.
            public NativeArray<Vector3> offsetVectors { get; internal set; }

            // Two scenarios for blending. When baking, only scenario0 is used
            [NonSerialized] public PerScenarioData scenario0;
            [NonSerialized] public PerScenarioData scenario1;

            public PerScenarioData bakingScenario => scenario0;

            public struct PerScenarioData
            {
                public NativeArray<float> shL0L1Data { get; internal set; } // pre-swizzled for runtime upload (12 coeffs)
                public NativeArray<float> shL2Data { get; internal set; } // pre-swizzled for runtime upload (15 coeffs)
            }

            static internal uint PackValidityAndMask(float validity, byte mask)
            {
                // 24 bits for validity at the top of the uint, 8 bits for the mask at the bottom.
                uint outValue = 0;
                outValue = (mask & 255u);
                uint packableValidity = 0;
                uint maxInt = (1 << 24) - 1u;
                packableValidity = (uint)(validity * maxInt + 0.5);
                outValue |= packableValidity << 8;
                return outValue;
            }

            internal float GetValidity(int probeIdx)
            {
                Debug.Assert(probeIdx < validity.Length);
                return GetValidityFromPacked(validity[probeIdx]);
            }

            internal static float GetValidityFromPacked(uint packedValidity)
            {
                uint extractionMask = (1 << 24) - 1;
                return ((packedValidity >> 8) & extractionMask) / (float)(extractionMask);
            }

            internal static byte GetValidityNeighMaskFromPacked(uint packedValidity)
            {
                return Convert.ToByte(packedValidity & 255);
            }

            internal byte GetNeighbourhoodValidityMask(int probeIdx)
            {
                Debug.Assert(probeIdx < validity.Length);
                return GetValidityNeighMaskFromPacked(validity[probeIdx]); // TODO_FCC: TMP.
            }

        }

        [DebuggerDisplay("Index = {cell.index} Loaded = {loaded}")]
        internal class CellInfo : IComparable<CellInfo>
        {
            public Cell cell;
            public BlendingCellInfo blendingCell;
            public List<Chunk> chunkList = new List<Chunk>();
            public int flatIdxInCellIndices = -1;
            public bool loaded = false;
            public ProbeBrickIndex.CellIndexUpdateInfo updateInfo;
            public int sourceAssetInstanceID;
            public float streamingScore;
            public int referenceCount = 0;

            public CellInstancedDebugProbes debugProbes;

            public int CompareTo(CellInfo other)
            {
                if (streamingScore < other.streamingScore)
                    return -1;
                else if (streamingScore > other.streamingScore)
                    return 1;
                else
                    return 0;
            }

            public void Clear()
            {
                cell = null;
                blendingCell = null;
                chunkList.Clear();
                flatIdxInCellIndices = -1;
                loaded = false;
                updateInfo = default(ProbeBrickIndex.CellIndexUpdateInfo);
                sourceAssetInstanceID = -1;
                streamingScore = 0;
                referenceCount = 0;
                debugProbes = null;
            }
        }

        [DebuggerDisplay("Index = {cellInfo.index} Factor = {blendingFactor}")]
        internal class BlendingCellInfo : IComparable<BlendingCellInfo>
        {
            public CellInfo cellInfo;
            public List<Chunk> chunkList = new List<Chunk>();
            public float streamingScore;
            public float blendingFactor;
            public bool blending;

            public int CompareTo(BlendingCellInfo other)
            {
                if (streamingScore < other.streamingScore)
                    return -1;
                else if (streamingScore > other.streamingScore)
                    return 1;
                else
                    return 0;
            }

            public void Clear()
            {
                cellInfo = null;
                chunkList.Clear();
                blendingFactor = 0;
                streamingScore = 0;
                blending = false;
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
            public Texture3D Validity;

        }

        bool m_IsInitialized = false;
        bool m_SupportStreaming = false;
        RefVolTransform m_Transform;
        int m_MaxSubdivision;
        ProbeBrickPool m_Pool;
        ProbeBrickIndex m_Index;
        ProbeCellIndices m_CellIndices;
        ProbeBrickBlendingPool m_BlendingPool;
        List<Chunk> m_TmpSrcChunks = new List<Chunk>();
        float[] m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        Bounds m_CurrGlobalBounds = new Bounds();

        internal Bounds globalBounds { get { return m_CurrGlobalBounds; } set { m_CurrGlobalBounds = value; } }

        internal Dictionary<int, CellInfo> cells = new Dictionary<int, CellInfo>();
        ObjectPool<CellInfo> m_CellInfoPool = new ObjectPool<CellInfo>(x => x.Clear(), null, false);
        ObjectPool<BlendingCellInfo> m_BlendingCellInfoPool = new ObjectPool<BlendingCellInfo>(x => x.Clear(), null, false);
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


        bool m_BricksLoaded = false;

        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeLoaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information on probes we need to remove.
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeUnloaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_ActiveAssets = new Dictionary<string, ProbeVolumeAsset>();

        bool m_NeedLoadAsset = false;
        bool m_ProbeReferenceVolumeInit = false;
        bool m_EnabledBySRP = false;

        /// <summary>Is Probe Volume initialized.</summary>
        public bool isInitialized => m_ProbeReferenceVolumeInit;
        internal bool enabledBySRP => m_EnabledBySRP;

        internal bool hasUnloadedCells => m_ToBeLoadedCells.size != 0;

        struct InitInfo
        {
            public Vector3Int pendingMinCellPosition;
            public Vector3Int pendingMaxCellPosition;
        }
        InitInfo m_PendingInitInfo;

        bool m_NeedsIndexRebuild = false;
        bool m_HasChangedIndex = false;

        int m_CBShaderID = Shader.PropertyToID("ShaderVariablesProbeVolumes");

#if UNITY_EDITOR
        // By default on editor we load a lot of cells in one go to avoid having to mess with scene view
        // to see results, this value can still be changed via API.
        private int m_NumberOfCellsLoadedPerFrame = 10000;
#else
        private int m_NumberOfCellsLoadedPerFrame = 2;
#endif

        internal int numberOfCellsLoadedPerFrame => m_NumberOfCellsLoadedPerFrame;

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        ProbeVolumeBlendingTextureMemoryBudget m_BlendingMemoryBudget;
        ProbeVolumeSHBands m_SHBands;
        float m_ProbeVolumesWeight;

        /// <summary>
        /// The <see cref="ProbeVolumeSHBands"/>
        /// </summary>
        public ProbeVolumeSHBands shBands => m_SHBands;

        internal bool clearAssetsOnVolumeClear = false;

        /// <summary>The active lighting scenario.</summary>
        public string lightingScenario
        {
            get => sceneData.lightingScenario;
            set => sceneData.SetActiveScenario(value);
        }

        /// <summary>The blending factor currently used to blend probe data. A value of 0 means blending is not active.</summary>
        public float scenarioBlendingFactor
        {
            get => sceneData.scenarioBlendingFactor;
            set => sceneData.BlendLightingScenario(sceneData.otherScenario, value);
        }

        /// <summary>Allows smooth transitions between two lighting scenarios. This only affects the runtime data used for lighting.</summary>
        /// <param name="otherScenario">The name of the scenario to load.</param>
        /// <param name="blendingFactor">The factor used to interpolate between the active scenario and otherScenario. Accepted values range from 0 to 1 and will progressively blend from the active scenario to otherScenario.</param>
        public void BlendLightingScenario(string otherScenario, float blendingFactor)
            => sceneData.BlendLightingScenario(otherScenario, blendingFactor);

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

        internal void RegisterPerSceneData(ProbeVolumePerSceneData data)
        {
            if (!perSceneDataList.Contains(data))
                perSceneDataList.Add(data);
        }

        internal void UnregisterPerSceneData(ProbeVolumePerSceneData data)
        {
            perSceneDataList.Remove(data);
        }

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
            m_SHBands = parameters.shBands;
            m_ProbeVolumesWeight = 1f;
            InitializeDebug(parameters);
            ProbeBrickBlendingPool.Initialize(parameters);
            InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, m_SHBands);
            m_IsInitialized = true;
            m_NeedsIndexRebuild = true;
            sceneData = parameters.sceneData;
            m_SupportStreaming = parameters.supportStreaming;

#if UNITY_EDITOR
            if (sceneData != null)
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += sceneData.OnSceneSaved;
            AdditionalGIBakeRequestsManager.instance.Init();
#endif
            m_EnabledBySRP = true;

            if (sceneData != null)
                foreach (var data in instance.perSceneDataList)
                    data.Initialize();
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

        // This is used for steps such as dilation that require the maximum order allowed to be loaded at all times. Should really never be used as a general purpose function.
        internal void ForceSHBand(ProbeVolumeSHBands shBands)
        {
            if (m_ProbeReferenceVolumeInit)
                CleanupLoadedData();
            m_SHBands = shBands;
            m_ProbeReferenceVolumeInit = false;
            InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, shBands);
        }

        /// <summary>
        /// Cleanup the Probe Volume system.
        /// </summary>
        public void Cleanup()
        {
            if (!m_ProbeReferenceVolumeInit) return;

#if UNITY_EDITOR
            AdditionalGIBakeRequestsManager.instance.Cleanup();
#endif

            if (!m_IsInitialized)
            {
                Debug.LogError("Probe Volume System has not been initialized first before calling cleanup.");
                return;
            }

            CleanupLoadedData();
            CleanupDebug();
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

        void RemoveCell(Cell cell)
        {
            if (cells.TryGetValue(cell.index, out var cellInfo))
            {
                cellInfo.referenceCount--;
                if (cellInfo.referenceCount <= 0)
                {
                    cells.Remove(cell.index);

                    if (cellInfo.loaded)
                    {
                        m_LoadedCells.Remove(cellInfo);
                        UnloadCell(cellInfo);
                    }
                    else
                    {
                        m_ToBeLoadedCells.Remove(cellInfo);
                    }

                    m_BlendingCellInfoPool.Release(cellInfo.blendingCell);
                    m_CellInfoPool.Release(cellInfo);
                }
            }
        }

        // This one is internal for baking purpose only.
        // Calling this from "outside" will not properly update Loaded/ToBeLoadedCells arrays and thus will break the state of streaming.
        internal void UnloadCell(CellInfo cellInfo)
        {
            // Streaming might have never loaded the cell in the first place
            if (cellInfo.loaded)
            {
                if (cellInfo.blendingCell.blending)
                {
                    m_LoadedBlendingCells.Remove(cellInfo.blendingCell);
                    UnloadBlendingCell(cellInfo.blendingCell);
                }
                else
                    m_ToBeLoadedBlendingCells.Remove(cellInfo.blendingCell);

                if (cellInfo.flatIdxInCellIndices >= 0)
                    m_CellIndices.MarkCellAsUnloaded(cellInfo.flatIdxInCellIndices);

                ReleaseBricks(cellInfo);

                cellInfo.loaded = false;
                cellInfo.updateInfo = new ProbeBrickIndex.CellIndexUpdateInfo();

                ClearDebugData();
            }
        }

        internal void UnloadBlendingCell(BlendingCellInfo blendingCell)
        {
            if (blendingCell.blending)
            {
                m_BlendingPool.Deallocate(blendingCell.chunkList);
                blendingCell.chunkList.Clear();
                blendingCell.blending = false;
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

        void AddCell(Cell cell, int assetInstanceID)
        {
            // The same cell can exist in more than one asset
            // Need to check existence because we don't want to add cells more than once to streaming structures
            // TODO: Check perf if relevant?
            if (!cells.TryGetValue(cell.index, out var cellInfo))
            {
                cellInfo = m_CellInfoPool.Get();
                cellInfo.cell = cell;
                cellInfo.flatIdxInCellIndices = m_CellIndices.GetFlatIdxForCell(cell.position);
                cellInfo.sourceAssetInstanceID = assetInstanceID;
                cellInfo.referenceCount = 1;
                cells[cell.index] = cellInfo;

                var blendingCell = m_BlendingCellInfoPool.Get();
                blendingCell.cellInfo = cellInfo;
                cellInfo.blendingCell = blendingCell;

                m_ToBeLoadedCells.Add(cellInfo);
            }
            else
            {
                cellInfo.referenceCount++;
            }
        }

        // This one is internal for baking purpose only.
        // Calling this from "outside" will not properly update Loaded/ToBeLoadedCells arrays and thus will break the state of streaming.
        internal bool LoadCell(CellInfo cellInfo)
        {
            if (GetCellIndexUpdate(cellInfo.cell, out var cellUpdateInfo)) // Allocate indices
            {
                minLoadedCellPos = Vector3Int.Min(minLoadedCellPos, cellInfo.cell.position);
                maxLoadedCellPos = Vector3Int.Max(maxLoadedCellPos, cellInfo.cell.position);

                return AddBricks(cellInfo, cellUpdateInfo);
            }
            else
            {
                return false;
            }
        }

        // May not load all cells if there is not enough space given the current budget.
        internal void LoadAllCells()
        {
            int loadedCellsCount = m_LoadedCells.size;
            for (int i = 0; i < m_ToBeLoadedCells.size; ++i)
            {
                CellInfo cellInfo = m_ToBeLoadedCells[i];
                if (LoadCell(cellInfo))
                    m_LoadedCells.Add(cellInfo);
            }

            for (int i = loadedCellsCount; i < m_LoadedCells.size; ++i)
            {
                m_ToBeLoadedCells.Remove(m_LoadedCells[i]);
            }
        }

        void RecomputeMinMaxLoadedCellPos()
        {
            minLoadedCellPos = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            maxLoadedCellPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            foreach (var cellInfo in cells.Values)
            {
                if (cellInfo.loaded)
                {
                    minLoadedCellPos = Vector3Int.Min(cellInfo.cell.position, minLoadedCellPos);
                    maxLoadedCellPos = Vector3Int.Max(cellInfo.cell.position, maxLoadedCellPos);
                }
            }
        }

        bool CheckCompatibilityWithCollection(ProbeVolumeAsset asset, Dictionary<string, ProbeVolumeAsset> collection)
        {
            if (collection.Count > 0)
            {
                // Any one is fine, they should all have the same properties. We need to go through them anyway as some might be pending deletion already.
                foreach (var collectionValue in collection.Values)
                {
                    // We don't care about this to check against, it is already pending deletion.
                    if (m_PendingAssetsToBeUnloaded.ContainsKey(collectionValue.GetSerializedFullPath()))
                        continue;

                    return collectionValue.CompatibleWith(asset);
                }
            }
            return true;
        }

        internal void AddPendingAssetLoading(ProbeVolumeAsset asset)
        {
            var key = asset.GetSerializedFullPath();

            if (m_PendingAssetsToBeLoaded.ContainsKey(key))
            {
                m_PendingAssetsToBeLoaded.Remove(key);
            }

            if (!CheckCompatibilityWithCollection(asset, m_ActiveAssets))
            {
                Debug.LogError($"Trying to load Probe Volume data for a scene that has been baked with different settings than currently loaded ones. " +
                               $"Please make sure all loaded scenes are in the same baking set.");
                return;
            }

            // If we don't have any loaded asset yet, we need to verify the other queued assets.
            if (!CheckCompatibilityWithCollection(asset, m_PendingAssetsToBeLoaded))
            {
                Debug.LogError($"Trying to load Probe Volume data for a scene that has been baked with different settings from other scenes that are being loaded. " +
                                $"Please make sure all loaded scenes are in the same baking set.");
                return;
            }

            m_PendingAssetsToBeLoaded.Add(key, asset);
            m_NeedLoadAsset = true;

            // Compute the max index dimension from all the loaded assets + assets we need to load
            Vector3Int indexDimension = Vector3Int.zero;
            Vector3Int minCellPosition = Vector3Int.one * 10000;
            Vector3Int maxCellPosition = Vector3Int.one * -10000;

            bool firstBound = true;
            foreach (var a in m_PendingAssetsToBeLoaded.Values)
            {
                minCellPosition = Vector3Int.Min(minCellPosition, a.minCellPosition);
                maxCellPosition = Vector3Int.Max(maxCellPosition, a.maxCellPosition);
                if (firstBound)
                {
                    m_CurrGlobalBounds = a.globalBounds;
                    firstBound = false;
                }
                else
                {
                    m_CurrGlobalBounds.Encapsulate(a.globalBounds);
                }
            }
            foreach (var a in m_ActiveAssets.Values)
            {
                minCellPosition = Vector3Int.Min(minCellPosition, a.minCellPosition);
                maxCellPosition = Vector3Int.Max(maxCellPosition, a.maxCellPosition);
                if (firstBound)
                {
                    m_CurrGlobalBounds = a.globalBounds;
                    firstBound = false;
                }
                else
                {
                    m_CurrGlobalBounds.Encapsulate(a.globalBounds);
                }
            }

            // |= because this can be called more than once before rebuild is done.
            m_NeedsIndexRebuild |= m_Index == null || m_PendingInitInfo.pendingMinCellPosition != minCellPosition || m_PendingInitInfo.pendingMaxCellPosition != maxCellPosition;

            m_PendingInitInfo.pendingMinCellPosition = minCellPosition;
            m_PendingInitInfo.pendingMaxCellPosition = maxCellPosition;
        }

        internal void AddPendingAssetRemoval(ProbeVolumeAsset asset)
        {
            var key = asset.GetSerializedFullPath();
            if (m_PendingAssetsToBeUnloaded.ContainsKey(key))
            {
                m_PendingAssetsToBeUnloaded.Remove(key);
            }
            m_PendingAssetsToBeUnloaded.Add(asset.GetSerializedFullPath(), asset);
        }

        internal void RemovePendingAsset(ProbeVolumeAsset asset)
        {
            var key = asset.GetSerializedFullPath();

            if (m_ActiveAssets.ContainsKey(key))
            {
                m_ActiveAssets.Remove(key);
            }

            // Remove bricks and empty cells
            foreach (var cell in asset.cells)
            {
                RemoveCell(cell);
            }

            // Remove all cells from the asset from streaming structures
            int assetInstanceID = asset.GetInstanceID();
            for (int i = m_LoadedCells.size - 1; i >= 0; i--)
            {
                if (m_LoadedCells[i].sourceAssetInstanceID == assetInstanceID)
                {
                    if (m_LoadedCells[i].blendingCell.blending)
                        m_LoadedBlendingCells.Remove(m_LoadedCells[i].blendingCell);
                    else
                        m_ToBeLoadedBlendingCells.Remove(m_LoadedCells[i].blendingCell);

                    m_LoadedCells.RemoveAt(i);
                }
            }
            for (int i = m_ToBeLoadedCells.size - 1; i >= 0; i--)
            {
                if (m_ToBeLoadedCells[i].sourceAssetInstanceID == assetInstanceID)
                {
                    m_ToBeLoadedCells.RemoveAt(i);
                }
            }

            ClearDebugData();
            RecomputeMinMaxLoadedCellPos();
        }

        void PerformPendingIndexChangeAndInit()
        {
            if (m_NeedsIndexRebuild)
            {
                CleanupLoadedData();
                InitProbeReferenceVolume(m_MemoryBudget, m_BlendingMemoryBudget, m_SHBands);
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

        void LoadAsset(ProbeVolumeAsset asset)
        {
            if (asset.Version != (int)ProbeVolumeAsset.AssetVersion.Current)
            {
                Debug.LogWarning($"Trying to load an asset {asset.GetSerializedFullPath()} that has been baked with a previous version of the system. Please re-bake the data.");
                return;
            }

            // Load info coming originally from profile
            SetMinBrickAndMaxSubdiv(asset.minBrickSize, asset.maxSubdivision);

            ClearDebugData();

            // Add all the cells to the system.
            // They'll be streamed in later on.
            for (int i = 0; i < asset.cells.Length; ++i)
            {
                AddCell(asset.cells[i], asset.GetInstanceID());
            }
        }

        void PerformPendingLoading()
        {
            if ((m_PendingAssetsToBeLoaded.Count == 0 && m_ActiveAssets.Count == 0) || !m_NeedLoadAsset || !m_ProbeReferenceVolumeInit)
                return;

            m_Pool.EnsureTextureValidity();
            m_BlendingPool.EnsureTextureValidity();

            // Load the ones that are already active but reload if we said we need to load
            if (m_HasChangedIndex)
            {
                foreach (var asset in m_ActiveAssets.Values)
                {
                    LoadAsset(asset);
                }
            }

            foreach (var asset in m_PendingAssetsToBeLoaded.Values)
            {
                LoadAsset(asset);
                if (!m_ActiveAssets.ContainsKey(asset.GetSerializedFullPath()))
                {
                    m_ActiveAssets.Add(asset.GetSerializedFullPath(), asset);
                }
            }

            m_PendingAssetsToBeLoaded.Clear();

            // Mark the loading as done.
            m_NeedLoadAsset = false;
        }

        void PerformPendingDeletion()
        {
            if (!m_ProbeReferenceVolumeInit)
            {
                m_PendingAssetsToBeUnloaded.Clear(); // If we are not init, we have not loaded yet.
            }

            var dictionaryValues = m_PendingAssetsToBeUnloaded.Values;
            foreach (var asset in dictionaryValues)
            {
                RemovePendingAsset(asset);
            }

            m_PendingAssetsToBeUnloaded.Clear();
        }

        internal int GetNumberOfBricksAtSubdiv(Vector3Int position, int minSubdiv, out Vector3Int minValidLocalIdxAtMaxRes, out Vector3Int sizeOfValidIndicesAtMaxRes)
        {
            minValidLocalIdxAtMaxRes = Vector3Int.zero;
            sizeOfValidIndicesAtMaxRes = Vector3Int.one;

            var posWS = new Vector3(position.x * MaxBrickSize(), position.y * MaxBrickSize(), position.z * MaxBrickSize());
            Bounds cellBounds = new Bounds();
            cellBounds.min = posWS;
            cellBounds.max = posWS + (Vector3.one * MaxBrickSize());

            Bounds intersectBound = new Bounds();
            intersectBound.min = Vector3.Max(cellBounds.min, m_CurrGlobalBounds.min);
            intersectBound.max = Vector3.Min(cellBounds.max, m_CurrGlobalBounds.max);

            var toStart = intersectBound.min - cellBounds.min;
            minValidLocalIdxAtMaxRes.x = Mathf.CeilToInt((toStart.x) / MinBrickSize());
            minValidLocalIdxAtMaxRes.y = Mathf.CeilToInt((toStart.y) / MinBrickSize());
            minValidLocalIdxAtMaxRes.z = Mathf.CeilToInt((toStart.z) / MinBrickSize());

            var toEnd = intersectBound.max - cellBounds.min;
            sizeOfValidIndicesAtMaxRes.x = Mathf.CeilToInt((toEnd.x) / MinBrickSize()) - minValidLocalIdxAtMaxRes.x + 1;
            sizeOfValidIndicesAtMaxRes.y = Mathf.CeilToInt((toEnd.y) / MinBrickSize()) - minValidLocalIdxAtMaxRes.y + 1;
            sizeOfValidIndicesAtMaxRes.z = Mathf.CeilToInt((toEnd.z) / MinBrickSize()) - minValidLocalIdxAtMaxRes.z + 1;

            Vector3Int bricksForCell = new Vector3Int();
            bricksForCell = sizeOfValidIndicesAtMaxRes / CellSize(minSubdiv);

            return bricksForCell.x * bricksForCell.y * bricksForCell.z;
        }

        bool GetCellIndexUpdate(Cell cell, out ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo)
        {
            cellUpdateInfo = new ProbeBrickIndex.CellIndexUpdateInfo();

            int brickCountsAtResolution = GetNumberOfBricksAtSubdiv(cell.position, cell.minSubdiv, out var minValidLocalIdx, out var sizeOfValidIndices);
            cellUpdateInfo.cellPositionInBricksAtMaxRes = cell.position * CellSize(m_MaxSubdivision - 1);
            cellUpdateInfo.minSubdivInCell = cell.minSubdiv;
            cellUpdateInfo.minValidBrickIndexForCellAtMaxRes = minValidLocalIdx;
            cellUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne = sizeOfValidIndices + minValidLocalIdx;

            return m_Index.AssignIndexChunksToCell(brickCountsAtResolution, ref cellUpdateInfo);
        }

        /// <summary>
        /// Perform all the operations that are relative to changing the content or characteristics of the probe reference volume.
        /// </summary>
        public void PerformPendingOperations()
        {
            PerformPendingDeletion();
            PerformPendingIndexChangeAndInit();
            PerformPendingLoading();
        }

        /// <summary>
        /// Initialize the reference volume.
        /// </summary>
        /// <param name ="allocationSize"> Size used for the chunk allocator that handles bricks.</param>
        /// <param name ="memoryBudget">Probe reference volume memory budget.</param>
        /// <param name ="shBands">Probe reference volume SH bands.</param>
        void InitProbeReferenceVolume(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeBlendingTextureMemoryBudget blendingMemoryBudget, ProbeVolumeSHBands shBands)
        {
            var minCellPosition = m_PendingInitInfo.pendingMinCellPosition;
            var maxCellPosition = m_PendingInitInfo.pendingMaxCellPosition;
            if (!m_ProbeReferenceVolumeInit)
            {
                Profiler.BeginSample("Initialize Reference Volume");
                m_Pool = new ProbeBrickPool(memoryBudget, shBands);
                m_BlendingPool = new ProbeBrickBlendingPool(blendingMemoryBudget, shBands);

                m_Index = new ProbeBrickIndex(memoryBudget);
                m_CellIndices = new ProbeCellIndices(minCellPosition, maxCellPosition, (int)Mathf.Pow(3, m_MaxSubdivision - 1));

                m_TemporaryDataLocation = ProbeBrickPool.CreateDataLocation(kTemporaryDataLocChunkCount * ProbeBrickPool.GetChunkSizeInProbeCount(), compressed: false, shBands, "APV_Intermediate", false, out m_TemporaryDataLocationMemCost);

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

        internal void SetMaxSubdivision(int maxSubdivision) => m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);
        internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);
        internal float BrickSize(int subdivisionLevel) => m_Transform.scale * CellSize(subdivisionLevel);
        internal float MinBrickSize() => m_Transform.scale;
        internal float MaxBrickSize() => BrickSize(m_MaxSubdivision - 1);
        internal RefVolTransform GetTransform() => m_Transform;
        internal int GetMaxSubdivision() => m_MaxSubdivision;
        internal int GetMaxSubdivision(float multiplier) => Mathf.CeilToInt(m_MaxSubdivision * multiplier);
        internal float GetDistanceBetweenProbes(int subdivisionLevel) => BrickSize(subdivisionLevel) / 3.0f;
        internal float MinDistanceBetweenProbes() => GetDistanceBetweenProbes(0);

        /// <summary>
        /// Returns whether any brick data has been loaded.
        /// </summary>
        /// <returns></returns>
        public bool DataHasBeenLoaded() => m_BricksLoaded;


        internal void Clear()
        {
            if (m_ProbeReferenceVolumeInit)
            {
                UnloadAllCells();
                m_Pool.Clear();
                m_BlendingPool.Clear();
                m_Index.Clear();
                cells.Clear();
            }

            if (clearAssetsOnVolumeClear)
            {
                m_PendingAssetsToBeLoaded.Clear();
                m_ActiveAssets.Clear();
            }
        }

        List<Chunk> GetSourceLocations(int count, int chunkSize)
        {
            var c = new Chunk();
            m_TmpSrcChunks.Clear();
            m_TmpSrcChunks.Add(c);

            // currently this code assumes that the texture width is a multiple of the allocation chunk size
            for (int j = 1; j < count; j++)
            {
                c.x += chunkSize * ProbeBrickPool.kBrickProbeCountPerDim;
                if (c.x >= m_TemporaryDataLocation.width)
                {
                    c.x = 0;
                    c.y += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (c.y >= m_TemporaryDataLocation.height)
                    {
                        c.y = 0;
                        c.z += ProbeBrickPool.kBrickProbeCountPerDim;
                    }
                }
                m_TmpSrcChunks.Add(c);
            }

            return m_TmpSrcChunks;
        }

        // Runtime API starts here
        bool AddBlendingBricks(BlendingCellInfo blendingCell)
        {
            Debug.Assert(blendingCell.cellInfo.loaded);

            Profiler.BeginSample("AddBlendingBricks");

            var cell = blendingCell.cellInfo.cell;
            var bricks = cell.bricks;

            // calculate the number of chunks necessary
            int chunkSize = ProbeBrickPool.GetChunkSize();
            blendingCell.chunkList.Clear();

            // If no blending is needed, bypass the blending pool and directly udpate uploaded cells
            bool bypassBlending = sceneData.otherScenario == null || !cell.hasTwoScenarios;

            // Try to allocate texture space
            if (!bypassBlending && !m_BlendingPool.Allocate(ProbeBrickPool.GetChunkCount(bricks.Length), blendingCell.chunkList))
                return false;

            // In order not to pre-allocate for the worse case, we update the texture by smaller chunks with a preallocated DataLoc
            int chunkCount = bypassBlending ? blendingCell.cellInfo.chunkList.Count : blendingCell.chunkList.Count;
            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex += kTemporaryDataLocChunkCount)
            {
                // copy chunks into pool
                int chunkToProcess = Math.Min(kTemporaryDataLocChunkCount, chunkCount - chunkIndex);
                var srcChunks = GetSourceLocations(chunkToProcess, chunkSize);

                if (bypassBlending)
                {
                    if (blendingCell.blendingFactor == scenarioBlendingFactor)
                        continue;

                    // No blending so do the same operation as AddBricks would do. But because cell is already loaded,
                    // no index or chunk data must change, so only probe values need to be updated

                    ProbeBrickPool.FillDataLocation(ref m_TemporaryDataLocation,
                        cell.shBands, cell.scenario0.shL0L1Data, cell.scenario0.shL2Data, cell.validity,
                        chunkIndex * ProbeBrickPool.GetChunkSizeInProbeCount(),
                        chunkToProcess * ProbeBrickPool.GetChunkSizeInProbeCount(),
                        m_SHBands);

                    // Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
                    m_Pool.Update(m_TemporaryDataLocation, srcChunks, blendingCell.cellInfo.chunkList, chunkIndex, m_SHBands);
                    continue;
                }

                // TODO: in both FillDataLocation below, don't load validity (and no need to allocate validity texture in the pool)

                // state0
                ProbeBrickPool.FillDataLocation(ref m_TemporaryDataLocation, cell.shBands,
                    cell.scenario0.shL0L1Data, cell.scenario0.shL2Data, cell.validity,
                    chunkIndex * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    chunkToProcess * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    m_SHBands);

                // Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
                m_BlendingPool.Update(m_TemporaryDataLocation, srcChunks, blendingCell.chunkList, chunkIndex, m_SHBands, 0);

                // state1
                ProbeBrickPool.FillDataLocation(ref m_TemporaryDataLocation, cell.shBands,
                    cell.scenario1.shL0L1Data, cell.scenario1.shL2Data, cell.validity,
                    chunkIndex * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    chunkToProcess * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    m_SHBands);

                m_BlendingPool.Update(m_TemporaryDataLocation, srcChunks, blendingCell.chunkList, chunkIndex, m_SHBands, 1);
            }

            blendingCell.blending = true;
            blendingCell.blendingFactor = cell.hasTwoScenarios ? scenarioBlendingFactor : 0.0f;

            Profiler.EndSample();

            return true;
        }

        bool AddBricks(CellInfo cellInfo, ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo)
        {
            Profiler.BeginSample("AddBricks");

            var cell = cellInfo.cell;
            var bricks = cell.bricks;

            // calculate the number of chunks necessary
            int chunkSize = ProbeBrickPool.GetChunkSize();
            int brickChunksCount = (bricks.Length + chunkSize - 1) / chunkSize;
            cellInfo.chunkList.Clear();

            // Try to allocate texture space
            if (!m_Pool.Allocate(brickChunksCount, cellInfo.chunkList))
                return false;

            // Queue this cell for blending
            bool useState0 = scenarioBlendingFactor < 0.5f || !cell.hasTwoScenarios;
            cellInfo.blendingCell.blendingFactor = useState0 ? 0.0f : 1.0f;
            m_ToBeLoadedBlendingCells.Add(cellInfo.blendingCell);

            // In order not to pre-allocate for the worse case, we update the texture by smaller chunks with a preallocated DataLoc
            int chunkIndex = 0;
            while (chunkIndex < cellInfo.chunkList.Count)
            {
                int chunkToProcess = Math.Min(kTemporaryDataLocChunkCount, cellInfo.chunkList.Count - chunkIndex);
                var shL0L1Data = useState0 ? cell.scenario0.shL0L1Data : cell.scenario1.shL0L1Data;
                var shL2Data = useState0 ? cell.scenario0.shL2Data : cell.scenario1.shL2Data;

                ProbeBrickPool.FillDataLocation(ref m_TemporaryDataLocation,
                    cell.shBands, shL0L1Data, shL2Data, cell.validity,
                    chunkIndex * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    chunkToProcess * ProbeBrickPool.GetChunkSizeInProbeCount(),
                    m_SHBands);

                // copy chunks into pool
                var srcChunks = GetSourceLocations(chunkToProcess, chunkSize);

                // Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
                m_Pool.Update(m_TemporaryDataLocation, srcChunks, cellInfo.chunkList, chunkIndex, m_SHBands);

                chunkIndex += kTemporaryDataLocChunkCount;
            }

            m_BricksLoaded = true;

            // Build index
            m_Index.AddBricks(cellInfo.cell, bricks, cellInfo.chunkList, ProbeBrickPool.GetChunkSize(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight(), cellUpdateInfo);

            // Update CellInfo
            cellInfo.updateInfo = cellUpdateInfo;
            cellInfo.loaded = true;

            // Update indirection buffer
            m_CellIndices.UpdateCell(cellInfo.flatIdxInCellIndices, cellUpdateInfo);

            ClearDebugData();

            Profiler.EndSample();

            return true;
        }

        void ReleaseBricks(CellInfo cellInfo)
        {
            if (cellInfo.chunkList.Count == 0)
            {
                Debug.Log("Tried to release bricks from an empty Cell.");
                return;
            }

            // clean up the index
            m_Index.RemoveBricks(cellInfo);

            // clean up the pool
            m_Pool.Deallocate(cellInfo.chunkList);

            cellInfo.chunkList.Clear();
        }

        /// <summary>
        /// Update the constant buffer used by Probe Volumes in shaders.
        /// </summary>
        /// <param name="cmd">A command buffer used to perform the data update.</param>
        /// <param name="parameters">Parameters to be used when sampling the probe volume.</param>
        public void UpdateConstantBuffer(CommandBuffer cmd, ProbeVolumeShadingParameters parameters)
        {
            float normalBias = parameters.normalBias;
            float viewBias = parameters.viewBias;

            if (parameters.scaleBiasByMinDistanceBetweenProbes)
            {
                normalBias *= MinDistanceBetweenProbes();
                viewBias *= MinDistanceBetweenProbes();
            }

            var minCellPos = m_CellIndices.GetCellMinPosition();
            var indexDim = m_CellIndices.GetCellIndexDimension();
            var poolDim = m_Pool.GetPoolDimensions();

            ShaderVariablesProbeVolumes shaderVars;
            shaderVars._Biases_CellInMinBrick_MinBrickSize = new Vector4(normalBias, viewBias, (int)Mathf.Pow(3, m_MaxSubdivision - 1), MinBrickSize());
            shaderVars._IndicesDim_IndexChunkSize = new Vector4(indexDim.x, indexDim.y, indexDim.z, ProbeBrickIndex.kIndexChunkSize);
            shaderVars._MinCellPos_Noise = new Vector4(minCellPos.x, minCellPos.y, minCellPos.z, parameters.samplingNoise);
            shaderVars._PoolDim_CellInMeters = new Vector4(poolDim.x, poolDim.y, poolDim.z, MaxBrickSize());
            shaderVars._Weight_MinLoadedCell = new Vector4(parameters.weight, minLoadedCellPos.x, minLoadedCellPos.y, minLoadedCellPos.z);
            shaderVars._MaxLoadedCell_FrameIndex = new Vector4(maxLoadedCellPos.x, maxLoadedCellPos.y, maxLoadedCellPos.z, parameters.frameIndexForNoise);
            shaderVars._LeakReductionParams = new Vector4((int)parameters.leakReductionMode, parameters.occlusionWeightContribution, parameters.minValidNormalWeight, 0.0f);

            ConstantBuffer.PushGlobal(cmd, shaderVars, m_CBShaderID);
        }

        /// <summary>
        /// Cleanup loaded data.
        /// </summary>
        void CleanupLoadedData()
        {
            m_BricksLoaded = false;

            UnloadAllCells();

            if (m_ProbeReferenceVolumeInit)
            {
                m_Index.Cleanup();
                m_CellIndices.Cleanup();
                m_Pool.Cleanup();
                m_BlendingPool.Cleanup();
                m_TemporaryDataLocation.Cleanup();
            }

            m_ProbeReferenceVolumeInit = false;
            ClearDebugData();
        }
    }
}
