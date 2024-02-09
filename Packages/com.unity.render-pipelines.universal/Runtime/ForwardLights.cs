using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        LightCookieManager m_LightCookieManager;
        ReflectionProbeManager m_ReflectionProbeManager;
        int m_WordsPerTile;
        float m_ZBinScale;
        float m_ZBinOffset;
        int m_LightCount;
        int m_BinCount;

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
                m_ReflectionProbeManager = ReflectionProbeManager.Create();
            }

            m_LightCookieManager = initParams.lightCookieManager;
        }

        void CreateForwardPlusBuffers()
        {
            m_ZBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.Persistent);
            m_ZBinsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxZBinWords / 4, UnsafeUtility.SizeOf<float4>());
            m_ZBinsBuffer.name = "URP Z-Bin Buffer";
            m_TileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.Persistent);
            m_TileMasksBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxTileWords / 4, UnsafeUtility.SizeOf<float4>());
            m_TileMasksBuffer.name = "URP Tile Buffer";
        }

        internal ReflectionProbeManager reflectionProbeManager => m_ReflectionProbeManager;

        static int AlignByteCount(int count, int align) => align * ((count + align - 1) / align);

        // Calculate view planes and viewToViewportScaleBias. This handles projection center in case the projection is off-centered
        void GetViewParams(Camera camera, float4x4 viewToClip, out float viewPlaneBot, out float viewPlaneTop, out float4 viewToViewportScaleBias)
        {
            // We want to calculate `fovHalfHeight = tan(fov / 2)`
            // `projection[1][1]` contains `1 / tan(fov / 2)`
            var viewPlaneHalfSizeInv = math.float2(viewToClip[0][0], viewToClip[1][1]);
            var viewPlaneHalfSize = math.rcp(viewPlaneHalfSizeInv);
            var centerClipSpace = camera.orthographic ? -math.float2(viewToClip[3][0], viewToClip[3][1]): math.float2(viewToClip[2][0], viewToClip[2][1]);

            viewPlaneBot = centerClipSpace.y * viewPlaneHalfSize.y - viewPlaneHalfSize.y;
            viewPlaneTop = centerClipSpace.y * viewPlaneHalfSize.y + viewPlaneHalfSize.y;
            viewToViewportScaleBias = math.float4(
                viewPlaneHalfSizeInv * 0.5f,
                -centerClipSpace * 0.5f + 0.5f
            );
        }

        internal void PreSetup(ref RenderingData renderingData)
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

                ref var cameraData = ref renderingData.cameraData;
                var camera = cameraData.camera;

                var screenResolution = math.int2(cameraData.pixelWidth, cameraData.pixelHeight);
#if ENABLE_VR && ENABLE_XR_MODULE
                var viewCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
                var viewCount = 1;
