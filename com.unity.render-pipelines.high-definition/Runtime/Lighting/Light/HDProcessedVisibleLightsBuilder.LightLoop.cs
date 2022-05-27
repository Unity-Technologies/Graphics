using System;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDProcessedVisibleLightsBuilder
    {
        private void SortLightKeys()
        {
            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.SortVisibleLights)))
            {
                SortArray(ref m_SortKeys, m_ProcessVisibleLightCounts[(int)ProcessLightsCountSlots.ProcessedLights]);
                SortArray(ref m_SortKeysDGI, m_ProcessDynamicGILightCounts[(int)ProcessLightsCountSlots.ProcessedLights]);
            }
        }

        private void SortArray(ref NativeArray<uint> array, int size)
        {
            //Tunning against ps4 console,
            //32 items insertion sort has a workst case of 3 micro seconds.
            //200 non recursive merge sort has around 23 micro seconds.
            //From 200 and more, Radix sort beats everything.
            if (size <= 32)
                CoreUnsafeUtils.InsertionSort(array, size);
            else if (size <= 200)
                CoreUnsafeUtils.MergeSort(array, size, ref m_SortSupportArray);
            else
                CoreUnsafeUtils.RadixSort(array, size, ref m_SortSupportArray);
        }

        private static Color GetLightFinalColor(Light light)
        {
            var finalColor = light.color.linear * light.intensity;

            if (light.useColorTemperature)
                finalColor *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);

            if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                finalColor = finalColor.gamma;

            return finalColor;
        }

        private void BuildVisibleLightEntities(in CullingResults cullResults, in SortedList<uint, Light> allDGIEnabledLights, bool processDynamicGI)
        {
            m_Size = 0;

            int totalCounts = m_ProcessVisibleLightCounts.IsCreated ? m_ProcessDynamicGILightCounts.Length : Enum.GetValues(typeof(ProcessLightsCountSlots)).Length;

            if (!m_ProcessVisibleLightCounts.IsCreated)
            {
                m_ProcessVisibleLightCounts.ResizeArray(totalCounts);
            }

            if (!m_ProcessDynamicGILightCounts.IsCreated)
            {
                m_ProcessDynamicGILightCounts.ResizeArray(totalCounts);
            }

            for (int i = 0; i < totalCounts; ++i)
            {
                m_ProcessVisibleLightCounts[i] = 0;
                m_ProcessDynamicGILightCounts[i] = 0;
            }

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.BuildVisibleLightEntities)))
            {
                int visibleLightCount = cullResults.visibleLights.Length;
                int dgiLightCount = processDynamicGI ? allDGIEnabledLights.Count : 0;
                int totalLightCount = visibleLightCount + dgiLightCount;

                if (totalLightCount == 0 || HDLightRenderDatabase.instance == null)
                    return;

                if (totalLightCount > m_Capacity)
                {
                    ResizeArrays(totalLightCount);
                }

                //TODO: this should be accelerated by a c++ API
                var defaultEntity = HDLightRenderDatabase.instance.GetDefaultLightEntity();
                int validDGIVisibleLights = 0;
                for (int i = 0; i < visibleLightCount; ++i)
                {
                    Light light = cullResults.visibleLights[i].light;

                    int dataIndex = FindDataIndex(light, ref defaultEntity);

                    StoreLightData(i, light, dataIndex);

                    if (dgiLightCount > 0 && dataIndex != HDLightRenderDatabase.InvalidDataIndex)
                    {
                        m_VisibleLightIDs[validDGIVisibleLights++] = (uint)light.GetInstanceID();
                    }
                }

                m_Size = visibleLightCount;

                if (dgiLightCount > 0)
                {
                    SortArray(ref m_VisibleLightIDs, validDGIVisibleLights);

                    int currentVisibleLightIdx = 0;

                    for (int i = 0; i < dgiLightCount; i++)
                    {
                        uint lightID = allDGIEnabledLights.Keys[i];

                        while (currentVisibleLightIdx < validDGIVisibleLights && m_VisibleLightIDs[currentVisibleLightIdx] < lightID)
                            currentVisibleLightIdx++;
                        if (currentVisibleLightIdx < validDGIVisibleLights && m_VisibleLightIDs[currentVisibleLightIdx] == lightID)
                            continue;

                        Light light = allDGIEnabledLights[lightID];
                        int dataIndex = FindDataIndex(light, ref defaultEntity);

                        if (dataIndex == HDLightRenderDatabase.InvalidDataIndex)
                            continue;

                        StoreLightData(m_Size, light, dataIndex);

                        // Create a fake VisibleLight
                        m_OffscreenDynamicGILights[m_Size - visibleLightCount] = new VisibleLight()
                        {
                            localToWorldMatrix = light.transform.localToWorldMatrix,
                            lightType = light.type,
                            screenRect = default, // Unused for offscreen lights
                            finalColor = GetLightFinalColor(light),
                            range = light.range,
                            spotAngle = light.spotAngle,
                        };

                        m_Size++;
                    }
                }
            }
        }

        private void StoreLightData(int index, Light light, int dataIndex)
        {
            m_VisibleLightEntityDataIndices[index] = dataIndex;
            m_VisibleLightBakingOutput[index] = light.bakingOutput;
            m_VisibleLightShadowCasterMode[index] = light.lightShadowCasterMode;
            m_VisibleLightShadows[index] = light.shadows;
            m_VisibleLightBounceIntensity[index] = light.bounceIntensity;
        }

        private static int FindDataIndex(Light light, ref HDLightRenderEntity defaultEntity)
        {
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
                else
                    dataIndex = HDLightRenderDatabase.instance.GetEntityDataIndex(defaultEntity);
            }
            // custom-begin:
#if UNITY_EDITOR
            else if (UnityEditor.SceneVisibilityManager.instance.IsHidden(light.gameObject))
            {
                dataIndex = HDLightRenderDatabase.InvalidDataIndex;
            }
#endif
            // custom-end
            return dataIndex;
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
                        HDAdditionalLightData additionalLightData = HDLightRenderDatabase.instance.hdAdditionalLightData[entity->dataIndex];

                        if (additionalLightData == null)
                            continue;

                        if ((!cullResults.GetShadowCasterBounds(lightIndex, out var bounds) && additionalLightData.shadowUpdateMode != ShadowUpdateMode.OnDemand) || defaultEntityDataIndex == entity->dataIndex)
                        {
                            entity->shadowMapFlags = ShadowMapFlags.None;
                            continue;
                        }

                        
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
