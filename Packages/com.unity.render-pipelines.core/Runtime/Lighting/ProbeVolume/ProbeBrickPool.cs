using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.Experimental.Rendering;
using Cell = UnityEngine.Rendering.ProbeReferenceVolume.Cell;
using CellStreamingScratchBuffer = UnityEngine.Rendering.ProbeReferenceVolume.CellStreamingScratchBuffer;
using CellStreamingScratchBufferLayout = UnityEngine.Rendering.ProbeReferenceVolume.CellStreamingScratchBufferLayout;

namespace UnityEngine.Rendering
{
    internal class ProbeBrickPool
    {
        /// <summary>
        /// Allocates all SH data texture arrays (L0/L1, optional L2), validity, sky occlusion,
        /// sky shading direction, probe occlusion, and rendering layer textures based on the
        /// memory budget and enabled feature set.
        /// </summary>
        static readonly ProfilerMarker k_CreateProbeBrickPool =
            new ProfilerMarker(ProfilerCategory.Render, "Create ProbeBrickPool",
                MarkerFlags.VerbosityAdvanced);

        internal static readonly int _Out_L0_L1Rx = Shader.PropertyToID("_Out_L0_L1Rx");
        internal static readonly int _Out_L1G_L1Ry = Shader.PropertyToID("_Out_L1G_L1Ry");
        internal static readonly int _Out_L1B_L1Rz = Shader.PropertyToID("_Out_L1B_L1Rz");
        internal static readonly int _Out_Shared = Shader.PropertyToID("_Out_Shared");
        internal static readonly int _Out_ProbeOcclusion = Shader.PropertyToID("_Out_ProbeOcclusion");
        internal static readonly int _Out_SkyOcclusionL0L1 = Shader.PropertyToID("_Out_SkyOcclusionL0L1");
        internal static readonly int _Out_SkyShadingDirectionIndices = Shader.PropertyToID("_Out_SkyShadingDirectionIndices");
        internal static readonly int _Out_L2_0 = Shader.PropertyToID("_Out_L2_0");
        internal static readonly int _Out_L2_1 = Shader.PropertyToID("_Out_L2_1");
        internal static readonly int _Out_L2_2 = Shader.PropertyToID("_Out_L2_2");
        internal static readonly int _Out_L2_3 = Shader.PropertyToID("_Out_L2_3");
        internal static readonly int _ProbeVolumeScratchBufferLayout = Shader.PropertyToID(nameof(ProbeReferenceVolume.CellStreamingScratchBufferLayout));
        internal static readonly int _ProbeVolumeScratchBuffer = Shader.PropertyToID("_ScratchBuffer");

        internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        const int k_ChunkSizeInBricks = 128;

        [DebuggerDisplay("Chunk ({x}, {y}, {z})")]
        public struct BrickChunkAlloc
        {
            public int x, y, z;

            internal int FlattenIndex(int sx, int sy) { return z * (sx * sy) + y * sx + x; }
        }

        public struct DataLocation
        {
            internal Texture m_TexL0L1rx;

            internal Texture m_TexL1GRy;
            internal Texture m_TexL1BRz;

            internal Texture m_TexL20;
            internal Texture m_TexL21;
            internal Texture m_TexL22;
            internal Texture m_TexL23;

            internal Texture m_TexProbeOcclusion;

            internal Texture m_TexValidity;
            internal Texture m_TexSkyOcclusion;
            internal Texture m_TexSkyShadingDirectionIndices;

            internal int m_Width;
            internal int m_Height;
            internal int m_Depth;

            internal void Cleanup()
            {
                CoreUtils.Destroy(m_TexL0L1rx);

                CoreUtils.Destroy(m_TexL1GRy);
                CoreUtils.Destroy(m_TexL1BRz);

                CoreUtils.Destroy(m_TexL20);
                CoreUtils.Destroy(m_TexL21);
                CoreUtils.Destroy(m_TexL22);
                CoreUtils.Destroy(m_TexL23);

                CoreUtils.Destroy(m_TexProbeOcclusion);

                CoreUtils.Destroy(m_TexValidity);
                CoreUtils.Destroy(m_TexSkyOcclusion);
                CoreUtils.Destroy(m_TexSkyShadingDirectionIndices);

                m_TexL0L1rx = null;

                m_TexL1GRy = null;
                m_TexL1BRz = null;

                m_TexL20 = null;
                m_TexL21 = null;
                m_TexL22 = null;
                m_TexL23 = null;
                m_TexProbeOcclusion = null;
                m_TexValidity = null;
                m_TexSkyOcclusion = null;
                m_TexSkyShadingDirectionIndices = null;
            }
        }

        internal const int k_BrickCellCount = 3;
        internal const int k_BrickProbeCountPerDim = k_BrickCellCount + 1;
        internal const int k_BrickProbeCountTotal = k_BrickProbeCountPerDim * k_BrickProbeCountPerDim * k_BrickProbeCountPerDim;
        internal const int k_ChunkProbeCountPerDim = k_ChunkSizeInBricks * k_BrickProbeCountPerDim;

