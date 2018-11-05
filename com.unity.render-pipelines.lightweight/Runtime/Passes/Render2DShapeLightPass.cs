using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{

    public class Render2DShapeLightPass : ScriptableRenderPass
    {
        //static RenderTargetHandle m_SpecularLightRTHandle;
        //static RenderTargetHandle m_AmbientLightRTHandle;
        //static RenderTargetHandle m_RimLightRTHandle;

        //static Color m_DefaultAmbientColor;
        //static Color m_DefaultRimColor;
        //static Color m_DefaultSpecularColor;

        //const string k_UseSpecularTexture = "USE_SPECULAR_TEXTURE";
        //const string k_UseAmbientTexture = "USE_AMBIENT_TEXTURE";
        //const string k_UseRimTexture = "USE_RIM_TEXTURE";

        //const float k_OverbrightMultiplier = 8.0f;
        //const string k_CreateShapeLightTag = "Render 2D Shape Light Pass";

        //static SortingLayer[] m_SortingLayers;

        ////==========================================================================================================================
        //// private functions
        ////==========================================================================================================================

        //void CreateRenderTexture(CommandBuffer cmd, RenderTargetHandle handle, RenderTextureDescriptor descriptor, FilterMode filterMode)
        //{
        //    if (handle != RenderTargetHandle.CameraTarget)
        //    {
        //        var rtDescriptor = descriptor;
        //        cmd.GetTemporaryRT(handle.id, rtDescriptor, filterMode);
        //    }
        //}


        //void CreateRenderTextures(CommandBuffer cmd, Default2DRendererSetup default2DRenderSetup)
        //{
        //    // The format here should come from graphics tier settings. For now we will get them from
        //    RenderTextureDescriptor descriptor;
        //    FilterMode filterMode;

        //    default2DRenderSetup.GetSpecularRenderTextureInfo(out descriptor, out filterMode);
        //    CreateRenderTexture(cmd, m_SpecularLightRTHandle, descriptor, filterMode);
        //    default2DRenderSetup.GetAmbientRenderTextureInfo(out descriptor, out filterMode);
        //    CreateRenderTexture(cmd, m_AmbientLightRTHandle, descriptor, filterMode);
        //    default2DRenderSetup.GetRimRenderTextureInfo(out descriptor, out filterMode);
        //    CreateRenderTexture(cmd, m_RimLightRTHandle, descriptor, filterMode);
        //}

        //static void ReleaseRenderTextures(CommandBuffer cmd)
        //{
        //    cmd.ReleaseTemporaryRT(m_SpecularLightRTHandle.id);
        //    cmd.ReleaseTemporaryRT(m_AmbientLightRTHandle.id);
        //    cmd.ReleaseTemporaryRT(m_RimLightRTHandle.id);
        //}

        //static void ClearTarget(CommandBuffer cmdBuffer, RenderTexture renderTexture, Color color, string shaderKeyword)
        //{
        //    cmdBuffer.DisableShaderKeyword(shaderKeyword);
        //    if (renderTexture != null)
        //    {
        //        cmdBuffer.SetRenderTarget(renderTexture);
        //        cmdBuffer.ClearRenderTarget(false, true, color, 1.0f);
        //    }
        //}

        //static void RenderLightSet(CommandBuffer cmdBuffer, int layerToRender, RenderTargetHandle renderTargetHandle, Color clearColor, string shaderKeyword, List<Light2D> lights)
        //{
        //    // This should only be called if we have a valid renderTargetHandle
        //    cmdBuffer.DisableShaderKeyword(shaderKeyword);
        //    bool renderedFirstLight = false;
        //    if (lights.Count > 0)
        //    {
        //        for (int i = 0; i < lights.Count; i++)
        //        {
        //            Light2D light = lights[i];
        //            if (light.IsLitLayer(layerToRender) && light.isActiveAndEnabled && light.m_LightIntensity > 0)
        //            {
        //                Material shapeLightMaterial = light.GetMaterial();
        //                if (shapeLightMaterial != null)
        //                {
        //                    Mesh lightMesh = light.GetMesh();
        //                    if (lightMesh != null)
        //                    {
        //                        if (!renderedFirstLight)
        //                        {
        //                            SetRenderTarget(cmdBuffer, renderTargetHandle.id, RenderBufferLoadAction.Clear, RenderBufferStoreAction.Resolve, ClearFlag.Color, clearColor, TextureDimension.Tex2D);
        //                            renderedFirstLight = true;
        //                        }

        //                        cmdBuffer.EnableShaderKeyword(shaderKeyword);
        //                        cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightMaterial);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //static void RenderLights(CommandBuffer cmdBuffer, int layerToRender)
        //{
        //    cmdBuffer.SetGlobalFloat("_LightIntensityScale", Light2D.GetIntensityScale());

        //    cmdBuffer.BeginSample("2D Shape Lights - Specular Lights");
        //    List<Light2D> specularLights = Light2D.GetSpecularLights();
        //    RenderLightSet(cmdBuffer, layerToRender, m_SpecularLightRTHandle, m_DefaultSpecularColor, k_UseSpecularTexture, specularLights);
        //    cmdBuffer.EndSample("2D Shape Lights - Specular Lights");

        //    cmdBuffer.BeginSample("2D Shape Lights - Ambient Lights");
        //    List<Light2D> ambientLights = Light2D.GetAmbientLights();
        //    RenderLightSet(cmdBuffer, layerToRender, m_AmbientLightRTHandle, m_DefaultAmbientColor, k_UseAmbientTexture, ambientLights);
        //    cmdBuffer.EndSample("2D Shape Lights - Ambient Lights");

        //    cmdBuffer.BeginSample("2D Shape Lights - Rim Lights");
        //    List<Light2D> rimLights = Light2D.GetRimLights();
        //    RenderLightSet(cmdBuffer, layerToRender, m_RimLightRTHandle, m_DefaultRimColor, k_UseRimTexture, rimLights);
        //    cmdBuffer.EndSample("2D Shape Lights - Rim Lights");
        //}

        ////==========================================================================================================================
        //// public functions
        ////==========================================================================================================================
        //public void Setup(int layerToRender, Color defaultAmbientColor, ref RenderTargetHandle ambientRTHandle, ref RenderTargetHandle specularRTHandle, ref RenderTargetHandle rimRTHandle)
        //{
        //    m_LayerToRender = layerToRender;

        //    m_AmbientLightRTHandle = new RenderTargetHandle();

        //    // This should probably come from the Light2D. Maybe this value should be assigned to the Light2D when the pipeline is created
        //    m_DefaultAmbientColor = defaultAmbientColor / k_OverbrightMultiplier;
        //    m_DefaultSpecularColor = Color.black;
        //    m_DefaultRimColor = Color.black;

        //    m_AmbientLightRTHandle = ambientRTHandle;
        //    m_SpecularLightRTHandle = specularRTHandle;
        //    m_RimLightRTHandle = rimRTHandle;
        //}

        //public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        //{
        //    CommandBuffer cmd = CommandBufferPool.Get(k_CreateShapeLightTag);
        //    context.ExecuteCommandBuffer(cmd);

        //    SortingLayer[] sortingLayers = SortingLayer.layers;

        //    CommandBufferPool.Release(cmd);
        //}

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }
    }
}
