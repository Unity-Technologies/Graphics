using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    // Consider getting negative lights working
    public class RendererShapeLights
    {
        static private RenderTextureFormat m_RenderTextureFormatToUse;
        static RenderTexture m_FullScreenSpecularLightTexture = null;
        static RenderTexture m_FullScreenAmbientLightTexture = null;
        static RenderTexture m_FullScreenRimLightTexture = null;
        static RenderTexture m_FullScreenShadowTexture = null;

        static Color m_DefaultAmbientColor;
        static Color m_DefaultRimColor;
        static Color m_DefaultSpecularColor;

        const string k_UseSpecularTexture = "USE_SPECULAR_TEXTURE";
        const string k_UseAmbientTexture = "USE_AMBIENT_TEXTURE";
        const string k_UseRimTexture = "USE_RIM_TEXTURE";

        static public void Setup(Color defaultAmbientColor, Color defaultSpecularColor, Color defaultRimColor)
        {
            m_RenderTextureFormatToUse = RenderTextureFormat.ARGB32;
            //if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
            //    m_RenderTextureFormatToUse = RenderTextureFormat.RGB111110Float;
            //else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            //    m_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;
            
            m_DefaultSpecularColor = defaultSpecularColor;
            m_DefaultAmbientColor = defaultAmbientColor;
            m_DefaultRimColor = defaultRimColor;
        }

        static public void CreateRenderTextures(Light2DRTInfo ambientLightRTInfo, Light2DRTInfo specularLightRTInfo, Light2DRTInfo rimLightRTInfo)
        {
            m_FullScreenSpecularLightTexture = specularLightRTInfo.GetRenderTexture(m_RenderTextureFormatToUse);
            m_FullScreenAmbientLightTexture = ambientLightRTInfo.GetRenderTexture(m_RenderTextureFormatToUse);
            m_FullScreenRimLightTexture = rimLightRTInfo.GetRenderTexture(m_RenderTextureFormatToUse);
        }

        static public void ReleaseRenderTextures()
        {
            if (m_FullScreenAmbientLightTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_FullScreenAmbientLightTexture);
                m_FullScreenAmbientLightTexture = null;
            }

            if (m_FullScreenSpecularLightTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_FullScreenSpecularLightTexture);
                m_FullScreenSpecularLightTexture = null;
            }

            if (m_FullScreenRimLightTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_FullScreenRimLightTexture);
                m_FullScreenRimLightTexture = null;
            }
        }


        static public void ClearTarget(CommandBuffer cmdBuffer, RenderTexture renderTexture, Color color, string shaderKeyword)
        {
            cmdBuffer.DisableShaderKeyword(shaderKeyword);
            if (renderTexture != null)
            {
                cmdBuffer.SetRenderTarget(renderTexture);
                cmdBuffer.ClearRenderTarget(false, true, color, 1.0f);
            }
        }

        static public void Clear(CommandBuffer cmdBuffer)
        {
            ClearTarget(cmdBuffer, m_FullScreenSpecularLightTexture, m_DefaultSpecularColor, k_UseSpecularTexture);
            ClearTarget(cmdBuffer, m_FullScreenAmbientLightTexture, m_DefaultAmbientColor, k_UseAmbientTexture);
            ClearTarget(cmdBuffer, m_FullScreenRimLightTexture, m_DefaultRimColor, k_UseRimTexture);
        }


        static private void RenderLightSet(CommandBuffer cmdBuffer, int layerToRender, RenderTexture renderTexture, Color fillColor, string shaderKeyword, List<Light2D> lights)
        {
            cmdBuffer.DisableShaderKeyword(shaderKeyword);
            if (renderTexture != null)
            {
                bool renderedFirstLight = false;
                if (lights.Count > 0)
                {
                    for (int i = 0; i < lights.Count; i++)
                    {
                        Light2D light = lights[i];
                        if (light.IsLitLayer(layerToRender) && light.isActiveAndEnabled)
                        {
                            Material shapeLightMaterial = light.GetMaterial();
                            if (shapeLightMaterial != null)
                            {
                                Mesh lightMesh = light.GetMesh();
                                if (lightMesh != null)
                                {
                                    if (!renderedFirstLight)
                                    {
                                        cmdBuffer.SetRenderTarget(renderTexture);
                                        cmdBuffer.ClearRenderTarget(false, true, fillColor, 1.0f);
                                        renderedFirstLight = true;
                                    }

                                    cmdBuffer.EnableShaderKeyword(shaderKeyword);
                                    cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightMaterial);
                                }
                            }
                        }
                    }
                }
            }
        }

        static public void SetShaderGlobals(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalColor("_AmbientColor", m_DefaultAmbientColor);
            cmdBuffer.SetGlobalColor("_RimColor", m_DefaultRimColor);
            cmdBuffer.SetGlobalTexture("_SpecularLightingTex", m_FullScreenSpecularLightTexture);
            cmdBuffer.SetGlobalTexture("_AmbientLightingTex", m_FullScreenAmbientLightTexture);
            cmdBuffer.SetGlobalTexture("_RimLightingTex", m_FullScreenRimLightTexture);
            cmdBuffer.SetGlobalTexture("_ShadowTex", m_FullScreenShadowTexture);
        }


        static public void RenderLights(CommandBuffer cmdBuffer, int layerToRender)
        {
            cmdBuffer.BeginSample("2D Shape Lights - Specular Lights");
            List<Light2D> specularLights = Light2D.GetSpecularLights();
            RenderLightSet(cmdBuffer, layerToRender, m_FullScreenSpecularLightTexture, m_DefaultSpecularColor, k_UseSpecularTexture, specularLights);
            cmdBuffer.EndSample("2D Shape Lights - Specular Lights");

            cmdBuffer.BeginSample("2D Shape Lights - Ambient Lights");
            List<Light2D> ambientLights = Light2D.GetAmbientLights();
            RenderLightSet(cmdBuffer, layerToRender, m_FullScreenAmbientLightTexture, m_DefaultAmbientColor, k_UseAmbientTexture, ambientLights);
            cmdBuffer.EndSample("2D Shape Lights - Ambient Lights");

            cmdBuffer.BeginSample("2D Shape Lights - Rim Lights");
            List<Light2D> rimLights = Light2D.GetRimLights();
            RenderLightSet(cmdBuffer, layerToRender, m_FullScreenRimLightTexture, m_DefaultRimColor, k_UseRimTexture, rimLights);
            cmdBuffer.EndSample("2D Shape Lights - Rim Lights");
        }
    }
}