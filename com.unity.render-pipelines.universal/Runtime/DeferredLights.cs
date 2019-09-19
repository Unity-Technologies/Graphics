using UnityEngine.Profiling;
using Unity.Collections;

// cleanup code
// RelLightIndices should be stored in ushort instead of uint.
// TODO use Unity.Mathematics
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity
// TODO Align() function duplicated. Is there an Unity function for that?

namespace UnityEngine.Rendering.Universal
{
    // Customization per platform.
    static class DeferredConfig
    {
        // Keep in sync with shader define USE_CBUFFER_FOR_TILELIST
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTDATA
        // Keep in sync with shader define USE_CBUFFER_FOR_LIGHTLIST
#if UNITY_SWITCH
        // Constant buffers are used for data that a repeatedly fetched by shaders.
        // Structured buffers are used for data only consumed once.
        public static bool kUseCBufferForTileList = false;
        public static bool kUseCBufferForLightData = true;
        public static bool kUseCBufferForLightList = false;
#else
        public static bool kUseCBufferForTileList = false;
        public static bool kUseCBufferForLightData = true;
        public static bool kUseCBufferForLightList = false;
#endif

        // Keep in sync with PREFERRED_CBUFFER_SIZE.
        public const int kPreferredCBufferSize = 64 * 1024;
        public const int kPreferredStructuredBufferSize = 128 * 1024;

        public const int kTilePixelWidth = 16;
        public const int kTilePixelHeight = 16;
        // Levels of hierachical tiling. Each level process 4x4 finer tiles.
        public const int kTilerDepth = 3;

         public const int kMaxLightPerTile = 31;

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
            public static readonly string TILE_SIZE_8 = "TILE_SIZE_8";
            public static readonly string TILE_SIZE_16 = "TILE_SIZE_16";

            public static readonly int UTileList = Shader.PropertyToID("UTileList");
            public static readonly int g_TileList = Shader.PropertyToID("g_TileList");
            public static readonly int UPointLightBuffer = Shader.PropertyToID("UPointLightBuffer");
            public static readonly int g_PointLightBuffer = Shader.PropertyToID("g_PointLightBuffer");
            public static readonly int URelLightList = Shader.PropertyToID("URelLightList");
            public static readonly int g_RelLightList = Shader.PropertyToID("g_RelLightList");
            public static readonly int g_TilePixelWidth = Shader.PropertyToID("g_TilePixelWidth");
            public static readonly int g_TilePixelHeight = Shader.PropertyToID("g_TilePixelHeight");
            public static readonly int g_InstanceOffset = Shader.PropertyToID("g_InstanceOffset");
            public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
            public static readonly int _MainTexSize = Shader.PropertyToID("_MainTexSize");
            public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");

            public static readonly int _InvCameraViewProj = Shader.PropertyToID("_InvCameraViewProj");
            public static readonly int g_unproject0 = Shader.PropertyToID("g_unproject0");
            public static readonly int g_unproject1 = Shader.PropertyToID("g_unproject1");
            public static readonly int g_DepthTex = Shader.PropertyToID("g_DepthTex");
        }

        internal struct DrawCall
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

        // Keep in sync with LIGHT_LIST_HEADER_SIZE.
        const int kLightListHeaderSize = 1;

        const string k_DeferredPass = "Deferred Pass";
        const string k_TileDepthRange = "Tile Depth Range";
        const string k_TiledDeferredPass = "Tile-Based Deferred Shading";
        const string k_StencilDeferredPass = "Stencil Deferred Shading";

        // TODO: move to UI.
        public bool useTiles = true;

        // Cached.
        int m_RenderWidth = 0;
        // Cached.
        int m_RenderHeight = 0;
        // Cached.
        Matrix4x4 m_CachedProjectionMatrix;

        // Hierarchical tilers.
        DeferredTiler[] m_Tilers;

        // Visible lights rendered using stencil.
        NativeArray<ushort> m_stencilVisLights;

