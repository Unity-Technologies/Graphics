using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using System;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickPool
    {
        protected const int kProbePoolChunkSize = 128;

        [DebuggerDisplay("Chunk ({x}, {y}, {z})")]
        public struct BrickChunkAlloc
        {
            public int x, y, z;

            internal int flattenIndex(int sx, int sy) { return z * (sx * sy) + y * sx + x; }
        }

        public struct DataLocation
        {
            internal Texture TexL0_L1rx;

            internal Texture TexL1_G_ry;
            internal Texture TexL1_B_rz;

            internal Texture TexL2_0;
            internal Texture TexL2_1;
            internal Texture TexL2_2;
            internal Texture TexL2_3;

            internal Texture3D TexValidity;

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

                CoreUtils.Destroy(TexValidity);

                TexL0_L1rx = null;

                TexL1_G_ry = null;
                TexL1_B_rz = null;

                TexL2_0 = null;
                TexL2_1 = null;
                TexL2_2 = null;
                TexL2_3 = null;
                TexValidity = null;
            }
        }

        internal const int kBrickCellCount = 3;
        internal const int kBrickProbeCountPerDim = kBrickCellCount + 1;
        internal const int kBrickProbeCountTotal = kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim;

        internal int estimatedVMemCost { get; private set; }

        internal int MaxAvailablebrickCount => (m_Pool.width * m_Pool.height * m_Pool.depth) / (kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim);

        const int kMaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

        ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        internal DataLocation m_Pool; // internal to access it from blending pool only
        BrickChunkAlloc m_NextFreeChunk;
        Stack<BrickChunkAlloc> m_FreeList;
        int m_AvailableChunkCount;
        int m_Layers;

        protected ProbeVolumeSHBands m_SHBands;

        // Temporary buffers for updating SH textures.
        static DynamicArray<Color> s_L0L1Rx_locData = new DynamicArray<Color>();
        static DynamicArray<Color> s_L1GL1Ry_locData = new DynamicArray<Color>();
        static DynamicArray<Color> s_L1BL1Rz_locData = new DynamicArray<Color>();
        static DynamicArray<byte> s_PackedValidity_locData = new DynamicArray<byte>();

        static DynamicArray<Color> s_L2_0_locData = null;
        static DynamicArray<Color> s_L2_1_locData = null;
        static DynamicArray<Color> s_L2_2_locData = null;
        static DynamicArray<Color> s_L2_3_locData = null;

        internal ProbeBrickPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands)
            : this(memoryBudget, shBands, 1) { }

        protected ProbeBrickPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands, int layers)
        {
            Profiler.BeginSample("Create ProbeBrickPool");
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_MemoryBudget = memoryBudget;
            m_SHBands = shBands;
            m_Layers = layers;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            DerivePoolSizeFromBudget(memoryBudget, out int width, out int height, out int depth);
            m_Pool = CreateDataLocation(width * height * depth, false, shBands, "APV", true, m_Layers, out int estimatedCost);
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
                m_Pool = CreateDataLocation(m_Pool.width * m_Pool.height * m_Pool.depth, false, m_SHBands, "APV", true, m_Layers, out int estimatedCost);
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
            rr.L0_L1rx = m_Pool.TexL0_L1rx as RenderTexture;

            rr.L1_G_ry = m_Pool.TexL1_G_ry as RenderTexture;
            rr.L1_B_rz = m_Pool.TexL1_B_rz as RenderTexture;

            rr.L2_0 = m_Pool.TexL2_0 as RenderTexture;
            rr.L2_1 = m_Pool.TexL2_1 as RenderTexture;
            rr.L2_2 = m_Pool.TexL2_2 as RenderTexture;
            rr.L2_3 = m_Pool.TexL2_3 as RenderTexture;

            rr.Validity = m_Pool.TexValidity;
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

        internal virtual void Deallocate(List<BrickChunkAlloc> allocations)
        {
            m_AvailableChunkCount += allocations.Count;

            foreach (var brick in allocations)
                m_FreeList.Push(brick);
        }

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations, int destStartIndex, ProbeVolumeSHBands bands, int layer = 0)
        {
            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[destStartIndex + i];
                dst.z += layer * kBrickProbeCountPerDim;

                for (int j = 0; j < kBrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(kProbePoolChunkSize * kBrickProbeCountPerDim, source.width - src.x);
                    Graphics.CopyTexture(source.TexL0_L1rx, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL0_L1rx, dst.z + j, 0, dst.x, dst.y);

                    Graphics.CopyTexture(source.TexL1_G_ry, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_G_ry, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.TexL1_B_rz, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexL1_B_rz, dst.z + j, 0, dst.x, dst.y);

                    Graphics.CopyTexture(source.TexValidity, src.z + j, 0, src.x, src.y, width, kBrickProbeCountPerDim, m_Pool.TexValidity, dst.z + j, 0, dst.x, dst.y);

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

        internal static Vector3Int ProbeCountToDataLocSize(int numProbes)
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

        public static Texture CreateDataTexture(int width, int height, int depth, GraphicsFormat format, string name, bool allocateRendertexture, ref int allocatedBytes)
        {
            int elementSize = format == GraphicsFormat.R16G16B16A16_SFloat ? 8 :
                format == GraphicsFormat.R8G8B8A8_UNorm ? 4 : 1;

            Texture texture;
            allocatedBytes += (width * height * depth) * elementSize;
            if (allocateRendertexture)
            {
                texture = new RenderTexture(new RenderTextureDescriptor()
                {
                    width = width,
                    height = height,
                    volumeDepth = depth,
                    graphicsFormat = format,
                    mipCount = 1,
                    enableRandomWrite = true,
                    dimension = TextureDimension.Tex3D,
                    msaaSamples = 1,
                });
            }
            else
                texture = new Texture3D(width, height, depth, format, TextureCreationFlags.None, 1);

            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.name = name;

            if (allocateRendertexture)
                (texture as RenderTexture).Create();
            return texture;
        }

        public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands, string name, out int allocatedBytes)
            => CreateDataLocation(numProbes, compressed, bands, name, false, 1, out allocatedBytes);

        static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands, string name, bool allocateRendertexture, int layers, out int allocatedBytes)
        {
            Vector3Int locSize = ProbeCountToDataLocSize(numProbes);
            int width = locSize.x;
            int height = locSize.y;
            int depth = locSize.z * layers;

            DataLocation loc;
            var primaryFormat = GraphicsFormat.R16G16B16A16_SFloat;
            var graphicFormat = compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

            allocatedBytes = 0;
            loc.TexL0_L1rx = CreateDataTexture(width, height, depth, primaryFormat, $"{name}_TexL0_L1rx", allocateRendertexture, ref allocatedBytes);
            loc.TexL1_G_ry = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL1_G_ry", allocateRendertexture, ref allocatedBytes);
            loc.TexL1_B_rz = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL1_B_rz", allocateRendertexture, ref allocatedBytes);
            loc.TexValidity = CreateDataTexture(width, height, depth, GraphicsFormat.R8_UNorm, $"{name}_Validity", false, ref allocatedBytes) as Texture3D;

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.TexL2_0 = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL2_0", allocateRendertexture, ref allocatedBytes);
                loc.TexL2_1 = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL2_1", allocateRendertexture, ref allocatedBytes);
                loc.TexL2_2 = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL2_2", allocateRendertexture, ref allocatedBytes);
                loc.TexL2_3 = CreateDataTexture(width, height, depth, graphicFormat, $"{name}_TexL2_3", allocateRendertexture, ref allocatedBytes);
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
            s_PackedValidity_locData.Resize(size);

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

        static void SetPixelAlpha(DynamicArray<Color> data, int x, int y, int z, int dataLocWidth, int dataLocHeight, float value)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            data[index].a = value;
        }

        static void SetPixel(DynamicArray<byte> data, int x, int y, int z, int dataLocWidth, int dataLocHeight, byte value)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            data[index] = value;
        }

        static void SetPixel(DynamicArray<float> data, int x, int y, int z, int dataLocWidth, int dataLocHeight, float value)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            data[index] = value;
        }

        static float GetData(DynamicArray<float> data, int x, int y, int z, int dataLocWidth, int dataLocHeight)
        {
            int index = x + dataLocWidth * (y + dataLocHeight * z);
            return data[index];
        }

        static int PackValidity(float[] validity)
        {
            int outputByte = 0;
            for (int i = 0; i < 8; ++i)
            {
                int val = (validity[i] > 0.05f) ? 0 : 1;
                outputByte |= (val << i);
            }
            return outputByte;
        }

        static Vector3Int GetSampleOffset(int i)
        {
            return new Vector3Int(i & 1, (i >> 1) & 1, (i >> 2) & 1);
        }

        internal static unsafe void FillDataLocation(ref DataLocation loc, ProbeVolumeSHBands srcBands, NativeArray<float> shL0L1Data, NativeArray<float> shL2Data, NativeArray<uint> validity, int startIndex, int count, ProbeVolumeSHBands dstBands)
        {
            // NOTE: The SH data arrays passed to this method should be pre-swizzled to the format expected by shader code.
            // TODO: The next step here would be to store de-interleaved, pre-quantized brick data that can be memcopied directly into texture pixeldata

            var inputProbesCount = shL0L1Data.Length / ProbeVolumeAsset.kL0L1ScalarCoefficientsCount;

            // Coefficient constants that end up as black after shader probe data decoding
            var kZZZH = new Color(0f, 0f, 0f, 0.5f);
            var kHHHH = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            int shidx = startIndex;
            int bx = 0, by = 0, bz = 0;

            ValidateTemporaryBuffers(loc, dstBands);

            var shL0L1Ptr = (float*)shL0L1Data.GetUnsafeReadOnlyPtr();
            var validityPtr = (uint*)validity.GetUnsafeReadOnlyPtr();
            var shL2Ptr = (float*)(shL2Data.IsCreated ? shL2Data.GetUnsafeReadOnlyPtr() : default);

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
                            // We fill with encoded black to avoid copying garbage in the final atlas.
                            if (shidx >= inputProbesCount)
                            {
                                SetPixel(s_L0L1Rx_locData, ix, iy, iz, loc.width, loc.height, kZZZH);
                                SetPixel(s_L1GL1Ry_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                SetPixel(s_L1BL1Rz_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                SetPixel(s_PackedValidity_locData, ix, iy, iz, loc.width, loc.height, 0);

                                if (dstBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                {
                                    SetPixel(s_L2_0_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                    SetPixel(s_L2_1_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                    SetPixel(s_L2_2_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                    SetPixel(s_L2_3_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                }
                            }
                            else
                            {
                                var shL0L1ColorPtr = (Color*)(shL0L1Ptr + shidx * ProbeVolumeAsset.kL0L1ScalarCoefficientsCount);
                                SetPixel(s_L0L1Rx_locData, ix, iy, iz, loc.width, loc.height, shL0L1ColorPtr[0]);
                                SetPixel(s_L1GL1Ry_locData, ix, iy, iz, loc.width, loc.height, shL0L1ColorPtr[1]);
                                SetPixel(s_L1BL1Rz_locData, ix, iy, iz, loc.width, loc.height, shL0L1ColorPtr[2]);
                                SetPixel(s_PackedValidity_locData, ix, iy, iz, loc.width, loc.height, ProbeReferenceVolume.Cell.GetValidityNeighMaskFromPacked(validityPtr[shidx]));

                                if (dstBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                {
                                    if (srcBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                                    {
                                        var shL2ColorPtr = (Color*)(shL2Ptr + shidx * ProbeVolumeAsset.kL2ScalarCoefficientsCount);
                                        SetPixel(s_L2_0_locData, ix, iy, iz, loc.width, loc.height, shL2ColorPtr[0]);
                                        SetPixel(s_L2_1_locData, ix, iy, iz, loc.width, loc.height, shL2ColorPtr[1]);
                                        SetPixel(s_L2_2_locData, ix, iy, iz, loc.width, loc.height, shL2ColorPtr[2]);
                                        SetPixel(s_L2_3_locData, ix, iy, iz, loc.width, loc.height, shL2ColorPtr[3]);
                                    }
                                    else
                                    {
                                        // We want L2 output, but only have L0L1 input. Fill with encoded black to preserve L0L1 lighting data.
                                        SetPixel(s_L2_0_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                        SetPixel(s_L2_1_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                        SetPixel(s_L2_2_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                        SetPixel(s_L2_3_locData, ix, iy, iz, loc.width, loc.height, kHHHH);
                                    }
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

            (loc.TexL0_L1rx as Texture3D).SetPixels(s_L0L1Rx_locData);
            (loc.TexL0_L1rx as Texture3D).Apply(false);
            (loc.TexL1_G_ry as Texture3D).SetPixels(s_L1GL1Ry_locData);
            (loc.TexL1_G_ry as Texture3D).Apply(false);
            (loc.TexL1_B_rz as Texture3D).SetPixels(s_L1BL1Rz_locData);
            (loc.TexL1_B_rz as Texture3D).Apply(false);

            loc.TexValidity.SetPixelData<byte>(s_PackedValidity_locData, 0);
            loc.TexValidity.Apply(false);

            if (dstBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                (loc.TexL2_0 as Texture3D).SetPixels(s_L2_0_locData);
                (loc.TexL2_0 as Texture3D).Apply(false);
                (loc.TexL2_1 as Texture3D).SetPixels(s_L2_1_locData);
                (loc.TexL2_1 as Texture3D).Apply(false);
                (loc.TexL2_2 as Texture3D).SetPixels(s_L2_2_locData);
                (loc.TexL2_2 as Texture3D).Apply(false);
                (loc.TexL2_3 as Texture3D).SetPixels(s_L2_3_locData);
                (loc.TexL2_3 as Texture3D).Apply(false);
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

        internal virtual void Cleanup()
        {
            m_Pool.Cleanup();
        }
    }

    internal class ProbeBrickBlendingPool : ProbeBrickPool
    {
        static ComputeShader stateBlendShader;
        static int stateBlendKernel = -1, copyBufferKernel = -1;

        static readonly int _ChunkDim_LerpFactor = Shader.PropertyToID("_ChunkDim_LerpFactor");
        static readonly int _ChunkMapping = Shader.PropertyToID("_ChunkMapping");

        static readonly int _Input_L0_L1Rx = Shader.PropertyToID("_Input_L0_L1Rx");
        static readonly int _Input_L1G_L1Ry = Shader.PropertyToID("_Input_L1G_L1Ry");
        static readonly int _Input_L1B_L1Rz = Shader.PropertyToID("_Input_L1B_L1Rz");
        static readonly int _Input_L2_0 = Shader.PropertyToID("_Input_L2_0");
        static readonly int _Input_L2_1 = Shader.PropertyToID("_Input_L2_1");
        static readonly int _Input_L2_2 = Shader.PropertyToID("_Input_L2_2");
        static readonly int _Input_L2_3 = Shader.PropertyToID("_Input_L2_3");

        static readonly int _Out_L0_L1Rx = Shader.PropertyToID("_Out_L0_L1Rx");
        static readonly int _Out_L1G_L1Ry = Shader.PropertyToID("_Out_L1G_L1Ry");
        static readonly int _Out_L1B_L1Rz = Shader.PropertyToID("_Out_L1B_L1Rz");
        static readonly int _Out_L2_0 = Shader.PropertyToID("_Out_L2_0");
        static readonly int _Out_L2_1 = Shader.PropertyToID("_Out_L2_1");
        static readonly int _Out_L2_2 = Shader.PropertyToID("_Out_L2_2");
        static readonly int _Out_L2_3 = Shader.PropertyToID("_Out_L2_3");

        internal static void Initialize(in ProbeVolumeSystemParameters parameters)
        {
            stateBlendShader = parameters.stateBlendShader;
            stateBlendKernel = stateBlendShader.FindKernel("BlendStates");
        }

        List<uint> m_IndexMapping;
        ComputeBuffer m_ChunkMapping;
        bool m_MappingNeedsReupload = true;
        int m_MappedChunks = 0;

        const uint s_UnmappedChunk = unchecked((uint)-1);

        internal ProbeBrickBlendingPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands)
            : base(memoryBudget, shBands, 2)
        {
            m_IndexMapping = new List<uint>(MaxAvailablebrickCount);
            for (int i = 0; i < MaxAvailablebrickCount; i++)
                m_IndexMapping.Add(s_UnmappedChunk);
            m_ChunkMapping = new ComputeBuffer(m_IndexMapping.Count, sizeof(uint));
            Upload();
        }

        void Upload()
        {
            if (m_MappingNeedsReupload)
            {
                m_ChunkMapping.SetData(m_IndexMapping);
                m_MappingNeedsReupload = false;
            }
        }

        static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        internal void PerformBlending(float factor, ProbeBrickPool dstPool)
        {
            if (m_MappedChunks == 0)
                return;

            Upload();

            stateBlendShader.SetTexture(stateBlendKernel, _Input_L0_L1Rx, m_Pool.TexL0_L1rx);
            stateBlendShader.SetTexture(stateBlendKernel, _Input_L1G_L1Ry, m_Pool.TexL1_G_ry);
            stateBlendShader.SetTexture(stateBlendKernel, _Input_L1B_L1Rz, m_Pool.TexL1_B_rz);

            stateBlendShader.SetTexture(stateBlendKernel, _Out_L0_L1Rx, dstPool.m_Pool.TexL0_L1rx);
            stateBlendShader.SetTexture(stateBlendKernel, _Out_L1G_L1Ry, dstPool.m_Pool.TexL1_G_ry);
            stateBlendShader.SetTexture(stateBlendKernel, _Out_L1B_L1Rz, dstPool.m_Pool.TexL1_B_rz);

            if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                stateBlendShader.EnableKeyword("PROBE_VOLUMES_L2");

                stateBlendShader.SetTexture(stateBlendKernel, _Input_L2_0, m_Pool.TexL2_0);
                stateBlendShader.SetTexture(stateBlendKernel, _Input_L2_1, m_Pool.TexL2_1);
                stateBlendShader.SetTexture(stateBlendKernel, _Input_L2_2, m_Pool.TexL2_2);
                stateBlendShader.SetTexture(stateBlendKernel, _Input_L2_3, m_Pool.TexL2_3);

                stateBlendShader.SetTexture(stateBlendKernel, _Out_L2_0, dstPool.m_Pool.TexL2_0);
                stateBlendShader.SetTexture(stateBlendKernel, _Out_L2_1, dstPool.m_Pool.TexL2_1);
                stateBlendShader.SetTexture(stateBlendKernel, _Out_L2_2, dstPool.m_Pool.TexL2_2);
                stateBlendShader.SetTexture(stateBlendKernel, _Out_L2_3, dstPool.m_Pool.TexL2_3);
            }
            else
                stateBlendShader.DisableKeyword("PROBE_VOLUMES_L2");

            var poolDims = new Vector4(
                GetPoolWidth() / (kProbePoolChunkSize * kBrickProbeCountPerDim),
                GetPoolHeight() / kBrickProbeCountPerDim,
                dstPool.GetPoolWidth(),
                dstPool.GetPoolHeight());

            var chunkSize = new Vector4(kProbePoolChunkSize * kBrickProbeCountPerDim, kBrickProbeCountPerDim, kBrickProbeCountPerDim, 0.0f);

            stateBlendShader.SetBuffer(stateBlendKernel, _ChunkMapping, m_ChunkMapping);
            stateBlendShader.SetVector(_ChunkDim_LerpFactor, new Vector4(chunkSize.x, chunkSize.y, chunkSize.z, factor));
            stateBlendShader.SetVector("_PoolDims", poolDims);

            const int numthreads = 8, numthreadsZ = 4;
            int threadX = DivRoundUp(m_Pool.width,  numthreads);
            int threadY = DivRoundUp(m_Pool.height, numthreads);
            int threadZ = DivRoundUp(m_Pool.depth << 1, numthreadsZ);
            stateBlendShader.Dispatch(stateBlendKernel, threadX, threadY, threadZ);
        }

        int GetChunkIndex(BrickChunkAlloc chunk)
        {
            Vector3Int chunkIndex = new Vector3Int(
                chunk.x / (kProbePoolChunkSize * kBrickProbeCountPerDim),
                chunk.y / kBrickProbeCountPerDim,
                chunk.z / kBrickProbeCountPerDim);

            Vector2Int chunkCount = new Vector2Int(
                GetPoolWidth() / (kProbePoolChunkSize * kBrickProbeCountPerDim),
                GetPoolHeight() / kBrickProbeCountPerDim);

            return chunkIndex.z * chunkCount.x * chunkCount.y + chunkIndex.y * chunkCount.x + chunkIndex.x;
        }

        internal void MapChunk(BrickChunkAlloc chunk, int dst)
        {
            int src = GetChunkIndex(chunk);
            Debug.Assert(m_IndexMapping[src] == s_UnmappedChunk);

            m_MappingNeedsReupload = true;
            m_IndexMapping[src] = (uint)dst;

            m_MappedChunks++;
        }

        internal override void Deallocate(List<BrickChunkAlloc> allocations)
        {
            base.Deallocate(allocations);
            m_MappingNeedsReupload |= allocations.Count != 0;
            foreach (var brick in allocations)
            {
                int index = GetChunkIndex(brick);
                Debug.Assert(m_IndexMapping[index] != s_UnmappedChunk);
                m_IndexMapping[index] = s_UnmappedChunk;
            }

            m_MappedChunks -= allocations.Count;
        }

        internal override void Cleanup()
        {
            base.Cleanup();
            m_ChunkMapping.Release();
        }
    }
}
