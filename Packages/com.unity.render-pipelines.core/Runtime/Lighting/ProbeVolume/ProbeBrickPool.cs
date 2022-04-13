using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickPool
    {
        const int kProbePoolChunkSize = 128;

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

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        DataLocation m_Pool;
        BrickChunkAlloc m_NextFreeChunk;
        Stack<BrickChunkAlloc> m_FreeList;
        int m_AvailableChunkCount;

        ProbeVolumeSHBands m_SHBands;

        // Temporary buffers for updating SH textures.
        static DynamicArray<Color> s_L0L1Rx_locData = new DynamicArray<Color>();
        static DynamicArray<Color> s_L1GL1Ry_locData = new DynamicArray<Color>();
        static DynamicArray<Color> s_L1BL1Rz_locData = new DynamicArray<Color>();

        static DynamicArray<Color> s_L2_0_locData = null;
        static DynamicArray<Color> s_L2_1_locData = null;
        static DynamicArray<Color> s_L2_2_locData = null;
        static DynamicArray<Color> s_L2_3_locData = null;

        internal ProbeBrickPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands)
        {
            Profiler.BeginSample("Create ProbeBrickPool");
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_MemoryBudget = memoryBudget;
            m_SHBands = shBands;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            int width, height, depth;
            DerivePoolSizeFromBudget(memoryBudget, out width, out height, out depth);
            int estimatedCost = 0;
            m_Pool = CreateDataLocation(width * height * depth, false, shBands, "APV", out estimatedCost);
            estimatedVMemCost = estimatedCost;

            m_AvailableChunkCount = (width / (kProbePoolChunkSize * kBrickProbeCountPerDim)) * (height / kBrickProbeCountPerDim) * (depth / kBrickProbeCountPerDim);

            Profiler.EndSample();
        }

        public int GetRemainingChunkCount()
        {
            return m_AvailableChunkCount;
        }


        internal void EnsureTextureValidity()
        {
            // We assume that if a texture is null, all of them are. In any case we reboot them altogether.
            if (m_Pool.TexL0_L1rx == null)
            {
                m_Pool.Cleanup();
                int estimatedCost = 0;
                m_Pool = CreateDataLocation(m_Pool.width * m_Pool.height * m_Pool.depth, false, m_SHBands, "APV", out estimatedCost);
                estimatedVMemCost = estimatedCost;
            }
        }

        internal static int GetChunkSize() { return kProbePoolChunkSize; }
        internal static int GetChunkSizeInProbeCount() { return kProbePoolChunkSize * kBrickProbeCountTotal; }

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

        internal static int GetChunkCount(int brickCount)
        {
            int chunkSize = GetChunkSize();
            return (brickCount + chunkSize - 1) / chunkSize;
        }

        internal bool Allocate(int numberOfBrickChunks, List<BrickChunkAlloc> outAllocations)
        {
            while (m_FreeList.Count > 0 && numberOfBrickChunks > 0)
            {
                outAllocations.Add(m_FreeList.Pop());
                numberOfBrickChunks--;
                m_AvailableChunkCount--;
            }

            for (uint i = 0; i < numberOfBrickChunks; i++)
            {
                if (m_NextFreeChunk.z >= m_Pool.depth)
                {
                    Debug.LogError("Cannot allocate more brick chunks, probe volume brick pool is full.");
                    return false; // failure case, pool is full
                }

                outAllocations.Add(m_NextFreeChunk);
                m_AvailableChunkCount--;

                m_NextFreeChunk.x += kProbePoolChunkSize * kBrickProbeCountPerDim;
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

            return true;
        }

        internal void Deallocate(List<BrickChunkAlloc> allocations)
        {
            m_AvailableChunkCount += allocations.Count;

            foreach (var brick in allocations)
                m_FreeList.Push(brick);
        }

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations, int destStartIndex, ProbeVolumeSHBands bands)
        {
            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[destStartIndex + i];

                for (int j = 0; j < kBrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(kProbePoolChunkSize * kBrickProbeCountPerDim, source.width - src.x);
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

        public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands, string name, out int allocatedBytes)
        {
            Vector3Int locSize = ProbeCountToDataLocSize(numProbes);
            int width = locSize.x;
            int height = locSize.y;
            int depth = locSize.z;

            int texelCount = width * height * depth;

            DataLocation loc;

            allocatedBytes = 0;
            loc.TexL0_L1rx = new Texture3D(width, height, depth, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None, 1);
            loc.TexL0_L1rx.hideFlags = HideFlags.HideAndDontSave;
            loc.TexL0_L1rx.name = $"{name}_TexL0_L1rx";
            allocatedBytes += texelCount * 8;

            loc.TexL1_G_ry = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            loc.TexL1_G_ry.hideFlags = HideFlags.HideAndDontSave;
            loc.TexL1_G_ry.name = $"{name}_TexL1_G_ry";
            allocatedBytes += texelCount * (compressed ? 1 : 4);

            loc.TexL1_B_rz = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
            loc.TexL1_B_rz.hideFlags = HideFlags.HideAndDontSave;
            loc.TexL1_B_rz.name = $"{name}_TexL1_B_rz";
            allocatedBytes += texelCount * (compressed ? 1 : 4);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_0.hideFlags = HideFlags.HideAndDontSave;
                loc.TexL2_0.name = $"{name}_TexL2_0";
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_1 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_1.hideFlags = HideFlags.HideAndDontSave;
                loc.TexL2_1.name = $"{name}_TexL2_1";
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_2 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_2.hideFlags = HideFlags.HideAndDontSave;
                loc.TexL2_2.name = $"{name}_TexL2_2";
                allocatedBytes += texelCount * (compressed ? 1 : 4);

                loc.TexL2_3 = new Texture3D(width, height, depth, compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None, 1);
                loc.TexL2_3.hideFlags = HideFlags.HideAndDontSave;
                loc.TexL2_3.name = $"{name}_TexL2_3";
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


        static void ValidateTemporaryBuffers(in DataLocation loc, ProbeVolumeSHBands bands)
        {
            var size = loc.width * loc.height * loc.depth;

            s_L0L1Rx_locData.Resize(size);
            s_L1GL1Ry_locData.Resize(size);
            s_L1BL1Rz_locData.Resize(size);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                if (s_L2_0_locData == null)
                {
                    s_L2_0_locData = new DynamicArray<Color>();
                    s_L2_1_locData = new DynamicArray<Color>();
                    s_L2_2_locData = new DynamicArray<Color>();
                    s_L2_3_locData = new DynamicArray<Color>();
                }

                s_L2_0_locData.Resize(size);
                s_L2_1_locData.Resize(size);
                s_L2_2_locData.Resize(size);
                s_L2_3_locData.Resize(size);
            }
            else
            {
                s_L2_0_locData = null;
                s_L2_1_locData = null;
                s_L2_2_locData = null;
                s_L2_3_locData = null;
            }
        }

        static void SetPixel(DynamicArray<Color> data, int x, int y, int z, int dataLocWidth, int dataLocHeight, Color value)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            data[index] = value;
        }

        public static void FillDataLocation(ref DataLocation loc, SphericalHarmonicsL2[] shl2, int startIndex, int count, ProbeVolumeSHBands bands)
        {
            int shidx = startIndex;
            int bx = 0, by = 0, bz = 0;
            Color c = new Color();

            ValidateTemporaryBuffers(loc, bands);

            for (int brickIdx = startIndex; brickIdx < (startIndex + count); brickIdx += kBrickProbeCountTotal)
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

                            // We are processing chunks at a time.
                            // So in practice we can go over the number of SH we have in the input list.
                            // We fill with black to avoid copying garbage in the final atlas.
                            if (shidx >= shl2.Length)
                            {
                                SetPixel(s_L0L1Rx_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                SetPixel(s_L1GL1Ry_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                SetPixel(s_L1BL1Rz_locData, ix, iy, iz, loc.width, loc.height, Color.black);

                                if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                {
                                    SetPixel(s_L2_0_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                    SetPixel(s_L2_1_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                    SetPixel(s_L2_2_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                    SetPixel(s_L2_3_locData, ix, iy, iz, loc.width, loc.height, Color.black);
                                }
                            }
                            else
                            {
                                c.r = shl2[shidx][0, 0]; // L0.r
                                c.g = shl2[shidx][1, 0]; // L0.g
                                c.b = shl2[shidx][2, 0]; // L0.b
                                c.a = shl2[shidx][0, 1]; // L1_R.r
                                SetPixel(s_L0L1Rx_locData, ix, iy, iz, loc.width, loc.height, c);

                                c.r = shl2[shidx][1, 1]; // L1_G.r
                                c.g = shl2[shidx][1, 2]; // L1_G.g
                                c.b = shl2[shidx][1, 3]; // L1_G.b
                                c.a = shl2[shidx][0, 2]; // L1_R.g
                                SetPixel(s_L1GL1Ry_locData, ix, iy, iz, loc.width, loc.height, c);

                                c.r = shl2[shidx][2, 1]; // L1_B.r
                                c.g = shl2[shidx][2, 2]; // L1_B.g
                                c.b = shl2[shidx][2, 3]; // L1_B.b
                                c.a = shl2[shidx][0, 3]; // L1_R.b
                                SetPixel(s_L1BL1Rz_locData, ix, iy, iz, loc.width, loc.height, c);

                                if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                {
                                    c.r = shl2[shidx][0, 4];
                                    c.g = shl2[shidx][0, 5];
                                    c.b = shl2[shidx][0, 6];
                                    c.a = shl2[shidx][0, 7];
                                    SetPixel(s_L2_0_locData, ix, iy, iz, loc.width, loc.height, c);

                                    c.r = shl2[shidx][1, 4];
                                    c.g = shl2[shidx][1, 5];
                                    c.b = shl2[shidx][1, 6];
                                    c.a = shl2[shidx][1, 7];
                                    SetPixel(s_L2_1_locData, ix, iy, iz, loc.width, loc.height, c);

                                    c.r = shl2[shidx][2, 4];
                                    c.g = shl2[shidx][2, 5];
                                    c.b = shl2[shidx][2, 6];
                                    c.a = shl2[shidx][2, 7];
                                    SetPixel(s_L2_2_locData, ix, iy, iz, loc.width, loc.height, c);

                                    c.r = shl2[shidx][0, 8];
                                    c.g = shl2[shidx][1, 8];
                                    c.b = shl2[shidx][2, 8];
                                    c.a = 1;
                                    SetPixel(s_L2_3_locData, ix, iy, iz, loc.width, loc.height, c);
                                }
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
                        Debug.Assert(bz < loc.depth || brickIdx == (startIndex + count - kBrickProbeCountTotal), "Location depth exceeds data texture.");
                    }
                }
            }

            loc.TexL0_L1rx.SetPixels(s_L0L1Rx_locData);
            loc.TexL0_L1rx.Apply(false);
            loc.TexL1_G_ry.SetPixels(s_L1GL1Ry_locData);
            loc.TexL1_G_ry.Apply(false);
            loc.TexL1_B_rz.SetPixels(s_L1BL1Rz_locData);
            loc.TexL1_B_rz.Apply(false);

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0.SetPixels(s_L2_0_locData);
                loc.TexL2_0.Apply(false);
                loc.TexL2_1.SetPixels(s_L2_1_locData);
                loc.TexL2_1.Apply(false);
                loc.TexL2_2.SetPixels(s_L2_2_locData);
                loc.TexL2_2.Apply(false);
                loc.TexL2_3.SetPixels(s_L2_3_locData);
                loc.TexL2_3.Apply(false);
            }
        }

        void DerivePoolSizeFromBudget(ProbeVolumeTextureMemoryBudget memoryBudget, out int width, out int height, out int depth)
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
