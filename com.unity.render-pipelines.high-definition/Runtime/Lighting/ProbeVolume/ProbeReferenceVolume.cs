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
            public Vector3 corner;
            public Vector3 X;   // the vectors are NOT normalized, their length determines the size of the box
            public Vector3 Y;
            public Vector3 Z;
        }

        internal struct BrickIndex
        {
            internal uint packedIndex;
        }

        internal struct Brick
        {
            internal int x, y, z;
            internal int size;
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

        internal int   cellSize(int subdivisionLevel) { return ProbeBrickPool.kBrickCellCount ^ m_MaxSubdivision; }
        internal float brickSize( int subdivisionLevel) { return m_MinBrickSize * cellSize(subdivisionLevel); }
        internal float minBrickSize() { return m_MinBrickSize; }
        internal float maxBrickSize() { return brickSize(m_MaxSubdivision); }
        private Matrix4x4 GetRefSpaceToWS() { return Matrix4x4.TRS(m_Position, m_Rotation, Vector3.one); }

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
            positionArraySize = m_TmpBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;
        }

        internal void ConvertBricks(List<Brick> bricks, Vector3[] outProbePositions)
        {
            Matrix4x4 m = GetRefSpaceToWS;
            int posIdx = 0;

            foreach( var b in bricks)
            {
                Vector3 offset = new Vector3(b.x, b.y, b.z);
                        offset = m * offset;
                float   size = brickSize(b.size);
                Vector3 X = m.GetColumn(0) * size * 0.25f;
                Vector3 Y = m.GetColumn(1) * size * 0.25f;
                Vector3 Z = m.GetColumn(2) * size * 0.25f;
                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                {
                    for(int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                    {
                        for(int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                        {
                            outProbePositions[posIdx] = offset + x * X + y * Y + z * Z;
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
            Matrix4x4 m = Matrix4x4.TRS(m_Position, m_Rotation, Vector3.one);
            m = m.inverse;

            Vector3 X = m * inVolume.X;
            Vector3 Y = m * inVolume.Y;
            Vector3 Z = m * inVolume.Z;
            float offset = maxBrickSize() * 1.5f;
            outVolume.corner = (Vector3)(m * inVolume.corner) - X * offset - Y * offset - Z * offset;
            outVolume.X = X;
            outVolume.Y = Y;
            outVolume.Z = Z;
        }

        private void Rasterize(Volume volume, List<Brick> outBricks)
        {

        }

        void encodeIndex(ProbeBrickPool.BrickChunkAlloc chunk)
        {

        }

    }
}
