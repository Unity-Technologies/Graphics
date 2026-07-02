using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Computes and submits lighting data to the GPU.
    /// </summary>
    public partial class ForwardLights
    {
        static class LightShaderPropertyId
        {
            public static readonly int _MainLightPosition = Shader.PropertyToID("_MainLightPosition");   // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static readonly int _MainLightColor = Shader.PropertyToID("_MainLightColor");         // DeferredLights.LightConstantBuffer also refers to the same ShaderPropertyID - TODO: move this definition to a common location shared by other UniversalRP classes
            public static readonly int _MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
            public static readonly int _MainLightLayerMask = Shader.PropertyToID("_MainLightLayerMask");

            public static readonly int _AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");

            // CBUFFER binding for persistent constant buffer path (default)
            public static readonly int _AdditionalLightsBuffer = Shader.PropertyToID("AdditionalLights");

            // Loose-uniform fallback path
            public static readonly int _AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
            public static readonly int _AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
            public static readonly int _AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
            public static readonly int _AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
            public static readonly int _AdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

            public static readonly int _AdditionalLightsLayerMasks = Shader.PropertyToID("_AdditionalLightsLayerMasks");

            // Forward+ CBUFFER bindings
            public static readonly int _ZBinBuffer = Shader.PropertyToID("urp_ZBinBuffer");
            public static readonly int _TileBuffer = Shader.PropertyToID("urp_TileBuffer");

            // Forward+ scalar params packed into vec4s.
            public static readonly int _FPParams0 = Shader.PropertyToID("_FPParams0");
            public static readonly int _FPParams1 = Shader.PropertyToID("_FPParams1");
            public static readonly int _FPParams2 = Shader.PropertyToID("_FPParams2");

            public static readonly int _EnableProbeVolumes = Shader.PropertyToID("_EnableProbeVolumes");
        }

        int m_AdditionalLightsStructuredBufferId;
        int m_AdditionalLightsIndicesId;

        const string k_SetupLightConstants = "Setup Light Constants";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_SetupLightConstants);
        private static readonly ProfilingSampler m_ProfilingSamplerFPSetup = new ProfilingSampler("Forward+ Setup");
        private static readonly ProfilingSampler m_ProfilingSamplerFPComplete = new ProfilingSampler("Forward+ Complete");
        private static readonly ProfilingSampler m_ProfilingSamplerFPUpload = new ProfilingSampler("Forward+ Upload");
        MixedLightingSetup m_MixedLightingSetup;
        
        // Persistent constant buffer path        
        const string k_AdditionalLightsCBName = "Additional Lights Buffer";
        // Channel order of m_AdditionalLightsData (NativeArray<Vector4>), matching the field order in
        // CBUFFER(AdditionalLights). With N = m_MaxVisibleAdditionalLights, channel C occupies [C*N .. (C+1)*N).
        // Keep in sync with Input.hlsl.
        const int k_AdditionalLightsPositionChannel    = 0; // _AdditionalLightsPosition
        const int k_AdditionalLightsColorChannel       = 1; // _AdditionalLightsColor
        const int k_AdditionalLightsAttenuationChannel = 2; // _AdditionalLightsAttenuation
        const int k_AdditionalLightsSpotDirChannel     = 3; // _AdditionalLightsSpotDir
        const int k_AdditionalLightsOcclusionChannel   = 4; // _AdditionalLightsOcclusionProbes
        const int k_AdditionalLightsChannelCount       = 5;

        // NativeArray + GraphicsBuffer rather than a struct (like HDRP CB pattern) because of Mono's 10kB struct limit.
        NativeArray<Vector4> m_AdditionalLightsData;
        GraphicsBuffer m_AdditionalLightsBuffer;

        // Loose-uniform fallback path
        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;

        float[] m_AdditionalLightsLayerMasks;  // Unity has no support for binding uint arrays. We will use asuint() in the shader instead.

        int m_MaxVisibleAdditionalLights;

        bool m_UseStructuredBuffer;
        bool m_UseConstantBuffer;

        bool m_UseForwardPlus;
        int m_DirectionalLightCount;
        int m_ActualTileWidth;
        int2 m_TileResolution;

        JobHandle m_CullingHandle;

        const string k_ZBinCBName =  "URP Z-Bin Buffer";
        NativeArray<uint> m_ZBins;
        GraphicsBuffer m_ZBinsBuffer;
        
        const string k_TileCBName = "URP Tile Buffer";
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
            m_UseConstantBuffer = RenderingUtils.usePersistentConstantBuffer;
            m_UseForwardPlus = initParams.forwardPlus;
            m_MaxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;

            if (m_UseStructuredBuffer)
            {
                m_AdditionalLightsStructuredBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
                m_AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");
            }
            else if (m_UseConstantBuffer)
            {
                CreateAdditionalLightsConstantBuffer();
            }
            else
            {
                CreateAdditionalLightsLooseUniformArrays();
            }

            if (m_UseForwardPlus)
            {
                CreateForwardPlusConstantBuffers();
                m_ReflectionProbeManager = ReflectionProbeManager.Create();
            }

            m_LightCookieManager = initParams.lightCookieManager;
        }

        void CreateAdditionalLightsConstantBuffer()
        {
            int length = m_MaxVisibleAdditionalLights * k_AdditionalLightsChannelCount;
            m_AdditionalLightsData = new NativeArray<Vector4>(length, Allocator.Persistent);

            // GraphicsBuffer ctor throws for zero length.
            if (length > 0)
            {
                m_AdditionalLightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, length, UnsafeUtility.SizeOf<Vector4>())
                {
                    name = k_AdditionalLightsCBName
                };
            }
            m_AdditionalLightsLayerMasks = new float[m_MaxVisibleAdditionalLights];
        }

        void DisposeAdditionalLightsConstantBuffer()
        {
            if (m_AdditionalLightsData.IsCreated)
                m_AdditionalLightsData.Dispose();

            if (m_AdditionalLightsBuffer != null)
            {
                Shader.SetGlobalConstantBuffer(LightShaderPropertyId._AdditionalLightsBuffer, (ComputeBuffer)null, 0, 0);
                m_AdditionalLightsBuffer.Dispose();
                m_AdditionalLightsBuffer = null;
            }
        }

        void CreateAdditionalLightsLooseUniformArrays()
        {
            m_AdditionalLightPositions = new Vector4[m_MaxVisibleAdditionalLights];
            m_AdditionalLightColors = new Vector4[m_MaxVisibleAdditionalLights];
            m_AdditionalLightAttenuations = new Vector4[m_MaxVisibleAdditionalLights];
            m_AdditionalLightSpotDirections = new Vector4[m_MaxVisibleAdditionalLights];
            m_AdditionalLightOcclusionProbeChannels = new Vector4[m_MaxVisibleAdditionalLights];
            m_AdditionalLightsLayerMasks = new float[m_MaxVisibleAdditionalLights];
        }

        void CreateForwardPlusConstantBuffers()
        {
            m_ZBins = new NativeArray<uint>(UniversalRenderPipeline.maxZBinWords, Allocator.Persistent);
            m_ZBinsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxZBinWords / 4, UnsafeUtility.SizeOf<float4>());
            m_ZBinsBuffer.name = k_ZBinCBName;
            m_TileMasks = new NativeArray<uint>(UniversalRenderPipeline.maxTileWords, Allocator.Persistent);
            m_TileMasksBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, UniversalRenderPipeline.maxTileWords / 4, UnsafeUtility.SizeOf<float4>());
            m_TileMasksBuffer.name = k_TileCBName;
        }

        void ResizeAdditionalLightsBuffer(int maxLights)
        {
            m_MaxVisibleAdditionalLights = maxLights;
            if (m_UseStructuredBuffer)
            {
                // SSBO buffers are sized on demand inside ShaderData
            }
            else if (m_UseConstantBuffer)
            {
                DisposeAdditionalLightsConstantBuffer();
                CreateAdditionalLightsConstantBuffer();
            }
            else
            {
                CreateAdditionalLightsLooseUniformArrays();
            }
        }

        internal ReflectionProbeManager reflectionProbeManager => m_ReflectionProbeManager;

        static int AlignByteCount(int count, int align) => align * ((count + align - 1) / align);

        // Calculate view planes and viewToViewportScaleBias. This handles projection center in case the projection is off-centered
        static void GetViewParams(
            bool isOrthographic,
            float4x4 viewToClip,
            out float viewPlaneBot,
            out float viewPlaneTop,
            out float4 viewToViewportScaleBias
        )
        {
            // We want to calculate `fovHalfHeight = tan(fov / 2)`
            // `projection[1][1]` contains `1 / tan(fov / 2)`
            var viewPlaneHalfSizeInv = math.float2(viewToClip[0][0], viewToClip[1][1]);
            var viewPlaneHalfSize = math.rcp(viewPlaneHalfSizeInv);
            var centerClipSpace = isOrthographic ? -math.float2(viewToClip[3][0], viewToClip[3][1]): math.float2(viewToClip[2][0], viewToClip[2][1]);

            viewPlaneBot = centerClipSpace.y * viewPlaneHalfSize.y - viewPlaneHalfSize.y;
            viewPlaneTop = centerClipSpace.y * viewPlaneHalfSize.y + viewPlaneHalfSize.y;
            viewToViewportScaleBias = math.float4(
                viewPlaneHalfSizeInv * 0.5f,
                -centerClipSpace * 0.5f + 0.5f
            );
        }

        /// <summary>
        /// This function is a purely functional (i.e. no global state mutation)
        /// way of invoking light clustering. It is used while actual rendering,
        /// but also in unit testing.
        /// </summary>
        internal static JobHandle ScheduleClusteringJobs(
            bool hasMainLight,
            bool supportsAdditionalLights,
            NativeArray<VisibleLight> lights,
            NativeArray<VisibleReflectionProbe> probes,
            NativeArray<uint> zBins,
            NativeArray<uint> tileMasks,
            Fixed2<float4x4> worldToViews,
            Fixed2<float4x4> viewToClips,
            int viewCount,
            int2 screenResolution,
            float nearClipPlane,
            float farClipPlane,
            bool isOrthographic,
            out int localLightCount,
            out int directionalLightCount,
            out int binCount,
            out float zBinScale,
            out float zBinOffset,
            out int2 tileResolution,
            out int actualTileWidth,
            out int wordsPerTile
        )
        {
            localLightCount = supportsAdditionalLights ? lights.Length: 0;
            // The lights array first has directional lights, and then local lights. We traverse the list to find the
            // index of the first local light.
            var firstLocalLightIdx = 0;
            while (firstLocalLightIdx < localLightCount && lights[firstLocalLightIdx].lightType == LightType.Directional)
            {
                firstLocalLightIdx++;
            }
            localLightCount -= firstLocalLightIdx;

            // If there's 1 or more directional lights, one of them could be the main light
            if (firstLocalLightIdx > 0)
            {

                directionalLightCount = firstLocalLightIdx;
                if (hasMainLight)
                    directionalLightCount -= 1;
            }
            else
            {
                directionalLightCount = 0;
            }

            var localLights = lights.GetSubArray(firstLocalLightIdx, localLightCount);

            var reflectionProbeCount = math.min(probes.Length, UniversalRenderPipeline.maxVisibleReflectionProbes);

            var itemsPerTile = localLights.Length + reflectionProbeCount;
            wordsPerTile = (itemsPerTile + 31) / 32;

            actualTileWidth = 8 >> 1;
            do
            {
                actualTileWidth <<= 1;
                tileResolution = (screenResolution + actualTileWidth - 1) / actualTileWidth;
            }
            while ((tileResolution.x * tileResolution.y * wordsPerTile * viewCount) > UniversalRenderPipeline.maxTileWords);

            if (!isOrthographic)
            {
                // Use to calculate binIndex = log2(z) * zBinScale + zBinOffset
                zBinScale = (UniversalRenderPipeline.maxZBinWords / viewCount) / ((math.log2(farClipPlane) - math.log2(nearClipPlane)) * (2 + wordsPerTile));
                zBinOffset = -math.log2(nearClipPlane) * zBinScale;
                binCount = (int)(math.log2(farClipPlane) * zBinScale + zBinOffset);
            }
            else
            {
                // Use to calculate binIndex = z * zBinScale + zBinOffset
                zBinScale = (UniversalRenderPipeline.maxZBinWords / viewCount) / ((farClipPlane - nearClipPlane) * (2 + wordsPerTile));
                zBinOffset = -nearClipPlane * zBinScale;
                binCount = (int)(farClipPlane * zBinScale + zBinOffset);
            }

            // Necessary to avoid negative bin count when the farClipPlane is set to Infinity in the editor.
            binCount = Math.Max(binCount, 0);

            var minMaxZs = new NativeArray<float2>(itemsPerTile * viewCount, Allocator.TempJob);

            var lightMinMaxZJob = new LightMinMaxZJob
            {
                worldToViews = worldToViews,
                lights = localLights,
                minMaxZs = minMaxZs.GetSubArray(0, localLightCount * viewCount)
            };
            // Innerloop batch count of 32 is not special, just a handwavy amount to not have too much scheduling overhead nor too little parallelism.
            var lightMinMaxZHandle = lightMinMaxZJob.ScheduleParallel(localLightCount * viewCount, 32, new JobHandle());

            var reflectionProbeRotation = GraphicsSettings.TryGetRenderPipelineSettings<URPReflectionProbeSettings>(out var reflectionProbeSettings) ? reflectionProbeSettings.UseReflectionProbeRotation : true;

            var reflectionProbeMinMaxZJob = new ReflectionProbeMinMaxZJob
            {
                worldToViews = worldToViews,
                reflectionProbes = probes,
                reflectionProbeRotation = reflectionProbeRotation,
                minMaxZs = minMaxZs.GetSubArray(localLightCount * viewCount, reflectionProbeCount * viewCount)
            };
            var reflectionProbeMinMaxZHandle = reflectionProbeMinMaxZJob.ScheduleParallel(reflectionProbeCount * viewCount, 32, lightMinMaxZHandle);


            var zBinningBatchCount = (binCount + ZBinningJob.batchSize - 1) / ZBinningJob.batchSize;
            var zBinningJob = new ZBinningJob
            {
                bins = zBins,
                minMaxZs = minMaxZs,
                zBinScale = zBinScale,
                zBinOffset = zBinOffset,
                binCount = binCount,
                wordsPerTile = wordsPerTile,
                lightCount = localLightCount,
                reflectionProbeCount = reflectionProbeCount,
                batchCount = zBinningBatchCount,
                viewCount = viewCount,
                isOrthographic = isOrthographic
            };
            var zBinningHandle = zBinningJob.ScheduleParallel(zBinningBatchCount * viewCount, 1, reflectionProbeMinMaxZHandle);

            GetViewParams(isOrthographic, viewToClips[0], out float viewPlaneBottom0, out float viewPlaneTop0, out float4 viewToViewportScaleBias0);
            GetViewParams(isOrthographic, viewToClips[1], out float viewPlaneBottom1, out float viewPlaneTop1, out float4 viewToViewportScaleBias1);

            // Each light needs 1 range for Y, and a range per row. Align to 128-bytes to avoid false sharing.
            var rangesPerItem = AlignByteCount((1 + tileResolution.y) * UnsafeUtility.SizeOf<InclusiveRange>(), 128) / UnsafeUtility.SizeOf<InclusiveRange>();
            var tileRanges = new NativeArray<InclusiveRange>(rangesPerItem * itemsPerTile * viewCount, Allocator.TempJob);
            var tilingJob = new TilingJob
            {
                lights = localLights,
                reflectionProbes = probes,
                reflectionProbeRotation = reflectionProbeRotation,
                tileRanges = tileRanges,
                itemsPerTile = itemsPerTile,
                rangesPerItem = rangesPerItem,
                worldToViews = worldToViews,
                tileScale = (float2)screenResolution / actualTileWidth,
                tileScaleInv = actualTileWidth / (float2)screenResolution,
                viewPlaneBottoms = new Fixed2<float>(viewPlaneBottom0, viewPlaneBottom1),
                viewPlaneTops = new Fixed2<float>(viewPlaneTop0, viewPlaneTop1),
                viewToViewportScaleBiases = new Fixed2<float4>(viewToViewportScaleBias0, viewToViewportScaleBias1),
                tileCount = tileResolution,
                near = nearClipPlane,
                isOrthographic = isOrthographic
            };

            var tileRangeHandle = tilingJob.ScheduleParallel(itemsPerTile * viewCount, 1, reflectionProbeMinMaxZHandle);

            var expansionJob = new TileRangeExpansionJob
            {
                tileRanges = tileRanges,
                tileMasks = tileMasks,
                rangesPerItem = rangesPerItem,
                itemsPerTile = itemsPerTile,
                wordsPerTile = wordsPerTile,
                tileResolution = tileResolution,
            };

            var tilingHandle = expansionJob.ScheduleParallel(tileResolution.y * viewCount, 1, tileRangeHandle);
            JobHandle cullingHandle = JobHandle.CombineDependencies(
                minMaxZs.Dispose(zBinningHandle),
                tileRanges.Dispose(tilingHandle));
            return cullingHandle;
        }

        internal void PreSetup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
            if (maxLights != m_MaxVisibleAdditionalLights)
            {
                ResizeAdditionalLightsBuffer(maxLights);
            }

            if (m_UseForwardPlus)
            {
                using var _ = new ProfilingScope(m_ProfilingSamplerFPSetup);

                m_ReflectionProbeManager.PreSetup();
                
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
                    CreateForwardPlusConstantBuffers();
                }
                else
                {
                    unsafe
                    {
                        UnsafeUtility.MemClear(m_ZBins.GetUnsafePtr(), m_ZBins.Length * sizeof(uint));
                        UnsafeUtility.MemClear(m_TileMasks.GetUnsafePtr(), m_TileMasks.Length * sizeof(uint));
                    }
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                var viewCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
                var viewCount = 1;
#endif

                var worldToViews = new Fixed2<float4x4>(cameraData.GetViewMatrix(0), cameraData.GetViewMatrix(math.min(1, viewCount - 1)));
                var viewToClips = new Fixed2<float4x4>(cameraData.GetProjectionMatrix(0), cameraData.GetProjectionMatrix(math.min(1, viewCount - 1)));

                m_CullingHandle = ScheduleClusteringJobs(
                    lightData.mainLightIndex != -1,
                    lightData.supportsAdditionalLights,
                    lightData.visibleLights,
                    renderingData.cullResults.visibleReflectionProbes,
                    m_ZBins,
                    m_TileMasks,
                    worldToViews,
                    viewToClips,
                    viewCount,
                    math.int2(cameraData.pixelWidth, cameraData.pixelHeight),
                    cameraData.camera.nearClipPlane,
                    cameraData.camera.farClipPlane,
                    cameraData.camera.orthographic,
                    out m_LightCount,
                    out m_DirectionalLightCount,
                    out m_BinCount,
                    out m_ZBinScale,
                    out m_ZBinOffset,
                    out m_TileResolution,
                    out m_ActualTileWidth,
                    out m_WordsPerTile
                );

                JobHandle.ScheduleBatchedJobs();
            }
        }

        static readonly ProfilingSampler s_SetupForwardLights = new ProfilingSampler("Setup Forward Lights");

        private class SetupLightPassData
        {
            internal UniversalRenderingData renderingData;
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal ForwardLights forwardLights;
        };
        /// <summary>
        /// Sets up the ForwardLight data for RenderGraph execution
        /// </summary>
        internal void SetupRenderGraphLights(RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            using (var builder = renderGraph.AddUnsafePass<SetupLightPassData>(s_SetupForwardLights.name, out var passData,
                s_SetupForwardLights))
            {
                passData.renderingData = renderingData;
                passData.cameraData = cameraData;
                passData.lightData = lightData;
                passData.forwardLights = this;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (SetupLightPassData data, UnsafeGraphContext rgContext) =>
                {
                    data.forwardLights.SetupLights(rgContext.cmd, data.renderingData, data.cameraData, data.lightData);
                });
            }
        }

        internal void SetupLights(UnsafeCommandBuffer cmd, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            int additionalLightsCount = lightData.additionalLightsCount;
            bool additionalLightsPerVertex = lightData.shadeAdditionalLightsPerVertex;
            using (new ProfilingScope(m_ProfilingSampler))
            {
                if (m_UseForwardPlus)
                {
                    if (lightData.reflectionProbeAtlas)
                    {
                        m_ReflectionProbeManager.UpdateGpuData(CommandBufferHelpers.GetNativeCommandBuffer(cmd), ref renderingData.cullResults);
                    }

                    using (new ProfilingScope(m_ProfilingSamplerFPComplete))
                    {
                        m_CullingHandle.Complete();
                    }

                    using (new ProfilingScope(m_ProfilingSamplerFPUpload))
                    {
                        m_ZBinsBuffer.SetData(m_ZBins.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        m_TileMasksBuffer.SetData(m_TileMasks.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()));
                        cmd.SetGlobalConstantBuffer(m_ZBinsBuffer, LightShaderPropertyId._ZBinBuffer, 0, UniversalRenderPipeline.maxZBinWords * 4);
                        cmd.SetGlobalConstantBuffer(m_TileMasksBuffer, LightShaderPropertyId._TileBuffer, 0, UniversalRenderPipeline.maxTileWords * 4);
                    }

                    cmd.SetGlobalVector(LightShaderPropertyId._FPParams0, math.float4(m_ZBinScale, m_ZBinOffset, m_LightCount, m_DirectionalLightCount));
                    cmd.SetGlobalVector(LightShaderPropertyId._FPParams1, math.float4(cameraData.pixelRect.size / m_ActualTileWidth, m_TileResolution.x, m_WordsPerTile));
                    cmd.SetGlobalVector(LightShaderPropertyId._FPParams2, math.float4(m_BinCount, m_TileResolution.x * m_TileResolution.y, 0, 0));
                }

                SetupShaderLightConstants(cmd, ref renderingData.cullResults, lightData, renderingData.reuseCullingResult);

                bool lightCountCheck = (cameraData.renderer.stripAdditionalLightOffVariants && lightData.supportsAdditionalLights) || additionalLightsCount > 0;
                cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightsVertex, lightCountCheck && additionalLightsPerVertex && !m_UseForwardPlus);
                cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightsPixel,  lightCountCheck && !additionalLightsPerVertex && !m_UseForwardPlus);
                cmd.SetKeyword(ShaderGlobalKeywords.ClusterLightLoop, m_UseForwardPlus);
                cmd.SetKeyword(ShaderGlobalKeywords.ForwardPlus, m_UseForwardPlus); // Backward compatibility. Deprecated in 6.1.

                bool isShadowMask = lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
                bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                bool isSubtractive = lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
                cmd.SetKeyword(ShaderGlobalKeywords.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                cmd.SetKeyword(ShaderGlobalKeywords.ShadowsShadowMask, isShadowMask);
                cmd.SetKeyword(ShaderGlobalKeywords.MixedLightingSubtractive, isSubtractive); // Backward compatibility
                cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeBlending, lightData.reflectionProbeBlending);
                cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeBoxProjection, lightData.reflectionProbeBoxProjection);
                cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeAtlas, lightData.reflectionProbeAtlas && m_UseForwardPlus && lightData.reflectionProbeBlending); // Needs to match shader stripping

                var asset = UniversalRenderPipeline.asset;
