using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;

// TODO remove g_deferredLights: it is currently a workaround for IJob not allowed to contains reference types (we need a reference/pointer to a DeferredTiler).
// TODO use subpass API to hide extra TileDepthPass
// TODO Improve the way _unproject0/_unproject1 are computed (Clip matrix issue)
// TODO remove Vector4UInt
// TODO Make sure GPU buffers are uploaded without copying into Unity CommandBuffer memory
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity

namespace UnityEngine.Rendering.Universal.Internal
{
    // Customization per platform.
    static class DeferredConfig
    {
        // Keep in sync with shader define USE_CBUFFER_FOR_DEPTHRANGE
        // Keep in sync with shader define USE_CBUFFER_FOR_TILELIST
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTDATA
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTLIST
#if UNITY_SWITCH
        // Constant buffers are used for data that a repeatedly fetched by shaders.
        // Structured buffers are used for data only consumed once.
        public static bool kUseCBufferForDepthRange = false;
        public static bool kUseCBufferForTileList = false;
        public static bool kUseCBufferForLightData = true;
        public static bool kUseCBufferForLightList = false;
#else
        public static bool kUseCBufferForDepthRange = false;
        public static bool kUseCBufferForTileList = false;
        public static bool kUseCBufferForLightData = true;
        public static bool kUseCBufferForLightList = false;
#endif

        // Keep in sync with PREFERRED_CBUFFER_SIZE.
        public const int kPreferredCBufferSize = 64 * 1024;
        public const int kPreferredStructuredBufferSize = 128 * 1024;

        public const int kTilePixelWidth = 16;
        public const int kTilePixelHeight = 16;
        // Levels of hierarchical tiling. Each level process 4x4 finer tiles. For example:
        // For platforms using 16x16 px tiles, we use a 16x16px tiles grid, a 64x64px tiles grid, and a 256x256px tiles grid
        // For platforms using  8x8  px tiles, we use a  8x8px  tiles grid, a 32x32px tiles grid, and a 128x128px tiles grid
        public const int kTilerDepth = 3;

        public const int kAvgLightPerTile = 32;

        // On platforms where the tile dimensions is large (16x16), it may be faster to generate tileDepthInfo texture
        // with an intermediate mip level, as this allows spawning more pixel shaders (avoid GPU starvation).
        // Set to -1 to disable.
#if UNITY_SWITCH
        public const int kTileDepthInfoIntermediateLevel = 1;
        #else
        public const int kTileDepthInfoIntermediateLevel = -1;
        #endif

#if !UNITY_EDITOR && UNITY_SWITCH
        public const bool kHasNativeQuadSupport = true;
#else
        public const bool kHasNativeQuadSupport = false;
#endif
    }

    // Manages tiled-based deferred lights.
    internal class DeferredLights
    {
        static class ShaderConstants
        {
            public static int _MainLightPosition = Shader.PropertyToID("_MainLightPosition");   // ForwardLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColor = Shader.PropertyToID("_MainLightColor");         // ForwardLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes

            public static readonly string DOWNSAMPLING_SIZE_2 = "DOWNSAMPLING_SIZE_2";
            public static readonly string DOWNSAMPLING_SIZE_4 = "DOWNSAMPLING_SIZE_4";
            public static readonly string DOWNSAMPLING_SIZE_8 = "DOWNSAMPLING_SIZE_8";
            public static readonly string DOWNSAMPLING_SIZE_16 = "DOWNSAMPLING_SIZE_16";

            public static readonly int UDepthRanges = Shader.PropertyToID("UDepthRanges");
            public static readonly int _DepthRanges = Shader.PropertyToID("_DepthRanges");
            public static readonly int _DownsamplingWidth = Shader.PropertyToID("_DownsamplingWidth");
            public static readonly int _DownsamplingHeight = Shader.PropertyToID("_DownsamplingHeight");
            public static readonly int _SourceShiftX = Shader.PropertyToID("_SourceShiftX");
            public static readonly int _SourceShiftY = Shader.PropertyToID("_SourceShiftY");
            public static readonly int _TileShiftX = Shader.PropertyToID("_TileShiftX");
            public static readonly int _TileShiftY = Shader.PropertyToID("_TileShiftY");
            public static readonly int _tileXCount = Shader.PropertyToID("_tileXCount");
            public static readonly int _DepthRangeOffset = Shader.PropertyToID("_DepthRangeOffset");
            public static readonly int _BitmaskTex = Shader.PropertyToID("_BitmaskTex");
            public static readonly int UTileList = Shader.PropertyToID("UTileList");
            public static readonly int _TileList = Shader.PropertyToID("_TileList");
            public static readonly int UPointLightBuffer = Shader.PropertyToID("UPointLightBuffer");
            public static readonly int _PointLightBuffer = Shader.PropertyToID("_PointLightBuffer");
            public static readonly int URelLightList = Shader.PropertyToID("URelLightList");
            public static readonly int _RelLightList = Shader.PropertyToID("_RelLightList");
            public static readonly int _TilePixelWidth = Shader.PropertyToID("_TilePixelWidth");
            public static readonly int _TilePixelHeight = Shader.PropertyToID("_TilePixelHeight");
            public static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");
            public static readonly int _DepthTex = Shader.PropertyToID("_DepthTex");
            public static readonly int _DepthTexSize = Shader.PropertyToID("_DepthTexSize");
            public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");

            public static readonly int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");
            public static readonly int _unproject0 = Shader.PropertyToID("_unproject0");
            public static readonly int _unproject1 = Shader.PropertyToID("_unproject1");
        }

        struct CullLightsJob : IJob
        {
            public int tilerLevel;
            [ReadOnly]
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeArray<DeferredTiler.PrePointLight> prePointLights;
            [ReadOnly]
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeArray<ushort> coarseTiles;
            public int coarseTileOffset;
            public int coarseVisLightCount;
            public int istart;
            public int iend;
            public int jstart;
            public int jend;

            public void Execute()
            {
                if (tilerLevel == 0)
                {
                    g_deferredLights.m_Tilers[tilerLevel].CullFinalLights(
                        ref prePointLights,
                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                        istart, iend, jstart, jend
                    );
                }
                else
                {
                    g_deferredLights.m_Tilers[tilerLevel].CullIntermediateLights(
                        ref prePointLights,
                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                        istart, iend, jstart, jend
                    );
                }
            }
        }

        struct DrawCall
        {
            public ComputeBuffer tileList;
            public ComputeBuffer pointLightBuffer;
            public ComputeBuffer relLightList;
            public int tileListSize;
            public int pointLightBufferSize;
            public int relLightListSize;
            public int instanceOffset;
            public int instanceCount;
        }

        internal static DeferredLights g_deferredLights;

        
        static readonly string k_SetupLights = "SetupLights";
        static readonly string k_DeferredPass = "Deferred Pass";
        static readonly string k_TileDepthInfo = "Tile Depth Info";
        static readonly string k_TiledDeferredPass = "Tile-Based Deferred Shading";
        static readonly string k_StencilDeferredPass = "Stencil Deferred Shading";
        static readonly string k_SetupLightConstants = "Setup Light Constants";