        // For rendering stencil point lights.
        Mesh m_SphereMesh;

        // Max tile instancing limit per draw call. It depends on the max capacity of uniform buffers.
        int m_MaxTilesPerBatch;
        // Max number of point lights that can be referenced per draw call.
        int m_MaxPointLightPerBatch;
        // Max number of relative light indices per draw call.
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
        // Per-tile depth range texture.
        internal RenderTargetHandle m_TileDepthRangeTexture;

        public DeferredLights(Material tileDepthInfoMaterial, Material tileDeferredMaterial, Material stencilDeferredMaterial)
        {
            m_TileDepthInfoMaterial = tileDepthInfoMaterial;
            m_TileDeferredMaterial = tileDeferredMaterial;
            m_StencilDeferredMaterial = stencilDeferredMaterial;

            // Compute some platform limits.
            m_MaxTilesPerBatch = (DeferredConfig.kUseCBufferForTileList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(TileData));
            m_MaxPointLightPerBatch = (DeferredConfig.kUseCBufferForLightData ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
            m_MaxRelLightIndicesPerBatch = (DeferredConfig.kUseCBufferForLightList ? DeferredConfig.kPreferredCBufferSize : DeferredConfig.kPreferredStructuredBufferSize) / sizeof(uint); // Should be ushort!

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
                    (DeferredConfig.kMaxLightPerTile + 1) * scale * scale - 1,
                    tilerLevel
                );
            }
        }

        public DeferredTiler GetTiler(int i)
        {
            return m_Tilers[i];
        }

        public void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DeferredShaderData.instance.ResetBuffers();

            // Precompute tile data again if the camera projection or the screen resolution has changed.
            if (m_RenderWidth != renderingData.cameraData.cameraTargetDescriptor.width
             || m_RenderHeight != renderingData.cameraData.cameraTargetDescriptor.height
             || m_CachedProjectionMatrix != renderingData.cameraData.camera.projectionMatrix)
            {
                m_RenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
                m_RenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;
                m_CachedProjectionMatrix = renderingData.cameraData.camera.projectionMatrix;

                foreach (DeferredTiler tiler in m_Tilers)
                {
                    tiler.PrecomputeTiles(renderingData.cameraData.camera.projectionMatrix,
                        renderingData.cameraData.camera.orthographic, m_RenderWidth, m_RenderHeight);
                }
            }

            // Allocate temporary resources for each hierarchical tiler.
            foreach (DeferredTiler tiler in m_Tilers)
                tiler.Setup();

            // Will hold point lights that will be rendered using tiles.
            NativeArray<DeferredTiler.PrePointLight> prePointLights;

            PrecomputeLights(
                out prePointLights,
                out m_stencilVisLights,
                ref renderingData.lightData.visibleLights,
                renderingData.cameraData.camera.worldToCameraMatrix,
                renderingData.cameraData.camera.orthographic,
                renderingData.cameraData.camera.nearClipPlane
            );

            NativeArray<ushort> defaultIndices = new NativeArray<ushort>(prePointLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < prePointLights.Length; ++i)
                defaultIndices[i] = (ushort)i;

            // Cull tile-friendly lights into the coarse tile structure.
            DeferredTiler coarsestTiler = m_Tilers[m_Tilers.Length - 1];
            if (m_Tilers.Length != 1)
            {
                coarsestTiler.CullIntermediateLights(ref prePointLights,
                    ref defaultIndices, 0, prePointLights.Length,
                    0, coarsestTiler.GetTileXCount(), 0, coarsestTiler.GetTileYCount()
                );

                // Filter to fine tile structure.
                for (int t = m_Tilers.Length - 2; t >= 0; --t)
                {
                    DeferredTiler fineTiler = m_Tilers[t];
                    DeferredTiler coarseTiler = m_Tilers[t + 1];
                    ref NativeArray<ushort> coarseTiles = ref coarseTiler.GetTiles();
                    int coarseTileHeader = coarseTiler.GetTileHeader();

                    int fineStepX = coarseTiler.GetTilePixelWidth() / fineTiler.GetTilePixelWidth();
                    int fineStepY = coarseTiler.GetTilePixelHeight() / fineTiler.GetTilePixelHeight();

                    for (int j = 0; j < coarseTiler.GetTileYCount(); ++j)
                    for (int i = 0; i < coarseTiler.GetTileXCount(); ++i)
                    {
                        int fine_istart = i * fineStepX;
                        int fine_jstart = j * fineStepY;
                        int fine_iend = Mathf.Min(fine_istart + fineStepX, fineTiler.GetTileXCount());
                        int fine_jend = Mathf.Min(fine_jstart + fineStepY, fineTiler.GetTileYCount());
                        int coarseTileOffset = coarseTiler.GetTileOffset(i, j);
                        int coarseVisLightCount = coarseTiles[coarseTileOffset];

                        if (t != 0)
                        {
                            fineTiler.CullIntermediateLights(ref prePointLights,
                                ref coarseTiles, coarseTileOffset + coarseTileHeader, coarseVisLightCount,
                                fine_istart, fine_iend, fine_jstart, fine_jend
                            );
                        }
                        else
                        {
                            fineTiler.CullFinalLights(ref prePointLights,
                                ref coarseTiles, coarseTileOffset + coarseTileHeader, coarseVisLightCount,
                                fine_istart, fine_iend, fine_jstart, fine_jend
                            );
                        }
                    }
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

            // We don't need this array anymore as all the lights have been inserted into the tile-grid structures.
            prePointLights.Dispose();
        }

        public void Setup(RenderTargetHandle depthCopyTexture, RenderTargetHandle tileDepthRangeTexture, RenderTargetHandle depthTexture, RenderTargetHandle lightingTexture)
        {
            m_DepthCopyTexture = depthCopyTexture;
            m_TileDepthRangeTexture = tileDepthRangeTexture;
            m_LightingTexture = lightingTexture;
            m_DepthTexture = depthTexture;
        }

        public void FrameCleanup(CommandBuffer cmd)
        {
            foreach (DeferredTiler tiler in m_Tilers)
                tiler.FrameCleanup();

            if (m_stencilVisLights.IsCreated)
                m_stencilVisLights.Dispose();
        }

        public void ExecuteTileDepthRangePass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDeferredMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(k_TileDepthRange);
            RenderTargetIdentifier depthSurface = m_DepthTexture.Identifier();
            RenderTargetIdentifier tileDepthRangeSurface = m_TileDepthRangeTexture.Identifier();
            int tileWidth = m_Tilers[0].GetTilePixelWidth();
            int tileHeight = m_Tilers[0].GetTilePixelHeight();

            cmd.SetGlobalTexture(ShaderConstants._MainTex, depthSurface);
            cmd.SetGlobalVector(ShaderConstants._MainTexSize, new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight));
            cmd.SetGlobalInt(ShaderConstants.g_TilePixelWidth, tileWidth);
            cmd.SetGlobalInt(ShaderConstants.g_TilePixelHeight, tileHeight);

            // Should we move to ShaderKeywordStrings?
            if (tileWidth == 8 && tileHeight == 8)
                cmd.EnableShaderKeyword(ShaderConstants.TILE_SIZE_8);
            else if (tileWidth == 16 && tileHeight == 16)
                cmd.EnableShaderKeyword(ShaderConstants.TILE_SIZE_16);

            cmd.Blit(depthSurface, tileDepthRangeSurface, m_TileDepthInfoMaterial, 0);

            cmd.DisableShaderKeyword(ShaderConstants.TILE_SIZE_8);
            cmd.DisableShaderKeyword(ShaderConstants.TILE_SIZE_16);

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

        void PrecomputeLights(out NativeArray<DeferredTiler.PrePointLight> prePointLights,
                              out NativeArray<ushort> stencilVisLights,
                              ref NativeArray<VisibleLight> visibleLights,
                              Matrix4x4 view,
                              bool isOrthographic,
                              float zNear)
        {
            const int lightTypeCount = (int)LightType.Disc + 1;

            NativeArray<int> tileLightCount = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            int stencilLightCount = 0;

            // Count the number of lights per type.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (vl.lightType == LightType.Point)
                {
                    if (this.useTiles)
                    {
                        ++tileLightCount[(int)vl.lightType];
                        continue;
                    }
                }

                // All remaining lights are processed as stencil volumes.
                ++stencilLightCount;
            }

            prePointLights = new NativeArray<DeferredTiler.PrePointLight>(tileLightCount[(int)LightType.Point], Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            stencilVisLights = new NativeArray<ushort>(stencilLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < tileLightCount.Length; ++i)
                tileLightCount[i] = 0;
            stencilLightCount = 0;

            // Precompute point light data.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (vl.lightType == LightType.Point)
                {
                    if (this.useTiles)
                    {
                        DeferredTiler.PrePointLight ppl;
                        ppl.vsPos = view.MultiplyPoint(vl.light.transform.position); // By convention, OpenGL RH coordinate space
                        ppl.radius = vl.light.range;

                        ppl.screenPos = new Vector2(ppl.vsPos.x, ppl.vsPos.y);
                        // Project on screen for perspective projections.
                        if (!isOrthographic && ppl.vsPos.z <= zNear)
                            ppl.screenPos = ppl.screenPos * (-zNear / ppl.vsPos.z);

                        ppl.visLightIndex = visLightIndex;

                        int i = tileLightCount[(int)LightType.Point]++;
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
                int tileXStride = tiler.GetTileXStride();
                int tileYStride = tiler.GetTileYStride();
                int maxLightPerTile = tiler.GetMaxLightPerTile();
                int tileHeader = tiler.GetTileHeader();
                ref NativeArray<ushort> tiles = ref tiler.GetTiles();

                int instanceOffset = 0;
                int tileCount = 0;
                int lightCount = 0;
                int relLightIndices = 0;

                NativeArray<Vector4UInt> tileList = new NativeArray<Vector4UInt>(m_MaxTilesPerBatch * sizeof_vec4_TileData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector4UInt> pointLightBuffer = new NativeArray<Vector4UInt>(m_MaxPointLightPerBatch * sizeof_vec4_PointLightData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint> relLightList = new NativeArray<uint>(m_MaxRelLightIndicesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                ComputeBuffer _tileList = DeferredShaderData.instance.ReserveTileList(m_MaxTilesPerBatch);
                ComputeBuffer _pointLightBuffer = DeferredShaderData.instance.ReservePointLightBuffer(m_MaxPointLightPerBatch);
                ComputeBuffer _relLightList = DeferredShaderData.instance.ReserveRelLightList(m_MaxRelLightIndicesPerBatch);

                // Acceleration structure to quickly find if a light has already been added to the uniform block data for the current draw call.
                NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<ushort> visLightToRelLights = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                for (int j = 0; j < tileYCount; ++j)
                {
                    for (int i = 0; i < tileXCount; ++i)
                    {
                        int tileOffset = i * tileXStride + j * tileYStride;
                        int tileLightCount = tiles[tileOffset];
                        if (tileLightCount == 0) // empty tile
                            continue;

                        // Find lights that are not in the batch yet.
                        int trimmedLightCount = TrimLights(ref trimmedLights, ref tiles, tileOffset + tileHeader, tileLightCount, ref usedLights);
                        Assertions.Assert.IsTrue(trimmedLightCount <= maxLightPerTile, "too many lights overlaps a tile");

                        // Checks whether one of the GPU buffers is reaching max capacity.
                        // In that case, the draw call must be flushed and new GPU buffer(s) be allocated.
                        bool tileListIsFull = (tileCount == m_MaxTilesPerBatch);
                        bool lightBufferIsFull = (lightCount + trimmedLightCount > m_MaxPointLightPerBatch);
                        bool relLightListIsFull = (relLightIndices + kLightListHeaderSize + tileLightCount > m_MaxRelLightIndicesPerBatch);

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
                                _tileList = DeferredShaderData.instance.ReserveTileList(m_MaxTilesPerBatch);
                                tileCount = 0;
                            }

                            if (lightBufferIsFull)
                            {
                                _pointLightBuffer.SetData(pointLightBuffer, 0, 0, pointLightBuffer.Length);
                                _pointLightBuffer = DeferredShaderData.instance.ReservePointLightBuffer(m_MaxPointLightPerBatch);
                                lightCount = 0;

                                // If pointLightBuffer was reset, then all lights in the current tile must be added.
                                trimmedLightCount = tileLightCount;
                                for (int l = 0; l < tileLightCount; ++l)
                                    trimmedLights[l] = tiles[tileOffset + tileHeader + l];
                                usedLights.Clear();
                            }

                            if (relLightListIsFull)
                            {
                                _relLightList.SetData(relLightList, 0, 0, relLightList.Length);
                                _relLightList = DeferredShaderData.instance.ReserveRelLightList(m_MaxRelLightIndicesPerBatch);
                                relLightIndices = 0;
                            }

                            instanceOffset = tileCount;
                        }

                        // Add TileData.
                        uint listDepthRange = (uint)tiles[tileOffset + 1] | ((uint)tiles[tileOffset + 2] << 16);
                        uint listBitMask = (uint)tiles[tileOffset + 3] | ((uint)tiles[tileOffset + 4] << 16);
                        StoreTileData(ref tileList, tileCount, PackTileID((uint)i, (uint)j), (ushort)relLightIndices, listDepthRange, listBitMask);
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

                        // Add tile header: make sure the size is matching kLightListHeaderSize.
                        relLightList[relLightIndices++] = (ushort)tileLightCount;

                        // Add light list for the tile.
                        for (int l = 0; l < tileLightCount; ++l)
                        {
                            int visLightIndex = tiles[tileOffset + tileHeader + l];
                            ushort relLightIndex = visLightToRelLights[visLightIndex];
                            relLightList[relLightIndices++] = relLightIndex;
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

                trimmedLights.Dispose();
                visLightToRelLights.Dispose();
                usedLights.Dispose();
                tileList.Dispose();
                pointLightBuffer.Dispose();
                relLightList.Dispose();
            }

            // Now draw all tile batches.
            using (new ProfilingSample(cmd, k_TiledDeferredPass))
            {
                MeshTopology topology = DeferredConfig.kHasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                int vertexCount = DeferredConfig.kHasNativeQuadSupport ? 4 : 6;

                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector(ShaderConstants._ScreenSize, screenSize);

                int tileWidth = m_Tilers[0].GetTilePixelWidth();
                int tileHeight = m_Tilers[0].GetTilePixelHeight();
                cmd.SetGlobalInt(ShaderConstants.g_TilePixelWidth, tileWidth);
                cmd.SetGlobalInt(ShaderConstants.g_TilePixelHeight, tileHeight);

                Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
                Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
                Matrix4x4 viewProjInv = Matrix4x4.Inverse(proj * view);
                cmd.SetGlobalMatrix(ShaderConstants._InvCameraViewProj, viewProjInv);

                Matrix4x4 clip = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0.5f, 0), new Vector4(0, 0, 0.5f, 1));
                Matrix4x4 projScreenInv = Matrix4x4.Inverse(clip * proj);
                cmd.SetGlobalVector(ShaderConstants.g_unproject0, projScreenInv.GetRow(2));
                cmd.SetGlobalVector(ShaderConstants.g_unproject1, projScreenInv.GetRow(3));

                cmd.SetGlobalTexture(m_TileDepthRangeTexture.id, m_TileDepthRangeTexture.Identifier());
                cmd.SetGlobalTexture(ShaderConstants.g_DepthTex, m_DepthCopyTexture.Identifier()); // We should bind m_DepthCopyTexture but currently not possible yet

                for (int i = 0; i < drawCallCount; ++i)
                {
                    DrawCall dc = drawCalls[i];

                    if (DeferredConfig.kUseCBufferForTileList)
                        cmd.SetGlobalConstantBuffer(dc.tileList, ShaderConstants.UTileList, 0, dc.tileListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants.g_TileList, dc.tileList);

                    if (DeferredConfig.kUseCBufferForLightData)
                        cmd.SetGlobalConstantBuffer(dc.pointLightBuffer, ShaderConstants.UPointLightBuffer, 0, dc.pointLightBufferSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants.g_PointLightBuffer, dc.pointLightBuffer);

                    if (DeferredConfig.kUseCBufferForLightList)
                        cmd.SetGlobalConstantBuffer(dc.relLightList, ShaderConstants.URelLightList, 0, dc.relLightListSize);
                    else
                        cmd.SetGlobalBuffer(ShaderConstants.g_RelLightList, dc.relLightList);

                    cmd.SetGlobalInt(ShaderConstants.g_InstanceOffset, dc.instanceOffset);
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

            Profiler.BeginSample(k_StencilDeferredPass);

            if (m_SphereMesh == null)
                m_SphereMesh = CreateSphereMesh();

            using (new ProfilingSample(cmd, k_StencilDeferredPass))
            {
                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector(ShaderConstants._ScreenSize, screenSize);

                Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
                Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
                Matrix4x4 viewProjInv = Matrix4x4.Inverse(proj * view);
                cmd.SetGlobalMatrix("_InvCameraViewProj", viewProjInv);

                cmd.SetGlobalTexture("g_DepthTex", m_DepthCopyTexture.Identifier()); // We should bind m_DepthCopyTexture but currently not possible yet

                NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;

                for (int i = 0; i < m_stencilVisLights.Length; ++i)
                {
                    ushort visLightIndex = m_stencilVisLights[i];
                    VisibleLight vl = visibleLights[visLightIndex];

                    if (vl.light.type == LightType.Point)
                    {
                        Vector3 wsPos = vl.light.transform.position;
                        float adjRadius = vl.light.range * 1.06067f; // adjust for approximate sphere geometry

                        Matrix4x4 sphereMatrix = new Matrix4x4(
                            new Vector4(adjRadius,      0.0f,      0.0f, 0.0f),
                            new Vector4(     0.0f, adjRadius,      0.0f, 0.0f),
                            new Vector4(     0.0f,      0.0f, adjRadius, 0.0f),
                            new Vector4(  wsPos.x,   wsPos.y,   wsPos.z, 1.0f)
                        );

                        cmd.SetGlobalVector("_LightWsPos", wsPos);
                        cmd.SetGlobalFloat("_LightRadius", vl.light.range);
                        cmd.SetGlobalVector("_LightColor", vl.light.color);

                        cmd.DrawMesh(m_SphereMesh, sphereMatrix, m_StencilDeferredMaterial, 0, 0);

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
            Vector3 wsPos = visibleLights[index].light.transform.position;
            pointLightBuffer[storeIndex * 2 + 0] = new Vector4UInt(FloatToUInt(wsPos.x), FloatToUInt(wsPos.y), FloatToUInt(wsPos.z), FloatToUInt(visibleLights[index].range));
            pointLightBuffer[storeIndex * 2 + 1] = new Vector4UInt(FloatToUInt(visibleLights[index].light.color.r), FloatToUInt(visibleLights[index].light.color.g), FloatToUInt(visibleLights[index].light.color.b), 0);
        }

        void StoreTileData(ref NativeArray<Vector4UInt> tileList, int storeIndex, uint tileID, ushort relLightOffset, uint listDepthRange, uint listBitMask)
        {
            // See struct TileData in TileDeferred.shader.
            tileList[storeIndex] = new Vector4UInt { x = tileID, y = relLightOffset, z = listDepthRange, w = listBitMask };
        }

        Mesh CreateSphereMesh()
        {
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

    internal struct BitArray : System.IDisposable
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
}
