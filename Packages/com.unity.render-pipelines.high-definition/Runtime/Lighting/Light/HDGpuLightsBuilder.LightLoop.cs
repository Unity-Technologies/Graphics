using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

            if (!m_LightTypeCounters.IsCreated)
                m_LightTypeCounters.ResizeArray(Enum.GetValues(typeof(GPULightTypeCountSlots)).Length);

            m_LightCount = 0;
            m_ContactShadowIndex = 0;
            m_ScreenSpaceShadowIndex = 0;
            m_ScreenSpaceShadowChannelSlot = 0;
            m_ScreenSpaceShadowsUnion.Clear();

            m_CurrentShadowSortedSunLightIndex = -1;
            m_CurrentSunShadowMapFlags = HDProcessedVisibleLightsBuilder.ShadowMapFlags.None;

            m_DebugSelectedLightShadowIndex = -1;
            m_DebugSelectedLightShadowCount = 0;

            for (int i = 0; i < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots; ++i)
            {
                m_CurrentScreenSpaceShadowData[i].additionalLightData = null;
                m_CurrentScreenSpaceShadowData[i].lightDataIndex = -1;
                m_CurrentScreenSpaceShadowData[i].valid = false;
            }

            for (int i = 0; i < m_LightTypeCounters.Length; ++i)
                m_LightTypeCounters[i] = 0;
        }

        //Builds the GPU light list.
        public void Build(
            CommandBuffer cmd,
            HDCamera hdCamera,
            in CullingResults cullingResult,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities,
            in HDShadowInitParameters shadowInitParams,
            DebugDisplaySettings debugDisplaySettings)
        {
            // Using the same pattern than shadowmaps, light have requested space in the atlas for their
            // cookies and now we can layout the atlas (re-insert all entries by order of size) if needed
            m_TextureCaches.lightCookieManager.LayoutIfNeeded();

            int totalLightsCount = visibleLights.sortedLightCounts;
            int lightsCount = visibleLights.sortedNonDirectionalLightCounts;
            int directionalCount = visibleLights.sortedDirectionalLightCounts;
            AllocateLightData(lightsCount, directionalCount);

            // TODO: Refactor shadow management
            // The good way of managing shadow:
            // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
            // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
            // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
            // will be use...)
            // The lightLoop is in charge, not the shadow pass.
            // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

            if (totalLightsCount > 0)
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
                CalculateAllLightDataTextureInfo(cmd, hdCamera, cullingResult, visibleLights, lightEntities, hdShadowSettings, shadowInitParams, debugDisplaySettings);
            }

            //Sanity check
            Debug.Assert(m_DirectionalLightCount == directionalLightCount, "Mismatch in Directional gpu lights processed. Lights should not be culled in this loop.");
            Debug.Assert(m_LightCount == areaLightCount + punctualLightCount, "Mismatch in Area and Punctual gpu lights processed. Lights should not be culled in this loop.");
        }

        //Calculates a shadow type for a light and sets the shadow index information into the LightData.
        public void ProcessLightDataShadowIndex(
            CommandBuffer cmd,
            in HDShadowInitParameters shadowInitParams,
            LightType lightType,
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
                    (lightType.IsSpot() && (lightComponent.cookie != null || additionalLightData.IESPoint != null)) ||
                    ((lightType.IsArea() && (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Disc)) && (lightComponent.cookie != null || additionalLightData.IESSpot != null)) ||
                    (lightType == LightType.Point && (lightComponent.cookie != null || additionalLightData.IESPoint != null))
                )
            )
            {
                switch (lightType)
                {
                    case LightType.Spot:
                    case LightType.Pyramid:
                    case LightType.Box:
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
                    case LightType.Point:
                        lightData.cookieMode = CookieMode.Repeat;
                        if (additionalLightData.IESPoint != null && lightComponent.cookie != null && additionalLightData.IESPoint != lightComponent.cookie)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie, additionalLightData.IESPoint);
                        else if (lightComponent.cookie != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, lightComponent.cookie);
                        else if (additionalLightData.IESPoint != null)
                            lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, additionalLightData.IESPoint);
                        break;
                    case LightType.Rectangle:
                    case LightType.Tube:
                    case LightType.Disc:
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
            else if (lightType == LightType.Pyramid || lightType == LightType.Box)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieMode = CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
            }
            else if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Disc)
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
                || m_ContactShadowIndex >= LightDefinitions.s_ContactShadowMaskMask)
                return;

            // Evaluate the contact shadow index of this light
            contactShadowMask = 1 << m_ContactShadowIndex;
            m_ContactShadowIndex++; // Update the index for next light that will need to cast contact shadows.

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

                    // increment the number of screen space shadows
                    m_ScreenSpaceShadowIndex++;

                    m_ScreenSpaceShadowsUnion.Add(additionalLightData);
                }
                m_CurrentSunLightDirectionalLightData = lightData;
                m_CurrentShadowSortedSunLightIndex = lightDataIndex;
                m_CurrentSunShadowMapFlags = shadowFlags;
            }

            // Get correct light cookie in case it is overriden by a volume
            CookieParameters cookieParams = new CookieParameters()
            {
                texture = lightComponent?.cookie,
                size = new Vector2(additionalLightData.shapeWidth, additionalLightData.shapeHeight),
                position = light.GetPosition()
            };

            if (lightComponent == HDRenderPipeline.currentPipeline.GetMainLight())
            {
                if (HDRenderPipeline.currentPipeline.skyManager.TryGetCloudSettings(hdCamera, out var cloudSettings, out var cloudRenderer))
                {
                    if (cloudRenderer.GetSunLightCookieParameters(cloudSettings, ref cookieParams))
                    {
                        var builtinParams = new BuiltinSunCookieParameters
                        {
                            cloudSettings = cloudSettings,
                            sunLight = lightComponent,
                            hdCamera = hdCamera,
                            commandBuffer = cmd
                        };
                        cloudRenderer.RenderSunLightCookie(builtinParams);
                    }
                }
            }

            if (cookieParams.texture)
            {
                lightData.cookieMode = cookieParams.texture.wrapMode == TextureWrapMode.Repeat ? CookieMode.Repeat : CookieMode.Clamp;
                lightData.cookieScaleOffset = m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, cookieParams.texture);
            }
            else
            {
                lightData.cookieMode = CookieMode.None;
            }

            lightData.right = light.GetRight() * 2 / Mathf.Max(cookieParams.size.x, 0.001f);
            lightData.up = light.GetUp() * 2 / Mathf.Max(cookieParams.size.y, 0.001f);

            // Apply precomputed atmospheric attenuation on light
            if (ShaderConfig.s_PrecomputedAtmosphericAttenuation != 0 && additionalLightData.interactsWithSky)
            {
                var skySettings = SkyManager.GetSkySetting(hdCamera.volumeStack);
                if (skySettings)
                {
                    Vector3 transm = skySettings.EvaluateAtmosphericAttenuation(-lightData.forward, hdCamera.camera.transform.position);
                    lightData.color.x *= transm.x;
                    lightData.color.y *= transm.y;
                    lightData.color.z *= transm.z;
                }
            }

            GetContactShadowMask(additionalLightData, HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset), hdCamera, ref lightData.contactShadowMask, ref lightData.isRayTracedContactShadow);

            lightData.shadowIndex = shadowIndex;
            additionalLightData.shadowIndex = shadowIndex;
        }

        private void CalculateLightDataTextureInfo(
            ref LightData lightData, CommandBuffer cmd, in Light lightComponent, HDAdditionalLightData additionalLightData, in HDShadowInitParameters shadowInitParams,
            in HDCamera hdCamera, BoolScalableSetting contactShadowScalableSetting,
            LightType lightType, HDProcessedVisibleLightsBuilder.ShadowMapFlags shadowFlags, bool rayTracingEnabled, int lightDataIndex, int shadowIndex)
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
        }

        private unsafe void CalculateAllLightDataTextureInfo(
            CommandBuffer cmd,
            HDCamera hdCamera,
            in CullingResults cullResults,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities,
            HDShadowSettings hdShadowSettings,
            in HDShadowInitParameters shadowInitParams,
            DebugDisplaySettings debugDisplaySettings)
        {
            BoolScalableSetting contactShadowScalableSetting = HDAdditionalLightData.ScalableSettings.UseContactShadow(m_Asset);
            bool rayTracingEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
            HDProcessedVisibleLight* processedLightArrayPtr = (HDProcessedVisibleLight*)visibleLights.processedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
            LightData* lightArrayPtr = (LightData*)m_Lights.GetUnsafePtr<LightData>();
            DirectionalLightData* directionalLightArrayPtr = (DirectionalLightData*)m_DirectionalLights.GetUnsafePtr<DirectionalLightData>();
            VisibleLight* visibleLightsArrayPtr = (VisibleLight*)cullResults.visibleLights.GetUnsafePtr<VisibleLight>();

            int directionalLightCount = visibleLights.sortedDirectionalLightCounts;
            int lightCounts = visibleLights.sortedLightCounts;
            EnsureScratchpadCapacity(lightCounts);

            NativeArray<int> shadowIndexResults = m_ShadowIndicesScratchpadArray;
            UpdateShadowRequestsAndCalculateShadowIndices(hdCamera, in cullResults, visibleLights, lightEntities, hdShadowSettings, debugDisplaySettings,
                m_ShadowManager, m_Asset, shadowIndexResults, ref m_DebugSelectedLightShadowIndex, ref m_DebugSelectedLightShadowCount);

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.CalculateLightDataTextureInfo)))
            {
                for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; ++sortKeyIndex)
                {
                    uint sortKey = visibleLights.sortKeys[sortKeyIndex];
                    HDGpuLightsBuilder.UnpackLightSortKey(sortKey, out var lightCategory, out var gpuLightType, out var lightVolumeType, out var lightIndex, out var offscreen);

                    // We don't need offscreen lights on the GPU
                    if (offscreen)
                        continue;

                    int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        continue;

                    HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                    if (additionalLightData == null)
                        continue;

                    //We utilize a raw light data pointer to avoid copying the entire structure
                    HDProcessedVisibleLight* processedEntityPtr = processedLightArrayPtr + lightIndex;
                    ref HDProcessedVisibleLight processedEntity = ref UnsafeUtility.AsRef<HDProcessedVisibleLight>(processedEntityPtr);
                    LightType lightType = processedEntity.lightType;

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
                            lightType, processedEntity.shadowMapFlags, rayTracingEnabled, lightDataIndex, shadowIndex);
                    }
                }
            }
        }

        internal unsafe void UpdateShadowRequestsAndCalculateShadowIndices(
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
            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.CalculateShadowIndices)))
            {
                var punctualShadowFilteringQuality = renderPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.punctualShadowFilteringQuality;
                var directionalShadowFilteringQuality = renderPipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.directionalShadowFilteringQuality;
                int lightCounts = visibleLights.sortedLightCounts;
                NativeBitArray shadowRequestValidityArray = visibleLights.shadowRequestValidityArray;

                HDShadowManagerDataForShadowRequestUpateJob shadowManagerData = default;
                shadowManager.GetUnmanageDataForShadowRequestJobs(ref shadowManagerData);

                bool usesReversedZBuffer = SystemInfo.usesReversedZBuffer;
                Vector3 worldSpaceCameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos;

                HDShadowRequestDatabase shadowRequestsDatabase = lightEntities.shadowRequests;
                shadowRequestsDatabase.EnsureNativeListsAreCreated();
#if UNITY_EDITOR
                NativeArray<int> shadowRequestCounts = m_ShadowRequestCountsScratchpad;
#endif
                NativeList<Vector3> cachedViewPositionsStorage = shadowRequestsDatabase.cachedViewPositionsStorage;
                NativeList<HDShadowRequest> requestStorage = shadowRequestsDatabase.hdShadowRequestStorage;
                ref UnsafeList<HDShadowRequest> requestStorageUnsafe = ref *requestStorage.GetUnsafeList();
                NativeList<int> requestIndicesStorage = shadowRequestsDatabase.hdShadowRequestIndicesStorage;
                NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos = lightEntities.additionalLightDataUpdateInfos;
                int shadowSettingsCascadeShadowSplitCount = hdShadowSettings.cascadeShadowSplitCount.value;

                HDShadowRequestUpdateJob shadowRequestsAndIndicesJob = new HDShadowRequestUpdateJob
                {
                    shadowManager = shadowManagerData,

                    shadowRequestValidityArray = shadowRequestValidityArray,
                    sortKeys = visibleLights.sortKeys,
                    visibleLightEntityDataIndices = visibleLights.visibleLightEntityDataIndices,
                    processedEntities = visibleLights.processedEntities,
                    visibleLights = cullResults.visibleLights,
                    additionalLightDataUpdateInfos = additionalLightDataUpdateInfos,
                    requestIndicesStorage = requestIndicesStorage,
                    cachedViewPositionsStorage = cachedViewPositionsStorage,

                    requestStorage = requestStorage,
                    cachedPointUpdateInfos = m_CachedPointUpdateInfos,
                    cachedSpotUpdateInfos = m_CachedSpotUpdateInfos,
                    cachedAreaRectangleUpdateInfos = m_CachedAreaRectangleUpdateInfos,
                    cachedDirectionalUpdateInfos = m_CachedDirectionalUpdateInfos,
                    dynamicPointUpdateInfos = m_DynamicPointUpdateInfos,
                    dynamicSpotUpdateInfos = m_DynamicSpotUpdateInfos,
                    dynamicAreaRectangleUpdateInfos = m_DynamicAreaRectangleUpdateInfos,
                    dynamicDirectionalUpdateInfos = m_DynamicDirectionalUpdateInfos,
                    frustumPlanesStorage =  shadowRequestsDatabase.frustumPlanesStorage,

                    shadowIndices = shadowIndices,
#if UNITY_EDITOR
                    shadowRequestCounts = shadowRequestCounts,
#endif
                    visibleLightsAndIndicesBuffer = visibleLights.visibleLightsAndIndicesBuffer,

                    cachedPointVisibleLightsAndIndices = visibleLights.cachedPointVisibleLightsAndIndices,
                    cachedSpotVisibleLightsAndIndices = visibleLights.cachedSpotVisibleLightsAndIndices,
                    cachedAreaRectangleVisibleLightsAndIndices = visibleLights.cachedAreaRectangleVisibleLightsAndIndices,
                    cachedDirectionalVisibleLightsAndIndices = visibleLights.cachedDirectionalVisibleLightsAndIndices,
                    dynamicPointVisibleLightsAndIndices = visibleLights.dynamicPointVisibleLightsAndIndices,
                    dynamicSpotVisibleLightsAndIndices = visibleLights.dynamicSpotVisibleLightsAndIndices,
                    dynamicAreaRectangleVisibleLightsAndIndices = visibleLights.dynamicAreaRectangleVisibleLightsAndIndices,
                    dynamicDirectionalVisibleLightsAndIndices = visibleLights.dynamicDirectionalVisibleLightsAndIndices,

                    cachedPointHDSplits = visibleLights.cachedPointHDSplits,
                    cachedSpotHDSplits = visibleLights.cachedSpotHDSplits,
                    cachedAreaRectangleHDSplits = visibleLights.cachedAreaRectangleHDSplits,
                    cachedDirectionalHDSplits = visibleLights.cachedDirectionalHDSplits,
                    dynamicPointHDSplits = visibleLights.dynamicPointHDSplits,
                    dynamicSpotHDSplits = visibleLights.dynamicSpotHDSplits,
                    dynamicAreaRectangleHDSplits = visibleLights.dynamicAreaRectangleHDSplits,
                    dynamicDirectionalHDSplits = visibleLights.dynamicDirectionalHDSplits,

                    lightCounts = lightCounts,
                    shadowSettingsCascadeShadowSplitCount = shadowSettingsCascadeShadowSplitCount,
                    worldSpaceCameraPos = worldSpaceCameraPos,
                    shaderConfigCameraRelativeRendering = ShaderConfig.s_CameraRelativeRendering,
                    shadowRequestCount = shadowManager.GetShadowRequestCount(),
                    punctualShadowFilteringQuality = punctualShadowFilteringQuality,
                    directionalShadowFilteringQuality = directionalShadowFilteringQuality,
                    usesReversedZBuffer = usesReversedZBuffer,

                    cachedDirectionalRequestsMarker =  ShadowRequestUpdateProfiling.cachedDirectionalRequestsMarker,
                    cachedSpotRequestsMarker =  ShadowRequestUpdateProfiling.cachedSpotRequestsMarker,
                    cachedPointRequestsMarker =  ShadowRequestUpdateProfiling.cachedPointRequestsMarker,
                    cachedAreaRectangleRequestsMarker =  ShadowRequestUpdateProfiling.cachedAreaRectangleRequestsMarker,
                    dynamicDirectionalRequestsMarker =  ShadowRequestUpdateProfiling.dynamicDirectionalRequestsMarker,
                    dynamicSpotRequestsMarker =  ShadowRequestUpdateProfiling.dynamicSpotRequestsMarker,
                    dynamicPointRequestsMarker =  ShadowRequestUpdateProfiling.dynamicPointRequestsMarker,
                    dynamicAreaRectangleRequestsMarker =  ShadowRequestUpdateProfiling.dynamicAreaRectangleRequestsMarker,
                };

                shadowRequestsAndIndicesJob.Run();

                ref UnsafeList<ShadowRequestIntermediateUpdateData> cachedDirectionalUpdateInfos = ref *(m_CachedDirectionalUpdateInfos.GetUnsafeList());
                int cachedDirectionalCount = cachedDirectionalUpdateInfos.Length;
                ref UnsafeList<ShadowRequestIntermediateUpdateData> dynamicDirectionalUpdateInfos = ref *(m_DynamicDirectionalUpdateInfos.GetUnsafeList());
                int dynamicDirectionalCount = dynamicDirectionalUpdateInfos.Length;

                HDAdditionalLightDataUpdateInfo* updateInfosUnsafePtr = (HDAdditionalLightDataUpdateInfo*)additionalLightDataUpdateInfos.GetUnsafePtr();

                int shaderConfigCameraRelativeRendering = ShaderConfig.s_CameraRelativeRendering;
                NativeList<float4> frustumPlanesStorage = shadowRequestsDatabase.frustumPlanesStorage;
                HDProcessedVisibleLight* processedLightArrayPtr = (HDProcessedVisibleLight*)visibleLights.processedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
                VisibleLight* visibleLightsArrayPtr = (VisibleLight*)cullResults.visibleLights.GetUnsafePtr<VisibleLight>();

                // Do all the directional light work we couldn't do inside the job.

                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.UpdateDirectionalShadowData)))
                {
                    for (int i = 0; i < cachedDirectionalCount; i++)
                    {
                        ref ShadowRequestIntermediateUpdateData directionalUpdateInfo = ref cachedDirectionalUpdateInfos.ElementAt(i);
                        ref HDShadowCullingSplit newCullingSplit = ref visibleLights.cachedDirectionalHDSplits.ElementAt(i);
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
                        ref HDAdditionalLightDataUpdateInfo updateInfo = ref UnsafeUtility.AsRef<HDAdditionalLightDataUpdateInfo>(updateInfosUnsafePtr + additionalLightDataIndex);

                        if (needToUpdateCachedContent)
                        {
                            cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition] = worldSpaceCameraPos;
                            shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);

                            // Write per light type matrices, splitDatas and culling parameters
                            UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, shadowRequestHandle.offset, worldSpaceCameraPos, ref shadowRequest, newCullingSplit);

                            SetDirectionalRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos,
                                shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, shadowRequest.cullingSplit.deviceProjectionMatrix, viewportSize,
                                lightIndex, directionalShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);

                            shadowRequest.shouldUseCachedShadowData = false;
                            shadowRequest.shouldRenderCachedComponent = true;
                        }
                        else
                        {
                            shadowRequest.cachedShadowData.cacheTranslationDelta = worldSpaceCameraPos - cachedViewPositionsStorage[shadowRequestHandle.storageIndexForCachedViewPosition];
                            shadowRequest.shouldUseCachedShadowData = true;
                            shadowRequest.shouldRenderCachedComponent = false;

                            var _ViewMatrix = shadowRequest.cullingSplit.view;
                            var _ProjMatrix = shadowRequest.cullingSplit.deviceProjectionYFlip;

                            // We still need to calculate the split data for directional.
                            UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, shadowRequestHandle.offset, worldSpaceCameraPos, ref shadowRequest, newCullingSplit);

                            shadowRequest.cullingSplit.view = _ViewMatrix;
                            shadowRequest.cullingSplit.deviceProjectionYFlip = _ProjMatrix;
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
                        ref HDShadowCullingSplit newCullingSplit = ref visibleLights.dynamicDirectionalHDSplits.ElementAt(i);
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
                        UpdateDirectionalShadowRequest(shadowManager, hdShadowSettings, shadowRequestHandle.offset, worldSpaceCameraPos, ref shadowRequest, newCullingSplit);

                        SetDirectionalRequestSettings(ref shadowRequest, shadowRequestHandle, visibleLight, worldSpaceCameraPos,
                            shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, shadowRequest.cullingSplit.deviceProjectionMatrix, viewportSize,
                            lightIndex, directionalShadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                    }
                }

