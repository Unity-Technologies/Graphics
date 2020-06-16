using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeBrickPool
    {
        internal struct BrickChunkAlloc
        {
            internal int x, y, z;

            internal int flattenIndex(int sx, int sy) { return z * (sx * sy) + y * sx + x; }
        }

        public struct DataLocation
        {
            internal Texture3D TexL0;
            internal Texture3D TexL1_R;
            internal Texture3D TexL1_G;
            internal Texture3D TexL1_B;

            internal int width  { get { return TexL0.width; } }
            internal int height { get { return TexL0.height; } }
            internal int depth  { get { return TexL0.depth; } }
        }

        internal const int kBrickCellCount = 3;
        internal const int kBrickProbeCountPerDim = kBrickCellCount + 1;
        internal const int kBrickProbeCountTotal = kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim;

        const int kMaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

        int                     m_AllocationSize;
        int                     m_MemoryBudget;
        DataLocation            m_Pool;
        BrickChunkAlloc         m_NextFreeChunk;
        Stack<BrickChunkAlloc>  m_FreeList;

        internal ProbeBrickPool(int AllocationSize, int MemoryBudget)
        {
            Profiler.BeginSample("Create ProbeBrickPool");
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_AllocationSize = AllocationSize;
            m_MemoryBudget = MemoryBudget;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            int width, height, depth;
            DerivePoolSizeFromBudget(AllocationSize, MemoryBudget, out width, out height, out depth);

            m_Pool = CreateDataLocation(width * height * depth, false);
            Profiler.EndSample();
        }

        internal int GetChunkSize() { return m_AllocationSize; }
        internal int GetPoolWidth() { return m_Pool.width; }
        internal int GetPoolHeight() { return m_Pool.height; }
        internal Vector3Int GetPoolDimensions() { return new Vector3Int(m_Pool.width, m_Pool.height, m_Pool.depth); }
        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            rr.L0 = m_Pool.TexL0;
            rr.L1_R = m_Pool.TexL1_R;
            rr.L1_G = m_Pool.TexL1_G;
            rr.L1_B = m_Pool.TexL1_B;
        }


        internal void Clear()
        {
            m_FreeList.Clear();
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;
        }

        internal void Allocate(int numberOfBrickChunks, List<BrickChunkAlloc> outAllocations)
        {
            while(m_FreeList.Count > 0 && numberOfBrickChunks > 0 )
            {
                outAllocations.Add(m_FreeList.Pop());
                numberOfBrickChunks--;
            }

            for( uint i = 0; i < numberOfBrickChunks; i++ )
            {
                if (m_NextFreeChunk.z >= m_Pool.depth)
                {
                    Debug.Assert(false, "Cannot allocate more brick chunks, probevolume brick pool is full.");
                    break; // failure case, pool is full
                }

                outAllocations.Add(m_NextFreeChunk);

                m_NextFreeChunk.x += m_AllocationSize * kBrickProbeCountPerDim;
                if(m_NextFreeChunk.x >= m_Pool.width )
                {
                    m_NextFreeChunk.x = 0;
                    m_NextFreeChunk.y += kBrickProbeCountPerDim;
                    if(m_NextFreeChunk.y >= m_Pool.height )
                    {
                        m_NextFreeChunk.y = 0;
                        m_NextFreeChunk.z += kBrickProbeCountPerDim;
                    }
                }
            }
        }

        internal void Deallocate(List<BrickChunkAlloc> allocations)
        {
            foreach (var brick in allocations)
                m_FreeList.Push(brick);
        }

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations)
        {
            Debug.Assert(srcLocations.Count == dstLocations.Count);

            for( int i = 0; i < srcLocations.Count; i++ )
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[i];

                for( int j = 0; j < kBrickProbeCountPerDim; j++ )
                {
                    int width = Mathf.Min(m_AllocationSize * kBrickProbeCountPerDim, source.width - src.x);
                    Graphics.CopyTexture(source.TexL0  , src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL0  , dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_R, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_R, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_G, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_G, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_B, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_B, dst.z + j, 0, dst.x, dst.y);
                }
            }
        }

        public static DataLocation CreateDataLocation(int numProbes, bool compressed)
        {
            Debug.Assert(numProbes % kBrickProbeCountTotal == 0);

            int numBricks = numProbes / kBrickProbeCountTotal;
            int poolWidth = kMaxPoolWidth / kBrickProbeCountPerDim;

            int width, height, depth;
            depth = (numBricks + poolWidth * poolWidth - 1) / (poolWidth * poolWidth);
            if (depth > 1)
                width = height = poolWidth;
            else
            {
                height = (numBricks + poolWidth - 1) / poolWidth;
                if (height > 1)
                    width = poolWidth;
                else
                    width = numBricks;
            }

            width  *= kBrickProbeCountPerDim;
            height *= kBrickProbeCountPerDim;
            depth  *= kBrickProbeCountPerDim;

            DataLocation loc;
            loc.TexL0   = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGB_BC6H_UFloat : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            loc.TexL1_R = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            loc.TexL1_G = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            loc.TexL1_B = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            return loc;
        }

        public static void FillDataLocation( ref DataLocation loc, SphericalHarmonicsL1[] shl1 )
        {
            int numBricks = shl1.Length / kBrickProbeCountTotal;
            int shidx = 0;
            int bx = 0, by = 0, bz = 0;
            Color c = new Color();
            for ( int brickIdx = 0; brickIdx < shl1.Length; brickIdx += kBrickProbeCountTotal)
            {
                for (int z = 0; z < kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < kBrickProbeCountPerDim; x++ )
                        {
                            int ix = bx + x;
                            int iy = by + y;
                            int iz = bz + z;

                            c.r = shl1[shidx].shAr[3];
                            c.g = shl1[shidx].shAg[3];
                            c.b = shl1[shidx].shAb[3];
                            loc.TexL0.SetPixel(ix, iy, iz, c);

                            c.r = shl1[shidx].shAr[0];
                            c.g = shl1[shidx].shAr[1];
                            c.b = shl1[shidx].shAr[2];
                            loc.TexL1_R.SetPixel(ix, iy, iz, c);

                            c.r = shl1[shidx].shAg[0];
                            c.g = shl1[shidx].shAg[1];
                            c.b = shl1[shidx].shAg[2];
                            loc.TexL1_G.SetPixel(ix, iy, iz, c);

                            c.r = shl1[shidx].shAb[0];
                            c.g = shl1[shidx].shAb[1];
                            c.b = shl1[shidx].shAb[2];
                            loc.TexL1_B.SetPixel(ix, iy, iz, c);

                            shidx++;
                        }
                    }
                }
                // update the pool index
                bx += kBrickProbeCountPerDim;
                if (bx >= loc.width)
                {
                    bx = 0;
                    by += kBrickProbeCountPerDim;
                    if (by >= loc.height)
                    {
                        by = 0;
                        bz += kBrickProbeCountPerDim;
                        Debug.Assert(bz < loc.depth || brickIdx == shl1.Length - kBrickProbeCountTotal, "Location depth exceeds data texture.");
                    }
                }
            }

            loc.TexL0.Apply(false);
            loc.TexL1_R.Apply(false);
            loc.TexL1_G.Apply(false);
            loc.TexL1_B.Apply(false);
        }

        private void DerivePoolSizeFromBudget(int AllocationSize, int MemoryBudget, out int width, out int height, out int depth)
        {
            // TODO: Calculate chunk memory size
            width  = 1024;
            height = 1024;
            depth  = kBrickProbeCountPerDim;
        }
    }
}
