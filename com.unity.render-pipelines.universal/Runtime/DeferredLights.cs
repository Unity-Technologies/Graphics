using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;

// cleanup code
// Stencil mesh for point lights is not regular enough.
// RelLightIndices should be stored in ushort instead of uint.
// TODO use Unity.Mathematics
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity

namespace UnityEngine.Rendering.Universal
{
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

    // Precomputed light data
    internal struct PrePointLight
    {
        // view-space position.
        public Vector3 vsPos;
        // Radius in world unit.
        public float radius;
        // Projected position of the sphere centre on the screen (near plane).
        public Vector2 screenPos;
        // Index into renderingData.lightData.visibleLights native array.
        public ushort visLightIndex;
    }

    internal struct DrawCall
    {
        public ComputeBuffer tileDataBuffer;
        public ComputeBuffer pointLightBuffer;
        public ComputeBuffer relLightIndexBuffer;
        public int tileDataBufferSize;
        public int pointLightBufferSize;
        public int relLightIndexBufferSize;
        public int instanceOffset;
        public int instanceCount;
    }

    // Manages tiled-based deferred lights.
    internal class DeferredLights
    {
        static class ShaderConstants
        {
            public static readonly int UTileDataBuffer = Shader.PropertyToID("UTileDataBuffer");
            public static readonly int UPointLightBuffer = Shader.PropertyToID("UPointLightBuffer");
            public static readonly int URelLightIndexBuffer = Shader.PropertyToID("URelLightIndexBuffer");
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

        enum ClipResult
        {
            Unknown,
            In,
            Out,
        }

        // TODO customize per platform.
        // Keep in sync with MAX_UNIFORM_BUFFER_SIZE.
        const int kMaxUniformBufferSize = 64 * 1024;
        // Keep in sync with LIGHT_LIST_HEADER_SIZE.
        const int kLightListHeaderSize = 1;

        const string k_TileDepthRange = "Tile Depth Range";
        const string k_TiledDeferredPass = "Tile-Based Deferred Shading";
        const string k_StencilDeferredPass = "Stencil Deferred Shading";

#if !UNITY_EDITOR && UNITY_SWITCH
        const bool k_HasNativeQuadSupport = true;
#else
        const bool k_HasNativeQuadSupport = false;
#endif
        public bool useTiles = true;

        int m_RenderWidth = 0;
        int m_RenderHeight = 0;
        int m_TilePixelWidth = 16;
        int m_TilePixelHeight = 16;
        int m_TileXCount = 0;
        int m_TileYCount = 0;
        int m_TileSize = 32;
        int m_TileHeader = 5; // ushort lightCount, half minDepth, half maxDepth, uint bitmask

        // Cached.
        Matrix4x4 m_CachedProjectionMatrix;

        // Adjusted frustum planes to account for tile size.
        FrustumPlanes m_FrustumPlanes;

        // Store all visible light indices for all tiles.
        NativeArray<ushort> m_Tiles;
        // Precompute tile data.
        NativeArray<PreTile> m_PreTiles;

        // Lights rendered using stencil.
        NativeArray<ushort> m_stencilLights;

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
            // Compute some platform limits.
            m_MaxTilesPerBatch = kMaxUniformBufferSize / (sizeof(uint) * 4); // TileData
            m_MaxPointLightPerBatch = kMaxUniformBufferSize / System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
            m_MaxRelLightIndicesPerBatch = kMaxUniformBufferSize / sizeof(uint); // Should be ushort!

            m_TileDepthInfoMaterial = tileDepthInfoMaterial;
            m_TileDeferredMaterial = tileDeferredMaterial;
            m_StencilDeferredMaterial = stencilDeferredMaterial;
        }

        public int GetTileXCount()
        {
            return m_TileXCount;
        }

        public int GetTileYCount()
        {
            return m_TileYCount;
        }

        public void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_RenderWidth != renderingData.cameraData.cameraTargetDescriptor.width
             || m_RenderHeight != renderingData.cameraData.cameraTargetDescriptor.height
             || m_CachedProjectionMatrix != renderingData.cameraData.camera.projectionMatrix)
            {
                m_RenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
                m_RenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;
                m_CachedProjectionMatrix = renderingData.cameraData.camera.projectionMatrix;

                m_TileXCount = (m_RenderWidth + m_TilePixelWidth - 1) / m_TilePixelWidth;
                m_TileYCount = (m_RenderHeight + m_TilePixelHeight - 1) / m_TilePixelHeight;

                PrecomputeTiles(out m_PreTiles, renderingData.cameraData.camera.projectionMatrix, renderingData.cameraData.camera.orthographic, m_RenderWidth, m_RenderHeight);
            }

