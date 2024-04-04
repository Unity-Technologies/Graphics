using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Rendering.HighDefinition
{
    static class HDShadowCullingUtils
    {
        static class ComputeShadowCullingInfoProfilerMarkers
        {
            public static ProfilerMarker indicesAndPreambleMarker = new ProfilerMarker("WriteShadowIndicesAndCollectLightInfo");
            public static ProfilerMarker lightBucketingMarker = new ProfilerMarker("LightBucketing");
            public static ProfilerMarker computePointShadowCullingInfosMarker = new ProfilerMarker("ComputePointShadowCullingInfos");
            public static ProfilerMarker computeSpotShadowCullingInfosMarker = new ProfilerMarker("ComputeSpotShadowCullingInfos");
            public static ProfilerMarker computeAreaRectangleShadowCullingInfosMarker = new ProfilerMarker("ComputeAreaRectangleShadowCullingInfos");
            public static ProfilerMarker computeDirectionalShadowCullingInfosMarker = new ProfilerMarker("ComputeDirectionalShadowCullingInfos");
        }

        public static unsafe void ComputeCullingSplits(in HDShadowInitParameters hdShadowInitParams,
            HDLightRenderDatabase lightRenderDatabase,
            HDShadowRequestDatabase shadowRequestDatabase,
            HDShadowManager shadowManager,
            HDShadowSettings shadowSettings,
            in CullingResults cullingResult,
            HDProcessedVisibleLightsBuilder processedVisibleLights,
            NativeArray<LightShadowCasterCullingInfo> outPerLightShadowCullingInfos,
            NativeArray<ShadowSplitData> outSplitBuffer,
            out int outTotalSplitCount)
        {
            using var profilerScope = new ProfilingScope(ProfilingSampler.Get(HDProfileId.ComputeShadowCullingSplits));

            HDShadowManagerDataForComputeCullingSplitsJob shadowManagerData = default;
            shadowManager.GetUnmanageDataForComputeCullingSplitsJob(ref shadowManagerData);

            int shadowLightCount = processedVisibleLights.shadowLightCount;
            int maxShadowSplitCount = shadowLightCount * HDShadowUtils.k_MaxShadowSplitCount;
            int visibleLightCount = cullingResult.visibleLights.Length;
            NativeArray<HDProcessedVisibleLight> processedLights = processedVisibleLights.processedEntities.GetSubArray(0, visibleLightCount);
            NativeArray<int> splitBufferOffset = new NativeArray<int>(1, Allocator.TempJob);
            NativeArray<Matrix4x4> cubemapFaces = new NativeArray<Matrix4x4>(HDShadowUtils.kCubemapFaces, Allocator.TempJob);

            NativeReference<float3> newCachedDirectionalAngles = new NativeReference<float3>(HDCachedShadowManager.instance.cachedDirectionalAngles, Allocator.TempJob);
            NativeArray<HDShadowCullingSplit> hdSplitBuffer = processedVisibleLights.shadowCullingSplitBuffer.GetSubArray(0, maxShadowSplitCount);
            NativeArray<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesBuffer = processedVisibleLights.visibleLightsAndIndicesBuffer;
            NativeList<ShadowIndicesAndVisibleLightData> splitVisibleLightsAndIndicesBuffer = processedVisibleLights.splitVisibleLightsAndIndicesBuffer;

            // Those lists are output by the Burst job. They will alias the splitVisibleLightsAndIndicesBuffer above so they must no be disposed.
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicPointVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedPointVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicSpotVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedSpotVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaRectangleVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaRectangleVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicDirectionalVisibleLightsAndIndices;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedDirectionalVisibleLightsAndIndices;

            // Those lists are output by the Burst job. They will alias the hdSplitBuffer above so they must not be disposed.
            UnsafeList<HDShadowCullingSplit> dynamicPointHDSplits;
            UnsafeList<HDShadowCullingSplit> cachedPointHDSplits;
            UnsafeList<HDShadowCullingSplit> dynamicSpotHDSplits;
            UnsafeList<HDShadowCullingSplit> cachedSpotHDSplits;
            UnsafeList<HDShadowCullingSplit> dynamicAreaRectangleHDSplits;
            UnsafeList<HDShadowCullingSplit> cachedAreaRectangleHDSplits;
            UnsafeList<HDShadowCullingSplit> dynamicDirectionalHDSplits;
            UnsafeList<HDShadowCullingSplit> cachedDirectionalHDSplits;

            var computeShadowCasterCullingInfosJob = new ComputeShadowCasterCullingInfosJob
            {
                indicesAndPreambleMarker = ComputeShadowCullingInfoProfilerMarkers.indicesAndPreambleMarker,
                lightBucketingMarker = ComputeShadowCullingInfoProfilerMarkers.lightBucketingMarker,
                computePointShadowCullingInfosMarker = ComputeShadowCullingInfoProfilerMarkers.computePointShadowCullingInfosMarker,
                computeSpotShadowCullingInfosMarker = ComputeShadowCullingInfoProfilerMarkers.computeSpotShadowCullingInfosMarker,
                computeAreaRectangleShadowCullingInfosMarker = ComputeShadowCullingInfoProfilerMarkers.computeAreaRectangleShadowCullingInfosMarker,
                computeDirectionalShadowCullingInfosMarker = ComputeShadowCullingInfoProfilerMarkers.computeDirectionalShadowCullingInfosMarker,

                shadowManager = shadowManagerData,
                cubeMapFaces = cubemapFaces,
                visibleLights = cullingResult.visibleLights,
                processedLights = processedLights,
                sortKeys = processedVisibleLights.sortKeys,
                visibleLightEntityDataIndices = processedVisibleLights.visibleLightEntityDataIndices,
                additionalLightDataUpdateInfos = lightRenderDatabase.additionalLightDataUpdateInfos,
                shadowResolutionRequestStorage = shadowManager.shadowResolutionRequestStorage.AsArray(),
                packedShadowRequestSetHandles = lightRenderDatabase.packedShadowRequestSetHandles.AsArray(),
                hdShadowRequestIndicesStorage = shadowRequestDatabase.hdShadowRequestIndicesStorage.AsArray(),
                cascadeShadowSplits = GetCascadeRatiosAsVector3(shadowSettings.cascadeShadowSplits),
                cascadeShadowSplitCount = shadowSettings.cascadeShadowSplitCount.value,
                punctualShadowFilteringQuality = hdShadowInitParams.punctualShadowFilteringQuality,
                usesReversedZBuffer = SystemInfo.usesReversedZBuffer,
                shadowNearPlaneOffset = QualitySettings.shadowNearPlaneOffset,
                sortedLightCount = processedVisibleLights.sortedLightCounts,
                invalidDataIndex = HDLightRenderDatabase.InvalidDataIndex,

                inOutSplitBufferOffset = splitBufferOffset,
                outNewCachedDirectionalAngles = newCachedDirectionalAngles,
                outShadowRequestValidityArray = processedVisibleLights.shadowRequestValidityArray,
                outHDSplitBuffer = hdSplitBuffer,
                outSplitBuffer = outSplitBuffer,
                outPerLightShadowCullingInfos = outPerLightShadowCullingInfos,
                outVisibleLightsAndIndicesBuffer = visibleLightsAndIndicesBuffer,
                outSplitVisibleLightsAndIndicesBuffer = splitVisibleLightsAndIndicesBuffer,

                outDynamicPointVisibleLightsAndIndices = &dynamicPointVisibleLightsAndIndices,
                outCachedPointVisibleLightsAndIndices = &cachedPointVisibleLightsAndIndices,
                outDynamicSpotVisibleLightsAndIndices = &dynamicSpotVisibleLightsAndIndices,
                outCachedSpotVisibleLightsAndIndices = &cachedSpotVisibleLightsAndIndices,
                outDynamicAreaRectangleVisibleLightsAndIndices = &dynamicAreaRectangleVisibleLightsAndIndices,
                outCachedAreaRectangleVisibleLightsAndIndices = &cachedAreaRectangleVisibleLightsAndIndices,
                outDynamicDirectionalVisibleLightsAndIndices = &dynamicDirectionalVisibleLightsAndIndices,
                outCachedDirectionalVisibleLightsAndIndices = &cachedDirectionalVisibleLightsAndIndices,

                outDynamicPointHDSplits = &dynamicPointHDSplits,
                outCachedPointHDSplits = &cachedPointHDSplits,
                outDynamicSpotHDSplits = &dynamicSpotHDSplits,
                outCachedSpotHDSplits = &cachedSpotHDSplits,
                outDynamicAreaRectangleHDSplits = &dynamicAreaRectangleHDSplits,
                outCachedAreaRectangleHDSplits = &cachedAreaRectangleHDSplits,
                outDynamicDirectionalHDSplits = &dynamicDirectionalHDSplits,
                outCachedDirectionalHDSplits = &cachedDirectionalHDSplits
            };

            // This double pass setup is needed because we can not compute the ShadowSplitData for directional lights using Burst.
            // We would need to call CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives from a Burst job, but that is not possible at the moment.
            // So the first pass is a Burst job processing all the lights except the directionals. It also output a LightMetadata struct to an UnsafeList for each "valid" directional light found.
            // The second pass is then just normal C# code processing the UnsafeList of directional lights found by the Burst job.
            computeShadowCasterCullingInfosJob.Run();
            computeShadowCasterCullingInfosJob.ProcessDirectionalLights(cullingResult, dynamicDirectionalVisibleLightsAndIndices, cachedDirectionalVisibleLightsAndIndices);
            outTotalSplitCount = splitBufferOffset[0];

            processedVisibleLights.dynamicPointVisibleLightsAndIndices = dynamicPointVisibleLightsAndIndices;
            processedVisibleLights.cachedPointVisibleLightsAndIndices = cachedPointVisibleLightsAndIndices;
            processedVisibleLights.dynamicSpotVisibleLightsAndIndices = dynamicSpotVisibleLightsAndIndices;
            processedVisibleLights.cachedSpotVisibleLightsAndIndices = cachedSpotVisibleLightsAndIndices;
            processedVisibleLights.dynamicAreaRectangleVisibleLightsAndIndices = dynamicAreaRectangleVisibleLightsAndIndices;
            processedVisibleLights.cachedAreaRectangleVisibleLightsAndIndices = cachedAreaRectangleVisibleLightsAndIndices;
            processedVisibleLights.dynamicDirectionalVisibleLightsAndIndices = dynamicDirectionalVisibleLightsAndIndices;
            processedVisibleLights.cachedDirectionalVisibleLightsAndIndices = cachedDirectionalVisibleLightsAndIndices;

            processedVisibleLights.dynamicPointHDSplits = dynamicPointHDSplits;
            processedVisibleLights.cachedPointHDSplits = cachedPointHDSplits;
            processedVisibleLights.dynamicSpotHDSplits = dynamicSpotHDSplits;
            processedVisibleLights.cachedSpotHDSplits = cachedSpotHDSplits;
            processedVisibleLights.dynamicAreaRectangleHDSplits = dynamicAreaRectangleHDSplits;
            processedVisibleLights.cachedAreaRectangleHDSplits = cachedAreaRectangleHDSplits;
            processedVisibleLights.dynamicDirectionalHDSplits = dynamicDirectionalHDSplits;
            processedVisibleLights.cachedDirectionalHDSplits = cachedDirectionalHDSplits;

            HDCachedShadowManager.instance.cachedDirectionalAngles = newCachedDirectionalAngles.Value;

            splitBufferOffset.Dispose();
            cubemapFaces.Dispose();
            newCachedDirectionalAngles.Dispose();
        }

        [BurstCompile]
        unsafe struct ComputeShadowCasterCullingInfosJob : IJob
        {
            [ReadOnly] public ProfilerMarker indicesAndPreambleMarker;
            [ReadOnly] public ProfilerMarker lightBucketingMarker;
            [ReadOnly] public ProfilerMarker computePointShadowCullingInfosMarker;
            [ReadOnly] public ProfilerMarker computeSpotShadowCullingInfosMarker;
            [ReadOnly] public ProfilerMarker computeAreaRectangleShadowCullingInfosMarker;
            [ReadOnly] public ProfilerMarker computeDirectionalShadowCullingInfosMarker;

            [ReadOnly] public NativeArray<Matrix4x4> cubeMapFaces;
            [ReadOnly] public NativeArray<VisibleLight> visibleLights;
            [ReadOnly] public NativeArray<HDProcessedVisibleLight> processedLights;
            [ReadOnly] public NativeArray<uint> sortKeys;
            [ReadOnly] public NativeArray<int> visibleLightEntityDataIndices;
            [ReadOnly] public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
            [ReadOnly] public NativeArray<HDShadowResolutionRequest> shadowResolutionRequestStorage;
            [ReadOnly] public NativeArray<HDShadowRequestSetHandle> packedShadowRequestSetHandles;
            [ReadOnly] public NativeArray<int> hdShadowRequestIndicesStorage;
            [ReadOnly] public Vector3 cascadeShadowSplits;
            [ReadOnly] public int cascadeShadowSplitCount;
            [ReadOnly] public HDShadowFilteringQuality punctualShadowFilteringQuality;
            [ReadOnly] public bool usesReversedZBuffer;
            [ReadOnly] public float shadowNearPlaneOffset;
            [ReadOnly] public int sortedLightCount;
            [ReadOnly] public int invalidDataIndex;

            public HDShadowManagerDataForComputeCullingSplitsJob shadowManager;
            public NativeArray<int> inOutSplitBufferOffset;
            public NativeReference<float3> outNewCachedDirectionalAngles;
            public NativeBitArray outShadowRequestValidityArray;
            public NativeArray<HDShadowCullingSplit> outHDSplitBuffer;
            public NativeArray<ShadowSplitData> outSplitBuffer;
            public NativeArray<LightShadowCasterCullingInfo> outPerLightShadowCullingInfos;
            public NativeArray<ShadowIndicesAndVisibleLightData> outVisibleLightsAndIndicesBuffer;
            public NativeList<ShadowIndicesAndVisibleLightData> outSplitVisibleLightsAndIndicesBuffer;

            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outDynamicPointVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outCachedPointVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outDynamicSpotVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outCachedSpotVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outDynamicAreaRectangleVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outCachedAreaRectangleVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outDynamicDirectionalVisibleLightsAndIndices;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<ShadowIndicesAndVisibleLightData>* outCachedDirectionalVisibleLightsAndIndices;

            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outDynamicPointHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outCachedPointHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outDynamicSpotHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outCachedSpotHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outDynamicAreaRectangleHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outCachedAreaRectangleHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outDynamicDirectionalHDSplits;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<HDShadowCullingSplit>* outCachedDirectionalHDSplits;

            public void Execute()
            {
                HDAdditionalLightDataUpdateInfo* updateInfosUnsafePtr = (HDAdditionalLightDataUpdateInfo*)additionalLightDataUpdateInfos.GetUnsafeReadOnlyPtr();
                ShadowIndicesAndVisibleLightData* visibleLightsAndIndicesBufferPtr = (ShadowIndicesAndVisibleLightData*)outVisibleLightsAndIndicesBuffer.GetUnsafePtr();

                int cachedAreaRectangleCount = 0;
                int cachedPointCount = 0;
                int cachedSpotCount = 0;
                int cachedDirectionalCount = 0;
                int dynamicAreaRectangleCount = 0;
                int dynamicPointCount = 0;
                int dynamicSpotCount = 0;
                int dynamicDirectionalCount = 0;
                int collectedLightCount = 0;

                // We do a few things in this first loop:
                //
                // 1. We gather counts for each light type/atlas combo so that we can categorically bucket them in the next loop.
                //      We do this to reduce the 30-odd atlas collections potentially involved with each light down to 9 or less per loop.
                //
                // 2. We copy relevant light data to a buffer, in order to reduce random access in subsequent loops.
                //

                using (indicesAndPreambleMarker.Auto())
                {
                    float3 newCachedDirectionalAngles = shadowManager.cachedDirectionalAngles;

                    for (int sortKeyIndex = 0; sortKeyIndex < sortedLightCount; sortKeyIndex++)
                    {
                        uint sortKey = sortKeys[sortKeyIndex];

                        int lightIndex = (int)(sortKey & 0xFFFF);
                        if ((processedLights[lightIndex].shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) == 0)
                            continue;

                        int dataIndex = visibleLightEntityDataIndices[lightIndex];
                        if (dataIndex == invalidDataIndex)
                            continue;

                        HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[dataIndex];
                        if (!shadowRequestSetHandle.valid)
                            continue;

                        ref HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref updateInfosUnsafePtr[dataIndex];
                        VisibleLight visibleLight = visibleLights[lightIndex];
                        int splitCount = HDAdditionalLightData.GetShadowRequestCount(cascadeShadowSplitCount, visibleLight.lightType);
                        ShadowMapUpdateType shadowUpdateType = HDAdditionalLightData.GetShadowUpdateType(visibleLight.lightType,
                            lightUpdateInfo.shadowUpdateMode,
                            lightUpdateInfo.alwaysDrawDynamicShadows,
                            shadowManager.cachedShadowManager.directionalHasCachedAtlas);

                        BitArray8 isSplitValidMask = new BitArray8(0);

                        for (int i = 0; i < splitCount; i++)
                        {
                            HDShadowRequestHandle shadowRequestIndexLocation = shadowRequestSetHandle[i];

                            int shadowRequestIndex = hdShadowRequestIndicesStorage[shadowRequestIndexLocation.storageIndexForRequestIndex];
                            if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManager.requestCount)
                                continue;

                            isSplitValidMask[(uint)i] = true;
                        }

                        if (isSplitValidMask.allFalse)
                            continue;

                        BitArray8 needCacheUpdateMask = ComputeNeedCacheUpdateMask(ref lightUpdateInfo, ref visibleLight, splitCount, ref newCachedDirectionalAngles);

                        ref ShadowIndicesAndVisibleLightData bufferElement = ref visibleLightsAndIndicesBufferPtr[lightIndex];
                        bufferElement.additionalLightUpdateInfo = lightUpdateInfo;
                        bufferElement.visibleLight = visibleLight;
                        bufferElement.dataIndex = dataIndex;
                        bufferElement.lightIndex = lightIndex;
                        bufferElement.shadowRequestSetHandle = shadowRequestSetHandle;
                        bufferElement.sortKeyIndex = sortKeyIndex;
                        bufferElement.splitCount = splitCount;
                        bufferElement.isSplitValidMask = isSplitValidMask;
                        bufferElement.needCacheUpdateMask = needCacheUpdateMask;
                        bufferElement.shadowUpdateType = shadowUpdateType;

                        outShadowRequestValidityArray.Set(sortKeyIndex, true);

                        switch (visibleLight.lightType)
                        {
                            case LightType.Spot:
                            case LightType.Box:
                            case LightType.Pyramid:
                                if (lightUpdateInfo.hasCachedComponent)
                                    ++cachedSpotCount;
                                else
                                    ++dynamicSpotCount;
                                break;
                            case LightType.Directional:
                                if (lightUpdateInfo.hasCachedComponent)
                                    ++cachedDirectionalCount;
                                else
                                    ++dynamicDirectionalCount;
                                break;
                            case LightType.Point:
                                if (lightUpdateInfo.hasCachedComponent)
                                    ++cachedPointCount;
                                else
                                    ++dynamicPointCount;
                                break;
                            case LightType.Rectangle:
                                if (lightUpdateInfo.hasCachedComponent)
                                    ++cachedAreaRectangleCount;
                                else
                                    ++dynamicAreaRectangleCount;
                                break;
                            default:
                                throw new NotImplementedException("Light type not supported in real-time");
                        }

                        ++collectedLightCount;
                    }

                    outNewCachedDirectionalAngles.Value = newCachedDirectionalAngles;
                }

                // Now that we have the counts for each bucket, we divide a scratchpad array into slices,
                // and categorically sort data from our buffer into each slice of it.
                // Future iterations may involve storing the data in a normalized form to begin with,
                // in which case we would skip both this and the count collection from the previous loop.

                outSplitVisibleLightsAndIndicesBuffer.Length = collectedLightCount;
                ShadowIndicesAndVisibleLightData* splitVisibleLightsAndIndicesBufferPtr = (ShadowIndicesAndVisibleLightData*)outSplitVisibleLightsAndIndicesBuffer.GetUnsafePtr();
                int bufferOffsetIterator = 0;

                UnsafeList<ShadowIndicesAndVisibleLightData> dynamicPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                dynamicPointVisibleLightsAndIndices.m_capacity = dynamicPointCount;
                bufferOffsetIterator += dynamicPointCount;
                UnsafeList<ShadowIndicesAndVisibleLightData> cachedPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                cachedPointVisibleLightsAndIndices.m_capacity = cachedPointCount;
                bufferOffsetIterator += cachedPointCount;

                UnsafeList<ShadowIndicesAndVisibleLightData> dynamicSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                dynamicSpotVisibleLightsAndIndices.m_capacity = dynamicSpotCount;
                bufferOffsetIterator += dynamicSpotCount;
                UnsafeList<ShadowIndicesAndVisibleLightData> cachedSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                cachedSpotVisibleLightsAndIndices.m_capacity = cachedSpotCount;
                bufferOffsetIterator += cachedSpotCount;

                UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                dynamicAreaRectangleVisibleLightsAndIndices.m_capacity = dynamicAreaRectangleCount;
                bufferOffsetIterator += dynamicAreaRectangleCount;
                UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                cachedAreaRectangleVisibleLightsAndIndices.m_capacity = cachedAreaRectangleCount;
                bufferOffsetIterator += cachedAreaRectangleCount;

                UnsafeList<ShadowIndicesAndVisibleLightData> dynamicDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                dynamicDirectionalVisibleLightsAndIndices.m_capacity = dynamicDirectionalCount;
                bufferOffsetIterator += dynamicDirectionalCount;
                UnsafeList<ShadowIndicesAndVisibleLightData> cachedDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
                cachedDirectionalVisibleLightsAndIndices.m_capacity = cachedDirectionalCount;
                bufferOffsetIterator += cachedDirectionalCount;

                using (lightBucketingMarker.Auto())
                {
                    for (int sortKeyIndex = 0; sortKeyIndex < sortedLightCount; sortKeyIndex++)
                    {
                        uint sortKey = sortKeys[sortKeyIndex];
                        if (!outShadowRequestValidityArray.IsSet(sortKeyIndex))
                            continue;

                        int lightIndex = (int)(sortKey & 0xFFFF);
                        ref ShadowIndicesAndVisibleLightData readData = ref visibleLightsAndIndicesBufferPtr[lightIndex];
                        ref HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref readData.additionalLightUpdateInfo;
                        LightType lightType = readData.visibleLight.lightType;
                        bool hasCachedComponent = lightUpdateInfo.shadowUpdateMode != ShadowUpdateMode.EveryFrame;

                        switch (lightType)
                        {
                            case LightType.Spot:
                            case LightType.Pyramid:
                            case LightType.Box:
                                if (hasCachedComponent)
                                {
                                    cachedSpotVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                else
                                {
                                    dynamicSpotVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                break;
                            case LightType.Directional:
                                if (hasCachedComponent)
                                {
                                    cachedDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                else
                                {
                                    dynamicDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                break;
                            case LightType.Point:
                                if (hasCachedComponent)
                                {
                                    cachedPointVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                else
                                {
                                    dynamicPointVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                break;
                            case LightType.Rectangle:
                                if (hasCachedComponent)
                                {
                                    cachedAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                else
                                {
                                    dynamicAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                                }
                                break;
                            default:
                                throw new NotImplementedException("Light type not supported in real-time");
                        }
                    }
                }

                int splitBufferOffset = inOutSplitBufferOffset[0];
                HDShadowCullingSplit* hdSplitBufferPtr = (HDShadowCullingSplit*)outHDSplitBuffer.GetUnsafePtr();

                UnsafeList<HDShadowCullingSplit> dynamicPointHDSplits;
                UnsafeList<HDShadowCullingSplit> cachedPointHDSplits;

                using (computePointShadowCullingInfosMarker.Auto())
                {
                    int dynamicSplitOffset = splitBufferOffset;
                    int dynamicSplitCount = ComputePointShadowCullingSplits(dynamicPointVisibleLightsAndIndices, dynamicSplitOffset);
                    splitBufferOffset += dynamicSplitCount;

                    int cachedSplitOffset = splitBufferOffset;
                    int cachedSplitCount = ComputePointShadowCullingSplits(cachedPointVisibleLightsAndIndices, cachedSplitOffset);
                    splitBufferOffset += cachedSplitCount;

                    dynamicPointHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + dynamicSplitOffset, dynamicSplitCount);
                    cachedPointHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + cachedSplitOffset, cachedSplitCount);
                }

                UnsafeList<HDShadowCullingSplit> dynamicSpotHDSplits;
                UnsafeList<HDShadowCullingSplit> cachedSpotHDSplits;

                using (computeSpotShadowCullingInfosMarker.Auto())
                {
                    int dynamicSplitOffset = splitBufferOffset;
                    int dynamicSplitCount = ComputeSpotShadowCullingSplits(dynamicSpotVisibleLightsAndIndices, dynamicSplitOffset);
                    splitBufferOffset += dynamicSplitCount;

                    int cachedSplitOffset = splitBufferOffset;
                    int cachedSplitCount = ComputeSpotShadowCullingSplits(cachedSpotVisibleLightsAndIndices, cachedSplitOffset);
                    splitBufferOffset += cachedSplitCount;

                    dynamicSpotHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + dynamicSplitOffset, dynamicSplitCount);
                    cachedSpotHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + cachedSplitOffset, cachedSplitCount);
                }

                UnsafeList<HDShadowCullingSplit> dynamicAreaRectangleHDSplits;
                UnsafeList<HDShadowCullingSplit> cachedAreaRectangleHDSplits;

                using (computeAreaRectangleShadowCullingInfosMarker.Auto())
                {
                    int dynamicSplitOffset = splitBufferOffset;
                    int dynamicSplitCount = ComputeAreaRectangleShadowCullingSplits(dynamicAreaRectangleVisibleLightsAndIndices, dynamicSplitOffset);
                    splitBufferOffset += dynamicSplitCount;

                    int cachedSplitOffset = splitBufferOffset;
                    int cachedSplitCount = ComputeAreaRectangleShadowCullingSplits(cachedAreaRectangleVisibleLightsAndIndices, cachedSplitOffset);
                    splitBufferOffset += cachedSplitCount;

                    dynamicAreaRectangleHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + dynamicSplitOffset, dynamicSplitCount);
                    cachedAreaRectangleHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + cachedSplitOffset, cachedSplitCount);
                }

                inOutSplitBufferOffset[0] = splitBufferOffset;

                *outDynamicPointVisibleLightsAndIndices = dynamicPointVisibleLightsAndIndices;
                *outCachedPointVisibleLightsAndIndices = cachedPointVisibleLightsAndIndices;
                *outDynamicSpotVisibleLightsAndIndices = dynamicSpotVisibleLightsAndIndices;
                *outCachedSpotVisibleLightsAndIndices = cachedSpotVisibleLightsAndIndices;
                *outDynamicAreaRectangleVisibleLightsAndIndices = dynamicAreaRectangleVisibleLightsAndIndices;
                *outCachedAreaRectangleVisibleLightsAndIndices = cachedAreaRectangleVisibleLightsAndIndices;
                *outDynamicDirectionalVisibleLightsAndIndices = dynamicDirectionalVisibleLightsAndIndices;
                *outCachedDirectionalVisibleLightsAndIndices = cachedDirectionalVisibleLightsAndIndices;

                *outDynamicPointHDSplits = dynamicPointHDSplits;
                *outCachedPointHDSplits = cachedPointHDSplits;
                *outDynamicSpotHDSplits = dynamicSpotHDSplits;
                *outCachedSpotHDSplits = cachedSpotHDSplits;
                *outDynamicAreaRectangleHDSplits = dynamicAreaRectangleHDSplits;
                *outCachedAreaRectangleHDSplits = cachedAreaRectangleHDSplits;
            }

            int ComputeSpotShadowCullingSplits(UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesDatas, int initialSplitBufferOffset)
            {
                if (visibleLightsAndIndicesDatas.Length == 0)
                    return 0;

                int lightSplitBufferOffset = initialSplitBufferOffset;

                for (int i = 0; i < visibleLightsAndIndicesDatas.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData visibleLightsAndIndicesData = ref visibleLightsAndIndicesDatas.ElementAt(i);
                    ref VisibleLight visibleLight = ref visibleLightsAndIndicesData.visibleLight;
                    ref HDAdditionalLightDataUpdateInfo light = ref visibleLightsAndIndicesData.additionalLightUpdateInfo;
                    int lightIndex = visibleLightsAndIndicesData.lightIndex;
                    int shadowRequestIndicesBeginIndex = visibleLightsAndIndicesData.shadowRequestSetHandle.storageIndexForRequestIndices;
                    NativeArray<int> shadowRequestIndices = hdShadowRequestIndicesStorage.GetSubArray(shadowRequestIndicesBeginIndex, HDShadowRequest.maxLightShadowRequestsCount);

                    Debug.Assert(visibleLightsAndIndicesData.splitCount == 1);

                    bool skipCulling = true;
                    ShadowSplitData splitData = default;
                    HDShadowCullingSplit hdSplit = default;
                    hdSplit.splitIndex = 0;

                    if (visibleLightsAndIndicesData.isSplitValidMask[0])
                    {
                        int shadowRequestIndex = shadowRequestIndices[0];
                        Vector2 viewportSize = shadowResolutionRequestStorage[shadowRequestIndex].resolution;
                        float spotAngleForShadows = light.useCustomSpotLightShadowCone ? Math.Min(light.customSpotLightShadowCone, visibleLight.spotAngle) : visibleLight.spotAngle;

                        Matrix4x4 view;
                        Matrix4x4 deviceProjectionYFlip;
                        Matrix4x4 projection;
                        Matrix4x4 invViewProjection;
                        Vector4 deviceProjection;

                        HDShadowUtils.ExtractSpotLightData(spotAngleForShadows, light.shadowNearPlane, light.aspectRatio, light.shapeWidth,
                            light.shapeHeight, visibleLight, viewportSize, light.normalBias, punctualShadowFilteringQuality, usesReversedZBuffer,
                            out view, out invViewProjection, out projection,
                            out deviceProjection, out deviceProjectionYFlip,
                            out splitData);

                        hdSplit.view = view;
                        hdSplit.deviceProjectionMatrix = default;
                        hdSplit.deviceProjectionYFlip = deviceProjectionYFlip;
                        hdSplit.projection = projection;
                        hdSplit.invViewProjection = invViewProjection;
                        hdSplit.deviceProjection = deviceProjection;
                        hdSplit.cullingSphere = splitData.cullingSphere;
                        hdSplit.viewportSize = viewportSize;
                        hdSplit.forwardOffset = 0;

                        if (!visibleLightsAndIndicesData.HasShadowCacheUpToDate(0))
                            skipCulling = false;
                    }

                    outHDSplitBuffer[lightSplitBufferOffset + 0] = hdSplit;
                    outSplitBuffer[lightSplitBufferOffset + 0] = splitData;

                    uint splitExclusionMask = skipCulling ? 0b1u : 0;

                    outPerLightShadowCullingInfos[lightIndex] = new LightShadowCasterCullingInfo
                    {
                        splitRange = new RangeInt(lightSplitBufferOffset, 1),
                        projectionType = GetSpotLightCullingProjectionType(visibleLight.lightType),
                        splitExclusionMask = (ushort)splitExclusionMask,
                    };

                    lightSplitBufferOffset += 1;
                }

                return lightSplitBufferOffset - initialSplitBufferOffset;
            }

            int ComputePointShadowCullingSplits(UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesDatas, int initialSplitBufferOffset)
            {
                const int SplitCount = 6;

                if (visibleLightsAndIndicesDatas.Length == 0)
                    return 0;

                int lightSplitBufferOffset = initialSplitBufferOffset;

                for (int i = 0; i < visibleLightsAndIndicesDatas.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData visibleLightsAndIndicesData = ref visibleLightsAndIndicesDatas.ElementAt(i);
                    ref VisibleLight visibleLight = ref visibleLightsAndIndicesData.visibleLight;
                    ref HDAdditionalLightDataUpdateInfo light = ref visibleLightsAndIndicesData.additionalLightUpdateInfo;
                    int lightIndex = visibleLightsAndIndicesData.lightIndex;
                    int shadowRequestIndicesBeginIndex = visibleLightsAndIndicesData.shadowRequestSetHandle.storageIndexForRequestIndices;
                    NativeArray<int> shadowRequestIndices = hdShadowRequestIndicesStorage.GetSubArray(shadowRequestIndicesBeginIndex, HDShadowRequest.maxLightShadowRequestsCount);

                    Debug.Assert(visibleLightsAndIndicesData.splitCount == SplitCount);

                    uint splitMaskRequest = 0;

                    for (int splitIndex = 0; splitIndex < SplitCount; splitIndex++)
                    {
                        ShadowSplitData splitData = default;
                        HDShadowCullingSplit hdSplit = default;
                        hdSplit.splitIndex = splitIndex;

                        if (visibleLightsAndIndicesData.isSplitValidMask[(uint)splitIndex])
                        {
                            int shadowRequestIndex = shadowRequestIndices[splitIndex];
                            Vector2 viewportSize = shadowResolutionRequestStorage[shadowRequestIndex].resolution;

                            Matrix4x4 view;
                            Matrix4x4 deviceProjectionYFlip;
                            Matrix4x4 projection;
                            Matrix4x4 invViewProjection;
                            Vector4 deviceProjection;

                            HDShadowUtils.ExtractPointLightData(cubeMapFaces, visibleLight, viewportSize, light.shadowNearPlane,
                                light.normalBias, (uint)splitIndex, punctualShadowFilteringQuality, usesReversedZBuffer,
                                out view, out invViewProjection, out projection,
                                out deviceProjection, out deviceProjectionYFlip,
                                out splitData);

                            hdSplit.view = view;
                            hdSplit.deviceProjectionMatrix = default;
                            hdSplit.deviceProjectionYFlip = deviceProjectionYFlip;
                            hdSplit.projection = projection;
                            hdSplit.invViewProjection = invViewProjection;
                            hdSplit.deviceProjection = deviceProjection;
                            hdSplit.cullingSphere = splitData.cullingSphere;
                            hdSplit.viewportSize = viewportSize;
                            hdSplit.forwardOffset = 0;

                            if (!visibleLightsAndIndicesData.HasShadowCacheUpToDate(splitIndex))
                                splitMaskRequest |= 1u << splitIndex;
                        }

                        outHDSplitBuffer[lightSplitBufferOffset + splitIndex] = hdSplit;
                        outSplitBuffer[lightSplitBufferOffset + splitIndex] = splitData;
                    }

                    uint splitExclusionMask = ~splitMaskRequest & ((1u << SplitCount) - 1);

                    outPerLightShadowCullingInfos[lightIndex] = new LightShadowCasterCullingInfo
                    {
                        splitRange = new RangeInt(lightSplitBufferOffset, SplitCount),
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitExclusionMask = (ushort)splitExclusionMask,
                    };

                    lightSplitBufferOffset += SplitCount;
                }

                return lightSplitBufferOffset - initialSplitBufferOffset;
            }

            int ComputeAreaRectangleShadowCullingSplits(UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesDatas, int initialSplitBufferOffset)
            {
                if (visibleLightsAndIndicesDatas.Length == 0)
                    return 0;

                int lightSplitBufferOffset = initialSplitBufferOffset;

                for (int i = 0; i < visibleLightsAndIndicesDatas.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData visibleLightsAndIndicesData = ref visibleLightsAndIndicesDatas.ElementAt(i);
                    ref HDAdditionalLightDataUpdateInfo light = ref visibleLightsAndIndicesData.additionalLightUpdateInfo;
                    ref VisibleLight visibleLight = ref visibleLightsAndIndicesData.visibleLight;
                    int lightIndex = visibleLightsAndIndicesData.lightIndex;
                    int shadowRequestIndicesBeginIndex = visibleLightsAndIndicesData.shadowRequestSetHandle.storageIndexForRequestIndices;
                    NativeArray<int> shadowRequestIndices = hdShadowRequestIndicesStorage.GetSubArray(shadowRequestIndicesBeginIndex, HDShadowRequest.maxLightShadowRequestsCount);

                    Debug.Assert(visibleLightsAndIndicesData.splitCount == 1);
                    Debug.Assert(visibleLight.lightType == LightType.Rectangle);

                    bool skipCulling = true;
                    ShadowSplitData splitData = default;
                    HDShadowCullingSplit hdSplit = default;
                    hdSplit.splitIndex = 0;

                    if (visibleLightsAndIndicesData.isSplitValidMask[0])
                    {
                        int shadowRequestIndex = shadowRequestIndices[0];
                        Vector2 viewportSize = shadowResolutionRequestStorage[shadowRequestIndex].resolution;
                        Vector2 shapeSize = new Vector2(light.shapeWidth, light.shapeHeight);
                        float forwardOffset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, light.areaLightShadowCone);

                        Matrix4x4 view;
                        Matrix4x4 deviceProjectionYFlip;
                        Matrix4x4 projection;
                        Matrix4x4 invViewProjection;
                        Vector4 deviceProjection;

                        HDShadowUtils.ExtractRectangleAreaLightData(visibleLight, forwardOffset, light.areaLightShadowCone,
                            light.shadowNearPlane, shapeSize, viewportSize, light.normalBias, usesReversedZBuffer,
                            out view, out invViewProjection, out projection,
                            out deviceProjection, out deviceProjectionYFlip,
                            out splitData);

                        hdSplit.view = view;
                        hdSplit.deviceProjectionMatrix = default;
                        hdSplit.deviceProjectionYFlip = deviceProjectionYFlip;
                        hdSplit.projection = projection;
                        hdSplit.invViewProjection = invViewProjection;
                        hdSplit.deviceProjection = deviceProjection;
                        hdSplit.cullingSphere = splitData.cullingSphere;
                        hdSplit.viewportSize = viewportSize;
                        hdSplit.forwardOffset = forwardOffset;

                        if (!visibleLightsAndIndicesData.HasShadowCacheUpToDate(0))
                            skipCulling = false;
                    }

                    outHDSplitBuffer[lightSplitBufferOffset + 0] = hdSplit;
                    outSplitBuffer[lightSplitBufferOffset + 0] = splitData;

                    uint splitExclusionMask = skipCulling ? 0b1u : 0;

                    outPerLightShadowCullingInfos[lightIndex] = new LightShadowCasterCullingInfo
                    {
                        splitRange = new RangeInt(lightSplitBufferOffset, 1),
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitExclusionMask = (ushort)splitExclusionMask,
                    };

                    lightSplitBufferOffset += 1;
                }

                return lightSplitBufferOffset - initialSplitBufferOffset;
            }

            [BurstDiscard]
            public void ProcessDirectionalLights(CullingResults cullingResults,
                UnsafeList<ShadowIndicesAndVisibleLightData> dynamicVisibleLightsAndIndicesDatas,
                UnsafeList<ShadowIndicesAndVisibleLightData> cachedVisibleLightsAndIndicesDatas)
            {
                int splitBufferOffset = inOutSplitBufferOffset[0];
                HDShadowCullingSplit* hdSplitBufferPtr = (HDShadowCullingSplit*)outHDSplitBuffer.GetUnsafePtr();

                UnsafeList<HDShadowCullingSplit> dynamicDirectionalHDSplits;
                UnsafeList<HDShadowCullingSplit> cachedDirectionalHDSplits;

                using (computeDirectionalShadowCullingInfosMarker.Auto())
                {
                    int dynamicSplitOffset = splitBufferOffset;
                    int dynamicSplitCount = ComputeDirectionalShadowCullingSplits(cullingResults, dynamicVisibleLightsAndIndicesDatas, dynamicSplitOffset);
                    splitBufferOffset += dynamicSplitCount;

                    int cachedSplitOffset = splitBufferOffset;
                    int cachedSplitCount = ComputeDirectionalShadowCullingSplits(cullingResults, cachedVisibleLightsAndIndicesDatas, cachedSplitOffset);
                    splitBufferOffset += cachedSplitCount;

                    dynamicDirectionalHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + dynamicSplitOffset, dynamicSplitCount);
                    cachedDirectionalHDSplits = new UnsafeList<HDShadowCullingSplit>(hdSplitBufferPtr + cachedSplitOffset, cachedSplitCount);
                }

                inOutSplitBufferOffset[0] = splitBufferOffset;
                *outDynamicDirectionalHDSplits = dynamicDirectionalHDSplits;
                *outCachedDirectionalHDSplits = cachedDirectionalHDSplits;
            }

            [BurstDiscard]
            int ComputeDirectionalShadowCullingSplits(CullingResults cullingResults, UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesDatas, int initialSplitBufferOffset)
            {
                if (visibleLightsAndIndicesDatas.Length == 0)
                    return 0;

                int lightSplitBufferOffset = initialSplitBufferOffset;

                for (int i = 0; i < visibleLightsAndIndicesDatas.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData visibleLightsAndIndicesData = ref visibleLightsAndIndicesDatas.ElementAt(i);
                    ref HDAdditionalLightDataUpdateInfo light = ref visibleLightsAndIndicesData.additionalLightUpdateInfo;
                    int splitCount = visibleLightsAndIndicesData.splitCount;
                    int lightIndex = visibleLightsAndIndicesData.lightIndex;
                    int shadowRequestIndicesBeginIndex = visibleLightsAndIndicesData.shadowRequestSetHandle.storageIndexForRequestIndices;
                    NativeArray<int> shadowRequestIndices = hdShadowRequestIndicesStorage.GetSubArray(shadowRequestIndicesBeginIndex, splitCount);

                    uint splitMaskRequest = 0;

                    for (int splitIndex = 0; splitIndex < splitCount; splitIndex++)
                    {
                        ShadowSplitData splitData = default;
                        HDShadowCullingSplit hdSplit = default;
                        hdSplit.splitIndex = splitIndex;

                        if (visibleLightsAndIndicesData.isSplitValidMask[(uint)splitIndex])
                        {
                            int shadowRequestIndex = shadowRequestIndices[splitIndex];
                            Vector2 viewportSize = shadowResolutionRequestStorage[shadowRequestIndex].resolution;

                            Matrix4x4 view;
                            Matrix4x4 deviceProjectionYFlip;
                            Matrix4x4 deviceProjectionMatrix;
                            Matrix4x4 projection;
                            Matrix4x4 invViewProjection;
                            Vector4 deviceProjection;

                            HDShadowUtils.ExtractDirectionalLightData(viewportSize, (uint)splitIndex, cascadeShadowSplitCount,
                                cascadeShadowSplits, shadowNearPlaneOffset, cullingResults, lightIndex,
                                out view, out invViewProjection, out projection,
                                out deviceProjectionMatrix, out deviceProjection, out deviceProjectionYFlip, out splitData);

                            hdSplit.view = view;
                            hdSplit.deviceProjectionMatrix = deviceProjectionMatrix;
                            hdSplit.deviceProjectionYFlip = deviceProjectionYFlip;
                            hdSplit.projection = projection;
                            hdSplit.invViewProjection = invViewProjection;
                            hdSplit.deviceProjection = deviceProjection;
                            hdSplit.cullingSphere = splitData.cullingSphere;
                            hdSplit.viewportSize = viewportSize;
                            hdSplit.forwardOffset = 0;

                            if (!visibleLightsAndIndicesData.HasShadowCacheUpToDate(splitIndex))
                                splitMaskRequest |= 1u << splitIndex;
                        }

                        outHDSplitBuffer[lightSplitBufferOffset + splitIndex] = hdSplit;
                        outSplitBuffer[lightSplitBufferOffset + splitIndex] = splitData;
                    }

                    uint splitExclusionMask = ~splitMaskRequest & ((1u << splitCount) - 1);

                    outPerLightShadowCullingInfos[lightIndex] = new LightShadowCasterCullingInfo
                    {
                        splitRange = new RangeInt(lightSplitBufferOffset, splitCount),
                        projectionType = BatchCullingProjectionType.Orthographic,
                        splitExclusionMask = (ushort)splitExclusionMask,
                    };

                    lightSplitBufferOffset += splitCount;
                }

                return lightSplitBufferOffset - initialSplitBufferOffset;
            }

            BitArray8 ComputeNeedCacheUpdateMask(ref HDAdditionalLightDataUpdateInfo lightUpdateInfo,
                ref VisibleLight visibleLight,
                int splitCount,
                ref float3 newCachedDirectionalAngles)
            {
                BitArray8 needCacheUpdateMask = new BitArray8(0);

                if (lightUpdateInfo.hasCachedComponent)
                {
                    float3 lightEulerAngles = visibleLight.localToWorldMatrix.rotation.eulerAngles;
                    int lightIdxForCachedShadows = lightUpdateInfo.lightIdxForCachedShadows;
                    bool shadowHasAtlasPlacement = lightIdxForCachedShadows != -1;

                    bool isDirectional = visibleLight.lightType == LightType.Directional;
                    bool isSpot = visibleLight.lightType == LightType.Spot || visibleLight.lightType == LightType.Pyramid || visibleLight.lightType == LightType.Box;
                    bool isPoint = visibleLight.lightType == LightType.Point;
                    bool isArea = visibleLight.lightType == LightType.Rectangle;

                    if (isDirectional)
                    {
                        bool needsRenderingDueToTransformChange = false;
                        if (lightUpdateInfo.updateUponLightMovement)
                        {
                            float angleDiffThreshold = lightUpdateInfo.cachedShadowAngleUpdateThreshold;
                            float3 angleDiff = newCachedDirectionalAngles - lightEulerAngles;
                            // Any angle difference
                            if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                            {
                                newCachedDirectionalAngles = lightEulerAngles;
                                needsRenderingDueToTransformChange = true;
                            }
                        }

                        BitArray8 directionalShadowPendingUpdate = shadowManager.cachedShadowManager.directionalShadowPendingUpdate;

                        for (int i = 0; i < splitCount; i++)
                        {
                            bool directionalShadowIdxPendingUpdate = directionalShadowPendingUpdate[(uint)(lightIdxForCachedShadows + i)];
                            bool needToUpdateCachedContent = shadowHasAtlasPlacement && (needsRenderingDueToTransformChange || directionalShadowIdxPendingUpdate);

                            needCacheUpdateMask[(uint)i] = needToUpdateCachedContent;
                        }
                    }
                    else if (isSpot || isPoint || isArea)
                    {
                        HDCachedShadowAtlasDataForShadowRequestUpdateJob cachedShadowAtlas = default;
                        if (isSpot || isPoint)
                        {
                            cachedShadowAtlas = shadowManager.cachedShadowManager.punctualShadowAtlas;
                        }
                        else if (isArea)
                        {
                            cachedShadowAtlas = shadowManager.cachedShadowManager.areaShadowAtlas;
                        }

                        bool needsRenderingDueToTransformChange = false;
                        if (lightUpdateInfo.updateUponLightMovement)
                        {
                            if (cachedShadowAtlas.transformCaches.TryGetValue(lightUpdateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
                            {
                                float positionThreshold = lightUpdateInfo.cachedShadowTranslationUpdateThreshold;
                                float3 positionDiffVec = cachedTransform.position - visibleLight.GetPosition();
                                float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                                if (positionDiff > positionThreshold * positionThreshold)
                                    needsRenderingDueToTransformChange = true;

                                float angleDiffThreshold = lightUpdateInfo.cachedShadowAngleUpdateThreshold;
                                float3 cachedAngles = cachedTransform.angles;
                                float3 angleDiff = cachedAngles - lightEulerAngles;
                                // Any angle difference
                                if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                                {
                                    needsRenderingDueToTransformChange = true;
                                }

                                if (needsRenderingDueToTransformChange)
                                {
                                    // Update the record
                                    cachedTransform.position = visibleLight.GetPosition();
                                    cachedTransform.angles = lightEulerAngles;
                                    cachedShadowAtlas.transformCaches[lightUpdateInfo.lightIdxForCachedShadows] = cachedTransform;
                                }
                            }
                        }

                        for (int i = 0; i < splitCount; i++)
                        {
                            bool needToUpdateCachedContent = false;

                            if (shadowHasAtlasPlacement)
                            {
                                int cachedShadowID = lightUpdateInfo.lightIdxForCachedShadows + i;

                                bool shadowsPendingRenderingContainedShadowID = cachedShadowAtlas.shadowsPendingRendering.Remove(cachedShadowID);
                                needToUpdateCachedContent = needsRenderingDueToTransformChange || shadowsPendingRenderingContainedShadowID;

                                if (shadowsPendingRenderingContainedShadowID)
                                {
                                    // Handshake with the cached shadow manager to notify about the rendering.
                                    // Technically the rendering has not happened yet, but it is scheduled.
                                    cachedShadowAtlas.shadowsWithValidData.TryAdd(cachedShadowID, cachedShadowID);
                                }
                            }

                            needCacheUpdateMask[(uint)i] = needToUpdateCachedContent;
                        }
                    }
                }

                return needCacheUpdateMask;
            }
        }

        static BatchCullingProjectionType GetSpotLightCullingProjectionType(LightType lightType)
        {
            if (lightType == LightType.Box)
            {
                return BatchCullingProjectionType.Orthographic;
            }
            else if (lightType == LightType.Spot || lightType == LightType.Pyramid)
            {
                return BatchCullingProjectionType.Perspective;
            }

            return BatchCullingProjectionType.Unknown;
        }

        static Vector3 GetCascadeRatiosAsVector3(float[] cascadeRatios)
        {
            Vector3 vec3 = Vector3.zero;
            for (int i = 0; i < math.min(cascadeRatios.Length, 3); i++)
            {
                vec3[i] = cascadeRatios[i];
            }
            return vec3;
        }
    }
}