        internal int estimatedVMemCost { get; private set; }

        const int k_MaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

        internal DataLocation m_Pool; // internal to access it from blending pool only
        BrickChunkAlloc m_NextFreeChunk;
        readonly Stack<BrickChunkAlloc> m_FreeList;
        int m_AvailableChunkCount;

        readonly ProbeVolumeSHBands m_SHBands;
        readonly bool m_ContainsValidity;
        bool m_ContainsProbeOcclusion;
        bool m_ContainsRenderingLayers;
        bool m_ContainsSkyOcclusion;
        bool m_ContainsSkyShadingDirection;

        static ComputeShader s_DataUploadCS;
        static int s_DataUploadKernel;
        static ComputeShader s_DataUploadL2CS;
        static int s_DataUploadL2Kernel;
        static LocalKeyword s_DataUploadShared;
        static LocalKeyword s_DataUploadProbeOcclusion;
        static LocalKeyword s_DataUploadSkyOcclusion;
        static LocalKeyword s_DataUploadSkyShadingDirection;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetStaticsOnLoad()
        {
            s_DataUploadCS = null;
            s_DataUploadKernel = -1;
            s_DataUploadL2CS = null;
            s_DataUploadL2Kernel = -1;
            s_DataUploadShared = default;
            s_DataUploadProbeOcclusion = default;
            s_DataUploadSkyOcclusion = default;
            s_DataUploadSkyShadingDirection = default;
        }
#endif

        internal static void Initialize()
        {
            if (!SystemInfo.supportsComputeShaders)
                return;

            s_DataUploadCS = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeRuntimeResources>()?.probeVolumeUploadDataCS;
            s_DataUploadL2CS = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeRuntimeResources>()?.probeVolumeUploadDataL2CS;

            if (s_DataUploadCS != null)
            {
                s_DataUploadKernel = s_DataUploadCS ? s_DataUploadCS.FindKernel("UploadData") : -1;
                s_DataUploadShared = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SHARED_DATA");
                s_DataUploadProbeOcclusion = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_PROBE_OCCLUSION");
                s_DataUploadSkyOcclusion = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SKY_OCCLUSION");
                s_DataUploadSkyShadingDirection = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SKY_SHADING_DIRECTION");
            }

            if (s_DataUploadL2CS != null)
            {
                s_DataUploadL2Kernel = s_DataUploadL2CS ? s_DataUploadL2CS.FindKernel("UploadDataL2") : -1;
            }
        }

        internal Texture GetValidityTexture()
        {
            return m_Pool.m_TexValidity;
        }

        internal Texture GetSkyOcclusionTexture()
        {
            return m_Pool.m_TexSkyOcclusion;
        }

        internal Texture GetSkyShadingDirectionIndicesTexture()
        {
            return m_Pool.m_TexSkyShadingDirectionIndices;
        }

        internal Texture GetProbeOcclusionTexture()
        {
            return m_Pool.m_TexProbeOcclusion;
        }

        internal ProbeBrickPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands, bool allocateValidityData = false, bool allocateRenderingLayerData = false, bool allocateSkyOcclusion = false, bool allocateSkyShadingData = false, bool allocateProbeOcclusionData = false)
        {
            using var _ = k_CreateProbeBrickPool.Auto();

            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

            m_SHBands = shBands;
            m_ContainsValidity = allocateValidityData;
            m_ContainsProbeOcclusion = allocateProbeOcclusionData;
            m_ContainsRenderingLayers = allocateRenderingLayerData;
            m_ContainsSkyOcclusion = allocateSkyOcclusion;
            m_ContainsSkyShadingDirection = allocateSkyShadingData;

            m_FreeList = new Stack<BrickChunkAlloc>(256);

            DerivePoolSizeFromBudget(memoryBudget, out int width, out int height, out int depth);
            AllocatePool(width, height, depth);

            m_AvailableChunkCount = (m_Pool.m_Width / (k_ChunkSizeInBricks * k_BrickProbeCountPerDim)) * (m_Pool.m_Height / k_BrickProbeCountPerDim) * (m_Pool.m_Depth / k_BrickProbeCountPerDim);
        }

        internal void AllocatePool(int width, int height, int depth)
        {
            m_Pool = CreateDataLocation(width * height * depth, false, m_SHBands, "APV", true,
                m_ContainsValidity, m_ContainsRenderingLayers, m_ContainsSkyOcclusion, m_ContainsSkyShadingDirection, m_ContainsProbeOcclusion, out int estimatedCost);
            estimatedVMemCost = estimatedCost;
        }

        public int GetRemainingChunkCount()
        {
            return m_AvailableChunkCount;
        }

