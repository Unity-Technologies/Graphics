using System.Collections.Generic;
using Chunk = UnityEngine.Rendering.HighDefinition.ProbeBrickPool.BrickChunkAlloc;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeReferenceVolume
    {
        // a few constants
        internal const int kMaxSubdivisionLevels  = 15; // 4 bits

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

        internal struct BrickIndex
        {
            internal uint packedIndex;
        }

        internal struct Brick
        {
            internal Vector3Int position;   // refspace index, indices are cell coordinates at max resolution
            internal int size;              // size as factor covered elementary cells

            internal Brick(Vector3Int position, int size)
            {
                this.position = position;
                this.size = size;
            }
        }

        internal struct RefVolTransform
        {
            public Matrix4x4   refSpaceToWS;
            public Vector3     posWS;
            public Quaternion  rot;
            public float       scale;
        }

        private RefVolTransform m_Transform;
        private int             m_MaxSubdivision;
        private ProbeBrickPool  m_Pool;
        private List<Brick>     m_TmpBricks  = new List<Brick>();
        private List<Chunk>     m_TmpSrcChunks = new List<Chunk>();
        private List<Chunk>     m_TmpDstChunks = new List<Chunk>();
        private float[]         m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];

        // index related
        Texture3D indexTex;

        internal ProbeReferenceVolume(int AllocationSize, int MemoryBudget)
        {
            m_Transform.posWS = Vector3.zero;
            m_Transform.rot = Quaternion.identity;
            m_Transform.scale = 1.0f;
            m_Transform.refSpaceToWS = Matrix4x4.identity;

            m_Pool = new ProbeBrickPool(AllocationSize, MemoryBudget);

            // initialize offsets
            m_PositionOffsets[0] = 0.0f;
            float probeDelta = 1.0f / ProbeBrickPool.kBrickCellCount;
            for (int i = 1; i < ProbeBrickPool.kBrickProbeCountPerDim - 1; i++)
                m_PositionOffsets[i] = i * probeDelta;
            m_PositionOffsets[m_PositionOffsets.Length-1] = 1.0f;
        }

        internal void SetGridDensity(float minBrickSize, int maxSubdivision)
        {
            m_MaxSubdivision = System.Math.Min(maxSubdivision, kMaxSubdivisionLevels);

            m_Transform.scale = minBrickSize;
            m_Transform.refSpaceToWS = Matrix4x4.TRS(m_Transform.posWS, m_Transform.rot, Vector3.one * m_Transform.scale);
        }

        internal int   cellSize(int subdivisionLevel) { return (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel); }
        internal float brickSize( int subdivisionLevel) { return m_Transform.scale * cellSize(subdivisionLevel); }
        internal float minBrickSize() { return m_Transform.scale; }
        internal float maxBrickSize() { return brickSize(m_MaxSubdivision); }
        internal Matrix4x4 GetRefSpaceToWS() { return m_Transform.refSpaceToWS; }

        internal delegate void SubdivisionDel(RefVolTransform refSpaceToWS, List<Brick> inBricks, List<Brick> outBricks);

        internal void CreateBricks(ref Volume volume, SubdivisionDel subdivider, List<Brick> outSortedBricks, out int positionArraySize)
        {
            Volume vol;
            Transform(volume, out vol);
            m_TmpBricks.Clear();
            // rasterize bricks according to the coarsest grid
            Rasterize(vol, m_TmpBricks);
            subdivider(m_Transform, m_TmpBricks, outSortedBricks);
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
            // communicate the required array size for storing positions to the caller
            positionArraySize = outSortedBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;
        }

        internal void SubdivideBricks(List<Brick> inBricks, List<Brick> outSubdividedBricks)
        {
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
                    b.position.z = b.position.z + z * offset;

                    for (int y = 0; y < ProbeBrickPool.kBrickCellCount; y++)
                    {
                        b.position.y = b.position.y + y * offset;

                        for (int x = 0; x < ProbeBrickPool.kBrickCellCount; x++)
                        {
                            b.position.x = b.position.x + x * offset;
                            outSubdividedBricks.Add(b);
                        }
                    }
                }
            }
        }

        internal void ConvertBricks(List<Brick> bricks, Vector3[] outProbePositions)
        {
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
        }

        internal void AddBricks(List<Brick> bricks, ProbeBrickPool.DataLocation dataloc)
        {
            m_TmpSrcChunks.Clear();
            m_TmpDstChunks.Clear();
            m_Pool.Allocate(bricks.Count / m_Pool.GetChunkSize(), m_TmpDstChunks);

            // fill m_TmpSrcChunks
            m_Pool.Update(dataloc, m_TmpSrcChunks, m_TmpDstChunks);
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

        private void Rasterize(Volume volume, List<Brick> outBricks)
        {
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
        }

        void encodeIndex(ProbeBrickPool.BrickChunkAlloc chunk)
        {

        }

    }
}
