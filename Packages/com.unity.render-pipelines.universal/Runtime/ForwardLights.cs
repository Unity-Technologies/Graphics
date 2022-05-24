using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Computes and submits lighting data to the GPU.
    /// </summary>
    public class ForwardLights
    {
        static class LightConstantBuffer
        {
            public static int _MainLightPosition;   // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightColor;      // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static int _MainLightOcclusionProbesChannel;    // Deferred?
            public static int _MainLightLayerMask;

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;
            public static int _AdditionalLightOcclusionProbeChannel;
            public static int _AdditionalLightsLayerMasks;
        }

        int m_AdditionalLightsBufferId;
        int m_AdditionalLightsIndicesId;

        const string k_SetupLightConstants = "Setup Light Constants";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_SetupLightConstants);
        private static readonly ProfilingSampler m_ProfilingSamplerFPSetup = new ProfilingSampler("Forward+ Setup");
        private static readonly ProfilingSampler m_ProfilingSamplerFPComplete = new ProfilingSampler("Forward+ Complete");
        private static readonly ProfilingSampler m_ProfilingSamplerFPUpload = new ProfilingSampler("Forward+ Upload");
        MixedLightingSetup m_MixedLightingSetup;

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;
        float[] m_AdditionalLightsLayerMasks;  // Unity has no support for binding uint arrays. We will use asuint() in the shader instead.

        bool m_UseStructuredBuffer;

        bool m_UseForwardPlus;
        int m_DirectionalLightCount;
        int m_ActualTileWidth;
        int2 m_TileResolution;

        JobHandle m_CullingHandle;
        NativeArray<uint> m_ZBins;
        GraphicsBuffer m_ZBinsBuffer;
        NativeArray<uint> m_TileMasks;
        GraphicsBuffer m_TileMasksBuffer;

        private LightCookieManager m_LightCookieManager;
        int m_WordsPerTile;
        float m_ZBinScale;
        float m_ZBinOffset;
        Dictionary<int, int> m_OrthographicWarningShown = new Dictionary<int, int>(8);
        Dictionary<int, int> m_XrWarningShown = new Dictionary<int, int>(8);
        List<int> m_KeysToRemove = new List<int>(8);

        internal struct InitParams
        {
            public LightCookieManager lightCookieManager;
            public bool forwardPlus;

            static internal InitParams Create()
            {
                InitParams p;
                {
                    var settings = LightCookieManager.Settings.Create();
                    var asset = UniversalRenderPipeline.asset;
                    if (asset)
                    {
                        settings.atlas.format = asset.additionalLightsCookieFormat;
                        settings.atlas.resolution = asset.additionalLightsCookieResolution;
                    }

                    p.lightCookieManager = new LightCookieManager(ref settings);
                    p.forwardPlus = false;
                }
                return p;
            }
        }

        /// <summary>
        /// Creates a new <c>ForwardLights</c> instance.
        /// </summary>
        public ForwardLights() : this(InitParams.Create()) { }

        internal ForwardLights(InitParams initParams)
        {
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_UseForwardPlus = initParams.forwardPlus;

            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
            LightConstantBuffer._MainLightLayerMask = Shader.PropertyToID("_MainLightLayerMask");
            LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");

            if (m_UseStructuredBuffer)
            {
                m_AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
                m_AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");
            }
            else
            {
                LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
                LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
                LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
                LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
                LightConstantBuffer._AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");
                LightConstantBuffer._AdditionalLightsLayerMasks = Shader.PropertyToID("_AdditionalLightsLayerMasks");

                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                m_AdditionalLightPositions = new Vector4[maxLights];
                m_AdditionalLightColors = new Vector4[maxLights];
                m_AdditionalLightAttenuations = new Vector4[maxLights];
                m_AdditionalLightSpotDirections = new Vector4[maxLights];
                m_AdditionalLightOcclusionProbeChannels = new Vector4[maxLights];
                m_AdditionalLightsLayerMasks = new float[maxLights];
            }

            if (m_UseForwardPlus)
            {
                CreateForwardPlusBuffers();
            }

            m_LightCookieManager = initParams.lightCookieManager;
        }

        void CreateForwardPlusBuffers()
        {
            m_ZBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.Persistent);
            m_ZBinsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxZBinWords / 4, UnsafeUtility.SizeOf<float4>());
            m_ZBinsBuffer.name = "AdditionalLightsZBins";
            m_TileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.Persistent);
            m_TileMasksBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxTileWords / 4, UnsafeUtility.SizeOf<float4>());
            m_TileMasksBuffer.name = "AdditionalLightsTiles";
        }

        static int AlignByteCount(int count, int align) => align * ((count + align - 1) / align);

        internal void ProcessLights(ref RenderingData renderingData)
        {
            if (m_UseForwardPlus)
            {
                using var _ = new ProfilingScope(null, m_ProfilingSamplerFPSetup);

                if (!m_CullingHandle.IsCompleted)
                {
                    throw new InvalidOperationException("Forward+ jobs have not completed yet.");
                }

                if (m_TileMasks.Length != UniversalRenderPipeline.maxTileWords)
                {
                    m_ZBins.Dispose();
                    m_ZBinsBuffer.Dispose();
                    m_TileMasks.Dispose();
                    m_TileMasksBuffer.Dispose();
                    CreateForwardPlusBuffers();
                }
                else
                {
                    unsafe
                    {
                        UnsafeUtility.MemClear(m_ZBins.GetUnsafePtr(), m_ZBins.Length * sizeof(uint));
                        UnsafeUtility.MemClear(m_TileMasks.GetUnsafePtr(), m_TileMasks.Length * sizeof(uint));
                    }
                }

                var camera = renderingData.cameraData.camera;

                var frameIndex = Time.renderedFrameCount;

                if (m_OrthographicWarningShown.Count > 0)
                {
                    foreach (var (cameraId, lastFrameIndex) in m_OrthographicWarningShown)
                    {
                        if (math.abs(frameIndex - lastFrameIndex) > 2)
                        {
                            m_KeysToRemove.Add(cameraId);
                        }
                    }

                    foreach (var cameraId in m_KeysToRemove)
                    {
                        m_OrthographicWarningShown.Remove(cameraId);
                    }

                    m_KeysToRemove.Clear();
                }

                if (m_XrWarningShown.Count > 0)
                {
                    foreach (var (cameraId, lastFrameIndex) in m_XrWarningShown)
                    {
                        if (math.abs(frameIndex - lastFrameIndex) > 2)
                        {
                            m_KeysToRemove.Add(cameraId);
                        }
                    }

                    foreach (var cameraId in m_KeysToRemove)
                    {
                        m_XrWarningShown.Remove(cameraId);
                    }

                    m_KeysToRemove.Clear();
                }

                if (camera.orthographic)
                {
                    var cameraId = camera.GetInstanceID();
                    if (!m_OrthographicWarningShown.ContainsKey(cameraId))
                    {
                        Debug.LogWarning("Orthographic projection is not supported when using Forward+.");
                    }

                    m_OrthographicWarningShown[cameraId] = frameIndex;
                }

                if (renderingData.cameraData.xrRendering)
                {
                    var cameraId = camera.GetInstanceID();
                    if (!m_XrWarningShown.ContainsKey(cameraId))
                    {
                        Debug.LogWarning("XR rendering is not supported when using Forward+.");
                    }

                    m_XrWarningShown[cameraId] = frameIndex;
                }

                var screenResolution = math.int2(renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);

                var lightCount = renderingData.lightData.visibleLights.Length;
                var lightOffset = 0;
                while (lightOffset < lightCount && renderingData.lightData.visibleLights[lightOffset].lightType == LightType.Directional)
                {
                    lightOffset++;
                }
                lightCount -= lightOffset;

                m_DirectionalLightCount = lightOffset;
                if (renderingData.lightData.mainLightIndex != -1 && m_DirectionalLightCount != 0) m_DirectionalLightCount -= 1;

                var visibleLights = renderingData.lightData.visibleLights.GetSubArray(lightOffset, lightCount);
                var lightsPerTile = visibleLights.Length;
                m_WordsPerTile = (lightsPerTile + 31) / 32;

                m_ActualTileWidth = 8 >> 1;
                do
                {
                    m_ActualTileWidth <<= 1;
                    m_TileResolution = (screenResolution + m_ActualTileWidth - 1) / m_ActualTileWidth;
                }
                while ((m_TileResolution.x * m_TileResolution.y * m_WordsPerTile) > UniversalRenderPipeline.maxTileWords);

                var fovHalfHeight = math.tan(math.radians(camera.fieldOfView * 0.5f));
                // binIndex = log2(z) * zBinScale + zBinOffset
                m_ZBinScale = 1f / math.log2(1f + 2f * fovHalfHeight / m_TileResolution.y);
                m_ZBinOffset = -math.log2(camera.nearClipPlane) * m_ZBinScale;
                var binCount = (int)(math.log2(camera.farClipPlane) * m_ZBinScale + m_ZBinOffset);
                // Clamp the bin count to stay within memory budget.
                // words = binCount * (1 + m_WordsPerTile) => binCount = words / (1 + m_WordsPerTile)
                binCount = math.min(UniversalRenderPipeline.maxZBinWords, binCount * (1 + m_WordsPerTile)) / (1 + m_WordsPerTile);

                var minMaxZs = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);
                // We allocate double array length because the sorting algorithm needs swap space to work in.
                var meanZs = new NativeArray<float>(lightCount * 2, Allocator.TempJob);

                Matrix4x4 worldToViewMatrix = renderingData.cameraData.GetViewMatrix();
                var minMaxZJob = new MinMaxZJob
                {
                    worldToViewMatrix = worldToViewMatrix,
                    lights = visibleLights,
                    minMaxZs = minMaxZs,
                    meanZs = meanZs
                };
                // Innerloop batch count of 32 is not special, just a handwavy amount to not have too much scheduling overhead nor too little parallelism.
                var minMaxZHandle = minMaxZJob.ScheduleParallel(lightCount, 32, new JobHandle());

                // We allocate double array length because the sorting algorithm needs swap space to work in.
                var indices = new NativeArray<int>(lightCount * 2, Allocator.TempJob);
                var radixSortJob = new RadixSortJob
                {
                    // Floats can be sorted bitwise with no special handling if positive floats only
                    keys = meanZs.Reinterpret<uint>(),
                    indices = indices
                };
                var zSortHandle = radixSortJob.Schedule(minMaxZHandle);

                var reorderedLights = new NativeArray<VisibleLight>(lightCount, Allocator.TempJob);
                var reorderedMinMaxZs = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);

                var reorderLightsJob = new ReorderJob<VisibleLight> { indices = indices, input = visibleLights, output = reorderedLights };
                var reorderLightsHandle = reorderLightsJob.ScheduleParallel(lightCount, 32, zSortHandle);

                var reorderMinMaxZsJob = new ReorderJob<LightMinMaxZ> { indices = indices, input = minMaxZs, output = reorderedMinMaxZs };
                var reorderMinMaxZsHandle = reorderMinMaxZsJob.ScheduleParallel(lightCount, 32, zSortHandle);

                var reorderHandle = JobHandle.CombineDependencies(
                    reorderLightsHandle,
                    reorderMinMaxZsHandle
                );

                JobHandle.ScheduleBatchedJobs();

                var zBinningJob = new ZBinningJob
                {
                    bins = m_ZBins,
                    minMaxZs = reorderedMinMaxZs,
                    zBinScale = m_ZBinScale,
                    zBinOffset = m_ZBinOffset,
                    binCount = binCount,
                    wordsPerTile = m_WordsPerTile
                };
                var zBinningHandle = zBinningJob.ScheduleParallel((binCount + ZBinningJob.batchCount - 1) / ZBinningJob.batchCount, 1, reorderHandle);

                // Each light needs 1 range for Y, and a range per row. Align to 128-bytes to avoid false sharing.
                var itemsPerLight = AlignByteCount((1 + m_TileResolution.y) * UnsafeUtility.SizeOf<InclusiveRange>(), 128) / UnsafeUtility.SizeOf<InclusiveRange>();
                var tileRanges = new NativeArray<InclusiveRange>(itemsPerLight * lightCount, Allocator.TempJob);
                var tilingJob = new TilingJob
                {
                    lights = reorderedLights,
                    tileRanges = tileRanges,
                    itemsPerLight = itemsPerLight,
                    worldToViewMatrix = worldToViewMatrix,
                    tileScale = (float2)screenResolution / m_ActualTileWidth,
                    tileScaleInv = m_ActualTileWidth / (float2)screenResolution,
                    viewPlaneHalfSize = fovHalfHeight * math.float2(camera.aspect, 1),
                    viewPlaneHalfSizeInv = math.rcp(fovHalfHeight * math.float2(camera.aspect, 1)),
                    tileCount = m_TileResolution,
                    near = camera.nearClipPlane,
                };

                var tileRangeHandle = tilingJob.ScheduleParallel(lightCount, 1, reorderHandle);

                var expansionJob = new TileRangeExpansionJob
                {
                    tileRanges = tileRanges,
                    lightMasks = m_TileMasks,
                    itemsPerLight = itemsPerLight,
                    lightCount = lightCount,
                    wordsPerTile = m_WordsPerTile,
                    tileResolution = m_TileResolution,
                };

                var tilingHandle = expansionJob.ScheduleParallel(m_TileResolution.y, 1, tileRangeHandle);

                m_CullingHandle = JobHandle.CombineDependencies(tilingHandle, zBinningHandle);

                reorderHandle.Complete();
                minMaxZs.Dispose();
                meanZs.Dispose();
                if (lightCount > 0) NativeArray<VisibleLight>.Copy(reorderedLights, 0, renderingData.lightData.visibleLights, lightOffset, lightCount);

                var tempBias = new NativeArray<Vector4>(lightCount, Allocator.Temp);
                var tempResolution = new NativeArray<int>(lightCount, Allocator.Temp);
                var tempIndices = new NativeArray<int>(lightCount, Allocator.Temp);

                for (var i = 0; i < lightCount; i++)
                {
                    tempBias[indices[i]] = renderingData.shadowData.bias[lightOffset + i];
                    tempResolution[indices[i]] = renderingData.shadowData.resolution[lightOffset + i];
                    tempIndices[indices[i]] = lightOffset + i;
                }

                indices.Dispose();

                for (var i = 0; i < lightCount; i++)
                {
                    renderingData.shadowData.bias[i + lightOffset] = tempBias[i];
                    renderingData.shadowData.resolution[i + lightOffset] = tempResolution[i];
                    renderingData.lightData.originalIndices[i + lightOffset] = tempIndices[i];
                }

                tempBias.Dispose();
                tempResolution.Dispose();
                tempIndices.Dispose();
                m_CullingHandle = JobHandle.CombineDependencies(
                    reorderedMinMaxZs.Dispose(zBinningHandle),
                    tileRanges.Dispose(tilingHandle),
                    reorderedLights.Dispose(m_CullingHandle));
                JobHandle.ScheduleBatchedJobs();
            }
        }

        /// <summary>
        /// Sets up the keywords and data for forward lighting.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(null, m_ProfilingSampler))
            {
                if (m_UseForwardPlus)
                {
                    using (new ProfilingScope(null, m_ProfilingSamplerFPComplete))
                    {
                        m_CullingHandle.Complete();
                    }

                    using (new ProfilingScope(null, m_ProfilingSamplerFPUpload))
                    {
                        m_ZBinsBuffer.SetData(m_ZBins.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        m_TileMasksBuffer.SetData(m_TileMasks.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        cmd.SetGlobalConstantBuffer(m_ZBinsBuffer, "AdditionalLightsZBins", 0, UniversalRenderPipeline.maxZBinWords * 4);
                        cmd.SetGlobalConstantBuffer(m_TileMasksBuffer, "AdditionalLightsTiles", 0, UniversalRenderPipeline.maxTileWords * 4);
                    }

                    cmd.SetGlobalInteger("_AdditionalLightsDirectionalCount", m_DirectionalLightCount);
                    cmd.SetGlobalVector("_AdditionalLightsParams0", new Vector4(m_ZBinScale, m_ZBinOffset));
                    cmd.SetGlobalVector("_AdditionalLightsTileScale", renderingData.cameraData.pixelRect.size / (float)m_ActualTileWidth);
                    cmd.SetGlobalInteger("_AdditionalLightsTileCountX", m_TileResolution.x);
                    cmd.SetGlobalInteger("_AdditionalLightsWordsPerTile", m_WordsPerTile);
                }

                SetupShaderLightConstants(cmd, ref renderingData);

                bool lightCountCheck = (renderingData.cameraData.renderer.stripAdditionalLightOffVariants && renderingData.lightData.supportsAdditionalLights) || additionalLightsCount > 0;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    lightCountCheck && additionalLightsPerVertex && !m_UseForwardPlus);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    lightCountCheck && !additionalLightsPerVertex && !m_UseForwardPlus);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ForwardPlus,
                    m_UseForwardPlus);

                bool isShadowMask = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
                bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                bool isSubtractive = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBlending, renderingData.lightData.reflectionProbeBlending);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBoxProjection, renderingData.lightData.reflectionProbeBoxProjection);

                bool lightLayers = renderingData.lightData.supportsLightLayers;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightLayers, lightLayers);

                m_LightCookieManager.Setup(context, cmd, ref renderingData.lightData);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        internal void Cleanup()
        {
            if (m_UseForwardPlus)
            {
                m_CullingHandle.Complete();
                m_ZBins.Dispose();
                m_TileMasks.Dispose();
                m_ZBinsBuffer.Dispose();
                m_ZBinsBuffer = null;
                m_TileMasksBuffer.Dispose();
                m_TileMasksBuffer = null;
            }
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel, out uint lightLayerMask)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);
            lightLayerMask = 0;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            Light light = lightData.light;

            if (light == null)
                return;

            if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightData.light.shadows != LightShadows.None &&
                m_MixedLightingSetup == MixedLightingSetup.None)
            {
                switch (light.bakingOutput.mixedLightingMode)
                {
                    case MixedLightingMode.Subtractive:
                        m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                        break;
                    case MixedLightingMode.Shadowmask:
                        m_MixedLightingSetup = MixedLightingSetup.ShadowMask;
                        break;
                }
            }

            var additionalLightData = light.GetUniversalAdditionalLightData();
            lightLayerMask = RenderingLayerUtils.ToRenderingLayers(additionalLightData.lightLayerMask);
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref renderingData.lightData);
            SetupAdditionalLightConstants(cmd, ref renderingData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            uint lightLayerMask;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel, out lightLayerMask);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightOcclusionProbesChannel, lightOcclusionChannel);
            cmd.SetGlobalInt(LightConstantBuffer._MainLightLayerMask, (int)lightLayerMask);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref LightData lightData = ref renderingData.lightData;
            var cullResults = renderingData.cullResults;
            var lights = lightData.visibleLights;
            int maxAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, ref lightData);
            if (additionalLightsCount > 0)
            {
                if (m_UseStructuredBuffer)
                {
                    NativeArray<ShaderInput.LightData> additionalLightsData = new NativeArray<ShaderInput.LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            ShaderInput.LightData data;
                            InitializeLightConstants(lights, i,
                                out data.position, out data.color, out data.attenuation,
                                out data.spotDirection, out data.occlusionProbeChannels,
                                out data.layerMask);
                            additionalLightsData[lightIter] = data;
                            lightIter++;
                        }
                    }

                    var lightDataBuffer = ShaderData.instance.GetLightDataBuffer(additionalLightsCount);
                    lightDataBuffer.SetData(additionalLightsData);

                    int lightIndices = cullResults.lightAndReflectionProbeIndexCount;
                    var lightIndicesBuffer = ShaderData.instance.GetLightIndicesBuffer(lightIndices);

                    cmd.SetGlobalBuffer(m_AdditionalLightsBufferId, lightDataBuffer);
                    cmd.SetGlobalBuffer(m_AdditionalLightsIndicesId, lightIndicesBuffer);

                    additionalLightsData.Dispose();
                }
                else
                {
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        VisibleLight light = lights[i];
                        if (lightData.mainLightIndex != i)
                        {
                            uint lightLayerMask;
                            InitializeLightConstants(lights, i, out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter],
                                out lightLayerMask);
                            m_AdditionalLightsLayerMasks[lightIter] = Unity.Mathematics.math.asfloat(lightLayerMask);
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);
                    cmd.SetGlobalFloatArray(LightConstantBuffer._AdditionalLightsLayerMasks, m_AdditionalLightsLayerMasks);
                }

                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount,
                    0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, Vector4.zero);
            }
        }

        int SetupPerObjectLightIndices(CullingResults cullResults, ref LightData lightData)
        {
            if (lightData.additionalLightsCount == 0 || m_UseForwardPlus)
                return lightData.additionalLightsCount;

            var visibleLights = lightData.visibleLights;
            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= UniversalRenderPipeline.maxVisibleAdditionalLights)
                    break;

                VisibleLight light = visibleLights[i];
                if (i == lightData.mainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullResults.SetLightIndexMap(perObjectLightIndexMap);

            if (m_UseStructuredBuffer && additionalLightsCount > 0)
            {
                int lightAndReflectionProbeIndices = cullResults.lightAndReflectionProbeIndexCount;
                Assertions.Assert.IsTrue(lightAndReflectionProbeIndices > 0, "Pipelines configures additional lights but per-object light and probe indices count is zero.");
                cullResults.FillLightAndReflectionProbeIndices(ShaderData.instance.GetLightIndicesBuffer(lightAndReflectionProbeIndices));
            }

            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }
    }
}