        public bool tiledDeferredShading = true; // <- true: TileDeferred.shader used for some lights (currently: point lights without shadows) - false: use StencilDeferred.shader for all lights
        public readonly bool useJobSystem = true;

        //
        internal int m_RenderWidth = 0;
        //
        internal int m_RenderHeight = 0;
        // Cached.
        internal int m_CachedRenderWidth = 0;
        // Cached.
        internal int m_CachedRenderHeight = 0;
        // Cached.
        Matrix4x4 m_CachedProjectionMatrix;

        // Hierarchical tilers.
        DeferredTiler[] m_Tilers;

        // Should any visible lights be rendered as tile?
        bool m_HasTileVisLights;
        // Visible lights rendered using stencil.
        NativeArray<ushort> m_stencilVisLights;

        // For rendering stencil point lights.
        Mesh m_SphereMesh;

        // Max number of tile depth range data that can be referenced per draw call.
        int m_MaxDepthRangePerBatch;
        // Max numer of instanced tile that can be referenced per draw call.
        int m_MaxTilesPerBatch;
        // Max number of point lights that can be referenced per draw call.
        int m_MaxPointLightPerBatch;
        // Max number of relative light indices that can be referenced per draw call.
        int m_MaxRelLightIndicesPerBatch;

        // Generate per-tile depth information.
        Material m_TileDepthInfoMaterial;
        // Hold all shaders for tiled-based deferred shading.
        Material m_TileDeferredMaterial;
        // Hold all shaders for stencil-volume deferred shading.
        Material m_StencilDeferredMaterial;

        // Output lighting result.
        internal RenderTargetHandle m_LightingTexture;
        // Input depth texture, also bound as read-only RT
        internal RenderTargetHandle m_DepthTexture;
        //
        internal RenderTargetHandle m_DepthCopyTexture;
        // Intermediate depth info texture.
        internal RenderTargetHandle m_DepthInfoTexture;
        // Per-tile depth info texture.
        internal RenderTargetHandle m_TileDepthInfoTexture;

