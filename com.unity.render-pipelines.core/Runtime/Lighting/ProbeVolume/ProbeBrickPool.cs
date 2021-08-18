using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickPool
    {
        [DebuggerDisplay("Chunk ({x}, {y}, {z})")]
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

        internal int estimatedVMemCost { get; private set; }

        const int kMaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

        int m_AllocationSize;
        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        DataLocation m_Pool;
        BrickChunkAlloc m_NextFreeChunk;
        Stack<BrickChunkAlloc> m_FreeList;

        ProbeVolumeSHBands m_SHBands;

        internal ProbeBrickPool(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands)
        {
            Profiler.BeginSample("Create ProbeBrickPool");
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_AllocationSize = allocationSize;
            m_MemoryBudget = memoryBudget;
            m_SHBands = shBands;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            int width, height, depth;
            DerivePoolSizeFromBudget(allocationSize, memoryBudget, out width, out height, out depth);
            int estimatedCost = 0;
            m_Pool = CreateDataLocation(width * height * depth, false, shBands, out estimatedCost);
            estimatedVMemCost = estimatedCost;

            Profiler.EndSample();
        }

        internal void EnsureTextureValidity()
        {
            // We assume that if a texture is null, all of them are. In any case we reboot them altogether.
            if (m_Pool.TexL0_L1rx == null)
            {
                m_Pool.Cleanup();
                int estimatedCost = 0;
                m_Pool = CreateDataLocation(m_Pool.width * m_Pool.height * m_Pool.depth, false, m_SHBands, out estimatedCost);
                estimatedVMemCost = estimatedCost;
            }
        }

        internal int GetChunkSize() { return m_AllocationSize; }
        internal int GetChunkSizeInProbeCount() { return m_AllocationSize * kBrickProbeCountTotal; }

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
                    Graphics.CopyTexture(source.TexL0_L1rx, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL0_L1rx, dst.z + j, 0, dst.x, dst.y);

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

        static Vector3Int ProbeCountToDataLocSize(int numProbes)
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

            width *= kBrickProbeCountPerDim;
            height *= kBrickProbeCountPerDim;
            depth *= kBrickProbeCountPerDim;

            return new Vector3Int(width, height, depth);
        }

        public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands, out int allocatedBytes)
        {
            Vector3Int locSize = ProbeCountToDataLocSize(numProbes);
            int width = locSize.x;
            int height = locSize.y;
            int depth = locSize.z;

            int texelCount = width * height * depth;

            DataLocation loc;

            allocatedBytes = 0;
            loc.TexL0_L1rx = new Texture3D(width, height, depth, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            allocatedBytes += texelCount * 8;

            loc.TexL1_G_ry = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            allocatedBytes += texelCount * (compressed ? 1 : 4);

            loc.TexL1_B_rz = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            allocatedBytes += texelCount * (compressed ? 1 : 4);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_1 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_2 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_3 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                allocatedBytes += texelCount * (compressed ? 1 : 4);
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

        static void SetPixel(ref Color[] data, int x, int y, int z, int dataLocWidth, int dataLocHeight, Color value)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            data[index] = value;
        }

        public static void FillDataLocation(ref DataLocation loc, SphericalHarmonicsL2[] shl2, ProbeVolumeSHBands bands)
        {
            int numBricks = shl2.Length / kBrickProbeCountTotal;
            int shidx = 0;
            int bx = 0, by = 0, bz = 0;
            Color c = new Color();

            Color[] L0L1Rx_locData = new Color[loc.width * loc.height * loc.depth * 2];
            Color[] L1GL1Ry_locData = new Color[loc.width * loc.height * loc.depth * 2];
            Color[] L1BL1Rz_locData = new Color[loc.width * loc.height * loc.depth * 2];

            Color[] L2_0_locData = null;
            Color[] L2_1_locData = null;
            Color[] L2_2_locData = null;
            Color[] L2_3_locData = null;


            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                L2_0_locData = new Color[loc.width * loc.height * loc.depth];
                L2_1_locData = new Color[loc.width * loc.height * loc.depth];
                L2_2_locData = new Color[loc.width * loc.height * loc.depth];
                L2_3_locData = new Color[loc.width * loc.height * loc.depth];
            }

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

                            c.r = shl2[shidx][0, 0]; // L0.r
                            c.g = shl2[shidx][1, 0]; // L0.g
                            c.b = shl2[shidx][2, 0]; // L0.b
                            c.a = shl2[shidx][0, 1]; // L1_R.r
                            SetPixel(ref L0L1Rx_locData, ix, iy, iz, loc.width, loc.height, c);

                            c.r = shl2[shidx][1, 1]; // L1_G.r
                            c.g = shl2[shidx][1, 2]; // L1_G.g
                            c.b = shl2[shidx][1, 3]; // L1_G.b
                            c.a = shl2[shidx][0, 2]; // L1_R.g
                            SetPixel(ref L1GL1Ry_locData, ix, iy, iz, loc.width, loc.height, c);

                            c.r = shl2[shidx][2, 1]; // L1_B.r
                            c.g = shl2[shidx][2, 2]; // L1_B.g
                            c.b = shl2[shidx][2, 3]; // L1_B.b
                            c.a = shl2[shidx][0, 3]; // L1_R.b
                            SetPixel(ref L1BL1Rz_locData, ix, iy, iz, loc.width, loc.height, c);

                            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                            {
                                c.r = shl2[shidx][0, 4];
                                c.g = shl2[shidx][0, 5];
                                c.b = shl2[shidx][0, 6];
                                c.a = shl2[shidx][0, 7];
                                SetPixel(ref L2_0_locData, ix, iy, iz, loc.width, loc.height, c);

                                c.r = shl2[shidx][1, 4];
                                c.g = shl2[shidx][1, 5];
                                c.b = shl2[shidx][1, 6];
                                c.a = shl2[shidx][1, 7];
                                SetPixel(ref L2_1_locData, ix, iy, iz, loc.width, loc.height, c);

                                c.r = shl2[shidx][2, 4];
                                c.g = shl2[shidx][2, 5];
                                c.b = shl2[shidx][2, 6];
                                c.a = shl2[shidx][2, 7];
                                SetPixel(ref L2_2_locData, ix, iy, iz, loc.width, loc.height, c);

                                c.r = shl2[shidx][0, 8];
                                c.g = shl2[shidx][1, 8];
                                c.b = shl2[shidx][2, 8];
                                c.a = 1;
                                SetPixel(ref L2_3_locData, ix, iy, iz, loc.width, loc.height, c);
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

            loc.TexL0_L1rx.SetPixels(L0L1Rx_locData);
            loc.TexL0_L1rx.Apply(false);
            loc.TexL1_G_ry.SetPixels(L1GL1Ry_locData);
            loc.TexL1_G_ry.Apply(false);
            loc.TexL1_B_rz.SetPixels(L1BL1Rz_locData);
            loc.TexL1_B_rz.Apply(false);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0.SetPixels(L2_0_locData);
                loc.TexL2_0.Apply(false);
                loc.TexL2_1.SetPixels(L2_1_locData);
                loc.TexL2_1.Apply(false);
                loc.TexL2_2.SetPixels(L2_2_locData);
                loc.TexL2_2.Apply(false);
                loc.TexL2_3.SetPixels(L2_3_locData);
                loc.TexL2_3.Apply(false);
            }
        }

        void DerivePoolSizeFromBudget(int allocationSize, ProbeVolumeTextureMemoryBudget memoryBudget, out int width, out int height, out int depth)
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
