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
        public ProbeVolumeTextureMemoryBudget memoryBudget;
        public Mesh probeDebugMesh;
        public Shader probeDebugShader;
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
        internal struct Cell
        {
            public int index;
            public Vector3Int position;
            public List<Brick> bricks;
            public Vector3[] probePositions;
            public SphericalHarmonicsL2[] sh;
            public float[] validity;
        }

        internal struct Volume
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
        }

        internal struct BrickFlags
        {
            uint flags;

            public bool discard { get { return (flags & 1) != 0; } set { flags = (flags & (~1u)) | (value ? 1u : 0); } }
            public bool subdivide { get { return (flags & 2) != 0; } set { flags = (flags & (~2u)) | (value ? 2u : 0); } }
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
        List<Brick>[]                   m_TmpBricks = new List<Brick>[2];
        List<BrickFlags>                m_TmpFlags = new List<BrickFlags>();
        List<Chunk>                     m_TmpSrcChunks = new List<Chunk>();
        List<Chunk>                     m_TmpDstChunks = new List<Chunk>();
        float[]                         m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        Dictionary<RegId, List<Chunk>>  m_Registry = new Dictionary<RegId, List<Chunk>>();

        internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();

        bool m_BricksLoaded = false;
        Dictionary<string, List<RegId>> m_AssetPathToBricks = new Dictionary<string, List<RegId>>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeLoaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information on probes we need to remove.
        Dictionary<string, ProbeVolumeAsset> m_PendingAssetsToBeUnloaded = new Dictionary<string, ProbeVolumeAsset>();
        // Information of the probe volume asset that is being loaded (if one is pending)
        Dictionary<string, ProbeVolumeAsset> m_ActiveAssets = new Dictionary<string, ProbeVolumeAsset>();

        bool m_NeedLoadAsset = false;
        bool m_ProbeReferenceVolumeInit = false;
        // Similarly the index dimensions come from the authoring component; if a change happens
        // a pending request for re-init (and what it implies) is added from the editor.
        Vector3Int m_PendingIndexDimChange;
        bool m_NeedsIndexDimChange = false;

        private int m_CBShaderID = Shader.PropertyToID("ShaderVariablesProbeVolumes");

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;

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
            InitializeDebug(parameters.probeDebugMesh, parameters.probeDebugShader);
            InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_PendingIndexDimChange);
            m_IsInitialized = true;
        }

        /// <summary>
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
            m_NeedsIndexDimChange = true;
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
                if (cells.ContainsKey(cell.index))
                    cells.Remove(cell.index);
            }

            // Unload brick data
            if (m_AssetPathToBricks.ContainsKey(key))
            {
                var regIds = m_AssetPathToBricks[key];
                foreach (var regId in regIds)
                    ReleaseBricks(regId);

                m_AssetPathToBricks.Remove(key);
            }
        }

        void PerformPendingIndexDimensionChangeAndInit()
        {
            if (m_NeedsIndexDimChange)
            {
                CleanupLoadedData();
                InitProbeReferenceVolume(kProbeIndexPoolAllocationSize, m_MemoryBudget, m_PendingIndexDimChange);
                m_NeedsIndexDimChange = false;
            }
        }

        void LoadAsset(ProbeVolumeAsset asset)
        {
            var path = asset.GetSerializedFullPath();
            m_AssetPathToBricks[path] = new List<RegId>();

            foreach (var cell in asset.cells)
            {
                // Push data to HDRP
                bool compressed = false;
                var dataLocation = ProbeBrickPool.CreateDataLocation(cell.sh.Length, compressed, ProbeVolumeSHBands.SphericalHarmonicsL2);
                ProbeBrickPool.FillDataLocation(ref dataLocation, cell.sh, ProbeVolumeSHBands.SphericalHarmonicsL2);

                // TODO register ID of brick list
                List<ProbeBrickIndex.Brick> brickList = new List<ProbeBrickIndex.Brick>();
                brickList.AddRange(cell.bricks);
                var regId = AddBricks(brickList, dataLocation);

                cells[cell.index] = cell;
                m_AssetPathToBricks[path].Add(regId);

                dataLocation.Cleanup();
            }
        }

        void PerformPendingLoading()
        {
            if ((m_PendingAssetsToBeLoaded.Count == 0 && m_ActiveAssets.Count == 0) || !m_NeedLoadAsset || !m_ProbeReferenceVolumeInit)
                return;

            m_Pool.EnsureTextureValidity();

            // Load the ones that are already active but reload if we said we need to load
            foreach (var asset in m_ActiveAssets.Values)
            {
                LoadAsset(asset);
            }

            foreach (var asset in m_PendingAssetsToBeLoaded.Values)
            {
                LoadAsset(asset);
                if (!m_ActiveAssets.ContainsKey(asset.GetSerializedFullPath()))
                {
                    m_ActiveAssets.Add(asset.GetSerializedFullPath(), asset);
                }
            }

            // Mark the loading as done.
            m_NeedLoadAsset = false;

            m_PendingAssetsToBeLoaded.Clear();
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

        /// <summary>
        /// Perform all the operations that are relative to changing the content or characteristics of the probe reference volume.
        /// </summary>
        public void PerformPendingOperations()
        {
            PerformPendingDeletion();
            PerformPendingIndexDimensionChangeAndInit();
            PerformPendingLoading();
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
                Profiler.BeginSample("Initialize Reference Volume");
                m_Pool = new ProbeBrickPool(allocationSize, memoryBudget);
                if ((indexDimensions.x * (indexDimensions.y + 1) * indexDimensions.z) == 0)
                {
                    // Give a momentarily dummy size to allow the system to function with no asset assigned.
                    indexDimensions = new Vector3Int(1, 1, 1);
                }
                m_Index = new ProbeBrickIndex(indexDimensions);

                m_TmpBricks[0] = new List<Brick>();
                m_TmpBricks[1] = new List<Brick>();
                m_TmpBricks[0].Capacity = m_TmpBricks[1].Capacity = 1024;

                // initialize offsets
                m_PositionOffsets[0] = 0.0f;
                float probeDelta = 1.0f / ProbeBrickPool.kBrickCellCount;
                for (int i = 1; i < ProbeBrickPool.kBrickProbeCountPerDim - 1; i++)
                    m_PositionOffsets[i] = i * probeDelta;
                m_PositionOffsets[m_PositionOffsets.Length - 1] = 1.0f;
                Profiler.EndSample();

                m_ProbeReferenceVolumeInit = true;

                ClearDebugData();
            }
            m_NeedLoadAsset = true;
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

        internal void SetMaxSubdivision(int maxSubdivision) { m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels); }
        internal static int CellSize(int subdivisionLevel) { return (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel); }
        internal float BrickSize(int subdivisionLevel) { return m_Transform.scale * CellSize(subdivisionLevel); }
        internal float MinBrickSize() { return m_Transform.scale; }
        internal float MaxBrickSize() { return BrickSize(m_MaxSubdivision); }
        internal Matrix4x4 GetRefSpaceToWS() { return m_Transform.refSpaceToWS; }
        internal RefVolTransform GetTransform() { return m_Transform; }
        internal int GetMaxSubdivision() => m_MaxSubdivision;
        internal int GetMaxSubdivision(float multiplier) => Mathf.CeilToInt(m_MaxSubdivision * multiplier);

        /// <summary>
        /// Returns whether any brick data has been loaded.
        /// </summary>
        /// <returns></returns>
        public bool DataHasBeenLoaded() { return m_BricksLoaded; }

        internal delegate void SubdivisionDel(RefVolTransform refSpaceToWS, int subdivisionLevel, List<Brick> inBricks, List<BrickFlags> outControlFlags);

        internal void Clear()
        {
            if (m_ProbeReferenceVolumeInit)
            {
                m_Pool.Clear();
                m_Index.Clear();
                cells.Clear();
            }
        }

