using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    // Dispose

//  TODO_FCC: SUPER IMPORTANT! CONSIDER THE CONFIG FILE FOR AREA LIGHT SHADOWS.

    
    class HDCachedShadowManager
    {
        // TODO_FCC: TODO Need to make it public somehow. Think later how. 
        static bool WouldFitInAtlas(int shadowResolution, HDLightType lightType)
        {
            bool fits = true;
            int x, y;

            if (lightType == HDLightType.Point)
            {
                for(int i=0; i < 6; ++i)
                {
                    fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);
                }
            }

            if(lightType == HDLightType.Spot)
                fits = fits && HDShadowManager.cachedShadowManager.punctualShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            if(lightType == HDLightType.Area)
                fits = fits && HDShadowManager.cachedShadowManager.areaShadowAtlas.FindSlotInAtlas(shadowResolution, out x, out y);

            return fits;
        }

        // TODO: MAKE THIS NOT PUBLIC OR INTERNAL AGAIN.
        internal HDCachedShadowAtlas punctualShadowAtlas;
        internal HDCachedShadowAtlas areaShadowAtlas;

        private HDShadowInitParameters m_initParams;        // Cache here to be able to compute resolutions. 


        internal HDCachedShadowManager()
        {
            punctualShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.PunctualAtlas);
            areaShadowAtlas = new HDCachedShadowAtlas(ShadowMapType.AreaLightAtlas);
        }

        internal void InitPunctualShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, int atlasSizeShaderID, Material clearMaterial, int maxShadowRequests, HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            punctualShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, atlasSizeShaderID, clearMaterial, maxShadowRequests, blurAlgorithm, filterMode, depthBufferBits, format, name, momentAtlasShaderID);
        }

        internal void InitAreaLightShadowAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, int atlasSizeShaderID, Material clearMaterial, int maxShadowRequests, HDShadowAtlas.BlurAlgorithm blurAlgorithm = HDShadowAtlas.BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            areaShadowAtlas.InitAtlas(renderPipelineResources, width, height, atlasShaderID, atlasSizeShaderID, clearMaterial, maxShadowRequests, blurAlgorithm, filterMode, depthBufferBits, format, name, momentAtlasShaderID);
        }

        internal void RegisterLight(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;
            Debug.Log(lightType != HDLightType.Directional); // TODO_FCC: HANDLE DIRECTIONAL!!

            if(lightType == HDLightType.Area)
            {
                areaShadowAtlas.RegisterLight(lightData);
            }
            else
            {
                punctualShadowAtlas.RegisterLight(lightData);
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            Debug.Assert(lightData.type != HDLightType.Directional); // TODO_FCC: HANDLE DIRECTIONAL!!
            if (lightType == HDLightType.Area)
            {
                areaShadowAtlas.EvictLight(lightData);
            }
            else
            {
                punctualShadowAtlas.EvictLight(lightData);
            }
        }

        internal void AssignSlotsInAtlases(HDShadowInitParameters initParams)
        {
            m_initParams = initParams;
            punctualShadowAtlas.AssignOffsetsInAtlas(initParams);
            areaShadowAtlas.AssignOffsetsInAtlas(initParams);
        }

        internal bool ShadowIsPendingUpdate(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                return punctualShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                return areaShadowAtlas.ShadowIsPendingRendering(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
            {
                Debug.Assert(false, "NOT SUPPORTED CASCADE DIRECTIONAL YET, PLS FIX"); // Not supported yet....
            }

            return false;
        }

        internal void MarkShadowAsRendered(int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.MarkAsRendered(shadowIdx);
            if (shadowMapType == ShadowMapType.CascadedDirectional)
            {
                Debug.Assert(false, "NOT SUPPORTED CASCADE DIRECTIONAL YET, PLS FIX"); // Not supported yet....
            }
        }

        internal void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx, ShadowMapType shadowMapType)
        {
            if (shadowMapType == ShadowMapType.PunctualAtlas)
                punctualShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.AreaLightAtlas)
                areaShadowAtlas.UpdateResolutionRequest(ref request, shadowIdx);
            else if (shadowMapType == ShadowMapType.CascadedDirectional)
                Debug.Assert(false, "NOT SUPPORTED CASCADE DIRECTIONAL YET, PLS FIX"); // Not supported yet....
        }

        internal void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            punctualShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
            areaShadowAtlas.UpdateDebugSettings(lightingDebugSettings);
        }

        internal void ClearShadowRequests()
        {
            punctualShadowAtlas.Clear();
            if (ShaderConfig.s_AreaLights == 1)
                areaShadowAtlas.Clear();
        }
        
        // DEBUG FUNCTIONS DELETE
        internal void DebugPrintPunctualLightAtlas()
        {
            punctualShadowAtlas.DebugPrintAtlas();
        }


    }
}
