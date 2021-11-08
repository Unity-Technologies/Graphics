using Unity.Collections;
using UnityEngine.PlayerLoop;
using Unity.Jobs;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

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
        MixedLightingSetup m_MixedLightingSetup;

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;
        float[] m_AdditionalLightsLayerMasks;  // Unity has no support for binding uint arrays. We will use asuint() in the shader instead.

        bool m_UseStructuredBuffer;

        bool m_UseClusteredRendering;
        int m_DirectionalLightCount;
        int m_ActualTileWidth;
        int2 m_TileResolution;
        int m_RequestedTileWidth;
        float m_ZBinFactor;
        int m_ZBinOffset;

        JobHandle m_CullingHandle;
        NativeArray<ZBin> m_ZBins;
        NativeArray<uint> m_TileLightMasks;

        ComputeBuffer m_ZBinBuffer;
        ComputeBuffer m_TileBuffer;

        private LightCookieManager m_LightCookieManager;

        internal struct InitParams
        {
            public LightCookieManager lightCookieManager;
            public bool clusteredRendering;
            public int tileSize;

            static internal InitParams GetDefault()
            {
                InitParams p;
                {
                    var settings = LightCookieManager.Settings.GetDefault();
                    var asset = UniversalRenderPipeline.asset;
                    if (asset)
                    {
                        settings.atlas.format = asset.additionalLightsCookieFormat;
                        settings.atlas.resolution = asset.additionalLightsCookieResolution;
                    }

                    p.lightCookieManager = new LightCookieManager(ref settings);
                    p.clusteredRendering = false;
                    p.tileSize = 32;
                }
                return p;
            }
        }

        public ForwardLights() : this(InitParams.GetDefault()) { }

        internal ForwardLights(InitParams initParams)
        {
            if (initParams.clusteredRendering) Assert.IsTrue(math.ispow2(initParams.tileSize));
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_UseClusteredRendering = initParams.clusteredRendering;

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

            m_LightCookieManager = initParams.lightCookieManager;

            if (m_UseClusteredRendering)
            {
                m_ZBinBuffer = new ComputeBuffer(UniversalRenderPipeline.maxZBins / 4, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Constant, ComputeBufferMode.Dynamic);
                m_TileBuffer = new ComputeBuffer(UniversalRenderPipeline.maxTileVec4s, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Constant, ComputeBufferMode.Dynamic);
                m_RequestedTileWidth = initParams.tileSize;
            }
        }

        internal void ProcessLights(ref RenderingData renderingData)
        {
            if (m_UseClusteredRendering)
            {
                var camera = renderingData.cameraData.camera;
                var screenResolution = math.int2(renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);

                var lightCount = renderingData.lightData.visibleLights.Length;
                var lightOffset = 0;
                while (lightOffset < lightCount && renderingData.lightData.visibleLights[lightOffset].lightType == LightType.Directional)
                {
                    lightOffset++;
                }
                if (lightOffset == lightCount) lightOffset = 0;
                lightCount -= lightOffset;

                m_DirectionalLightCount = lightOffset;
                if (renderingData.lightData.mainLightIndex != -1) m_DirectionalLightCount -= 1;

                var visibleLights = renderingData.lightData.visibleLights.GetSubArray(lightOffset, lightCount);
                var lightsPerTile = UniversalRenderPipeline.lightsPerTile;
                var wordsPerTile = lightsPerTile / 32;

                m_ActualTileWidth = m_RequestedTileWidth >> 1;
                do
                {
                    m_ActualTileWidth = m_ActualTileWidth << 1;
                    m_TileResolution = (screenResolution + m_ActualTileWidth - 1) / m_ActualTileWidth;
                }
                while ((m_TileResolution.x * m_TileResolution.y * wordsPerTile) > (UniversalRenderPipeline.maxTileVec4s * 4));

                var fovHalfHeight = math.tan(math.radians(camera.fieldOfView * 0.5f));
                // TODO: Make this work with VR
                var fovHalfWidth = fovHalfHeight * (float)screenResolution.x / (float)screenResolution.y;

                var maxZFactor = (float)UniversalRenderPipeline.maxZBins / (math.sqrt(camera.farClipPlane) - math.sqrt(camera.nearClipPlane));
                m_ZBinFactor = maxZFactor;
                m_ZBinOffset = (int)(math.sqrt(camera.nearClipPlane) * m_ZBinFactor);
                var binCount = (int)(math.sqrt(camera.farClipPlane) * m_ZBinFactor) - m_ZBinOffset;
                // Must be a multiple of 4 to be able to alias to vec4
                binCount = ((binCount + 3) / 4) * 4;
                binCount = math.min(UniversalRenderPipeline.maxZBins, binCount);
                m_ZBins = new NativeArray<ZBin>(binCount, Allocator.TempJob);
                Assert.AreEqual(UnsafeUtility.SizeOf<uint>(), UnsafeUtility.SizeOf<ZBin>());

                using var minMaxZs = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);
                // We allocate double array length because the sorting algorithm needs swap space to work in.
                using var meanZs = new NativeArray<float>(lightCount * 2, Allocator.TempJob);

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
                using var indices = new NativeArray<int>(lightCount * 2, Allocator.TempJob);
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

                LightExtractionJob lightExtractionJob;
                lightExtractionJob.lights = reorderedLights;
                var lightTypes = lightExtractionJob.lightTypes = new NativeArray<LightType>(lightCount, Allocator.TempJob);
                var radiuses = lightExtractionJob.radiuses = new NativeArray<float>(lightCount, Allocator.TempJob);
                var directions = lightExtractionJob.directions = new NativeArray<float3>(lightCount, Allocator.TempJob);
                var positions = lightExtractionJob.positions = new NativeArray<float3>(lightCount, Allocator.TempJob);
                var coneRadiuses = lightExtractionJob.coneRadiuses = new NativeArray<float>(lightCount, Allocator.TempJob);
                var lightExtractionHandle = lightExtractionJob.ScheduleParallel(lightCount, 32, reorderHandle);

                var zBinningJob = new ZBinningJob
                {
                    bins = m_ZBins,
                    minMaxZs = reorderedMinMaxZs,
                    binOffset = m_ZBinOffset,
                    zFactor = m_ZBinFactor
                };
                var zBinningHandle = zBinningJob.ScheduleParallel((binCount + ZBinningJob.batchCount - 1) / ZBinningJob.batchCount, 1, reorderHandle);
                reorderedMinMaxZs.Dispose(zBinningHandle);

                // Must be a multiple of 4 to be able to alias to vec4
                var lightMasksLength = (((wordsPerTile) * m_TileResolution + 3) / 4) * 4;
                var horizontalLightMasks = new NativeArray<uint>(lightMasksLength.y, Allocator.TempJob);
                var verticalLightMasks = new NativeArray<uint>(lightMasksLength.x, Allocator.TempJob);

                // Vertical slices along the x-axis
                var verticalJob = new SliceCullingJob
                {
                    scale = (float)m_ActualTileWidth / (float)screenResolution.x,
                    viewOrigin = camera.transform.position,
                    viewForward = camera.transform.forward,
                    viewRight = camera.transform.right * fovHalfWidth,
                    viewUp = camera.transform.up * fovHalfHeight,
                    lightTypes = lightTypes,
                    radiuses = radiuses,
                    directions = directions,
                    positions = positions,
                    coneRadiuses = coneRadiuses,
                    lightsPerTile = lightsPerTile,
                    sliceLightMasks = verticalLightMasks
                };
                var verticalHandle = verticalJob.ScheduleParallel(m_TileResolution.x, 1, lightExtractionHandle);

                // Horizontal slices along the y-axis
                var horizontalJob = verticalJob;
                horizontalJob.scale = (float)m_ActualTileWidth / (float)screenResolution.y;
                horizontalJob.viewRight = camera.transform.up * fovHalfHeight;
                horizontalJob.viewUp = -camera.transform.right * fovHalfWidth;
                horizontalJob.sliceLightMasks = horizontalLightMasks;
                var horizontalHandle = horizontalJob.ScheduleParallel(m_TileResolution.y, 1, lightExtractionHandle);

                var slicesHandle = JobHandle.CombineDependencies(horizontalHandle, verticalHandle);

                m_TileLightMasks = new NativeArray<uint>(((m_TileResolution.x * m_TileResolution.y * (wordsPerTile) + 3) / 4) * 4, Allocator.TempJob);
                var sliceCombineJob = new SliceCombineJob
                {
                    tileResolution = m_TileResolution,
                    wordsPerTile = wordsPerTile,
                    sliceLightMasksH = horizontalLightMasks,
                    sliceLightMasksV = verticalLightMasks,
                    lightMasks = m_TileLightMasks
                };
                var sliceCombineHandle = sliceCombineJob.ScheduleParallel(m_TileResolution.y, 1, slicesHandle);

                m_CullingHandle = JobHandle.CombineDependencies(sliceCombineHandle, zBinningHandle);

                reorderHandle.Complete();
                NativeArray<VisibleLight>.Copy(reorderedLights, 0, renderingData.lightData.visibleLights, lightOffset, lightCount);

                var tempBias = new NativeArray<Vector4>(lightCount, Allocator.Temp);
                var tempResolution = new NativeArray<int>(lightCount, Allocator.Temp);
                var tempIndices = new NativeArray<int>(lightCount, Allocator.Temp);

                for (var i = 0; i < lightCount; i++)
                {
                    tempBias[indices[i]] = renderingData.shadowData.bias[lightOffset + i];
                    tempResolution[indices[i]] = renderingData.shadowData.resolution[lightOffset + i];
                    tempIndices[indices[i]] = lightOffset + i;
                }

                for (var i = 0; i < lightCount; i++)
                {
                    renderingData.shadowData.bias[i + lightOffset] = tempBias[i];
                    renderingData.shadowData.resolution[i + lightOffset] = tempResolution[i];
                    renderingData.lightData.originalIndices[i + lightOffset] = tempIndices[i];
                }

                tempBias.Dispose();
                tempResolution.Dispose();
                tempIndices.Dispose();

                lightTypes.Dispose(m_CullingHandle);
                radiuses.Dispose(m_CullingHandle);
                directions.Dispose(m_CullingHandle);
                positions.Dispose(m_CullingHandle);
                coneRadiuses.Dispose(m_CullingHandle);
                reorderedLights.Dispose(m_CullingHandle);
                horizontalLightMasks.Dispose(m_CullingHandle);
                verticalLightMasks.Dispose(m_CullingHandle);
                JobHandle.ScheduleBatchedJobs();
            }
        }

        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(null, m_ProfilingSampler))
            {
                var useClusteredRendering = m_UseClusteredRendering;
                if (useClusteredRendering)
                {
                    m_CullingHandle.Complete();

                    m_ZBinBuffer.SetData(m_ZBins.Reinterpret<float4>(UnsafeUtility.SizeOf<ZBin>()), 0, 0, m_ZBins.Length / 4);
                    m_TileBuffer.SetData(m_TileLightMasks.Reinterpret<float4>(UnsafeUtility.SizeOf<uint>()), 0, 0, m_TileLightMasks.Length / 4);

                    cmd.SetGlobalInteger("_AdditionalLightsDirectionalCount", m_DirectionalLightCount);
                    cmd.SetGlobalInteger("_AdditionalLightsZBinOffset", m_ZBinOffset);
                    cmd.SetGlobalFloat("_AdditionalLightsZBinScale", m_ZBinFactor);
                    cmd.SetGlobalVector("_AdditionalLightsTileScale", renderingData.cameraData.pixelRect.size / (float)m_ActualTileWidth);
                    cmd.SetGlobalInteger("_AdditionalLightsTileCountX", m_TileResolution.x);

                    cmd.SetGlobalConstantBuffer(m_ZBinBuffer, "AdditionalLightsZBins", 0, m_ZBins.Length * 4);
                    cmd.SetGlobalConstantBuffer(m_TileBuffer, "AdditionalLightsTiles", 0, m_TileLightMasks.Length * 4);

                    m_ZBins.Dispose();
                    m_TileLightMasks.Dispose();
                }

                SetupShaderLightConstants(cmd, ref renderingData);

                bool lightCountCheck = (renderingData.cameraData.renderer.stripAdditionalLightOffVariants && renderingData.lightData.supportsAdditionalLights) || additionalLightsCount > 0;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    lightCountCheck && additionalLightsPerVertex && !useClusteredRendering);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    lightCountCheck && !additionalLightsPerVertex && !useClusteredRendering);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ClusteredRendering,
                    useClusteredRendering);

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
            CommandBufferPool.Release(cmd);
        }

        internal void Cleanup()
        {
            if (m_UseClusteredRendering)
            {
                m_ZBinBuffer.Dispose();
                m_TileBuffer.Dispose();
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
            lightLayerMask = (uint)additionalLightData.lightLayerMask;
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
            if (lightData.additionalLightsCount == 0)
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
