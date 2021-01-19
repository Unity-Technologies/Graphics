using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
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
            internal Texture3D TexL0;

            internal Texture3D TexL1_R;
            internal Texture3D TexL1_G;
            internal Texture3D TexL1_B;

            internal Texture3D TexL2_R;
            internal Texture3D TexL2_G;
            internal Texture3D TexL2_B;

            internal int width;
            internal int height;
            internal int depth;

            internal void Cleanup()
            {
                CoreUtils.Destroy(TexL0);

                CoreUtils.Destroy(TexL1_R);
                CoreUtils.Destroy(TexL1_G);
                CoreUtils.Destroy(TexL1_B);

                CoreUtils.Destroy(TexL2_R);
                CoreUtils.Destroy(TexL2_G);
                CoreUtils.Destroy(TexL2_B);

                TexL0 = null;

                TexL1_R = null;
                TexL1_G = null;
                TexL1_B = null;

                TexL2_R = null;
                TexL2_G = null;
                TexL2_B = null;
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
            if (m_Pool.TexL0 == null)
            {
                m_Pool.Cleanup();
            }
            m_Pool = CreateDataLocation(m_Pool.width * m_Pool.height * m_Pool.depth, false, ProbeVolumeSHBands.SphericalHarmonicsL2);
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

            rr.L2_R = m_Pool.TexL2_R;
            rr.L2_G = m_Pool.TexL2_G;
            rr.L2_B = m_Pool.TexL2_B;
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

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations)
        {
            Debug.Assert(srcLocations.Count == dstLocations.Count);

            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[i];

                for (int j = 0; j < kBrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(m_AllocationSize * kBrickProbeCountPerDim, source.width - src.x);
                    Graphics.CopyTexture(source.TexL0  , src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL0  , dst.z + j, 0, dst.x, dst.y);

                    Graphics.CopyTexture(source.TexL1_R, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_R, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_G, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_G, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_B, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_B, dst.z + j, 0, dst.x, dst.y);

                    // TODO: IF L2
                    Graphics.CopyTexture(source.TexL2_R, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_R, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL2_G, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_G, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL2_B, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL2_B, dst.z + j, 0, dst.x, dst.y);
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

            loc.TexL0   = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGB_BC6H_UFloat : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);

            loc.TexL1_R = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            loc.TexL1_G = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            loc.TexL1_B = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm  : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_R = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
                loc.TexL2_G = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
                loc.TexL2_B = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            }
            else
            {
                loc.TexL2_R = null;
                loc.TexL2_G = null;
                loc.TexL2_B = null;
            }

            loc.width = width;
            loc.height = height;
            loc.depth = depth;

            return loc;
        }

        public static void FillDataLocation(ref DataLocation loc, SphericalHarmonicsL1[] shl1)
        {
            int numBricks = shl1.Length / kBrickProbeCountTotal;
            int shidx = 0;
            int bx = 0, by = 0, bz = 0;
            Color c = new Color();
            for (int brickIdx = 0; brickIdx < shl1.Length; brickIdx += kBrickProbeCountTotal)
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

                            c.r = L1R.x;
                            c.g = L1R.y;
                            c.b = L1R.z;
                            c.a = L0.x;
                            loc.TexL1_R.SetPixel(ix, iy, iz, c);

                            // L1g
                            c.r = L1G.x;
                            c.g = L1G.y;
                            c.b = L1G.z;
                            c.a = L0.y;
                            loc.TexL1_G.SetPixel(ix, iy, iz, c);

                            c.r = L1B.x;
                            c.g = L1B.y;
                            c.b = L1B.z;
                            c.a = L0.z;
                            loc.TexL1_B.SetPixel(ix, iy, iz, c);

                            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                Vector3 L2_0, L2_1, L2_2, L2_3, L2_4;
                                SphericalHarmonicsL2Utils.GetL2(shl2[shidx], out L2_0, out L2_1, out L2_2, out L2_3, out L2_4);

                                c.r = L2_0.x;
                                c.g = L2_1.x;
                                c.b = L2_2.x;
                                c.a = L2_3.x;
                                loc.TexL2_R.SetPixel(ix, iy, iz, c);

                                c.r = L2_0.y;
                                c.g = L2_1.y;
                                c.b = L2_2.y;
                                c.a = L2_3.y;
                                loc.TexL2_G.SetPixel(ix, iy, iz, c);


                                c.r = L2_0.z;
                                c.g = L2_1.z;
                                c.b = L2_2.z;
                                c.a = L2_3.z;
                                loc.TexL2_B.SetPixel(ix, iy, iz, c);

                                c.r = L2_4.x;
                                c.g = L2_4.y;
                                c.b = L2_4.z;
                                c.a = 1;
                                loc.TexL0.SetPixel(ix, iy, iz, c);
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

            loc.TexL0.Apply(false);

            loc.TexL1_R.Apply(false);
            loc.TexL1_G.Apply(false);
            loc.TexL1_B.Apply(false);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_R.Apply(false);
                loc.TexL2_G.Apply(false);
                loc.TexL2_B.Apply(false);
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
