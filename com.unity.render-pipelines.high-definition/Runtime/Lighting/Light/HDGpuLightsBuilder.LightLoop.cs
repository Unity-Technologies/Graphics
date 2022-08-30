using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Mathematics;
using Unity.Jobs;

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

            int numLightTypes = m_LightTypeCounters.IsCreated ? m_LightTypeCounters.Length : Enum.GetValues(typeof(GPULightTypeCountSlots)).Length;
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
            EnsureScratchpadCapacity(lightCounts);

            NativeArray<int> shadowIndexResults = m_ShadowIndicesScratchpadArray;
            CalculateShadowIndices(hdCamera, in cullResults, visibleLights, lightEntities, hdShadowSettings, debugDisplaySettings,
                m_ShadowManager, m_Asset, shadowIndexResults, ref m_DebugSelectedLightShadowIndex, ref m_DebugSelectedLightShadowCount);

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CalculateLightDataTextureInfo)))
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

                    int shadowIndex = shadowIndexResults[sortKeyIndex];
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
        }

		internal unsafe void CalculateShadowIndices(
            HDCamera hdCamera,
            in CullingResults cullResults,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities,
            HDShadowSettings hdShadowSettings,
            DebugDisplaySettings debugDisplaySettings,
            HDShadowManager shadowManager, HDRenderPipelineAsset renderPipelineAsset,
            NativeArray<int> shadowIndices,
            ref int debugSelectedLightShadowIndex, ref int debugSelectedLightShadowCount)
        {
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CalculateShadowIndices)))
            {
                var shadowFilteringQuality = renderPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowFilteringQuality;

                int lightCounts = visibleLights.sortedLightCounts;

                NativeBitArray isValidIndex = m_IsValidIndexScratchpadArray;
                int invalidIndex = HDLightRenderDatabase.InvalidDataIndex;

                HDShadowManagerUnmanaged unmanagedShadowManagerData = default;
                unmanagedShadowManagerData.cachedShadowManager.cachedDirectionalAngles = m_CachedDirectionalAnglesArray;
                shadowManager.GetUnmanageDataForShadowRequestJobs(ref unmanagedShadowManagerData);

                bool usesReversedZBuffer = SystemInfo.usesReversedZBuffer;
                Vector3 worldSpaceCameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;


                HDShadowRequestDatabase shadowRequestsDatabase = lightEntities.shadowRequests;
                shadowRequestsDatabase.EnsureNativeListsAreCreated();
#if UNITY_EDITOR
                NativeArray<int> shadowRequestCounts = m_ShadowRequestCountsScratchpad;
#endif
                NativeList<Vector3> cachedViewPositionsStorage = shadowRequestsDatabase.cachedViewPositionsStorage;
                NativeList<HDShadowRequestSetHandle> packedShadowRequestSetHandles = lightEntities.packedShadowRequestSetHandles;

                NativeList<HDShadowRequest> requestStorage = shadowRequestsDatabase.hdShadowRequestStorage;
                ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();

                NativeList<int> requestIndicesStorage = shadowRequestsDatabase.hdShadowRequestIndicesStorage;

                NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos = lightEntities.additionalLightDataUpdateInfos;

                int shadowSettingsCascadeShadowSplitCount = hdShadowSettings.cascadeShadowSplitCount.value;

                UpdateShadowRequestsAndCalculateIndicesJob shadowRequestsAndIndicesJob = new UpdateShadowRequestsAndCalculateIndicesJob
                {
                    shadowManager = unmanagedShadowManagerData,

                    isValidIndex = isValidIndex,
                    sortKeys = visibleLights.sortKeys,
                    visibleLightEntityDataIndices = visibleLights.visibleLightEntityDataIndices,
                    processedEntities = visibleLights.processedEntities,
                    visibleLights = cullResults.visibleLights,
                    additionalLightDataUpdateInfos = additionalLightDataUpdateInfos,
                    packedShadowRequestSetHandles = packedShadowRequestSetHandles,
                    requestIndicesStorage = requestIndicesStorage,
                    cachedViewPositionsStorage = cachedViewPositionsStorage,

                    requestStorage = requestStorage,
                    cachedPointUpdateInfos = m_CachedPointUpdateInfos,
                    cachedSpotUpdateInfos = m_CachedSpotUpdateInfos,
                    cachedAreaRectangleUpdateInfos = m_CachedAreaRectangleUpdateInfos,
                    cachedAreaOtherUpdateInfos = m_CachedAreaOtherUpdateInfos,
                    cachedDirectionalUpdateInfos = m_CachedDirectionalUpdateInfos,
                    dynamicPointUpdateInfos = m_DynamicPointUpdateInfos,
                    dynamicSpotUpdateInfos = m_DynamicSpotUpdateInfos,
                    dynamicAreaRectangleUpdateInfos = m_DynamicAreaRectangleUpdateInfos,
                    dynamicAreaOtherUpdateInfos = m_DynamicAreaOtherUpdateInfos,
                    dynamicDirectionalUpdateInfos = m_DynamicDirectionalUpdateInfos,
                    kCubemapFaces =  m_CachedCubeMapFaces,
                    frustumPlanesStorage =  shadowRequestsDatabase.frustumPlanesStorage,

                    shadowIndices = shadowIndices,
#if UNITY_EDITOR
                    shadowRequestCounts = shadowRequestCounts,
#endif
                    visibleLightsAndIndicesBuffer =  m_VisibleLightsAndIndicesBuffer,
                    splitVisibleLightsAndIndicesBuffer = m_SplitVisibleLightsAndIndicesBuffer,

                    lightCounts = lightCounts,
                    shadowSettingsCascadeShadowSplitCount = shadowSettingsCascadeShadowSplitCount,
                    invalidIndex = invalidIndex,
                    worldSpaceCameraPos = worldSpaceCameraPos,
                    shaderConfigCameraRelativeRendering = ShaderConfig.s_CameraRelativeRendering,
                    shadowRequestCount = shadowManager.GetShadowRequestCount(),
                    shadowFilteringQuality = shadowFilteringQuality,
                    usesReversedZBuffer = usesReversedZBuffer,

                    validIndexCalculationsMarker = ShadowRequestUpdateProfiling.validIndexCalculationsMarker,
                    indicesAndPreambleMarker = ShadowRequestUpdateProfiling.indicesAndPreambleMarker,
                    cachedDirectionalRequestsMarker =  ShadowRequestUpdateProfiling.cachedDirectionalRequestsMarker,
                    cachedSpotRequestsMarker =  ShadowRequestUpdateProfiling.cachedSpotRequestsMarker,
                    cachedPointRequestsMarker =  ShadowRequestUpdateProfiling.cachedPointRequestsMarker,
                    cachedAreaRectangleRequestsMarker =  ShadowRequestUpdateProfiling.cachedAreaRectangleRequestsMarker,
                    cachedAreaOtherRequestsMarker =  ShadowRequestUpdateProfiling.cachedAreaOtherRequestsMarker,
                    dynamicDirectionalRequestsMarker =  ShadowRequestUpdateProfiling.dynamicDirectionalRequestsMarker,
                    dynamicSpotRequestsMarker =  ShadowRequestUpdateProfiling.dynamicSpotRequestsMarker,
                    dynamicPointRequestsMarker =  ShadowRequestUpdateProfiling.dynamicPointRequestsMarker,
                    dynamicAreaRectangleRequestsMarker =  ShadowRequestUpdateProfiling.dynamicAreaRectangleRequestsMarker,
                    dynamicAreaOtherRequestsMarker =  ShadowRequestUpdateProfiling.dynamicAreaOtherRequestsMarker
                };

                shadowRequestsAndIndicesJob.Run();

                HDCachedShadowManager.instance.SetCachedDirectionalAngles(m_CachedDirectionalAnglesArray[0]);

                ref UnsafeList<ShadowRequestIntermediateUpdateData> cachedDirectionalUpdateInfos = ref *(m_CachedDirectionalUpdateInfos.GetUnsafeList());
                int cachedDirectionalCount = cachedDirectionalUpdateInfos.Length;
                ref UnsafeList<ShadowRequestIntermediateUpdateData> dynamicDirectionalUpdateInfos = ref *(m_DynamicDirectionalUpdateInfos.GetUnsafeList());
                int dynamicDirectionalCount = dynamicDirectionalUpdateInfos.Length;

                HDAdditionalLightDataUpdateInfo* updateInfosUnsafePtr = (HDAdditionalLightDataUpdateInfo*)additionalLightDataUpdateInfos.GetUnsafePtr();

                int shaderConfigCameraRelativeRendering = ShaderConfig.s_CameraRelativeRendering;
                NativeList<float4> frustumPlanesStorage = shadowRequestsDatabase.frustumPlanesStorage;
                HDProcessedVisibleLight* processedLightArrayPtr = (HDProcessedVisibleLight*)visibleLights.processedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
                VisibleLight* visibleLightsArrayPtr = (VisibleLight*)cullResults.visibleLights.GetUnsafePtr<VisibleLight>();

                using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.UpdateDirectionalShadowData)))
                {
                    for (int i = 0; i < cachedDirectionalCount; i++)
                    {
                        ref ShadowRequestIntermediateUpdateData directionalUpdateInfo = ref cachedDirectionalUpdateInfos.ElementAt(i);
                        bool needToUpdateCachedContent = directionalUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                        HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
                        ref HDShadowRequest shadowRequest = ref requestStorageUnsafe.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                        int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
                        int lightIndex = directionalUpdateInfo.lightIndex;
                        Vector2 viewportSize = directionalUpdateInfo.viewportSize;
                        VisibleLight* visibleLightPtr = visibleLightsArrayPtr + lightIndex;
                        ref VisibleLight visibleLight = ref UnsafeUtility.AsRef<VisibleLight>(visibleLightPtr);

                        //We utilize a raw light data pointer to avoid copying the entire structure
                        HDProcessedVisibleLight* processedEntityPtr = processedLightArrayPtr + lightIndex;
                        ref HDProcessedVisibleLight processedEntity = ref UnsafeUtility.AsRef<HDProcessedVisibleLight>(processedEntityPtr);
                        HDLightType lightType = processedEntity.lightType;

                        ref HDAdditionalLightDataUpdateInfo updateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + additionalLightDataIndex);

                        if (needToUpdateCachedContent)
                        {
                            cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                            shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                            // Write per light type matrices, splitDatas and culling parameters
                            UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, visibleLight, cullResults, viewportSize,
                                shadowRequestHandle.offset, lightIndex, worldSpaceCameraPos, ref shadowRequest, out Matrix4x4 invViewProjection);

                            // Assign all setting common to every lights
                            SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                                lightIndex, lightType, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

                            shadowRequest.shouldUseCachedShadowData = false;
                            shadowRequest.shouldRenderCachedComponent = true;
                        }
                        else
                        {
                            shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
                            shadowRequest.shouldUseCachedShadowData = true;
                            shadowRequest.shouldRenderCachedComponent = false;
                            var _ViewMatrix = shadowRequest.view;
                            var _ProjMatrix = shadowRequest.deviceProjectionYFlip;
                            // If directional we still need to calculate the split data.
                            UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, visibleLight, cullResults, viewportSize,
                                shadowRequestHandle.offset, lightIndex, worldSpaceCameraPos, ref shadowRequest, out Matrix4x4 invViewProjection);

                            shadowRequest.view = _ViewMatrix;
                            shadowRequest.deviceProjectionYFlip = _ProjMatrix;
                        }

                        int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                        HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                        if (needToUpdateCachedContent && hdCamera.camera.cameraType != CameraType.Reflection)
                        {
                            HDCachedShadowManager.instance.MarkDirectionalShadowAsRendered(additionalLightData.lightIdxForCachedShadows + directionalUpdateInfo.shadowRequestHandle.offset);
                        }
                    }

                    for (int i = 0; i < dynamicDirectionalCount; i++)
                    {
                        ref ShadowRequestIntermediateUpdateData directionalUpdateInfo = ref dynamicDirectionalUpdateInfos.ElementAt(i);
                        HDShadowRequestHandle shadowRequestHandle = directionalUpdateInfo.shadowRequestHandle;
                        ref HDShadowRequest shadowRequest = ref requestStorageUnsafe.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                        int additionalLightDataIndex = directionalUpdateInfo.additionalLightDataIndex;
                        int lightIndex = directionalUpdateInfo.lightIndex;
                        Vector2 viewportSize = directionalUpdateInfo.viewportSize;
                        VisibleLight* visibleLightPtr = visibleLightsArrayPtr + lightIndex;
                        ref VisibleLight visibleLight = ref UnsafeUtility.AsRef<VisibleLight>(visibleLightPtr);

                        ref HDAdditionalLightDataUpdateInfo updateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + additionalLightDataIndex);

                        shadowRequest.shouldUseCachedShadowData = false;

                        shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                        // Write per light type matrices, splitDatas and culling parameters
                        UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, visibleLight, cullResults, viewportSize,
                            shadowRequestHandle.offset, lightIndex, worldSpaceCameraPos, ref shadowRequest, out Matrix4x4 invViewProjection);

                        // Assign all setting common to every lights
                        SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos, invViewProjection, viewportSize,
                            lightIndex, HDLightType.Directional, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                    }
                }

