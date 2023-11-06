using System;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDProcessedVisibleLightsBuilder
    {
        private void SortLightKeys()
        {
            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.SortVisibleLights)))
            {
                //Tunning against ps4 console,
                //32 items insertion sort has a workst case of 3 micro seconds.
                //200 non recursive merge sort has around 23 micro seconds.
                //From 200 and more, Radix sort beats everything.
                var sortSize = sortedLightCounts;
                if (sortSize <= 32)
                    CoreUnsafeUtils.InsertionSort(m_SortKeys, sortSize);
                else if (m_Size <= 200)
                    CoreUnsafeUtils.MergeSort(m_SortKeys, sortSize, ref m_SortSupportArray);
                else
                    CoreUnsafeUtils.RadixSort(m_SortKeys, sortSize, ref m_SortSupportArray);
            }
        }

        private unsafe void BuildVisibleLightEntities(in CullingResults cullResults)
        {
            m_Size = 0;

            if (!m_ProcessVisibleLightCounts.IsCreated)
            {
                int totalCounts = Enum.GetValues(typeof(ProcessLightsCountSlots)).Length;
                m_ProcessVisibleLightCounts.ResizeArray(totalCounts);
            }

            for (int i = 0; i < m_ProcessVisibleLightCounts.Length; ++i)
                m_ProcessVisibleLightCounts[i] = 0;

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.BuildVisibleLightEntities)))
            {
                if (cullResults.visibleLights.Length > 0 && HDLightRenderDatabase.instance != null)
                {
                    if (cullResults.visibleLights.Length > m_Capacity)
                    {
                        ResizeArrays(cullResults.visibleLights.Length);
                    }

                    m_Size = cullResults.visibleLights.Length;

                    //TODO: this should be accelerated by a c++ API
                    for (int i = 0; i < cullResults.visibleLights.Length; ++i)
                    {
                        Light light = cullResults.visibleLights[i].light;
                        int dataIndex = HDLightRenderDatabase.instance.FindEntityDataIndex(light);
                        if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                        {
                            //Shuriken lights thing: this happens because shuriken lights dont have the HDAdditionalLightData OnEnabled.
                            //Because of this, we have to forcefully create a light render entity on the rendering side. Horrible!!!
                            if (light.TryGetComponent<HDAdditionalLightData>(out var hdAdditionalLightData))
                            {
                                if (!hdAdditionalLightData.lightEntity.valid)
                                    hdAdditionalLightData.CreateHDLightRenderEntity(autoDestroy: true);
                            }
                            // This can happen if a scene is created via new asset creation vs proper scene creation dialog. In this situation we create a default additional light data.
                            // This is bad, but should happen *extremely* rarely and all the entities will 99.9% of the time end up in the branch above.
                            else
                            {
                                var hdLightData = light.gameObject.AddComponent<HDAdditionalLightData>();
                                if (hdLightData)
                                {
                                    HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLightData);
                                }
                                if (!hdLightData.lightEntity.valid)
                                    hdLightData.CreateHDLightRenderEntity(autoDestroy: true);

                                // Make sure we have a valid data index
                                dataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(hdLightData.lightEntity);
                            }
                        }

                        m_VisibleLightEntityDataIndices[i] = dataIndex;
                        m_VisibleLightBakingOutput[i] = light.bakingOutput;
                        m_VisibleLightShadowCasterMode[i] = light.lightShadowCasterMode;
                        m_VisibleLightShadows[i] = light.shadows;
                    }
                }

                splitVisibleLightsAndIndicesBuffer.Clear();
                shadowRequestValidityArray.SetBits(0, false, m_Size);

                dynamicPointVisibleLightsAndIndices = default;
                cachedPointVisibleLightsAndIndices = default;
                dynamicSpotVisibleLightsAndIndices = default;
                cachedSpotVisibleLightsAndIndices = default;
                dynamicAreaRectangleVisibleLightsAndIndices = default;
                cachedAreaRectangleVisibleLightsAndIndices = default;
                dynamicDirectionalVisibleLightsAndIndices = default;
                cachedDirectionalVisibleLightsAndIndices = default;

                dynamicPointHDSplits = default;
                cachedPointHDSplits = default;
                dynamicSpotHDSplits = default;
                cachedSpotHDSplits = default;
                dynamicAreaRectangleHDSplits = default;
                cachedAreaRectangleHDSplits = default;
                dynamicDirectionalHDSplits = default;
                cachedDirectionalHDSplits = default;
            }
        }

        private unsafe void ProcessShadows(
            HDCamera hdCamera,
            HDShadowManager shadowManager,
            LightingDebugSettings lightingDebugSettings,
            in HDShadowInitParameters inShadowInitParameters,
            in CullingResults cullResults)
        {
            if (shadowLightCount == 0)
                return;

            using (new ProfilingScope(ProfilingSampler.Get(HDProfileId.ProcessShadows)))
            {
                NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
                var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                int defaultEntityDataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);
                HDProcessedVisibleLight* entitiesPtr = (HDProcessedVisibleLight*)m_ProcessedEntities.GetUnsafePtr();

                for (int i = 0; i < shadowLightCount; ++i)
                {
                    int lightIndex = m_ShadowLightsDataIndices[i];
                    HDProcessedVisibleLight* entity = entitiesPtr + lightIndex;
                    if (!cullResults.GetShadowCasterBounds(lightIndex, out var bounds) || defaultEntityDataIndex == entity->dataIndex)
                    {
                        entity->shadowMapFlags = ShadowMapFlags.None;
                        continue;
                    }

                    HDAdditionalLightData additionalLightData = HDLightRenderDatabase.instance.hdAdditionalLightData[entity->dataIndex];
                    if (additionalLightData == null)
                        continue;

                    VisibleLight visibleLight = visibleLights[lightIndex];

                    if (additionalLightData.GetResolutionFromSettings(additionalLightData.GetShadowMapType(visibleLight.lightType), inShadowInitParameters) == 0)
                        continue;

                    additionalLightData.ReserveShadowMap(hdCamera.camera, shadowManager, hdShadowSettings, inShadowInitParameters, visibleLight, entity->lightType, visibleLight.forcedVisible);
                }

                // Now that all the lights have requested a shadow resolution, we can layout them in the atlas
                // And if needed rescale the whole atlas
                shadowManager.LayoutShadowMaps(lightingDebugSettings);

                for (int i = 0; i < shadowLightCount; ++i)
                {
                    int lightIndex = m_ShadowLightsDataIndices[i];
                    HDProcessedVisibleLight* entity = entitiesPtr + lightIndex;

                    if (defaultEntityDataIndex == entity->dataIndex)
                        continue;

                    if ((entity->shadowMapFlags & ShadowMapFlags.WillRenderShadowMap) == 0)
                        continue;

                    HDAdditionalLightData additionalLightData = HDLightRenderDatabase.instance.hdAdditionalLightData[entity->dataIndex];
                    if (additionalLightData == null)
                        continue;

                    VisibleLight visibleLight = visibleLights[lightIndex];

                    if (additionalLightData.GetResolutionFromSettings(additionalLightData.GetShadowMapType(visibleLight.lightType), inShadowInitParameters) == 0)
                        continue;

                    if (additionalLightData.HasShadowAtlasPlacement())
                        additionalLightData.OverrideShadowResolutionRequestsWithShadowCache(shadowManager, hdShadowSettings, entity->lightType);
                }
            }
        }

        private void FilterVisibleLightsByAOV(AOVRequestData aovRequest)
        {
            if (!aovRequest.hasLightFilter)
                return;

            for (int i = 0; i < m_Size; ++i)
            {
                var dataIndex = m_VisibleLightEntityDataIndices[i];
                if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    continue;

                var go = HDLightRenderDatabase.instance.aovGameObjects[dataIndex];
                if (go == null)
                    continue;

                if (!aovRequest.IsLightEnabled(go))
                    m_VisibleLightEntityDataIndices[i] = HDLightRenderDatabase.InvalidDataIndex;
            }
        }

    }
}