#endif

                m_LightCount = renderingData.lightData.visibleLights.Length;
                var lightOffset = 0;
                while (lightOffset < m_LightCount && renderingData.lightData.visibleLights[lightOffset].lightType == LightType.Directional)
                {
                    lightOffset++;
                }
                m_LightCount -= lightOffset;

                m_DirectionalLightCount = lightOffset;
                if (renderingData.lightData.mainLightIndex != -1 && m_DirectionalLightCount != 0) m_DirectionalLightCount -= 1;

                var visibleLights = renderingData.lightData.visibleLights.GetSubArray(lightOffset, m_LightCount);
                var reflectionProbes = renderingData.cullResults.visibleReflectionProbes;
                var reflectionProbeCount = math.min(reflectionProbes.Length, UniversalRenderPipeline.maxVisibleReflectionProbes);
                var itemsPerTile = visibleLights.Length + reflectionProbeCount;
                m_WordsPerTile = (itemsPerTile + 31) / 32;

                m_ActualTileWidth = 8 >> 1;
                do
                {
                    m_ActualTileWidth <<= 1;
                    m_TileResolution = (screenResolution + m_ActualTileWidth - 1) / m_ActualTileWidth;
                }
                while ((m_TileResolution.x * m_TileResolution.y * m_WordsPerTile * viewCount) > UniversalRenderPipeline.maxTileWords);

                if (!camera.orthographic)
                {
                    // Use to calculate binIndex = log2(z) * zBinScale + zBinOffset
                    m_ZBinScale = (UniversalRenderPipeline.maxZBinWords / viewCount) / ((math.log2(camera.farClipPlane) - math.log2(camera.nearClipPlane)) * (2 + m_WordsPerTile));
                    m_ZBinOffset = -math.log2(camera.nearClipPlane) * m_ZBinScale;
                    m_BinCount = (int)(math.log2(camera.farClipPlane) * m_ZBinScale + m_ZBinOffset);
                }
                else
                {
                    // Use to calculate binIndex = z * zBinScale + zBinOffset
                    m_ZBinScale = (UniversalRenderPipeline.maxZBinWords / viewCount) / ((camera.farClipPlane - camera.nearClipPlane) * (2 + m_WordsPerTile));
                    m_ZBinOffset = -camera.nearClipPlane * m_ZBinScale;
                    m_BinCount = (int)(camera.farClipPlane * m_ZBinScale + m_ZBinOffset);
                }

                var worldToViews = new Fixed2<float4x4>(cameraData.GetViewMatrix(0), cameraData.GetViewMatrix(math.min(1, viewCount - 1)));
                var viewToClips = new Fixed2<float4x4>(cameraData.GetProjectionMatrix(0), cameraData.GetProjectionMatrix(math.min(1, viewCount - 1)));
                
                var minMaxZs = new NativeArray<float2>(itemsPerTile * viewCount, Allocator.TempJob);

                var lightMinMaxZJob = new LightMinMaxZJob
                {
                    worldToViews = worldToViews,
                    lights = visibleLights,
                    minMaxZs = minMaxZs.GetSubArray(0, m_LightCount * viewCount)
                };
                // Innerloop batch count of 32 is not special, just a handwavy amount to not have too much scheduling overhead nor too little parallelism.
                var lightMinMaxZHandle = lightMinMaxZJob.ScheduleParallel(m_LightCount * viewCount, 32, new JobHandle());

                var reflectionProbeMinMaxZJob = new ReflectionProbeMinMaxZJob
                {
                    worldToViews = worldToViews,
                    reflectionProbes = reflectionProbes,
                    minMaxZs = minMaxZs.GetSubArray(m_LightCount * viewCount, reflectionProbeCount * viewCount)
                };
                var reflectionProbeMinMaxZHandle = reflectionProbeMinMaxZJob.ScheduleParallel(reflectionProbeCount * viewCount, 32, lightMinMaxZHandle);

                var zBinningBatchCount = (m_BinCount + ZBinningJob.batchSize - 1) / ZBinningJob.batchSize;
                var zBinningJob = new ZBinningJob
                {
                    bins = m_ZBins,
                    minMaxZs = minMaxZs,
                    zBinScale = m_ZBinScale,
                    zBinOffset = m_ZBinOffset,
                    binCount = m_BinCount,
                    wordsPerTile = m_WordsPerTile,
                    lightCount = m_LightCount,
                    reflectionProbeCount = reflectionProbeCount,
                    batchCount = zBinningBatchCount,
                    viewCount = viewCount,
                    isOrthographic = camera.orthographic
                };
                var zBinningHandle = zBinningJob.ScheduleParallel(zBinningBatchCount * viewCount, 1, reflectionProbeMinMaxZHandle);

                reflectionProbeMinMaxZHandle.Complete();

                GetViewParams(camera, viewToClips[0], out float viewPlaneBottom0, out float viewPlaneTop0, out float4 viewToViewportScaleBias0);
                GetViewParams(camera, viewToClips[1], out float viewPlaneBottom1, out float viewPlaneTop1, out float4 viewToViewportScaleBias1);

                // Each light needs 1 range for Y, and a range per row. Align to 128-bytes to avoid false sharing.
                var rangesPerItem = AlignByteCount((1 + m_TileResolution.y) * UnsafeUtility.SizeOf<InclusiveRange>(), 128) / UnsafeUtility.SizeOf<InclusiveRange>();
                var tileRanges = new NativeArray<InclusiveRange>(rangesPerItem * itemsPerTile * viewCount, Allocator.TempJob);
                var tilingJob = new TilingJob
                {
                    lights = visibleLights,
                    reflectionProbes = reflectionProbes,
                    tileRanges = tileRanges,
                    itemsPerTile = itemsPerTile,
                    rangesPerItem = rangesPerItem,
                    worldToViews = worldToViews,
                    tileScale = (float2)screenResolution / m_ActualTileWidth,
                    tileScaleInv = m_ActualTileWidth / (float2)screenResolution,
                    viewPlaneBottoms = new Fixed2<float>(viewPlaneBottom0, viewPlaneBottom1),
                    viewPlaneTops = new Fixed2<float>(viewPlaneTop0, viewPlaneTop1),
                    viewToViewportScaleBiases = new Fixed2<float4>(viewToViewportScaleBias0, viewToViewportScaleBias1),
                    tileCount = m_TileResolution,
                    near = camera.nearClipPlane,
                    isOrthographic = camera.orthographic
                };

                var tileRangeHandle = tilingJob.ScheduleParallel(itemsPerTile * viewCount, 1, reflectionProbeMinMaxZHandle);

                var expansionJob = new TileRangeExpansionJob
                {
                    tileRanges = tileRanges,
                    tileMasks = m_TileMasks,
                    rangesPerItem = rangesPerItem,
                    itemsPerTile = itemsPerTile,
                    wordsPerTile = m_WordsPerTile,
                    tileResolution = m_TileResolution,
                };

                var tilingHandle = expansionJob.ScheduleParallel(m_TileResolution.y * viewCount, 1, tileRangeHandle);
                m_CullingHandle = JobHandle.CombineDependencies(
                    minMaxZs.Dispose(zBinningHandle),
                    tileRanges.Dispose(tilingHandle));

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
                    m_ReflectionProbeManager.UpdateGpuData(cmd, ref renderingData);

                    using (new ProfilingScope(null, m_ProfilingSamplerFPComplete))
                    {
                        m_CullingHandle.Complete();
                    }

                    using (new ProfilingScope(null, m_ProfilingSamplerFPUpload))
                    {
                        m_ZBinsBuffer.SetData(m_ZBins.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        m_TileMasksBuffer.SetData(m_TileMasks.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        cmd.SetGlobalConstantBuffer(m_ZBinsBuffer, "URP_ZBinBuffer", 0, UniversalRenderPipeline.maxZBinWords * 4);
                        cmd.SetGlobalConstantBuffer(m_TileMasksBuffer, "urp_TileBuffer", 0, UniversalRenderPipeline.maxTileWords * 4);
                    }

                    cmd.SetGlobalVector("_FPParams0", math.float4(m_ZBinScale, m_ZBinOffset, m_LightCount, m_DirectionalLightCount));
                    cmd.SetGlobalVector("_FPParams1", math.float4(renderingData.cameraData.pixelRect.size / m_ActualTileWidth, m_TileResolution.x, m_WordsPerTile));
                    cmd.SetGlobalVector("_FPParams2", math.float4(m_BinCount, m_TileResolution.x * m_TileResolution.y, 0, 0));
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

                var asset = UniversalRenderPipeline.asset;

                // TODO: If we can robustly detect LIGHTMAP_ON, we can skip SH logic.
                var shMode = PlatformAutoDetect.ShAutoDetect(asset.shEvalMode);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.EVALUATE_SH_MIXED, shMode == ShEvalMode.Mixed);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.EVALUATE_SH_VERTEX, shMode == ShEvalMode.PerVertex);

                bool lightLayers = renderingData.lightData.supportsLightLayers;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightLayers, lightLayers && !CoreUtils.IsSceneLightingDisabled(renderingData.cameraData.camera));

                if (m_LightCookieManager != null)
                {
                    m_LightCookieManager.Setup(context, cmd, ref renderingData.lightData);
                }
                else
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightCookies, false);
                }
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
                m_ReflectionProbeManager.Dispose();
            }
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel, out uint lightLayerMask, out bool isSubtractive)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);
            lightLayerMask = 0;
            isSubtractive = false;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            ref VisibleLight lightData = ref lights.UnsafeElementAtMutable(lightIndex);
            Light light = lightData.light;
            var lightBakingOutput = light.bakingOutput;
            isSubtractive = lightBakingOutput.isBaked && lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed && lightBakingOutput.mixedLightingMode == MixedLightingMode.Subtractive;

            if (light == null)
                return;

            if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightData.light.shadows != LightShadows.None &&
                m_MixedLightingSetup == MixedLightingSetup.None)
            {
                switch (lightBakingOutput.mixedLightingMode)
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
            lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);
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
            bool isSubtractive;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel, out lightLayerMask, out isSubtractive);
            lightColor.w = isSubtractive ? 0f : 1f;

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
                                out data.layerMask, out _);
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
                        if (lightData.mainLightIndex != i)
                        {
                            InitializeLightConstants(
                                lights,
                                i,
                                out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter],
                                out uint lightLayerMask,
                                out var isSubtractive);

                            m_AdditionalLightsLayerMasks[lightIter] = math.asfloat(lightLayerMask);
                            m_AdditionalLightColors[lightIter].w = isSubtractive ? 1f : 0f;
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

            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            int maxVisibleAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
            int len = lightData.visibleLights.Length;
            for (int i = 0; i < len; ++i)
            {
                if (additionalLightsCount >= maxVisibleAdditionalLightsCount)
                    break;

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
