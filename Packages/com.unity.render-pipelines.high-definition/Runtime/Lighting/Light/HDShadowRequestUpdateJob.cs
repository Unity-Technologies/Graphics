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

        [Unity.Collections.ReadOnly] public NativeBitArray shadowRequestValidityArray;
        [Unity.Collections.ReadOnly] public NativeArray<uint> sortKeys;
        [Unity.Collections.ReadOnly] public NativeArray<int> visibleLightEntityDataIndices;
        [Unity.Collections.ReadOnly] public NativeArray<HDProcessedVisibleLight> processedEntities;
        [Unity.Collections.ReadOnly] public NativeArray<VisibleLight> visibleLights;
        [Unity.Collections.ReadOnly] public NativeList<int> requestIndicesStorage;
        [Unity.Collections.ReadOnly] public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
        [Unity.Collections.ReadOnly] public NativeArray<ShadowIndicesAndVisibleLightData> visibleLightsAndIndicesBuffer;

        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> cachedPointVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> cachedSpotVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> cachedAreaRectangleVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> cachedDirectionalVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> dynamicPointVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> dynamicSpotVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> dynamicAreaRectangleVisibleLightsAndIndices;
        [Unity.Collections.ReadOnly] public UnsafeList<ShadowIndicesAndVisibleLightData> dynamicDirectionalVisibleLightsAndIndices;

        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> dynamicPointHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> cachedPointHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> dynamicSpotHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> cachedSpotHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> dynamicAreaRectangleHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> cachedAreaRectangleHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> dynamicDirectionalHDSplits;
        [Unity.Collections.ReadOnly] public UnsafeList<HDShadowCullingSplit> cachedDirectionalHDSplits;

        public NativeList<HDShadowRequest> requestStorage;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> cachedDirectionalUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicPointUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicSpotUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicAreaRectangleUpdateInfos;
        public NativeList<ShadowRequestIntermediateUpdateData> dynamicDirectionalUpdateInfos;
        public NativeList<float4> frustumPlanesStorage;
        public NativeList<Vector3> cachedViewPositionsStorage;

        public NativeArray<int> shadowIndices;

#if UNITY_EDITOR
        [WriteOnly] public NativeArray<int> shadowRequestCounts;
