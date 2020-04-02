using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    // Dispose

    public class HDCachedShadowManager
    {
        // TODO: MAKE THIS NOT PUBLIC OR INTERNAL AGAIN.
        internal HDCachedShadowAtlas punctualShadowAtlas;
        internal HDCachedShadowAtlas areaShadowAtlas;


        internal HDCachedShadowManager()
        {
            punctualShadowAtlas = new HDCachedShadowAtlas();
            areaShadowAtlas = new HDCachedShadowAtlas();
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

            switch (lightType)
            {
                case HDLightType.Point:
                    punctualShadowAtlas.RegisterLight(lightData);
                    break;
                case HDLightType.Area:
                    areaShadowAtlas.RegisterLight(lightData);
                    break;
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            HDLightType lightType = lightData.type;

            Debug.Assert(lightData.type != HDLightType.Directional); // TODO_FCC: HANDLE DIRECTIONAL!!

            switch (lightType)
            {
                case HDLightType.Point:
                    punctualShadowAtlas.EvictLight(lightData);
                    break;
                case HDLightType.Area:
                    areaShadowAtlas.EvictLight(lightData);
                    break;
            }
        }

        internal void AssignSlotsInAtlases(HDShadowInitParameters initParams)
        {
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
