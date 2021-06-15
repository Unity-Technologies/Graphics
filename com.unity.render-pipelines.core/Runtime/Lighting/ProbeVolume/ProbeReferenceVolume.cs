using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
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
        /// The debug mesh used to draw probes in the debug view.
        /// </summary>
        public Mesh probeDebugMesh;
        /// <summary>
        /// The shader used to visualize the probes in the debug view.
        /// </summary>
        public Shader probeDebugShader;

        public ProbeVolumeSceneBounds sceneBounds;
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
            public static bool operator==(RegId lhs, RegId rhs) => lhs.id == rhs.id;
            public static bool operator!=(RegId lhs, RegId rhs) => lhs.id != rhs.id;
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

        bool                            m_IsInitialized = false;
        int                             m_ID = 0;
        RefVolTransform                 m_Transform;
        int                             m_MaxSubdivision;
        ProbeBrickPool                  m_Pool;
        ProbeBrickIndex                 m_Index;
        List<Chunk>                     m_TmpSrcChunks = new List<Chunk>();
        float[]                         m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        Dictionary<RegId, List<Chunk>>  m_Registry = new Dictionary<RegId, List<Chunk>>();

        internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();
        Dictionary<int, CellChunkInfo> m_ChunkInfo = new Dictionary<int, CellChunkInfo>();

        internal ProbeVolumeSceneBounds sceneBounds;


        bool m_BricksLoaded = false;
        Dictionary<string, List<RegId>> m_AssetPathToBricks = new Dictionary<string, List<RegId>>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeLoaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information on probes we need to remove.
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeUnloaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_ActiveAssets = new Dictionary<string, ProbeVolumeAsset>();

        // List of info for cells that are yet to be loaded.
        private List<CellSortInfo> m_CellsToBeLoaded = new List<CellSortInfo>();

        bool m_NeedLoadAsset = false;
        bool m_ProbeReferenceVolumeInit = false;
        internal bool isInitialized => m_ProbeReferenceVolumeInit;

        // Similarly the index dimensions come from the authoring component; if a change happens
        // a pending request for re-init (and what it implies) is added from the editor.
        Vector3Int m_PendingIndexDimChange;
        bool m_NeedsIndexDimChange = false;
        bool m_HasChangedIndexDim = false;

        int m_CBShaderID = Shader.PropertyToID("ShaderVariablesProbeVolumes");

#if UNITY_EDITOR
        // By default on editor we load a lot of cells in one go to avoid having to mess with scene view
        // to see results, this value can still be changed via API.
        private int m_NumberOfCellsLoadedPerFrame = 10000;
#else
        private int m_NumberOfCellsLoadedPerFrame = 2;
#endif

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;

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
        /// <param name="parameters">Initialization parameters.</param>
        public void Initialize(in ProbeVolumeSystemParameters parameters)
        {
            if (m_IsInitialized)
            {
                Debug.LogError("Probe Volume System has already been initialized.");
                return;
            }

            m_MemoryBudget = parameters.memoryBudget;
            InitializeDebug(parameters.probeDebugMesh, parameters.probeDebugShader);
            InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_PendingIndexDimChange);
            m_IsInitialized = true;
            sceneBounds = parameters.sceneBounds;
#if UNITY_EDITOR
            if (sceneBounds != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += sceneBounds.UpdateSceneBounds;
            }