            m_Tiles = new NativeArray<ushort>(m_TileXCount * m_TileYCount * m_TileSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Point lights rendered using tiles.
            NativeArray<PrePointLight> prePointLights;
            PrecomputeLights(out prePointLights, out m_stencilLights, renderingData.lightData.visibleLights, renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.orthographic, m_FrustumPlanes.zNear);

            // Cull lights into the tile-grid structure.
            CullLightsWithMinMaxDepth(prePointLights, renderingData.cameraData.camera.orthographic, m_FrustumPlanes);

            // We don't need this array anymore as all the lights have been inserted into the tile-grid structure.
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
            if (m_Tiles.IsCreated)
                m_Tiles.Dispose();
            if (m_stencilLights.IsCreated)
                m_stencilLights.Dispose();
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
            // Adjusted source texture dimensions.
            int sourceAdjWidth = m_TileXCount * m_TilePixelWidth;
            int sourceAdjHeight = m_TileYCount * m_TilePixelWidth;

            cmd.SetGlobalTexture(ShaderConstants._MainTex, depthSurface);
            cmd.SetGlobalVector(ShaderConstants._MainTexSize, new Vector4(sourceAdjWidth, sourceAdjHeight, 1.0f / sourceAdjWidth, 1.0f / sourceAdjHeight));
            cmd.Blit(depthSurface, tileDepthRangeSurface, m_TileDepthInfoMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void ExecuteDeferredPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderTiledPointLights(context, ref renderingData);

            RenderStencilLights(context, ref renderingData);
        }

        void PrecomputeLights(out NativeArray<PrePointLight> prePointLights,
                              out NativeArray<ushort> stencilLights,
                              NativeArray<VisibleLight> visibleLights,
                              Matrix4x4 view,
                              bool isOrthographic,
                              float zNear)
        {
            int lightTypeCount = 5;
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

            prePointLights = new NativeArray<PrePointLight>(tileLightCount[(int)LightType.Point], Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            stencilLights = new NativeArray<ushort>(stencilLightCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

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
                        PrePointLight ppl;
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
                stencilLights[stencilLightCount++] = visLightIndex;
            }
            tileLightCount.Dispose();
        }

        void CullLights(NativeArray<PrePointLight> visPointLights)
        {
            Profiler.BeginSample("CullLights");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;

            for (int j = 0; j < m_TileYCount; ++j)
            {
                for (int i = 0; i < m_TileXCount; ++i)
                {
                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    for (ushort visLightIndex = 0; visLightIndex < visPointLights.Length; ++visLightIndex)
                    {
                        PrePointLight ppl = visPointLights[visLightIndex];

                        // This is faster than IntersectionLineSphere().
                        if (!Clip(ref preTile, ppl.vsPos, ppl.radius))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        m_Tiles[tileOffset + tileHeader + tileLightCount] = ppl.visLightIndex;
                        ++tileLightCount;
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                }
            }

            Profiler.EndSample();
        }

        void CullLightsWithMinMaxDepth(NativeArray<PrePointLight> visPointLights, bool isOrthographic, FrustumPlanes fplanes)
        {
            Profiler.BeginSample("CullLightsWithMinMaxDepth");

            Assertions.Assert.IsTrue(m_TileHeader >= 5, "not enough space to store min&max depth information for light list ");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - m_TileHeader;
            int tileHeader = m_TileHeader;

            Vector2 tileSize = new Vector2((fplanes.right - fplanes.left) / m_TileXCount, (fplanes.top - fplanes.bottom) / m_TileYCount);
            Vector2 tileExtents = tileSize * 0.5f;
            Vector2 tileExtentsInv = new Vector2(1.0f / tileExtents.x, 1.0f / tileExtents.y);

            // Temporary store min&max depth range for each light in a tile.
            Vector2[] minMax = new Vector2[maxLightPerTile];

            for (int j = 0; j < m_TileYCount; ++j)
            {
                float tileYCentre = fplanes.top - (tileExtents.y + j * tileSize.y);

                for (int i = 0; i < m_TileXCount; ++i)
                {
                    float tileXCentre = fplanes.left + tileExtents.x + i * tileSize.x;

                    PreTile preTile = m_PreTiles[i + j * m_TileXCount];
                    int tileOffset = i * tileXStride + j * tileYStride;
                    ushort tileLightCount = 0;

                    // For the current tile's light list, min&max depth range (absolute values).
                    float listMinDepth = float.MaxValue;
                    float listMaxDepth = -float.MaxValue;

                    for (ushort visLightIndex = 0; visLightIndex < visPointLights.Length; ++visLightIndex)
                    {
                        PrePointLight ppl = visPointLights[visLightIndex];

                        // Offset tileCentre toward the light to calculate a more conservative minMax depth bound,
                        // but it must remains inside the tile and must not pass further than the light centre.
                        Vector2 tileCentre = new Vector3(tileXCentre, tileYCentre);
                        Vector2 dir = ppl.screenPos - tileCentre;
                        Vector2 d = Abs(dir * tileExtentsInv);
                        float s = Max(d.x, d.y, 1.0f);
                        Vector3 tileOffCentre;
                        Vector3 tileOrigin;

                        if (isOrthographic)
                        {
                            tileOrigin = new Vector3(tileCentre.x + dir.x / s, tileCentre.y + dir.y / s, 0.0f);
                            tileOffCentre = new Vector3(0, 0, -fplanes.zNear);
                        }
                        else
                        {
                            tileOrigin = Vector3.zero;
                            tileOffCentre = new Vector3(tileCentre.x + dir.x / s, tileCentre.y + dir.y / s, -fplanes.zNear);
                        }

                        float t0, t1;
                        // This is more expensive than Clip() but allow to compute min&max depth range for the part of the light inside the tile.
                        if (!IntersectionLineSphere(ppl.vsPos, ppl.radius, tileOrigin, tileOffCentre, out t0, out t1))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        // Looking towards -z axin in view space, we want absolute depth values.
                        float minDepth = fplanes.zNear * t0;
                        float maxDepth = fplanes.zNear * t1;
                        listMinDepth = listMinDepth < minDepth ? listMinDepth : minDepth;
                        listMaxDepth = listMaxDepth > maxDepth ? listMaxDepth : maxDepth;
                        minMax[tileLightCount].x = minDepth;
                        minMax[tileLightCount].y = maxDepth;

                        m_Tiles[tileOffset + tileHeader + tileLightCount] = ppl.visLightIndex;
                        ++tileLightCount;
                    }

                    // Clamp our light list depth range.
                    listMinDepth = Mathf.Max(listMinDepth, fplanes.zNear);
                    listMaxDepth = Mathf.Min(listMaxDepth, fplanes.zFar);

                    // Calculate bitmask for 2.5D culling.
                    uint bitMask = 0;
                    float depthRangeInv = 1.0f / (listMaxDepth - listMinDepth);
                    for (int tileLightIndex = 0; tileLightIndex < tileLightCount; ++tileLightIndex)
                    {
                        int firstBit = (int)((minMax[tileLightIndex].x - listMinDepth) * 32.0f * depthRangeInv);
                        int lastBit = (int)((minMax[tileLightIndex].y - listMinDepth) * 32.0f * depthRangeInv);
                        int bitCount = lastBit - firstBit + 1;
                        bitCount = (bitCount > 32 ? 32 : bitCount);
                        bitMask |= (uint)(((1ul << bitCount) - 1) << firstBit);
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                    m_Tiles[tileOffset + 1] = Mathf.FloatToHalf(listMinDepth);
                    m_Tiles[tileOffset + 2] = Mathf.FloatToHalf(listMaxDepth);
                    m_Tiles[tileOffset + 3] = (ushort)(bitMask & 0xFFFF);
                    m_Tiles[tileOffset + 4] = (ushort)((bitMask >> 16) & 0xFFFF);
                }
            }

            Profiler.EndSample();
        }

        void RenderTiledPointLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TileDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDeferredMaterial, GetType().Name);
                return;
            }

            // Allow max 256 draw calls for rendering all the batches of tiles
            DrawCall[] drawCalls = new DrawCall[256];
            int drawCallCount = 0;

            {
                Profiler.BeginSample(k_TiledDeferredPass);

                int sizeof_TileData = 16;
                int sizeof_vec4_TileData = sizeof_TileData >> 4;
                int sizeof_PointLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
                int sizeof_vec4_PointLightData = sizeof_PointLightData >> 4;

                int tileXStride = m_TileSize;
                int tileYStride = m_TileSize * m_TileXCount;
                int maxLightPerTile = m_TileSize - m_TileHeader;

                int instanceOffset = 0;
                int tileCount = 0;
                int lightCount = 0;
                int relLightIndices = 0;

                ComputeBuffer _tileDataBuffer = DeferredShaderData.instance.ReserveTileDataBuffer(m_MaxTilesPerBatch);
                ComputeBuffer _pointLightBuffer = DeferredShaderData.instance.ReservePointLightBuffer(m_MaxPointLightPerBatch);
                ComputeBuffer _relLightIndexBuffer = DeferredShaderData.instance.ReserveRelLightIndexBuffer(m_MaxRelLightIndicesPerBatch);

                // Acceleration structure to quickly find if a light has already been added to the uniform block data for the current draw call.
                NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<ushort> visLightToRelLights = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                NativeArray<Vector4UInt> tileDataBuffer = new NativeArray<Vector4UInt>(m_MaxTilesPerBatch * sizeof_vec4_TileData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<Vector4UInt> pointLightBuffer = new NativeArray<Vector4UInt>(m_MaxPointLightPerBatch * sizeof_vec4_PointLightData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint> relLightIndexBuffer = new NativeArray<uint>(m_MaxRelLightIndicesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                for (int j = 0; j < m_TileYCount; ++j)
                {
                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        int tileOffset = i * tileXStride + j * tileYStride;
                        int tileLightCount = m_Tiles[tileOffset];
                        if (tileLightCount == 0) // empty tile
                            continue;

                        // Find lights that are not in the batch yet.
                        int trimmedLightCount = TrimLights(trimmedLights, m_Tiles, tileOffset + m_TileHeader, tileLightCount, usedLights);
                        Assertions.Assert.IsTrue(trimmedLightCount <= maxLightPerTile, "too many lights overlaps a tile");

                        // Find if one of the GPU buffers is reaching max capacity.
                        // In that case, the draw call must be flushed and new GPU buffer(s) be allocated.
                        bool tileDataBufferIsFull = (tileCount == m_MaxTilesPerBatch);
                        bool lightBufferIsFull = (lightCount + trimmedLightCount > m_MaxPointLightPerBatch);
                        bool relLightIndexBufferIsFull = (relLightIndices + kLightListHeaderSize + tileLightCount > m_MaxRelLightIndicesPerBatch);

                        if (tileDataBufferIsFull || lightBufferIsFull || relLightIndexBufferIsFull)
                        {
                            drawCalls[drawCallCount++] = new DrawCall
                            {
                                tileDataBuffer = _tileDataBuffer,
                                pointLightBuffer = _pointLightBuffer,
                                relLightIndexBuffer = _relLightIndexBuffer,
                                tileDataBufferSize = tileCount * sizeof_TileData,
                                pointLightBufferSize = lightCount * sizeof_PointLightData,
                                relLightIndexBufferSize = Align(relLightIndices, 4) * 4,
                                instanceOffset = instanceOffset,
                                instanceCount = tileCount - instanceOffset
                            };

                            if (tileDataBufferIsFull)
                            {
                                _tileDataBuffer.SetData(tileDataBuffer, 0, 0, m_MaxTilesPerBatch); // Must pass complete array (restriction for binding Unity Constant Buffers)
                                _tileDataBuffer = DeferredShaderData.instance.ReserveTileDataBuffer(m_MaxTilesPerBatch);
                                tileCount = 0;
                            }

                            if (lightBufferIsFull)
                            {
                                _pointLightBuffer.SetData(pointLightBuffer, 0, 0, m_MaxPointLightPerBatch * sizeof_vec4_PointLightData);
                                _pointLightBuffer = DeferredShaderData.instance.ReservePointLightBuffer(m_MaxPointLightPerBatch);
                                lightCount = 0;

                                // If pointLightBuffer was reset, then all lights in the current tile must be added.
                                trimmedLightCount = tileLightCount;
                                for (int l = 0; l < tileLightCount; ++l)
                                    trimmedLights[l] = m_Tiles[tileOffset + m_TileHeader + l];
                                usedLights.Clear();
                            }

                            if (relLightIndexBufferIsFull)
                            {
                                _relLightIndexBuffer.SetData(relLightIndexBuffer, 0, 0, m_MaxRelLightIndicesPerBatch);
                                _relLightIndexBuffer = DeferredShaderData.instance.ReserveRelLightIndexBuffer(m_MaxRelLightIndicesPerBatch);
                                relLightIndices = 0;
                            }

                            instanceOffset = tileCount;
                        }

                        // Add TileData.
                        uint listDepthRange = (uint)m_Tiles[tileOffset + 1] | ((uint)m_Tiles[tileOffset + 2] << 16);
                        uint listBitMask = (uint)m_Tiles[tileOffset + 3] | ((uint)m_Tiles[tileOffset + 4] << 16);
                        StoreTileData(tileDataBuffer, tileCount, PackTileID((uint)i, (uint)j), (ushort)relLightIndices, listDepthRange, listBitMask);
                        ++tileCount;

                        // Add newly discovered lights.
                        for (int l = 0; l < trimmedLightCount; ++l)
                        {
                            int visLightIndex = trimmedLights[l];
                            StorePointLightData(pointLightBuffer, lightCount, renderingData.lightData.visibleLights, visLightIndex);
                            visLightToRelLights[visLightIndex] = (ushort)lightCount;
                            ++lightCount;
                            usedLights.Set(visLightIndex, true);
                        }

                        // Add tile header: make sure the size is matching kLightListHeaderSize.
                        relLightIndexBuffer[relLightIndices++] = (ushort)tileLightCount;

                        // Add light list for the tile.
                        for (int l = 0; l < tileLightCount; ++l)
                        {
                            int visLightIndex = m_Tiles[tileOffset + m_TileHeader + l];
                            ushort relLightIndex = visLightToRelLights[visLightIndex];
                            relLightIndexBuffer[relLightIndices++] = relLightIndex;
                        }
                    }
                }

                int instanceCount = tileCount - instanceOffset;
                if (instanceCount > 0)
                {
                    _tileDataBuffer.SetData(tileDataBuffer, 0, 0, m_MaxTilesPerBatch * sizeof_vec4_TileData); // Must pass complete array (restriction for binding Unity Constant Buffers)
                    _pointLightBuffer.SetData(pointLightBuffer, 0, 0, m_MaxPointLightPerBatch * sizeof_vec4_PointLightData);
                    _relLightIndexBuffer.SetData(relLightIndexBuffer, 0, 0, m_MaxRelLightIndicesPerBatch);

                    drawCalls[drawCallCount++] = new DrawCall
                    {
                        tileDataBuffer = _tileDataBuffer,
                        pointLightBuffer = _pointLightBuffer,
                        relLightIndexBuffer = _relLightIndexBuffer,
                        tileDataBufferSize = tileCount * sizeof_TileData,
                        pointLightBufferSize = lightCount * sizeof_PointLightData,
                        relLightIndexBufferSize = Align(relLightIndices, 4) * 4,
                        instanceOffset = instanceOffset,
                        instanceCount = instanceCount
                    };
                }

                trimmedLights.Dispose();
                visLightToRelLights.Dispose();
                usedLights.Dispose();
                tileDataBuffer.Dispose();
                pointLightBuffer.Dispose();
                relLightIndexBuffer.Dispose();

                DeferredShaderData.instance.ResetBuffers();

                Profiler.EndSample();
            }

            CommandBuffer cmd = CommandBufferPool.Get(k_TiledDeferredPass);
            using (new ProfilingSample(cmd, k_TiledDeferredPass))
            {
                MeshTopology topology = k_HasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                int vertexCount = k_HasNativeQuadSupport ? 4 : 6;

                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector(ShaderConstants._ScreenSize, screenSize);
                cmd.SetGlobalInt(ShaderConstants.g_TilePixelWidth, m_TilePixelWidth);
                cmd.SetGlobalInt(ShaderConstants.g_TilePixelHeight, m_TilePixelHeight);

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
                    cmd.SetGlobalConstantBuffer(dc.tileDataBuffer, ShaderConstants.UTileDataBuffer, 0, dc.tileDataBufferSize);
                    cmd.SetGlobalConstantBuffer(dc.pointLightBuffer, ShaderConstants.UPointLightBuffer, 0, dc.pointLightBufferSize);
                    cmd.SetGlobalConstantBuffer(dc.relLightIndexBuffer, ShaderConstants.URelLightIndexBuffer, 0, dc.relLightIndexBufferSize);
                    cmd.SetGlobalInt(ShaderConstants.g_InstanceOffset, dc.instanceOffset);
                    cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, 0, topology, vertexCount, dc.instanceCount);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        void RenderStencilLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_StencilDeferredMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_StencilDeferredMaterial, GetType().Name);
                return;
            }

            if (m_SphereMesh == null)
                m_SphereMesh = CreateSphereMesh();

            CommandBuffer cmd = CommandBufferPool.Get(k_StencilDeferredPass);
            using (new ProfilingSample(cmd, k_StencilDeferredPass))
            {
                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector(ShaderConstants._ScreenSize, screenSize);

                Matrix4x4 proj = renderingData.cameraData.camera.projectionMatrix;
                Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
                Matrix4x4 viewProjInv = Matrix4x4.Inverse(proj * view);
                cmd.SetGlobalMatrix("_InvCameraViewProj", viewProjInv);

                cmd.SetGlobalTexture("g_DepthTex", m_DepthTexture.Identifier());

                NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;

                for (int i = 0; i < m_stencilLights.Length; ++i)
                {
                    ushort visLightIndex = m_stencilLights[i];
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

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

        }

        void PrecomputeTiles(out NativeArray<PreTile> preTiles, Matrix4x4 proj, bool isOrthographic, int renderWidth, int renderHeight)
        {
            preTiles = DeferredShaderData.instance.GetPreTiles(m_TileXCount * m_TileYCount);

            // Adjust render width and height to account for tile size expanding over the screen (tiles have a fixed pixel size).
            int adjustedRenderWidth = Align(renderWidth, m_TilePixelWidth);
            int adjustedRenderHeight = Align(renderHeight, m_TilePixelHeight);

            // Now adjust the right and bottom clipping planes.
            m_FrustumPlanes = proj.decomposeProjection;
            m_FrustumPlanes.right = m_FrustumPlanes.left + (m_FrustumPlanes.right - m_FrustumPlanes.left) * (adjustedRenderWidth / (float)renderWidth);
            m_FrustumPlanes.bottom = m_FrustumPlanes.top + (m_FrustumPlanes.bottom - m_FrustumPlanes.top) * (adjustedRenderHeight / (float)renderHeight);

            // Tile size in world units.
            float tileWsWidth = (m_FrustumPlanes.right - m_FrustumPlanes.left) / m_TileXCount;
            float tileWsHeight = (m_FrustumPlanes.top - m_FrustumPlanes.bottom) / m_TileYCount;

            if (!isOrthographic) // perspective
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = m_FrustumPlanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = m_FrustumPlanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        // In view space with perspective projection, all planes pass by (0,0,0).
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear));

                        preTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
            else
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = m_FrustumPlanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = m_FrustumPlanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear), new Vector3(tileRight, tileBottom, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileLeft, tileBottom, -m_FrustumPlanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear), new Vector3(tileLeft, tileTop, -m_FrustumPlanes.zNear - 1.0f), new Vector3(tileRight, tileTop, -m_FrustumPlanes.zNear));

