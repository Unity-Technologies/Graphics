using Unity.Collections;
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

        bool m_UseClusteredRendering;
        int m_DirectionalLightCount;
        int m_ActualTileWidth;
        int m_RequestedTileWidth;
        float m_ZBinFactor;
        int m_ZBinOffset;

        JobHandle m_CullingHandle;
        NativeArray<ZBin> m_ZBins;
        NativeArray<uint> m_HorizontalLightMasks;
        NativeArray<uint> m_VerticalLightMasks;

        Vector4[] m_ZBinBuffer;
        Vector4[] m_HorizontalBuffer;
        Vector4[] m_VerticalBuffer;

        public ForwardLights(bool clusteredRendering, int tileSize)
        {
            Assert.IsTrue(math.ispow2(tileSize));
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;
            m_UseClusteredRendering = clusteredRendering;

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

            if (m_UseClusteredRendering)
            {
                m_ZBinBuffer = new Vector4[UniversalRenderPipeline.maxZBins / 4];
                m_HorizontalBuffer = new Vector4[UniversalRenderPipeline.maxVisibilityVec4s];
                m_VerticalBuffer = new Vector4[UniversalRenderPipeline.maxVisibilityVec4s];
                m_RequestedTileWidth = tileSize;
            }
        }

        public void ProcessLights(ref RenderingData renderingData)
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

                m_ActualTileWidth = m_RequestedTileWidth >> 1;
                int2 tileResolution;
                do
                {
                    m_ActualTileWidth = m_ActualTileWidth << 1;
                    tileResolution = (screenResolution + m_ActualTileWidth - 1) / m_ActualTileWidth;
                }
                while (math.any(tileResolution * lightsPerTile / 32 / 4 > UniversalRenderPipeline.maxVisibilityVec4s));

                var fovHalfHeight = math.tan(math.radians(camera.fieldOfView * 0.5f));
                // TODO: Make this work with VR
                var fovHalfWidth = fovHalfHeight * (float)screenResolution.x / (float)screenResolution.y;

                // TODO: Decide whether to go for desired or always max
                //m_ZBinFactor = math.sqrt((float)screenResolution.y / (math.sqrt(2f) * fovHalfHeight));
                var maxZFactor = (float)UniversalRenderPipeline.maxZBins / (math.sqrt(camera.farClipPlane) - math.sqrt(camera.nearClipPlane));
                // m_ZBinFactor = math.min(m_ZBinFactor, maxZFactor);
                m_ZBinFactor = maxZFactor;
                m_ZBinOffset = (int)(math.sqrt(camera.nearClipPlane) * m_ZBinFactor);
                var binCount = (int)(math.sqrt(camera.farClipPlane) * m_ZBinFactor) - m_ZBinOffset;
                // c = sqrt(far) * factor - sqrt(near) * factor => c = factor * (sqrt(far) - sqrt(near)) => factor = c / (sqrt(far) - sqrt(near))
                // Must be a multiple of 4 to be able to alias to vec4
                binCount = ((binCount + 3) / 4) * 4;
                binCount = math.min(UniversalRenderPipeline.maxZBins, binCount);
                m_ZBins = new NativeArray<ZBin>(binCount, Allocator.TempJob);
                Assert.AreEqual(UnsafeUtility.SizeOf<uint>(), UnsafeUtility.SizeOf<ZBin>());

                using var minMaxZs = new NativeArray<LightMinMaxZ>(lightCount, Allocator.TempJob);
                using var meanZs = new NativeArray<float>(lightCount * 2, Allocator.TempJob);

                Matrix4x4 worldToViewMatrix = renderingData.cameraData.GetViewMatrix();
                var minMaxZJob = new MinMaxZJob
                {
                    worldToViewMatrix = worldToViewMatrix,
                    lights = visibleLights,
                    minMaxZs = minMaxZs,
                    meanZs = meanZs
                };
                var minMaxZHandle = minMaxZJob.ScheduleParallel(lightCount, 32, new JobHandle());

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
                var lightMasksLength = ((lightsPerTile / 32 * tileResolution + 3) / 4) * 4;
                m_HorizontalLightMasks = new NativeArray<uint>(lightMasksLength.y, Allocator.TempJob);
                m_VerticalLightMasks = new NativeArray<uint>(lightMasksLength.x, Allocator.TempJob);

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
                    sliceLightMasks = m_VerticalLightMasks
                };
                var verticalHandle = verticalJob.ScheduleParallel(tileResolution.x, 1, lightExtractionHandle);

                // Horizontal slices along the y-axis
                var horizontalJob = verticalJob;
                horizontalJob.scale = (float)m_ActualTileWidth / (float)screenResolution.y;
                horizontalJob.viewRight = camera.transform.up * fovHalfHeight;
                horizontalJob.viewUp = -camera.transform.right * fovHalfWidth;
                horizontalJob.sliceLightMasks = m_HorizontalLightMasks;
                var horizontalHandle = horizontalJob.ScheduleParallel(tileResolution.y, 1, lightExtractionHandle);

                m_CullingHandle = JobHandle.CombineDependencies(horizontalHandle, verticalHandle, zBinningHandle);

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
                if (m_UseClusteredRendering)
                {
                    m_CullingHandle.Complete();

                    unsafe
                    {
                        fixed(Vector4* zBinPtr = m_ZBinBuffer, horizontalPtr = m_HorizontalBuffer, verticalPtr = m_VerticalBuffer)
                        {
                            UnsafeUtility.MemCpy(zBinPtr, m_ZBins.GetUnsafeReadOnlyPtr(), m_ZBins.Length * sizeof(ZBin));
                            UnsafeUtility.MemCpy(horizontalPtr, m_HorizontalLightMasks.GetUnsafeReadOnlyPtr(), m_HorizontalLightMasks.Length * sizeof(uint));
                            UnsafeUtility.MemCpy(verticalPtr, m_VerticalLightMasks.GetUnsafeReadOnlyPtr(), m_VerticalLightMasks.Length * sizeof(uint));
                        }
                    }

                    // NativeArray<Vector4>.Copy(m_ZBins.Reinterpret<Vector4>(UnsafeUtility.SizeOf<ZBin>()), m_ZBinBuffer, m_ZBins.Length / 4);
                    // NativeArray<Vector4>.Copy(m_HorizontalLightMasks.Reinterpret<Vector4>(UnsafeUtility.SizeOf<uint>()), m_HorizontalBuffer, m_HorizontalLightMasks.Length / 4);
                    // NativeArray<Vector4>.Copy(m_VerticalLightMasks.Reinterpret<Vector4>(UnsafeUtility.SizeOf<uint>()), m_VerticalBuffer, m_VerticalLightMasks.Length / 4);

                    cmd.SetGlobalInteger("_AdditionalLightsDirectionalCount", m_DirectionalLightCount);
                    cmd.SetGlobalInteger("_AdditionalLightsZBinOffset", m_ZBinOffset);
                    cmd.SetGlobalFloat("_AdditionalLightsZBinScale", m_ZBinFactor);
                    cmd.SetGlobalVector("_AdditionalLightsTileScale", renderingData.cameraData.pixelRect.size / (float)m_ActualTileWidth);

                    cmd.SetGlobalVectorArray("_AdditionalLightsZBins", m_ZBinBuffer);
                    cmd.SetGlobalVectorArray("_AdditionalLightsHorizontalVisibility", m_HorizontalBuffer);
                    cmd.SetGlobalVectorArray("_AdditionalLightsVerticalVisibility", m_VerticalBuffer);

                    m_ZBins.Dispose();
                    m_HorizontalLightMasks.Dispose();
                    m_VerticalLightMasks.Dispose();
                }

                SetupShaderLightConstants(cmd, ref renderingData);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsVertex,
                    additionalLightsCount > 0 && additionalLightsPerVertex && !m_UseClusteredRendering);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightsPixel,
                    additionalLightsCount > 0 && !additionalLightsPerVertex && !m_UseClusteredRendering);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ClusteredRenderingCPU,
                    m_UseClusteredRendering);

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

        public void Dispose()
        {
            // if (m_UseClusteredRendering)
            // {
            //     m_ZBinBuffer.Dispose();
            //     m_HorizontalBuffer.Dispose();
            //     m_VerticalBuffer.Dispose();
            // }
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
