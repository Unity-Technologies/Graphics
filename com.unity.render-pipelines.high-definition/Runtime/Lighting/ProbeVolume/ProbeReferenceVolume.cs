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
            internal int x, y, z;
            internal int size;

            internal Vector3Int Position { get { return new Vector3Int(x, y, z); } }

            internal Brick(Vector3Int position, int size)
            {
                this.x = position.x;
                this.y = position.y;
                this.z = position.z;
                this.size = size;
            }
        }

        private Quaternion      m_Rotation;
        private Vector3         m_Position;
        private int             m_MaxSubdivision;
        private float           m_MinBrickSize;
        private ProbeBrickPool  m_Pool;
        private List<Brick>     m_TmpBricks  = new List<Brick>();
        private List<Chunk>     m_TmpSrcChunks = new List<Chunk>();
        private List<Chunk>     m_TmpDstChunks = new List<Chunk>();

        // index related
        Texture3D indexTex;

        internal ProbeReferenceVolume(int AllocationSize, int MemoryBudget)
        {
            m_Rotation = Quaternion.identity;
            m_Position = Vector3.zero;
            m_Pool = new ProbeBrickPool(AllocationSize, MemoryBudget);
        }

        internal void SetGridDensity(float minBrickSize, int maxSubdivision)
        {
            m_MinBrickSize = minBrickSize;
            m_MaxSubdivision = System.Math.Min(maxSubdivision, kMaxSubdivisionLevels);
        }

        internal int   cellSize(int subdivisionLevel) { return (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel); }
        internal float brickSize( int subdivisionLevel) { return m_MinBrickSize * cellSize(subdivisionLevel); }
        internal float minBrickSize() { return m_MinBrickSize; }
        internal float maxBrickSize() { return brickSize(m_MaxSubdivision); }
        internal Matrix4x4 GetRefSpaceToWS() { return Matrix4x4.TRS(m_Position, m_Rotation, Vector3.one); }

        internal delegate void SubdivisionDel(Matrix4x4 refSpaceToWS, List<Brick> inBricks, List<Brick> outBricks);

        internal void CreateBricks(ref Volume volume, SubdivisionDel subdivider, List<Brick> outSortedBricks, out int positionArraySize)
        {
            Volume vol;
            Transform(volume, out vol);
            m_TmpBricks.Clear();
            // rasterize bricks according to the coarsest grid
            Rasterize(vol, m_TmpBricks);
            subdivider(GetRefSpaceToWS(), m_TmpBricks, outSortedBricks);
            // sort from larger to smaller bricks
            outSortedBricks.Sort( (Brick lhs, Brick rhs) =>
            {
                if (lhs.size != rhs.size)
                    return lhs.size > rhs.size ? -1 : 1;
                if (lhs.z != rhs.z)
                    return lhs.z < rhs.z ? -1 : 1;
                if (lhs.y != rhs.y)
                    return lhs.y < rhs.y ? -1 : 1;
                if (lhs.x != rhs.x)
                    return lhs.x < rhs.x ? -1 : 1;

                return 0;
            });
            // communicate the required array size for storing positions to the caller
            positionArraySize = outSortedBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;
        }

        internal void ConvertBricks(List<Brick> bricks, Vector3[] outProbePositions)
        {
            Matrix4x4 m = GetRefSpaceToWS() * Matrix4x4.Scale(Vector3.one * minBrickSize());
            int posIdx = 0;

            for (int i = 0; i < bricks.Count; i++)
            {
                Vector3 origin = bricks[i].Position;
                float size = cellSize(bricks[i].size);

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            outProbePositions[posIdx++] = m.MultiplyPoint(origin + new Vector3(x, y, z) * (size / 3f));
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

            // Handle translation and rotation
            outVolume.Corner = m.MultiplyPoint(inVolume.Corner);
            outVolume.X = m.MultiplyVector(inVolume.X);
            outVolume.Y = m.MultiplyVector(inVolume.Y);
            outVolume.Z = m.MultiplyVector(inVolume.Z);
        }

        private void Rasterize(Volume volume, List<Brick> outBricks)
        {
            // Calculate bounding box for volume in refvol space
            var AABB = volume.CalculateAABB();
            AABB.center /= minBrickSize();
            AABB.size /= minBrickSize();

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
