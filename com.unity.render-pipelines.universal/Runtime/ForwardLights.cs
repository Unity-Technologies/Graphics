using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using Unity.Profiling;
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

            public static int _AdditionalLightsCount;
            public static int _AdditionalLightsPosition;
            public static int _AdditionalLightsColor;
            public static int _AdditionalLightsAttenuation;
            public static int _AdditionalLightsSpotDir;
            public static int _AdditionalLightOcclusionProbeChannel;
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

        bool m_UseStructuredBuffer;

        public ForwardLights()
        {
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;

            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._MainLightOcclusionProbesChannel = Shader.PropertyToID("_MainLightOcclusionProbes");
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

                int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                m_AdditionalLightPositions = new Vector4[maxLights];
                m_AdditionalLightColors = new Vector4[maxLights];
                m_AdditionalLightAttenuations = new Vector4[maxLights];
                m_AdditionalLightSpotDirections = new Vector4[maxLights];
                m_AdditionalLightOcclusionProbeChannels = new Vector4[maxLights];
            }
        }

        ProfilerMarker mainThreadMarker = new ProfilerMarker("Forward+");
        ComputeBuffer m_ZBinBuffer;
        ComputeBuffer m_TileBuffer;

        public void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            mainThreadMarker.Begin();

            var camera = renderingData.cameraData.camera;

            var tilingLevels = 3;
            var tile0Width = 16;
            var screenResolution = math.int2(renderingData.cameraData.pixelWidth, renderingData.cameraData.pixelHeight);

            var lightCount = renderingData.lightData.visibleLights.Length;
            var lightsPerTile = lightCount + (32 - (lightCount % 33));

            var fovHalfHeight = math.tan(math.radians(camera.fieldOfView * 0.5f));
            var fovHalfWidth = fovHalfHeight * (float)camera.pixelWidth / (float)camera.pixelHeight;

            var zFactor = math.sqrt((float)screenResolution.y / (math.sqrt(2f) * fovHalfHeight));
            var binOffset = (int)(math.sqrt(camera.nearClipPlane) * zFactor);
            var binCount = (int)(math.sqrt(camera.farClipPlane) * zFactor) - binOffset;
            var zBins = new NativeArray<ZBin>(binCount, Allocator.TempJob);

            var minMaxZs = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);
            var meanZs = new NativeArray<float>(lightCount * 2, Allocator.TempJob);

            Matrix4x4 worldToViewMatrix = renderingData.cameraData.GetViewMatrix();
            var minMaxZJob = new MinMaxZJob
            {
                worldToViewMatrix = worldToViewMatrix,
                lights = renderingData.lightData.visibleLights,
                minMaxZs = minMaxZs,
                meanZs = meanZs
            };
            var minMaxZHandle = minMaxZJob.ScheduleParallel(lightCount, 32, new JobHandle());

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

            var reorderLightsJob = new ReorderJob<VisibleLight> { indices = indices, input = renderingData.lightData.visibleLights, output = reorderedLights };
            var reorderLightsHandle = reorderLightsJob.ScheduleParallel(lightCount, 32, zSortHandle);

            var reorderMinMaxZsJob = new ReorderJob<LightMinMaxZ> { indices = indices, input = minMaxZs, output = reorderedMinMaxZs };
            var reorderMinMaxZsHandle = reorderMinMaxZsJob.ScheduleParallel(lightCount, 32, zSortHandle);
            minMaxZs.Dispose(reorderMinMaxZsHandle);
            minMaxZs = reorderedMinMaxZs;

            var reorderHandle = JobHandle.CombineDependencies(
                reorderLightsHandle,
                reorderMinMaxZsHandle
            );

            LightExtractionJob lightExtractionJob;
            lightExtractionJob.viewOrigin = camera.transform.position;
            lightExtractionJob.lights = reorderedLights;
            lightExtractionJob.tilingLights = new NativeArray<TilingLightData>(lightCount, Allocator.TempJob);
            var lightExtractionHandle = lightExtractionJob.ScheduleParallel(lightCount, 32, reorderHandle);

            var zBinningJob = new ZBinningJob
            {
                bins = zBins,
                minMaxZs = minMaxZs,
                binOffset = binOffset,
                zFactor = zFactor
            };
            var zBinningHandle = zBinningJob.ScheduleParallel((binCount + ZBinningJob.batchCount - 1) / ZBinningJob.batchCount, 1, reorderHandle);

            NativeArray<uint> tiles = default;

            var tilingHandle = lightExtractionHandle;

            var groupWidth = tile0Width;
            for (var i = 0; i < tilingLevels; i++)
            {
                groupWidth *= FineTilingJob.groupWidth;
            }
            var groupResolution = (screenResolution + groupWidth - 1) / groupWidth;

            for (var i = 0; i < tilingLevels; i++)
            {
                var tileWidth = groupWidth / FineTilingJob.groupWidth;
                var tileResolution = groupResolution * FineTilingJob.groupWidth;
                var tileCount = tileResolution.x * tileResolution.y;
                var groupCount = groupResolution.x * groupResolution.y;
                int groupWordCount = groupCount * lightsPerTile / 32;

                var groupTilesActive = new NativeArray<uint>(groupWordCount, Allocator.TempJob);
                NativeArray<uint> groupTilesHit;
                if (i == 0)
                {
                    groupTilesHit = new NativeArray<uint>(groupWordCount, Allocator.TempJob);
                    var initTilesJob = new InitTilesJob
                    {
                        tilesHit = groupTilesHit,
                        tilesActive = groupTilesActive,
                        tileResolution = groupResolution,
                        lightCount = lightCount,
                        lightsPerTile = lightsPerTile
                    };
                    tilingHandle = initTilesJob.ScheduleParallel(groupCount, 16, tilingHandle);
                }
                else
                {
                    groupTilesHit = tiles;
                    var activeTilesJob = new ActiveTilesJob
                    {
                        tilesHit = groupTilesHit,
                        tilesActive = groupTilesActive,
                        tileResolution = groupResolution,
                        lightCount = lightCount,
                        lightsPerTile = lightsPerTile
                    };
                    tilingHandle = activeTilesJob.ScheduleParallel(groupCount, 16, tilingHandle);
                }

                tiles = new NativeArray<uint>(tileCount * lightsPerTile / 32, Allocator.TempJob);

                FineTilingJob tilingJob;
                tilingJob.minMaxZs = minMaxZs;
                tilingJob.lights = lightExtractionJob.tilingLights;
                tilingJob.lightsPerTile = lightsPerTile;
                tilingJob.tiles = tiles;
                tilingJob.groupTilesHit = groupTilesHit;
                tilingJob.groupTilesActive = groupTilesActive;
                tilingJob.screenResolution = screenResolution;
                tilingJob.groupResolution = groupResolution;
                tilingJob.tileResolution = tileResolution;
                tilingJob.tileWidth = tileWidth;
                tilingJob.viewOrigin = camera.transform.position;
                tilingJob.viewForward = camera.transform.forward.normalized;
                tilingJob.viewRight = camera.transform.right.normalized * fovHalfWidth;
                tilingJob.viewUp = camera.transform.up.normalized * fovHalfHeight;
                tilingJob.fovHalf = math.float2(fovHalfWidth, fovHalfHeight);
                tilingJob.worldToViewMatrix = camera.worldToCameraMatrix;
                tilingJob.farPlane = camera.farClipPlane;
                tilingJob.tileAperture = math.SQRT2 * fovHalfHeight / (((float)screenResolution.y / (float)tileWidth));
                tilingHandle = tilingJob.ScheduleParallel(groupCount, 1, tilingHandle);
                groupTilesHit.Dispose(tilingHandle);
                groupTilesActive.Dispose(tilingHandle);

                groupResolution = tileResolution;
                groupWidth = tileWidth;
            }

            // Note: From this point, groupResolution and groupWidth are actually tile rather than group.

            JobHandle.CompleteAll(ref zBinningHandle, ref tilingHandle);

            for (var i = 1; i < lightCount; i++)
            {
                Assert.IsTrue(meanZs[i] >= meanZs[i - 1]);
            }

            if (m_ZBinBuffer == null || m_ZBinBuffer.count < binCount)
            {
                if (m_ZBinBuffer != null)
                {
                    m_ZBinBuffer.Dispose();
                }

                m_ZBinBuffer = new ComputeBuffer(math.ceilpow2(binCount), UnsafeUtility.SizeOf<ZBin>());
                Shader.SetGlobalBuffer("_ZBinBuffer", m_ZBinBuffer);
            }

            if (m_TileBuffer == null || m_TileBuffer.count < tiles.Length)
            {
                if (m_TileBuffer != null)
                {
                    m_TileBuffer.Dispose();
                }

                m_TileBuffer = new ComputeBuffer(math.ceilpow2(tiles.Length), sizeof(uint));
                Shader.SetGlobalBuffer("_TileBuffer", m_TileBuffer);
            }

            {
                var tileResolution = groupResolution;
                var tileCount = tileResolution.x * tileResolution.y;
                m_ZBinBuffer.SetData(zBins, 0, 0, zBins.Length);
                m_TileBuffer.SetData(tiles, 0, 0, tiles.Length);
                Shader.SetGlobalInteger("_ZBinOffset", binOffset);
                Shader.SetGlobalFloat("_ZBinFactor", zFactor);
                Shader.SetGlobalInteger("_ZBinLightCount", lightCount);
                Shader.SetGlobalMatrix("_FPWorldToViewMatrix", minMaxZJob.worldToViewMatrix);
                Shader.SetGlobalVector("_InvNormalizedTileSize", (Vector2)((float2)(screenResolution / tile0Width)));
                Shader.SetGlobalFloat("_TileXCount", tileResolution.x);
                Shader.SetGlobalFloat("_TileYCount", tileResolution.y);
                Shader.SetGlobalInteger("_LightCount", lightCount);
                Shader.SetGlobalInteger("_WordsPerTile", lightsPerTile / 32);
                Shader.SetGlobalFloat("_TileCount", tileCount);
            }

            minMaxZs.Dispose();
            meanZs.Dispose();
            indices.Dispose();
            reorderedLights.Dispose();
            zBins.Dispose();
            tiles.Dispose();
            lightExtractionJob.tilingLights.Dispose();

            mainThreadMarker.End();

            int additionalLightsCount = renderingData.lightData.additionalLightsCount;
            bool additionalLightsPerVertex = renderingData.lightData.shadeAdditionalLightsPerVertex;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(null, m_ProfilingSampler))
            {
                SetupShaderLightConstants(cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    additionalLightsCount > 0 && additionalLightsPerVertex);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    additionalLightsCount > 0 && !additionalLightsPerVertex);

                bool isShadowMask = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
                bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
                bool isSubtractive = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, lightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionProbeChannel);

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
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir, out lightOcclusionChannel);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightOcclusionProbesChannel, lightOcclusionChannel);
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
                                out data.spotDirection, out data.occlusionProbeChannels);
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
                            InitializeLightConstants(lights, i, out m_AdditionalLightPositions[lightIter],
                                out m_AdditionalLightColors[lightIter],
                                out m_AdditionalLightAttenuations[lightIter],
                                out m_AdditionalLightSpotDirections[lightIter],
                                out m_AdditionalLightOcclusionProbeChannels[lightIter]);
                            lightIter++;
                        }
                    }

                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, m_AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, m_AdditionalLightColors);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);
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
