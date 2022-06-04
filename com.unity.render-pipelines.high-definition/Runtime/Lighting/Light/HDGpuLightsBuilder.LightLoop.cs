using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDGpuLightsBuilder
    {
        #region internal HDRP API

        //Preallocates number of lights for bounds arrays and resets all internal counters. Must be called once per frame per view always.
        public void NewFrame(HDCamera hdCamera, int maxLightCount)
        {
            int viewCounts = hdCamera.viewCount;
            if (viewCounts > m_LighsPerViewCapacity)
            {
                m_LighsPerViewCapacity = viewCounts;
                m_LightsPerView.ResizeArray(m_LighsPerViewCapacity);
            }

            m_LightsPerViewCount = viewCounts;

            int totalBoundsCount = maxLightCount * viewCounts;
            int requestedBoundsCount = Math.Max(totalBoundsCount, 1);
            if (requestedBoundsCount > m_LightBoundsCapacity)
            {
                m_LightBoundsCapacity = Math.Max(Math.Max(m_LightBoundsCapacity * 2, requestedBoundsCount), ArrayCapacity);
                m_LightBounds.ResizeArray(m_LightBoundsCapacity);
                m_LightVolumes.ResizeArray(m_LightBoundsCapacity);
            }
            m_LightBoundsCount = totalBoundsCount;

            m_BoundsEyeDataOffset = maxLightCount;

            for (int viewId = 0; viewId < viewCounts; ++viewId)
            {
                m_LightsPerView[viewId] = new LightsPerView()
                {
                    worldToView = HDRenderPipeline.GetWorldToViewMatrix(hdCamera, viewId),
                    boundsOffset = viewId * m_BoundsEyeDataOffset,
                    boundsCount = 0
                };
            }

            int numLightTypes = Enum.GetValues(typeof(GPULightTypeCountSlots)).Length;
            if (!m_LightTypeCounters.IsCreated)
                m_LightTypeCounters.ResizeArray(numLightTypes);
            if (!m_DGILightTypeCounters.IsCreated)
                m_DGILightTypeCounters.ResizeArray(numLightTypes);

            m_LightCount = 0;
            m_DGILightCount = 0;
            m_ContactShadowIndex = 0;
            m_ScreenSpaceShadowIndex = 0;
            m_ScreenSpaceShadowChannelSlot = 0;
            m_ScreenSpaceShadowsUnion.Clear();

            m_CurrentShadowSortedSunLightIndex = -1;
            m_CurrentSunLightAdditionalLightData = null;
            m_CurrentSunShadowMapFlags = HDProcessedVisibleLightsBuilder.ShadowMapFlags.None;

            m_DebugSelectedLightShadowIndex = -1;
            m_DebugSelectedLightShadowCount = 0;

            for (int i = 0; i < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots; ++i)
            {
                m_CurrentScreenSpaceShadowData[i].additionalLightData = null;
                m_CurrentScreenSpaceShadowData[i].lightDataIndex = -1;
                m_CurrentScreenSpaceShadowData[i].valid = false;
            }

            for (int i = 0; i < numLightTypes; ++i)
            {
                m_LightTypeCounters[i] = 0;
                m_DGILightTypeCounters[i] = 0;
            }
        }

        //Builds the GPU light list.
        public void Build(
            CommandBuffer cmd,
            HDCamera hdCamera,
            in CullingResults cullingResult,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities,
            in HDShadowInitParameters shadowInitParams,
            DebugDisplaySettings debugDisplaySettings,
            HDRenderPipeline.HierarchicalVarianceScreenSpaceShadowsData hierarchicalVarianceScreenSpaceShadowsData,
            bool processVisibleLights,
            bool processDynamicGI)
        {
            int totalVisibleLightsCount = processVisibleLights ? visibleLights.sortedLightCounts : 0;
            int visibleLightsCount = processVisibleLights ? visibleLights.sortedNonDirectionalLightCounts : 0;
            int visibleDirectionalCount = processVisibleLights ? visibleLights.sortedDirectionalLightCounts : 0;

            int dgiLightsCount = processDynamicGI ? visibleLights.sortedDGILightCounts : 0;

            AllocateLightData(visibleLightsCount, visibleDirectionalCount, dgiLightsCount);

            // TODO: Refactor shadow management
            // The good way of managing shadow:
            // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
            // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
            // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
            // will be use...)
            // The lightLoop is in charge, not the shadow pass.
            // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

            if (totalVisibleLightsCount > 0 || dgiLightsCount > 0)
            {
                for (int viewId = 0; viewId < hdCamera.viewCount; ++viewId)
                {
                    var viewInfo = m_LightsPerView[viewId];
                    viewInfo.boundsCount += lightsCount;
                    m_LightsPerView[viewId] = viewInfo;
                }

                var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
                StartCreateGpuLightDataJob(hdCamera, cullingResult, hdShadowSettings, visibleLights, lightEntities);
                CompleteGpuLightDataJob();
                CalculateAllLightDataTextureInfo(cmd, hdCamera, cullingResult, visibleLights, lightEntities, hdShadowSettings, shadowInitParams, debugDisplaySettings, hierarchicalVarianceScreenSpaceShadowsData);
            }

            //Sanity check
            Debug.Assert(m_DirectionalLightCount == visibleDirectionalCount, "Mismatch in Directional gpu lights processed. Lights should not be culled in this loop.");
            Debug.Assert(m_LightCount == areaLightCount + punctualLightCount, "Mismatch in Area and Punctual gpu Visible lights processed. Lights should not be culled in this loop.");
            Debug.Assert(m_DGILightCount == dgiAreaLightCount + dgiPunctualLightCount, "Mismatch in Area and Punctual gpu Dynamic GI lights processed. Lights should not be culled in this loop.");
        }

        //Calculates a shadow type for a light and sets the shadow index information into the LightData.
        public void ProcessLightDataShadowIndex(
            CommandBuffer cmd,
            in HDShadowInitParameters shadowInitParams,
            HDLightType lightType,
            Light lightComponent,
            HDAdditionalLightData additionalLightData,
            int shadowIndex,
            ref LightData lightData)
        {
            if (lightData.lightType == GPULightType.ProjectorBox && shadowIndex >= 0)
            {
                // We subtract a bit from the safe extent depending on shadow resolution
                float shadowRes = additionalLightData.shadowResolution.Value(shadowInitParams.shadowResolutionPunctual);
                shadowRes = Mathf.Clamp(shadowRes, 128.0f, 2048.0f); // Clamp in a somewhat plausible range.
                // The idea is to subtract as much as 0.05 for small resolutions.
                float shadowResFactor = Mathf.Lerp(0.05f, 0.01f, Mathf.Max(shadowRes / 2048.0f, 0.0f));
                lightData.boxLightSafeExtent = 1.0f - shadowResFactor;
            }

            if (lightComponent != null &&
                (
                    (lightType == HDLightType.Spot && (lightComponent.cookie != null || additionalLightData.IESPoint != null)) ||
                    ((lightType == HDLightType.Area && lightData.lightType == GPULightType.Rectangle) && (lightComponent.cookie != null || additionalLightData.IESSpot != null)) ||
                    (lightType == HDLightType.Point && (lightComponent.cookie != null || additionalLightData.IESPoint != null))
                )
            )
            {
                switch (lightType)
                {
                    case HDLightType.Spot:
                        lightData.cookieMode = (lightComponent.cookie?.wrapMode == TextureWrapMode.Repeat) ? CookieMode.Repeat : CookieMode.Clamp;
                        if (additionalLightData.IESSpot != null && lightComponent.cookie != null && additionalLightData.IESSpot != lightComponent.cookie)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie, additionalLightData.IESSpot);
                        else if (lightComponent.cookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie);
                        else if (additionalLightData.IESSpot != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, additionalLightData.IESSpot);
                        else
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
                        break;
                    case HDLightType.Point:
                        lightData.cookieMode = CookieMode.Repeat;
                        if (additionalLightData.IESPoint != null && lightComponent.cookie != null && additionalLightData.IESPoint != lightComponent.cookie)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie, additionalLightData.IESPoint);
                        else if (lightComponent.cookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie);
                        else if (additionalLightData.IESPoint != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, additionalLightData.IESPoint);
                        break;
                    case HDLightType.Area:
                        lightData.cookieMode = CookieMode.Clamp;
                        if (additionalLightData.areaLightCookie != null && additionalLightData.IESSpot != null && additionalLightData.areaLightCookie != additionalLightData.IESSpot)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie, additionalLightData.IESSpot);
                        else if (additionalLightData.IESSpot != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.IESSpot);
                        else if (additionalLightData.areaLightCookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie);
                        break;
                }
            }
            else if (lightType == HDLightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieMode = CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
            }
            else if (lightData.lightType == GPULightType.Rectangle)
            {
                if (additionalLightData.areaLightCookie != null || additionalLightData.IESPoint != null)
                {
                    lightData.cookieMode = CookieMode.Clamp;
                    if (additionalLightData.areaLightCookie != null && additionalLightData.IESSpot != null && additionalLightData.areaLightCookie != additionalLightData.IESSpot)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie, additionalLightData.IESSpot);
                    else if (additionalLightData.IESSpot != null)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.IESSpot);
                    else if (additionalLightData.areaLightCookie != null)
                        lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie);
                }
            }

            lightData.shadowIndex = shadowIndex;
            additionalLightData.shadowIndex = shadowIndex;
        }


        #endregion


        // The first rendered 24 lights that have contact shadow enabled have a mask used to select the bit that contains
        // the contact shadow shadowed information (occluded or not). Otherwise -1 is written
        private void GetContactShadowMask(HDAdditionalLightData hdAdditionalLightData, BoolScalableSetting contactShadowEnabled, HDCamera hdCamera, ref int contactShadowMask, ref float rayTracingShadowFlag)
        {
            contactShadowMask = 0;
            rayTracingShadowFlag = 0.0f;
            // If contact shadows are not enabled or we already reached the manimal number of contact shadows
            // or this is not rasterization
            if ((!hdAdditionalLightData.useContactShadow.Value(contactShadowEnabled))
                || m_ContactShadowIndex >= LightDefinitions.s_LightListMaxPrunedEntries)
                return;

            // Evaluate the contact shadow index of this light
            contactShadowMask = 1 << m_ContactShadowIndex++;

            // If this light has ray traced contact shadow
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && hdAdditionalLightData.rayTraceContactShadow)
                rayTracingShadowFlag = 1.0f;
        }

        private bool EnoughScreenSpaceShadowSlots(GPULightType gpuLightType, int screenSpaceChannelSlot)
        {
            if (gpuLightType == GPULightType.Rectangle)
            {
                // Area lights require two shadow slots
                return (screenSpaceChannelSlot + 1) < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
            else
            {
                return screenSpaceChannelSlot < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots;
            }
        }

        private void CalculateDirectionalLightDataTextureInfo(
            ref DirectionalLightData lightData, CommandBuffer cmd, in VisibleLight light, in Light lightComponent, in HDAdditionalLightData additionalLightData,
            HDCamera hdCamera, HDProcessedVisibleLightsBuilder.ShadowMapFlags shadowFlags, int lightDataIndex, int shadowIndex)
        {
            if (shadowIndex != -1)
            {
                if ((shadowFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderScreenSpaceShadow) != 0)
                {
                    lightData.screenSpaceShadowIndex = m_ScreenSpaceShadowChannelSlot;
                    bool willRenderRtShadows = (shadowFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderRayTracedShadow) != 0;
                    if (additionalLightData.colorShadow && willRenderRtShadows)
                    {
                        m_ScreenSpaceShadowChannelSlot += 3;
                        lightData.screenSpaceShadowIndex |= (int)LightDefinitions.s_ScreenSpaceColorShadowFlag;
                    }
                    else
                    {
                        m_ScreenSpaceShadowChannelSlot++;
                    }

                    // Raise the ray tracing flag in case the light is ray traced
                    if (willRenderRtShadows)
                        lightData.screenSpaceShadowIndex |= (int)LightDefinitions.s_RayTracedScreenSpaceShadowFlag;

                    m_ScreenSpaceShadowChannelSlot++;
                    m_ScreenSpaceShadowsUnion.Add(additionalLightData);
                }
                m_CurrentSunLightAdditionalLightData = additionalLightData;
                m_CurrentSunLightDirectionalLightData = lightData;
                m_CurrentShadowSortedSunLightIndex = lightDataIndex;
                m_CurrentSunShadowMapFlags = shadowFlags;
            }

            if (lightComponent != null && lightComponent.cookie != null)
            {
                lightData.cookieMode = lightComponent.cookie.wrapMode == TextureWrapMode.Repeat ? CookieMode.Repeat : CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, lightComponent.cookie);
            }
            else
            {
                lightData.cookieMode = CookieMode.None;
            }

            if (additionalLightData.surfaceTexture == null)
            {
                lightData.surfaceTextureScaleOffset = Vector4.zero;
            }
            else
            {
                lightData.surfaceTextureScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, additionalLightData.surfaceTexture);
            }

            GetContactShadowMask(additionalLightData, HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset), hdCamera, ref lightData.contactShadowMask, ref lightData.isRayTracedContactShadow);

            lightData.shadowIndex = shadowIndex;
        }

        private void CalculateLightDataTextureInfo(
            ref LightData lightData, CommandBuffer cmd, in Light lightComponent, HDAdditionalLightData additionalLightData, in HDShadowInitParameters shadowInitParams,
            in HDCamera hdCamera, BoolScalableSetting contactShadowScalableSetting,
            HDLightType lightType, HDProcessedVisibleLightsBuilder.ShadowMapFlags shadowFlags, bool rayTracingEnabled, int lightDataIndex, int shadowIndex, GPULightType gpuLightType, HDRenderPipeline.HierarchicalVarianceScreenSpaceShadowsData hierarchicalVarianceScreenSpaceShadowsData)
        {
            ProcessLightDataShadowIndex(
                cmd,
                shadowInitParams,
                lightType,
                lightComponent,
                additionalLightData,
                shadowIndex,
                ref lightData);

            GetContactShadowMask(additionalLightData, contactShadowScalableSetting, hdCamera, ref lightData.contactShadowMask, ref lightData.isRayTracedContactShadow);

            // If there is still a free slot in the screen space shadow array and this needs to render a screen space shadow
            if (rayTracingEnabled
                && EnoughScreenSpaceShadowSlots(lightData.lightType, m_ScreenSpaceShadowChannelSlot)
                && (shadowFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderScreenSpaceShadow) != 0)
            {
                if (lightData.lightType == GPULightType.Rectangle)
                {
                    // Rectangle area lights require 2 consecutive slots.
                    // Meaning if (screenSpaceChannelSlot % 4 ==3), we'll need to skip a slot
                    // so that the area shadow gets the first two slots of the next following texture
                    if (m_ScreenSpaceShadowChannelSlot % 4 == 3)
                    {
                        m_ScreenSpaceShadowChannelSlot++;
                    }
                }

                // Bind the next available slot to the light
                lightData.screenSpaceShadowIndex = m_ScreenSpaceShadowChannelSlot;

                // Keep track of the screen space shadow data
                m_CurrentScreenSpaceShadowData[m_ScreenSpaceShadowIndex].additionalLightData = additionalLightData;
                m_CurrentScreenSpaceShadowData[m_ScreenSpaceShadowIndex].lightDataIndex = lightDataIndex;
                m_CurrentScreenSpaceShadowData[m_ScreenSpaceShadowIndex].valid = true;
                m_ScreenSpaceShadowsUnion.Add(additionalLightData);

                // increment the number of screen space shadows
                m_ScreenSpaceShadowIndex++;

                // Based on the light type, increment the slot usage
                if (lightData.lightType == GPULightType.Rectangle)
                    m_ScreenSpaceShadowChannelSlot += 2;
                else
                    m_ScreenSpaceShadowChannelSlot++;
            }

            lightData.hierarchicalVarianceScreenSpaceShadowsIndex = -1;
            if ((gpuLightType == GPULightType.Point) || (gpuLightType == GPULightType.Spot))
            {
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.HierarchicalVarianceScreenSpaceShadows)
                    && additionalLightData.useHierarchicalVarianceScreenSpaceShadows
                    && hierarchicalVarianceScreenSpaceShadowsData != null)
                {
                    float lightDepthVS = Vector3.Dot(hdCamera.camera.transform.forward, lightData.positionRWS);
                    lightData.hierarchicalVarianceScreenSpaceShadowsIndex = hierarchicalVarianceScreenSpaceShadowsData.Push(lightData.positionRWS, lightDepthVS, lightData.range);
                }
            }
        }

        internal static Unity.Profiling.ProfilerMarker calculateLightDataTextureInfoMarker = new ProfilerMarker("CalculateLightDataTextureInfo");

        private unsafe void CalculateAllLightDataTextureInfo(
            CommandBuffer cmd,
            HDCamera hdCamera,
            in CullingResults cullResults,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities,
            HDShadowSettings hdShadowSettings,
            in HDShadowInitParameters shadowInitParams,
            DebugDisplaySettings debugDisplaySettings,
            HDRenderPipeline.HierarchicalVarianceScreenSpaceShadowsData hierarchicalVarianceScreenSpaceShadowsData)
        {
            BoolScalableSetting contactShadowScalableSetting = HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset);
            bool rayTracingEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
            HDProcessedVisibleLight* processedLightArrayPtr = (HDProcessedVisibleLight*)visibleLights.processedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
            LightData* lightArrayPtr = (LightData*)m_Lights.GetUnsafePtr<LightData>();
            DirectionalLightData* directionalLightArrayPtr = (DirectionalLightData*)m_DirectionalLights.GetUnsafePtr<DirectionalLightData>();
            LightData* dgiLightArrayPtr = (LightData*)m_DGILights.GetUnsafePtr<LightData>();
            VisibleLight* visibleLightsArrayPtr = (VisibleLight*)cullResults.visibleLights.GetUnsafePtr<VisibleLight>();
            var shadowFilteringQuality = m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality;

            int directionalLightCount = visibleLights.sortedDirectionalLightCounts;
            int lightCounts = visibleLights.sortedLightCounts;
            NativeArray<int> shadowIndices = new NativeArray<int>(lightCounts, Allocator.Temp);
            HDAdditionalLightData.CalculateShadowIndices(hdCamera, in cullResults, visibleLights, lightEntities, hdShadowSettings, debugDisplaySettings,
                m_ShadowManager, m_Asset, shadowIndices, ref m_DebugSelectedLightShadowIndex, ref m_DebugSelectedLightShadowCount);

            using (calculateLightDataTextureInfoMarker.Auto())
            {
                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; ++sortKeyIndex)
                {
                    uint sortKey = visibleLights.sortKeys[sortKeyIndex];
                    LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                    GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                    LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                    int lightIndex = (int)(sortKey & 0xFFFF);

                    int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        continue;

                    HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                    if (additionalLightData == null)
                        continue;

                    //We utilize a raw light data pointer to avoid copying the entire structure
                    HDProcessedVisibleLight* processedEntityPtr = processedLightArrayPtr + lightIndex;
                    ref HDProcessedVisibleLight processedEntity = ref UnsafeUtility.AsRef<HDProcessedVisibleLight>(processedEntityPtr);
                    HDLightType lightType = processedEntity.lightType;

                    Light lightComponent = additionalLightData.legacyLight;

                    int shadowIndex = shadowIndices[sortKeyIndex];
                    if (gpuLightType == GPULightType.Directional)
                    {
                        VisibleLight* visibleLightPtr = visibleLightsArrayPtr + lightIndex;
                        ref VisibleLight light = ref UnsafeUtility.AsRef<VisibleLight>(visibleLightPtr);
                        int directionalLightDataIndex = sortKeyIndex;
                        DirectionalLightData* lightDataPtr = directionalLightArrayPtr + directionalLightDataIndex;
                        ref DirectionalLightData lightData = ref UnsafeUtility.AsRef<DirectionalLightData>(lightDataPtr);
                        CalculateDirectionalLightDataTextureInfo(
                            ref lightData, cmd, light, lightComponent, additionalLightData,
                            hdCamera, processedEntity.shadowMapFlags, directionalLightDataIndex, shadowIndex);
                    }
                    else
                    {
                        int lightDataIndex = sortKeyIndex - directionalLightCount;
                        LightData* lightDataPtr = lightArrayPtr + lightDataIndex;
                        ref LightData lightData = ref UnsafeUtility.AsRef<LightData>(lightDataPtr);
                        CalculateLightDataTextureInfo(
                            ref lightData, cmd, lightComponent, additionalLightData, shadowInitParams,
                            hdCamera, contactShadowScalableSetting,
                            lightType, processedEntity.shadowMapFlags, rayTracingEnabled, lightDataIndex, shadowIndex, gpuLightType, hierarchicalVarianceScreenSpaceShadowsData);
                    }
                }

                int dgiLightCounts = visibleLights.sortedDGILightCounts;
                for (int sortKeyIndex = 0; sortKeyIndex < dgiLightCounts; ++sortKeyIndex)
                {
                    uint sortKey = visibleLights.sortKeysDGI[sortKeyIndex];
                    HDGpuLightsBuilder.UnpackLightSortKey(sortKey, out var lightCategory, out var gpuLightType, out var lightVolumeType, out var lightIndex);

                    int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        continue;

                    HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                    if (additionalLightData == null)
                        continue;

                    //We utilize a raw light data pointer to avoid copying the entire structure
                    HDProcessedVisibleLight* processedEntityPtr = processedLightArrayPtr + lightIndex;
                    ref HDProcessedVisibleLight processedEntity = ref UnsafeUtility.AsRef<HDProcessedVisibleLight>(processedEntityPtr);
                    HDLightType lightType = processedEntity.lightType;

                    Light lightComponent = additionalLightData.legacyLight;

                    // use the same shadow index from the previously computed one for visible lights
                    int shadowIndex = additionalLightData.shadowIndex;

                    if (gpuLightType != GPULightType.Directional)
                    {
                        int lightDataIndex = sortKeyIndex;
                        LightData* lightDataPtr = dgiLightArrayPtr + lightDataIndex;
                        ref LightData lightData = ref UnsafeUtility.AsRef<LightData>(lightDataPtr);
                        CalculateLightDataTextureInfo(
                            ref lightData, cmd, lightComponent, additionalLightData, shadowInitParams,
                            hdCamera, contactShadowScalableSetting,
                            lightType, processedEntity.shadowMapFlags, rayTracingEnabled, lightDataIndex, shadowIndex, gpuLightType, hierarchicalVarianceScreenSpaceShadowsData);
                    }
                }
            }

            shadowIndices.Dispose();
        }
    }


    [BurstCompile] [NoAlias]
        internal unsafe struct UpdateShadowRequestsAndCalculateIndicesJob : IJob
        {
            public HDShadowManagerUnmanaged shadowManager;

            [ReadOnly] public NativeBitArray isValidIndex;
            [ReadOnly] public NativeArray<uint> sortKeys;
            [ReadOnly] public NativeArray<int> visibleLightEntityDataIndices;
            [ReadOnly] public NativeArray<HDProcessedVisibleLight> processedEntities;
            [ReadOnly] public NativeArray<VisibleLight> visibleLights;
            [ReadOnly] public NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles;
            [ReadOnly] public NativeList<int> requestIndicesStorage;
            [ReadOnly] public NativeArray<Matrix4x4> kCubemapFaces;

            public NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos;
            public NativeList<HDShadowRequest> requestStorage;
            //public UnsafePtrList<UnsafeList<ShadowRequestDataUpdateInfo>> shadowRequestDataLists;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> cachedPointUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> cachedSpotUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> cachedAreaRectangleUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> cachedAreaOtherUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> cachedDirectionalUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> dynamicPointUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> dynamicSpotUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> dynamicAreaRectangleUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> dynamicAreaOtherUpdateInfos;
            public NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> dynamicDirectionalUpdateInfos;
            public NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage;
            public NativeList<VisibleLightAndIndices> visibleLightsAndIndicesBuffer; // sized to lightCounts
            public NativeList<VisibleLightAndIndices> splitVisibleLightsAndIndicesBuffer; // sized to lightCounts
            public NativeList<Vector4> frustumPlanesStorage;
            public NativeList<Vector3> cachedViewPositionsStorage;

            [WriteOnly] public NativeArray<int> shadowIndices;

#if UNITY_EDITOR
            [WriteOnly] public NativeArray<int> shadowRequestCounts;
#endif

            [ReadOnly] public int lightCounts;
            [ReadOnly] public int shadowSettingsCascadeShadowSplitCount;
            [ReadOnly] public CameraType cameraType;
            [ReadOnly] public int invalidIndex;
            [ReadOnly] public int sortKeysCount;

            [ReadOnly] public Vector3 worldSpaceCameraPos;
            [ReadOnly] public int shaderConfigCameraRelativeRendering;
            [ReadOnly] public HDShadowFilteringQuality shadowFilteringQuality;
            [ReadOnly] public bool usesReversedZBuffer;

            public Unity.Profiling.ProfilerMarker validIndexCalculationsMarker;
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
            public ProfilerMarker pointProfilerMarker;

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
                // if (!shadowRequestDataLists.IsCreated || shadowRequestDataLists.Length <= (int)HDLightType.Area)
                //     throw new Exception("ShadowRequestDataLists not initialized.");
                //
                // for (int i = 0; i < shadowRequestDataLists.Length; i++)
                // {
                //     if (!shadowRequestDataLists[i]->IsCreated)
                //     {
                //         throw new Exception("ShadowRequestDataLists not initialized.");
                //     }
                // }
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
                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
                {
                    if (!isValidIndex.IsSet(sortKeyIndex))
                        continue;

                    uint sortKey = sortKeys[sortKeyIndex];
                    int lightIndex = (int)(sortKey & 0xFFFF);
                    int dataIndex = visibleLightEntityDataIndices[lightIndex];

                    ref readonly HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + dataIndex);
                    HDLightType lightType = HDAdditionalLightData.TranslateLightType(visibleLights[lightIndex].lightType, lightUpdateInfo.pointLightHDType);
                    //Light lightComponent = additionalLightData.legacyLight;
                    int firstShadowRequestIndex = -1;
                    int shadowRequestCount = -1;

                    if ( /*lightComponent != null && */(processedEntities[lightIndex].shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) != 0)
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

                int bufferedDataCount = visibleLightsAndIndicesBuffer.Length;

                VisibleLightAndIndices* splitVisibleLightsAndIndicesBufferPtr = (VisibleLightAndIndices*)splitVisibleLightsAndIndicesBuffer.GetUnsafePtr();
                int buffereOffsetIterator = 0;
                UnsafeList<VisibleLightAndIndices> cachedDirectionalVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                cachedDirectionalVisibleLightsAndIndices.m_capacity = cachedDirectionalCount;
                buffereOffsetIterator += cachedDirectionalCount;
                UnsafeList<VisibleLightAndIndices> cachedAreaRectangleVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                cachedAreaRectangleVisibleLightsAndIndices.m_capacity = cachedAreaRectangleCount;
                buffereOffsetIterator += cachedAreaRectangleCount;
                UnsafeList<VisibleLightAndIndices> cachedAreaOtherVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                cachedAreaOtherVisibleLightsAndIndices.m_capacity = cachedAreaOtherCount;
                buffereOffsetIterator += cachedAreaOtherCount;
                UnsafeList<VisibleLightAndIndices> cachedPointVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                cachedPointVisibleLightsAndIndices.m_capacity = cachedPointCount;
                buffereOffsetIterator += cachedPointCount;
                UnsafeList<VisibleLightAndIndices> cachedSpotVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                cachedSpotVisibleLightsAndIndices.m_capacity = cachedSpotCount;
                buffereOffsetIterator += cachedSpotCount;

                UnsafeList<VisibleLightAndIndices> dynamicAreaRectangleVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                dynamicAreaRectangleVisibleLightsAndIndices.m_capacity = dynamicAreaRectangleCount;
                buffereOffsetIterator += dynamicAreaRectangleCount;
                UnsafeList<VisibleLightAndIndices> dynamicAreaOtherVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                dynamicAreaOtherVisibleLightsAndIndices.m_capacity = dynamicAreaOtherCount;
                buffereOffsetIterator += dynamicAreaOtherCount;
                UnsafeList<VisibleLightAndIndices> dynamicPointVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                dynamicPointVisibleLightsAndIndices.m_capacity = dynamicPointCount;
                buffereOffsetIterator += dynamicPointCount;
                UnsafeList<VisibleLightAndIndices> dynamicSpotVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                dynamicSpotVisibleLightsAndIndices.m_capacity = dynamicSpotCount;
                buffereOffsetIterator += dynamicSpotCount;
                UnsafeList<VisibleLightAndIndices> dynamicDirectionalVisibleLightsAndIndices = new UnsafeList<VisibleLightAndIndices>(splitVisibleLightsAndIndicesBufferPtr + buffereOffsetIterator, 0);
                dynamicDirectionalVisibleLightsAndIndices.m_capacity = dynamicDirectionalCount;
                buffereOffsetIterator += dynamicDirectionalCount;

                for (int bufferedDataIndex = 0; bufferedDataIndex < bufferedDataCount; bufferedDataIndex++)
                {
                    ref readonly VisibleLightAndIndices readData = ref visibleLightsAndIndicesBuffer.ElementAt(bufferedDataIndex);
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

                //ref UnsafeList<ShadowRequestDataUpdateInfo> directionalUpdateDataList = ref *shadowRequestDataLists[(int)LightType.Directional];

                for (int i = 0; i < cachedDirectionalCount; i++)
                {
                    ref readonly VisibleLightAndIndices visibleLightAndIndices = ref cachedDirectionalVisibleLightsAndIndices.ElementAt(i);

                    HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[visibleLightAndIndices.dataIndex];
                    ShadowMapUpdateType cachedDirectionalUpdateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode, visibleLightAndIndices.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    for (int index = 0; index < visibleLightAndIndices.shadowRequestCount; index++)
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
                        ref HDAdditionalLightData.ShadowRequestDataUpdateInfo updateInfo = ref cachedDirectionalUpdateInfos.ElementAt(updateDataListIndex);
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent] = true;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = visibleLightAndIndices.dataIndex;
                        updateInfo.updateType = cachedDirectionalUpdateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = visibleLightAndIndices.lightIndex;
                        if (shadowRequestIndex < shadowManagerRequestCount)
                        {
                            shadowManager.cascadeShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                        }
                    }
                }

                for (int i = 0; i < dynamicDirectionalCount; i++)
                {
                    ref readonly VisibleLightAndIndices visibleLightAndIndices = ref dynamicDirectionalVisibleLightsAndIndices.ElementAt(i);

                    HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[visibleLightAndIndices.dataIndex];
                    int count = HDAdditionalLightData.GetShadowRequestCount(shadowSettingsCascadeShadowSplitCount, HDLightType.Directional);
                    var updateType = HDAdditionalLightData.GetShadowUpdateType(HDLightType.Directional, visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode, visibleLightAndIndices.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode);
                    bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                    // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows

                    for (int index = 0; index < visibleLightAndIndices.shadowRequestCount; index++)
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
                        ref HDAdditionalLightData.ShadowRequestDataUpdateInfo updateInfo = ref dynamicDirectionalUpdateInfos.ElementAt(updateDataListIndex);
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent] = hasCachedComponent;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = visibleLightAndIndices.dataIndex;
                        updateInfo.updateType = updateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = visibleLightAndIndices.lightIndex;
                        if (shadowRequestIndex < shadowManagerRequestCount)
                        {
                            shadowManager.cascadeShadowAtlas.shadowRequests.Add(shadowRequestSetHandle[index]);
                        }
                    }
                }

                //ref UnsafeList<ShadowRequestDataUpdateInfo> areaUpdateDataList = ref *shadowRequestDataLists[(int)HDLightType.Area];

                // Update cached area rectangle:
                using (cachedAreaRectangleRequestsMarker.Auto())
                UpdateNonDirectionalCachedRequests(ShadowMapType.AreaLightAtlas, HDLightType.Area,
                    cachedAreaRectangleVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedAreaRectangleUpdateInfos,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowRequests, shadowManager.areaShadowAtlas.shadowRequests,
                    shadowManager.areaShadowAtlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.areaShadowAtlas.transformCaches, shadowManager.cachedShadowManager.areaShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.areaShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.areaShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.areaShadowAtlas.placedShadows, cachedAreaRectangleCount, shadowManagerRequestCount);

                // Update cached area other:
                using (cachedAreaOtherRequestsMarker.Auto())
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Area,
                    cachedAreaOtherVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedAreaOtherUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.areaShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedAreaOtherCount, shadowManagerRequestCount);

                //ref UnsafeList<ShadowRequestDataUpdateInfo> pointUpdateDataList = ref *shadowRequestDataLists[(int)HDLightType.Point];

                // Update cached point:
                using (cachedPointRequestsMarker.Auto())
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Point,
                    cachedPointVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedPointUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.punctualShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedPointCount, shadowManagerRequestCount);

                //ref UnsafeList<ShadowRequestDataUpdateInfo> spotUpdateDataList = ref *shadowRequestDataLists[(int)HDLightType.Spot];

                // Update cached spot:
                using (cachedSpotRequestsMarker.Auto())
                UpdateNonDirectionalCachedRequests(ShadowMapType.PunctualAtlas, HDLightType.Spot,
                    cachedSpotVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, cachedSpotUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, shadowManager.cachedShadowManager.punctualShadowAtlas.transformCaches, shadowManager.cachedShadowManager.punctualShadowAtlas.registeredLightDataPendingPlacement,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.recordsPendingPlacement, shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsPendingRendering,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowsWithValidData, shadowManager.cachedShadowManager.punctualShadowAtlas.placedShadows, cachedSpotCount, shadowManagerRequestCount);

                // Update dynamic area rectangle:
                using (dynamicAreaRectangleRequestsMarker.Auto())
                UpdateNonDirectionalDynamicRequests(ShadowMapType.AreaLightAtlas, HDLightType.Area,
                    dynamicAreaRectangleVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicAreaRectangleUpdateInfos,
                    shadowManager.cachedShadowManager.areaShadowAtlas.shadowRequests, shadowManager.areaShadowAtlas.shadowRequests,
                    shadowManager.areaShadowAtlas.mixedRequestsPendingBlits, dynamicAreaRectangleCount, shadowManagerRequestCount);

                // Update dynamic area other:
                using (dynamicAreaOtherRequestsMarker.Auto())
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Area,
                    dynamicAreaOtherVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicAreaOtherUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicAreaOtherCount, shadowManagerRequestCount);

                // Update dynamic point:
                using (dynamicPointRequestsMarker.Auto())
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Point,
                    dynamicPointVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicPointUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicPointCount, shadowManagerRequestCount);

                // Update dynamic point:
                using (dynamicSpotRequestsMarker.Auto())
                UpdateNonDirectionalDynamicRequests(ShadowMapType.PunctualAtlas, HDLightType.Spot,
                    dynamicSpotVisibleLightsAndIndices, packedShadowRequestSetHandles, requestStorage, requestIndicesStorage, hdShadowResolutionRequestStorage, dynamicSpotUpdateInfos,
                    shadowManager.cachedShadowManager.punctualShadowAtlas.shadowRequests, shadowManager.atlas.shadowRequests,
                    shadowManager.atlas.mixedRequestsPendingBlits, dynamicSpotCount, shadowManagerRequestCount);

                UpdateCachedPointShadowRequestsAndResolutionRequests();
                UpdateDynamicPointShadowRequestsAndResolutionRequests();

                UpdateCachedSpotShadowRequestsAndResolutionRequests();
                UpdateDynamicSpotShadowRequestsAndResolutionRequests();

                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaRectangleUpdateInfos);
                UpdateCachedAreaShadowRequestsAndResolutionRequests(cachedAreaOtherUpdateInfos);

                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaRectangleUpdateInfos);
                UpdateDynamicAreaShadowRequestsAndResolutionRequests(dynamicAreaOtherUpdateInfos);

                //                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
 //                {
 //                    if (!isValidIndex[sortKeyIndex])
 //                        continue;
 //                    uint sortKey = sortKeys[sortKeyIndex];
 //                    int lightIndex = (int)(sortKey & 0xFFFF);
 //                    int dataIndex = visibleLightEntityDataIndices[lightIndex];
 //                    ref HDAdditionalLightDataUpdateInfo lightUpdateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + dataIndex);
 //                    VisibleLight visibleLight = visibleLights[lightIndex];
 //                    HDLightType lightType = TranslateLightType(visibleLight.lightType, lightUpdateInfo.pointLightHDType);
 //                    //Light lightComponent = additionalLightData.legacyLight;
 //                    int firstShadowRequestIndex = -1;
 //                    int shadowRequestCount = -1;
 //                    if (/*lightComponent != null && */(processedEntities[lightIndex].shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) != 0)
 //                    {
 //                        shadowRequestCount = 0;
 //                        ShadowMapType shadowMapType = GetShadowMapType(lightType, lightUpdateInfo.areaLightShape);
 //                        int count = GetShadowRequestCount(shadowSettingsCascadeShadowSplitCount, lightType);
 //                        var updateType = GetShadowUpdateType(lightType, lightUpdateInfo.shadowUpdateMode, lightUpdateInfo.alwaysDrawDynamicShadows);
 //                        bool hasCachedComponent = !ShadowIsUpdatedEveryFrame(lightUpdateInfo.shadowUpdateMode);
 //                        bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
 //                        bool needsRenderingDueToTransformChange = false;
 //                        // Note if we are in cached system, but if a placement has not been found by this point we bail out shadows
 //                        bool shadowHasAtlasPlacement = true;
 //                        if (hasCachedComponent)
 //                        {
 //                            // If we force evicted the light, it will have lightIdxForCachedShadows == -1
 //                            shadowHasAtlasPlacement =
 //                                !shadowManager.cachedShadowManager.LightIsPendingPlacement(lightUpdateInfo.lightIdxForCachedShadows, shadowMapType) &&
 //                                (lightUpdateInfo.lightIdxForCachedShadows != -1);
 //                            needsRenderingDueToTransformChange =
 //                                shadowManager.cachedShadowManager.NeedRenderingDueToTransformChange(in lightUpdateInfo, in visibleLight, lightType);
 //                        }
 //
 //                        ref UnsafeList<ShadowRequestDataUpdateInfo> updateDataList = ref *shadowRequestDataLists[(int)lightType];
 //                        HDShadowRequestSetHandle shadowRequestSetHandle = packedShadowRequestSetHandles[dataIndex];
 //                        for (int index = 0; index < count; index++)
 //                        {
 //                            HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
 //                            ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);
 //
 //                            int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
 //                            HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);
 //                            if (!resolutionRequestHandle.valid)
 //                                continue;
 //                            ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);
 //                            int cachedShadowID = lightUpdateInfo.lightIdxForCachedShadows + index;
 //                            bool needToUpdateCachedContent = false;
 //                            //bool needToUpdateDynamicContent = !isSampledFromCache;
 //                            //bool hasUpdatedRequestData = false;
 //                            if (hasCachedComponent && shadowHasAtlasPlacement)
 //                            {
 //                                needToUpdateCachedContent = needsRenderingDueToTransformChange ||
 //                                                            shadowManager.cachedShadowManager.ShadowIsPendingUpdate(cachedShadowID, shadowMapType);
 //                                shadowManager.cachedShadowManager.UpdateResolutionRequest(ref resolutionRequest, cachedShadowID,
 //                                    shadowMapType);
 //                            }
 //
 //                            shadowRequest.isInCachedAtlas = isSampledFromCache;
 //                            shadowRequest.isMixedCached = updateType == ShadowMapUpdateType.Mixed;
 //                            shadowRequest.shouldUseCachedShadowData = false;
 //                            shadowRequest.shadowMapType = shadowMapType;
 //                            shadowRequest.dynamicAtlasViewport = resolutionRequest.dynamicAtlasViewport;
 //                            shadowRequest.cachedAtlasViewport = resolutionRequest.cachedAtlasViewport;
 //                            int updateDataListIndex = updateDataList.Length;
 //                            updateDataList.Length = updateDataListIndex + 1;
 //                            ref ShadowRequestDataUpdateInfo updateInfo = ref updateDataList.ElementAt(updateDataListIndex);
 //                            updateInfo.states[ShadowRequestDataUpdateInfo.k_HasCachedComponent] = hasCachedComponent;
 //                            //updateInfo.states[ShadowRequestDataUpdateInfo.k_IsSampledFromCache] = isSampledFromCache;
 //                            //updateInfo.states[ShadowRequestDataUpdateInfo.k_ShadowHasAtlasPlacement] = shadowHasAtlasPlacement;
 //                            //updateInfo.states[ShadowRequestDataUpdateInfo.k_NeedsRenderingDueToTransformChange] = needsRenderingDueToTransformChange;
 //                            updateInfo.states[ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent] = needToUpdateCachedContent;
 //                            updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
 //                            updateInfo.additionalLightDataIndex = dataIndex;
 //                            updateInfo.updateType = updateType;
 //                            updateInfo.viewportSize = resolutionRequest.resolution;
 //                            updateInfo.lightIndex = lightIndex;
 //                            shadowManager.UpdateShadowRequest(shadowRequestIndex, shadowRequestSetHandle[index], updateType, shadowMapType);
 //                            if (needToUpdateCachedContent && (lightType != HDLightType.Directional ||
 //                                                              cameraType != CameraType.Reflection))
 //                            {
 //                                // Handshake with the cached shadow manager to notify about the rendering.
 //                                // Technically the rendering has not happened yet, but it is scheduled.
 //                                shadowManager.cachedShadowManager.MarkShadowAsRendered(cachedShadowID, shadowMapType);
 //                            }
 //                            // Store the first shadow request id to return it
 //                             if (firstShadowRequestIndex == -1)
 //                                 firstShadowRequestIndex = shadowRequestIndex;
 //                             shadowRequestCount++;
 //                        }
 //                    }
 // #if UNITY_EDITOR
 //                     shadowRequestCounts[sortKeyIndex] = shadowRequestCount;
 // #endif
 //                     shadowIndices[sortKeyIndex] = firstShadowRequestIndex;
 //                }
            }

            public void UpdateCachedPointShadowRequestsAndResolutionRequests()
            {
                int pointCount = cachedPointUpdateInfos.Length;

                using (pointProfilerMarker.Auto())
                for (int i = 0; i < pointCount; i++)
                {
                    ref readonly HDAdditionalLightData.ShadowRequestDataUpdateInfo pointUpdateInfo = ref cachedPointUpdateInfos.ElementAt(i);
                    bool needToUpdateCachedContent = pointUpdateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
                    bool hasCachedComponent = pointUpdateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent];
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

                using (pointProfilerMarker.Auto())
                for (int i = 0; i < pointCount; i++)
                {
                    ref readonly HDAdditionalLightData.ShadowRequestDataUpdateInfo pointUpdateInfo = ref dynamicPointUpdateInfos.ElementAt(i);
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
                    ref readonly HDAdditionalLightData.ShadowRequestDataUpdateInfo spotUpdateInfo = ref cachedSpotUpdateInfos.ElementAt(i);
                    bool needToUpdateCachedContent = spotUpdateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
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
                    ref readonly HDAdditionalLightData.ShadowRequestDataUpdateInfo directionalUpdateInfo = ref dynamicSpotUpdateInfos.ElementAt(i);
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

            public void UpdateCachedAreaShadowRequestsAndResolutionRequests(NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> areaUpdateInfos)
            {
                int areaCount = areaUpdateInfos.Length;

                for (int i = 0; i < areaCount; i++)
                {
                    HDAdditionalLightData.ShadowRequestDataUpdateInfo areaUpdateInfo = areaUpdateInfos[i];
                    bool needToUpdateCachedContent = areaUpdateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent];
                    bool hasCachedComponent = areaUpdateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent];
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

            public void UpdateDynamicAreaShadowRequestsAndResolutionRequests(NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> areaUpdateInfos)
            {
                int areaCount = areaUpdateInfos.Length;

                for (int i = 0; i < areaCount; i++)
                {
                    HDAdditionalLightData.ShadowRequestDataUpdateInfo areaUpdateInfo = areaUpdateInfos[i];
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
                UnsafeList<VisibleLightAndIndices> visibleLightsAndIndices, NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles,
                NativeList<HDShadowRequest> requestStorage, NativeList<int> requestIndicesStorage, NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage,
                NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> updateDataList,
                NativeList<HDShadowRequestHandle> cachedAtlasShadowRequests, NativeList<HDShadowRequestHandle> dynamicAtlasShadowRequests, NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits,
                NativeHashMap<int, HDCachedShadowAtlas.CachedTransform> transformCaches, NativeHashMap<int, HDLightRenderEntity> registeredLightDataPendingPlacement, NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> recordsPendingPlacement,
                NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> shadowsPendingRendering, NativeHashMap<int, int> shadowsWithValidData, NativeHashMap<int, HDCachedShadowAtlas.CachedShadowRecord> placedShadows,
                int count, int shadowManagerRequestCount)
            {
                for (int i = 0; i < visibleLightsAndIndices.Length; i++)
                {
                    ref readonly VisibleLightAndIndices visibleLightAndIndices = ref visibleLightsAndIndices.ElementAt(i);
                    int lightIdxForCachedShadows = visibleLightAndIndices.additionalLightUpdateInfo.lightIdxForCachedShadows;
                    // If we force evicted the light, it will have lightIdxForCachedShadows == -1
                    bool shadowHasAtlasPlacement = !(registeredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) ||
                                                     recordsPendingPlacement.ContainsKey(lightIdxForCachedShadows)) && lightIdxForCachedShadows != -1;
                    bool needsRenderingDueToTransformChange = false;
                    if (visibleLightAndIndices.additionalLightUpdateInfo.updateUponLightMovement)
                    {
                        if (transformCaches.TryGetValue(visibleLightAndIndices.additionalLightUpdateInfo.lightIdxForCachedShadows, out HDCachedShadowAtlas.CachedTransform cachedTransform))
                        {
                            float positionThreshold = visibleLightAndIndices.additionalLightUpdateInfo.cachedShadowTranslationUpdateThreshold;
                            float3 positionDiffVec = cachedTransform.position - visibleLightAndIndices.visibleLight.GetPosition();
                            float positionDiff = math.dot(positionDiffVec, positionDiffVec);
                            if (positionDiff > positionThreshold * positionThreshold)
                            {
                                needsRenderingDueToTransformChange = true;
                            }
                            float angleDiffThreshold = visibleLightAndIndices.additionalLightUpdateInfo.cachedShadowAngleUpdateThreshold;
                            float3 cachedAngles = cachedTransform.angles;
                            float3 angleDiff = cachedAngles - HDShadowUtils.QuaternionToEulerZXY(new quaternion(visibleLightAndIndices.visibleLight.localToWorldMatrix));
                            // Any angle difference
                            if (math.abs(angleDiff.x) > angleDiffThreshold || math.abs(angleDiff.y) > angleDiffThreshold || math.abs(angleDiff.z) > angleDiffThreshold)
                            {
                                needsRenderingDueToTransformChange = true;
                            }

                            if (needsRenderingDueToTransformChange)
                            {
                                // Update the record
                                cachedTransform.position = visibleLightAndIndices.visibleLight.GetPosition();
                                cachedTransform.angles = HDShadowUtils.QuaternionToEulerZXY(new quaternion(visibleLightAndIndices.visibleLight.localToWorldMatrix));
                                transformCaches[visibleLightAndIndices.additionalLightUpdateInfo.lightIdxForCachedShadows] = cachedTransform;
                            }
                        }
                    }

                    var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode, visibleLightAndIndices.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);

                    HDShadowRequestSetHandle shadowRequestSetHandle = visibleLightAndIndices.shadowRequestSetHandle;
                    for (int index = 0; index < visibleLightAndIndices.shadowRequestCount; index++)
                    {
                        HDShadowRequestHandle indexHandle = shadowRequestSetHandle[index];
                        ref var shadowRequest = ref requestStorage.ElementAt(indexHandle.storageIndexForShadowRequest);

                        int shadowRequestIndex = requestIndicesStorage[indexHandle.storageIndexForRequestIndex];
                        HDShadowResolutionRequestHandle resolutionRequestHandle = HDShadowManager.GetResolutionRequestHandle(shadowRequestIndex, shadowManagerRequestCount);
                        if (!resolutionRequestHandle.valid)
                            continue;
                        ref HDShadowResolutionRequest resolutionRequest = ref hdShadowResolutionRequestStorage.ElementAt(resolutionRequestHandle.index);
                        int cachedShadowID = visibleLightAndIndices.additionalLightUpdateInfo.lightIdxForCachedShadows + index;
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
                        ref HDAdditionalLightData.ShadowRequestDataUpdateInfo updateInfo = ref updateDataList.ElementAt(updateDataListIndex);
                        updateInfo.visibleLight = visibleLightAndIndices.visibleLight;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent] = true;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent] = needToUpdateCachedContent;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = visibleLightAndIndices.dataIndex;
                        updateInfo.updateType = updateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = visibleLightAndIndices.lightIndex;

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
                UnsafeList<VisibleLightAndIndices> visibleLightsAndIndices, NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles,
                NativeList<HDShadowRequest> requestStorage, NativeList<int> requestIndicesStorage, NativeList<HDShadowResolutionRequest> hdShadowResolutionRequestStorage,
                NativeList<HDAdditionalLightData.ShadowRequestDataUpdateInfo> updateDataList,
                NativeList<HDShadowRequestHandle> cachedAtlasShadowRequests, NativeList<HDShadowRequestHandle> dynamicAtlasShadowRequests, NativeList<HDShadowRequestHandle> mixedRequestsPendingBlits,
                int count, int shadowManagerRequestCount)
            {
                for (int i = 0; i < visibleLightsAndIndices.Length; i++)
                {
                    ref readonly VisibleLightAndIndices visibleLightAndIndices = ref visibleLightsAndIndices.ElementAt(i);
                    var updateType = HDAdditionalLightData.GetShadowUpdateType(hdLightType, visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode, visibleLightAndIndices.additionalLightUpdateInfo.alwaysDrawDynamicShadows);
                    bool hasCachedComponent = !HDAdditionalLightData.ShadowIsUpdatedEveryFrame(visibleLightAndIndices.additionalLightUpdateInfo.shadowUpdateMode);
                    bool isSampledFromCache = (updateType == ShadowMapUpdateType.Cached);
                    HDShadowRequestSetHandle shadowRequestSetHandle = visibleLightAndIndices.shadowRequestSetHandle;

                    for (int index = 0; index < visibleLightAndIndices.shadowRequestCount; index++)
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
                        ref HDAdditionalLightData.ShadowRequestDataUpdateInfo updateInfo = ref updateDataList.ElementAt(updateDataListIndex);
                        updateInfo.visibleLight = visibleLightAndIndices.visibleLight;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_HasCachedComponent] = hasCachedComponent;
                        updateInfo.states[HDAdditionalLightData.ShadowRequestDataUpdateInfo.k_NeedToUpdateCachedContent] = false;
                        updateInfo.shadowRequestHandle = shadowRequestSetHandle[index];
                        updateInfo.additionalLightDataIndex = visibleLightAndIndices.dataIndex;
                        updateInfo.updateType = updateType;
                        updateInfo.viewportSize = resolutionRequest.resolution;
                        updateInfo.lightIndex = visibleLightAndIndices.lightIndex;

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
