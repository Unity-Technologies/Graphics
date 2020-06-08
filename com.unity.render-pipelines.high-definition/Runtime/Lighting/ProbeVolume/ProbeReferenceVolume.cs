using System.Collections.Generic;
using UnityEngine.Profiling;
using Chunk = UnityEngine.Rendering.HighDefinition.ProbeBrickPool.BrickChunkAlloc;
using Brick = UnityEngine.Rendering.HighDefinition.ProbeBrickIndex.Brick;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeReferenceVolume
    {
        internal struct Volume
        {
            public Vector3 Corner;
            public Vector3 X;   // the vectors are NOT normalized, their length determines the size of the box
            public Vector3 Y;
            public Vector3 Z;

            internal Volume(Matrix4x4 trs)
            {
                X = trs.GetColumn(0);
                Y = trs.GetColumn(1);
                Z = trs.GetColumn(2);
                Corner = (Vector3)trs.GetColumn(3) - X * 0.5f - Y * 0.5f - Z * 0.5f;
            }

            internal Bounds CalculateAABB()
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
        }

        internal struct BrickFlags
        {
            uint  flags;

            public bool discard { get { return (flags & 1) != 0; } set { flags = (flags & (~1u)) | (value ? 1u : 0); } }
            public bool subdivide { get { return (flags & 2) != 0; } set { flags = (flags & (~2u)) | (value ? 2u : 0); } }
        }

        internal struct RefVolTransform
        {
            public Matrix4x4   refSpaceToWS;
            public Vector3     posWS;
            public Quaternion  rot;
            public float       scale;
        }

        private RefVolTransform     m_Transform;
        private int                 m_MaxSubdivision;
        private ProbeBrickPool      m_Pool;
        private ProbeBrickIndex     m_Index;
        private List<Brick>[]       m_TmpBricks = new List<Brick>[2];
        private List<BrickFlags>    m_TmpFlags = new List<BrickFlags>();
        private List<Chunk>         m_TmpSrcChunks = new List<Chunk>();
        private List<Chunk>         m_TmpDstChunks = new List<Chunk>();
        private float[]             m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];

        // index related
        Texture3D indexTex;

        internal ProbeReferenceVolume(int allocationSize, int memoryBudget, Vector3Int indexDimensions)
        {
            Profiler.BeginSample("Create Reference volume");
            m_Transform.posWS = Vector3.zero;
            m_Transform.rot = Quaternion.identity;
            m_Transform.scale = 1.0f;
            m_Transform.refSpaceToWS = Matrix4x4.identity;

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
            m_PositionOffsets[m_PositionOffsets.Length-1] = 1.0f;
            Profiler.EndSample();
        }

        internal void SetGridDensity(float minBrickSize, int maxSubdivision)
        {
            m_MaxSubdivision = System.Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);

            m_Transform.scale = minBrickSize;
            m_Transform.refSpaceToWS = Matrix4x4.TRS(m_Transform.posWS, m_Transform.rot, Vector3.one * m_Transform.scale);
        }

        internal static int cellSize(int subdivisionLevel) { return (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel); }
        internal float brickSize( int subdivisionLevel) { return m_Transform.scale * cellSize(subdivisionLevel); }
        internal float minBrickSize() { return m_Transform.scale; }
        internal float maxBrickSize() { return brickSize(m_MaxSubdivision); }
        internal Matrix4x4 GetRefSpaceToWS() { return m_Transform.refSpaceToWS; }

        internal delegate void SubdivisionDel(RefVolTransform refSpaceToWS, List<Brick> inBricks, List<BrickFlags> outControlFlags);

        internal void CreateBricks(List<Volume> volumes, SubdivisionDel subdivider, List<Brick> outSortedBricks, out int positionArraySize)
        {
            Profiler.BeginSample("CreateBricks");
            // generate bricks for all areas covered by the passed in volumes, potentially subdividing them based on the subdivider's decisions
            foreach( var v in volumes)
            {
                ConvertVolume(v, subdivider, outSortedBricks);
            }

            Profiler.BeginSample("sort");
            // sort from larger to smaller bricks
            outSortedBricks.Sort( (Brick lhs, Brick rhs) =>
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
            Profiler.EndSample();
        }

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
        internal void AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc)
        {
            Profiler.BeginSample("AddBricks");
            m_TmpSrcChunks.Clear();
            m_TmpDstChunks.Clear();

            // calculate the number of chunks necessary
            int chunk_size = m_Pool.GetChunkSize();
            m_Pool.Allocate((bricks.Count + chunk_size - 1) / chunk_size, m_TmpDstChunks);

            // fill m_TmpSrcChunks
            m_TmpSrcChunks.Capacity = m_TmpDstChunks.Count;
            Chunk c;
            c.x = 0;
            c.y = 0;
            c.z = 0;

            // currently this code assumes that the texture width is a multiple of the allocation chunk size
            for( int i = 0; i < m_TmpDstChunks.Count; i++ )
            {
                m_TmpSrcChunks.Add(c);
                c.x += chunk_size * ProbeBrickPool.kBrickProbeCountPerDim;
                if( c.x >= dataloc.width )
                {
                    c.x = 0;
                    c.y += ProbeBrickPool.kBrickProbeCountPerDim;
                    if( c.y >= dataloc.height )
                    {
                        c.y = 0;
                        c.z += ProbeBrickPool.kBrickProbeCountPerDim;
                    }
                }
            }

            // Update the pool and index and ignore any potential frame latency related issues
            m_Pool.Update(dataloc, m_TmpSrcChunks, m_TmpDstChunks);
            m_Index.AddBricks(bricks, m_TmpDstChunks, m_Pool.GetChunkSize(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());

            Profiler.EndSample();
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
            int brickSubDivLevel = Mathf.CeilToInt(Mathf.Log(minVolumeSize, 3));
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
    }
}