#if UNITY_EDITOR
        internal void CreateBricks(List<Volume> cellVolumes, List<Volume> subVolumes, SubdivisionDel subdivider, List<Brick> outSortedBricks, out int positionArraySize)
        {
            Profiler.BeginSample("CreateBricks");
            // generate bricks for all areas covered by the passed in volumes, potentially subdividing them based on the subdivider's decisions
            foreach (var v in cellVolumes)
            {
                ConvertVolume(v, subVolumes, subdivider, outSortedBricks);
            }

            Profiler.BeginSample("sort");
            // sort from larger to smaller bricks
            outSortedBricks.Sort((Brick lhs, Brick rhs) =>
            {
                if (lhs.subdivisionLevel != rhs.subdivisionLevel)
                    return lhs.subdivisionLevel > rhs.subdivisionLevel ? -1 : 1;
                if (lhs.position.z != rhs.position.z)
                    return lhs.position.z < rhs.position.z ? -1 : 1;
                if (lhs.position.y != rhs.position.y)
                    return lhs.position.y < rhs.position.y ? -1 : 1;
                if (lhs.position.x != rhs.position.x)
                    return lhs.position.x < rhs.position.x ? -1 : 1;

                return 0;
            });
            Profiler.EndSample();
            // communicate the required array size for storing positions to the caller
            positionArraySize = outSortedBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;

            Profiler.EndSample();
        }

        // brick subdivision according to an octree kBrickCellCount * kBrickCellCount * kBrickCellCount scheme
        internal static void SubdivideBricks(List<Brick> inBricks, List<Brick> outSubdividedBricks)
        {
            Profiler.BeginSample("Subdivide");
            // reserve enough space
            outSubdividedBricks.Capacity = outSubdividedBricks.Count + inBricks.Count * ProbeBrickPool.kBrickCellCount * ProbeBrickPool.kBrickCellCount;

            foreach (var brick in inBricks)
            {
                if (brick.subdivisionLevel == 0)
                    continue;

                Brick b = new Brick();
                b.subdivisionLevel = brick.subdivisionLevel - 1;
                int offset = CellSize(b.subdivisionLevel);

                for (int z = 0; z < ProbeBrickPool.kBrickCellCount; z++)
                {
                    b.position.z = brick.position.z + z * offset;

                    for (int y = 0; y < ProbeBrickPool.kBrickCellCount; y++)
                    {
                        b.position.y = brick.position.y + y * offset;

                        for (int x = 0; x < ProbeBrickPool.kBrickCellCount; x++)
                        {
                            b.position.x = brick.position.x + x * offset;
                            outSubdividedBricks.Add(b);
                        }
                    }
                }
            }
            Profiler.EndSample();
        }

        // converts a volume into bricks, subdivides the bricks and culls subdivided volumes falling outside the original volume
        void ConvertVolume(Volume cellVolume, List<Volume> subVolumes, SubdivisionDel subdivider, List<Brick> outSortedBricks)
        {
            Profiler.BeginSample("ConvertVolume");
            m_TmpBricks[0].Clear();
            Transform(cellVolume, out Volume vol);
            // rasterize bricks according to the coarsest grid
            Rasterize(vol, m_TmpBricks[0]);

            int subDivCount = 0;

            // iterative subdivision
            while (m_TmpBricks[0].Count > 0 && subDivCount <= GetMaxSubdivision(cellVolume.maxSubdivisionMultiplier))
            {
                m_TmpBricks[1].Clear();
                m_TmpFlags.Clear();
                m_TmpFlags.Capacity = Mathf.Max(m_TmpFlags.Capacity, m_TmpBricks[0].Count);

                Profiler.BeginSample("Subdivider");
                subdivider(m_Transform, subDivCount, m_TmpBricks[0], m_TmpFlags);
                Profiler.EndSample();
                Debug.Assert(m_TmpBricks[0].Count == m_TmpFlags.Count);

                for (int i = 0; i < m_TmpFlags.Count; i++)
                {
                    if (!m_TmpFlags[i].discard)
                        outSortedBricks.Add(m_TmpBricks[0][i]);
                    if (m_TmpFlags[i].subdivide)
                        m_TmpBricks[1].Add(m_TmpBricks[0][i]);
                }

                m_TmpBricks[0].Clear();
                if (m_TmpBricks[1].Count > 0)
                {
                    SubdivideBricks(m_TmpBricks[1], m_TmpBricks[0]);

                    // Cull out of bounds bricks
                    Profiler.BeginSample("Cull bricks");
                    for (int i = m_TmpBricks[0].Count - 1; i >= 0; i--)
                    {
                        if (!ProbeVolumePositioning.OBBIntersect(ref m_Transform, m_TmpBricks[0][i], ref cellVolume))
                        {
                            m_TmpBricks[0].RemoveAt(i);
                        }
                    }
                    Profiler.EndSample();
                }

                subDivCount++;
            }
            Profiler.EndSample();
        }

