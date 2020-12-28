using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

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
        private bool[] m_DirectionalShadowPendingUpdate = new bool[m_MaxShadowCascades];
        private Vector3 m_CachedDirectionalForward;
        private Vector3 m_CachedDirectionalAngles;

        // Cached atlas
        internal HDCachedShadowAtlas punctualShadowAtlas;
        internal HDCachedShadowAtlas areaShadowAtlas;
        // Cache here to be able to compute resolutions. 
        private HDShadowInitParameters m_InitParams;

        // ------------------------ Debug API -------------------------------
#if UNITY_EDITOR
        internal void PrintLightStatusInCachedAtlas()
        {
            bool headerPrinted = false;
            var lights = GameObject.FindObjectsOfType<HDAdditionalLightData>();
            foreach (var light in lights)
            {
                ShadowMapType shadowMapType = light.GetShadowMapType(light.type);
                if (instance.LightIsPendingPlacement(light, shadowMapType))
                {
                    if (!headerPrinted)
                    {
                        Debug.Log(" ===== Lights pending placement in the cached shadow atlas: ===== ");
                        headerPrinted = true;
                    }
                    Debug.Log("\t Name: " + light.name + " Type: " + light.type + " Resolution: " + light.GetResolutionFromSettings(shadowMapType, m_InitParams));
                }
            }

            headerPrinted = false;
            foreach (var light in lights)
            {
                ShadowMapType shadowMapType = light.GetShadowMapType(light.type);
                if (!(instance.LightIsPendingPlacement(light, light.GetShadowMapType(light.type))) && light.lightIdxForCachedShadows != -1)
                {
                    if (!headerPrinted)
                    {
                        Debug.Log("===== Lights placed in cached shadow atlas: ===== ");
                        headerPrinted = true;
                    }
                    Debug.Log("\t Name: " + light.name + " Type: " + light.type + " Resolution: " + light.GetResolutionFromSettings(shadowMapType, m_InitParams));
                }
            }
        }