                        preTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
        }

        int TrimLights(NativeArray<ushort> trimmedLights, NativeArray<ushort> tiles, int offset, int lightCount, BitArray usedLights)
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

        void StorePointLightData(NativeArray<Vector4UInt> pointLightBuffer, int storeIndex, NativeArray<VisibleLight> visibleLights, int index)
        {
            Vector3 wsPos = visibleLights[index].light.transform.position;
            pointLightBuffer[storeIndex * 2 + 0] = new Vector4UInt(FloatToUInt(wsPos.x), FloatToUInt(wsPos.y), FloatToUInt(wsPos.z), FloatToUInt(visibleLights[index].range));

            pointLightBuffer[storeIndex * 2 + 1] = new Vector4UInt(
                Half2ToUInt(visibleLights[index].light.color.r, visibleLights[index].light.color.g),
                Half2ToUInt(visibleLights[index].light.color.b, 0.0f),
                0,
                0
            );
        }

        void StoreTileData(NativeArray<Vector4UInt> tileDataBuffer, int storeIndex, uint tileID, ushort relLightOffset, uint listDepthRange, uint listBitMask)
        {
            // See struct TileData in TileDeferred.shader.
            tileDataBuffer[storeIndex] = new Vector4UInt { x = tileID, y = relLightOffset, z = listDepthRange, w = listBitMask };
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

        static Vector4 MakePlane(Vector3 pb, Vector3 pc)
        {
            Vector3 v0 = pb;
            Vector3 v1 = pc;
            Vector3 n = Vector3.Cross(v0, v1);
            n = Vector3.Normalize(n);

            // The planes pass all by the origin.
            return new Vector4(n.x, n.y, n.z, 0.0f);
        }

        static Vector4 MakePlane(Vector3 pa, Vector3 pb, Vector3 pc)
        {
            Vector3 v0 = pb - pa;
            Vector3 v1 = pc - pa;
            Vector3 n = Vector3.Cross(v0, v1);
            n = Vector3.Normalize(n);

            return new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, pa));
        }

        static float DistanceToPlane(Vector4 plane, Vector3 p)
        {
            return plane.x * p.x + plane.y * p.y + plane.z * p.z + plane.w;
        }

        static float SignedSq(float f)
        {
            // slower!
            //return Mathf.Sign(f) * (f * f);
            return (f < 0.0f ? -1.0f : 1.0f) * (f * f);
        }

        static float Max(float a, float b, float c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        static Vector2 Abs(Vector2 v)
        {
            return new Vector2(v.x < 0.0f ? -v.x : v.x, v.y < 0.0f ? -v.y : v.y);
        }

        // Return parametric intersection between a sphere and a line.
        // The intersections points P0 and P1 are:
        // P0 = raySource + rayDirection * t0.
        // P1 = raySource + rayDirection * t1.
        static bool IntersectionLineSphere(Vector3 centre, float radius, Vector3 raySource, Vector3 rayDirection, out float t0, out float t1)
        {
            float A = Vector3.Dot(rayDirection, rayDirection); // always >= 0
            float B = Vector3.Dot(raySource - centre, rayDirection);
            float C = Vector3.Dot(raySource, raySource)
                    + Vector3.Dot(centre, centre)
                    - (radius * radius)
                    - 2 * Vector3.Dot(raySource, centre);
            float discriminant = (B*B) - A * C;
            if (discriminant > 0)
            {
                float sqrt_discriminant = Mathf.Sqrt(discriminant);
                float A_inv = 1.0f / A;
                t0 = (-B - sqrt_discriminant) * A_inv;
                t1 = (-B + sqrt_discriminant) * A_inv;
                return true;
            }
            else
            {
                t0 = 0.0f; // invalid
                t1 = 0.0f; // invalid
                return false;
            }
        }

        // Clip a sphere against a 2D tile. Near and far planes are ignored (already tested).
        static bool Clip(ref PreTile tile, Vector3 vsPos, float radius)
        {
            // Simplified clipping code, only deals with 4 clipping planes.
            // zNear and zFar clipping planes are ignored as presumably the light is already visible to the camera frustum.
            
            float radiusSq = radius * radius;
            int insideCount = 0;
            ClipResult res;

            res = ClipPartial(tile.planeLeft, tile.planeBottom, tile.planeTop, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeRight, tile.planeBottom, tile.planeTop, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeTop, tile.planeLeft, tile.planeRight, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            res = ClipPartial(tile.planeBottom, tile.planeLeft, tile.planeRight, vsPos, radius, radiusSq, ref insideCount);
            if (res != ClipResult.Unknown)
                return res == ClipResult.In;

            return insideCount == 4;
        }

        // Internal function to clip against 1 plane of a cube, with additional 2 side planes for false-positive detection (normally 4 planes, but near and far planes are ignored).
        static ClipResult ClipPartial(Vector4 plane, Vector4 sidePlaneA, Vector4 sidePlaneB, Vector3 vsPos, float radius, float radiusSq, ref int insideCount)
        {
            float d = DistanceToPlane(plane, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return ClipResult.Out;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)plane * d;
                float rSq = radiusSq - d * d;
                if (SignedSq(DistanceToPlane(sidePlaneA, p)) >= -rSq
                 && SignedSq(DistanceToPlane(sidePlaneB, p)) >= -rSq)
                    return ClipResult.In;
            }
            else // consider as good as completely inside
                ++insideCount;

            return ClipResult.Unknown;
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
}