#endif

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        internal void ConvertBricks(List<Brick> bricks, Vector3[] outProbePositions)
        {
            Profiler.BeginSample("ConvertBricks");
            Matrix4x4 m = GetRefSpaceToWS();
            int posIdx = 0;

            foreach (var b in bricks)
            {
                Vector3 offset = b.position;
                offset = m.MultiplyPoint(offset);
                float scale = CellSize(b.subdivisionLevel);
                Vector3 X = m.GetColumn(0) * scale;
                Vector3 Y = m.GetColumn(1) * scale;
                Vector3 Z = m.GetColumn(2) * scale;

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    float zoff = m_PositionOffsets[z];
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        float yoff = m_PositionOffsets[y];
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            float xoff = m_PositionOffsets[x];
                            outProbePositions[posIdx] = offset + xoff * X + yoff * Y + zoff * Z;
                            posIdx++;
                        }
                    }
                }
            }
            Profiler.EndSample();
        }

        // Runtime API starts here
        RegId AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc)
        {
            Profiler.BeginSample("AddBricks");

            // calculate the number of chunks necessary
            int ch_size = m_Pool.GetChunkSize();
            List<Chunk> ch_list = new List<Chunk>((bricks.Count + ch_size - 1) / ch_size);
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

            // update the index
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

        void Transform(Volume inVolume, out Volume outVolume)
        {
            Matrix4x4 m = GetRefSpaceToWS().inverse;

            // Handle TRS
            outVolume.corner = m.MultiplyPoint(inVolume.corner);
            outVolume.X = m.MultiplyVector(inVolume.X);
            outVolume.Y = m.MultiplyVector(inVolume.Y);
            outVolume.Z = m.MultiplyVector(inVolume.Z);
            outVolume.maxSubdivisionMultiplier = inVolume.maxSubdivisionMultiplier;
            outVolume.minSubdivisionMultiplier = inVolume.minSubdivisionMultiplier;
        }

        // Creates bricks at the coarsest level for all areas that are overlapped by the pass in volume
        void Rasterize(Volume volume, List<Brick> outBricks)
        {
            Profiler.BeginSample("Rasterize");
            // Calculate bounding box for volume in refvol space
            var AABB = volume.CalculateAABB();

            // Calculate smallest brick size capable of covering shortest AABB dimension
            float minVolumeSize = Mathf.Min(AABB.size.x, Mathf.Min(AABB.size.y, AABB.size.z));
            int brickSubDivLevel = Mathf.Min(Mathf.CeilToInt(Mathf.Log(minVolumeSize, 3)), m_MaxSubdivision);
            int brickTotalSize = (int)Mathf.Pow(3, brickSubDivLevel);

            // Extend AABB to have origin that lies on a grid point
            AABB.Encapsulate(new Vector3(
                brickTotalSize * Mathf.Floor(AABB.min.x / brickTotalSize),
                brickTotalSize * Mathf.Floor(AABB.min.y / brickTotalSize),
                brickTotalSize * Mathf.Floor(AABB.min.z / brickTotalSize)));

            // Calculate origin of bricks and how many are needed to cover volume
            Vector3Int origin = Vector3Int.FloorToInt(AABB.min);
            Vector3 logicalBrickRes = Vector3Int.CeilToInt(AABB.size / brickTotalSize);

            // Cover the volume with bricks
            for (int x = 0; x < logicalBrickRes.x; x++)
            {
                for (int y = 0; y < logicalBrickRes.y; y++)
                {
                    for (int z = 0; z < logicalBrickRes.z; z++)
                    {
                        Vector3Int pos = origin + new Vector3Int(x, y, z) * brickTotalSize;
                        outBricks.Add(new Brick(pos, brickSubDivLevel));
                    }
                }
            }
            Profiler.EndSample();
        }

        /// <summary>
        /// Update the constant buffer used by Probe Volumes in shaders.
        /// </summary>
        /// <param name="cmd">A command buffer used to perform the data update.</param>
        /// <param name="normalBias">Normal bias to apply to the position used to sample probe volumes.</param>
        public void UpdateConstantBuffer(CommandBuffer cmd, float normalBias)
        {
            ShaderVariablesProbeVolumes shaderVars;
            shaderVars._WStoRS = Matrix4x4.Inverse(m_Transform.refSpaceToWS);
            shaderVars._IndexDim = m_Index.GetIndexDimension();
            shaderVars._NormalBias = normalBias;
            shaderVars._PoolDim = m_Pool.GetPoolDimensions();
            shaderVars.pad0 = 0;

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