#if UNITY_EDITOR
                using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.EditorOnlyDebugSelectedLightShadow)))
                {
                    for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
                    {
                        if (!isValidIndex.IsSet(sortKeyIndex))
                            continue;

                        int shadowIndex = shadowIndices[sortKeyIndex];
                        if (shadowIndex < 0)
                            continue;

                        int shadowRequestCount = shadowRequestCounts[sortKeyIndex];

                        uint sortKey = visibleLights.sortKeys[sortKeyIndex];
                        int lightIndex = (int)(sortKey & 0xFFFF);
                        int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                        HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                        //We utilize a raw light data pointer to avoid copying the entire structure
                        HDProcessedVisibleLight* processedEntityPtr = processedLightArrayPtr + lightIndex;
                        ref HDProcessedVisibleLight processedEntity = ref UnsafeUtility.AsRef<HDProcessedVisibleLight>(processedEntityPtr);

                        Light lightComponent = additionalLightData.legacyLight;

                        if (lightComponent != null && (processedEntity.shadowMapFlags & HDProcessedVisibleLightsBuilder.ShadowMapFlags.WillRenderShadowMap) != 0)
                        {
                            if ((debugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection
                                 || debugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                                && UnityEditor.Selection.activeGameObject == lightComponent.gameObject)
                            {
                                debugSelectedLightShadowIndex = shadowIndex;
                                debugSelectedLightShadowCount = shadowRequestCount;
                            }
                        }
                    }
                }
#endif

                m_CachedPointUpdateInfos.ResizeUninitialized(0);
                m_CachedSpotUpdateInfos.ResizeUninitialized(0);
                m_CachedAreaRectangleUpdateInfos.ResizeUninitialized(0);
                m_CachedAreaOtherUpdateInfos.ResizeUninitialized(0);
                m_CachedDirectionalUpdateInfos.ResizeUninitialized(0);
                m_DynamicPointUpdateInfos.ResizeUninitialized(0);
                m_DynamicSpotUpdateInfos.ResizeUninitialized(0);
                m_DynamicAreaRectangleUpdateInfos.ResizeUninitialized(0);
                m_DynamicAreaOtherUpdateInfos.ResizeUninitialized(0);
                m_DynamicDirectionalUpdateInfos.ResizeUninitialized(0);
            }
        }

        internal static void SetCommonShadowRequestSettings(ref HDShadowRequest shadowRequest, HDShadowRequestHandle shadowRequestHandle, VisibleLight visibleLight, Vector3 cameraPos, Matrix4x4 invViewProjection, Vector2 viewportSize, int lightIndex, HDLightType lightType, HDShadowFilteringQuality filteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering, NativeList<float4> frustumPlanesStorage)
        {
            // zBuffer param to reconstruct depth position (for transmission)
            float f = visibleLight.range;
            float n = lightType == HDLightType.Area || lightType == HDLightType.Spot && additionalLightData.spotLightShape == SpotLightShape.Box ? additionalLightData.shadowNearPlane : Mathf.Max(additionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane);
            shadowRequest.zBufferParam = new float4((f-n)/n, 1.0f, (f-n)/(n*f), 1.0f/f);
            shadowRequest.worldTexelSize = 2.0f / shadowRequest.deviceProjectionYFlip.m00 / viewportSize.x * Mathf.Sqrt(2.0f);
            shadowRequest.normalBias = additionalLightData.normalBias;

            // Make light position camera relative:
            // TODO: think about VR (use different camera position for each eye)
            if (shaderConfigCameraRelativeRendering != 0)
            {
                CoreMatrixUtils.MatrixTimesTranslation(ref shadowRequest.view, cameraPos);
                CoreMatrixUtils.TranslationTimesMatrix(ref invViewProjection, -cameraPos);
            }

            bool hasOrthoMatrix = false;
            if (lightType == HDLightType.Directional || lightType == HDLightType.Spot && additionalLightData.spotLightShape == SpotLightShape.Box)
            {
                hasOrthoMatrix = true;
                shadowRequest.position = new Vector3(shadowRequest.view.m03, shadowRequest.view.m13, shadowRequest.view.m23);
            }
            else
            {
                var vlPos = visibleLight.GetPosition();
                shadowRequest.position = (shaderConfigCameraRelativeRendering != 0) ? vlPos - cameraPos : vlPos;
            }

            shadowRequest.shadowToWorld = invViewProjection.transpose;
            shadowRequest.zClip = (lightType != HDLightType.Directional);
            shadowRequest.lightIndex = lightIndex;
            // We don't allow shadow resize for directional cascade shadow
            if (lightType == HDLightType.Directional)
            {
                shadowRequest.shadowMapType = ShadowMapType.CascadedDirectional;
            }
            else if (lightType == HDLightType.Area && additionalLightData.areaLightShape == AreaLightShape.Rectangle)
            {
                shadowRequest.shadowMapType = ShadowMapType.AreaLightAtlas;
            }
            else
            {
                shadowRequest.shadowMapType = ShadowMapType.PunctualAtlas;
            }

            Matrix4x4 finalMatrix = CoreMatrixUtils.MultiplyProjectionMatrix(shadowRequest.projection, shadowRequest.view, hasOrthoMatrix);

            ref float4 frustumPlanesLeft = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes);
            ref float4 frustumPlanesRight = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 1);
            ref float4 frustumPlanesBottom = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 2);
            ref float4 frustumPlanesTop = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 3);
            ref float4 frustumPlanesNear = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 4);
            ref float4 frustumPlanesFar = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 5);

            // shadow clip planes (used for tessellation clipping)
            HDShadowUtils.CalculateFrustumPlanes(finalMatrix, out frustumPlanesLeft, out frustumPlanesRight, out frustumPlanesBottom, out frustumPlanesTop, out frustumPlanesNear, out frustumPlanesFar);

            float softness = 0.0f;
            if (lightType == HDLightType.Directional)
            {
                var devProj = shadowRequest.deviceProjection;
                float frustumExtentZ = Vector4.Dot(new Vector4(devProj.m32, -devProj.m32, -devProj.m22, devProj.m22), new Vector4(devProj.m22, devProj.m32, devProj.m23, devProj.m33)) /
                        (devProj.m22 * (devProj.m22 - devProj.m32));

                // We use the light view frustum derived from view projection matrix and angular diameter to work out a filter size in
                // shadow map space, essentially figuring out the footprint of the cone subtended by the light on the shadow map
                float halfAngleTan = Mathf.Tan(0.5f * Mathf.Deg2Rad * (additionalLightData.softnessScale * additionalLightData.angularDiameter) / 2);
                softness = Mathf.Abs(halfAngleTan * frustumExtentZ / (2.0f * shadowRequest.splitData.cullingSphere.w));
                float range = 2.0f * (1.0f / devProj.m22);
                float rangeScale = Mathf.Abs(range)  / 100.0f;
                shadowRequest.zBufferParam.x = rangeScale;
            }
            else
            {
                // This derivation has been fitted with quartic regression checking against raytracing reference and with a resolution of 512
                float x = additionalLightData.shapeRadius * additionalLightData.softnessScale;
                float x2 = x * x;
                softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
                softness /= 100.0f;
            }

            var viewportWidth = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport.width : shadowRequest.dynamicAtlasViewport.width;
            softness *= (viewportWidth / 512);  // Make it resolution independent whereas the baseline is 512

            // Bias
            // This base bias is a good value if we expose a [0..1] since values within [0..5] are empirically shown to be sensible for the slope-scale bias with the width of our PCF.
            float baseBias = 5.0f;
            // If we are PCSS, the blur radius can be quite big, hence we need to tweak up the slope bias
            if (filteringQuality == HDShadowFilteringQuality.High)
            {
                if(softness > 0.01f)
                {
                    // maxBaseBias is an empirically set value, also the lerp stops at a shadow softness of 0.05, then is clamped.
                    float maxBaseBias = 18.0f;
                    baseBias = Mathf.Lerp(baseBias, maxBaseBias, Mathf.Min(1.0f, (softness * 100) / 5));
                }
            }

            shadowRequest.slopeBias = HDShadowUtils.GetSlopeBias(baseBias, additionalLightData.slopeBias);

            // Shadow algorithm parameters
            shadowRequest.shadowSoftness = softness;
            shadowRequest.blockerSampleCount = additionalLightData.blockerSampleCount;
            shadowRequest.filterSampleCount = additionalLightData.filterSampleCount;
            shadowRequest.minFilterSize = additionalLightData.minFilterSize * 0.001f; // This divide by 1000 is here to have a range [0...1] exposed to user

            shadowRequest.kernelSize = (uint)additionalLightData.kernelSize;
            shadowRequest.lightAngle = (additionalLightData.lightAngle * Mathf.PI / 180.0f);
            shadowRequest.maxDepthBias = additionalLightData.maxDepthBias;
            // We transform it to base two for faster computation.
            // So e^x = 2^y where y = x * log2 (e)
            const float log2e = 1.44269504089f;
            shadowRequest.evsmParams.x = additionalLightData.evsmExponent * log2e;
            shadowRequest.evsmParams.y = additionalLightData.evsmLightLeakBias;
            shadowRequest.evsmParams.z = additionalLightData.evsmVarianceBias;
            shadowRequest.evsmParams.w = additionalLightData.evsmBlurPasses;
        }

        internal static void SetCommonShadowRequestSettingsPoint(ref HDShadowRequest shadowRequest, HDShadowRequestHandle shadowRequestHandle, in VisibleLight visibleLight, Vector3 cameraPos, Matrix4x4 invViewProjection, Vector2 viewportSize, int lightIndex, HDLightType lightType, HDShadowFilteringQuality filteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering, NativeList<float4> frustumPlanesStorage)
        {
            // zBuffer param to reconstruct depth position (for transmission)
            float f = visibleLight.range;
            float n = Mathf.Max(additionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane);
            shadowRequest.zBufferParam = new float4((f-n)/n, 1.0f, (f-n)/(n*f), 1.0f/f);
            shadowRequest.worldTexelSize = 2.0f / shadowRequest.deviceProjectionYFlip.m00 / viewportSize.x * math.sqrt(2.0f);
            shadowRequest.normalBias = additionalLightData.normalBias;

            // Make light position camera relative:
            // TODO: think about VR (use different camera position for each eye)
            if (shaderConfigCameraRelativeRendering != 0)
            {
                CoreMatrixUtils.MatrixTimesTranslation(ref shadowRequest.view, cameraPos);
                CoreMatrixUtils.TranslationTimesMatrix(ref invViewProjection, -cameraPos);
            }

            var lightAxisAndPosition = visibleLight.GetAxisAndPosition();
                shadowRequest.position = lightAxisAndPosition.Position;
                if (shaderConfigCameraRelativeRendering != 0)
                    shadowRequest.position -= cameraPos;

            shadowRequest.shadowToWorld = invViewProjection.transpose;
            shadowRequest.zClip = true;
            shadowRequest.lightIndex = lightIndex;

            shadowRequest.shadowMapType = ShadowMapType.PunctualAtlas;

            float4x4 finalMatrix = (float4x4)shadowRequest.projection * (float4x4)shadowRequest.view;

            ref float4 frustumPlanesLeft = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes);
            ref float4 frustumPlanesRight = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 1);
            ref float4 frustumPlanesBottom = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 2);
            ref float4 frustumPlanesTop = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 3);
            ref float4 frustumPlanesNear = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 4);
            ref float4 frustumPlanesFar = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 5);

            // shadow clip planes (used for tessellation clipping)
            HDShadowUtils.CalculateFrustumPlanes(finalMatrix, out frustumPlanesLeft, out frustumPlanesRight, out frustumPlanesBottom, out frustumPlanesTop, out frustumPlanesNear, out frustumPlanesFar);

            float softness = 0.0f;
            // This derivation has been fitted with quartic regression checking against raytracing reference and with a resolution of 512
            float x = additionalLightData.shapeRadius * additionalLightData.softnessScale;
            float x2 = x * x;
            softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
            softness /= 100.0f;

            var viewportWidth = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport.width : shadowRequest.dynamicAtlasViewport.width;
            softness *= (viewportWidth / 512);  // Make it resolution independent whereas the baseline is 512

            // Bias
            // This base bias is a good value if we expose a [0..1] since values within [0..5] are empirically shown to be sensible for the slope-scale bias with the width of our PCF.
            float baseBias = 5.0f;
            // If we are PCSS, the blur radius can be quite big, hence we need to tweak up the slope bias
            if (filteringQuality == HDShadowFilteringQuality.High)
            {
                if(softness > 0.01f)
                {
                    // maxBaseBias is an empirically set value, also the lerp stops at a shadow softness of 0.05, then is clamped.
                    float maxBaseBias = 18.0f;
                    baseBias = math.lerp(baseBias, maxBaseBias, math.min(1.0f, (softness * 100) / 5));
                }
            }

            shadowRequest.slopeBias = HDShadowUtils.GetSlopeBias(baseBias, additionalLightData.slopeBias);

            // Shadow algorithm parameters
            shadowRequest.shadowSoftness = softness;
            shadowRequest.blockerSampleCount = additionalLightData.blockerSampleCount;
            shadowRequest.filterSampleCount = additionalLightData.filterSampleCount;
            shadowRequest.minFilterSize = additionalLightData.minFilterSize * 0.001f; // This divide by 1000 is here to have a range [0...1] exposed to user

            shadowRequest.kernelSize = (uint)additionalLightData.kernelSize;
            shadowRequest.lightAngle = (additionalLightData.lightAngle * Mathf.PI / 180.0f);
            shadowRequest.maxDepthBias = additionalLightData.maxDepthBias;
            // We transform it to base two for faster computation.
            // So e^x = 2^y where y = x * log2 (e)
            const float log2e = 1.44269504089f;
            shadowRequest.evsmParams.x = additionalLightData.evsmExponent * log2e;
            shadowRequest.evsmParams.y = additionalLightData.evsmLightLeakBias;
            shadowRequest.evsmParams.z = additionalLightData.evsmVarianceBias;
            shadowRequest.evsmParams.w = additionalLightData.evsmBlurPasses;
        }

        private static void UpdateDirectionalShadowRequest(HDShadowManager manager, HDShadowSettings shadowSettings, VisibleLight visibleLight, CullingResults cullResults, Vector2 viewportSize, int requestIndex, int lightIndex, Vector3 cameraPos, ref HDShadowRequest shadowRequest, out Matrix4x4 invViewProjection)
        {
            Vector4 cullingSphere;
            float nearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

            HDShadowUtils.ExtractDirectionalLightData(
                visibleLight, viewportSize, (uint)requestIndex, shadowSettings.cascadeShadowSplitCount.value,
                shadowSettings.cascadeShadowSplits, nearPlaneOffset, cullResults, lightIndex,
                out shadowRequest.view, out invViewProjection, out shadowRequest.projection,
                out shadowRequest.deviceProjection, out shadowRequest.deviceProjectionYFlip, out shadowRequest.splitData
            );

            cullingSphere = shadowRequest.splitData.cullingSphere;

            // Camera relative for directional light culling sphere
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                cullingSphere.x -= cameraPos.x;
                cullingSphere.y -= cameraPos.y;
                cullingSphere.z -= cameraPos.z;
            }

            manager.UpdateCascade(requestIndex, cullingSphere, shadowSettings.cascadeShadowBorders[requestIndex]);
        }
    }
}