        internal void EnsureTextureValidity()
        {
            // We assume that if a texture is null, all of them are. In any case we reboot them altogether.
            if (m_Pool.m_TexL0L1rx == null)
            {
                m_Pool.Cleanup();
                AllocatePool(m_Pool.m_Width, m_Pool.m_Height, m_Pool.m_Depth);
            }
        }

        internal bool EnsureTextureValidity(bool renderingLayers, bool skyOcclusion, bool skyDirection, bool probeOcclusion)
        {
            if (m_ContainsRenderingLayers != renderingLayers || m_ContainsSkyOcclusion != skyOcclusion || m_ContainsSkyShadingDirection != skyDirection || m_ContainsProbeOcclusion != probeOcclusion)
            {
                m_Pool.Cleanup();

                m_ContainsRenderingLayers = renderingLayers;
                m_ContainsSkyOcclusion = skyOcclusion;
                m_ContainsSkyShadingDirection = skyDirection;
                m_ContainsProbeOcclusion = probeOcclusion;
                AllocatePool(m_Pool.m_Width, m_Pool.m_Height, m_Pool.m_Depth);
                return false;
            }
            return true;
        }

        internal static int GetChunkSizeInBrickCount() { return k_ChunkSizeInBricks; }
        internal static int GetChunkSizeInProbeCount() { return k_ChunkSizeInBricks * k_BrickProbeCountTotal; }

        internal int GetPoolWidth() { return m_Pool.m_Width; }
        internal int GetPoolHeight() { return m_Pool.m_Height; }
        internal Vector3Int GetPoolDimensions() { return new Vector3Int(m_Pool.m_Width, m_Pool.m_Height, m_Pool.m_Depth); }
        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            rr.L0_L1rx = m_Pool.m_TexL0L1rx as RenderTexture;

            rr.L1_G_ry = m_Pool.m_TexL1GRy as RenderTexture;
            rr.L1_B_rz = m_Pool.m_TexL1BRz as RenderTexture;

            rr.L2_0 = m_Pool.m_TexL20 as RenderTexture;
            rr.L2_1 = m_Pool.m_TexL21 as RenderTexture;
            rr.L2_2 = m_Pool.m_TexL22 as RenderTexture;
            rr.L2_3 = m_Pool.m_TexL23 as RenderTexture;

            rr.ProbeOcclusion = m_Pool.m_TexProbeOcclusion as RenderTexture;