#if UNITY_EDITOR
                using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.EditorOnlyDebugSelectedLightShadow)))
                {
                    for (int sortKeyIndex = 0; sortKeyIndex < lightCounts; sortKeyIndex++)
                    {
                        if (!shadowRequestValidityArray.IsSet(sortKeyIndex))
                            continue;

                        int shadowIndex = shadowIndices[sortKeyIndex];
                        if (shadowIndex < 0)
                            continue;

                        uint sortKey = visibleLights.sortKeys[sortKeyIndex];
                        int lightIndex = (int)(sortKey & 0xFFFF);
                        int dataIndex = visibleLights.visibleLightEntityDataIndices[lightIndex];
                        Light lightComponent = lightEntities.hdAdditionalLightData[dataIndex].legacyLight;

                        if (lightComponent != null)
                        {
                            LightingDebugSettings debugSettings = debugDisplaySettings.data.lightingDebugSettings;

                            if ((debugSettings.shadowDebugUseSelection || debugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                                && UnityEditor.Selection.activeGameObject == lightComponent.gameObject)
                            {
                                debugSelectedLightShadowIndex = shadowIndex;
                                debugSelectedLightShadowCount = shadowRequestCounts[sortKeyIndex];
                            }
                        }
                    }
                }
#endif

                // Unfortunately we have to redo a lot of shadow update calculations for lights with custom view callbacks.
                // Since they are managed delegates, they can't be part of a Burst compiled job.
                // We do what we can to minimize extra cost for the common case here,
                // but if a user has many spot lights with custom view callbacks then their loop will be much more expensive.

                if (lightEntities.validCustomViewCallbackEvents > 0)
                {
                    DynamicArray<HDLightRenderDatabase.SpotLightCallbackData> callbacks = lightEntities.customViewCallbackEvents;

                    UpdateCachedSpotShadowRequestsAndResolutionRequests(visibleLights, callbacks, m_CachedSpotUpdateInfos,
                        shadowRequestsDatabase.hdShadowRequestStorage, shadowRequestsDatabase.cachedViewPositionsStorage,
                        lightEntities.additionalLightDataUpdateInfos, punctualShadowFilteringQuality,
                        shadowRequestsDatabase.frustumPlanesStorage, shaderConfigCameraRelativeRendering, worldSpaceCameraPos,
                        usesReversedZBuffer);

                    UpdateDynamicSpotShadowRequestsAndResolutionRequests(visibleLights, callbacks, m_DynamicSpotUpdateInfos,
                        shadowRequestsDatabase.hdShadowRequestStorage,
                        lightEntities.additionalLightDataUpdateInfos, punctualShadowFilteringQuality,
                        shadowRequestsDatabase.frustumPlanesStorage, shaderConfigCameraRelativeRendering, worldSpaceCameraPos,
                        usesReversedZBuffer);
                }

                // Reset our scratchpad arrays.
                m_CachedPointUpdateInfos.ResizeUninitialized(0);
                m_CachedSpotUpdateInfos.ResizeUninitialized(0);
                m_CachedAreaRectangleUpdateInfos.ResizeUninitialized(0);
                m_CachedDirectionalUpdateInfos.ResizeUninitialized(0);
                m_DynamicPointUpdateInfos.ResizeUninitialized(0);
                m_DynamicSpotUpdateInfos.ResizeUninitialized(0);
                m_DynamicAreaRectangleUpdateInfos.ResizeUninitialized(0);
                m_DynamicDirectionalUpdateInfos.ResizeUninitialized(0);
            }
        }

        /// <summary>
        /// A duplicate of the function of the same name inside HDShadowRequestUpdateJob.cs
        /// Duplicated to support the expensive managed callback per spot light, without sacrificing performance in the Burst compiled job.
        /// Any changes made here should be duplicated in the other function and vice versa.
        /// </summary>
        internal static unsafe void UpdateCachedSpotShadowRequestsAndResolutionRequests(HDProcessedVisibleLightsBuilder visibleLights, DynamicArray<HDLightRenderDatabase.SpotLightCallbackData> callbacks, NativeList<ShadowRequestIntermediateUpdateData> cachedSpotUpdateInfos, NativeList<HDShadowRequest> requestStorage,
            NativeList<Vector3> cachedViewPositionsStorage, NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos, HDShadowFilteringQuality shadowFilteringQuality, NativeList<float4> frustumPlanesStorage,
            int shaderConfigCameraRelativeRendering, Vector3 worldSpaceCameraPos, bool usesReversedZBuffer)
        {
            int spotCount = cachedSpotUpdateInfos.Length;

            ref UnsafeList<ShadowRequestIntermediateUpdateData> updateDataUnsafe = ref *cachedSpotUpdateInfos.GetUnsafeList();

            for (int i = 0; i < spotCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData spotUpdateInfo = ref updateDataUnsafe.ElementAt(i);
                ref HDShadowCullingSplit newCullingSplit = ref visibleLights.cachedSpotHDSplits.ElementAt(i);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                ref HDLightRenderDatabase.SpotLightCallbackData callbackData = ref callbacks[additionalLightDataIndex];

                if (callbackData.isAnythingRegistered)
                {
                    bool needToUpdateCachedContent = spotUpdateInfo.states[ShadowRequestIntermediateUpdateData.k_NeedToUpdateCachedContent];
                    HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                    ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
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
                        shadowRequest.cullingSplit = newCullingSplit;

                        if (callbackData.callback != null)
                        {
                            shadowRequest.cullingSplit.view = callbackData.callback(spotUpdateInfo.visibleLight.localToWorldMatrix);
                        }

                        SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos,
                            shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
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
                        shadowRequest.cullingSplit = newCullingSplit;

                        if (callbackData.callback != null)
                        {
                            shadowRequest.cullingSplit.view = callbackData.callback(spotUpdateInfo.visibleLight.localToWorldMatrix);
                        }

                        SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos,
                            shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                            lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                    }
                }
            }
        }

        internal static unsafe void UpdateDynamicSpotShadowRequestsAndResolutionRequests(HDProcessedVisibleLightsBuilder visibleLights, DynamicArray<HDLightRenderDatabase.SpotLightCallbackData> callbacks, NativeList<ShadowRequestIntermediateUpdateData> dynamicSpotUpdateInfos, NativeList<HDShadowRequest> requestStorage,
            NativeArray<HDAdditionalLightDataUpdateInfo> additionalLightDataUpdateInfos, HDShadowFilteringQuality shadowFilteringQuality, NativeList<float4> frustumPlanesStorage,
            int shaderConfigCameraRelativeRendering, Vector3 worldSpaceCameraPos, bool usesReversedZBuffer)
        {
            int spotCount = dynamicSpotUpdateInfos.Length;

            ref UnsafeList<ShadowRequestIntermediateUpdateData> updateDataUnsafe = ref *dynamicSpotUpdateInfos.GetUnsafeList();

            for (int i = 0; i < spotCount; i++)
            {
                ref ShadowRequestIntermediateUpdateData spotUpdateInfo = ref updateDataUnsafe.ElementAt(i);
                ref HDShadowCullingSplit newCullingSplit = ref visibleLights.dynamicSpotHDSplits.ElementAt(i);
                int additionalLightDataIndex = spotUpdateInfo.additionalLightDataIndex;
                ref HDLightRenderDatabase.SpotLightCallbackData callbackData = ref callbacks[additionalLightDataIndex];

                if (callbackData.isAnythingRegistered)
                {
                    HDShadowRequestHandle shadowRequestHandle = spotUpdateInfo.shadowRequestHandle;
                    ref HDShadowRequest shadowRequest = ref requestStorage.ElementAt(shadowRequestHandle.storageIndexForRequestIndex);
                    int lightIndex = spotUpdateInfo.lightIndex;
                    Vector2 viewportSize = spotUpdateInfo.viewportSize;

                    HDAdditionalLightDataUpdateInfo updateInfo = additionalLightDataUpdateInfos[additionalLightDataIndex];

                    shadowRequest.shouldUseCachedShadowData = false;
                    shadowRequest.cachedShadowData.cacheTranslationDelta = new Vector3(0.0f, 0.0f, 0.0f);
                    shadowRequest.cullingSplit = newCullingSplit;

                    if (callbackData.callback != null)
                    {
                        shadowRequest.cullingSplit.view = callbackData.callback(spotUpdateInfo.visibleLight.localToWorldMatrix);
                    }

                    SetSpotRequestSettings(ref shadowRequest, shadowRequestHandle, spotUpdateInfo.visibleLight, 0f, worldSpaceCameraPos,
                        shadowRequest.cullingSplit.invViewProjection, shadowRequest.cullingSplit.projection, viewportSize,
                        lightIndex, shadowFilteringQuality, updateInfo, shaderConfigCameraRelativeRendering, frustumPlanesStorage);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 GetZBufferParam(in VisibleLight visibleLight, float nearPlaneForZBufferParam)
        {
            // zBuffer param to reconstruct depth position (for transmission)
            float f = visibleLight.range;
            float n = nearPlaneForZBufferParam;
            return new float4((f-n)/n, 1.0f, (f-n)/(n*f), 1.0f/f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 GetDirectionalZBufferParam(in VisibleLight visibleLight, float nearPlaneForZBufferParam, float rangeScale)
        {
            float4 zBufferParam = GetZBufferParam(visibleLight, nearPlaneForZBufferParam);
            zBufferParam.x = rangeScale;
            return zBufferParam;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MakeViewAndViewProjectionCameraRelative(ref HDShadowRequest shadowRequest, ref Matrix4x4 invViewProjection, int shaderConfigCameraRelativeRendering, Vector3 cameraPos)
        {
            // Make light position camera relative:
            // TODO: think about VR (use different camera position for each eye)
            if (shaderConfigCameraRelativeRendering != 0)
            {
                CoreMatrixUtils.MatrixTimesTranslation(ref shadowRequest.cullingSplit.view, cameraPos);
                CoreMatrixUtils.TranslationTimesMatrix(ref invViewProjection, -cameraPos);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 GetDirectionalLightSoftnessAndRangeScale(in HDShadowRequest shadowRequest, in HDAdditionalLightDataUpdateInfo additionalLightData, Matrix4x4 devProj)
        {
            float frustumExtentZ = Vector4.Dot(new Vector4(devProj.m32, -devProj.m32, -devProj.m22, devProj.m22), new Vector4(devProj.m22, devProj.m32, devProj.m23, devProj.m33)) /
                                   (devProj.m22 * (devProj.m22 - devProj.m32));

            // We use the light view frustum derived from view projection matrix and angular diameter to work out a filter size in
            // shadow map space, essentially figuring out the footprint of the cone subtended by the light on the shadow map
            float halfAngleTan = Mathf.Tan(0.5f * Mathf.Deg2Rad * (additionalLightData.softnessScale * additionalLightData.angularDiameter) / 2);
            float softness = Mathf.Abs(halfAngleTan * frustumExtentZ / (2.0f * shadowRequest.cullingSplit.cullingSphere.w));
            float range = 2.0f * (1.0f / devProj.m22);
            float rangeScale = Mathf.Abs(range)  / 100.0f;

            var viewportWidth = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport.width : shadowRequest.dynamicAtlasViewport.width;
            softness *= (viewportWidth / 512);  // Make it resolution independent whereas the baseline is 512

            return new float2(softness, rangeScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetNonDirectionalSoftness(in HDShadowRequest shadowRequest, in HDAdditionalLightDataUpdateInfo additionalLightData)
        {
            // This derivation has been fitted with quartic regression checking against raytracing reference and with a resolution of 512
            float x = additionalLightData.shapeRadius * additionalLightData.softnessScale;
            float x2 = x * x;
            float softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
            softness /= 100.0f;

            var viewportWidth = shadowRequest.isInCachedAtlas ? shadowRequest.cachedAtlasViewport.width : shadowRequest.dynamicAtlasViewport.width;
            softness *= (viewportWidth / 512);  // Make it resolution independent whereas the baseline is 512

            return softness;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetBaseBias(bool isHighQuality, float softness)
        {
            // Bias
            // This base bias is a good value if we expose a [0..1] since values within [0..5] are empirically shown to be sensible for the slope-scale bias with the width of our PCF.
            float baseBias = 5.0f;
            // If we are PCSS, the blur radius can be quite big, hence we need to tweak up the slope bias
            if (isHighQuality && softness > 0.01f)
            {
                // maxBaseBias is an empirically set value, also the lerp stops at a shadow softness of 0.05, then is clamped.
                float maxBaseBias = 18.0f;
                baseBias = Mathf.Lerp(baseBias, maxBaseBias, Mathf.Min(1.0f, (softness * 100) / 5));
            }

            return baseBias;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetPositionFromView(in Matrix4x4 view)
        {
            return new Vector3(view.m03, view.m13, view.m23);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetPositionFromVisibleLight(in VisibleLight visibleLight, Vector3 cameraPos, float forwardOffset, int shaderConfigCameraRelativeRendering)
        {
            var lightAxisAndPosition = visibleLight.GetAxisAndPosition();
            Vector3 position = lightAxisAndPosition.Position + lightAxisAndPosition.Forward * forwardOffset;
            if (shaderConfigCameraRelativeRendering != 0)
                position -= cameraPos;

            return position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetDirectionalRequestSettings(ref HDShadowRequest shadowRequest,
            HDShadowRequestHandle shadowRequestHandle, in VisibleLight visibleLight, Vector3 cameraPos,
            Matrix4x4 invViewProjection, in Matrix4x4 projection, in Matrix4x4 devProjMatrix, Vector2 viewportSize, int lightIndex, HDShadowFilteringQuality filteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering, NativeList<float4> frustumPlanesStorage)
        {
            MakeViewAndViewProjectionCameraRelative(ref shadowRequest, ref invViewProjection, shaderConfigCameraRelativeRendering, cameraPos);

            float nearPlane = Mathf.Max(additionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane);
            float2 softnessAndRangeScale = GetDirectionalLightSoftnessAndRangeScale(shadowRequest, additionalLightData, devProjMatrix);
            float4 zBufferParam = GetDirectionalZBufferParam(visibleLight, nearPlane, softnessAndRangeScale.y);
            Vector3 position = new Vector3(shadowRequest.cullingSplit.view.m03, shadowRequest.cullingSplit.view.m13, shadowRequest.cullingSplit.view.m23);

            // New directional light PCSS implementation no longer requires boosting base bias
            float baseBias = GetBaseBias(filteringQuality == HDShadowFilteringQuality.High, 0.0f);

            SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, cameraPos, invViewProjection, projection,
                viewportSize, lightIndex, additionalLightData, shaderConfigCameraRelativeRendering, frustumPlanesStorage,
                ShadowMapType.CascadedDirectional, zBufferParam, softnessAndRangeScale.x, position, baseBias,
                true, false);

            // Directional light PCSS parameters
            if(filteringQuality == HDShadowFilteringQuality.High)
            {
                float lightAngularDiameter = additionalLightData.softnessScale * additionalLightData.angularDiameter;
                float halfAngularDiameterTangent = Mathf.Tan(0.5f * Mathf.Deg2Rad * lightAngularDiameter);
                float shadowMapDepth2RadialScale = Mathf.Abs(devProjMatrix.m00 / devProjMatrix.m22);
                shadowRequest.dirLightPCSSDepth2RadialScale = halfAngularDiameterTangent * shadowMapDepth2RadialScale;
                shadowRequest.dirLightPCSSRadial2DepthScale = 1.0f / shadowRequest.dirLightPCSSDepth2RadialScale;
                shadowRequest.dirLightPCSSMaxBlockerDistance = additionalLightData.dirLightPCSSMaxPenumbraSize / (2.0f * halfAngularDiameterTangent);
                shadowRequest.dirLightPCSSMaxSamplingDistance = additionalLightData.dirLightPCSSMaxSamplingDistance;
                shadowRequest.dirLightPCSSMinFilterSizeTexels = additionalLightData.dirLightPCSSMinFilterSizeTexels;
                // Ensure min filter angular diameter covers blocker search angular diameter
                float minFilterAngularDiameter = Mathf.Max(additionalLightData.dirLightPCSSBlockerSearchAngularDiameter,
                    additionalLightData.dirLightPCSSMinFilterMaxAngularDiameter);
                float halfMinFilterAngularDiameterTangent = Mathf.Tan(0.5f * Mathf.Deg2Rad * Mathf.Max(minFilterAngularDiameter, lightAngularDiameter));
                shadowRequest.dirLightPCSSMinFilterRadial2DepthScale = 1.0f / (halfMinFilterAngularDiameterTangent * shadowMapDepth2RadialScale);
                float halfBlockerSearchAngularDiameterTangent = Mathf.Tan(0.5f * Mathf.Deg2Rad * Mathf.Max(additionalLightData.dirLightPCSSBlockerSearchAngularDiameter, lightAngularDiameter));
                shadowRequest.dirLightPCSSBlockerRadial2DepthScale = 1.0f / (halfBlockerSearchAngularDiameterTangent * shadowMapDepth2RadialScale);
                // Uniform distribution is sqrt of linear range, so we remap the exponent to the [0.5, 3.0] range
                shadowRequest.dirLightPCSSBlockerSamplingClumpExponent = 0.5f * additionalLightData.dirLightPCSSBlockerSamplingClumpExponent;
                shadowRequest.blockerSampleCount = (byte)additionalLightData.dirLightPCSSBlockerSampleCount;
                shadowRequest.filterSampleCount = (byte)additionalLightData.dirLightPCSSFilterSampleCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetAreaRequestSettings(ref HDShadowRequest shadowRequest,
            HDShadowRequestHandle shadowRequestHandle, in VisibleLight visibleLight, float forwardOffset,
            Vector3 cameraPos, Matrix4x4 invViewProjection, Matrix4x4 projection, Vector2 viewportSize, int lightIndex,
            HDAreaShadowFilteringQuality areaFilteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering,
            NativeList<float4> frustumPlanesStorage)
        {
            MakeViewAndViewProjectionCameraRelative(ref shadowRequest, ref invViewProjection, shaderConfigCameraRelativeRendering, cameraPos);

            float nearPlane = additionalLightData.shadowNearPlane;
            float4 zBufferParam = GetZBufferParam(visibleLight, nearPlane);

            Vector3 position = GetPositionFromVisibleLight(visibleLight, cameraPos, forwardOffset, shaderConfigCameraRelativeRendering);
            float softness = GetNonDirectionalSoftness(shadowRequest, additionalLightData);
            float baseBias = GetBaseBias(areaFilteringQuality == HDAreaShadowFilteringQuality.High, softness);

            SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, cameraPos, invViewProjection, projection,
                viewportSize, lightIndex, additionalLightData, shaderConfigCameraRelativeRendering, frustumPlanesStorage,
                ShadowMapType.AreaLightAtlas, zBufferParam, softness, position, baseBias,
                false, true);

            // We transform it to base two for faster computation.
            // So e^x = 2^y where y = x * log2 (e)
            const float log2e = 1.44269504089f;
            shadowRequest.evsmParams.x = additionalLightData.evsmExponent * log2e;
            shadowRequest.evsmParams.y = additionalLightData.evsmLightLeakBias;
            shadowRequest.evsmParams.z = additionalLightData.evsmVarianceBias;
            shadowRequest.evsmParams.w = additionalLightData.evsmBlurPasses;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetSpotRequestSettings(ref HDShadowRequest shadowRequest,
            HDShadowRequestHandle shadowRequestHandle, in VisibleLight visibleLight, float forwardOffset,
            Vector3 cameraPos, Matrix4x4 invViewProjection, Matrix4x4 projection, Vector2 viewportSize, int lightIndex,
            HDShadowFilteringQuality filteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering,
            NativeList<float4> frustumPlanesStorage)
        {
            MakeViewAndViewProjectionCameraRelative(ref shadowRequest, ref invViewProjection, shaderConfigCameraRelativeRendering, cameraPos);

            bool isBoxShape = visibleLight.lightType == LightType.Box;
            float nearPlane = additionalLightData.shadowNearPlane;
            float minimumConeAndPyramidNearPlane = Mathf.Max(nearPlane, HDShadowUtils.k_MinShadowNearPlane);
            nearPlane = isBoxShape ? nearPlane : minimumConeAndPyramidNearPlane;
            float4 zBufferParam = GetZBufferParam(visibleLight, nearPlane);
            bool hasOrthoMatrix = isBoxShape;

            Vector3 position = GetPositionFromVisibleLight(visibleLight, cameraPos, forwardOffset, shaderConfigCameraRelativeRendering);

            if (isBoxShape)
            {
                position = GetPositionFromView(shadowRequest.cullingSplit.view);
            }

            float softness = GetNonDirectionalSoftness(shadowRequest, additionalLightData);
            float baseBias = GetBaseBias(filteringQuality == HDShadowFilteringQuality.High, softness);

            SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, cameraPos, invViewProjection, projection,
                viewportSize, lightIndex, additionalLightData, shaderConfigCameraRelativeRendering, frustumPlanesStorage,
                ShadowMapType.PunctualAtlas, zBufferParam, softness, position, baseBias,
                hasOrthoMatrix, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetPointRequestSettings(ref HDShadowRequest shadowRequest,
            HDShadowRequestHandle shadowRequestHandle, in VisibleLight visibleLight,
            Vector3 cameraPos, Matrix4x4 invViewProjection, Matrix4x4 projection, Vector2 viewportSize, int lightIndex,
            HDShadowFilteringQuality filteringQuality,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering,
            NativeList<float4> frustumPlanesStorage)
        {
            MakeViewAndViewProjectionCameraRelative(ref shadowRequest, ref invViewProjection, shaderConfigCameraRelativeRendering, cameraPos);

            float nearPlane = Mathf.Max(additionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane);
            float4 zBufferParam = GetZBufferParam(visibleLight, nearPlane);
            Vector3 position = GetPositionFromVisibleLight(visibleLight, cameraPos, 0f, shaderConfigCameraRelativeRendering);
            float softness = GetNonDirectionalSoftness(shadowRequest, additionalLightData);
            float baseBias = GetBaseBias(filteringQuality == HDShadowFilteringQuality.High, softness);

            SetCommonShadowRequestSettings(ref shadowRequest, shadowRequestHandle, cameraPos, invViewProjection, projection,
                viewportSize, lightIndex, additionalLightData, shaderConfigCameraRelativeRendering, frustumPlanesStorage,
                ShadowMapType.PunctualAtlas, zBufferParam, softness, position, baseBias,
                false, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetCommonShadowRequestSettings(ref HDShadowRequest shadowRequest,
            HDShadowRequestHandle shadowRequestHandle, Vector3 cameraPos, Matrix4x4 invViewProjection, Matrix4x4 projection, Vector2 viewportSize, int lightIndex,
            in HDAdditionalLightDataUpdateInfo additionalLightData, int shaderConfigCameraRelativeRendering,
            NativeList<float4> frustumPlanesStorage, ShadowMapType shadowMapType, float4 zBufferParam, float softness, Vector3 position, float baseBias, bool hasOrthoMatrix, bool zClip)
        {
            shadowRequest.zBufferParam = zBufferParam;
            shadowRequest.worldTexelSize = 2.0f / shadowRequest.cullingSplit.deviceProjectionYFlip.m00 / viewportSize.x * Mathf.Sqrt(2.0f);
            shadowRequest.normalBias = additionalLightData.normalBias;

            shadowRequest.position = position;

            shadowRequest.shadowToWorld = invViewProjection.transpose;
            shadowRequest.zClip = zClip;
            shadowRequest.lightIndex = lightIndex;
            // We don't allow shadow resize for directional cascade shadow
            shadowRequest.shadowMapType = shadowMapType;

            Matrix4x4 finalMatrix = CoreMatrixUtils.MultiplyProjectionMatrix(projection, shadowRequest.cullingSplit.view, hasOrthoMatrix);

            ref float4 frustumPlanesLeft = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes);
            ref float4 frustumPlanesRight = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 1);
            ref float4 frustumPlanesBottom = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 2);
            ref float4 frustumPlanesTop = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 3);
            ref float4 frustumPlanesNear = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 4);
            ref float4 frustumPlanesFar = ref frustumPlanesStorage.ElementAt(shadowRequestHandle.storageIndexForFrustumPlanes + 5);

            // shadow clip planes (used for tessellation clipping)
            HDShadowUtils.CalculateFrustumPlanes(finalMatrix, out frustumPlanesLeft, out frustumPlanesRight, out frustumPlanesBottom, out frustumPlanesTop, out frustumPlanesNear, out frustumPlanesFar);

            shadowRequest.slopeBias = HDShadowUtils.GetSlopeBias(baseBias, additionalLightData.slopeBias);

            // Shadow algorithm parameters
            shadowRequest.shadowSoftness = softness;
            shadowRequest.blockerSampleCount = additionalLightData.blockerSampleCount;
            shadowRequest.filterSampleCount = additionalLightData.filterSampleCount;
            shadowRequest.minFilterSize = additionalLightData.minFilterSize * 0.001f; // This divide by 1000 is here to have a range [0...1] exposed to user

            shadowRequest.kernelSize = (uint)additionalLightData.kernelSize;
        }

        private static void UpdateDirectionalShadowRequest(HDShadowManager manager, HDShadowSettings shadowSettings, int requestIndex, Vector3 cameraPos, ref HDShadowRequest shadowRequest, in HDShadowCullingSplit newCullingSplit)
        {
            Vector4 cullingSphere = newCullingSplit.cullingSphere;

            // Camera relative for directional light culling sphere
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                cullingSphere.x -= cameraPos.x;
                cullingSphere.y -= cameraPos.y;
                cullingSphere.z -= cameraPos.z;
            }

            manager.UpdateCascade(requestIndex, cullingSphere, shadowSettings.cascadeShadowBorders[requestIndex]);
            shadowRequest.cullingSplit = newCullingSplit;
        }
    }
}
