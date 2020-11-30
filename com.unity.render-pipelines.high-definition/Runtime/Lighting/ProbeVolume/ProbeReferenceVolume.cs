using System.Collections.Generic;
using UnityEngine.Profiling;
using Chunk = UnityEngine.Rendering.HighDefinition.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Rendering.HighDefinition.ProbeBrickIndex.Brick;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeReferenceVolume
    {
        [System.Serializable]
        public struct Cell
        {
            public int index;
            public Vector3Int position;
            public List<Brick> bricks;
            public Vector3[] probePositions;
            public SphericalHarmonicsL1[] sh;
            public float[] validity;
        }

        public struct Volume
        {
            public Vector3 Corner;
            public Vector3 X;   // the vectors are NOT normalized, their length determines the size of the box
            public Vector3 Y;
            public Vector3 Z;

            public Volume(Matrix4x4 trs)
            {
                X = trs.GetColumn(0);
                Y = trs.GetColumn(1);
                Z = trs.GetColumn(2);
                Corner = (Vector3)trs.GetColumn(3) - X * 0.5f - Y * 0.5f - Z * 0.5f;
            }

            public Volume(Volume copy)
            {
                X = copy.X;
                Y = copy.Y;
                Z = copy.Z;
                Corner = copy.Corner;
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

                            Vector3 pt = Corner
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

            public void Transform(Matrix4x4 trs)
            {
                Corner = trs.MultiplyPoint(Corner);
                X = trs.MultiplyVector(X);
                Y = trs.MultiplyVector(Y);
                Z = trs.MultiplyVector(Z);
            }

            public override string ToString()
            {
                return $"Corner: {Corner}, X: {X}, Y: {Y}, Z: {Z}";
            }
        }

        public struct BrickFlags
        {
            uint flags;

            public bool discard { get { return (flags & 1) != 0; } set { flags = (flags & (~1u)) | (value ? 1u : 0); } }
            public bool subdivide { get { return (flags & 2) != 0; } set { flags = (flags & (~2u)) | (value ? 2u : 0); } }
        }

        public struct RefVolTransform
        {
            public Matrix4x4 refSpaceToWS;
            public Vector3 posWS;
            public Quaternion rot;
            public float scale;
        }

        public struct RuntimeResources
        {
            public ComputeBuffer index;
            public Texture3D L0;
            public Texture3D L1_R;
            public Texture3D L1_G;
            public Texture3D L1_B;
        }

        public struct RegId
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

        private int m_id = 0;
        private RefVolTransform m_Transform;
        private float m_NormalBias;
        private int m_MaxSubdivision;
        private ProbeBrickPool m_Pool;
        private ProbeBrickIndex m_Index;
        private List<Brick>[] m_TmpBricks = new List<Brick>[2];
        private List<BrickFlags> m_TmpFlags = new List<BrickFlags>();
        private List<Chunk> m_TmpSrcChunks = new List<Chunk>();
        private List<Chunk> m_TmpDstChunks = new List<Chunk>();
        private float[] m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
        private Dictionary<RegId, List<Chunk>> m_Registry = new Dictionary<RegId, List<Chunk>>();

        public List<Cell> Cells = new List<Cell>();

        // index related
        Texture3D indexTex;

        static private ProbeReferenceVolume _instance = null;

        public static ProbeReferenceVolume instance
        {
            get
            {
                // TODO: Make this editable. 
                // Reinit upon changes
                // 
                if (_instance == null)
                {
                    // TODO: Allow resizing
                    _instance = new ProbeReferenceVolume(64, 1024 * 1024 * 1024, new Vector3Int(1024, 64, 1024));
                }
                return _instance;
            }
        }

        private ProbeReferenceVolume(int allocationSize, int memoryBudget, Vector3Int indexDimensions)
        {
            Profiler.BeginSample("Create Reference volume");
            m_Transform.posWS = Vector3.zero;
            m_Transform.rot = Quaternion.identity;
            m_Transform.scale = 1f;
            m_Transform.refSpaceToWS = Matrix4x4.identity;

            m_NormalBias = 0f;

            m_Pool = new ProbeBrickPool(allocationSize, memoryBudget);
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
        }

        public RuntimeResources GetRuntimeResources()
        {
            RuntimeResources rr = new RuntimeResources();
            m_Index.GetRuntimeResources(ref rr);
            m_Pool.GetRuntimeResources(ref rr);
            return rr;
        }

        public void SetTRS(Vector3 position, Quaternion rotation, float minBrickSize)
        {
            m_Transform.posWS = position;
            m_Transform.rot = rotation;
            m_Transform.scale = minBrickSize;
            m_Transform.refSpaceToWS = Matrix4x4.TRS(m_Transform.posWS, m_Transform.rot, Vector3.one * m_Transform.scale);
        }

        public void SetMaxSubdivision(int maxSubdivision) { m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels); }
        public void SetNormalBias(float normalBias) { m_NormalBias = normalBias; }

        internal static int cellSize(int subdivisionLevel) { return (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel); }
        internal float brickSize(int subdivisionLevel) { return m_Transform.scale * cellSize(subdivisionLevel); }
        internal float minBrickSize() { return m_Transform.scale; }
        internal float maxBrickSize() { return brickSize(m_MaxSubdivision); }
        public Matrix4x4 GetRefSpaceToWS() { return m_Transform.refSpaceToWS; }
        public RefVolTransform GetTransform() { return m_Transform; }

        public delegate void SubdivisionDel(RefVolTransform refSpaceToWS, List<Brick> inBricks, List<BrickFlags> outControlFlags);

        public void Clear()
        {
            m_Pool.Clear();
            m_Index.Clear();
            Cells.Clear();
        }

#if UNITY_EDITOR
        public void CreateBricks(List<Volume> volumes, SubdivisionDel subdivider, List<Brick> outSortedBricks, out int positionArraySize)
        {
            Profiler.BeginSample("CreateBricks");
            // generate bricks for all areas covered by the passed in volumes, potentially subdividing them based on the subdivider's decisions
            foreach (var v in volumes)
            {
                ConvertVolume(v, subdivider, outSortedBricks);
            }

            Profiler.BeginSample("sort");
            // sort from larger to smaller bricks
            outSortedBricks.Sort((Brick lhs, Brick rhs) =>
            {
                if (lhs.size != rhs.size)
                    return lhs.size > rhs.size ? -1 : 1;
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
                if (brick.size == 0)
                    continue;

                Brick b = new Brick();
                b.size = brick.size - 1;
                int offset = cellSize(b.size);

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
        private void ConvertVolume(Volume volume, SubdivisionDel subdivider, List<Brick> outSortedBricks)
        {
            Profiler.BeginSample("ConvertVolume");
            m_TmpBricks[0].Clear();
            Transform(volume, out Volume vol);
            // rasterize bricks according to the coarsest grid
            Rasterize(vol, m_TmpBricks[0]);

            // iterative subdivision
            while (m_TmpBricks[0].Count > 0)
            {
                m_TmpBricks[1].Clear();
                m_TmpFlags.Clear();
                m_TmpFlags.Capacity = Mathf.Max(m_TmpFlags.Capacity, m_TmpBricks[0].Count);

                Profiler.BeginSample("Subdivider");
                subdivider(m_Transform, m_TmpBricks[0], m_TmpFlags);
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
                    Debug.Log("Calling SubdivideBricks with " + m_TmpBricks[1].Count + " bricks.");
                    SubdivideBricks(m_TmpBricks[1], m_TmpBricks[0]);

                    // Cull out of bounds bricks
                    Profiler.BeginSample("Cull bricks");
                    for (int i = m_TmpBricks[0].Count - 1; i >= 0; i--)
                    {
                        if (!ProbeVolumePositioning.OBBIntersect(ref m_Transform, m_TmpBricks[0][i], ref volume))
                        {
                            m_TmpBricks[0].RemoveAt(i);
                        }
                    }
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
        }
#endif

        // Converts brick information into positional data at kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim resolution
        public void ConvertBricks(List<Brick> bricks, Vector3[] outProbePositions)
        {
            Profiler.BeginSample("ConvertBricks");
            Matrix4x4 m = GetRefSpaceToWS();
            int posIdx = 0;

            foreach (var b in bricks)
            {
                Vector3 offset = b.position;
                offset = m.MultiplyPoint(offset);
                float scale = cellSize(b.size);
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
        public RegId AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc)
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

            // We need to make sure that textures are allocated if they were not already.
            m_Pool.EnsureTextureValidity();
            // Update the pool and index and ignore any potential frame latency related issues for now
            m_Pool.Update(dataloc, m_TmpSrcChunks, ch_list);

            // create a registry entry for this request
            RegId id;
            m_id++;
            id.id = m_id;
            m_Registry.Add(id, ch_list);

            // update the index
            m_Index.AddBricks(id, bricks, ch_list, m_Pool.GetChunkSize(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());
            m_Index.WriteConstants(ref m_Transform, m_Pool.GetPoolDimensions(), m_NormalBias);

            Profiler.EndSample();

            return id;
        }

        public void ReleaseBricks(RegId id)
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

        private void Transform(Volume inVolume, out Volume outVolume)
        {
            Matrix4x4 m = GetRefSpaceToWS().inverse;

            // Handle TRS
            outVolume.Corner = m.MultiplyPoint(inVolume.Corner);
            outVolume.X = m.MultiplyVector(inVolume.X);
            outVolume.Y = m.MultiplyVector(inVolume.Y);
            outVolume.Z = m.MultiplyVector(inVolume.Z);
        }

        // Creates bricks at the coarsest level for all areas that are overlapped by the pass in volume
        private void Rasterize(Volume volume, List<Brick> outBricks)
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
                        Debug.Assert(pos.x >= 0 && pos.y >= 0 && pos.z >= 0);
                        outBricks.Add(new Brick(pos, brickSubDivLevel));
                    }
                }
            }
            Profiler.EndSample();
        }

        internal void Cleanup()
        {
            m_Index.Cleanup();
            m_Pool.Cleanup();
        }
    }
}
