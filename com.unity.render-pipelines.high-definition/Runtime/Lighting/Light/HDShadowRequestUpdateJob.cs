using System;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Rendering.HighDefinition
{
    [BurstCompile] [NoAlias]
    internal unsafe struct HDShadowRequestUpdateJob : IJob
    {
        public HDShadowManagerDataForShadowRequestUpateJob shadowManager;

        public NativeBitArray isValidIndex;
        [Unity.Collections.ReadOnly] public NativeArray<uint> sortKeys;
        [Unity.Collections.ReadOnly] public NativeArray<int> visibleLightEntityDataIndices;
        [Unity.Collections.ReadOnly] public NativeArray<HDProcessedVisibleLight> processedEntities;
        [Unity.Collections.ReadOnly] public NativeArray<VisibleLight> visibleLights;
        [Unity.Collections.ReadOnly] public NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles;
        [Unity.Collections.ReadOnly] public NativeList<int> requestIndicesStorage;
        [Unity.Collections.ReadOnly] public NativeArray<Matrix4x4> kCubemapFaces;

        [Unity.Collections.ReadOnly] public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
        public NativeList<HDShadowRequest> requestStorage;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedDirectionalUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicDirectionalUpdateInfos;
        public NativeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesBuffer; // sized to lightCounts
        public NativeList<ShadowIndicesAndVisibleLightData> splitVisibleLightsAndIndicesBuffer; // sized to lightCounts
        public NativeList<float4> frustumPlanesStorage;
        public NativeList<Vector3> cachedViewPositionsStorage;

        public NativeArray<int> shadowIndices;

#if UNITY_EDITOR
        [WriteOnly] public NativeArray<int> shadowRequestCounts;
#endif

        [Unity.Collections.ReadOnly] public int lightCounts;
        [Unity.Collections.ReadOnly] public int shadowSettingsCascadeShadowSplitCount;
        [Unity.Collections.ReadOnly] public int invalidIndex;

        [Unity.Collections.ReadOnly] public Vector3 worldSpaceCameraPos;
        [Unity.Collections.ReadOnly] public int shaderConfigCameraRelativeRendering;
        [Unity.Collections.ReadOnly] public int shadowRequestCount;
        [Unity.Collections.ReadOnly] public HDShadowFilteringQuality shadowFilteringQuality;
        [Unity.Collections.ReadOnly] public bool usesReversedZBuffer;

        public ProfilerMarker validIndexCalculationsMarker;
        public ProfilerMarker indicesAndPreambleMarker;
        public ProfilerMarker cachedRequestsMarker;
        public ProfilerMarker cachedDirectionalRequestsMarker;
        public ProfilerMarker dynamicDirectionalRequestsMarker;
        public ProfilerMarker dynamicPointRequestsMarker;
        public ProfilerMarker dynamicSpotRequestsMarker;
        public ProfilerMarker dynamicAreaRectangleRequestsMarker;
        public ProfilerMarker cachedPointRequestsMarker;
        public ProfilerMarker cachedSpotRequestsMarker;
        public ProfilerMarker cachedAreaRectangleRequestsMarker;

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
            HDAdditionalLightDataUpdateInfo* updateInfosUnsafePtr = (HDAdditionalLightDataUpdateInfo*)additionalLightDataUpdateInfos.GetUnsafeReadOnlyPtr();

            int shadowManagerRequestCount = shadowRequestCount;

            int cachedAreaRectangleCount = 0;
            int cachedPointCount = 0;
            int cachedSpotCount = 0;
            int cachedDirectionalCount = 0;
            int dynamicAreaRectangleCount = 0;
            int dynamicPointCount = 0;
            int dynamicSpotCount = 0;
            int dynamicDirectionalCount = 0;

            int bufferCount = 0;

            // We do a few things in this first loop:
            //
            // 1. We gather counts for each light type/atlas combo so that we can categorically bucket them in the next loop.
            //      We do this to reduce the 30-odd atlas collections potentially involved with each light down to 9 or less per loop.
            //
            // 2. We copy relevant light data to a buffer, in order to reduce random access in subsequent loops.
            //
            // 3. We write the first relevant shadow index to the shadowIndices buffer, one of this job's outputs.
            //      The value may get overwritten later in the job with -1 if the request does not have atlas placement,
            //      but we don't know which shadows do or don't have atlas placement yet.
            //      We may want to extract this and the overwriting to a different set of loops in future iterations.

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

                    if ((processedEntities[lightIndex].shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) != 0)
                    {
                        shadowRequestCount = 0;
                        int count = HDAdditionalLightData.GetShadowRequestCount(shadowSettingsCascadeShadowSplitCount, lightType);
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
                        bufferElement.sortKeyIndex = sortKeyIndex;

                        bool hasCachedComponent = lightUpdateInfo.shadowUpdateMode != ShadowUpdateMode.EveryFrame;

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
                                break;
                        }

                        splitVisibleLightsAndIndicesBuffer.Length = visibleLightsAndIndicesBuffer.Length;

                        for (int index = 0; index < count; index++)
                        {
                            HDShadowRequestHandle shadowRequestIndexLocation = shadowRequestSetHandle[index];

                            int shadowRequestIndex = requestIndicesStorage[shadowRequestIndexLocation.storageIndexForRequestIndex];

                            if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManagerRequestCount)
                                continue;

                            shadowManager.shadowRequests[shadowRequestIndex] = shadowRequestIndexLocation;

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

            // Now that we have the counts for each bucket, we divide a scratchpad array into slices,
            // and categorically sort data from our buffer into each slice of it.
            // Future iterations may involve storing the data in a normalized form to begin with,
            // in which case we would skip both this and the count collection from the previous loop.

            int bufferedDataCount = visibleLightsAndIndicesBuffer.Length;

            ShadowIndicesAndVisibleLightData* splitVisibleLightsAndIndicesBufferPtr = (ShadowIndicesAndVisibleLightData*)splitVisibleLightsAndIndicesBuffer.GetUnsafePtr();
            int bufferOffsetIterator = 0;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            cachedDirectionalVisibleLightsAndIndices.m_capacity = cachedDirectionalCount;
            bufferOffsetIterator += cachedDirectionalCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            cachedAreaRectangleVisibleLightsAndIndices.m_capacity = cachedAreaRectangleCount;
            bufferOffsetIterator += cachedAreaRectangleCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            cachedPointVisibleLightsAndIndices.m_capacity = cachedPointCount;
            bufferOffsetIterator += cachedPointCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> cachedSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            cachedSpotVisibleLightsAndIndices.m_capacity = cachedSpotCount;
            bufferOffsetIterator += cachedSpotCount;

            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaRectangleVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            dynamicAreaRectangleVisibleLightsAndIndices.m_capacity = dynamicAreaRectangleCount;
            bufferOffsetIterator += dynamicAreaRectangleCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicPointVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            dynamicPointVisibleLightsAndIndices.m_capacity = dynamicPointCount;
            bufferOffsetIterator += dynamicPointCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicSpotVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            dynamicSpotVisibleLightsAndIndices.m_capacity = dynamicSpotCount;
            bufferOffsetIterator += dynamicSpotCount;
            UnsafeList<ShadowIndicesAndVisibleLightData> dynamicDirectionalVisibleLightsAndIndices = new UnsafeList<ShadowIndicesAndVisibleLightData>(splitVisibleLightsAndIndicesBufferPtr + bufferOffsetIterator, 0);
            dynamicDirectionalVisibleLightsAndIndices.m_capacity = dynamicDirectionalCount;
            bufferOffsetIterator += dynamicDirectionalCount;

            for (int bufferedDataIndex = 0; bufferedDataIndex < bufferedDataCount; bufferedDataIndex++)
            {
                ref readonly ShadowIndicesAndVisibleLightData readData = ref visibleLightsAndIndicesBuffer.ElementAt(bufferedDataIndex);
                ref readonly HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref readData.additionalLightUpdateInfo;
                HDLightType lightType = readData.lightType;
                bool hasCachedComponent = lightUpdateInfo.shadowUpdateMode != ShadowUpdateMode.EveryFrame;

                switch (lightType)
                {
                    case HDLightType.Spot:
                        if (hasCachedComponent)
                        {
                            cachedSpotVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            dynamicSpotVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Directional:
                        if (hasCachedComponent)
                        {
                            cachedDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            dynamicDirectionalVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Point:
                        if (hasCachedComponent)
                        {
                            cachedPointVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        else
                        {
                            dynamicPointVisibleLightsAndIndices.AddNoResize(readData);
                        }
                        break;
                    case HDLightType.Area:
                        if (lightUpdateInfo.areaLightShape == AreaLightShape.Rectangle)
                        {
                            if (hasCachedComponent)
                            {
                                cachedAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                            }
                            else
                            {
                                dynamicAreaRectangleVisibleLightsAndIndices.AddNoResize(readData);
                            }
                        }
                        break;
                }
            }

            // Finally, we perform two loops for each combination of light type and atlas associativity.
            // In the first loop, we get all the atlas-related work out of the way.
            // In the second, we do the heavier ALU work for shadow requests with matrix math etc.
            // Note: some of the work for directional lights is still managed, and so we do it after this job completes.

            using (cachedDirectionalRequestsMarker.Auto())
            {
                for (int i = 0; i < cachedDirectionalCount; i++)
                {
                    ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref cachedDirectionalVisibleLightsAndIndices.ElementAt(i);

                    HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[shadowIndicesAndVisibleLightData.dataIndex];
                    ShadowMapUpdateType cachedDirectionalUpdateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows, shadowManager.cachedShadowManager.directionalHasCachedAtlas);

                    int lightIdxForCachedShadows = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows;
                    bool shadowHasAtlasPlacement = lightIdxForCachedShadows != -1;

                    bool needsRenderingDueToTransformChange = false;
                    float angleDiffThreshold = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.cachedShadowAngleUpdateThreshold;
                    float3 cachedAngles = shadowManager.cachedShadowManager.cachedDirectionalAngles.Value;
                    float3 currentAngles = HDShadowUtils.QuaternionToEulerZXY( new quaternion(shadowIndicesAndVisibleLightData.visibleLight.localToWorldMatrix));
                    float3 angleDiff = cachedAngles - currentAngles;
                    // Any angle difference
                    if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                    {
                        shadowManager.cachedShadowManager.cachedDirectionalAngles.Value = currentAngles;
                        needsRenderingDueToTransformChange = true;
                    }

                    int shadowIndex = shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex];
                    shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex] = shadowHasAtlasPlacement ? shadowIndex : -1;

                    BitArray8 directionalShadowPendingUpdate = shadowManager.cachedShadowManager.directionalShadowPendingUpdate;

                    for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                    {
                        bool directionalShadowIdxPendingUpdate = lightIdxForCachedShadows != -1 && directionalShadowPendingUpdate[(uint)(lightIdxForCachedShadows + index)];
                        bool needToUpdateCachedContent = shadowHasAtlasPlacement && needsRenderingDueToTransformChange || directionalShadowIdxPendingUpdate;

                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];

                        ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);
                        resolutionRequest.cachedAtlasViewport = resolutionRequest.dynamicAtlasViewport;

                        shadowRequest.isInCachedAtlas = cachedDirectionalUpdateType == ShadowMapUpdateType.Cached;
                        shadowRequest.isMixedCached = cachedDirectionalUpdateType == ShadowMapUpdateType.Mixed;
                        shadowRequest.shouldUseCachedShadowData = !needToUpdateCachedContent;
                        shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
                        shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                        shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
                        int updateDataListIndex = cachedDirectionalUpdateInfos.Length;
                        cachedDirectionalUpdateInfos.Length = updateDataListIndex + 1;
                        ref ShadowRequestIntermediateUpdateData updateInfo = ref cachedDirectionalUpdateInfos.ElementAt(updateDataListIndex);
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = true;
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = needToUpdateCachedContent;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                        updateInfo.updateType = cachedDirectionalUpdateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;

                        if (shadowRequestIndex < shadowManagerRequestCount)
                        {
                            if (cachedDirectionalUpdateType == ShadowMapUpdateType.Mixed && shadowManager.cachedShadowManager.directionalHasCachedAtlas)
                            {
                                shadowManager.cachedShadowManager.directionalLightAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                                shadowManager.cascadeShadowAtlas.mixedRequestsPendingBlits.Add(shadowRequestSetHandle[index]);
                            }

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
                    var updateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows, shadowManager.cachedShadowManager.directionalHasCachedAtlas);
                    bool hasCachedComponent = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode != ShadowUpdateMode.EveryFrame;
                    bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                    for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                    {
                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];

                        if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManagerRequestCount)
                            continue;

                        ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);

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
                UpdateNonDirectionalCachedRequests(HDLightType.Area,
                    shadowManager.cachedShadowManager.areaShadowAtlas,
                    shadowManager.areaShadowAtlas,
                    cachedAreaRectangleVisibleLightsAndIndices, cachedAreaRectangleUpdateInfos,
                    shadowIndices, shadowManagerRequestCount);
                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaRectangleUpdateInfos);
            }

            // Update cached point:
            using (cachedPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(HDLightType.Point,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    cachedPointVisibleLightsAndIndices, cachedPointUpdateInfos,
                    shadowIndices, shadowManagerRequestCount);
                UpdateCachedPointShadowRequestsAndResolutionRequests();
            }

            // Update cached spot:
            using (cachedSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(HDLightType.Spot,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    cachedSpotVisibleLightsAndIndices, cachedSpotUpdateInfos,
                    shadowIndices, shadowManagerRequestCount);
                UpdateCachedSpotShadowRequestsAndResolutionRequests();
            }

            // Update dynamic area rectangle:
            using (dynamicAreaRectangleRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(HDLightType.Area,
                    shadowManager.cachedShadowManager.areaShadowAtlas,
                    shadowManager.areaShadowAtlas,
                    dynamicAreaRectangleVisibleLightsAndIndices, dynamicAreaRectangleUpdateInfos,
                    shadowManagerRequestCount);
                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaRectangleUpdateInfos);
            }

            // Update dynamic point:
            using (dynamicPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(HDLightType.Point,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    dynamicPointVisibleLightsAndIndices, dynamicPointUpdateInfos,
                    shadowManagerRequestCount);
                UpdateDynamicPointShadowRequestsAndResolutionRequests();
            }


            // Update dynamic point:
            using (dynamicSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(HDLightType.Spot,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    dynamicSpotVisibleLightsAndIndices, dynamicSpotUpdateInfos,
                    shadowManagerRequestCount);
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
                        out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

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
                        out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
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
                    out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                    out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                    out shadowRequest.splitData
                );

                shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                    lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        /// <summary>
        /// This method has a duplicate of the same name in HDGpuLightsBuilder.LightLoop.cs
        /// This was duplicated to support the expensive managed callback per spot light, without sacrificing performance in the Burst compiled job.
        /// Any changes made here should be duplicated in the other function and vice versa.
        /// </summary>
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
                        out shadowRequest.view, out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    shadowRequest.projectionType = (updateInfo.spotLightShape == SpotLightShape.Box) ?
                        BatchCullingProjectionType.Orthographic:
                        BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

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
                        out shadowRequest.view, out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData
                    );

                    shadowRequest.projectionType = (updateInfo.spotLightShape == SpotLightShape.Box) ?
                        BatchCullingProjectionType.Orthographic:
                        BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicSpotShadowRequestsAndResolutionRequests()
        {
            int spotCount = dynamicSpotUpdateInfos.Length;

            for (int i = 0; i < spotCount; i++)
            {
                ref readonly ShadowRequestIntermediateUpdateData spotUpdateInfo = ref dynamicSpotUpdateInfos.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                int lightIndex = spotUpdateInfo.lightIndex;
                Vector2 viewportSize = spotUpdateInfo.viewportSize;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;

                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                // Write per light type matrices, splitDatas and culling parameters
                float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, spotUpdateInfo.visibleLight.spotAngle) : spotUpdateInfo.visibleLight.spotAngle;
                HDShadowUtils.ExtractSpotLightData(
                    updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
                    updateInfo.shapeHeight, spotUpdateInfo.visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
                    out shadowRequest.view, out Matrix4x4 invViewProjection, out Matrix4x4 projection,
                    out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                    out shadowRequest.splitData
                );

                shadowRequest.projectionType = (updateInfo.spotLightShape == SpotLightShape.Box) ?
                    BatchCullingProjectionType.Orthographic:
                    BatchCullingProjectionType.Perspective;

                HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                    lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateCachedAreaShadowRequestsAndResolutionRequests(NativeList<ShadowRequestIntermediateUpdateData> areaUpdateInfos)
        {
            int areaCount = areaUpdateInfos.Length;

            for (int i = 0; i < areaCount; i++)
            {
                ShadowRequestIntermediateUpdateData areaUpdateInfo = areaUpdateInfos[i];
                bool needToUpdateCachedContent = areaUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
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

                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
                float forwardOffset = 0f;
                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                    // Write per light type matrices, splitDatas and culling parameters
                    Matrix4x4 invViewProjection = default;

                    Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                    forwardOffset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                    float offset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                    HDShadowUtils.ExtractRectangleAreaLightData(visibleLight,
                        offset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
                        shapeSize, viewportSize, updateInfo.normalBias, usesReversedZBuffer,
                        out shadowRequest.view, out invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData);

                    shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, forwardOffset, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowManager.areaShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

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

                    Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                    forwardOffset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                    float offset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                    HDShadowUtils.ExtractRectangleAreaLightData(visibleLight,
                        offset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
                        shapeSize, viewportSize, updateInfo.normalBias, usesReversedZBuffer,
                        out shadowRequest.view, out invViewProjection, out Matrix4x4 projection,
                        out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
                        out shadowRequest.splitData);

                    shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                    HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, forwardOffset, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                        lightIndex, shadowManager.areaShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
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

                float forwardOffset = 0f;
                Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
                forwardOffset = HDAdditionalLightData.GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
                HDShadowUtils.ExtractRectangleAreaLightData(areaUpdateInfo.visibleLight, forwardOffset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane, shapeSize, viewportSize, updateInfo.normalBias, usesReversedZBuffer,
                    out shadowRequest.view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData);
                shadowRequest.projectionType = BatchCullingProjectionType.Perspective;

                HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, areaUpdateInfo.visibleLight, forwardOffset, worldSpaceCameraPos, invViewProjection, projection, viewportSize,
                    lightIndex, shadowManager.areaShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateNonDirectionalCachedRequests(HDLightType hdLightType,
            HDCachedShadowAtlasDataForShadowRequestUpdateJob cachedShadowAtlas,
            HDDynamicShadowAtlasDataForShadowRequestUpdateJob dynamicShadowAtlas,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            NativeArray<int> shadowIndices,
            int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                int lightIdxForCachedShadows = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows;
                // If we force evicted the light, it will have lightIdxForCachedShadows == -1
                bool shadowHasAtlasPlacement = !(cachedShadowAtlas.registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                                                 cachedShadowAtlas.recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows)) && lightIdxForCachedShadows != -1;

                int shadowIndex = shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex];
                shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex] = shadowHasAtlasPlacement ? shadowIndex : -1;

                bool needsRenderingDueToTransformChange = false;
                if (shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.updateUponLightMovement)
                {
                    if (cachedShadowAtlas.transformCaches.TryGetValue(shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
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
                            cachedShadowAtlas.transformCaches[shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows] = cachedTransform;
                        }
                    }
                }

                var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows, shadowManager.cachedShadowManager.directionalHasCachedAtlas);
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;
                for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                {
                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];

                    if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManagerRequestCount)
                        continue;

                    ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);
                    int cachedShadowID = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows + index;
                    bool needToUpdateCachedContent = false;

                    if (shadowHasAtlasPlacement)
                    {
                        bool shadowsPendingRenderingContainedShadowID = cachedShadowAtlas.shadowsPendingRendering.Remove(cachedShadowID);
                        needToUpdateCachedContent = needsRenderingDueToTransformChange || shadowsPendingRenderingContainedShadowID;

                        if (shadowsPendingRenderingContainedShadowID)
                        {
                            // Handshake with the cached shadow manager to notify about the rendering.
                            // Technically the rendering has not happened yet, but it is scheduled.
                            cachedShadowAtlas.shadowsWithValidData.Add(cachedShadowID, cachedShadowID);
                        }

                        HDCachedShadowAtlas.CachedShadowRecord record;
                        bool valueFound = cachedShadowAtlas.placedShadows.TryGetValue(cachedShadowID, out record);

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
                            cachedShadowAtlas.shadowRequests.Add(shadowRequestHandle);
                        if (addDynamic)
                        {
                            dynamicShadowAtlas.shadowRequests.Add(shadowRequestHandle);
                            if(updateType == ShadowMapUpdateType.Mixed)
                                dynamicShadowAtlas.mixedRequestsPendingBlits.Add(shadowRequestHandle);
                        }
                    }
                }
            }
        }

        public void UpdateNonDirectionalDynamicRequests(HDLightType hdLightType,
            HDCachedShadowAtlasDataForShadowRequestUpdateJob cachedShadowAtlas,
            HDDynamicShadowAtlasDataForShadowRequestUpdateJob dynamicShadowAtlas,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref readonly ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode, shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.alwaysDrawDynamicShadows, shadowManager.cachedShadowManager.directionalHasCachedAtlas);
                bool hasCachedComponent = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.shadowUpdateMode != ShadowUpdateMode.EveryFrame;
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;

                for (int index = 0; index < shadowIndicesAndVisibleLightData.shadowRequestCount; index++)
                {
                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];

                    if (shadowRequestIndex < 0 || shadowRequestIndex >= shadowManagerRequestCount)
                        continue;

                    ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);

                    shadowRequest.isInCachedAtlas = isSampledFromCache;
                    shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
                    shadowRequest.shouldUseCachedShadowData = false;
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
                            cachedShadowAtlas.shadowRequests.Add(shadowRequestHandle);
                        if (addDynamic)
                        {
                            dynamicShadowAtlas.shadowRequests.Add(shadowRequestHandle);
                            if(updateType == ShadowMapUpdateType.Mixed)
                                dynamicShadowAtlas.mixedRequestsPendingBlits.Add(shadowRequestHandle);
                        }
                    }
                }
            }
        }
    }

    internal struct ShadowRequestIntermediateUpdateData
    {
        public VisibleLight visibleLight;
        public HDShadowRequestHandle shadowRequestHandle;
        public int additionalLightDataIndex;
        public Vector2 viewportSize;

        public int lightIndex;

        public ShadowMapUpdateType updateType;
        public BitArray8 states;

        public const int k_HasCachedComponent = 0;
        public const int k_IsSampledFromCache = 1;
        public const int k_NeedsRenderingDueToTransformChange = 2;
        public const int k_ShadowHasAtlasPlacement = 3;
        public const int k_NeedToUpdateCachedContent = 4;
    }

    internal struct ShadowIndicesAndVisibleLightData
    {
        public HDAdditionalLightDataUpdateInfo additionalLightUpdateInfo;
        public VisibleLight visibleLight;
        public int dataIndex;
        public int lightIndex;
        public HDShadowRequestSetHandle shadowRequestSetHandle;
        public HDLightType lightType;
        public int shadowRequestCount;
        public int sortKeyIndex;
    }

    internal class ShadowRequestUpdateProfiling
    {

        internal static ProfilerMarker validIndexCalculationsMarker = new ProfilerMarker("ValidIndexCalculations");

        internal static ProfilerMarker indicesAndPreambleMarker = new ProfilerMarker("WriteShadowIndicesAndCollectLightInfo");
        internal static ProfilerMarker dynamicDirectionalRequestsMarker = new ProfilerMarker("UpdateDynamicDirectionalRequests");
        internal static ProfilerMarker dynamicPointRequestsMarker = new ProfilerMarker("UpdateDynamicPointRequests");
        internal static ProfilerMarker dynamicSpotRequestsMarker = new ProfilerMarker("UpdateDynamicSpotRequests");
        internal static ProfilerMarker dynamicAreaRectangleRequestsMarker = new ProfilerMarker("UpdateDynamicAreaRectangleRequests");
        internal static ProfilerMarker cachedDirectionalRequestsMarker = new ProfilerMarker("UpdateCachedDirectionalRequests");
        internal static ProfilerMarker cachedPointRequestsMarker = new ProfilerMarker("UpdateCachedPointRequests");
        internal static ProfilerMarker cachedSpotRequestsMarker = new ProfilerMarker("UpdateCachedSpotRequests");
        internal static ProfilerMarker cachedAreaRectangleRequestsMarker = new ProfilerMarker("UpdateCachedAreaRectangleRequests");
    }
}