        public DeferredLights(Material tileDepthInfoMaterial, Material tileDeferredMaterial, Material stencilDeferredMaterial)
        {
            m_TileDepthInfoMaterial = tileDepthInfoMaterial;
            m_TileDeferredMaterial = tileDeferredMaterial;
            m_StencilDeferredMaterial = stencilDeferredMaterial;

            // Compute some platform limits.
            m_MaxDepthRangePerBatch = (DeferredConfig.kUseCBufferForDepthRange ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / sizeof(uint);
            m_MaxTilesPerBatch = (DeferredConfig.kUseCBufferForTileList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(TileData));
            m_MaxPointLightPerBatch = (DeferredConfig.kUseCBufferForLightData ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
            m_MaxRelLightIndicesPerBatch = (DeferredConfig.kUseCBufferForLightList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / sizeof(uint);

            m_Tilers = new DeferredTiler[DeferredConfig.kTilerDepth];

            // Initialize hierarchical tilers. Next tiler processes 4x4 of the tiles of the previous tiler.
            // Tiler 0 has finest tiles, coarser tilers follow.
            for (int tilerLevel = 0; tilerLevel < DeferredConfig.kTilerDepth; ++tilerLevel)
            {
                int scale = (int)Mathf.Pow(4, tilerLevel);
                // Tile header size is:
                // 5 for finest tiles: ushort lightCount, half minDepth, half maxDepth, uint bitmask
                // 1 for coarser tiles: ushort lightCount
                m_Tilers[tilerLevel] = new DeferredTiler(
                    DeferredConfig.kTilePixelWidth * scale,
                    DeferredConfig.kTilePixelHeight * scale,
                    DeferredConfig.kAvgLightPerTile * scale * scale,
                    tilerLevel
                );
            }

            m_HasTileVisLights = false;
        }

        public DeferredTiler GetTiler(int i)
        {
            return m_Tilers[i];
        }

        // adapted from ForwardLights.SetupShaderLightConstants
        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal Forward pipeline only supports a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
            SetupAdditionalLightConstants(cmd, ref renderingData);
        }

        // adapted from ForwardLights.SetupShaderLightConstants
        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            ForwardLights.InitializeLightConstants_Common(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            cmd.SetGlobalVector(ShaderConstants._MainLightPosition, lightPos);
            cmd.SetGlobalVector(ShaderConstants._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.SetGlobalTexture(ShaderConstants._DepthTex, m_DepthCopyTexture.Identifier()); // We should bind m_DepthCopyTexture but currently not possible yet

            float yInversionFactor = SystemInfo.graphicsUVStartsAtTop ? -1.0f : 1.0f;
            // TODO There is an inconsistency in UniversalRP where the screen is already y-inverted. Why?
            yInversionFactor *= -1.0f;

            Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
            Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
            // Go to pixel coordinates for xy coordinates, z goes to texture space [0.0; 1.0].
            Matrix4x4 toScreen = new Matrix4x4(
                new Vector4(0.5f * m_RenderWidth, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.5f * yInversionFactor * m_RenderHeight, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                new Vector4(0.5f * m_RenderWidth, 0.5f * m_RenderHeight, 0.5f, 1.0f)
            );
            Matrix4x4 toReversedZ = new Matrix4x4(
                new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, SystemInfo.usesReversedZBuffer ? -1.0f : 1.0f, 0.0f),
                new Vector4(0.0f, 0.0f, SystemInfo.usesReversedZBuffer ? 1.0f : 0.0f, 1.0f)
            );
            Matrix4x4 clipToWorld = Matrix4x4.Inverse(toReversedZ * toScreen * proj * view);
            cmd.SetGlobalMatrix(ShaderConstants._ScreenToWorld, clipToWorld);
        }

        public void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(k_SetupLights);

            DeferredShaderData.instance.ResetBuffers();

            m_RenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
            m_RenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;

            if (this.tiledDeferredShading)
            {
                // Precompute tile data again if the camera projection or the screen resolution has changed.
                if (m_CachedRenderWidth != renderingData.cameraData.cameraTargetDescriptor.width
                    || m_CachedRenderHeight != renderingData.cameraData.cameraTargetDescriptor.height
                    || m_CachedProjectionMatrix != renderingData.cameraData.camera.projectionMatrix)
                {
                    m_CachedRenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
                    m_CachedRenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;
                    m_CachedProjectionMatrix = renderingData.cameraData.camera.projectionMatrix;

                    foreach (DeferredTiler tiler in m_Tilers)
                    {
                        tiler.PrecomputeTiles(renderingData.cameraData.camera.projectionMatrix,
                            renderingData.cameraData.camera.orthographic, m_CachedRenderWidth, m_CachedRenderHeight);
                    }
                }

                // Allocate temporary resources for each hierarchical tiler.
                foreach (DeferredTiler tiler in m_Tilers)
                    tiler.Setup();
            }

            // Will hold point lights that will be rendered using tiles.
            NativeArray<DeferredTiler.PrePointLight> prePointLights;

            // inspect lights in renderingData.lightData.visibleLights and convert them to entries in prePointLights OR m_stencilVisLights
            // currently we store pointlights and spotlights that can be rendered by TiledDeferred, in the same prePointLights list
            PrecomputeLights(
                out prePointLights,
                out m_stencilVisLights,
                ref renderingData.lightData.visibleLights,
                renderingData.cameraData.camera.worldToCameraMatrix,
                renderingData.cameraData.camera.orthographic,
                renderingData.cameraData.camera.nearClipPlane
            );

            // Shared uniform constants for all lights.
            {
                CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
                SetupShaderLightConstants(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            if (this.tiledDeferredShading)
            {
                // Sort lights front to back.
                // This allows a further optimisation where per-tile light lists can be more easily trimmed on both ends in the vertex shading instancing the tiles.
                SortLights(ref prePointLights);

                NativeArray<ushort> defaultIndices = new NativeArray<ushort>(prePointLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < prePointLights.Length; ++i)
                    defaultIndices[i] = (ushort)i;

                // Cull tile-friendly lights into the coarse tile structure.
                DeferredTiler coarsestTiler = m_Tilers[m_Tilers.Length - 1];
                if (m_Tilers.Length != 1)
                {
                    // Fill coarsestTiler.m_Tiles with for each tile, a list of lightIndices from prePointLights that intersect the tile
                    coarsestTiler.CullIntermediateLights(ref prePointLights,
                        ref defaultIndices, 0, prePointLights.Length,
                        0, coarsestTiler.GetTileXCount(), 0, coarsestTiler.GetTileYCount()
                    );

                    // Filter to fine tile structure.
                    for (int t = m_Tilers.Length - 2; t >= 0; --t)
                    {
                        DeferredTiler fineTiler = m_Tilers[t];
                        DeferredTiler coarseTiler = m_Tilers[t + 1];
                        int fineTileXCount = fineTiler.GetTileXCount();
                        int fineTileYCount = fineTiler.GetTileYCount();
                        int coarseTileXCount = coarseTiler.GetTileXCount();
                        int coarseTileYCount = coarseTiler.GetTileYCount();
                        ref NativeArray<ushort> coarseTiles = ref coarseTiler.GetTiles();
                        int fineStepX = coarseTiler.GetTilePixelWidth() / fineTiler.GetTilePixelWidth();
                        int fineStepY = coarseTiler.GetTilePixelHeight() / fineTiler.GetTilePixelHeight();

                        // TODO Hacky workaround because the jobs cannot access the DeferredTiler instances otherwise. Fix this.
                        g_deferredLights = this;
                        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(coarseTileXCount * coarseTileYCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        int jobCount = 0;

                        for (int j = 0; j < coarseTileYCount; ++j)
                        for (int i = 0; i < coarseTileXCount; ++i)
                        {
                            int fine_istart = i * fineStepX;
                            int fine_jstart = j * fineStepY;
                            int fine_iend = Mathf.Min(fine_istart + fineStepX, fineTileXCount);
                            int fine_jend = Mathf.Min(fine_jstart + fineStepY, fineTileYCount);
                            int coarseTileOffset;
                            int coarseVisLightCount;
                            coarseTiler.GetTileOffsetAndCount(i, j, out coarseTileOffset, out coarseVisLightCount);

                            if (this.useJobSystem)
                            {
                                CullLightsJob job = new CullLightsJob
                                {
                                    tilerLevel = t,
                                    prePointLights = prePointLights,
                                    coarseTiles = coarseTiles,
                                    coarseTileOffset = coarseTileOffset,
                                    coarseVisLightCount = coarseVisLightCount,
                                    istart = fine_istart,
                                    iend = fine_iend,
                                    jstart = fine_jstart,
                                    jend = fine_jend
                                };
                                jobHandles[jobCount++] = job.Schedule();
                            }
                            else
                            {
                                if (t != 0)
                                {
                                    // Fill fineTiler.m_Tiles with for each tile, a list of lightIndices from prePointLights that intersect the tile
                                    // (The prePointLights excluded during previous coarser tiler Culling are not processed any more)
                                    fineTiler.CullIntermediateLights(ref prePointLights,
                                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                                        fine_istart, fine_iend, fine_jstart, fine_jend
                                    );
                                }
                                else
                                {
                                    // Fill fineTiler.m_Tiles with for each tile, a list of lightIndices from prePointLights that intersect the tile
                                    // (The prePointLights excluded during previous coarser tiler Culling are not processed any more)
                                    // Also fills additional per-tile "m_TileHeaders"
                                    fineTiler.CullFinalLights(ref prePointLights,
                                        ref coarseTiles, coarseTileOffset, coarseVisLightCount,
                                        fine_istart, fine_iend, fine_jstart, fine_jend
                                    );
                                }
                            }
                        }

                        if (this.useJobSystem)
                            JobHandle.CompleteAll(jobHandles);
                        jobHandles.Dispose();
                    }
                }
                else
                {
                    coarsestTiler.CullFinalLights(ref prePointLights,
                        ref defaultIndices, 0, prePointLights.Length,
                        0, coarsestTiler.GetTileXCount(), 0, coarsestTiler.GetTileYCount()
                    );
                }

                defaultIndices.Dispose();
            }

            // We don't need this array anymore as all the lights have been inserted into the tile-grid structures.
            prePointLights.Dispose();

            Profiler.EndSample();
        }

        public void Setup(ref RenderingData renderingData, RenderTargetHandle depthCopyTexture, RenderTargetHandle depthInfoTexture, RenderTargetHandle tileDepthInfoTexture, RenderTargetHandle depthTexture, RenderTargetHandle lightingTexture)
        {
            m_DepthCopyTexture = depthCopyTexture;
            m_DepthInfoTexture = depthInfoTexture;
            m_TileDepthInfoTexture = tileDepthInfoTexture;
            m_LightingTexture = lightingTexture;
            m_DepthTexture = depthTexture;

            m_HasTileVisLights = this.tiledDeferredShading && CheckHasTileLights(ref renderingData.lightData.visibleLights);
        }

        public void FrameCleanup(CommandBuffer cmd)
        {
            foreach (DeferredTiler tiler in m_Tilers)
                tiler.FrameCleanup();

            if (m_stencilVisLights.IsCreated)
                m_stencilVisLights.Dispose();
        }

        public bool HasTileLights()
        {
            return m_HasTileVisLights;
        }

        public bool HasTileDepthRangeExtraPass()
        {
            DeferredTiler tiler = m_Tilers[0];
            int tilePixelWidth = tiler.GetTilePixelWidth();
            int tilePixelHeight = tiler.GetTilePixelHeight();
            int tileMipLevel = (int)Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2);
            return DeferredConfig.kTileDepthInfoIntermediateLevel >= 0 && DeferredConfig.kTileDepthInfoIntermediateLevel < tileMipLevel;
        }

        public void ExecuteTileDepthInfoPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDepthInfoMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
                return;
            }

            uint invalidDepthRange = (uint)Mathf.FloatToHalf(-2.0f) | (((uint)Mathf.FloatToHalf(-1.0f)) << 16);

            DeferredTiler tiler = m_Tilers[0];
            int tileXCount = tiler.GetTileXCount();
            int tileYCount = tiler.GetTileYCount();
            int tilePixelWidth = tiler.GetTilePixelWidth();
            int tilePixelHeight = tiler.GetTilePixelHeight();
            int tileMipLevel = (int)Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2);
            int intermediateMipLevel = DeferredConfig.kTileDepthInfoIntermediateLevel >= 0 && DeferredConfig.kTileDepthInfoIntermediateLevel < tileMipLevel ? DeferredConfig.kTileDepthInfoIntermediateLevel : tileMipLevel;
            int tileShiftMipLevel = tileMipLevel - intermediateMipLevel;
            int alignment = 1 << intermediateMipLevel;
            int depthInfoWidth = (m_RenderWidth + alignment - 1) >> intermediateMipLevel;
            int depthInfoHeight = (m_RenderHeight + alignment - 1) >> intermediateMipLevel;
            ref NativeArray<ushort> tiles = ref tiler.GetTiles();
            ref NativeArray<uint> tileHeaders = ref tiler.GetTileHeaders();

            CommandBuffer cmd = CommandBufferPool.Get(k_TileDepthInfo);
            RenderTargetIdentifier depthSurface = m_DepthTexture.Identifier();
            RenderTargetIdentifier depthInfoSurface = ((tileMipLevel == intermediateMipLevel) ? m_TileDepthInfoTexture : m_DepthInfoTexture).Identifier();

            cmd.SetGlobalTexture(ShaderConstants._DepthTex, depthSurface);
            cmd.SetGlobalVector(ShaderConstants._DepthTexSize, new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight));
            cmd.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
            cmd.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);
            cmd.SetGlobalInt(ShaderConstants._SourceShiftX, intermediateMipLevel);
            cmd.SetGlobalInt(ShaderConstants._SourceShiftY, intermediateMipLevel);
            cmd.SetGlobalInt(ShaderConstants._TileShiftX, tileShiftMipLevel);
            cmd.SetGlobalInt(ShaderConstants._TileShiftY, tileShiftMipLevel);

            Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
            Matrix4x4 clip = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0.5f, 0), new Vector4(0, 0, 0.5f, 1));
            Matrix4x4 projScreenInv = Matrix4x4.Inverse(clip * proj);
            cmd.SetGlobalVector(ShaderConstants._unproject0, projScreenInv.GetRow(2));
            cmd.SetGlobalVector(ShaderConstants._unproject1, projScreenInv.GetRow(3));

            string shaderVariant = null;
            if (tilePixelWidth == tilePixelHeight)
            {
                if (intermediateMipLevel == 1)
                    shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_2;
                else if (intermediateMipLevel == 2)
                    shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_4;
                else if (intermediateMipLevel == 3)
                    shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_8;
                else if (intermediateMipLevel == 4)
                    shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_16;
            }
            if (shaderVariant != null)
                cmd.EnableShaderKeyword(shaderVariant);

            int tileY = 0;
            int tileYIncrement = (DeferredConfig.kUseCBufferForDepthRange ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / (tileXCount * 4);

            NativeArray<uint> depthRanges = new NativeArray<uint>(m_MaxDepthRangePerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            while (tileY < tileYCount)
            {
                int tileYEnd = Mathf.Min(tileYCount, tileY + tileYIncrement);

                for (int j = tileY; j < tileYEnd; ++j)
                {
                    for (int i = 0; i < tileXCount; ++i)
                    {
                        int headerOffset = tiler.GetTileHeaderOffset(i, j);
                        int tileLightCount = (int)tileHeaders[headerOffset + 1];
                        uint listDepthRange = tileLightCount == 0 ? invalidDepthRange : tileHeaders[headerOffset + 2];
                        depthRanges[i + (j - tileY) * tileXCount] = listDepthRange;
                    }
                }

                ComputeBuffer _depthRanges = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxDepthRangePerBatch, DeferredConfig.kUseCBufferForDepthRange);
                _depthRanges.SetData(depthRanges, 0, 0, depthRanges.Length);

                if (DeferredConfig.kUseCBufferForDepthRange)
                    cmd.SetGlobalConstantBuffer(_depthRanges, ShaderConstants.UDepthRanges, 0, m_MaxDepthRangePerBatch * 4);
                else
                    cmd.SetGlobalBuffer(ShaderConstants._DepthRanges, _depthRanges);

                cmd.SetGlobalInt(ShaderConstants._tileXCount, tileXCount);
                cmd.SetGlobalInt(ShaderConstants._DepthRangeOffset, tileY * tileXCount);

                cmd.EnableScissorRect(new Rect(0, tileY << tileShiftMipLevel, depthInfoWidth, (tileYEnd - tileY) << tileShiftMipLevel));
                cmd.Blit(depthSurface, depthInfoSurface, m_TileDepthInfoMaterial, 0);

                tileY = tileYEnd;
            }

            cmd.DisableScissorRect();

            if (shaderVariant != null)
                cmd.DisableShaderKeyword(shaderVariant);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            depthRanges.Dispose();
        }

        public void ExecuteDownsampleBitmaskPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDepthInfoMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(k_TileDepthInfo);
            RenderTargetIdentifier depthInfoSurface = m_DepthInfoTexture.Identifier();
            RenderTargetIdentifier tileDepthInfoSurface = m_TileDepthInfoTexture.Identifier();

            DeferredTiler tiler = m_Tilers[0];
            int tilePixelWidth = tiler.GetTilePixelWidth();
            int tilePixelHeight = tiler.GetTilePixelHeight();
            int tileWidthLevel = (int)Mathf.Log(tilePixelWidth, 2);
            int tileHeightLevel = (int)Mathf.Log(tilePixelHeight, 2);
            int intermediateMipLevel = DeferredConfig.kTileDepthInfoIntermediateLevel;
            int diffWidthLevel = tileWidthLevel - intermediateMipLevel;
            int diffHeightLevel = tileHeightLevel - intermediateMipLevel;

            cmd.SetGlobalTexture(ShaderConstants._BitmaskTex, depthInfoSurface);
            cmd.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
            cmd.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);

            int alignment = 1 << DeferredConfig.kTileDepthInfoIntermediateLevel;
            int depthInfoWidth = (m_RenderWidth + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;
            int depthInfoHeight = (m_RenderHeight + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;
            cmd.SetGlobalVector("_BitmaskTexSize", new Vector4(depthInfoWidth, depthInfoHeight, 1.0f / depthInfoWidth, 1.0f / depthInfoHeight));

            string shaderVariant = null;
            if (diffWidthLevel == 1 && diffHeightLevel == 1)
                shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_2;
            else if (diffWidthLevel == 2 && diffHeightLevel == 2)
                shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_4;
            else if (diffWidthLevel == 3 && diffHeightLevel == 3)
                shaderVariant = ShaderConstants.DOWNSAMPLING_SIZE_8;

            if (shaderVariant != null)
                cmd.EnableShaderKeyword(shaderVariant);

            cmd.Blit(depthInfoSurface, tileDepthInfoSurface, m_TileDepthInfoMaterial, 1);

            if (shaderVariant != null)
                cmd.DisableShaderKeyword(shaderVariant);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void ExecuteDeferredPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_DeferredPass);

            Profiler.BeginSample(k_DeferredPass);

            RenderTiledPointLights(context, cmd, ref renderingData);

            RenderStencilLights(context, cmd, ref renderingData);

            Profiler.EndSample();

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        void SortLights(ref NativeArray<DeferredTiler.PrePointLight> prePointLights)
        {
            DeferredTiler.PrePointLight[] array = prePointLights.ToArray(); // TODO Use NativeArrayExtensions and avoid dynamic memory allocation.
            System.Array.Sort<DeferredTiler.PrePointLight>(array, new SortPrePointLight());
            prePointLights.CopyFrom(array);
        }

        bool CheckHasTileLights(ref NativeArray<VisibleLight> visibleLights)
        {
            for (int visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                if (IsTileLight(visibleLights[visLightIndex].light))
                    return true;
            }

            return false;
        }

        void PrecomputeLights(out NativeArray<DeferredTiler.PrePointLight> prePointLights,
                              out NativeArray<ushort> stencilVisLights,
                              ref NativeArray<VisibleLight> visibleLights,
                              Matrix4x4 view,
                              bool isOrthographic,
                              float zNear)
        {
            const int lightTypeCount = (int)LightType.Disc + 1;

            // number of supported lights rendered by the TileDeferred system, for each light type (Spot, Directional, Point, Area, Rectangle, Disc, plus one slot at the end)
            NativeArray<int> tileLightCount = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            int stencilLightCount = 0;

            if (this.tiledDeferredShading)
            {
                // Count the number of lights per type.
                for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
                {
                    VisibleLight vl = visibleLights[visLightIndex];
                    if (IsTileLight(vl.light))
                        ++tileLightCount[(int)vl.lightType];
                    else // All remaining lights are processed as stencil volumes.
                        ++stencilLightCount;
                }
            }
            else
                stencilLightCount = visibleLights.Length;

            // for now we store spotlights and pointlights in the same list
            prePointLights = new NativeArray<DeferredTiler.PrePointLight>(tileLightCount[(int)LightType.Point] + tileLightCount[(int)LightType.Spot], Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            stencilVisLights = new NativeArray<ushort>(stencilLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < tileLightCount.Length; ++i)
                tileLightCount[i] = 0;
            stencilLightCount = 0;

            // Precompute point light data.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (vl.lightType == LightType.Point || vl.lightType == LightType.Spot)
                {
                    if (this.tiledDeferredShading && IsTileLight(vl.light))
                    {
                        DeferredTiler.PrePointLight ppl;
                        ppl.posVS = view.MultiplyPoint(vl.light.transform.position); // By convention, OpenGL RH coordinate space
                        ppl.radius = vl.light.range;
                        ppl.minDist = max(0.0f, length(ppl.posVS) - ppl.radius);

                        ppl.screenPos = new Vector2(ppl.posVS.x, ppl.posVS.y);
                        // Project on screen for perspective projections.
                        if (!isOrthographic && ppl.posVS.z <= zNear)
                            ppl.screenPos = ppl.screenPos * (-zNear / ppl.posVS.z);

                        ppl.visLightIndex = visLightIndex;

                        int i = tileLightCount[(int)LightType.Point /*vl.lightType*/ ]++;  // for now we store spotlights and pointlights in the same list
                        prePointLights[i] = ppl;
                        continue;
                    }
                }

                // All remaining lights are processed as stencil volumes.
                stencilVisLights[stencilLightCount++] = visLightIndex;
            }
            tileLightCount.Dispose();
        }

        void RenderTiledPointLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_TileDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDeferredMaterial, GetType().Name);
                return;
            }

            if (!m_HasTileVisLights)
                return;

            Profiler.BeginSample(k_TiledDeferredPass);

            // Allow max 256 draw calls for rendering all the batches of tiles
            DrawCall[] drawCalls = new DrawCall[256];
            int drawCallCount = 0;

            {
                DeferredTiler tiler = m_Tilers[0];

                int sizeof_TileData = 16;
                int sizeof_vec4_TileData = sizeof_TileData >> 4;
                int sizeof_PointLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
                int sizeof_vec4_PointLightData = sizeof_PointLightData >> 4;

                int tileXCount = tiler.GetTileXCount();
                int tileYCount = tiler.GetTileYCount();
                int maxLightPerTile = tiler.GetMaxLightPerTile();
                ref NativeArray<ushort> tiles = ref tiler.GetTiles();
                ref NativeArray<uint> tileHeaders = ref tiler.GetTileHeaders();

                int instanceOffset = 0;
                int tileCount = 0;
                int lightCount = 0;
                int relLightIndices = 0;

                ComputeBuffer _tileList = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.kUseCBufferForTileList);
                ComputeBuffer _pointLightBuffer = DeferredShaderData.instance.ReserveBuffer<PointLightData>(m_MaxPointLightPerBatch, DeferredConfig.kUseCBufferForLightData);
                ComputeBuffer _relLightList = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.kUseCBufferForLightList);

                NativeArray<Vector4UInt> tileList = new NativeArray<Vector4UInt>(m_MaxTilesPerBatch * sizeof_vec4_TileData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector4UInt> pointLightBuffer = new NativeArray<Vector4UInt>(m_MaxPointLightPerBatch * sizeof_vec4_PointLightData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint> relLightList = new NativeArray<uint>(m_MaxRelLightIndicesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                // Acceleration structure to quickly find if a light has already been added to the uniform block data for the current draw call.
                NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<ushort> visLightToRelLights = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                for (int j = 0; j < tileYCount; ++j)
                {
                    for (int i = 0; i < tileXCount; ++i)
                    {
                        int tileOffset;
                        int tileLightCount;
                        tiler.GetTileOffsetAndCount(i, j, out tileOffset, out tileLightCount);
                        if (tileLightCount == 0) // empty tile
                            continue;

                        // Find lights that are not in the batch yet.
                        int trimmedLightCount = TrimLights(ref trimmedLights, ref tiles, tileOffset, tileLightCount, ref usedLights);
                        Assertions.Assert.IsTrue(trimmedLightCount <= maxLightPerTile); // too many lights overlaps a tile

                        // Checks whether one of the GPU buffers is reaching max capacity.
                        // In that case, the draw call must be flushed and new GPU buffer(s) be allocated.
                        bool tileListIsFull = (tileCount == m_MaxTilesPerBatch);
                        bool lightBufferIsFull = (lightCount + trimmedLightCount > m_MaxPointLightPerBatch);
                        bool relLightListIsFull = (relLightIndices + tileLightCount > m_MaxRelLightIndicesPerBatch);

                        if (tileListIsFull || lightBufferIsFull || relLightListIsFull)
                        {
                            drawCalls[drawCallCount++] = new DrawCall
                            {
                                tileList = _tileList,
                                pointLightBuffer = _pointLightBuffer,
                                relLightList = _relLightList,
                                tileListSize = tileCount * sizeof_TileData,
                                pointLightBufferSize = lightCount * sizeof_PointLightData,
                                relLightListSize = Align(relLightIndices, 4) * 4,
                                instanceOffset = instanceOffset,
                                instanceCount = tileCount - instanceOffset
                            };

                            if (tileListIsFull)
                            {
                                _tileList.SetData(tileList, 0, 0, tileList.Length); // Must pass complete array (restriction for binding Unity Constant Buffers)
                                _tileList = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.kUseCBufferForTileList);
                                tileCount = 0;
                            }

                            if (lightBufferIsFull)
                            {
                                _pointLightBuffer.SetData(pointLightBuffer, 0, 0, pointLightBuffer.Length);
                                _pointLightBuffer = DeferredShaderData.instance.ReserveBuffer<PointLightData>(m_MaxPointLightPerBatch, DeferredConfig.kUseCBufferForLightData);
                                lightCount = 0;

                                // If pointLightBuffer was reset, then all lights in the current tile must be added.
                                trimmedLightCount = tileLightCount;
                                for (int l = 0; l < tileLightCount; ++l)
                                    trimmedLights[l] = tiles[tileOffset + l];
                                usedLights.Clear();
                            }

                            if (relLightListIsFull)
                            {
                                _relLightList.SetData(relLightList, 0, 0, relLightList.Length);
                                _relLightList = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.kUseCBufferForLightList);
                                relLightIndices = 0;
                            }

                            instanceOffset = tileCount;
                        }

                        // Add TileData.
                        int headerOffset = tiler.GetTileHeaderOffset(i, j);
                        uint listBitMask = tileHeaders[headerOffset + 3];
                        StoreTileData(ref tileList, tileCount, PackTileID((uint)i, (uint)j), listBitMask, (ushort)relLightIndices, (ushort)tileLightCount);
                        ++tileCount;

                        // Add newly discovered lights.
                        for (int l = 0; l < trimmedLightCount; ++l)
                        {
                            int visLightIndex = trimmedLights[l];
                            StorePointLightData(ref pointLightBuffer, lightCount, ref renderingData.lightData.visibleLights, visLightIndex);
                            visLightToRelLights[visLightIndex] = (ushort)lightCount;
                            ++lightCount;
                            usedLights.Set(visLightIndex, true);
                        }

                        // Add light list for the tile.
                        for (int l = 0; l < tileLightCount; ++l)
                        {
                            int visLightIndex = tiles[tileOffset + l];
                            ushort relLightIndex = visLightToRelLights[visLightIndex];
                            ushort relLightBitRange = tiles[tileOffset + tileLightCount + l];
                            relLightList[relLightIndices++] = (uint)relLightIndex | (uint)(relLightBitRange << 16);
                        }
                    }
                }

                int instanceCount = tileCount - instanceOffset;
                if (instanceCount > 0)
                {
                    _tileList.SetData(tileList, 0, 0, tileList.Length); // Must pass complete array (restriction for binding Unity Constant Buffers)
                    _pointLightBuffer.SetData(pointLightBuffer, 0, 0, pointLightBuffer.Length);
                    _relLightList.SetData(relLightList, 0, 0, relLightList.Length);

                    drawCalls[drawCallCount++] = new DrawCall
                    {
                        tileList = _tileList,
                        pointLightBuffer = _pointLightBuffer,
                        relLightList = _relLightList,
                        tileListSize = tileCount * sizeof_TileData,
                        pointLightBufferSize = lightCount * sizeof_PointLightData,
                        relLightListSize = Align(relLightIndices, 4) * 4,
                        instanceOffset = instanceOffset,
                        instanceCount = instanceCount
                    };
                }

                tileList.Dispose();
                pointLightBuffer.Dispose();
                relLightList.Dispose();
                trimmedLights.Dispose();
                visLightToRelLights.Dispose();
                usedLights.Dispose();
            }

            // Now draw all tile batches.
            using (new ProfilingSample(cmd, k_TiledDeferredPass))
            {
                MeshTopology topology = DeferredConfig.kHasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                int vertexCount = DeferredConfig.kHasNativeQuadSupport ? 4 : 6;

                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector(ShaderConstants._ScreenSize, screenSize);

                cmd.SetGlobalTexture(ShaderConstants._DepthTex, m_DepthCopyTexture.Identifier()); // We should bind m_DepthCopyTexture but currently not possible yet

                int tileWidth = m_Tilers[0].GetTilePixelWidth();
                int tileHeight = m_Tilers[0].GetTilePixelHeight();
                cmd.SetGlobalInt(ShaderConstants._TilePixelWidth, tileWidth);
                cmd.SetGlobalInt(ShaderConstants._TilePixelHeight, tileHeight);

                cmd.SetGlobalTexture(m_TileDepthInfoTexture.id, m_TileDepthInfoTexture.Identifier());

                for (int i = 0; i < drawCallCount; ++i)
                {
                    DrawCall dc = drawCalls[i];

                    if (DeferredConfig.kUseCBufferForTileList)
                        cmd.SetGlobalConstantBuffer(dc.tileList, ShaderConstants.UTileList, 0, dc.tileListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._TileList, dc.tileList);

                    if (DeferredConfig.kUseCBufferForLightData)
                        cmd.SetGlobalConstantBuffer(dc.pointLightBuffer, ShaderConstants.UPointLightBuffer, 0, dc.pointLightBufferSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._PointLightBuffer, dc.pointLightBuffer);

                    if (DeferredConfig.kUseCBufferForLightList)
                        cmd.SetGlobalConstantBuffer(dc.relLightList, ShaderConstants.URelLightList, 0, dc.relLightListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants._RelLightList, dc.relLightList);

                    cmd.SetGlobalInt(ShaderConstants._InstanceOffset, dc.instanceOffset);
                    cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, 0, topology, vertexCount, dc.instanceCount);
                }
            }

            Profiler.EndSample();
        }

        void RenderStencilLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_StencilDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_StencilDeferredMaterial, GetType().Name);
                return;
            }

            if (m_stencilVisLights.Length == 0)
                return;

            Profiler.BeginSample(k_StencilDeferredPass);

            if (m_SphereMesh == null)
                m_SphereMesh = CreateSphereMesh();

            using (new ProfilingSample(cmd, k_StencilDeferredPass))
            {
                NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;

                cmd.SetGlobalTexture(ShaderConstants._DepthTex, m_DepthCopyTexture.Identifier()); // We should bind m_DepthCopyTexture but currently not possible yet

                for (int i = 0; i < m_stencilVisLights.Length; ++i)
                {
                    ushort visLightIndex = m_stencilVisLights[i];
                    VisibleLight vl = visibleLights[visLightIndex];

                    if (vl.light.type == LightType.Point || vl.light.type == LightType.Spot)
                    {
                        Vector3 posWS = vl.light.transform.position;
                        float adjRadius = vl.light.range * 1.06067f; // adjust for approximate sphere geometry

                        Matrix4x4 sphereMatrix = new Matrix4x4(
                            new Vector4(adjRadius,      0.0f,      0.0f, 0.0f),
                            new Vector4(     0.0f, adjRadius,      0.0f, 0.0f),
                            new Vector4(     0.0f,      0.0f, adjRadius, 0.0f),
                            new Vector4(  posWS.x,   posWS.y,   posWS.z, 1.0f)
                        );

                        cmd.SetGlobalVector("_LightPosWS", posWS);
                        cmd.SetGlobalFloat("_LightRadius2", vl.light.range * vl.light.range);  // TODO is this still needed?
                        cmd.SetGlobalVector("_LightColor", /*vl.light.color*/ vl.finalColor ); // VisibleLight.finalColor already returns color in active color space

                        Vector4 lightAttenuation;
                        Vector4 lightSpotDir4;
                        ForwardLights.GetLightAttenuationAndSpotDirection(
                            vl.light.type, vl.range /*vl.light.range*/, vl.localToWorldMatrix,
                            vl.spotAngle, vl.light?.innerSpotAngle,
                            out lightAttenuation, out lightSpotDir4);
                        cmd.SetGlobalVector("_LightAttenuation", lightAttenuation);
                        Vector3 lightSpotDir3 = new Vector3(lightSpotDir4.x, lightSpotDir4.y, lightSpotDir4.z);
                        cmd.SetGlobalVector("_LightSpotDirection", lightSpotDir3);

                        // stencil pass
                        cmd.DrawMesh(m_SphereMesh, sphereMatrix, m_StencilDeferredMaterial, 0, 0);

                        // point light pass
                        cmd.DrawMesh(m_SphereMesh, sphereMatrix, m_StencilDeferredMaterial, 0, 1);
                    }
                }
            }

            Profiler.EndSample();
        }

        int TrimLights(ref NativeArray<ushort> trimmedLights, ref NativeArray<ushort> tiles, int offset, int lightCount, ref BitArray usedLights)
        {
            int trimCount = 0;
            for (int i = 0; i < lightCount; ++i)
            {
                ushort visLightIndex = tiles[offset + i];
                if (usedLights.IsSet(visLightIndex))
                    continue;
                trimmedLights[trimCount++] = visLightIndex;
            }
            return trimCount;
        }

        void StorePointLightData(ref NativeArray<Vector4UInt> pointLightBuffer, int storeIndex, ref NativeArray<VisibleLight> visibleLights, int index)
        {
            Vector3 posWS = visibleLights[index].light.transform.position;
            pointLightBuffer[storeIndex * 4 + 0] = new Vector4UInt(FloatToUInt(posWS.x), FloatToUInt(posWS.y), FloatToUInt(posWS.z), FloatToUInt(visibleLights[index].range * visibleLights[index].range));
            pointLightBuffer[storeIndex * 4 + 1] = new Vector4UInt(FloatToUInt(visibleLights[index].finalColor.r), FloatToUInt(visibleLights[index].finalColor.g), FloatToUInt(visibleLights[index].finalColor.b), 0);

            Vector4 lightAttenuation;
            Vector4 lightSpotDir;
            ForwardLights.GetLightAttenuationAndSpotDirection(
                visibleLights[index].lightType, visibleLights[index].range, visibleLights[index].localToWorldMatrix,
                visibleLights[index].spotAngle, visibleLights[index].light?.innerSpotAngle,
                out lightAttenuation, out lightSpotDir);
            pointLightBuffer[storeIndex * 4 + 2] = new Vector4UInt(FloatToUInt(lightAttenuation.x), FloatToUInt(lightAttenuation.y), FloatToUInt(lightAttenuation.z), FloatToUInt(lightAttenuation.w));
            pointLightBuffer[storeIndex * 4 + 3] = new Vector4UInt(FloatToUInt(lightSpotDir.x), FloatToUInt(lightSpotDir.y), FloatToUInt(lightSpotDir.z), FloatToUInt(lightSpotDir.w));
        }

        void StoreTileData(ref NativeArray<Vector4UInt> tileList, int storeIndex, uint tileID, uint listBitMask, ushort relLightOffset, ushort lightCount)
        {
            // See struct TileData in TileDeferred.shader.
            tileList[storeIndex] = new Vector4UInt { x = tileID, y = listBitMask, z = relLightOffset | ((uint)lightCount << 16), w = 0 };
        }

        bool IsTileLight(Light light)
        {
            // tileDeferred might render a lot of point lights in the same draw call.
            // point light shadows require generating cube shadow maps in real-time, requiring extra CPU/GPU resources ; which can become expensive quickly
            return (light.type == LightType.Point && light.shadows == LightShadows.None)
                || light.type == LightType.Spot ;
        }

        Mesh CreateSphereMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            Vector3 [] spherePos = {
                new Vector3(0.000000f, -1.000000f, 0.000000f), new Vector3(1.000000f, 0.000000f, 0.000000f),
                new Vector3(0.000000f, 1.000000f, 0.000000f), new Vector3(-1.000000f, 0.000000f, 0.000000f),
                new Vector3(0.000000f, 0.000000f, 1.000000f), new Vector3(0.000000f, 0.000000f, -1.000000f),
                new Vector3(0.707107f, -0.707107f, 0.000000f), new Vector3(0.707107f, 0.000000f, 0.707107f),
                new Vector3(0.000000f, -0.707107f, 0.707107f), new Vector3(0.707107f, 0.707107f, 0.000000f),
                new Vector3(0.000000f, 0.707107f, 0.707107f), new Vector3(-0.707107f, 0.707107f, 0.000000f),
                new Vector3(-0.707107f, 0.000000f, 0.707107f), new Vector3(-0.707107f, -0.707107f, 0.000000f),
                new Vector3(0.000000f, -0.707107f, -0.707107f), new Vector3(0.707107f, 0.000000f, -0.707107f),
                new Vector3(0.000000f, 0.707107f, -0.707107f), new Vector3(-0.707107f, 0.000000f, -0.707107f),
                new Vector3(0.816497f, -0.408248f, 0.408248f), new Vector3(0.408248f, -0.408248f, 0.816497f),
                new Vector3(0.408248f, -0.816497f, 0.408248f), new Vector3(0.408248f, 0.816497f, 0.408248f),
                new Vector3(0.408248f, 0.408248f, 0.816497f), new Vector3(0.816497f, 0.408248f, 0.408248f),
                new Vector3(-0.816497f, 0.408248f, 0.408248f), new Vector3(-0.408248f, 0.408248f, 0.816497f),
                new Vector3(-0.408248f, 0.816497f, 0.408248f), new Vector3(-0.408248f, -0.816497f, 0.408248f),
                new Vector3(-0.408248f, -0.408248f, 0.816497f), new Vector3(-0.816497f, -0.408248f, 0.408248f),
                new Vector3(0.408248f, -0.816497f, -0.408248f), new Vector3(0.408248f, -0.408248f, -0.816497f),
                new Vector3(0.816497f, -0.408248f, -0.408248f), new Vector3(0.816497f, 0.408248f, -0.408248f),
                new Vector3(0.408248f, 0.408248f, -0.816497f), new Vector3(0.408248f, 0.816497f, -0.408248f),
                new Vector3(-0.408248f, 0.816497f, -0.408248f), new Vector3(-0.408248f, 0.408248f, -0.816497f),
                new Vector3(-0.816497f, 0.408248f, -0.408248f), new Vector3(-0.816497f, -0.408248f, -0.408248f),
                new Vector3(-0.408248f, -0.408248f, -0.816497f), new Vector3(-0.408248f, -0.816497f, -0.408248f),
                new Vector3(0.382683f, -0.923880f, 0.000000f), new Vector3(0.000000f, -0.923880f, 0.382683f),
                new Vector3(0.923880f, 0.000000f, 0.382683f), new Vector3(0.923880f, -0.382683f, 0.000000f),
                new Vector3(0.000000f, -0.382683f, 0.923880f), new Vector3(0.382683f, 0.000000f, 0.923880f),
                new Vector3(0.923880f, 0.382683f, 0.000000f), new Vector3(0.000000f, 0.923880f, 0.382683f),
                new Vector3(0.382683f, 0.923880f, 0.000000f), new Vector3(0.000000f, 0.382683f, 0.923880f),
                new Vector3(-0.382683f, 0.923880f, 0.000000f), new Vector3(-0.923880f, 0.000000f, 0.382683f),
                new Vector3(-0.923880f, 0.382683f, 0.000000f), new Vector3(-0.382683f, 0.000000f, 0.923880f),
                new Vector3(-0.923880f, -0.382683f, 0.000000f), new Vector3(-0.382683f, -0.923880f, 0.000000f),
                new Vector3(0.923880f, 0.000000f, -0.382683f), new Vector3(0.000000f, -0.923880f, -0.382683f),
                new Vector3(0.382683f, 0.000000f, -0.923880f), new Vector3(0.000000f, -0.382683f, -0.923880f),
                new Vector3(0.000000f, 0.923880f, -0.382683f), new Vector3(0.000000f, 0.382683f, -0.923880f),
                new Vector3(-0.923880f, 0.000000f, -0.382683f), new Vector3(-0.382683f, 0.000000f, -0.923880f),
            };

            int [] sphereIndices = {
                18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
                36, 37, 38, 39, 40, 41, 42, 20, 43, 44, 18, 45, 46, 19, 47, 48, 23, 44,
                49, 21, 50, 47, 22, 51, 52, 26, 49, 53, 24, 54, 51, 25, 55, 56, 29, 53,
                43, 27, 57, 55, 28, 46, 45, 32, 58, 59, 30, 42, 60, 31, 61, 50, 35, 62,
                58, 33, 48, 63, 34, 60, 54, 38, 64, 62, 36, 52, 65, 37, 63, 57, 41, 59,
                64, 39, 56, 61, 40, 65, 6, 18, 20, 7, 19, 18, 8, 20, 19, 9, 21, 23,
                10, 22, 21, 7, 23, 22, 11, 24, 26, 12, 25, 24, 10, 26, 25, 13, 27, 29,
                8, 28, 27, 12, 29, 28, 6, 30, 32, 14, 31, 30, 15, 32, 31, 9, 33, 35,
                15, 34, 33, 16, 35, 34, 11, 36, 38, 16, 37, 36, 17, 38, 37, 13, 39, 41,
                17, 40, 39, 14, 41, 40, 0, 42, 43, 6, 20, 42, 8, 43, 20, 1, 44, 45,
                7, 18, 44, 6, 45, 18, 4, 46, 47, 8, 19, 46, 7, 47, 19, 1, 48, 44,
                9, 23, 48, 7, 44, 23, 2, 49, 50, 10, 21, 49, 9, 50, 21, 4, 47, 51,
                7, 22, 47, 10, 51, 22, 2, 52, 49, 11, 26, 52, 10, 49, 26, 3, 53, 54,
                12, 24, 53, 11, 54, 24, 4, 51, 55, 10, 25, 51, 12, 55, 25, 3, 56, 53,
                13, 29, 56, 12, 53, 29, 0, 43, 57, 8, 27, 43, 13, 57, 27, 4, 55, 46,
                12, 28, 55, 8, 46, 28, 1, 45, 58, 6, 32, 45, 15, 58, 32, 0, 59, 42,
                14, 30, 59, 6, 42, 30, 5, 60, 61, 15, 31, 60, 14, 61, 31, 2, 50, 62,
                9, 35, 50, 16, 62, 35, 1, 58, 48, 15, 33, 58, 9, 48, 33, 5, 63, 60,
                16, 34, 63, 15, 60, 34, 3, 54, 64, 11, 38, 54, 17, 64, 38, 2, 62, 52,
                16, 36, 62, 11, 52, 36, 5, 65, 63, 17, 37, 65, 16, 63, 37, 0, 57, 59,
                13, 41, 57, 14, 59, 41, 3, 64, 56, 17, 39, 64, 13, 56, 39, 5, 61, 65,
                14, 40, 61, 17, 65, 40
            };
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = spherePos;
            mesh.triangles = sphereIndices;

            return mesh;
        }

        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }

        // Keep in sync with UnpackTileID().
        static uint PackTileID(uint i, uint j)
        {
            return i | (j << 16);
        }

        static uint FloatToUInt(float val)
        {
            // TODO different order for little-endian and big-endian platforms.
            byte[] bytes = System.BitConverter.GetBytes(val);
            return bytes[0] | (((uint)bytes[1]) << 8) | (((uint)bytes[2]) << 16) | (((uint)bytes[3]) << 24);
            //return bytes[3] | (((uint)bytes[2]) << 8) | (((uint)bytes[1]) << 16) | (((uint)bytes[0]) << 24);
        }

        static uint Half2ToUInt(float x, float y)
        {
            uint hx = Mathf.FloatToHalf(x);
            uint hy = Mathf.FloatToHalf(y);
            return hx | (hy << 16);
        }
    }

    class SortPrePointLight : System.Collections.Generic.IComparer<DeferredTiler.PrePointLight>
    {
        public int Compare(DeferredTiler.PrePointLight a, DeferredTiler.PrePointLight b)
        {
            if (a.minDist < b.minDist)
                return -1;
            else if (a.minDist > b.minDist)
                return 1;
            else
                return 0;
        }
    }

    struct BitArray : System.IDisposable
    {
        NativeArray<uint> m_Mem; // ulong not supported in il2cpp???
        int m_BitCount;
        int m_IntCount;

        public BitArray(int bitCount, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            m_BitCount = bitCount;
            m_IntCount = (bitCount + 31) >> 5;
            m_Mem = new NativeArray<uint>(m_IntCount, allocator, options);
        }

        public void Dispose()
        {
            m_Mem.Dispose();
        }

        public void Clear()
        {
            for (int i = 0; i < m_IntCount; ++i)
                m_Mem[i] = 0;
        }

        public bool IsSet(int bitIndex)
        {
            return (m_Mem[bitIndex >> 5] & (1u << (bitIndex & 31))) != 0;
        }

        public void Set(int bitIndex, bool val)
        {
            if (val)
                m_Mem[bitIndex >> 5] |= 1u << (bitIndex & 31);
            else
                m_Mem[bitIndex >> 5] &= ~(1u << (bitIndex & 31));
        }
    };

    struct Vector4UInt
    {
        public uint x;
        public uint y;
        public uint z;
        public uint w;

        public Vector4UInt(uint _x, uint _y, uint _z, uint _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }
    };
}
