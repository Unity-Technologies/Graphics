using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
            HDAdditionalLightData.CalculateShadowIndices(cmd, hdCamera, in cullResults, visibleLights, lightEntities, hdShadowSettings, in shadowInitParams, debugDisplaySettings,
                hierarchicalVarianceScreenSpaceShadowsData, ManageShadowRequests, CopyShadowIndices, SkipDirectionalLights, m_ShadowManager, m_Asset,
                m_DirectionalLights, m_Lights, shadowIndices, ref m_DebugSelectedLightShadowIndex, ref m_DebugSelectedLightShadowCount);

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


}