#if UNITY_META_QUEST
                if (asset != null)
                    cmd.SetKeyword(ShaderGlobalKeywords.META_QUEST_LIGHTUNROLL, asset.maxAdditionalLightsCount == 1 && asset.additionalLightsRenderingMode != LightRenderingMode.Disabled);
#endif
                bool apvIsEnabled = asset != null && asset.lightProbeSystem == LightProbeSystem.ProbeVolumes;
                #if UNITY_WEBGL && !UNITY_EDITOR
                apvIsEnabled &= SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU; // APV not supported on WebGL, don't try to enable it. WebGPU is fine, though.
                #endif

                ProbeVolumeSHBands probeVolumeSHBands = asset.probeVolumeSHBands;

                cmd.SetKeyword(ShaderGlobalKeywords.ProbeVolumeL1, apvIsEnabled && probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1);
                cmd.SetKeyword(ShaderGlobalKeywords.ProbeVolumeL2, apvIsEnabled && probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2);

				// TODO: If we can robustly detect LIGHTMAP_ON, we can skip SH logic.
                var shMode = PlatformAutoDetect.ShAutoDetect(asset.shEvalMode);
                cmd.SetKeyword(ShaderGlobalKeywords.EVALUATE_SH_MIXED, shMode == ShEvalMode.Mixed);
                cmd.SetKeyword(ShaderGlobalKeywords.EVALUATE_SH_VERTEX, shMode == ShEvalMode.PerVertex);

                var stack = VolumeManager.instance.stack;
                bool enableProbeVolumes = ProbeReferenceVolume.instance.UpdateShaderVariablesProbeVolumes(
                    CommandBufferHelpers.GetNativeCommandBuffer(cmd),
                    stack.GetComponent<ProbeVolumesOptions>(),
                    cameraData.IsTemporalAAEnabled() ? Time.frameCount : 0,
                    lightData.supportsLightLayers);

                cmd.SetGlobalInt(LightShaderPropertyId._EnableProbeVolumes, enableProbeVolumes ? 1 : 0);
                cmd.SetKeyword(ShaderGlobalKeywords.LightLayers, lightData.supportsLightLayers && !CoreUtils.IsSceneLightingDisabled(cameraData.camera));

                if (m_LightCookieManager != null)
                {
                    m_LightCookieManager.Setup(CommandBufferHelpers.GetNativeCommandBuffer(cmd), lightData);
                }
                else
                {
                    cmd.SetKeyword(ShaderGlobalKeywords.LightCookies, false);
                }

                if (GraphicsSettings.TryGetRenderPipelineSettings<LightmapSamplingSettings>(out var lightmapSamplingSettings))
                    cmd.SetKeyword(ShaderGlobalKeywords.LIGHTMAP_BICUBIC_SAMPLING, lightmapSamplingSettings.useBicubicLightmapSampling);
                else
                    cmd.SetKeyword(ShaderGlobalKeywords.LIGHTMAP_BICUBIC_SAMPLING, false);

                if (GraphicsSettings.TryGetRenderPipelineSettings<URPReflectionProbeSettings>(out var reflectionProbeSettings))
                    cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeRotation, reflectionProbeSettings.UseReflectionProbeRotation);
                else
                    cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeRotation, false);
            }
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

            DisposeAdditionalLightsConstantBuffer();

            m_LightCookieManager?.Dispose();
            m_LightCookieManager = null;
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, bool supportsLightLayers, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel, out uint lightLayerMask, out bool isSubtractive)
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

            if (supportsLightLayers)
            {
                var additionalLightData = light.GetUniversalAdditionalLightData();
                lightLayerMask = RenderingLayerUtils.ToValidRenderingLayers(additionalLightData.renderingLayers);
            }
        }

        void SetupShaderLightConstants(UnsafeCommandBuffer cmd, ref CullingResults cullResults, UniversalLightData lightData, bool reuseCullingResult)
        {
            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Universal pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, lightData);
            SetupAdditionalLightConstants(cmd, ref cullResults, lightData, reuseCullingResult);
        }

        void SetupMainLightConstants(UnsafeCommandBuffer cmd, UniversalLightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir, lightOcclusionChannel;
            bool supportsLightLayers = lightData.supportsLightLayers;
            uint lightLayerMask;
            bool isSubtractive;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, supportsLightLayers, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel, out lightLayerMask, out isSubtractive);
            lightColor.w = isSubtractive ? 0f : 1f;

            cmd.SetGlobalVector(LightShaderPropertyId._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightShaderPropertyId._MainLightColor, lightColor);
            cmd.SetGlobalVector(LightShaderPropertyId._MainLightOcclusionProbesChannel, lightOcclusionChannel);

            if (supportsLightLayers)
                cmd.SetGlobalInt(LightShaderPropertyId._MainLightLayerMask, (int)lightLayerMask);
        }

        void SetupAdditionalLightConstants(UnsafeCommandBuffer cmd, ref CullingResults cullResults, UniversalLightData lightData, bool reuseCullingResult)
        {
            bool supportsLightLayers = lightData.supportsLightLayers;
            var lights = lightData.visibleLights;
            int maxAdditionalLightsCount = m_MaxVisibleAdditionalLights;
            int additionalLightsCount = SetupPerObjectLightIndices(cullResults, lightData, reuseCullingResult);
            if (additionalLightsCount > 0)
            {
                int mainLight = lightData.mainLightIndex;
                if (m_UseStructuredBuffer)
                {
                    NativeArray<ShaderInput.LightData> additionalLightsData = new NativeArray<ShaderInput.LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        if (mainLight != i)
                        {
                            ShaderInput.LightData data;
                            InitializeLightConstants(lights, i, supportsLightLayers,
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

                    cmd.SetGlobalBuffer(m_AdditionalLightsStructuredBufferId, lightDataBuffer);
                    cmd.SetGlobalBuffer(m_AdditionalLightsIndicesId, lightIndicesBuffer);

                    additionalLightsData.Dispose();
                }
                else if (m_UseConstantBuffer)
                {
                    int positionOffset    = maxAdditionalLightsCount * k_AdditionalLightsPositionChannel;
                    int colorOffset       = maxAdditionalLightsCount * k_AdditionalLightsColorChannel;
                    int attenuationOffset = maxAdditionalLightsCount * k_AdditionalLightsAttenuationChannel;
                    int spotDirOffset     = maxAdditionalLightsCount * k_AdditionalLightsSpotDirChannel;
                    int occlusionOffset   = maxAdditionalLightsCount * k_AdditionalLightsOcclusionChannel;

                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        if (mainLight != i)
                        {
                            InitializeLightConstants(
                                lights,
                                i,
                                supportsLightLayers,
                                out Vector4 position,
                                out Vector4 color,
                                out Vector4 attenuation,
                                out Vector4 spotDir,
                                out Vector4 occlusionProbes,
                                out uint lightLayerMask,
                                out var isSubtractive);

                            color.w = isSubtractive ? 1f : 0f;

                            m_AdditionalLightsData[positionOffset    + lightIter] = position;
                            m_AdditionalLightsData[colorOffset       + lightIter] = color;
                            m_AdditionalLightsData[attenuationOffset + lightIter] = attenuation;
                            m_AdditionalLightsData[spotDirOffset     + lightIter] = spotDir;
                            m_AdditionalLightsData[occlusionOffset   + lightIter] = occlusionProbes;

                            if (supportsLightLayers)
                                m_AdditionalLightsLayerMasks[lightIter] = math.asfloat(lightLayerMask);

                            lightIter++;
                        }
                    }

                    if (m_AdditionalLightsBuffer != null)
                    {
                        m_AdditionalLightsBuffer.SetData(m_AdditionalLightsData);

                        cmd.SetGlobalConstantBuffer(m_AdditionalLightsBuffer, LightShaderPropertyId._AdditionalLightsBuffer, 0,
                            m_AdditionalLightsData.Length * UnsafeUtility.SizeOf<Vector4>());
                    }

                    if (supportsLightLayers)
                        cmd.SetGlobalFloatArray(LightShaderPropertyId._AdditionalLightsLayerMasks, m_AdditionalLightsLayerMasks);
                }
                else
                {
                    for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                    {
                        if (mainLight != i)
                        {
                            InitializeLightConstants(
                                lights,
                                i,
                                supportsLightLayers,
                                out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter],
                                out uint lightLayerMask,
                                out var isSubtractive);

                            if (supportsLightLayers)
                                m_AdditionalLightsLayerMasks[lightIter] = math.asfloat(lightLayerMask);

                            m_AdditionalLightColors[lightIter].w = isSubtractive ? 1f : 0f;
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightShaderPropertyId._AdditionalLightsPosition, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightShaderPropertyId._AdditionalLightsColor, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightShaderPropertyId._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightShaderPropertyId._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightShaderPropertyId._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);

                    if (supportsLightLayers)
                        cmd.SetGlobalFloatArray(LightShaderPropertyId._AdditionalLightsLayerMasks, m_AdditionalLightsLayerMasks);
                }

                cmd.SetGlobalVector(LightShaderPropertyId._AdditionalLightsCount, new Vector4(lightData.maxPerObjectAdditionalLightsCount, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                cmd.SetGlobalVector(LightShaderPropertyId._AdditionalLightsCount, Vector4.zero);
            }
        }

        int SetupPerObjectLightIndices(CullingResults cullResults, UniversalLightData lightData, bool reuseCullingResult)
        {
            if (lightData.additionalLightsCount == 0 || m_UseForwardPlus)
                return lightData.additionalLightsCount;

            // SetLightIndexMap was already applied on the previous pass; calling it again would
            // mutate the shared per-object cull a second time.
            if (reuseCullingResult)
                return lightData.additionalLightsCount;

            var perObjectLightIndexMap = cullResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            int len = lightData.visibleLights.Length;
            for (int i = 0; i < len; ++i)
            {
                if (additionalLightsCount >= m_MaxVisibleAdditionalLights)
                    break;

                if (i == lightData.mainLightIndex)
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalDirectionalLightsCount;
                }
                else
                {
                    if (lightData.visibleLights[i].lightType == LightType.Directional ||
                        lightData.visibleLights[i].lightType == LightType.Spot ||
                        lightData.visibleLights[i].lightType == LightType.Point)
                    {
                        // Light type is supported
                        perObjectLightIndexMap[i] -= globalDirectionalLightsCount;
                    }
                    else
                    {
                        // Light type is not supported. Skip the light.
                        perObjectLightIndexMap[i] = -1;
                    }

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
