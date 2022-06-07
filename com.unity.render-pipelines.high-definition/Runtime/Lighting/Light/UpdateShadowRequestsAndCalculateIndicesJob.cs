using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Rendering.HighDefinition
{
    [BurstCompile] [NoAlias]
    internal unsafe struct UpdateShadowRequestsAndCalculateIndicesJob : IJob
    {
        public HDShadowManagerUnmanaged shadowManager;

        public NativeBitArray isValidIndex;
        [ReadOnly] public NativeArray<uint> sortKeys;
        [ReadOnly] public NativeArray<int> visibleLightEntityDataIndices;
        [ReadOnly] public NativeArray<HDProcessedVisibleLight> processedEntities;
        [ReadOnly] public NativeArray<VisibleLight> visibleLights;
        [ReadOnly] public NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles;
        [ReadOnly] public NativeList<int> requestIndicesStorage;
        [ReadOnly] public NativeArray<Matrix4x4> kCubemapFaces;

        public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
        public NativeList<HDShadowRequest> requestStorage;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedAreaOtherUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedDirectionalUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicAreaOtherUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicDirectionalUpdateInfos;
        public NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage;
        public NativeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesBuffer; // sized to lightCounts
        public NativeList<ShadowIndicesAndVisibleLightData> splitVisibleLightsAndIndicesBuffer; // sized to lightCounts
        public NativeList<float4> frustumPlanesStorage;
        public NativeList<Vector3> cachedViewPositionsStorage;

        [WriteOnly] public NativeArray<int> shadowIndices;

#if UNITY_EDITOR
        [WriteOnly] public NativeArray<int> shadowRequestCounts;
#endif

        [ReadOnly] public int lightCounts;
        [ReadOnly] public int shadowSettingsCascadeShadowSplitCount;
        [ReadOnly] public int invalidIndex;

        [ReadOnly] public Vector3 worldSpaceCameraPos;
        [ReadOnly] public int shaderConfigCameraRelativeRendering;
        [ReadOnly] public HDShadowFilteringQuality shadowFilteringQuality;
        [ReadOnly] public bool usesReversedZBuffer;

        public ProfilerMarker validIndexCalculationsMarker;
        public ProfilerMarker indicesAndPreambleMarker;
        public ProfilerMarker cachedRequestsMarker;
        public ProfilerMarker cachedDirectionalRequestsMarker;
        public ProfilerMarker dynamicDirectionalRequestsMarker;
        public ProfilerMarker dynamicPointRequestsMarker;
        public ProfilerMarker dynamicSpotRequestsMarker;
        public ProfilerMarker dynamicAreaRectangleRequestsMarker;
        public ProfilerMarker dynamicAreaOtherRequestsMarker;
        public ProfilerMarker cachedPointRequestsMarker;
        public ProfilerMarker cachedSpotRequestsMarker;
        public ProfilerMarker cachedAreaRectangleRequestsMarker;
        public ProfilerMarker cachedAreaOtherRequestsMarker;

        public void Execute()
        {
            using (validIndexCalculationsMarker.Auto())
            {
                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
                {
                    uint sortKey = sortKeys[sortKeyIndex];
                    int lightIndex = (int)(sortKey & 0xFFFF);

                    int dataIndex = visibleLightEntityDataIndices[lightIndex];
                    isValidIndex.Set(sortKeyIndex, dataIndex != invalidIndex);
                }
            }

            visibleLightsAndIndicesBuffer.Length = 0;
            splitVisibleLightsAndIndicesBuffer.Length = 0;
            HDAdditionalLightDataUpdateInfo* updateInfosUnsafePtr = (HDAdditionalLightDataUpdateInfo*)additionalLightDataUpdateInfos.GetUnsafePtr();

            int shadowManagerRequestCount = shadowManager.fields[0].m_ShadowRequestCount;

            int cachedAreaRectangleCount = 0;
            int cachedAreaOtherCount = 0;
            int cachedPointCount = 0;
            int cachedSpotCount = 0;
            int cachedDirectionalCount = 0;
            int dynamicAreaRectangleCount = 0;
            int dynamicAreaOtherCount = 0;
            int dynamicPointCount = 0;
            int dynamicSpotCount = 0;
            int dynamicDirectionalCount = 0;

            int bufferCount = 0;
            using (indicesAndPreambleMarker.Auto())
            {
                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
                {
                    if (!isValidIndex.IsSet(sortKeyIndex))
                        continue;

                    uint sortKey = sortKeys[sortKeyIndex];
                    int lightIndex = (int)(sortKey & 0xFFFF);
                    int dataIndex = visibleLightEntityDataIndices[lightIndex];

                    ref readonly HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + dataIndex);
                    HDLightType lightType = HDAdditionalLightData.TranslateLightType(visibleLights[lightIndex].lightType, lightUpdateInfo.pointLightHDType);
                    int firstShadowRequestIndex = -1;
                    int shadowRequestCount = -1;

                    if ( /*lightComponent != null && */(processedEntities[lightIndex].shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) != 0)
                    {
                        shadowRequestCount = 0;
                        int count = HDAdditionalLightData.GetShadowRequestCountForLightType(shadowSettingsCascadeShadowSplitCount, lightType);
                        HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[dataIndex];

                        visibleLightsAndIndicesBuffer.Length++;
                        ref var bufferElement = ref visibleLightsAndIndicesBuffer.ElementAt(bufferCount);
                        bufferCount++;

                        bufferElement.additionalLightUpdateInfo = lightUpdateInfo;
                        bufferElement.visibleLight = visibleLights[lightIndex];
                        bufferElement.dataIndex = dataIndex;
                        bufferElement.lightIndex = lightIndex;
                        bufferElement.shadowRequestSetHandle = shadowRequestSetHandle;
                        bufferElement.lightType = lightType;

                        bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(lightUpdateInfo.shadowUpdateMode);

                        switch (lightType)
                        {
                            case HDLightType.Spot:
                                if (hasCachedComponent)
                                    ++cachedSpotCount;
                                else
                                    ++dynamicSpotCount;
                                break;
                            case HDLightType.Directional:
                                if (hasCachedComponent)
                                    ++cachedDirectionalCount;
                                else
                                    ++dynamicDirectionalCount;
                                break;
                            case HDLightType.Point:
                                if (hasCachedComponent)
                                    ++cachedPointCount;
                                else
                                    ++dynamicPointCount;
                                break;
                            case HDLightType.Area:
                                if (lightUpdateInfo.areaLightShape == AreaLightShape.Rectangle)
                                {
                                    if (hasCachedComponent)
                                        ++cachedAreaRectangleCount;
                                    else
                                        ++dynamicAreaRectangleCount;
                                }
                                else
                                {
                                    if (hasCachedComponent)
                                        ++cachedAreaOtherCount;
                                    else
                                        ++dynamicAreaOtherCount;
                                }
                                break;
                        }

                        splitVisibleLightsAndIndicesBuffer.Length = visibleLightsAndIndicesBuffer.Length;

                        for (int index = 0; index < count; index++)
                        {
                            HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];

                            int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                            HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);

                            if (!resolutionRequestHandle.valid)
                                continue;

                            bufferElement.shadowRequestIndices[shadowRequestCount] = shadowRequestIndex;
                            bufferElement.shadowResolutionRequestIndices[shadowRequestCount] = resolutionRequestHandle.index;

                            shadowManager.WriteShadowRequestIndex(shadowRequestIndex, shadowManagerRequestCount, indexHandle);
                            // Store the first shadow request id to return it
                            if (firstShadowRequestIndex == -1)
                                firstShadowRequestIndex = shadowRequestIndex;
                            shadowRequestCount++;
                        }

                        bufferElement.shadowRequestCount = shadowRequestCount;
                    }
#if UNITY_EDITOR
                    shadowRequestCounts[sortKeyIndex] = shadowRequestCount;
#endif
                    shadowIndices[sortKeyIndex] = firstShadowRequestIndex;
                }
            }


            int bufferedDataCount = visibleLightsAndIndicesBuffer.Length;

            ShadowIndicesAndVisibleLightData* splitVisibleLightsAndIndicesBufferPtr = (ShadowIndicesAndVisibleLightData*)splitVisibleLightsAndIndicesBuffer.GetUnsafePtr();
            int buffereOffsetIterator = 0;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            cachedDirectionalVisibleLightsAndIndices.m_capacity = cachedDirectionalCount;
            buffereOffsetIterator += cachedDirectionalCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            cachedAreaRectangleVisibleLightsAndIndices.m_capacity = cachedAreaRectangleCount;
            buffereOffsetIterator += cachedAreaRectangleCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaOtherVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            cachedAreaOtherVisibleLightsAndIndices.m_capacity = cachedAreaOtherCount;
            buffereOffsetIterator += cachedAreaOtherCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            cachedPointVisibleLightsAndIndices.m_capacity = cachedPointCount;
            buffereOffsetIterator += cachedPointCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            cachedSpotVisibleLightsAndIndices.m_capacity = cachedSpotCount;
            buffereOffsetIterator += cachedSpotCount;

            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            dynamicAreaRectangleVisibleLightsAndIndices.m_capacity = dynamicAreaRectangleCount;
            buffereOffsetIterator += dynamicAreaRectangleCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaOtherVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            dynamicAreaOtherVisibleLightsAndIndices.m_capacity = dynamicAreaOtherCount;
            buffereOffsetIterator += dynamicAreaOtherCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            dynamicPointVisibleLightsAndIndices.m_capacity = dynamicPointCount;
            buffereOffsetIterator += dynamicPointCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            dynamicSpotVisibleLightsAndIndices.m_capacity = dynamicSpotCount;
            buffereOffsetIterator += dynamicSpotCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
            dynamicDirectionalVisibleLightsAndIndices.m_capacity = dynamicDirectionalCount;
            buffereOffsetIterator += dynamicDirectionalCount;

            for (int bufferedDataIndex = 0; bufferedDataIndex < bufferedDataCount; bufferedDataIndex++)
            {
                ref readonly ShadowIndicesAndVisibleLightData readData = ref visibleLightsAndIndicesBuffer.ElementAt(bufferedDataIndex);
                ref readonly HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref readData.additionalLightUpdateInfo;
                HDLightType lightType = readData.lightType;
                bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(lightUpdateInfo.shadowUpdateMode);

                switch (lightType)
                {
                    case HDLightType.Spot:
                        if (hasCachedComponent)
                        {
                            // if (cachedSpotVisibleLightsAndIndices.Length >= cachedSpotCount)
                            // {
                            //     throw new Exception($"Cached spot count beyond limit of {cachedSpotCount}");
                            // }
                            cachedSpotVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            // if (dynamicSpotVisibleLightsAndIndices.Length >= dynamicSpotCount)
                            // {
                            //     throw new Exception($"Dynamic spot count beyond limit of {dynamicSpotCount}");
                            // }
                            dynamicSpotVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Directional:
                        if (hasCachedComponent)
                        {
                            // if (cachedDirectionalVisibleLightsAndIndices.Length >= cachedDirectionalCount)
                            // {
                            //     throw new Exception($"Cached directional count beyond limit of {cachedDirectionalCount}");
                            // }
                            cachedDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            // if (dynamicDirectionalVisibleLightsAndIndices.Length >= dynamicDirectionalCount)
                            // {
                            //     throw new Exception($"Dynamic directional count beyond limit of {dynamicDirectionalCount}");
                            // }
                            dynamicDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Point:
                        if (hasCachedComponent)
                        {
                            // if (cachedPointVisibleLightsAndIndices.Length >= cachedPointCount)
                            // {
                            //     throw new Exception($"Cached point count beyond limit of {cachedPointCount}");
                            // }
                            cachedPointVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            // if (dynamicPointVisibleLightsAndIndices.Length >= dynamicPointCount)
                            // {
                            //     throw new Exception($"Dynamic point count beyond limit of {dynamicPointCount}");
                            // }
                            dynamicPointVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Area:
                        if (lightUpdateInfo.areaLightShape == AreaLightShape.Rectangle)
                        {
                            if (hasCachedComponent)
                            {
                                // if (cachedAreaRectangleVisibleLightsAndIndices.Length >= cachedAreaRectangleCount)
                                // {
                                //     throw new Exception($"Cachced area rectangle count beyond limit of {cachedAreaRectangleCount}");
                                // }
                                cachedAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                            }
                            else
                            {
                                // if (dynamicAreaRectangleVisibleLightsAndIndices.Length >= dynamicAreaRectangleCount)
                                // {
                                //     throw new Exception($"Dynamic area rectangle count beyond limit of {dynamicAreaRectangleCount}");
                                // }
                                dynamicAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                            }
                        }
                        else
                        {
                            if (hasCachedComponent)
                            {
                                // if (cachedAreaOtherVisibleLightsAndIndices.Length >= cachedAreaOtherCount)
                                // {
                                //     throw new Exception($"Cached area other count beyond limit of {cachedAreaOtherCount}");
                                // }
                                cachedAreaOtherVisibleLightsAndIndices.AddNoResize(readData);
                            }
                            else
                            {
                                // if (dynamicAreaOtherVisibleLightsAndIndices.Length >= dynamicAreaOtherCount)
                                // {
                                //     throw new Exception($"Dynamic area other count beyond limit of {dynamicAreaOtherCount}");
                                // }
                                dynamicAreaOtherVisibleLightsAndIndices.AddNoResize(readData);
                            }
                        }
                        break;
                }
            }

            using (cachedDirectionalRequestsMarker.Auto())
            {
                for (int i = 0; i < cachedDirectionalCount; i++)
                {
                    ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref cachedDirectionalVisibleLightsAndIndices.ElementAt(i);

                    HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[shadowIndicesAndVisibleLightData.dataIndex];
                    ShadowMapUpdateType cachedDirectionalUpdateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                    {
                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                        HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);

                        ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);

                        shadowRequest.isInCachedAtlas = (cachedDirectionalUpdateType == ShadowMapUpdateType.Cached);;
                        shadowRequest.isMixedCached = cachedDirectionalUpdateType == ShadowMapUpdateType.Mixed;
                        shadowRequest.shouldUseCachedShadowData = false;
                        shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
                        shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                        shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                        int updateDataListIndex = cachedDirectionalUpdateInfos.Length;
                        cachedDirectionalUpdateInfos.Length = updateDataListIndex + 1;
                        ref ShadowRequestIntermediateUpdateData updateInfo = ref cachedDirectionalUpdateInfos.ElementAt(updateDataListIndex);
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = true;
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                        updateInfo.updateType = cachedDirectionalUpdateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;
                        if (shadowRequestIndex < shadowManagerRequestCount)
                        {
                            shadowManager.cascadeShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                        }
                    }
                }
            }


            using (dynamicDirectionalRequestsMarker.Auto())
            {
                for (int i = 0; i < dynamicDirectionalCount; i++)
                {
                    ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref dynamicDirectionalVisibleLightsAndIndices.ElementAt(i);

                    HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[shadowIndicesAndVisibleLightData.dataIndex];
                    int count = HDAdditionalLightData.GetShadowRequestCountForLightType(shadowSettingsCascadeShadowSplitCount, HDLightType.Directional);
                    var updateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode);
                    bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                    // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows

                    for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                    {
                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                        HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);
                        if (!resolutionRequestHandle.valid)
                            continue;
                        ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);

                        shadowRequest.isInCachedAtlas = isSampledFromCache;
                        shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
                        shadowRequest.shouldUseCachedShadowData = false;
                        shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
                        shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                        shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                        int updateDataListIndex = dynamicDirectionalUpdateInfos.Length;
                        dynamicDirectionalUpdateInfos.Length = updateDataListIndex + 1;
                        ref ShadowRequestIntermediateUpdateData updateInfo = ref dynamicDirectionalUpdateInfos.ElementAt(updateDataListIndex);
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = hasCachedComponent;
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                        updateInfo.updateType = updateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;
                        if (shadowRequestIndex < shadowManagerRequestCount)
                        {
                            shadowManager.cascadeShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                        }
                    }
                }
            }

            // Update cached area rectangle:
            using (cachedAreaRectangleRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(ShadowMapType.AreaLightAtlas, HDLightType.Area,
                    cachedAreaRectangleVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedAreaRectangleUpdateInfos,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowRequests, shadowManager.areaShadowAtlas.shadowRequests,
                    shadowManager.areaShadowAtlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.areaShadowAtlas.transformCaches, shadowManager.cachedShadowManager.areaShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.areaShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.areaShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.areaShadowAtlas.placedShadows, cachedAreaRectangleCount, shadowManagerRequestCount);
                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaRectangleUpdateInfos);
            }

            // Update cached area other:
            using (cachedAreaOtherRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Area,
                    cachedAreaOtherVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedAreaOtherUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.areaShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedAreaOtherCount, shadowManagerRequestCount);
                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaOtherUpdateInfos);
            }

            // Update cached point:
            using (cachedPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Point,
                    cachedPointVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedPointUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.punctualShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedPointCount, shadowManagerRequestCount);
                UpdateCachedPointShadowRequestsAndResolutionRequests();
            }

            // Update cached spot:
            using (cachedSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Spot,
                    cachedSpotVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedSpotUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.punctualShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedSpotCount, shadowManagerRequestCount);
                UpdateCachedSpotShadowRequestsAndResolutionRequests();
            }

            // Update dynamic area rectangle:
            using (dynamicAreaRectangleRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(ShadowMapType.AreaLightAtlas, HDLightType.Area,
                    dynamicAreaRectangleVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicAreaRectangleUpdateInfos,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowRequests, shadowManager.areaShadowAtlas.shadowRequests,
                    shadowManager.areaShadowAtlas.mixedRequestsPendingBlits, dynamicAreaRectangleCount, shadowManagerRequestCount);
                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaRectangleUpdateInfos);
            }

            // Update dynamic area other:
            using (dynamicAreaOtherRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Area,
                    dynamicAreaOtherVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicAreaOtherUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicAreaOtherCount, shadowManagerRequestCount);
                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaOtherUpdateInfos);
            }


            // Update dynamic point:
            using (dynamicPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Point,
                    dynamicPointVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicPointUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicPointCount, shadowManagerRequestCount);
                UpdateDynamicPointShadowRequestsAndResolutionRequests();
            }


            // Update dynamic point:
            using (dynamicSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Spot,
                    dynamicSpotVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicSpotUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicSpotCount, shadowManagerRequestCount);
                UpdateDynamicSpotShadowRequestsAndResolutionRequests();
            }
        }

        public void UpdateCachedPointShadowRequestsAndResolutionRequests()
        {
            int pointCount = cachedPointUpdateInfos.Length;

            for (int i = 0; i < pointCount; i++)
            {
                ref readonly ShadowRequestIntermediateUpdateData pointUpdateInfo = ref cachedPointUpdateInfos.ElementAt(i);
                bool needToUpdateCachedContent = pointUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                bool hasCachedComponent = pointUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent];
                HDShadowRequestHandle shadowRequestHandle = pointUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = pointUpdateInfo.additionalLightDataIndex;
                int lightIndex = pointUpdateInfo.lightIndex;
                Vector2 viewportSize = pointUpdateInfo.viewportSize;
                ShadowMapUpdateType updateType = pointUpdateInfo.updateType;

                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    HDShadowUtils.ExtractPointLightData(kCubemapFaces,
                        pointUpdateInfo.visibleLight, viewportSize, updateInfo.shadowNearPlane,
                        updateInfo.normalBias, (uint) shadowRequestHandle.offset, shadowFilteringQuality, usesReversedZBuffer, out shadowRequest.view,
                        out Matrix4x4 invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettingsPoint(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, HDLightType.Point, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

                    hasUpdatedRequestData = true;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shouldRenderCachedComponent = true;
                }
                else if (hasCachedComponent)
                {
                    shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
                    shadowRequest.shouldUseCachedShadowData = true;
                    shadowRequest.shouldRenderCachedComponent = false;
                }

                if (needToUpdateDynamicContent && !hasUpdatedRequestData)
                {
                    shadowRequest.shouldUseCachedShadowData = false;

                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    HDShadowUtils.ExtractPointLightData(kCubemapFaces,
                        pointUpdateInfo.visibleLight, viewportSize, updateInfo.shadowNearPlane,
                        updateInfo.normalBias, (uint) shadowRequestHandle.offset, shadowFilteringQuality, usesReversedZBuffer,out shadowRequest.view,
                        out Matrix4x4 invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettingsPoint(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, HDLightType.Point, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicPointShadowRequestsAndResolutionRequests()
        {
            int pointCount = dynamicPointUpdateInfos.Length;

            for (int i = 0; i < pointCount; i++)
            {
                ref readonly ShadowRequestIntermediateUpdateData pointUpdateInfo = ref dynamicPointUpdateInfos.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = pointUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = pointUpdateInfo.additionalLightDataIndex;
                int lightIndex = pointUpdateInfo.lightIndex;
                Vector2 viewportSize = pointUpdateInfo.viewportSize;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;

                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                // Write per light type matrices, splitDatas and culling parameters
                HDShadowUtils.ExtractPointLightData(kCubemapFaces,
                    pointUpdateInfo.visibleLight, viewportSize, updateInfo.shadowNearPlane,
                    updateInfo.normalBias, (uint) shadowRequestHandle.offset, shadowFilteringQuality, usesReversedZBuffer,out shadowRequest.view,
                    out Matrix4x4 invViewProjection, out shadowRequest.projection,
                    out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                    out shadowRequest.splitData
                );

                // Assign all setting common to every lights
                HDAdditionalLightData.SetCommonShadowRequestSettingsPoint(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                    lightIndex, HDLightType.Point, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateCachedSpotShadowRequestsAndResolutionRequests()
        {
            int spotCount = cachedSpotUpdateInfos.Length;

            for (int i = 0; i < spotCount; i++)
            {
                ref readonly ShadowRequestIntermediateUpdateData spotUpdateInfo = ref cachedSpotUpdateInfos.ElementAt(i);
                bool needToUpdateCachedContent = spotUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                int lightIndex = spotUpdateInfo.lightIndex;
                Vector2 viewportSize = spotUpdateInfo.viewportSize;
                ShadowMapUpdateType updateType = spotUpdateInfo.updateType;

                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, spotUpdateInfo.visibleLight.spotAngle) : spotUpdateInfo.visibleLight.spotAngle;
                    HDShadowUtils.ExtractSpotLightData(
                        updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
                        updateInfo.shapeHeight, spotUpdateInfo.visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                        out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, HDLightType.Spot, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

                    hasUpdatedRequestData = true;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shouldRenderCachedComponent = true;
                }
                else
                {
                    shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
                    shadowRequest.shouldUseCachedShadowData = true;
                    shadowRequest.shouldRenderCachedComponent = false;
                }

                if (needToUpdateDynamicContent && !hasUpdatedRequestData)
                {
                    shadowRequest.shouldUseCachedShadowData = false;

                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, spotUpdateInfo.visibleLight.spotAngle) : spotUpdateInfo.visibleLight.spotAngle;
                    HDShadowUtils.ExtractSpotLightData(
                        updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
                        updateInfo.shapeHeight, spotUpdateInfo.visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                        out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, HDLightType.Spot, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicSpotShadowRequestsAndResolutionRequests()
        {
            int spotCount = dynamicSpotUpdateInfos.Length;

            for (int i = 0; i < spotCount; i++)
            {
                ref readonly ShadowRequestIntermediateUpdateData directionalUpdateInfo = ref dynamicSpotUpdateInfos.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
                int lightIndex = directionalUpdateInfo.lightIndex;
                Vector2 viewportSize = directionalUpdateInfo.viewportSize;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;

                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                // Write per light type matrices, splitDatas and culling parameters
                float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, directionalUpdateInfo.visibleLight.spotAngle) : directionalUpdateInfo.visibleLight.spotAngle;
                HDShadowUtils.ExtractSpotLightData(
                    updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
                    updateInfo.shapeHeight, directionalUpdateInfo.visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                    out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
                    out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                    out shadowRequest.splitData
                );

                // Assign all setting common to every lights
                HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, directionalUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                    lightIndex, HDLightType.Spot, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateCachedAreaShadowRequestsAndResolutionRequests(NativeList<ShadowRequestIntermediateUpdateData> areaUpdateInfos)
        {
            int areaCount = areaUpdateInfos.Length;

            for (int i = 0; i < areaCount; i++)
            {
                ShadowRequestIntermediateUpdateData areaUpdateInfo = areaUpdateInfos[i];
                bool needToUpdateCachedContent = areaUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                bool hasCachedComponent = areaUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent];
                HDShadowRequestHandle shadowRequestHandle = areaUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = areaUpdateInfo.additionalLightDataIndex;
                int lightIndex = areaUpdateInfo.lightIndex;
                Vector2 viewportSize = areaUpdateInfo.viewportSize;
                VisibleLight visibleLight = visibleLights[lightIndex];
                ShadowMapUpdateType updateType = areaUpdateInfo.updateType;

                HDProcessedVisibleLight processedEntity = processedEntities[lightIndex];
                HDLightType lightType = processedEntity.lightType;

                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    Matrix4x4 invViewProjection = default;
                    switch (updateInfo.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                            float offset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                            Vector3 shadowOffset = offset * visibleLight.GetForward();
                            HDShadowUtils.ExtractRectangleAreaLightData(visibleLight,
                                visibleLight.GetPosition() + shadowOffset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
                                shapeSize, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                                out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                                out shadowRequest.splitData);
                            break;
                        case AreaLightShape.Tube:
                            //Tube do not cast shadow at the moment.
                            //They should not call this method.
                            break;
                    }

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

                    hasUpdatedRequestData = true;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shouldRenderCachedComponent = true;
                }
                else
                {
                    shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
                    shadowRequest.shouldUseCachedShadowData = true;
                    shadowRequest.shouldRenderCachedComponent = false;
                }

                if (needToUpdateDynamicContent && !hasUpdatedRequestData)
                {
                    shadowRequest.shouldUseCachedShadowData = false;

                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    /// Write per light type matrices, splitDatas and culling parameters
                    Matrix4x4 invViewProjection = default;
                    switch (updateInfo.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                            float offset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                            Vector3 shadowOffset = offset * visibleLight.GetForward();
                            HDShadowUtils.ExtractRectangleAreaLightData(visibleLight,
                                visibleLight.GetPosition() + shadowOffset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
                                shapeSize, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                                out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                                out shadowRequest.splitData);
                            break;
                        case AreaLightShape.Tube:
                            //Tube do not cast shadow at the moment.
                            //They should not call this method.
                            break;
                    }

                    // Assign all setting common to every lights
                    HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                        lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicAreaShadowRequestsAndResolutionRequests(NativeList<ShadowRequestIntermediateUpdateData> areaUpdateInfos)
        {
            int areaCount = areaUpdateInfos.Length;

            for (int i = 0; i < areaCount; i++)
            {
                ShadowRequestIntermediateUpdateData areaUpdateInfo = areaUpdateInfos[i];
                HDShadowRequestHandle shadowRequestHandle = areaUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = areaUpdateInfo.additionalLightDataIndex;
                int lightIndex = areaUpdateInfo.lightIndex;
                Vector2 viewportSize = areaUpdateInfo.viewportSize;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;

                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                /// Write per light type matrices, splitDatas and culling parameters
                Matrix4x4 invViewProjection = default;
                switch (updateInfo.areaLightShape)
                {
                    case AreaLightShape.Rectangle:
                        Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                        float offset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                        Vector3 shadowOffset = offset * areaUpdateInfo.visibleLight.GetForward();
                        HDShadowUtils.ExtractRectangleAreaLightData(areaUpdateInfo.visibleLight,
                            areaUpdateInfo.visibleLight.GetPosition() + shadowOffset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
                            shapeSize, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                            out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                            out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                            out shadowRequest.splitData);
                        break;
                    case AreaLightShape.Tube:
                        //Tube do not cast shadow at the moment.
                        //They should not call this method.
                        break;
                }

                // Assign all setting common to every lights
                HDAdditionalLightData.SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, areaUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                    lightIndex, HDLightType.Area, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public static void UpdateNonDirectionalCachedRequests(ShadowMapType shadowMapType, HDLightType hdLightType,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices, NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles,
            NativeList<HDShadowRequest> requestStorage, NativeList<int> requestIndicesStorage, NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            NativeList<HDShadowRequestHandle> cachedAtlasShadowRequests, NativeList<HDShadowRequestHandle> dynamicAtlasShadowRequests, NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits,
            NativeHashMap<int, HDCachedShadowAtlas.CachedTransform> transformCaches, NativeHashMap<int, HDLightRenderEntity> registeredLightDataPendingPlacement, NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> recordsPendingPlacement,
            NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> shadowsPendingRendering, NativeHashMap<int, int> shadowsWithValidData, NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> placedShadows,
            int count, int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                int lightIdxForCachedShadows = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows;
                // If we force evicted the light, it will have lightIdxForCachedShadows == -1
                bool shadowHasAtlasPlacement = !(registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                                                 recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows)) && lightIdxForCachedShadows != -1;
                bool needsRenderingDueToTransformChange = false;
                if (shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.updateUponLightMovement)
                {
                    if (transformCaches.TryGetValue(shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
                    {
                        float positionThreshold = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.cachedShadowTranslationUpdateThreshold;
                        float3 positionDiffVec = cachedTransform.position - shadowIndicesAndVisibleLightData.visibleLight.GetPosition();
                        float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                        if (positionDiff > positionThreshold * positionThreshold)
                        {
                            needsRenderingDueToTransformChange = true;
                        }
                        float angleDiffThreshold = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.cachedShadowAngleUpdateThreshold;
                        float3 cachedAngles = cachedTransform.angles;
                        float3 angleDiff = cachedAngles - HDShadowUtils.QuaternionToEulerZXY(new quaternion(shadowIndicesAndVisibleLightData.visibleLight.localToWorldMatrix));
                        // Any angle difference
                        if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                        {
                            needsRenderingDueToTransformChange = true;
                        }

                        if (needsRenderingDueToTransformChange)
                        {
                            // Update the record
                            cachedTransform.position = shadowIndicesAndVisibleLightData.visibleLight.GetPosition();
                            cachedTransform.angles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(shadowIndicesAndVisibleLightData.visibleLight.localToWorldMatrix));
                            transformCaches[shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows] = cachedTransform;
                        }
                    }
                }

                var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;
                for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                {
                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                    HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);
                    if (!resolutionRequestHandle.valid)
                        continue;
                    ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);
                    int cachedShadowID = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows + index;
                    bool needToUpdateCachedContent = false;
                    //bool needToUpdateDynamicContent = !isSampledFromCache;
                    //bool hasUpdatedRequestData = false;

                    if (shadowHasAtlasPlacement)
                    {
                        bool shadowsPendingRenderingContainedShadowID = shadowsPendingRendering.Remove(cachedShadowID);
                        needToUpdateCachedContent = needsRenderingDueToTransformChange || shadowsPendingRenderingContainedShadowID;

                        if (shadowsPendingRenderingContainedShadowID)
                        {
                            // Handshake with the cached shadow manager to notify about the rendering.
                            // Technically the rendering has not happened yet, but it is scheduled.
                            shadowsWithValidData.Add(cachedShadowID, cachedShadowID);
                        }

                        HDCachedShadowAtlas.CachedShadowRecord record;
                        bool valueFound = placedShadows.TryGetValue(cachedShadowID, out record);

                        if (!valueFound)
                        {
                            Debug.LogWarning("Trying to render a cached shadow map that doesn't have a slot in the atlas yet.");
                        }

                        resolutionRequest.cachedAtlasViewport = new Rect(record.offsetInAtlas.x, record.offsetInAtlas.y, record.viewportSize, record.viewportSize);
                        resolutionRequest.resolution = new Vector2(record.viewportSize, record.viewportSize);
                    }

                    shadowRequest.isInCachedAtlas = isSampledFromCache;
                    shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shadowMapType = shadowMapType;
                    shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                    shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                    int updateDataListIndex = updateDataList.Length;
                    updateDataList.Length = updateDataListIndex + 1;
                    ref ShadowRequestIntermediateUpdateData updateInfo = ref updateDataList.ElementAt(updateDataListIndex);
                    updateInfo.visibleLight = shadowIndicesAndVisibleLightData.visibleLight;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = true;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = needToUpdateCachedContent;
                    updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                    updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                    updateInfo.updateType = updateType;
                    updateInfo.viewportSize = resolutionRequest.resolution;
                    updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;

                    if (shadowRequestIndex < shadowManagerRequestCount)
                    {
                        bool addToCached = updateType == ShadowMapUpdateType.Cached || updateType == ShadowMapUpdateType.Mixed;
                        bool addDynamic = updateType == ShadowMapUpdateType.Dynamic || updateType == ShadowMapUpdateType.Mixed;
                        HDShadowRequestHandle shadowRequestHandle = shadowRequestSetHandle[index];
                        if (addToCached)
                            cachedAtlasShadowRequests.Add(shadowRequestHandle);
                        if (addDynamic)
                        {
                            dynamicAtlasShadowRequests.Add(shadowRequestHandle);
                            if(updateType == ShadowMapUpdateType.Mixed)
                                mixedRequestsPendingBlits.Add(shadowRequestHandle);
                        }
                    }
                }
            }
        }

        public static void UpdateNonDirectionalDynamicRequests(ShadowMapType shadowMapType, HDLightType hdLightType,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices, NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles,
            NativeList<HDShadowRequest> requestStorage, NativeList<int> requestIndicesStorage, NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            NativeList<HDShadowRequestHandle> cachedAtlasShadowRequests, NativeList<HDShadowRequestHandle> dynamicAtlasShadowRequests, NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits,
            int count, int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode);
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;

                for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                {
                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                    HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);
                    if (!resolutionRequestHandle.valid)
                        continue;
                    ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);

                    shadowRequest.isInCachedAtlas = isSampledFromCache;
                    shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.shadowMapType = shadowMapType;
                    shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                    shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                    int updateDataListIndex = updateDataList.Length;
                    updateDataList.Length = updateDataListIndex + 1;
                    ref ShadowRequestIntermediateUpdateData updateInfo = ref updateDataList.ElementAt(updateDataListIndex);
                    updateInfo.visibleLight = shadowIndicesAndVisibleLightData.visibleLight;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = hasCachedComponent;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = false;
                    updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                    updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                    updateInfo.updateType = updateType;
                    updateInfo.viewportSize = resolutionRequest.resolution;
                    updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;

                    if (shadowRequestIndex < shadowManagerRequestCount)
                    {
                        bool addToCached = updateType == ShadowMapUpdateType.Cached || updateType == ShadowMapUpdateType.Mixed;
                        bool addDynamic = updateType == ShadowMapUpdateType.Dynamic || updateType == ShadowMapUpdateType.Mixed;
                        HDShadowRequestHandle shadowRequestHandle = shadowRequestSetHandle[index];
                        if (addToCached)
                            cachedAtlasShadowRequests.Add(shadowRequestHandle);
                        if (addDynamic)
                        {
                            dynamicAtlasShadowRequests.Add(shadowRequestHandle);
                            if(updateType == ShadowMapUpdateType.Mixed)
                                mixedRequestsPendingBlits.Add(shadowRequestHandle);
                        }
                    }
                }
            }
        }
    }
}
