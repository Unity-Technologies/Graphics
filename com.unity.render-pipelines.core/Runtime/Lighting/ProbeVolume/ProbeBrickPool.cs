using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickPool
    {
        public struct BrickChunkAlloc
        {
            public int x, y, z;

            internal int flattenIndex(int sx, int sy) { return z * (sx * sy) + y * sx + x; }
        }

        public struct DataLocation
        {
            internal Texture3D TexL0_L1rx;

            internal Texture3D TexL1_G_ry;
            internal Texture3D TexL1_B_rz;

            internal Texture3D TexL2_0;
            internal Texture3D TexL2_1;
            internal Texture3D TexL2_2;
            internal Texture3D TexL2_3;

            internal int width;
            internal int height;
            internal int depth;

            internal void Cleanup()
            {
                CoreUtils.Destroy(TexL0_L1rx);

                CoreUtils.Destroy(TexL1_G_ry);
                CoreUtils.Destroy(TexL1_B_rz);

                CoreUtils.Destroy(TexL2_0);
                CoreUtils.Destroy(TexL2_1);
                CoreUtils.Destroy(TexL2_2);
                CoreUtils.Destroy(TexL2_3);

                TexL0_L1rx = null;

                TexL1_G_ry = null;
                TexL1_B_rz = null;

                TexL2_0 = null;
                TexL2_1 = null;
                TexL2_2 = null;
                TexL2_3 = null;
            }
        }

        internal const int kBrickCellCount = 3;
        internal const int kBrickProbeCountPerDim = kBrickCellCount + 1;
        internal const int kBrickProbeCountTotal = kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim;

        const int kMaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

        int                            m_AllocationSize;
        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        DataLocation                   m_Pool;
        BrickChunkAlloc                m_NextFreeChunk;
        Stack<BrickChunkAlloc>         m_FreeList;

        internal ProbeBrickPool(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget)
        {
            Profiler.BeginSample("Create ProbeBrickPool");
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_AllocationSize = allocationSize;
            m_MemoryBudget = memoryBudget;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            int width, height, depth;
            DerivePoolSizeFromBudget(allocationSize, memoryBudget, out width, out height, out depth);

            m_Pool = CreateDataLocation(width * height * depth, false, ProbeVolumeSHBands.SphericalHarmonicsL2);
            Profiler.EndSample();
        }

        internal ProbeVolumeTextureMemoryBudget GetMemoryBudget()
        {
            return m_MemoryBudget;
        }

        internal void EnsureTextureValidity()
        {
            // We assume that if a texture is null, all of them are. In any case we reboot them altogether.
            if (m_Pool.TexL0_L1rx == null)
            {
                m_Pool.Cleanup();
                m_Pool = CreateDataLocation(m_Pool.width * m_Pool.height * m_Pool.depth, false, ProbeVolumeSHBands.SphericalHarmonicsL2);
            }
        }

        internal int GetChunkSize() { return m_AllocationSize; }
        internal int GetPoolWidth() { return m_Pool.width; }
        internal int GetPoolHeight() { return m_Pool.height; }
        internal Vector3Int GetPoolDimensions() { return new Vector3Int(m_Pool.width, m_Pool.height, m_Pool.depth); }
        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            rr.L0_L1rx = m_Pool.TexL0_L1rx;

            rr.L1_G_ry = m_Pool.TexL1_G_ry;
            rr.L1_B_rz = m_Pool.TexL1_B_rz;

            rr.L2_0 = m_Pool.TexL2_0;
            rr.L2_1 = m_Pool.TexL2_1;
            rr.L2_2 = m_Pool.TexL2_2;
            rr.L2_3 = m_Pool.TexL2_3;
        }

        internal void Clear()
        {
            m_FreeList.Clear();
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;
        }

        internal void Allocate(int numberOfBrickChunks, List<BrickChunkAlloc> outAllocations)
        {
            while (m_FreeList.Count > 0 && numberOfBrickChunks > 0)
            {
                outAllocations.Add(m_FreeList.Pop());
                numberOfBrickChunks--;
            }

            for (uint i = 0; i < numberOfBrickChunks; i++)
            {
                if (m_NextFreeChunk.z >= m_Pool.depth)
                {
                    Debug.Assert(false, "Cannot allocate more brick chunks, probevolume brick pool is full.");
                    break; // failure case, pool is full
                }

                outAllocations.Add(m_NextFreeChunk);

                m_NextFreeChunk.x += m_AllocationSize * kBrickProbeCountPerDim;
                if (m_NextFreeChunk.x >= m_Pool.width)
                {
                    m_NextFreeChunk.x = 0;
                    m_NextFreeChunk.y += kBrickProbeCountPerDim;
                    if (m_NextFreeChunk.y >= m_Pool.height)
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

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations, ProbeVolumeSHBands bands)
        {
            Debug.Assert(srcLocations.Count == dstLocations.Count);

            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[i];

                for (int j = 0; j < kBrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(m_AllocationSize * kBrickProbeCountPerDim, source.width - src.x);
                    Graphics.CopyTexture(source.TexL0_L1rx  , src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL0_L1rx  , dst.z + j, 0, dst.x, dst.y);

                    Graphics.CopyTexture(source.TexL1_G_ry, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_G_ry, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_B_rz, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_B_rz, dst.z + j, 0, dst.x, dst.y);

                    if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        Graphics.CopyTexture(source.TexL2_0, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_0, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.TexL2_1, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_1, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.TexL2_2, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_2, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.TexL2_3, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_3, dst.z + j, 0, dst.x, dst.y);
                    }
                }
            }
        }

        public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands)
        {
            Debug.Assert(numProbes != 0);
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

            loc.TexL0_L1rx   = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGB_BC6H_UFloat : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);

            loc.TexL1_G_ry = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            loc.TexL1_B_rz = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_1 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_2 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_3 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            }
            else
            {
                loc.TexL2_0 = null;
                loc.TexL2_1 = null;
                loc.TexL2_2 = null;
                loc.TexL2_3 = null;
            }

            loc.width = width;
            loc.height = height;
            loc.depth = depth;

            return loc;
        }

        public static void FillDataLocation(ref DataLocation loc, SphericalHarmonicsL2[] shl2, ProbeVolumeSHBands bands)
        {
            int numBricks = shl2.Length / kBrickProbeCountTotal;
            int shidx = 0;
            int bx = 0, by = 0, bz = 0;
            Color c = new Color();
            for (int brickIdx = 0; brickIdx < shl2.Length; brickIdx += kBrickProbeCountTotal)
            {
                for (int z = 0; z < kBrickProbeCountPerDim; z++)
                {
                    for (int y = 0; y < kBrickProbeCountPerDim; y++)
                    {
                        for (int x = 0; x < kBrickProbeCountPerDim; x++)
                        {
                            int ix = bx + x;
                            int iy = by + y;
                            int iz = bz + z;

                            Vector3 L0 = SphericalHarmonicsL2Utils.GetCoefficient(shl2[shidx], 0);
                            Vector3 L1R, L1G, L1B;
                            SphericalHarmonicsL2Utils.GetL1(shl2[shidx], out L1R, out L1G, out L1B);

                            // First texture will have L0 coefficients in RGB, and L1R.x in the alpha channel
                            Color L0_L1Rx = new Color(L0.x, L0.y, L0.z, L1R.x);
                            // Second texture will have L1_G coefficients in RGB and L1R.y in the alpha channel
                            Color L1G_L1Ry = new Color(L1G.x, L1G.y, L1G.z, L1R.y);
                            // Third texture will have L1_B coefficients in RGB and L1R.z in the alpha channel
                            Color L1B_L1Rz = new Color(L1B.x, L1B.y, L1B.z, L1R.z);

                            loc.TexL0_L1rx.SetPixel(ix, iy, iz, L0_L1Rx);
                            loc.TexL1_G_ry.SetPixel(ix, iy, iz, L1G_L1Ry);
                            loc.TexL1_B_rz.SetPixel(ix, iy, iz, L1B_L1Rz);

                            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                Vector3 L2_0, L2_1, L2_2, L2_3, L2_4;
                                SphericalHarmonicsL2Utils.GetL2(shl2[shidx], out L2_0, out L2_1, out L2_2, out L2_3, out L2_4);

                                c.r = L2_0.x;
                                c.g = L2_1.x;
                                c.b = L2_2.x;
                                c.a = L2_3.x;
                                loc.TexL2_0.SetPixel(ix, iy, iz, c);

                                c.r = L2_0.y;
                                c.g = L2_1.y;
                                c.b = L2_2.y;
                                c.a = L2_3.y;
                                loc.TexL2_1.SetPixel(ix, iy, iz, c);


                                c.r = L2_0.z;
                                c.g = L2_1.z;
                                c.b = L2_2.z;
                                c.a = L2_3.z;
                                loc.TexL2_2.SetPixel(ix, iy, iz, c);

                                c.r = L2_4.x;
                                c.g = L2_4.y;
                                c.b = L2_4.z;
                                c.a = 1;
                                loc.TexL2_3.SetPixel(ix, iy, iz, c);
                            }

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
                        Debug.Assert(bz < loc.depth || brickIdx == shl2.Length - kBrickProbeCountTotal, "Location depth exceeds data texture.");
                    }
                }
            }

            loc.TexL0_L1rx.Apply(false);

            loc.TexL1_G_ry.Apply(false);
            loc.TexL1_B_rz.Apply(false);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0.Apply(false);
                loc.TexL2_1.Apply(false);
                loc.TexL2_2.Apply(false);
                loc.TexL2_3.Apply(false);
            }
        }

        private void DerivePoolSizeFromBudget(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget, out int width, out int height, out int depth)
        {
            // TODO: This is fairly simplistic for now and relies on the enum to have the value set to the desired numbers,
            // might change the heuristic later on.
            width = (int)memoryBudget;
            height = (int)memoryBudget;
            depth = kBrickProbeCountPerDim;
        }

        internal void Cleanup()
        {
            m_Pool.Cleanup();
        }
    }
}