            rr.Validity = m_Pool.m_TexValidity as RenderTexture;
            rr.SkyOcclusionL0L1 = m_Pool.m_TexSkyOcclusion as RenderTexture;
            rr.SkyShadingDirectionIndices = m_Pool.m_TexSkyShadingDirectionIndices as RenderTexture;
        }

        internal void Clear()
        {
            m_FreeList.Clear();
            m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;
        }

        internal static int GetChunkCount(int brickCount)
        {
            int chunkSize = k_ChunkSizeInBricks;
            return (brickCount + chunkSize - 1) / chunkSize;
        }

        internal bool Allocate(int numberOfBrickChunks, List<BrickChunkAlloc> outAllocations, bool ignoreErrorLog)
        {
            while (m_FreeList.Count > 0 && numberOfBrickChunks > 0)
            {
                outAllocations.Add(m_FreeList.Pop());
                numberOfBrickChunks--;
                m_AvailableChunkCount--;
            }

            for (uint i = 0; i < numberOfBrickChunks; i++)
            {
                if (m_NextFreeChunk.z >= m_Pool.m_Depth)
                {
                    // During baking we know we can hit this when trying to do dilation of all cells at the same time.
                    // We don't want controlled error message spam during baking so we ignore it.
                    // In theory this should never happen with proper streaming/defrag but we keep the message just in case otherwise.
                    if (!ignoreErrorLog)
                        Debug.LogError("Cannot allocate more brick chunks, probe volume brick pool is full.");

                    Deallocate(outAllocations);
                    outAllocations.Clear();
                    return false; // failure case, pool is full
                }

                outAllocations.Add(m_NextFreeChunk);
                m_AvailableChunkCount--;

                m_NextFreeChunk.x += k_ChunkSizeInBricks * k_BrickProbeCountPerDim;
                if (m_NextFreeChunk.x >= m_Pool.m_Width)
                {
                    m_NextFreeChunk.x = 0;
                    m_NextFreeChunk.y += k_BrickProbeCountPerDim;
                    if (m_NextFreeChunk.y >= m_Pool.m_Height)
                    {
                        m_NextFreeChunk.y = 0;
                        m_NextFreeChunk.z += k_BrickProbeCountPerDim;
                    }
                }
            }

            return true;
        }

        internal void Deallocate(List<BrickChunkAlloc> allocations)
        {
            m_AvailableChunkCount += allocations.Count;

            foreach (var brick in allocations)
            {
                m_FreeList.Push(brick);
            }
        }

        internal void Update(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations, int destStartIndex, ProbeVolumeSHBands bands)
        {
            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[destStartIndex + i];

                for (int j = 0; j < k_BrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(k_ChunkSizeInBricks * k_BrickProbeCountPerDim, source.m_Width - src.x);
                    Graphics.CopyTexture(source.m_TexL0L1rx, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL0L1rx, dst.z + j, 0, dst.x, dst.y);

                    Graphics.CopyTexture(source.m_TexL1GRy, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL1GRy, dst.z + j, 0, dst.x, dst.y);
                    Graphics.CopyTexture(source.m_TexL1BRz, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL1BRz, dst.z + j, 0, dst.x, dst.y);

                    if (m_ContainsValidity)
                        Graphics.CopyTexture(source.m_TexValidity, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexValidity, dst.z + j, 0, dst.x, dst.y);

                    if (m_ContainsSkyOcclusion)
                    {
                        Graphics.CopyTexture(source.m_TexSkyOcclusion, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexSkyOcclusion, dst.z + j, 0, dst.x, dst.y);
                        if (m_ContainsSkyShadingDirection)
                        {
                            Graphics.CopyTexture(source.m_TexSkyShadingDirectionIndices, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexSkyShadingDirectionIndices, dst.z + j, 0, dst.x, dst.y);
                        }
                    }

                    if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        Graphics.CopyTexture(source.m_TexL20, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL20, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.m_TexL21, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL21, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.m_TexL22, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL22, dst.z + j, 0, dst.x, dst.y);
                        Graphics.CopyTexture(source.m_TexL23, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexL23, dst.z + j, 0, dst.x, dst.y);
                    }

                    if (m_ContainsProbeOcclusion)
                    {
                        Graphics.CopyTexture(source.m_TexProbeOcclusion, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexProbeOcclusion, dst.z + j, 0, dst.x, dst.y);
                    }
                }
            }
        }

        internal void Update(CommandBuffer cmd, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout,
            List<BrickChunkAlloc> dstLocations, bool updateSharedData, Texture validityTexture, ProbeVolumeSHBands bands,
            bool skyOcclusion, Texture skyOcclusionTexture, bool skyShadingDirections, Texture skyShadingDirectionsTexture, bool probeOcclusion)
        {
            using (new ProfilingScope(cmd, CoreProfilingSamplers.APVDiskStreamingUpdatePool, m_Pool.m_TexL0L1rx))
            {
                int chunkCount = dstLocations.Count;

                cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_L0_L1Rx, m_Pool.m_TexL0L1rx);
                cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_L1G_L1Ry, m_Pool.m_TexL1GRy);
                cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_L1B_L1Rz, m_Pool.m_TexL1BRz);

                if (updateSharedData)
                {
                    cmd.EnableKeyword(s_DataUploadCS, s_DataUploadShared);
                    cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_Shared, validityTexture);

                    if (skyOcclusion)
                    {
                        cmd.EnableKeyword(s_DataUploadCS, s_DataUploadSkyOcclusion);
                        cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_SkyOcclusionL0L1, skyOcclusionTexture);
                        if (skyShadingDirections)
                        {
                            cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_SkyShadingDirectionIndices, skyShadingDirectionsTexture);
                            cmd.EnableKeyword(s_DataUploadCS, s_DataUploadSkyShadingDirection);
                        }
                        else
                            cmd.DisableKeyword(s_DataUploadCS, s_DataUploadSkyShadingDirection);
                    }
                }
                else
                {
                    cmd.DisableKeyword(s_DataUploadCS, s_DataUploadShared);
                    cmd.DisableKeyword(s_DataUploadCS, s_DataUploadSkyOcclusion);
                    cmd.DisableKeyword(s_DataUploadCS, s_DataUploadSkyShadingDirection);
                }

                if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    cmd.SetComputeTextureParam(s_DataUploadL2CS, s_DataUploadL2Kernel, _Out_L2_0, m_Pool.m_TexL20);
                    cmd.SetComputeTextureParam(s_DataUploadL2CS, s_DataUploadL2Kernel, _Out_L2_1, m_Pool.m_TexL21);
                    cmd.SetComputeTextureParam(s_DataUploadL2CS, s_DataUploadL2Kernel, _Out_L2_2, m_Pool.m_TexL22);
                    cmd.SetComputeTextureParam(s_DataUploadL2CS, s_DataUploadL2Kernel, _Out_L2_3, m_Pool.m_TexL23);
                }

                if (probeOcclusion)
                {
                    cmd.EnableKeyword(s_DataUploadCS, s_DataUploadProbeOcclusion);
                    cmd.SetComputeTextureParam(s_DataUploadCS, s_DataUploadKernel, _Out_ProbeOcclusion, m_Pool.m_TexProbeOcclusion);
                }
                else
                {
                    cmd.DisableKeyword(s_DataUploadCS, s_DataUploadProbeOcclusion);
                }

                const int numthreads = 64;
                const int probePerThread = 4; // We can upload 4 probes per thread in the current shader.
                int threadX = DivRoundUp(k_ChunkSizeInBricks * k_BrickProbeCountTotal / probePerThread, numthreads);

                ConstantBuffer.Push(cmd, layout, s_DataUploadCS, _ProbeVolumeScratchBufferLayout);
                cmd.SetComputeBufferParam(s_DataUploadCS, s_DataUploadKernel, _ProbeVolumeScratchBuffer, dataBuffer.buffer);
                cmd.DispatchCompute(s_DataUploadCS, s_DataUploadKernel, threadX, 1, chunkCount);

                if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    ConstantBuffer.Push(cmd, layout, s_DataUploadL2CS, _ProbeVolumeScratchBufferLayout);
                    cmd.SetComputeBufferParam(s_DataUploadL2CS, s_DataUploadL2Kernel, _ProbeVolumeScratchBuffer, dataBuffer.buffer);
                    cmd.DispatchCompute(s_DataUploadL2CS, s_DataUploadL2Kernel, threadX, 1, chunkCount);
                }
            }
        }

        internal void UpdateValidity(DataLocation source, List<BrickChunkAlloc> srcLocations, List<BrickChunkAlloc> dstLocations, int destStartIndex)
        {
            Debug.Assert(m_ContainsValidity);

            for (int i = 0; i < srcLocations.Count; i++)
            {
                BrickChunkAlloc src = srcLocations[i];
                BrickChunkAlloc dst = dstLocations[destStartIndex + i];

                for (int j = 0; j < k_BrickProbeCountPerDim; j++)
                {
                    int width = Mathf.Min(k_ChunkSizeInBricks * k_BrickProbeCountPerDim, source.m_Width - src.x);
                    Graphics.CopyTexture(source.m_TexValidity, src.z + j, 0, src.x, src.y, width, k_BrickProbeCountPerDim, m_Pool.m_TexValidity, dst.z + j, 0, dst.x, dst.y);
                }
            }
        }

        internal static Vector3Int ProbeCountToDataLocSize(int numProbes)
        {
            Debug.Assert(numProbes != 0);
            Debug.Assert(numProbes % k_BrickProbeCountTotal == 0);

            int numBricks = numProbes / k_BrickProbeCountTotal;
            int poolWidth = k_MaxPoolWidth / k_BrickProbeCountPerDim;

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

            width *= k_BrickProbeCountPerDim;
            height *= k_BrickProbeCountPerDim;
            depth *= k_BrickProbeCountPerDim;

            return new Vector3Int(width, height, depth);
        }

        static int EstimateMemoryCost(int width, int height, int depth, GraphicsFormat format)
        {
            int elementSize = format == GraphicsFormat.R16G16B16A16_SFloat ? 8 :
                format == GraphicsFormat.R8G8B8A8_UNorm ? 4 : 1;
            return (width * height * depth) * elementSize;
        }

        // Only computes the cost of textures allocated by the blending pool
        internal static int EstimateMemoryCostForBlending(ProbeVolumeTextureMemoryBudget memoryBudget, bool compressed, ProbeVolumeSHBands bands)
        {
            if (memoryBudget == 0)
                return 0;

            DerivePoolSizeFromBudget(memoryBudget, out int width, out int height, out int depth);
            Vector3Int locSize = ProbeCountToDataLocSize(width * height * depth);
            width = locSize.x;
            height = locSize.y;
            depth = locSize.z;

            int allocatedBytes = 0;
            var L0Format = GraphicsFormat.R16G16B16A16_SFloat;
            var L1L2Format = compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

            allocatedBytes += EstimateMemoryCost(width, height, depth, L0Format);
            allocatedBytes += EstimateMemoryCost(width, height, depth, L1L2Format) * 2;

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                allocatedBytes += EstimateMemoryCost(width, height, depth, L1L2Format) * 3;

            return allocatedBytes;
        }

        public static Texture CreateDataTexture(int width, int height, int depth, GraphicsFormat format, string name, bool allocateRendertexture, ref int allocatedBytes)
        {
            allocatedBytes += EstimateMemoryCost(width, height, depth, format);

            Texture texture;
            if (allocateRendertexture)
            {
                texture = new RenderTexture(new RenderTextureDescriptor
                {
                    width = width,
                    height = height,
                    volumeDepth = depth,
                    graphicsFormat = format,
                    mipCount = 1,
                    enableRandomWrite = SystemInfo.supportsComputeShaders,
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

        public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands bands, string name, bool allocateRendertexture,
            bool allocateValidityData, bool allocateRenderingLayers, bool allocateSkyOcclusionData, bool allocateSkyShadingDirectionData, bool allocateProbeOcclusionData, out int allocatedBytes)
        {
            Vector3Int locSize = ProbeCountToDataLocSize(numProbes);
            int width = locSize.x;
            int height = locSize.y;
            int depth = locSize.z;

            DataLocation loc;
            var L0Format = GraphicsFormat.R16G16B16A16_SFloat;
            var L1L2Format = compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

            var validityFormat = allocateRenderingLayers ?
                // for 32 bits we use a float format but it's an uint
                GraphicsFormat.R32_SFloat :
                // NOTE: Platforms that do not support Sample nor LoadStore for R8_UNorm need to fallback to RGBA8_UNorm since that format should be supported for both (e.g. GLES3.x)
                SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Sample | GraphicsFormatUsage.LoadStore) ? GraphicsFormat.R8_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

            allocatedBytes = 0;
            loc.m_TexL0L1rx = CreateDataTexture(width, height, depth, L0Format, $"{name}_TexL0_L1rx", allocateRendertexture, ref allocatedBytes);
            loc.m_TexL1GRy = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL1_G_ry", allocateRendertexture, ref allocatedBytes);
            loc.m_TexL1BRz = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL1_B_rz", allocateRendertexture, ref allocatedBytes);

            if (allocateValidityData)
                loc.m_TexValidity = CreateDataTexture(width, height, depth, validityFormat, $"{name}_Validity", allocateRendertexture, ref allocatedBytes);
            else
                loc.m_TexValidity = null;

            if (allocateSkyOcclusionData)
                loc.m_TexSkyOcclusion = CreateDataTexture(width, height, depth, GraphicsFormat.R16G16B16A16_SFloat, $"{name}_SkyOcclusion", allocateRendertexture, ref allocatedBytes);
            else
                loc.m_TexSkyOcclusion = null;

            if (allocateSkyShadingDirectionData)
                loc.m_TexSkyShadingDirectionIndices = CreateDataTexture(width, height, depth, GraphicsFormat.R8_UNorm, $"{name}_SkyShadingDirectionIndices", allocateRendertexture, ref allocatedBytes);
            else
                loc.m_TexSkyShadingDirectionIndices = null;

            if (allocateProbeOcclusionData)
                loc.m_TexProbeOcclusion = CreateDataTexture(width, height, depth, GraphicsFormat.R8G8B8A8_UNorm, $"{name}_ProbeOcclusion", allocateRendertexture, ref allocatedBytes);
            else
                loc.m_TexProbeOcclusion = null;

            if (bands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                loc.m_TexL20 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_0", allocateRendertexture, ref allocatedBytes);
                loc.m_TexL21 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_1", allocateRendertexture, ref allocatedBytes);
                loc.m_TexL22 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_2", allocateRendertexture, ref allocatedBytes);
                loc.m_TexL23 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_3", allocateRendertexture, ref allocatedBytes);
            }
            else
            {
                loc.m_TexL20 = null;
                loc.m_TexL21 = null;
                loc.m_TexL22 = null;
                loc.m_TexL23 = null;
            }

            loc.m_Width = width;
            loc.m_Height = height;
            loc.m_Depth = depth;

            return loc;
        }

        static void DerivePoolSizeFromBudget(ProbeVolumeTextureMemoryBudget memoryBudget, out int width, out int height, out int depth)
        {
            // TODO: This is fairly simplistic for now and relies on the enum to have the value set to the desired numbers,
            // might change the heuristic later on.
            width = (int)memoryBudget;
            height = (int)memoryBudget;
            depth = k_BrickProbeCountPerDim;
        }

        internal void Cleanup()
        {
            m_Pool.Cleanup();
        }
    }

    internal class ProbeBrickBlendingPool
    {
        static ComputeShader s_StateBlendShader;
        static int s_ScenarioBlendingKernel = -1;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetStaticsOnLoad()
        {
            s_StateBlendShader = null;
            s_ScenarioBlendingKernel = -1;
        }
#endif

        static readonly int _PoolDim_LerpFactor = Shader.PropertyToID("_PoolDim_LerpFactor");
        static readonly int _ChunkList = Shader.PropertyToID("_ChunkList");

        static readonly int _State0_L0_L1Rx = Shader.PropertyToID("_State0_L0_L1Rx");
        static readonly int _State0_L1G_L1Ry = Shader.PropertyToID("_State0_L1G_L1Ry");
        static readonly int _State0_L1B_L1Rz = Shader.PropertyToID("_State0_L1B_L1Rz");
        static readonly int _State0_L2_0 = Shader.PropertyToID("_State0_L2_0");
        static readonly int _State0_L2_1 = Shader.PropertyToID("_State0_L2_1");
        static readonly int _State0_L2_2 = Shader.PropertyToID("_State0_L2_2");
        static readonly int _State0_L2_3 = Shader.PropertyToID("_State0_L2_3");
        static readonly int _State0_ProbeOcclusion = Shader.PropertyToID("_State0_ProbeOcclusion");

        static readonly int _State1_L0_L1Rx = Shader.PropertyToID("_State1_L0_L1Rx");
        static readonly int _State1_L1G_L1Ry = Shader.PropertyToID("_State1_L1G_L1Ry");
        static readonly int _State1_L1B_L1Rz = Shader.PropertyToID("_State1_L1B_L1Rz");
        static readonly int _State1_L2_0 = Shader.PropertyToID("_State1_L2_0");
        static readonly int _State1_L2_1 = Shader.PropertyToID("_State1_L2_1");
        static readonly int _State1_L2_2 = Shader.PropertyToID("_State1_L2_2");
        static readonly int _State1_L2_3 = Shader.PropertyToID("_State1_L2_3");
        static readonly int _State1_ProbeOcclusion = Shader.PropertyToID("_State1_ProbeOcclusion");

        internal static void Initialize()
        {
            if (SystemInfo.supportsComputeShaders)
            {
                s_StateBlendShader = GraphicsSettings.GetRenderPipelineSettings<ProbeVolumeRuntimeResources>()?.probeVolumeBlendStatesCS;
                s_ScenarioBlendingKernel = s_StateBlendShader ? s_StateBlendShader.FindKernel("BlendScenarios") : -1;
            }
        }

        Vector4[] m_ChunkList;
        int m_MappedChunks;

        ProbeBrickPool m_State0, m_State1;
        readonly ProbeVolumeTextureMemoryBudget m_MemoryBudget;
        readonly ProbeVolumeSHBands m_ShBands;
        readonly bool m_ProbeOcclusion;

        internal bool isAllocated => m_State0 != null;
        internal int estimatedVMemCost
        {
            get
            {
                if (!ProbeReferenceVolume.instance.supportScenarioBlending)
                    return 0;
                if (isAllocated)
                    return m_State0.estimatedVMemCost + m_State1.estimatedVMemCost;
                return ProbeBrickPool.EstimateMemoryCostForBlending(m_MemoryBudget, false, m_ShBands) * 2;
            }
        }

        internal int GetPoolWidth() { return m_State0.m_Pool.m_Width; }
        internal int GetPoolHeight() { return m_State0.m_Pool.m_Height; }
        internal int GetPoolDepth() { return m_State0.m_Pool.m_Depth; }

        internal ProbeBrickBlendingPool(ProbeVolumeBlendingTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands, bool probeOcclusion)
        {
            // Casting to other memory budget struct works cause it's casted to int in the end anyway
            m_MemoryBudget = (ProbeVolumeTextureMemoryBudget)memoryBudget;
            m_ShBands = shBands;
            m_ProbeOcclusion = probeOcclusion;
        }

        internal void AllocateResourcesIfNeeded()
        {
            if (isAllocated)
                return;

            m_State0 = new ProbeBrickPool(m_MemoryBudget, m_ShBands, allocateProbeOcclusionData: m_ProbeOcclusion);
            m_State1 = new ProbeBrickPool(m_MemoryBudget, m_ShBands, allocateProbeOcclusionData: m_ProbeOcclusion);

            int maxAvailablebrickCount = (GetPoolWidth() / ProbeBrickPool.k_ChunkProbeCountPerDim)
                                       * (GetPoolHeight() / ProbeBrickPool.k_BrickProbeCountPerDim)
                                       * (GetPoolDepth() / ProbeBrickPool.k_BrickProbeCountPerDim);

            m_ChunkList = new Vector4[maxAvailablebrickCount];
            m_MappedChunks = 0;
        }

        internal void Update(ProbeBrickPool.DataLocation source, List<ProbeBrickPool.BrickChunkAlloc> srcLocations, List<ProbeBrickPool.BrickChunkAlloc> dstLocations, int destStartIndex, ProbeVolumeSHBands bands, int state)
        {
            (state == 0 ? m_State0 : m_State1).Update(source, srcLocations, dstLocations, destStartIndex, bands);
        }

        internal void Update(CommandBuffer cmd, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout,
            List<ProbeBrickPool.BrickChunkAlloc> dstLocations, ProbeVolumeSHBands bands, int state, Texture validityTexture,
            bool skyOcclusion, Texture skyOcclusionTexture, bool skyShadingDirections, Texture skyShadingDirectionsTexture, bool probeOcclusion)
        {
            bool updateShared = state == 0 ? true : false;

            (state == 0 ? m_State0 : m_State1).Update(cmd, dataBuffer, layout, dstLocations,
                updateShared, validityTexture, bands, updateShared && skyOcclusion, skyOcclusionTexture,
                updateShared && skyShadingDirections, skyShadingDirectionsTexture, probeOcclusion);
        }

        internal void PerformBlending(CommandBuffer cmd, float factor, ProbeBrickPool dstPool)
        {
            if (m_MappedChunks == 0)
                return;

            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L0_L1Rx, m_State0.m_Pool.m_TexL0L1rx);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L1G_L1Ry, m_State0.m_Pool.m_TexL1GRy);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L1B_L1Rz, m_State0.m_Pool.m_TexL1BRz);

            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L0_L1Rx, m_State1.m_Pool.m_TexL0L1rx);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L1G_L1Ry, m_State1.m_Pool.m_TexL1GRy);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L1B_L1Rz, m_State1.m_Pool.m_TexL1BRz);

            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L0_L1Rx, dstPool.m_Pool.m_TexL0L1rx);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L1G_L1Ry, dstPool.m_Pool.m_TexL1GRy);
            cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L1B_L1Rz, dstPool.m_Pool.m_TexL1BRz);

            if (m_ShBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                s_StateBlendShader.EnableKeyword("PROBE_VOLUMES_L2");

                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L2_0, m_State0.m_Pool.m_TexL20);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L2_1, m_State0.m_Pool.m_TexL21);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L2_2, m_State0.m_Pool.m_TexL22);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_L2_3, m_State0.m_Pool.m_TexL23);

                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L2_0, m_State1.m_Pool.m_TexL20);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L2_1, m_State1.m_Pool.m_TexL21);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L2_2, m_State1.m_Pool.m_TexL22);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_L2_3, m_State1.m_Pool.m_TexL23);

                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L2_0, dstPool.m_Pool.m_TexL20);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L2_1, dstPool.m_Pool.m_TexL21);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L2_2, dstPool.m_Pool.m_TexL22);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_L2_3, dstPool.m_Pool.m_TexL23);
            }
            else
                s_StateBlendShader.DisableKeyword("PROBE_VOLUMES_L2");

            if (m_ProbeOcclusion)
            {
                s_StateBlendShader.EnableKeyword("USE_APV_PROBE_OCCLUSION");
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State0_ProbeOcclusion, m_State0.m_Pool.m_TexProbeOcclusion);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, _State1_ProbeOcclusion, m_State1.m_Pool.m_TexProbeOcclusion);
                cmd.SetComputeTextureParam(s_StateBlendShader, s_ScenarioBlendingKernel, ProbeBrickPool._Out_ProbeOcclusion, dstPool.m_Pool.m_TexProbeOcclusion);
            }
            else
                s_StateBlendShader.DisableKeyword("USE_APV_PROBE_OCCLUSION");

            var poolDimLerpFactor = new Vector4(dstPool.GetPoolWidth(), dstPool.GetPoolHeight(), factor, 0.0f);

            const int numthreads = 4;
            int threadX = ProbeBrickPool.DivRoundUp(ProbeBrickPool.k_ChunkProbeCountPerDim, numthreads);
            int threadY = ProbeBrickPool.DivRoundUp(ProbeBrickPool.k_BrickProbeCountPerDim, numthreads);
            int threadZ = ProbeBrickPool.DivRoundUp(ProbeBrickPool.k_BrickProbeCountPerDim, numthreads);

            cmd.SetComputeVectorArrayParam(s_StateBlendShader, _ChunkList, m_ChunkList);
            cmd.SetComputeVectorParam(s_StateBlendShader, _PoolDim_LerpFactor, poolDimLerpFactor);
            cmd.DispatchCompute(s_StateBlendShader, s_ScenarioBlendingKernel, threadX, threadY, threadZ * m_MappedChunks);
            m_MappedChunks = 0;
        }

        internal void BlendChunks(Cell cell, ProbeBrickPool dstPool)
        {
            for (int c = 0; c < cell.blendingInfo.chunkList.Count; c++)
            {
                var chunk = cell.blendingInfo.chunkList[c];
                int dst = cell.poolInfo.chunkList[c].FlattenIndex(dstPool.GetPoolWidth(), dstPool.GetPoolHeight());

                m_ChunkList[m_MappedChunks++] = new Vector4(chunk.x, chunk.y, chunk.z, dst);
            }
        }

        internal void Clear()
            => m_State0?.Clear();

        internal bool Allocate(int numberOfBrickChunks, List<ProbeBrickPool.BrickChunkAlloc> outAllocations)
        {
            AllocateResourcesIfNeeded();
            if (numberOfBrickChunks > m_State0.GetRemainingChunkCount())
                return false;

            return m_State0.Allocate(numberOfBrickChunks, outAllocations, false);
        }

        internal void Deallocate(List<ProbeBrickPool.BrickChunkAlloc> allocations)
        {
            if (allocations.Count == 0)
                return;

            m_State0.Deallocate(allocations);
        }

        internal void EnsureTextureValidity()
        {
            if (isAllocated)
            {
                m_State0.EnsureTextureValidity();
                m_State1.EnsureTextureValidity();
            }
        }

        internal void Cleanup()
        {
            if (isAllocated)
            {
                m_State0.Cleanup();
                m_State1.Cleanup();
            }
        }
    }
}
