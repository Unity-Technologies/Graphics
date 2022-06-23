using System;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
        // [BurstCompile]
        // internal unsafe struct UpdateLightShadowRequestDataJob : IJob
        // {
        //     [ReadOnly] public NativeList<ShadowRequestDataUpdateInfo> pointUpdateInfos;
        //     [ReadOnly] public NativeList<ShadowRequestDataUpdateInfo> spotUpdateInfos;
        //     [ReadOnly] public NativeList<ShadowRequestDataUpdateInfo> areaUpdateInfos;
        //     [ReadOnly] public NativeArray<HDProcessedVisibleLight> processedVisibleLights;
        //     [ReadOnly] public NativeArray<VisibleLight> visibleLights;
        //     [ReadOnly] public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
        //     [ReadOnly] public NativeArray<Matrix4x4> kCubemapFaces;
        //
        //     public NativeList<HDShadowRequest> requestStorage;
        //     public NativeList<Vector3> cachedViewPositionsStorage;
        //     public NativeList<Vector4> frustumPlanesStorage;
        //
        //     [ReadOnly] public Vector3 worldSpaceCameraPos;
        //     [ReadOnly] public int shaderConfigCameraRelativeRendering;
        //     [ReadOnly] public HDShadowFilteringQuality shadowFilteringQuality;
        //     [ReadOnly] public bool usesReversedZBuffer;
        //
        //     public ProfilerMarker pointProfilerMarker;
        //
        //     public void Execute()
        //     {
        //         int pointCount = pointUpdateInfos.Length;
        //
        //         using (pointProfilerMarker.Auto())
        //         for (int i = 0; i < pointCount; i++)
        //         {
        //             ref readonly ShadowRequestDataUpdateInfo directionalUpdateInfo = ref pointUpdateInfos.ElementAt(i);
        //             bool needToUpdateCachedContent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
        //             bool hasCachedComponent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_HasCachedComponent];
        //             HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
        //             ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
        //             int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
        //             int lightIndex = directionalUpdateInfo.lightIndex;
        //             Vector2 viewportSize = directionalUpdateInfo.viewportSize;
        //             VisibleLight visibleLight = visibleLights[lightIndex];
        //             ShadowMapUpdateType updateType = directionalUpdateInfo.updateType;
        //
        //             HDProcessedVisibleLight processedEntity = processedVisibleLights[lightIndex];
        //             HDLightType lightType = processedEntity.lightType;
        //
        //             bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
        //
        //             // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
        //             bool needToUpdateDynamicContent = !isSampledFromCache;
        //             bool hasUpdatedRequestData = false;
        //
        //             HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
        //
        //             if (needToUpdateCachedContent)
        //             {
        //                 cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 HDShadowUtils.ExtractPointLightData(kCubemapFaces,
        //                     visibleLight, viewportSize, updateInfo.shadowNearPlane,
        //                     updateInfo.normalBias, (uint) shadowRequestHandle.offset, shadowFilteringQuality, usesReversedZBuffer, out shadowRequest.view,
        //                     out Matrix4x4 invViewProjection, out shadowRequest.projection,
        //                     out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                     out shadowRequest.splitData
        //                 );
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettingsPoint(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //
        //                 hasUpdatedRequestData = true;
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //                 shadowRequest.shouldRenderCachedComponent = true;
        //             }
        //             else if (hasCachedComponent)
        //             {
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
        //                 shadowRequest.shouldUseCachedShadowData = true;
        //                 shadowRequest.shouldRenderCachedComponent = false;
        //             }
        //
        //             if (needToUpdateDynamicContent && !hasUpdatedRequestData)
        //             {
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 HDShadowUtils.ExtractPointLightData(kCubemapFaces,
        //                     visibleLight, viewportSize, updateInfo.shadowNearPlane,
        //                     updateInfo.normalBias, (uint) shadowRequestHandle.offset, shadowFilteringQuality, usesReversedZBuffer,out shadowRequest.view,
        //                     out Matrix4x4 invViewProjection, out shadowRequest.projection,
        //                     out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                     out shadowRequest.splitData
        //                 );
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //             }
        //         }
        //
        //         int spotCount = spotUpdateInfos.Length;
        //
        //         for (int i = 0; i < spotCount; i++)
        //         {
        //             ShadowRequestDataUpdateInfo directionalUpdateInfo = spotUpdateInfos[i];
        //             bool needToUpdateCachedContent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
        //             bool hasCachedComponent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_HasCachedComponent];
        //             HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
        //             ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
        //             int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
        //             int lightIndex = directionalUpdateInfo.lightIndex;
        //             Vector2 viewportSize = directionalUpdateInfo.viewportSize;
        //             VisibleLight visibleLight = visibleLights[lightIndex];
        //             ShadowMapUpdateType updateType = directionalUpdateInfo.updateType;
        //
        //             HDProcessedVisibleLight processedEntity = processedVisibleLights[lightIndex];
        //             HDLightType lightType = processedEntity.lightType;
        //
        //             bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
        //
        //             // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
        //             bool needToUpdateDynamicContent = !isSampledFromCache;
        //             bool hasUpdatedRequestData = false;
        //
        //             HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
        //
        //             if (needToUpdateCachedContent)
        //             {
        //                 cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, visibleLight.spotAngle) : visibleLight.spotAngle;
        //                 HDShadowUtils.ExtractSpotLightData(
        //                     updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
        //                     updateInfo.shapeHeight, visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
        //                     out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
        //                     out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                     out shadowRequest.splitData
        //                 );
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //
        //                 hasUpdatedRequestData = true;
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //                 shadowRequest.shouldRenderCachedComponent = true;
        //             }
        //             else if (hasCachedComponent)
        //             {
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
        //                 shadowRequest.shouldUseCachedShadowData = true;
        //                 shadowRequest.shouldRenderCachedComponent = false;
        //             }
        //
        //             if (needToUpdateDynamicContent && !hasUpdatedRequestData)
        //             {
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone ? Math.Min(updateInfo.customSpotLightShadowCone, visibleLight.spotAngle) : visibleLight.spotAngle;
        //                 HDShadowUtils.ExtractSpotLightData(
        //                     updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
        //                     updateInfo.shapeHeight, visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
        //                     out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
        //                     out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                     out shadowRequest.splitData
        //                 );
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //             }
        //         }
        //
        //         int areaCount = areaUpdateInfos.Length;
        //
        //         for (int i = 0; i < areaCount; i++)
        //         {
        //             ShadowRequestDataUpdateInfo directionalUpdateInfo = areaUpdateInfos[i];
        //             bool needToUpdateCachedContent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
        //             bool hasCachedComponent = directionalUpdateInfo.states[ShadowRequestDataUpdateInfo.k_HasCachedComponent];
        //             HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
        //             ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
        //             int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
        //             int lightIndex = directionalUpdateInfo.lightIndex;
        //             Vector2 viewportSize = directionalUpdateInfo.viewportSize;
        //             VisibleLight visibleLight = visibleLights[lightIndex];
        //             ShadowMapUpdateType updateType = directionalUpdateInfo.updateType;
        //
        //             HDProcessedVisibleLight processedEntity = processedVisibleLights[lightIndex];
        //             HDLightType lightType = processedEntity.lightType;
        //
        //             bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
        //
        //             // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
        //             bool needToUpdateDynamicContent = !isSampledFromCache;
        //             bool hasUpdatedRequestData = false;
        //
        //             HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];
        //
        //             if (needToUpdateCachedContent)
        //             {
        //                 cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 Matrix4x4 invViewProjection = default;
        //                 switch (updateInfo.areaLightShape)
        //                 {
        //                     case AreaLightShape.Rectangle:
        //                         Vector2 shapeSize = new Vector2(updateInfo.shapeWidth, updateInfo.shapeHeight);
        //                         float offset = GetAreaLightOffsetForShadows(shapeSize, updateInfo.areaLightShadowCone);
        //                         Vector3 shadowOffset = offset * visibleLight.GetForward();
        //                         HDShadowUtils.ExtractRectangleAreaLightData(visibleLight,
        //                             visibleLight.GetPosition() + shadowOffset, updateInfo.areaLightShadowCone, updateInfo.shadowNearPlane,
        //                             shapeSize, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
        //                             out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
        //                             out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                             out shadowRequest.splitData);
        //                         break;
        //                     case AreaLightShape.Tube:
        //                         //Tube do not cast shadow at the moment.
        //                         //They should not call this method.
        //                         break;
        //                 }
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //
        //                 hasUpdatedRequestData = true;
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //                 shadowRequest.shouldRenderCachedComponent = true;
        //             }
        //             else if (hasCachedComponent)
        //             {
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
        //                 shadowRequest.shouldUseCachedShadowData = true;
        //                 shadowRequest.shouldRenderCachedComponent = false;
        //             }
        //
        //             if (needToUpdateDynamicContent && !hasUpdatedRequestData)
        //             {
        //                 shadowRequest.shouldUseCachedShadowData = false;
        //
        //                 shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
        //
        //                 // Write per light type matrices, splitDatas and culling parameters
        //                 float spotAngleForShadows = updateInfo.useCustomSpotLightShadowCone
        //                     ? Math.Min(updateInfo.customSpotLightShadowCone, visibleLight.spotAngle)
        //                     : visibleLight.spotAngle;
        //                 HDShadowUtils.ExtractSpotLightData(
        //                     updateInfo.spotLightShape, spotAngleForShadows, updateInfo.shadowNearPlane, updateInfo.aspectRatio, updateInfo.shapeWidth,
        //                     updateInfo.shapeHeight, visibleLight, viewportSize, updateInfo.normalBias, shadowFilteringQuality, usesReversedZBuffer,
        //                     out shadowRequest.view, out Matrix4x4 invViewProjection, out shadowRequest.projection,
        //                     out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip,
        //                     out shadowRequest.splitData
        //                 );
        //
        //                 // Assign all setting common to every lights
        //                 SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
        //                     lightIndex, lightType, shadowFilteringQuality, ref updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
        //             }
        //         }
        //     }
        // }
    //Data of VisibleLight that has been processed / evaluated.
    internal struct HDProcessedVisibleLight
    {
        public int dataIndex;
        public GPULightType gpuLightType;
        public HDLightType lightType;
        public float lightDistanceFade;
        public float lightVolumetricDistanceFade;
        public float distanceToCamera;
        public HDProcessedVisibleLightsBuilder.ShadowMapFlags shadowMapFlags;
        public bool isBakedShadowMask;
    }

    //Class representing lights in the context of a view.
    internal partial class HDProcessedVisibleLightsBuilder
    {
        #region internal HDRP API
        [Flags]
        internal enum ShadowMapFlags
        {
            None = 0,
            WillRenderShadowMap = 1 << 0,
            WillRenderScreenSpaceShadow = 1 << 1,
            WillRenderRayTracedShadow = 1 << 2
        }

        //Member lights counts
        public int sortedLightCounts => m_ProcessVisibleLightCounts.IsCreated ? m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ProcessedLights] : 0;
        public int sortedDirectionalLightCounts => m_ProcessVisibleLightCounts.IsCreated ? m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.DirectionalLights] : 0;
        public int sortedNonDirectionalLightCounts => sortedLightCounts - sortedDirectionalLightCounts;
        public int bakedShadowsCount => m_ProcessVisibleLightCounts.IsCreated ? m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.BakedShadows] : 0;
        public int sortedDGILightCounts => m_ProcessDynamicGILightCounts.IsCreated ? m_ProcessDynamicGILightCounts[(int)ProcessLightsCountSlots.ProcessedLights] : 0;

        //Indexed by VisibleLights
        public NativeArray<LightBakingOutput> visibleLightBakingOutput => m_VisibleLightBakingOutput;
        public NativeArray<LightShadowCasterMode> visibleLightShadowCasterMode => m_VisibleLightShadowCasterMode;
        public NativeArray<int> visibleLightEntityDataIndices => m_VisibleLightEntityDataIndices;
        public NativeArray<HDProcessedVisibleLight> processedEntities => m_ProcessedEntities;
        public NativeArray<float> visibleLightBounceIntensity => m_VisibleLightBounceIntensity;

        //Indexed by sorted lights.
        public NativeArray<uint> sortKeys => m_SortKeys;
        public NativeArray<uint> sortKeysDGI => m_SortKeysDGI;
        public NativeArray<uint> sortSupportArray => m_SortSupportArray;
        public NativeArray<int> shadowLightsDataIndices => m_ShadowLightsDataIndices;

        //Other
        public NativeArray<VisibleLight> offscreenDynamicGILights => m_OffscreenDynamicGILights;

        //Resets internal size of processed lights.
        public void Reset()
        {
            m_Size = 0;
        }

        //Builds sorted HDProcessedVisibleLight structures.
        public void Build(
            HDCamera hdCamera,
            in CullingResults cullingResult,
            in SortedList<uint, Light> allDGIEnabledLights,
            HDShadowManager shadowManager,
            in HDShadowInitParameters inShadowInitParameters,
            in AOVRequestData aovRequestData,
            in GlobalLightLoopSettings lightLoopSettings,
            DebugDisplaySettings debugDisplaySettings,
            bool processDynamicGI)
        {
            BuildVisibleLightEntities(cullingResult, allDGIEnabledLights, processDynamicGI);

            if (m_Size == 0)
                return;

            FilterVisibleLightsByAOV(aovRequestData);
            StartProcessVisibleLightJob(hdCamera, cullingResult.visibleLights, m_OffscreenDynamicGILights, lightLoopSettings, debugDisplaySettings, processDynamicGI);
            CompleteProcessVisibleLightJob();
            SortLightKeys();
            ProcessShadows(hdCamera, shadowManager, inShadowInitParameters, cullingResult);
        }

        #endregion

        #region private definitions

        private enum ProcessLightsCountSlots
        {
            ProcessedLights,
            DirectionalLights,
            PunctualLights,
            AreaLightCounts,
            ShadowLights,
            BakedShadows,
        }

        private const int ArrayCapacity = 32;

        private NativeArray<int> m_ProcessVisibleLightCounts;
        private NativeArray<int> m_ProcessDynamicGILightCounts;
        private NativeArray<int> m_VisibleLightEntityDataIndices;
        private NativeArray<uint> m_VisibleLightIDs;
        private NativeArray<VisibleLight> m_OffscreenDynamicGILights;
        private NativeArray<LightBakingOutput> m_VisibleLightBakingOutput;
        private NativeArray<LightShadowCasterMode> m_VisibleLightShadowCasterMode;
        private NativeArray<LightShadows> m_VisibleLightShadows;
        private NativeArray<float> m_VisibleLightBounceIntensity;
        private NativeArray<HDProcessedVisibleLight> m_ProcessedEntities;

        private int m_Capacity = 0;
        private int m_Size = 0;


        private int m_OffscreenDgiIndicesCapacity;
        private int m_OffscreenDgiIndicesSize;

        private NativeArray<uint> m_SortKeys;
        private NativeArray<uint> m_SortKeysDGI;
        private NativeArray<uint> m_SortSupportArray;
        private NativeArray<int> m_ShadowLightsDataIndices;

        private void ResizeArrays(int newCapacity)
        {
            m_Capacity = Math.Max(Math.Max(newCapacity, ArrayCapacity), m_Capacity * 2);
            m_VisibleLightEntityDataIndices.ResizeArray(m_Capacity);
            m_VisibleLightIDs.ResizeArray(m_Capacity);
            m_OffscreenDynamicGILights.ResizeArray(m_Capacity);
            m_VisibleLightBakingOutput.ResizeArray(m_Capacity);
            m_VisibleLightShadowCasterMode.ResizeArray(m_Capacity);
            m_VisibleLightShadows.ResizeArray(m_Capacity);
            m_VisibleLightBounceIntensity.ResizeArray(m_Capacity);

            m_ProcessedEntities.ResizeArray(m_Capacity);
            m_SortKeys.ResizeArray(m_Capacity);
            m_SortKeysDGI.ResizeArray(m_Capacity);
            m_ShadowLightsDataIndices.ResizeArray(m_Capacity);
        }

        public void Cleanup()
        {
            if (m_SortSupportArray.IsCreated)
                m_SortSupportArray.Dispose();

            if (m_ProcessVisibleLightCounts.IsCreated)
                m_ProcessVisibleLightCounts.Dispose();

            if (m_ProcessDynamicGILightCounts.IsCreated)
                m_ProcessDynamicGILightCounts.Dispose();

            if (m_Capacity == 0)
                return;

            m_VisibleLightEntityDataIndices.Dispose();
            m_VisibleLightIDs.Dispose();
            m_OffscreenDynamicGILights.Dispose();
            m_VisibleLightBakingOutput.Dispose();
            m_VisibleLightShadowCasterMode.Dispose();
            m_VisibleLightShadows.Dispose();
            m_VisibleLightBounceIntensity.Dispose();

            m_ProcessedEntities.Dispose();
            m_SortKeys.Dispose();
            m_SortKeysDGI.Dispose();
            m_ShadowLightsDataIndices.Dispose();

            m_Capacity = 0;
            m_Size = 0;
        }

        #endregion
    }
}