#endif
        }

        /// Cleanup the Probe Volume system.
        /// </summary>
        public void Cleanup()
        {
            if (!m_IsInitialized)
            {
                Debug.LogError("Probe Volume System has not been initialized first before calling cleanup.");
                return;
            }

            CleanupLoadedData();
            CleanupDebug();
            m_IsInitialized = false;
        }

        void RemoveCell(int cellIndex)
        {
            if (cells.ContainsKey(cellIndex))
                cells.Remove(cellIndex);

            if (m_ChunkInfo.ContainsKey(cellIndex))
                m_ChunkInfo.Remove(cellIndex);
        }

        void AddCell(Cell cell, List<Chunk> chunks)
        {
            cells[cell.index] = cell;

            var cellChunks = new CellChunkInfo();
            cellChunks.chunks = chunks;
            m_ChunkInfo[cell.index] = cellChunks;
        }

        internal void AddPendingAssetLoading(ProbeVolumeAsset asset)
        {
            var key = asset.GetSerializedFullPath();
            if (m_PendingAssetsToBeLoaded.ContainsKey(key))
            {
                m_PendingAssetsToBeLoaded.Remove(key);
            }
            m_PendingAssetsToBeLoaded.Add(asset.GetSerializedFullPath(), asset);
            m_NeedLoadAsset = true;

            // Compute the max index dimension from all the loaded assets + assets we need to load
            Vector3Int indexDimension = Vector3Int.zero;
            foreach (var a in m_PendingAssetsToBeLoaded.Values)
                indexDimension = Vector3Int.Max(indexDimension, a.maxCellIndex);
            foreach (var a in m_ActiveAssets.Values)
                indexDimension = Vector3Int.Max(indexDimension, a.maxCellIndex);

            m_PendingIndexDimChange = indexDimension;
            m_NeedsIndexDimChange = m_Index == null || (m_Index != null && indexDimension != m_Index.GetIndexDimension());
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
                RemoveCell(cell.index);
            }

            // Unload brick data
            if (m_AssetPathToBricks.ContainsKey(key))
            {
                var regIds = m_AssetPathToBricks[key];
                foreach (var regId in regIds)
                    ReleaseBricks(regId);

                m_AssetPathToBricks.Remove(key);
            }

            ClearDebugData();
        }

        void PerformPendingIndexDimensionChangeAndInit()
        {
            if (m_NeedsIndexDimChange)
            {
                CleanupLoadedData();
                InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_PendingIndexDimChange);
                m_HasChangedIndexDim = true;
                m_NeedsIndexDimChange = false;
            }
            else
            {
                m_HasChangedIndexDim = false;
            }
        }

        void LoadAsset(ProbeVolumeAsset asset)
        {
            var path = asset.GetSerializedFullPath();
            m_AssetPathToBricks[path] = new List<RegId>();


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
            if (m_HasChangedIndexDim)
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

        void LoadPendingCells(bool loadAll = false)
        {
            int count = Mathf.Min(m_NumberOfCellsLoadedPerFrame, m_CellsToBeLoaded.Count);
            count = loadAll ? m_CellsToBeLoaded.Count : count;

            if (count != 0)
                ClearDebugData();

            for (int i = 0; i < count; ++i)
            {
                // Pop from queue.
                var sortInfo = m_CellsToBeLoaded[0];
                var cell = sortInfo.cell;
                var path = sortInfo.sourceAsset;

                bool compressed = false;
                var dataLocation = ProbeBrickPool.CreateDataLocation(cell.sh.Length, compressed, ProbeVolumeSHBands.SphericalHarmonicsL2);
                ProbeBrickPool.FillDataLocation(ref dataLocation, cell.sh, ProbeVolumeSHBands.SphericalHarmonicsL2);

                // TODO register ID of brick list
                List<ProbeBrickIndex.Brick> brickList = new List<ProbeBrickIndex.Brick>();
                brickList.AddRange(cell.bricks);
                List<Chunk> chunkList = new List<Chunk>();
                var regId = AddBricks(brickList, dataLocation, out chunkList);

                AddCell(cell, chunkList);
                m_AssetPathToBricks[path].Add(regId);

                dataLocation.Cleanup();
                m_CellsToBeLoaded.RemoveAt(0);
            }
        }

        /// <summary>
        /// Perform all the operations that are relative to changing the content or characteristics of the probe reference volume.
        /// </summary>
        /// <param name ="loadAllCells"> True when all cells are to be immediately loaded..</param>
        public void PerformPendingOperations(bool loadAllCells = false)
        {
            PerformPendingDeletion();
            PerformPendingIndexDimensionChangeAndInit();
            PerformPendingLoading();
            LoadPendingCells(loadAllCells);
        }

        /// <summary>
        /// Initialize the reference volume.
        /// </summary>
        /// <param name ="allocationSize"> Size used for the chunk allocator that handles bricks.</param>
        /// <param name ="memoryBudget">Probe reference volume memory budget.</param>
        /// <param name ="indexDimensions">Dimensions of the index data structure.</param>
        void InitProbeReferenceVolume(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget, Vector3Int indexDimensions)
        {
            if (!m_ProbeReferenceVolumeInit)
            {
                int indexSize = 0;
                try
                {
                    indexSize = checked(indexDimensions.x * indexDimensions.y * indexDimensions.z);
                }
                catch
                {
                    Debug.LogError($"Index Dimension too big: {indexDimensions}. Please reduce the area covered by the probe volumes.");
                    return;
                }
                Profiler.BeginSample("Initialize Reference Volume");
                m_Pool = new ProbeBrickPool(allocationSize, memoryBudget);
                if (indexSize == 0)
                {
                    // Give a momentarily dummy size to allow the system to function with no asset assigned.
                    indexDimensions = new Vector3Int(1, 1, 1);
                }
                m_Index = new ProbeBrickIndex(indexDimensions);

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
                m_NeedsIndexDimChange = true;
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
        RegId AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc, out List<Chunk> ch_list)
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
            m_Pool.Update(dataloc, m_TmpSrcChunks, ch_list, ProbeVolumeSHBands.SphericalHarmonicsL2);

            m_BricksLoaded = true;

            // create a registry entry for this request
            RegId id;
            m_ID++;
            id.id = m_ID;
            m_Registry.Add(id, ch_list);

            // Build index
            m_Index.AddBricks(id, bricks, ch_list, m_Pool.GetChunkSize(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());

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
            m_Index.RemoveBricks(id);

            // clean up the pool
            m_Pool.Deallocate(ch_list);
            m_Registry.Remove(id);
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
            shaderVars._WStoRS = Matrix4x4.Inverse(m_Transform.refSpaceToWS);
            shaderVars._IndexDim = m_Index.GetIndexDimension();
            shaderVars._NormalBias = normalBias;
            shaderVars._PoolDim = m_Pool.GetPoolDimensions();
            shaderVars._ViewBias = viewBias;
            shaderVars._PVSamplingNoise = parameters.samplingNoise;
            shaderVars.pad0 = Vector2.zero;

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
                m_Pool.Cleanup();
            }

            m_ProbeReferenceVolumeInit = false;
            ClearDebugData();
        }
    }
}