#endif

        [Unity.Collections.ReadOnly] public int lightCounts;
        [Unity.Collections.ReadOnly] public int shadowSettingsCascadeShadowSplitCount;

        [Unity.Collections.ReadOnly] public Vector3 worldSpaceCameraPos;
        [Unity.Collections.ReadOnly] public int shaderConfigCameraRelativeRendering;
        [Unity.Collections.ReadOnly] public int shadowRequestCount;
        [Unity.Collections.ReadOnly] public HDShadowFilteringQuality punctualShadowFilteringQuality;
        [Unity.Collections.ReadOnly] public HDShadowFilteringQuality directionalShadowFilteringQuality;
        [Unity.Collections.ReadOnly] public bool usesReversedZBuffer;

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
            // Write the first relevant shadow index to the shadowIndices buffer, one of this job's outputs.
            // The value may get overwritten later in the job with -1 if the request does not have atlas placement,
            // but we don't know which shadows do or don't have atlas placement yet.
            // We may want to extract this and the overwriting to a different set of loops in future iterations.

            for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
            {
                int shadowRequestCount = 0;
                int firstShadowRequestIndex = -1;

                if (shadowRequestValidityArray.IsSet(sortKeyIndex))
                {
                    uint sortKey = sortKeys[sortKeyIndex];
                    int lightIndex = (int)(sortKey & 0xFFFF);

                    ShadowIndicesAndVisibleLightData visibleLightsAndIndices = visibleLightsAndIndicesBuffer[lightIndex];
                    HDShadowRequestSetHandle shadowRequestSetHandle = visibleLightsAndIndices.shadowRequestSetHandle;

                    for (int index = 0; index < visibleLightsAndIndices.splitCount; index++)
                    {
                        if (!visibleLightsAndIndices.isSplitValidMask[(uint)index])
                            continue;

                        HDShadowRequestHandle shadowRequestIndexLocation = shadowRequestSetHandle[index];
                        int shadowRequestIndex = requestIndicesStorage[shadowRequestIndexLocation.storageIndexForRequestIndex];
                        shadowManager.shadowRequests[shadowRequestIndex] = shadowRequestIndexLocation;

                        // Store the first shadow request id to return it
                        if (firstShadowRequestIndex == -1)
                            firstShadowRequestIndex = shadowRequestIndex;
                    }

                    shadowRequestCount = visibleLightsAndIndices.splitCount;
                }

#if UNITY_EDITOR
                shadowRequestCounts[sortKeyIndex] = shadowRequestCount;
#endif
                shadowIndices[sortKeyIndex] = firstShadowRequestIndex;
            }

            // Perform two loops for each combination of light type and atlas associativity.
            // In the first loop, we get all the atlas-related work out of the way.
            // In the second, we do the heavier ALU work for shadow requests with matrix math etc.
            // Note: some of the work for directional lights is still managed, and so we do it after this job completes.

            using (cachedDirectionalRequestsMarker.Auto())
            {
                for (int i = 0; i < cachedDirectionalVisibleLightsAndIndices.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref cachedDirectionalVisibleLightsAndIndices.ElementAt(i);
                    HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;
                    ShadowMapUpdateType cachedDirectionalUpdateType = shadowIndicesAndVisibleLightData.shadowUpdateType;
                    BitArray8 needCacheUpdateMask = shadowIndicesAndVisibleLightData.needCacheUpdateMask;
                    int lightIdxForCachedShadows = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows;
                    bool shadowHasAtlasPlacement = lightIdxForCachedShadows != -1;

                    if (!shadowHasAtlasPlacement)
                        shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex] = -1;

                    for (int index = 0; index < shadowIndicesAndVisibleLightData.splitCount; index++)
                    {
                        if (!shadowIndicesAndVisibleLightData.isSplitValidMask[(uint)index])
                            continue;

                        bool needToUpdateCachedContent = needCacheUpdateMask[(uint)index];
                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                        ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);

                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
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

                        if (shadowRequestIndex < shadowRequestCount)
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
                for (int i = 0; i < dynamicDirectionalVisibleLightsAndIndices.Length; i++)
                {
                    ref ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref dynamicDirectionalVisibleLightsAndIndices.ElementAt(i);
                    HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;

                    for (int index = 0; index < shadowIndicesAndVisibleLightData.splitCount; index++)
                    {
                        if (!shadowIndicesAndVisibleLightData.isSplitValidMask[(uint)index])
                            continue;

                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                        ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);

                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
                        shadowRequest.isInCachedAtlas = false;
                        shadowRequest.isMixedCached = false;
                        shadowRequest.shouldUseCachedShadowData = false;
                        shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
                        shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                        shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;

                        int updateDataListIndex = dynamicDirectionalUpdateInfos.Length;
                        dynamicDirectionalUpdateInfos.Length = updateDataListIndex + 1;
                        ref ShadowRequestIntermediateUpdateData updateInfo = ref dynamicDirectionalUpdateInfos.ElementAt(updateDataListIndex);

                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = false;
                        updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                        updateInfo.updateType = ShadowMapUpdateType.Dynamic;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;

                        if (shadowRequestIndex < shadowRequestCount)
                            shadowManager.cascadeShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                    }
                }
            }

            // Update cached area rectangle:
            using (cachedAreaRectangleRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(LightType.Rectangle,
                    shadowManager.cachedShadowManager.areaShadowAtlas,
                    shadowManager.areaShadowAtlas,
                    cachedAreaRectangleVisibleLightsAndIndices, cachedAreaRectangleUpdateInfos,
                    shadowIndices, shadowRequestCount);
                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaRectangleUpdateInfos);
            }

            // Update cached point:
            using (cachedPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(LightType.Point,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    cachedPointVisibleLightsAndIndices, cachedPointUpdateInfos,
                    shadowIndices, shadowRequestCount);
                UpdateCachedPointShadowRequestsAndResolutionRequests();
            }

            // Update cached spot:
            using (cachedSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalCachedRequests(LightType.Spot,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    cachedSpotVisibleLightsAndIndices, cachedSpotUpdateInfos,
                    shadowIndices, shadowRequestCount);
                UpdateCachedSpotShadowRequestsAndResolutionRequests();
            }

            // Update dynamic area rectangle:
            using (dynamicAreaRectangleRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(LightType.Rectangle,
                    shadowManager.cachedShadowManager.areaShadowAtlas,
                    shadowManager.areaShadowAtlas,
                    dynamicAreaRectangleVisibleLightsAndIndices, dynamicAreaRectangleUpdateInfos,
                    shadowRequestCount);
                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaRectangleUpdateInfos);
            }

            // Update dynamic point:
            using (dynamicPointRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(LightType.Point,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    dynamicPointVisibleLightsAndIndices, dynamicPointUpdateInfos,
                    shadowRequestCount);
                UpdateDynamicPointShadowRequestsAndResolutionRequests();
            }


            // Update dynamic point:
            using (dynamicSpotRequestsMarker.Auto())
            {
                UpdateNonDirectionalDynamicRequests(LightType.Spot,
                    shadowManager.cachedShadowManager.punctualShadowAtlas,
                    shadowManager.atlas,
                    dynamicSpotVisibleLightsAndIndices, dynamicSpotUpdateInfos,
                    shadowRequestCount);
                UpdateDynamicSpotShadowRequestsAndResolutionRequests();
            }
        }

        public void UpdateCachedPointShadowRequestsAndResolutionRequests()
        {
            int pointCount = cachedPointUpdateInfos.Length;

            for (int i = 0; i < pointCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData pointUpdateInfo = ref cachedPointUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref cachedPointHDSplits.ElementAt(i);
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
                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
                bool hasUpdatedRequestData = false;

                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight,
                        worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

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
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight,
                        worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicPointShadowRequestsAndResolutionRequests()
        {
            int pointCount = dynamicPointUpdateInfos.Length;

            for (int i = 0; i < pointCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData pointUpdateInfo = ref dynamicPointUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref dynamicPointHDSplits.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = pointUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = pointUpdateInfo.additionalLightDataIndex;
                int lightIndex = pointUpdateInfo.lightIndex;
                Vector2 viewportSize = pointUpdateInfo.viewportSize;
                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;
                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                shadowRequest.cullingSplit = newShadowCullingSplit;

                HDGpuLightsBuilder.SetPointRequestSettings(ref shadowRequest, shadowRequestHandle, in pointUpdateInfo.visibleLight,
                    worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                    lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
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
                ref ShadowRequestIntermediateUpdateData spotUpdateInfo = ref cachedSpotUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref cachedSpotHDSplits.ElementAt(i);
                bool needToUpdateCachedContent = spotUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                int lightIndex = spotUpdateInfo.lightIndex;
                Vector2 viewportSize = spotUpdateInfo.viewportSize;
                ShadowMapUpdateType updateType = spotUpdateInfo.updateType;
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                bool needToUpdateDynamicContent = !isSampledFromCache;
                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
                bool hasUpdatedRequestData = false;

                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight,
                        0f, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

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
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight,
                        0f, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicSpotShadowRequestsAndResolutionRequests()
        {
            int spotCount = dynamicSpotUpdateInfos.Length;

            for (int i = 0; i < spotCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData spotUpdateInfo = ref dynamicSpotUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref dynamicSpotHDSplits.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                int lightIndex = spotUpdateInfo.lightIndex;
                Vector2 viewportSize = spotUpdateInfo.viewportSize;
                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;
                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                shadowRequest.cullingSplit = newShadowCullingSplit;

                HDGpuLightsBuilder.SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight,
                    0f, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                    lightIndex, punctualShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateCachedAreaShadowRequestsAndResolutionRequests(NativeList<ShadowRequestIntermediateUpdateData> areaUpdateInfos)
        {
            int areaCount = areaUpdateInfos.Length;

            for (int i = 0; i < areaCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData areaUpdateInfo = ref areaUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref cachedAreaRectangleHDSplits.ElementAt(i);
                bool needToUpdateCachedContent = areaUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                HDShadowRequestHandle shadowRequestHandle = areaUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = areaUpdateInfo.additionalLightDataIndex;
                int lightIndex = areaUpdateInfo.lightIndex;
                Vector2 viewportSize = areaUpdateInfo.viewportSize;
                VisibleLight visibleLight = visibleLights[lightIndex];
                ShadowMapUpdateType updateType = areaUpdateInfo.updateType;
                bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                bool needToUpdateDynamicContent = !isSampledFromCache;
                bool hasUpdatedRequestData = false;

                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
                if (needToUpdateCachedContent)
                {
                    cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight,
                        shadowRequest.cullingSplit.forwardOffset, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
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
                    shadowRequest.cullingSplit = newShadowCullingSplit;

                    HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight,
                        shadowRequest.cullingSplit.forwardOffset, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, shadowManager.areaShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        public void UpdateDynamicAreaShadowRequestsAndResolutionRequests(NativeList<ShadowRequestIntermediateUpdateData> areaUpdateInfos)
        {
            int areaCount = areaUpdateInfos.Length;

            for (int i = 0; i < areaCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData areaUpdateInfo = ref areaUpdateInfos.ElementAt(i);
                ref HDShadowCullingSplit newShadowCullingSplit = ref dynamicAreaRectangleHDSplits.ElementAt(i);
                HDShadowRequestHandle shadowRequestHandle = areaUpdateInfo.shadowRequestHandle;
                ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                int additionalLightDataIndex = areaUpdateInfo.additionalLightDataIndex;
                int lightIndex = areaUpdateInfo.lightIndex;
                Vector2 viewportSize = areaUpdateInfo.viewportSize;
                HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                shadowRequest.shouldUseCachedShadowData = false;
                shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                shadowRequest.cullingSplit = newShadowCullingSplit;

                HDGpuLightsBuilder.SetAreaRequestSettings(ref shadowRequest, shadowRequestHandle, areaUpdateInfo.visibleLight,
                    shadowRequest.cullingSplit.forwardOffset, worldSpaceCameraPos, shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                    lightIndex, shadowManager.areaShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
            }
        }

        public void UpdateNonDirectionalCachedRequests(LightType lightType,
            HDCachedShadowAtlasDataForShadowRequestUpdateJob cachedShadowAtlas,
            HDDynamicShadowAtlasDataForShadowRequestUpdateJob dynamicShadowAtlas,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            NativeArray<int> shadowIndices,
            int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;
                ShadowMapUpdateType updateType = shadowIndicesAndVisibleLightData.shadowUpdateType;
                BitArray8 needCacheUpdateMask = shadowIndicesAndVisibleLightData.needCacheUpdateMask;
                int lightIdxForCachedShadows = shadowIndicesAndVisibleLightData.additionalLightUpdateInfo.lightIdxForCachedShadows;

                // If we force evicted the light, it will have lightIdxForCachedShadows == -1
                bool shadowHasAtlasPlacement = !(cachedShadowAtlas.registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                                                 cachedShadowAtlas.recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows)) && lightIdxForCachedShadows != -1;

                if (!shadowHasAtlasPlacement)
                    shadowIndices[shadowIndicesAndVisibleLightData.sortKeyIndex] = -1;

                for (int index = 0; index < shadowIndicesAndVisibleLightData.splitCount; index++)
                {
                    if (!shadowIndicesAndVisibleLightData.isSplitValidMask[(uint)index])
                        continue;

                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                    ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);
                    bool needToUpdateCachedContent = needCacheUpdateMask[(uint)index];

                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
                    shadowRequest.isInCachedAtlas = updateType == ShadowMapUpdateType.Cached;
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

        public void UpdateNonDirectionalDynamicRequests(LightType lightType,
            HDCachedShadowAtlasDataForShadowRequestUpdateJob cachedShadowAtlas,
            HDDynamicShadowAtlasDataForShadowRequestUpdateJob dynamicShadowAtlas,
            UnsafeList<ShadowIndicesAndVisibleLightData> visibleLightsAndIndices,
            NativeList<ShadowRequestIntermediateUpdateData> updateDataList,
            int shadowManagerRequestCount)
        {
            for (int i = 0; i < visibleLightsAndIndices.Length; i++)
            {
                ref ShadowIndicesAndVisibleLightData shadowIndicesAndVisibleLightData = ref visibleLightsAndIndices.ElementAt(i);
                HDShadowRequestSetHandle shadowRequestSetHandle = shadowIndicesAndVisibleLightData.shadowRequestSetHandle;

                for (int index = 0; index < shadowIndicesAndVisibleLightData.splitCount; index++)
                {
                    if (!shadowIndicesAndVisibleLightData.isSplitValidMask[(uint)index])
                        continue;

                    HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                    int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                    ref HDShadowResolutionRequest resolutionRequest = ref shadowManager.shadowResolutionRequestStorage.ElementAt(shadowRequestIndex);

                    ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
                    shadowRequest.isInCachedAtlas = false;
                    shadowRequest.isMixedCached = false;
                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
                    shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;

                    int updateDataListIndex = updateDataList.Length;
                    updateDataList.Length = updateDataListIndex + 1;
                    ref ShadowRequestIntermediateUpdateData updateInfo = ref updateDataList.ElementAt(updateDataListIndex);

                    updateInfo.visibleLight = shadowIndicesAndVisibleLightData.visibleLight;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_HasCachedComponent] = false;
                    updateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent] = false;
                    updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                    updateInfo.additionalLightDataIndex = shadowIndicesAndVisibleLightData.dataIndex;
                    updateInfo.updateType = ShadowMapUpdateType.Dynamic;
                    updateInfo.viewportSize = resolutionRequest.resolution;
                    updateInfo.lightIndex = shadowIndicesAndVisibleLightData.lightIndex;

                    if (shadowRequestIndex < shadowManagerRequestCount)
                        dynamicShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
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

    internal class ShadowRequestUpdateProfiling
    {
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
