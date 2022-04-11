using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
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

        private static List<SphericalHarmonicsL2> m_SHCoefficients = new List<SphericalHarmonicsL2>();
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
                return requestID;
            }
            else
            {
                int requestID = m_RequestPositions.Count;
                m_RequestPositions.Add(capturePosition);
                m_SHCoefficients.Add(new SphericalHarmonicsL2());
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
        /// <returns>Whether the request for light probe rendering has been fulfilled and sh is valid.</returns>
        public bool RetrieveProbeSH(int requestID, out SphericalHarmonicsL2 sh)
        {
            if (requestID >= 0 && requestID < m_SHCoefficients.Count
                && ComputeCapturePositionIsValid(m_RequestPositions[requestID]))
            {
                sh = m_SHCoefficients[requestID];
                return true;
            }
            else
            {
                sh = new SphericalHarmonicsL2();
                return false;
            }
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

            UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(s_BakingID, sh, validity, bakedProbeOctahedralDepth);

            SetSHCoefficients(sh);
            ProbeReferenceVolume.instance.retrieveExtraDataAction?.Invoke(new ProbeReferenceVolume.ExtraDataActionInput());

            sh.Dispose();
            validity.Dispose();
            bakedProbeOctahedralDepth.Dispose();
        }

        private void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh)
        {
            Debug.Assert(sh.Length == m_SHCoefficients.Count);
            for (int i = 0; i < sh.Length; ++i)
            {
                m_SHCoefficients[i] = sh[i];
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
        /// The debug mesh used to draw probes in the debug view.
        /// </summary>
        public Mesh probeDebugMesh;
        /// <summary>
        /// The shader used to visualize the probes in the debug view.
        /// </summary>
        public Shader probeDebugShader;

        public ProbeVolumeSceneData sceneData;
        public ProbeVolumeSHBands shBands;
    }

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
        const int kProbeIndexPoolAllocationSize = 128;

        [System.Serializable]
        internal class Cell
        {
            public int index;
            public Vector3Int position;
            public List<Brick> bricks;
            public Vector3[] probePositions;
            public SphericalHarmonicsL2[] sh;
            public float[] validity;
            public int minSubdiv;

            [System.NonSerialized]
            public int flatIdxInCellIndices = -1;

            [System.NonSerialized]
            public bool loaded = false;
        }

        class CellChunkInfo
        {
            public List<Chunk> chunks;
        }

        private class CellSortInfo : IComparable
        {
            internal string sourceAsset;
            internal Cell cell;
            internal float distanceToCamera = 0;
            internal Vector3 position;

            public int CompareTo(object obj)
            {
                CellSortInfo other = obj as CellSortInfo;

                if (distanceToCamera < other.distanceToCamera)
                    return 1;
                else if (distanceToCamera > other.distanceToCamera)
                    return -1;
                else
                    return 0;
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
            public Matrix4x4 refSpaceToWS;
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
            public Texture3D L0_L1rx;
            /// <summary>
            /// Texture containing the second channel of Spherical Harmonics L1 band data and second coefficient of L1_R.
            /// </summary>
            public Texture3D L1_G_ry;
            /// <summary>
            /// Texture containing the second channel of Spherical Harmonics L1 band data and third coefficient of L1_R.
            /// </summary>
            public Texture3D L1_B_rz;
            /// <summary>
            /// Texture containing the first coefficient of Spherical Harmonics L2 band data and first channel of the fifth.
            /// </summary>
            public Texture3D L2_0;
            /// <summary>
            /// Texture containing the second coefficient of Spherical Harmonics L2 band data and second channel of the fifth.
            /// </summary>
            public Texture3D L2_1;
            /// <summary>
            /// Texture containing the third coefficient of Spherical Harmonics L2 band data and third channel of the fifth.
            /// </summary>
            public Texture3D L2_2;
            /// <summary>
            /// Texture containing the fourth coefficient of Spherical Harmonics L2 band data.
            /// </summary>
            public Texture3D L2_3;
        }

        internal struct RegId
        {
            internal int id;

            public bool IsValid() => id != 0;
            public void Invalidate() => id = 0;
            public static bool operator ==(RegId lhs, RegId rhs) => lhs.id == rhs.id;
            public static bool operator !=(RegId lhs, RegId rhs) => lhs.id != rhs.id;
            public override bool Equals(object obj)
            {
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    RegId p = (RegId)obj;
                    return p == this;
                }
            }

            public override int GetHashCode() => id;
        }

        bool m_IsInitialized = false;
        int m_ID = 0;
        RefVolTransform m_Transform;
        int m_MaxSubdivision;
        ProbeBrickPool m_Pool;
        ProbeBrickIndex m_Index;
        ProbeCellIndices m_CellIndices;
        List<Chunk> m_TmpSrcChunks = new List<Chunk>();
        float[] m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        Dictionary<RegId, List<Chunk>> m_Registry = new Dictionary<RegId, List<Chunk>>();
        Bounds m_CurrGlobalBounds = new Bounds();

        internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();
        Dictionary<int, CellChunkInfo> m_ChunkInfo = new Dictionary<int, CellChunkInfo>();

        internal ProbeVolumeSceneData sceneData;


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
        Dictionary<Cell, RegId> m_CellToBricks = new Dictionary<Cell, RegId>();
        Dictionary<RegId, ProbeBrickIndex.CellIndexUpdateInfo> m_BricksToCellUpdateInfo = new Dictionary<RegId, ProbeBrickIndex.CellIndexUpdateInfo>();

        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeLoaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information on probes we need to remove.
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeUnloaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_ActiveAssets = new Dictionary<string, ProbeVolumeAsset>();

        // List of info for cells that are yet to be loaded.
        private List<CellSortInfo> m_CellsToBeLoaded = new List<CellSortInfo>();


        // Ref counting here as a separate dictionary as a temporary measure to facilitate future changes that will soon go in.
        // cell.index, refCount
        Dictionary<int, int> m_CellRefCounting = new Dictionary<int, int>();
        void InvalidateAllCellRefs()
        {
            m_CellRefCounting.Clear();
        }

        bool m_NeedLoadAsset = false;
        bool m_ProbeReferenceVolumeInit = false;
        bool m_EnabledBySRP = false;

        internal bool isInitialized => m_ProbeReferenceVolumeInit;
        internal bool enabledBySRP => m_EnabledBySRP;

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

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        ProbeVolumeSHBands m_SHBands;

        public ProbeVolumeSHBands shBands { get { return m_SHBands; } }

        internal bool clearAssetsOnVolumeClear = false;

        /// <summary>
        /// Get the memory budget for the Probe Volume system.
        /// </summary>
        public ProbeVolumeTextureMemoryBudget memoryBudget => m_MemoryBudget;

        static ProbeReferenceVolume _instance = new ProbeReferenceVolume();

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
        /// Set the number of cells that are loaded per frame when needed.
        /// </summary>
        /// <param name="numberOfCells"></param>
        public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
        {
            m_NumberOfCellsLoadedPerFrame = Mathf.Max(1, numberOfCells);
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
            m_SHBands = parameters.shBands;
            InitializeDebug(parameters.probeDebugMesh, parameters.probeDebugShader);
            InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_SHBands);
            m_IsInitialized = true;
            m_NeedsIndexRebuild = true;
            sceneData = parameters.sceneData;
#if UNITY_EDITOR
            if (sceneData != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += sceneData.OnSceneSaved;
            }
            AdditionalGIBakeRequestsManager.instance.Init();
#endif
            m_EnabledBySRP = true;
        }

        /// <summary>
        /// Communicate to the Probe Volume system whether the SRP enables Probe Volume.
        /// It is important to keep in mind that this is not used by the system for anything else but book-keeping,
        /// the SRP is still responsible to disable anything Probe volume related on SRP side.
        /// </summary>
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
            InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, shBands);
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
        /// <returns>An approximation of the video memory impact, in bytes, of the system<returns>
        public int GetVideoMemoryCost()
        {
            if (!m_ProbeReferenceVolumeInit)
                return 0;

            return m_Pool.estimatedVMemCost + m_Index.estimatedVMemCost + m_CellIndices.estimatedVMemCost;
        }

        void RemoveCell(Cell cell)
        {
            if (cell.loaded)
            {
                bool needsUnloading = true;
                if (m_CellRefCounting.ContainsKey(cell.index))
                {
                    m_CellRefCounting[cell.index]--;
                    needsUnloading = m_CellRefCounting[cell.index] <= 0;
                    if (needsUnloading)
                    {
                        m_CellRefCounting[cell.index] = 0;
                    }
                }

                if (needsUnloading)
                {
                    if (cells.ContainsKey(cell.index))
                        cells.Remove(cell.index);

                    if (m_ChunkInfo.ContainsKey(cell.index))
                        m_ChunkInfo.Remove(cell.index);

                    if (cell.flatIdxInCellIndices >= 0)
                        m_CellIndices.MarkCellAsUnloaded(cell.flatIdxInCellIndices);

                    RegId cellBricksID = new RegId();
                    if (m_CellToBricks.TryGetValue(cell, out cellBricksID))
                    {
                        ReleaseBricks(cellBricksID);
                        m_CellToBricks.Remove(cell);
                    }
                }
            }

            cell.loaded = false;
        }

        void AddCell(Cell cell, List<Chunk> chunks)
        {
            if (m_CellRefCounting.ContainsKey(cell.index)) m_CellRefCounting[cell.index]++;
            else m_CellRefCounting.Add(cell.index, 1);

            cell.loaded = true;
            cells[cell.index] = cell;

            var cellChunks = new CellChunkInfo();
            cellChunks.chunks = chunks;
            m_ChunkInfo[cell.index] = cellChunks;
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
            Vector3Int minCellPosition = Vector3Int.zero;
            Vector3Int maxCellPosition = Vector3Int.zero;

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

            for (int i = m_CellsToBeLoaded.Count - 1; i >= 0; i--)
            {
                if (m_CellsToBeLoaded[i].sourceAsset == key)
                    m_CellsToBeLoaded.RemoveAt(i);
            }

            if (m_ActiveAssets.ContainsKey(key))
            {
                m_ActiveAssets.Remove(key);
            }

            // Remove bricks and empty cells
            foreach (var cell in asset.cells)
            {
                RemoveCell(cell);
            }

            ClearDebugData();
        }

        void PerformPendingIndexChangeAndInit()
        {
            if (m_NeedsIndexRebuild)
            {
                CleanupLoadedData();
                InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_SHBands);
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

            var path = asset.GetSerializedFullPath();

            // Load info coming originally from profile
            SetMinBrickAndMaxSubdiv(asset.minBrickSize, asset.maxSubdivision);

            for (int i = 0; i < asset.cells.Count; ++i)
            {
                var cell = asset.cells[i];
                CellSortInfo sortInfo = new CellSortInfo();
                sortInfo.cell = cell;
                sortInfo.position = ((Vector3)cell.position * MaxBrickSize() * 0.5f) + m_Transform.posWS;
                sortInfo.sourceAsset = asset.GetSerializedFullPath();
                m_CellsToBeLoaded.Add(sortInfo);
            }
        }

        void PerformPendingLoading()
        {
            if ((m_PendingAssetsToBeLoaded.Count == 0 && m_ActiveAssets.Count == 0) || !m_NeedLoadAsset || !m_ProbeReferenceVolumeInit)
                return;

            m_Pool.EnsureTextureValidity();

            // Load the ones that are already active but reload if we said we need to load
            if (m_HasChangedIndex)
            {
                // We changed index so all assets are going to be re-loaded, hence the refs will be repopulated from scratch
                InvalidateAllCellRefs();

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

        int GetNumberOfBricksAtSubdiv(Cell cell, out Vector3Int minValidLocalIdxAtMaxRes, out Vector3Int sizeOfValidIndicesAtMaxRes)
        {
            minValidLocalIdxAtMaxRes = Vector3Int.zero;
            sizeOfValidIndicesAtMaxRes = Vector3Int.one;

            var posWS = new Vector3(cell.position.x * MaxBrickSize(), cell.position.y * MaxBrickSize(), cell.position.z * MaxBrickSize());
            Bounds cellBounds = new Bounds();
            cellBounds.min = posWS;
            cellBounds.max = posWS + (Vector3.one * MaxBrickSize());

            Bounds intersectBound = new Bounds();
            intersectBound.min = Vector3.Max(cellBounds.min, m_CurrGlobalBounds.min);
            intersectBound.max = Vector3.Min(cellBounds.max, m_CurrGlobalBounds.max);

            Vector3 size = intersectBound.max - intersectBound.min;

            var toStart = intersectBound.min - cellBounds.min;
            minValidLocalIdxAtMaxRes.x = Mathf.CeilToInt((toStart.x) / MinBrickSize());
            minValidLocalIdxAtMaxRes.y = Mathf.CeilToInt((toStart.y) / MinBrickSize());
            minValidLocalIdxAtMaxRes.z = Mathf.CeilToInt((toStart.z) / MinBrickSize());

            var toEnd = intersectBound.max - cellBounds.min;
            sizeOfValidIndicesAtMaxRes.x = Mathf.CeilToInt((toEnd.x) / MinBrickSize()) - minValidLocalIdxAtMaxRes.x + 1;
            sizeOfValidIndicesAtMaxRes.y = Mathf.CeilToInt((toEnd.y) / MinBrickSize()) - minValidLocalIdxAtMaxRes.y + 1;
            sizeOfValidIndicesAtMaxRes.z = Mathf.CeilToInt((toEnd.z) / MinBrickSize()) - minValidLocalIdxAtMaxRes.z + 1;

            Vector3Int bricksForCell = new Vector3Int();
            bricksForCell = sizeOfValidIndicesAtMaxRes / CellSize(cell.minSubdiv);

            return bricksForCell.x * bricksForCell.y * bricksForCell.z;
        }

        bool GetCellIndexUpdate(Cell cell, out ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo)
        {
            cellUpdateInfo = new ProbeBrickIndex.CellIndexUpdateInfo();

            int brickCountsAtResolution = GetNumberOfBricksAtSubdiv(cell, out var minValidLocalIdx, out var sizeOfValidIndices);
            cellUpdateInfo.cellPositionInBricksAtMaxRes = cell.position * CellSize(m_MaxSubdivision - 1);
            cellUpdateInfo.minSubdivInCell = cell.minSubdiv;
            cellUpdateInfo.minValidBrickIndexForCellAtMaxRes = minValidLocalIdx;
            cellUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne = sizeOfValidIndices + minValidLocalIdx;

            return m_Index.AssignIndexChunksToCell(cell, brickCountsAtResolution, ref cellUpdateInfo);
        }

        void LoadPendingCells(bool loadAll = false)
        {
            int count = Mathf.Min(m_NumberOfCellsLoadedPerFrame, m_CellsToBeLoaded.Count);
            count = loadAll ? m_CellsToBeLoaded.Count : count;

            // This should never happen, *unless* an asset was baked with previous version of index buffer.
            if (m_PendingInitInfo.pendingMinCellPosition == m_PendingInitInfo.pendingMaxCellPosition && count > 1)
                return;

            if (count != 0)
                ClearDebugData();

            for (int i = 0; i < count; ++i)
            {
                // Pop from queue.
                var sortInfo = m_CellsToBeLoaded[0];
                var cell = sortInfo.cell;
                var path = sortInfo.sourceAsset;

                bool compressed = false;
                int allocatedBytes = 0;
                var dataLocation = ProbeBrickPool.CreateDataLocation(cell.sh.Length, compressed, m_SHBands, out allocatedBytes);
                ProbeBrickPool.FillDataLocation(ref dataLocation, cell.sh, m_SHBands);

                cell.flatIdxInCellIndices = m_CellIndices.GetFlatIdxForCell(cell.position);

                if (GetCellIndexUpdate(cell, out var cellUpdateInfo))
                {
                    List<ProbeBrickIndex.Brick> brickList = new List<ProbeBrickIndex.Brick>();
                    brickList.AddRange(cell.bricks);
                    List<Chunk> chunkList = new List<Chunk>();

                    var regId = AddBricks(brickList, dataLocation, cellUpdateInfo, out chunkList);
                    m_BricksToCellUpdateInfo.Add(regId, cellUpdateInfo);

                    m_CellIndices.AddCell(cell.flatIdxInCellIndices, cellUpdateInfo);

                    AddCell(cell, chunkList);
                    m_CellToBricks[cell] = regId;

                    dataLocation.Cleanup();
                    m_CellsToBeLoaded.RemoveAt(0);
                }
                else
                {
                    // We need to first remove something to fit, can't load things further.
                    return;
                }
            }
        }

        /// <summary>
        /// Perform all the operations that are relative to changing the content or characteristics of the probe reference volume.
        /// </summary>
        /// <param name ="loadAllCells"> True when all cells are to be immediately loaded..</param>
        public void PerformPendingOperations(bool loadAllCells = false)
        {
            PerformPendingDeletion();
            PerformPendingIndexChangeAndInit();
            PerformPendingLoading();
            LoadPendingCells(loadAllCells);
        }

        /// <summary>
        /// Initialize the reference volume.
        /// </summary>
        /// <param name ="allocationSize"> Size used for the chunk allocator that handles bricks.</param>
        /// <param name ="memoryBudget">Probe reference volume memory budget.</param>
        /// <param name ="shBands">Probe reference volume SH bands.</param>
        void InitProbeReferenceVolume(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands)
        {
            var minCellPosition = m_PendingInitInfo.pendingMinCellPosition;
            var maxCellPosition = m_PendingInitInfo.pendingMaxCellPosition;
            if (!m_ProbeReferenceVolumeInit)
            {
                Profiler.BeginSample("Initialize Reference Volume");
                m_Pool = new ProbeBrickPool(allocationSize, memoryBudget, shBands);

                m_Index = new ProbeBrickIndex(memoryBudget);
                m_CellIndices = new ProbeCellIndices(minCellPosition, maxCellPosition, (int)Mathf.Pow(3, m_MaxSubdivision - 1));

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

        /// <summary>
        /// Perform sorting of pending cells to be loaded.
        /// </summary>
        /// <param name ="cameraPosition"> The position to sort against (closer to the position will be loaded first).</param>
        public void SortPendingCells(Vector3 cameraPosition)
        {
            if (m_CellsToBeLoaded.Count > 0)
            {
                for (int i = 0; i < m_CellsToBeLoaded.Count; ++i)
                {
                    m_CellsToBeLoaded[i].distanceToCamera = Vector3.Distance(cameraPosition, m_CellsToBeLoaded[i].position);
                }

                m_CellsToBeLoaded.Sort();
            }
        }

        ProbeReferenceVolume()
        {
            m_Transform.posWS = Vector3.zero;
            m_Transform.rot = Quaternion.identity;
            m_Transform.scale = 1f;
            m_Transform.refSpaceToWS = Matrix4x4.identity;
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
            m_Transform.refSpaceToWS = Matrix4x4.TRS(m_Transform.posWS, m_Transform.rot, Vector3.one * m_Transform.scale);
        }

        internal void SetMaxSubdivision(int maxSubdivision) => m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);
        internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);
        internal float BrickSize(int subdivisionLevel) => m_Transform.scale * CellSize(subdivisionLevel);
        internal float MinBrickSize() => m_Transform.scale;
        internal float MaxBrickSize() => BrickSize(m_MaxSubdivision - 1);
        internal Matrix4x4 GetRefSpaceToWS() => m_Transform.refSpaceToWS;
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
                m_Pool.Clear();
                m_Index.Clear();
                cells.Clear();
                m_ChunkInfo.Clear();
            }

            if (clearAssetsOnVolumeClear)
            {
                m_PendingAssetsToBeLoaded.Clear();
                m_ActiveAssets.Clear();
            }
        }

        // Runtime API starts here
        RegId AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc, ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo, out List<Chunk> ch_list)
        {
            Profiler.BeginSample("AddBricks");

            // calculate the number of chunks necessary
            int ch_size = m_Pool.GetChunkSize();
            ch_list = new List<Chunk>((bricks.Count + ch_size - 1) / ch_size);
            m_Pool.Allocate(ch_list.Capacity, ch_list);

            // copy chunks into pool
            m_TmpSrcChunks.Clear();
            m_TmpSrcChunks.Capacity = ch_list.Count;
            Chunk c;
            c.x = 0;
            c.y = 0;
            c.z = 0;

            // currently this code assumes that the texture width is a multiple of the allocation chunk size
            for (int i = 0; i < ch_list.Count; i++)
            {
                m_TmpSrcChunks.Add(c);
                c.x += ch_size * ProbeBrickPool.kBrickProbeCountPerDim;
                if (c.x >= dataloc.width)
                {
                    c.x = 0;
                    c.y += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (c.y >= dataloc.height)
                    {
                        c.y = 0;
                        c.z += ProbeBrickPool.kBrickProbeCountPerDim;
                    }
                }
            }

            // Update the pool and index and ignore any potential frame latency related issues for now
            m_Pool.Update(dataloc, m_TmpSrcChunks, ch_list, m_SHBands);

            m_BricksLoaded = true;

            // create a registry entry for this request
            RegId id;
            m_ID++;
            id.id = m_ID;
            m_Registry.Add(id, ch_list);

            // Build index
            m_Index.AddBricks(id, bricks, ch_list, m_Pool.GetChunkSize(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight(), cellUpdateInfo);

            Profiler.EndSample();

            return id;
        }

        void ReleaseBricks(RegId id)
        {
            List<Chunk> ch_list;
            if (!m_Registry.TryGetValue(id, out ch_list))
            {
                Debug.Log("Tried to release bricks with id=" + id.id + " but no bricks were registered under this id.");
                return;
            }

            // clean up the index
            m_Index.RemoveBricks(id, m_BricksToCellUpdateInfo[id]);

            // clean up the pool
            m_Pool.Deallocate(ch_list);
            m_Registry.Remove(id);
            m_BricksToCellUpdateInfo.Remove(id);
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

            ShaderVariablesProbeVolumes shaderVars;
            shaderVars._NormalBias = normalBias;
            shaderVars._PoolDim = m_Pool.GetPoolDimensions();
            shaderVars._ViewBias = viewBias;
            shaderVars._PVSamplingNoise = parameters.samplingNoise;
            shaderVars._CellInMinBricks = (int)Mathf.Pow(3, m_MaxSubdivision - 1);
            shaderVars._CellIndicesDim = m_CellIndices.GetCellIndexDimension();
            shaderVars._MinCellPosition = m_CellIndices.GetCellMinPosition();
            shaderVars._MinBrickSize = MinBrickSize();
            shaderVars._IndexChunkSize = ProbeBrickIndex.kIndexChunkSize;
            shaderVars._CellInMeters = MaxBrickSize();

            ConstantBuffer.PushGlobal(cmd, shaderVars, m_CBShaderID);
        }

        /// <summary>
        /// Cleanup loaded data.
        /// </summary>
        void CleanupLoadedData()
        {
            m_BricksLoaded = false;

            if (m_ProbeReferenceVolumeInit)
            {
                m_Index.Cleanup();
                m_CellIndices.Cleanup();
                m_Pool.Cleanup();
            }

            m_ProbeReferenceVolumeInit = false;
            ClearDebugData();
        }
    }
}
