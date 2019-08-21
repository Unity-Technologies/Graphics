using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Profiling;
using Unity.Collections;

// TODO use Unity.Mathematics
// TODO allocate using Temp allocator.
// TODO have better management for the compute buffer.
// TODO Check if there is a bitarray structure (with dynamic size) available in Unity
// TODO use global ids for uniform constant names.

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
        // Index into visibleLight native array.
        public ushort visLightIndex;
    }

    internal struct DrawCall
    {
        public ComputeBuffer tileIDBuffer;
        public ComputeBuffer tileRelLightBuffer;
        public ComputeBuffer pointLightBuffer;
        public ComputeBuffer relLightIndexBuffer;
        public int tileIDBufferSize;
        public int tileRelLightBufferSize;
        public int pointLightBufferSize;
        public int relLightIndexBufferSize;
        public int instanceOffset;
        public int instanceCount;
    }

    // Render all tiled-based deferred lights.
    internal class DeferredPass : ScriptableRenderPass
    {
        // TODO customize per platform.
        const int kMaxUniformBufferSize = 64 * 1024;

        const string k_TileBasedDeferredShading = "Tile-Based Deferred Shading";

        #if !UNITY_EDITOR && UNITY_SWITCH
        const bool k_HasNativeQuadSupport = true;
        #else
        const bool k_HasNativeQuadSupport = false;
        #endif

        int m_RenderWidth = 0;
        int m_RenderHeight = 0;
        int m_TilePixelWidth = 16;
        int m_TilePixelHeight = 16;
        int m_TileXCount = 0;
        int m_TileYCount = 0;
        int m_TileSize = 32;

        // Store all visible light indices for all tiles.
        NativeArray<ushort> m_Tiles;
        // Precompute tile data.
        NativeArray<PreTile> m_PreTiles;

        // Max tile instancing limit per draw call. It depends on the max capacity of uniform buffers.
        int m_MaxTilesPerBatch;
        // Max number of point lights that can be referenced per draw call.
        int m_MaxPointLightPerBatch;
        // Max number of relative light indices per draw call.
        int m_MaxRelLightIndicesPerBatch;

        // Hold all shaders for tiled-based deferred shading.
        Material m_TilingMaterial;

        public DeferredPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, Material tilingMaterial)
        {
            this.renderPassEvent = evt;

            // Compute some platform limits.
            m_MaxTilesPerBatch = kMaxUniformBufferSize / sizeof(uint); // TileID is uint, but packed in uint4
            m_MaxPointLightPerBatch = kMaxUniformBufferSize / System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
            m_MaxRelLightIndicesPerBatch = kMaxUniformBufferSize / sizeof(uint); // Should be ushort!

            m_TilingMaterial = tilingMaterial;
        }

        public void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_RenderWidth != renderingData.cameraData.cameraTargetDescriptor.width
             || m_RenderHeight != renderingData.cameraData.cameraTargetDescriptor.height)
            {
                m_RenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
                m_RenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;

                m_TileXCount = (m_RenderWidth + m_TilePixelWidth - 1) / m_TilePixelWidth;
                m_TileYCount = (m_RenderHeight + m_TilePixelHeight - 1) / m_TilePixelHeight;

                PrecomputeTiles(out m_PreTiles, renderingData.cameraData.camera.projectionMatrix, renderingData.cameraData.camera.orthographic, m_RenderWidth, m_RenderHeight);
            }

            m_Tiles = new NativeArray<ushort>(m_TileXCount * m_TileYCount * m_TileSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            NativeArray<PrePointLight> prePointLights;
            PrecomputeLights(out prePointLights, renderingData.lightData.visibleLights, renderingData.cameraData.camera.worldToCameraMatrix);

            CullLights(prePointLights);

            prePointLights.Dispose();
        }

        // ScriptableRenderPass
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            /*
            RenderTextureDescriptor descriptor = cameraTextureDescripor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(destination.id, descriptor, m_DownsamplingMethod == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            */
        }

        // ScriptableRenderPass
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_TilingMaterial == null)
                return;

            // Allow max 256 draw calls for rendering all the batches of tiles
            DrawCall[] drawCalls = new DrawCall[256];
            int drawCallCount = 0;

            {
                Profiler.BeginSample("k_TileBasedDeferredShading");

                int sizeof_PointLightData = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLightData));
                int sizeof_vec4_PointLightData = sizeof_PointLightData / 16;

                int tileXStride = m_TileSize;
                int tileYStride = m_TileSize * m_TileXCount;
                int maxLightPerTile = m_TileSize - 1;

                int instanceOffset = 0;
                int tileCount = 0;
                int lightCount = 0;
                int relLightIndices = 0;

                ComputeBuffer _tileIDBuffer = DeferredShaderData.instance.ReserveTileIDBuffer(m_MaxTilesPerBatch);
                ComputeBuffer _tileRelLightBuffer = DeferredShaderData.instance.ReserveTileRelLightBuffer(m_MaxTilesPerBatch);
                ComputeBuffer _pointLightBuffer = DeferredShaderData.instance.ReservePointLightBuffer(m_MaxPointLightPerBatch);
                ComputeBuffer _relLightIndexBuffer = DeferredShaderData.instance.ReserveRelLightIndexBuffer(m_MaxRelLightIndicesPerBatch);

                // Acceleration structure to quickly find if a light has already been added to the uniform block data for the current draw call.
                NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<ushort> visLightToRelLights = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                NativeArray<uint> tileIDBuffer = new NativeArray<uint>(m_MaxTilesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                NativeArray<uint> tileRelLightBuffer = new NativeArray<uint>(m_MaxTilesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#if UNITY_SUPPORT_STRUCT_IN_CBUFFER
                NativeArray<PointLightData> pointLightBuffer = new NativeArray<PointLightData>(m_MaxPointLightPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#else
                NativeArray<Vector4> pointLightBuffer = new NativeArray<Vector4>(m_MaxPointLightPerBatch * sizeof_vec4_PointLightData, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
#endif
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
                        int trimmedLightCount = TrimLights(trimmedLights, m_Tiles, tileOffset + 1, tileLightCount, usedLights);
                        Assertions.Assert.IsTrue(trimmedLightCount <= maxLightPerTile, "too many lights overlaps a tile: max allowed is " + maxLightPerTile);

                        // Find if one of the GPU buffers is reaching max capacity.
                        // In that case, the draw call must be flushed and new GPU buffer(s) be allocated.
                        bool tileIDBufferIsFull = (tileCount == m_MaxTilesPerBatch);
                        bool lightBufferIsFull = (lightCount + trimmedLightCount > m_MaxPointLightPerBatch);
                        bool relLightIndexBufferIsFull = (relLightIndices + 1 + tileLightCount > m_MaxRelLightIndicesPerBatch); // +1 for list size

                        if (tileIDBufferIsFull || lightBufferIsFull || relLightIndexBufferIsFull)
                        {
                            int _instanceCount = tileCount - instanceOffset;
                            int _tileIDBufferSize = Align(tileCount, 4) * 4;
                            int _tileRelLightBufferSize = Align(tileCount, 4) * 4;
#if UNITY_SUPPORT_STRUCT_IN_CBUFFER
                            int _pointLightBufferSize = lightCount * sizeof_PointLightData;
#else
                            int _pointLightBufferSize = m_MaxPointLightPerBatch * sizeof_PointLightData;
#endif
                            int _relLightIndexBufferSize = Align(relLightIndices, 4) * 4;

                            drawCalls[drawCallCount++] = new DrawCall
                            {
                                tileIDBuffer = _tileIDBuffer,
                                tileRelLightBuffer = _tileRelLightBuffer,
                                pointLightBuffer = _pointLightBuffer,
                                relLightIndexBuffer = _relLightIndexBuffer,
                                tileIDBufferSize = _tileIDBufferSize,
                                tileRelLightBufferSize = _tileRelLightBufferSize,
                                pointLightBufferSize = _pointLightBufferSize,
                                relLightIndexBufferSize = _relLightIndexBufferSize,
                                instanceOffset = instanceOffset,
                                instanceCount = _instanceCount
                            };

                            if (tileIDBufferIsFull)
                            {
                                _tileIDBuffer.SetData(tileIDBuffer, 0, 0, m_MaxTilesPerBatch); // Must pass complete array (restriction for binding Unity Constant Buffers)
                                _tileRelLightBuffer.SetData(tileRelLightBuffer, 0, 0, m_MaxTilesPerBatch);
                                _tileIDBuffer = DeferredShaderData.instance.ReserveTileIDBuffer(m_MaxTilesPerBatch);
                                _tileRelLightBuffer = DeferredShaderData.instance.ReserveTileRelLightBuffer(m_MaxTilesPerBatch);
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
                                    trimmedLights[l] = m_Tiles[tileOffset + 1 + l];
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

                        // Add TileID.
                        tileIDBuffer[tileCount] = PackTileID((uint)i, (uint)j);
                        tileRelLightBuffer[tileCount] = (ushort)relLightIndices;
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

                        // Add light list for the tile.
                        relLightIndexBuffer[relLightIndices++] = (ushort)tileLightCount;
                        for (int l = 0; l < tileLightCount; ++l)
                        {
                            int visLightIndex = m_Tiles[tileOffset + 1 + l];
                            ushort relLightIndex = visLightToRelLights[visLightIndex];
                            relLightIndexBuffer[relLightIndices++] = relLightIndex;
                        }
                    }
                }

                int instanceCount = tileCount - instanceOffset;
                if (instanceCount > 0)
                {
                    _tileIDBuffer.SetData(tileIDBuffer, 0, 0, m_MaxTilesPerBatch); // Must pass complete array (restriction for binding Unity Constant Buffers)
                    _tileRelLightBuffer.SetData(tileRelLightBuffer, 0, 0, m_MaxTilesPerBatch);
                    _pointLightBuffer.SetData(pointLightBuffer, 0, 0, m_MaxPointLightPerBatch * sizeof_vec4_PointLightData);
                    _relLightIndexBuffer.SetData(relLightIndexBuffer, 0, 0, m_MaxRelLightIndicesPerBatch);

                    drawCalls[drawCallCount++] = new DrawCall
                    {
                        tileIDBuffer = _tileIDBuffer,
                        tileRelLightBuffer = _tileRelLightBuffer,
                        pointLightBuffer = _pointLightBuffer,
                        relLightIndexBuffer = _relLightIndexBuffer,
                        tileIDBufferSize = Align(tileCount, 4) * 4,
                        tileRelLightBufferSize = Align(tileCount, 4) * 4,
#if UNITY_SUPPORT_STRUCT_IN_CBUFFER
                        pointLightBufferSize = lightCount * sizeof_PointLightData,
#else
                        pointLightBufferSize = m_MaxPointLightPerBatch * sizeof_PointLightData,
#endif
                        relLightIndexBufferSize = Align(relLightIndices, 4) * 4,
                        instanceOffset = instanceOffset,
                        instanceCount = instanceCount
                    };
                }

                trimmedLights.Dispose();
                visLightToRelLights.Dispose();
                usedLights.Dispose();
                tileIDBuffer.Dispose();
                tileRelLightBuffer.Dispose();
                pointLightBuffer.Dispose();
                relLightIndexBuffer.Dispose();

                DeferredShaderData.instance.ResetBuffers();

                Profiler.EndSample();
            }

            CommandBuffer cmd = CommandBufferPool.Get(k_TileBasedDeferredShading);
            using (new ProfilingSample(cmd, k_TileBasedDeferredShading))
            {
                MeshTopology topology = k_HasNativeQuadSupport ? MeshTopology.Quads : MeshTopology.Triangles;
                int vertexCount = k_HasNativeQuadSupport ? 4 : 6;

                // It doesn't seem UniversalRP use this.
                Vector4 screenSize = new Vector4(m_RenderWidth, m_RenderHeight, 1.0f / m_RenderWidth, 1.0f / m_RenderHeight);
                cmd.SetGlobalVector("_ScreenSize", screenSize);
                cmd.SetGlobalInt("g_TilePixelWidth", m_TilePixelWidth);
                cmd.SetGlobalInt("g_TilePixelHeight", m_TilePixelHeight);

                for (int i = 0; i < drawCallCount; ++i)
                { 
                    DrawCall dc = drawCalls[i];
                    cmd.SetGlobalConstantBuffer(dc.tileIDBuffer, Shader.PropertyToID("UTileIDBuffer"), 0, dc.tileIDBufferSize);
                    cmd.SetGlobalConstantBuffer(dc.tileRelLightBuffer, Shader.PropertyToID("UTileRelLightBuffer"), 0, dc.tileRelLightBufferSize);
                    cmd.SetGlobalConstantBuffer(dc.pointLightBuffer, Shader.PropertyToID("UPointLightBuffer"), 0, dc.pointLightBufferSize);
                    cmd.SetGlobalConstantBuffer(dc.relLightIndexBuffer, Shader.PropertyToID("URelLightIndexBuffer"), 0, dc.relLightIndexBufferSize);
                    cmd.SetGlobalInt("g_InstanceOffset", dc.instanceOffset);
                    cmd.DrawProcedural(Matrix4x4.identity, m_TilingMaterial, 0, topology, vertexCount, dc.instanceCount);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        // ScriptableRenderPass
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (m_Tiles.IsCreated)
                m_Tiles.Dispose();
        }

        void PrecomputeTiles(out NativeArray<PreTile> preTiles, Matrix4x4 proj, bool isOrthographic, int renderWidth, int renderHeight)
        {
            preTiles = DeferredShaderData.instance.GetPreTiles(m_TileXCount * m_TileYCount);

            // Adjust render width and height to account for tile size expanding over the screen (tiles have a fixed pixel size).
            int adjustedRenderWidth = Align(renderWidth, m_TilePixelWidth);
            int adjustedRenderHeight = Align(renderHeight, m_TilePixelHeight);

            // Now adjust the right and bottom clipping planes.
            FrustumPlanes fplanes = proj.decomposeProjection;
            fplanes.right = fplanes.left + (fplanes.right - fplanes.left) * (adjustedRenderWidth / (float)renderWidth);
            fplanes.bottom = fplanes.top + (fplanes.bottom - fplanes.top) * (adjustedRenderHeight / (float)renderHeight);

            // Tile size in world units.
            float tileWsWidth = (fplanes.right - fplanes.left) / m_TileXCount;
            float tileWsHeight = (fplanes.top - fplanes.bottom) / m_TileYCount;

            if (!isOrthographic) // perspective
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = fplanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = fplanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        // In view space with perspective projection, all planes pass by (0,0,0).
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -fplanes.zNear), new Vector3(tileLeft, tileTop, -fplanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -fplanes.zNear), new Vector3(tileRight, tileBottom, -fplanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -fplanes.zNear), new Vector3(tileLeft, tileBottom, -fplanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -fplanes.zNear), new Vector3(tileRight, tileTop, -fplanes.zNear));

                        preTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
            else
            {
                for (int j = 0; j < m_TileYCount; ++j)
                {
                    float tileTop = fplanes.top - tileWsHeight * j;
                    float tileBottom = tileTop - tileWsHeight;

                    for (int i = 0; i < m_TileXCount; ++i)
                    {
                        float tileLeft = fplanes.left + tileWsWidth * i;
                        float tileRight = tileLeft + tileWsWidth;

                        // Camera view space is always OpenGL RH coordinates system.
                        PreTile preTile;
                        preTile.planeLeft = MakePlane(new Vector3(tileLeft, tileBottom, -fplanes.zNear), new Vector3(tileLeft, tileBottom, -fplanes.zNear - 1.0f), new Vector3(tileLeft, tileTop, -fplanes.zNear));
                        preTile.planeRight = MakePlane(new Vector3(tileRight, tileTop, -fplanes.zNear), new Vector3(tileRight, tileTop, -fplanes.zNear - 1.0f), new Vector3(tileRight, tileBottom, -fplanes.zNear));
                        preTile.planeBottom = MakePlane(new Vector3(tileRight, tileBottom, -fplanes.zNear), new Vector3(tileRight, tileBottom, -fplanes.zNear - 1.0f), new Vector3(tileLeft, tileBottom, -fplanes.zNear));
                        preTile.planeTop = MakePlane(new Vector3(tileLeft, tileTop, -fplanes.zNear), new Vector3(tileLeft, tileTop, -fplanes.zNear - 1.0f), new Vector3(tileRight, tileTop, -fplanes.zNear));

                        preTiles[i + j * m_TileXCount] = preTile;
                    }
                }
            }
        }

        void PrecomputeLights(out NativeArray<PrePointLight> prePointLights, NativeArray<VisibleLight> visibleLights, Matrix4x4 view)
        {
            int lightTypeCount = 5;
            NativeArray<int> lightCount = new NativeArray<int>(lightTypeCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

            // Count the number of lights per type.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];
                ++lightCount[(int)vl.lightType];
            }

            prePointLights = new NativeArray<PrePointLight>(lightCount[(int)LightType.Point], Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < lightCount.Length; ++i)
                lightCount[i] = 0;

            // Precompute point light data.
            for (ushort visLightIndex = 0; visLightIndex < visibleLights.Length; ++visLightIndex)
            {
                VisibleLight vl = visibleLights[visLightIndex];

                if (vl.lightType == LightType.Point)
                {
                    PrePointLight ppl;
                    ppl.vsPos = view.MultiplyPoint(vl.light.transform.position); // By convention, OpenGL RH coordinate space
                    ppl.radius = vl.light.range;
                    ppl.visLightIndex = visLightIndex;

                    int i = lightCount[(int)LightType.Point]++;
                    prePointLights[i] = ppl;
                }
            }
            lightCount.Dispose();
        }

        void CullLights(NativeArray<PrePointLight> visPointLights)
        {
            Profiler.BeginSample("CullLights");

            int tileXStride = m_TileSize;
            int tileYStride = m_TileSize * m_TileXCount;
            int maxLightPerTile = m_TileSize - 1;

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

                        if (!Clip(ref preTile, ppl.vsPos, ppl.radius))
                            continue;

                        if (tileLightCount == maxLightPerTile)
                        {
                            // TODO log error: tile is full
                            break;
                        }

                        m_Tiles[tileOffset + 1 + tileLightCount] = ppl.visLightIndex;
                        ++tileLightCount;
                    }

                    m_Tiles[tileOffset] = tileLightCount;
                }
            }

            Profiler.EndSample();
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

