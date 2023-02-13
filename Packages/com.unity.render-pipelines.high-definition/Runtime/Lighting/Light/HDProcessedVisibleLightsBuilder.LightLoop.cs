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
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.SortVisibleLights)))
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

        private void BuildVisibleLightEntities(in CullingResults cullResults)
        {
            m_Size = 0;

            if (!m_ProcessVisibleLightCounts.IsCreated)
            {
                int totalCounts = Enum.GetValues(typeof(ProcessLightsCountSlots)).Length;
                m_ProcessVisibleLightCounts.ResizeArray(totalCounts);
            }

            for (int i = 0; i < m_ProcessVisibleLightCounts.Length; ++i)
                m_ProcessVisibleLightCounts[i] = 0;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.BuildVisibleLightEntities)))
            {
                if (cullResults.visibleLights.Length == 0
                    || HDLightRenderDatabase.instance == null)
                    return;

                if (cullResults.visibleLights.Length > m_Capacity)
                {
                    ResizeArrays(cullResults.visibleLights.Length);
                }

                m_Size = cullResults.visibleLights.Length;

                //TODO: this should be accelerated by a c++ API
                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                for (int i = 0; i < cullResults.visibleLights.Length; ++i)
                {
                    Light light = cullResults.visibleLights[i].light;
                    int dataIndex = HDLightRenderDatabase.instance.FindEntityDataIndex(light);
                    if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                    {
                        //Shuriken lights bullshit: this happens because shuriken lights dont have the HDAdditionalLightData OnEnabled.
                        //Because of this, we have to forcefully create a light render entity on the rendering side. Horrible!!!
                        if (light.TryGetComponent<HDAdditionalLightData>(out var hdAdditionalLightData))
                        {
                            if (!hdAdditionalLightData.lightEntity.valid)
                                hdAdditionalLightData.CreateHDLightRenderEntity(autoDestroy: true);
                        }
                        // This can happen if a scene is created via new asset creation vs proper scene creation dialog. In this situation we create a default additional light data.
                        // This is bad, but should happen *extremely* rarely and all the entities will 99.9% of the time end up in the branch above.
                        else if (light != null && light.type == LightType.Directional)
                        {
                            var hdLightData = light.gameObject.AddComponent<HDAdditionalLightData>();
                            if (hdLightData)
                            {
                                HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLightData);
                            }
                            if (!hdLightData.lightEntity.valid)
                                hdLightData.CreateHDLightRenderEntity(autoDestroy: true);
                        }
                        else
                            dataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);
                    }

                    m_VisibleLightEntityDataIndices[i] = dataIndex;
                    m_VisibleLightBakingOutput[i] = light.bakingOutput;
                    m_VisibleLightShadowCasterMode[i] = light.lightShadowCasterMode;
                    m_VisibleLightShadows[i] = light.shadows;
                }
            }
        }

        private void ProcessShadows(
            HDCamera hdCamera,
            HDShadowManager shadowManager,
            in HDShadowInitParameters inShadowInitParameters,
            in CullingResults cullResults)
        {
            int shadowLights = m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ShadowLights];
            if (shadowLights == 0)
                return;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.ProcessShadows)))
            {
                NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
                var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();

                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                int defaultEntityDataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);

                unsafe
                {
                    HDProcessedVisibleLight* entitiesPtr = (HDProcessedVisibleLight*)m_ProcessedEntities.GetUnsafePtr<HDProcessedVisibleLight>();
                    for (int i = 0; i < shadowLights; ++i)
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
                        additionalLightData.ReserveShadowMap(hdCamera.camera, shadowManager, hdShadowSettings, inShadowInitParameters, visibleLight, entity->lightType);
                    }
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
