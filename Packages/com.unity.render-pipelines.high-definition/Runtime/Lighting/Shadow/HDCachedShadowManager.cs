using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Note: The punctual and area light shadows have a specific atlas, however because there can be only be only one directional light casting shadow
    // we use this cached shadow manager only as a source of utilities functions, but the data is stored in the dynamic shadow atlas.

    /// <summary>
    /// The class responsible to handle cached shadow maps (shadows with Update mode set to OnEnable or OnDemand).
    /// </summary>
    public class HDCachedShadowManager
    {
        private static HDCachedShadowManager s_Instance = new HDCachedShadowManager();
        /// <summary>
        /// Get the cached shadow manager to control cached shadow maps.
        /// </summary>
        public static HDCachedShadowManager instance { get { return s_Instance; } }

        // Data for cached directional light shadows.
        private const int m_MaxShadowCascades = 4;
        private BitArray8 m_DirectionalShadowHasRendered;
        private BitArray8 m_DirectionalShadowPendingUpdate;
        private bool m_AllowDirectionalMixedCached = false;

        internal BitArray8 directionalShadowPendingUpdate => m_DirectionalShadowPendingUpdate;
        internal float3 cachedDirectionalAngles;

        internal const int k_MinSlotSize = 64;

        // Helper array used to check what has been tmp filled.
        private (int, int)[] m_TempFilled = new (int, int)[6];

        // Cached atlas
        internal HDCachedShadowAtlas punctualShadowAtlas;
        internal HDCachedShadowAtlas areaShadowAtlas;
        // This is only used for mixed cached shadows and directional, don't really need any capabilities of either the dynamic and cached versions of the atlas, but instead just a render shadows capabilities and
        // a physical atlas to write to. This is updated with the same frequency as the normal cached shadows and only when we decide that we want mixed cached shadow.
        internal HDShadowAtlas directionalLightAtlas;
        private int m_DirectionalLightCacheSize = 1;

        // Cache here to be able to compute resolutions.
        private HDShadowInitParameters m_InitParams;

        // Empty data to be used if area light is off
        internal HDCachedShadowAtlasDataForShadowRequestUpdateJob emptyAreaShadowAtlasJob;

        // ------------------------ Public API -------------------------------

        /// <summary>
        /// This function verifies if a shadow map of resolution shadowResolution for a light of type lightType would fit in the atlas when inserted.
        /// </summary>
        /// <param name="shadowResolution">The resolution of the hypothetical shadow map that we are assessing.</param>
        /// <param name="lightType">The type of the light that cast the hypothetical shadow map that we are assessing.</param>
        /// <returns>True if the shadow map would fit in the atlas, false otherwise.</returns>
        public bool WouldFitInAtlas(int shadowResolution, LightType lightType)
        {
            bool fits = true;
            int x = 0;
            int y = 0;

            if (lightType == LightType.Point)
            {
                int fitted = 0;
                for (int i = 0; i < 6; ++i)
                {
                    fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, true, out x, out y);
                    if (fits)
                    {
                        m_TempFilled[fitted++] = (x, y);
                    }
                    else
                    {
                        // Free the temp filled ones.
                        for (int filled = 0; filled < fitted; ++filled)
                        {
                            HDShadowManager.cachedShadowManager.punctualShadowAtlas.FreeTempFilled(m_TempFilled[filled].Item1, m_TempFilled[filled].Item2, shadowResolution);
                        }
                        return false;
                    }
                }

                // Free the temp filled ones.
                for (int filled = 0; filled < fitted; ++filled)
                {
                    HDShadowManager.cachedShadowManager.punctualShadowAtlas.FreeTempFilled(m_TempFilled[filled].Item1, m_TempFilled[filled].Item2, shadowResolution);
                }
            }

            if (lightType.IsSpot())
                fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            if (lightType.IsArea())
                fits = fits && HDShadowManager.cachedShadowManager.areaShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            return fits;
        }

        /// <summary>
        /// This function verifies if the shadow map for the passed light would fit in the atlas when inserted.
        /// </summary>
        /// <param name="lightData">The light that we try to fit in the atlas.</param>
        /// <returns>True if the shadow map would fit in the atlas, false otherwise. If lightData does not cast shadows, false is returned.</returns>
        public bool WouldFitInAtlas(HDAdditionalLightData lightData)    
        {
            if (lightData.legacyLight.shadows != LightShadows.None)
            {
                var lightType = lightData.legacyLight.type;
                var resolution = lightData.GetResolutionFromSettings(lightData.GetShadowMapType(lightType), m_InitParams, cachedResolution: true);
                return WouldFitInAtlas(resolution, lightType);
            }
            return false;
        }

        /// <summary>
        /// If a light is added after a scene is loaded, its placement in the atlas might be not optimal and the suboptimal placement might prevent a light to find a place in the atlas.
        /// This function will force a defragmentation of the atlas containing lights of type lightType and redistributes the shadows inside so that the placement is optimal. Note however that this will also mark the shadow maps
        /// as dirty and they will be re-rendered as soon the light will come into view for the first time after this function call.
        /// </summary>
        /// <param name="lightType">The type of the light contained in the atlas that need defragmentation.</param>
        public void DefragAtlas(LightType lightType)
        {
            if (lightType.IsArea())
                instance.areaShadowAtlas.DefragmentAtlasAndReRender(instance.m_InitParams);
            if (lightType.IsSpot() || lightType == LightType.Point)
                instance.punctualShadowAtlas.DefragmentAtlasAndReRender(instance.m_InitParams);
        }

        /// <summary>
        /// This function can be used to evict a light from its atlas. The slots occupied by such light will be available to be occupied by other shadows.
        /// Note that eviction happens automatically upon light destruction and, if lightData.preserveCachedShadow is false, upon disabling of the light.
        /// </summary>
        /// <param name="lightData">The light to evict from the atlas.</param>
        public void ForceEvictLight(HDAdditionalLightData lightData)
        {
            EvictLight(lightData, lightData.legacyLight.type);
            lightData.lightIdxForCachedShadows = -1;
        }

        /// <summary>
        /// This function can be used to register a light to the cached shadow system if not already registered. It is necessary to call this function if a light has been
        /// evicted with ForceEvictLight and it needs to be registered again. Please note that a light is automatically registered when enabled or when the shadow update changes
        /// from EveryFrame to OnDemand or OnEnable.
        /// </summary>
        /// <param name="lightData">The light to register.</param>
        public void ForceRegisterLight(HDAdditionalLightData lightData)
        {
            // Note: this is for now just calling the internal API, but having a separate API helps with future
            // changes to the process.
            RegisterLight(lightData);
        }

        /// <summary>
        /// This function verifies if the light has its shadow maps placed in the cached shadow atlas.
        /// </summary>
        /// <param name="lightData">The light that we want to check the placement of.</param>
        /// <returns>True if the shadow map is already placed in the atlas, false otherwise.</returns>
        public bool LightHasBeenPlacedInAtlas(HDAdditionalLightData lightData)
        {
            var lightType = lightData.legacyLight.type;
            if (lightType.IsArea())
                return instance.areaShadowAtlas.LightIsPlaced(lightData);
            if (lightType.IsSpot() || lightType == LightType.Point)
                return instance.punctualShadowAtlas.LightIsPlaced(lightData);
            if (lightType == LightType.Directional)
                return !lightData.ShadowIsUpdatedEveryFrame();

            return false;
        }

        /// <summary>
        /// This function verifies if the light has its shadow maps placed in the cached shadow atlas and if it was rendered at least once.
        /// </summary>
        /// <param name="lightData">The light that we want to check.</param>
        /// <param name="numberOfCascades">Optional parameter required only when querying data about a directional light. It needs to match the number of cascades used by the directional light.</param>
        /// <returns>True if the shadow map is already placed in the atlas and rendered at least once, false otherwise.</returns>
        public bool LightHasBeenPlaceAndRenderedAtLeastOnce(HDAdditionalLightData lightData, int numberOfCascades = 0)
        {
            var lightType = lightData.legacyLight.type;
            if (lightType.IsArea())
            {
                return instance.areaShadowAtlas.LightIsPlaced(lightData) && instance.areaShadowAtlas.FullLightShadowHasRenderedAtLeastOnce(lightData);
            }
            if (lightType.IsSpot() || lightType == LightType.Point)
            {
                return instance.punctualShadowAtlas.LightIsPlaced(lightData) && instance.punctualShadowAtlas.FullLightShadowHasRenderedAtLeastOnce(lightData);
            }
            if (lightType == LightType.Directional)
            {
                Debug.Assert(numberOfCascades <= m_MaxShadowCascades, "numberOfCascades is bigger than the maximum cascades allowed");
                bool hasRendered = true;
                for (int i = 0; i < numberOfCascades; ++i)
                {
                    hasRendered = hasRendered && m_DirectionalShadowHasRendered[(uint)i];
                }
                return !lightData.ShadowIsUpdatedEveryFrame() && hasRendered;
            }

            return false;
        }

        /// <summary>
        /// This function verifies if the light if a specific sub-shadow maps is placed in the cached shadow atlas and if it was rendered at least once.
        /// </summary>
        /// <param name="lightData">The light that we want to check.</param>
        /// <param name="shadowIndex">The sub-shadow index (e.g. cascade index or point light face). It is ignored when irrelevant to the light type.</param>
        /// <returns>True if the shadow map is already placed in the atlas and rendered at least once, false otherwise.</returns>
        public bool ShadowHasBeenPlaceAndRenderedAtLeastOnce(HDAdditionalLightData lightData, int shadowIndex)
        {
            var lightType = lightData.legacyLight.type;
            if (lightType.IsArea())
            {
                return instance.areaShadowAtlas.LightIsPlaced(lightData) && instance.areaShadowAtlas.ShadowHasRenderedAtLeastOnce(lightData.lightIdxForCachedShadows);
            }
            if (lightType.IsSpot())
            {
                return instance.punctualShadowAtlas.LightIsPlaced(lightData) && instance.punctualShadowAtlas.ShadowHasRenderedAtLeastOnce(lightData.lightIdxForCachedShadows);
            }
            if (lightType == LightType.Point)
            {
                Debug.Assert(shadowIndex < 6, "Shadow Index is bigger than the available sub-shadows");
                return instance.punctualShadowAtlas.LightIsPlaced(lightData) && instance.punctualShadowAtlas.ShadowHasRenderedAtLeastOnce(lightData.lightIdxForCachedShadows + shadowIndex);
            }
            if (lightType == LightType.Directional)
            {
                Debug.Assert(shadowIndex < m_MaxShadowCascades, "Shadow Index is bigger than the maximum cascades allowed");
                return !lightData.ShadowIsUpdatedEveryFrame() && m_DirectionalShadowHasRendered[(uint)shadowIndex];
            }

            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------

        private void MarkAllDirectionalShadowsForUpdate()
        {
            for (int i = 0; i < m_MaxShadowCascades; ++i)
            {
                m_DirectionalShadowPendingUpdate[(uint)i] = true;
                m_DirectionalShadowHasRendered[(uint)i] = false;
            }
        }

        private HDCachedShadowManager()
        {
            punctualShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.PunctualAtlas);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.AreaLightAtlas);
            else
                emptyAreaShadowAtlasJob.initEmpty();

            directionalLightAtlas = new HDShadowAtlas();
        }

        internal void InitDirectionalState(HDShadowAtlas.HDShadowAtlasInitParameters atlasInitParams, bool allowMixedCachedShadows)
        {

            // ----- TODO : THIS INIT PARAMS SHOWS 1x1, this assumes that the resolution is set via other means.

            m_AllowDirectionalMixedCached = allowMixedCachedShadows;
            // If we allow mixed cached shadows for Directional Lights we need an auxiliary texture, but no need to allocate it if we don't do mixed cached shadows.
            if (m_AllowDirectionalMixedCached)
            {
                m_DirectionalLightCacheSize = atlasInitParams.width;
                atlasInitParams.isShadowCache = true;
                atlasInitParams.useSharedTexture = true;
                directionalLightAtlas.InitAtlas(atlasInitParams);
            }
        }

        internal void InitPunctualShadowAtlas(HDShadowAtlas.HDShadowAtlasInitParameters atlasInitParams)
        {
            m_InitParams = atlasInitParams.initParams;
            atlasInitParams.isShadowCache = true; // Should be already correctly set by this point, but enforcing it doesn't hurt
            punctualShadowAtlas.InitAtlas(atlasInitParams);
        }

        internal void InitAreaLightShadowAtlas(HDShadowAtlas.HDShadowAtlasInitParameters atlasInitParams)
        {
            m_InitParams = atlasInitParams.initParams;
            atlasInitParams.isShadowCache = true; // Should be already correctly set by this point, but enforcing it doesn't hurt
            areaShadowAtlas.InitAtlas(atlasInitParams);
        }

        internal bool DirectionalHasCachedAtlas()
        {
            return m_AllowDirectionalMixedCached;
        }

        internal void UpdateDirectionalCacheTexture(RenderGraph renderGraph)
        {
            TextureHandle cacheHandle = directionalLightAtlas.GetOutputTexture(renderGraph);
            var desiredDesc = directionalLightAtlas.GetAtlasDesc();
            if (m_DirectionalLightCacheSize != desiredDesc.width)
            {
                renderGraph.RefreshSharedTextureDesc(cacheHandle, desiredDesc);
                m_DirectionalLightCacheSize = desiredDesc.width;
            }
        }
        internal void RegisterLight(HDAdditionalLightData lightData)
        {
            if (!lightData.lightEntity.valid || lightData.legacyLight.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
            {
                return;
            }
            LightType lightType = lightData.legacyLight.type;

            if (lightType == LightType.Directional)
            {
                lightData.lightIdxForCachedShadows = 0;
                MarkAllDirectionalShadowsForUpdate();
            }

            if (lightType.IsSpot() || lightType == LightType.Point)
            {
                punctualShadowAtlas.RegisterLight(lightData);
            }

            if (ShaderConfig.s_AreaLights == 1 && lightType == LightType.Rectangle)
            {
                areaShadowAtlas.RegisterLight(lightData);
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData, LightType cachedLightType)
        {
            if (cachedLightType == LightType.Directional)
            {
                lightData.lightIdxForCachedShadows = -1;
                MarkAllDirectionalShadowsForUpdate();
            }

            if (cachedLightType.IsSpot() || cachedLightType == LightType.Point)
            {
                punctualShadowAtlas.EvictLight(lightData, cachedLightType);
            }

            if (ShaderConfig.s_AreaLights == 1 && cachedLightType.IsArea())
            {
                areaShadowAtlas.EvictLight(lightData, cachedLightType);
            }
        }

        internal void RegisterTransformToCache(HDAdditionalLightData lightData)
        {
            LightType lightType = lightData.legacyLight.type;

            if (lightType.IsSpot() || lightType == LightType.Point)
                punctualShadowAtlas.RegisterTransformCacheSlot(lightData);
            if (ShaderConfig.s_AreaLights == 1 && lightType.IsArea())
                areaShadowAtlas.RegisterTransformCacheSlot(lightData);
            if (lightType == LightType.Directional)
                cachedDirectionalAngles = lightData.transform.eulerAngles;
        }

        internal void AssignSlotsInAtlases()
        {
            punctualShadowAtlas.AssignOffsetsInAtlas(m_InitParams);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.AssignOffsetsInAtlas(m_InitParams);
        }

        internal void MarkDirectionalShadowAsRendered(int shadowIdx)
        {
            m_DirectionalShadowPendingUpdate[(uint)shadowIdx] = false;
            m_DirectionalShadowHasRendered[(uint)shadowIdx] = true;
        }

        internal void OverrideShadowResolutionRequestWithCachedData(ref HDShadowResolutionRequest request, int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.OverrideShadowResolutionRequestWithCachedData(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.OverrideShadowResolutionRequestWithCachedData(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.CascadedDirectional)
                request.cachedAtlasViewport = request.dynamicAtlasViewport;
        }

        internal void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            punctualShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
            if (m_AllowDirectionalMixedCached)
                directionalLightAtlas.UpdateDebugSettings(lightingDebugSettings);
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData light)
        {
            var lightType = light.legacyLight.type;
            if (lightType == LightType.Point || lightType.IsSpot())
                punctualShadowAtlas.ScheduleShadowUpdate(light);
            else if (lightType.IsArea())
                areaShadowAtlas.ScheduleShadowUpdate(light);
            else if (lightType == LightType.Directional)
            {
                MarkAllDirectionalShadowsForUpdate();
            }
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData light, int subShadowIndex)
        {
            var lightType = light.legacyLight.type;
            if (lightType.IsSpot())
                punctualShadowAtlas.ScheduleShadowUpdate(light);
            if (lightType.IsArea())
                areaShadowAtlas.ScheduleShadowUpdate(light);
            if (lightType == LightType.Point)
            {
                Debug.Assert(subShadowIndex < 6);
                punctualShadowAtlas.ScheduleShadowUpdate(light.lightIdxForCachedShadows + subShadowIndex);
            }
            if (lightType == LightType.Directional)
            {
                Debug.Assert(subShadowIndex < m_MaxShadowCascades);
                m_DirectionalShadowPendingUpdate[(uint)subShadowIndex] = true;
            }
        }

        internal bool LightIsPendingPlacement(int lightIdxForCachedShadows, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.LightIsPendingPlacement(lightIdxForCachedShadows);

            return false;
        }

        internal void GetUnmanagedDataForShadowRequestJobs(ref HDCachedShadowManagerDataForShadowRequestUpdateJob dataForShadowRequestUpdateJob)
        {

            dataForShadowRequestUpdateJob.directionalShadowPendingUpdate = m_DirectionalShadowPendingUpdate;
            punctualShadowAtlas.GetUnmanageDataForShadowRequestJobs(ref dataForShadowRequestUpdateJob.punctualShadowAtlas);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.GetUnmanageDataForShadowRequestJobs(ref dataForShadowRequestUpdateJob.areaShadowAtlas);
            else
                dataForShadowRequestUpdateJob.areaShadowAtlas = emptyAreaShadowAtlasJob;

            dataForShadowRequestUpdateJob.directionalLightAtlas.shadowRequests = directionalLightAtlas.m_ShadowRequests;
            dataForShadowRequestUpdateJob.directionalHasCachedAtlas = DirectionalHasCachedAtlas();
        }

        internal void ClearShadowRequests()
        {
            punctualShadowAtlas.Clear();
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.Clear();
            if (m_AllowDirectionalMixedCached)
                directionalLightAtlas.Clear();
        }

        internal void Cleanup(RenderGraph renderGraph)
        {
            if (m_AllowDirectionalMixedCached)
            {
                directionalLightAtlas.Release(renderGraph);
            }
            punctualShadowAtlas.Release(renderGraph);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.Release(renderGraph);
        }

        internal void DisposeNativeCollections()
        {
            if (directionalLightAtlas != null)
                directionalLightAtlas.DisposeNativeCollections();

            if (punctualShadowAtlas != null)
                punctualShadowAtlas.DisposeNativeCollections();

            if (areaShadowAtlas != null)
                areaShadowAtlas.DisposeNativeCollections();

            emptyAreaShadowAtlasJob.DisposeNativeCollections();
        }
    }
}
