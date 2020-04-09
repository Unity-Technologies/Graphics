using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    // Note: The punctual and area light shadows have a specific atlas, however because there can be only be only one directional light casting shadow
    // we use this cached shadow manager only as a source of utilities functions, but the data is stored in the dynamic shadow atlas.

    class HDCachedShadowManager
    {
        // Constants
        private const int m_MaxShadowCascades = 4;

        internal HDCachedShadowAtlas punctualShadowAtlas;
        internal HDCachedShadowAtlas areaShadowAtlas;

        private HDShadowInitParameters m_initParams;        // Cache here to be able to compute resolutions. 

        // Data for cached directional light shadows.
        private bool[] m_DirectionalShadowPendingUpdate = new bool[m_MaxShadowCascades];

        // TODO_FCC: TODO Need to make it public somehow. Think later how. 
        static bool WouldFitInAtlas(int shadowResolution, HDLightType lightType)
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

        private void MarkAllDirectionalShadowsForUpdate()
        {
            for (int i = 0; i < m_MaxShadowCascades; ++i)
            {
                m_DirectionalShadowPendingUpdate[i] = true;
            }
        }

        internal HDCachedShadowManager()
        {
            punctualShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.PunctualAtlas);
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.AreaLightAtlas);
        }

        internal void InitPunctualShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, Material clearMaterial, int maxShadowRequests, HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            punctualShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, clearMaterial, maxShadowRequests, blurAlgorithm, filterMode, depthBufferBits, format, name, momentAtlasShaderID);
        }

        internal void InitAreaLightShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, Material clearMaterial, int maxShadowRequests, HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            areaShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, clearMaterial, maxShadowRequests, blurAlgorithm, filterMode, depthBufferBits, format, name, momentAtlasShaderID);
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

            if (ShaderConfig.s_AreaLights == 1 && lightType == HDLightType.Area)
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

        internal void AssignSlotsInAtlases(HDShadowInitParameters initParams)
        {
            m_initParams = initParams;
            punctualShadowAtlas.AssignOffsetsInAtlas(initParams);
            if(ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.AssignOffsetsInAtlas(initParams);
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