#if UNITY_SUPPORT_STRUCT_IN_CBUFFER
        void StorePointLightData(NativeArray<PointLightData> pointLightBuffer, int storeIndex, NativeArray<VisibleLight> visibleLights, int index)
        {
            pointLightBuffer[storeIndex].WsPos = visibleLights[index].light.transform.position;
            pointLightBuffer[storeIndex].Radius = visibleLights[index].range;
            pointLightBuffer[storeIndex].Color = visibleLights[index].light.color;
        }
#else
        void StorePointLightData(NativeArray<Vector4> pointLightBuffer, int storeIndex, NativeArray<VisibleLight> visibleLights, int index)
        {
            Vector3 wsPos = visibleLights[index].light.transform.position;
            pointLightBuffer[                           0 + storeIndex] = new Vector4(wsPos.x, wsPos.y, wsPos.z, visibleLights[index].range);
            pointLightBuffer[this.m_MaxPointLightPerBatch + storeIndex] = visibleLights[index].light.color;
        }
#endif

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

        static float distanceToPlane(Vector4 plane, Vector3 p)
        {
            return plane.x * p.x + plane.y * p.y + plane.z * p.z + plane.w;
        }

        static float signedSq(float f)
        {
            return Mathf.Sign(f) * (f * f);
        }

        static bool Clip(ref PreTile tile, Vector3 vsPos, float radius)
        {
            // Simplified clipping code, only deals with 4 clipping planes.
            // zNear and zFar clipping planes are ignored as presumably the light is already visible to the camera frustum.

            float radius2 = radius * radius;
            float d;
            int insideCount = 0;

            d = distanceToPlane(tile.planeLeft, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return false;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)tile.planeLeft * d;
                float r2 = radius2 - d * d;
                if (signedSq(distanceToPlane(tile.planeBottom, p)) >= -r2
                 && signedSq(distanceToPlane(tile.planeTop, p)) >= -r2)
                    return true;
            }
            else // consider as good as completely inside
                ++insideCount;
            d = distanceToPlane(tile.planeRight, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return false;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)tile.planeRight * d;
                float r2 = radius2 - d * d;
                if (signedSq(distanceToPlane(tile.planeBottom, p)) >= -r2
                 && signedSq(distanceToPlane(tile.planeTop, p)) >= -r2)
                    return true;
            }
            else // consider as good as completely inside
                ++insideCount;
            d = distanceToPlane(tile.planeTop, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return false;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)tile.planeTop * d;
                float r2 = radius2 - d * d;
                if (signedSq(distanceToPlane(tile.planeLeft, p)) >= -r2
                 && signedSq(distanceToPlane(tile.planeRight, p)) >= -r2)
                    return true;
            }
            else // consider as good as completely inside
                ++insideCount;
            d = distanceToPlane(tile.planeBottom, vsPos);
            if (d + radius <= 0.0f) // completely outside
                return false;
            else if (d < 0.0f) // intersection: further check: only need to consider case where more than half the sphere is outside
            {
                Vector3 p = vsPos - (Vector3)tile.planeBottom * d;
                float r2 = radius2 - d * d;
                if (signedSq(distanceToPlane(tile.planeLeft, p)) >= -r2
                 && signedSq(distanceToPlane(tile.planeRight, p)) >= -r2)
                    return true;
            }
            else // completely inside
                ++insideCount;

            return insideCount == 4;
        }

        // Keep in sync with UnpackTileID().
        static uint PackTileID(uint i, uint j)
        {
            return i | (j << 16);
        }
    }
}