#endif
        // ------------------------ Public API -------------------------------

        /// <summary>
        /// This function verifies if a shadow map of resolution shadowResolution for a light of type lightType would fit in the atlas when inserted. 
        /// </summary>
        /// <param name="shadowResolution">The resolution of the hypothetical shadow map that we are assessing.</param>
        /// <param name="lightType">The type of the light that cast the hypothetical shadow map that we are assessing.</param>
        /// <returns>True if the shadow map would fit in the atlas, false otherwise.</returns>
        public bool WouldFitInAtlas(int shadowResolution, HDLightType lightType)
        {
            bool fits = true;
            int x, y;

            if (lightType == HDLightType.Point)
            {
                for (int i = 0; i < 6; ++i)
                {
                    fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);
                }
            }

            if (lightType == HDLightType.Spot)
                fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            if (lightType == HDLightType.Area)
                fits = fits && HDShadowManager.cachedShadowManager.areaShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            return fits;
        }

        /// <summary>
        /// If a light is added after a scene is loaded, its placement in the atlas might be not optimal and the suboptimal placement might prevent a light to find a place in the atlas.
        /// This function will force a defragmentation of the atlas containing lights of type lightType and redistributes the shadows inside so that the placement is optimal. Note however that this will also mark the shadow maps
        /// as dirty and they will be re-rendered as soon the light will come into view for the first time after this function call. 
        /// </summary>
        /// <param name="lightType">The type of the light contained in the atlas that need defragmentation.</param>
        public void DefragAtlas(HDLightType lightType)
        {
            if (lightType == HDLightType.Area)
                instance.areaShadowAtlas.DefragmentAtlasAndReRender(instance.m_InitParams);
            if (lightType == HDLightType.Point || lightType == HDLightType.Spot)
                instance.punctualShadowAtlas.DefragmentAtlasAndReRender(instance.m_InitParams);
        }

        /// <summary>
        /// This function can be used to evict a light from its atlas. The slots occupied by such light will be available to be occupied by other shadows.
        /// Note that eviction happens automatically upon light destruction and, if lightData.preserveCachedShadow is false, upon disabling of the light.
        /// </summary>
        /// <param name="lightData">The light to evict from the atlas.</param>
        public void ForceEvictLight(HDAdditionalLightData lightData)
        {
            EvictLight(lightData);
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

        // ------------------------------------------------------------------------------------------------------------------

        private void MarkAllDirectionalShadowsForUpdate()
        {
            for (int i = 0; i < m_MaxShadowCascades; ++i)
            {
                m_DirectionalShadowPendingUpdate[i] = true;
            }
        }

        private HDCachedShadowManager()
        {
            punctualShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.PunctualAtlas);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.AreaLightAtlas);
        }

        internal void InitPunctualShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, Material clearMaterial, int maxShadowRequests, HDShadowInitParameters initParams,
                                            HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            m_InitParams = initParams;
            punctualShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, clearMaterial, maxShadowRequests, initParams, blurAlgorithm, filterMode, depthBufferBits, format, name);
        }

        internal void InitAreaLightShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, Material clearMaterial, int maxShadowRequests, HDShadowInitParameters initParams,
                                            HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            m_InitParams = initParams;
            areaShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, clearMaterial, maxShadowRequests, initParams, blurAlgorithm, filterMode, depthBufferBits, format, name);
        }

        internal void RegisterLight(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            if (lightType == HDLightType.Directional)
            {
                lightData.lightIdxForCachedShadows = 0;
                MarkAllDirectionalShadowsForUpdate();
            }

            if (lightType == HDLightType.Spot || lightType == HDLightType.Point)
            {
                punctualShadowAtlas.RegisterLight(lightData);
            }

            if (ShaderConfig.s_AreaLights == 1 && lightType == HDLightType.Area && lightData.areaLightShape == AreaLightShape.Rectangle)
            {
                areaShadowAtlas.RegisterLight(lightData);
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            if (lightType == HDLightType.Directional)
            {
                lightData.lightIdxForCachedShadows = -1;
                MarkAllDirectionalShadowsForUpdate();
            }

            if (lightType == HDLightType.Spot || lightType == HDLightType.Point)
            {
                punctualShadowAtlas.EvictLight(lightData);
            }

            if (ShaderConfig.s_AreaLights == 1 && lightType == HDLightType.Area)
            {
                areaShadowAtlas.EvictLight(lightData);
            }
        }

        internal void RegisterTransformToCache(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            if (lightType == HDLightType.Spot || lightType == HDLightType.Point)
                punctualShadowAtlas.RegisterTransformCacheSlot(lightData);
            if (ShaderConfig.s_AreaLights == 1 && lightType == HDLightType.Area)
                areaShadowAtlas.RegisterTransformCacheSlot(lightData);
            if (lightType == HDLightType.Directional)
                m_CachedDirectionalAngles = lightData.transform.eulerAngles;
        }

        internal void RemoveTransformFromCache(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            if (lightType == HDLightType.Spot || lightType == HDLightType.Point)
                punctualShadowAtlas.RemoveTransformFromCache(lightData);
            if (ShaderConfig.s_AreaLights == 1 && lightType == HDLightType.Area)
                areaShadowAtlas.RemoveTransformFromCache(lightData);
        }


        internal void AssignSlotsInAtlases()
        {
            punctualShadowAtlas.AssignOffsetsInAtlas(m_InitParams);
            if(ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.AssignOffsetsInAtlas(m_InitParams);
        }

        internal bool NeedRenderingDueToTransformChange(HDAdditionalLightData lightData, HDLightType lightType)
        {
            if(lightData.updateUponLightMovement)
            {
                if (lightType == HDLightType.Directional)
                {
                    float angleDiffThreshold = lightData.cachedShadowAngleUpdateThreshold;
                    Vector3 angleDiff = m_CachedDirectionalAngles - lightData.transform.eulerAngles;
                    return (Mathf.Abs(angleDiff.x) > angleDiffThreshold || Mathf.Abs(angleDiff.y) > angleDiffThreshold || Mathf.Abs(angleDiff.z) > angleDiffThreshold);
                }
                else if (lightType == HDLightType.Area)
                {
                    return areaShadowAtlas.NeedRenderingDueToTransformChange(lightData, lightType);
                }
                else
                {
                    return punctualShadowAtlas.NeedRenderingDueToTransformChange(lightData, lightType);
                }
            }

            return false;
        }

        internal bool ShadowIsPendingUpdate(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
                return m_DirectionalShadowPendingUpdate[shadowIdx];

            return false;
        }

        internal void MarkShadowAsRendered(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
                m_DirectionalShadowPendingUpdate[shadowIdx] = false;
        }

        internal void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.CascadedDirectional)
                request.cachedAtlasViewport = request.dynamicAtlasViewport;
        }

        internal void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            punctualShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData light)
        {
            var lightType = light.type;
            if (lightType == HDLightType.Point || lightType == HDLightType.Spot)
                punctualShadowAtlas.ScheduleShadowUpdate(light);
            else if (lightType == HDLightType.Area)
                areaShadowAtlas.ScheduleShadowUpdate(light);
            else if (lightType == HDLightType.Directional)
            {
                MarkAllDirectionalShadowsForUpdate();
            }
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData light, int subShadowIndex)
        {
            var lightType = light.type;
            if (lightType == HDLightType.Spot)
                punctualShadowAtlas.ScheduleShadowUpdate(light);
            if (lightType == HDLightType.Area)
                areaShadowAtlas.ScheduleShadowUpdate(light);
            if (lightType == HDLightType.Point)
            {
                Debug.Assert(subShadowIndex < 6);
                punctualShadowAtlas.ScheduleShadowUpdate(light.lightIdxForCachedShadows + subShadowIndex);
            }
            if (lightType == HDLightType.Directional)
            {
                Debug.Assert(subShadowIndex < m_MaxShadowCascades);
                m_DirectionalShadowPendingUpdate[subShadowIndex] = true;
            }
        }

        internal bool LightIsPendingPlacement(HDAdditionalLightData light, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.LightIsPendingPlacement(light);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.LightIsPendingPlacement(light);

            return false;
        }

        internal void ClearShadowRequests()
        {
            punctualShadowAtlas.Clear();
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.Clear();
        }

        internal void Dispose()
        {
            punctualShadowAtlas.Release();
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.Release();
        }
    }
}
