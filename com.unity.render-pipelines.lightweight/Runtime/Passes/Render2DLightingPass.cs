using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class Render2DLightingPass : ScriptableRenderPass
    {
        RenderTargetHandle m_SpecularLightRTHandle;
        RenderTargetHandle m_AmbientLightRTHandle;
        RenderTargetHandle m_RimLightRTHandle;
        RenderTargetHandle m_NormalMapRTHandle;
        RenderTargetHandle m_PointLightRTHandle;  // Probably able to be combined with another texture
        RenderTargetHandle m_ShadowMapRTHandle;   // This will be something we can't combine

        RenderTextureDescriptor m_AmbientRTDescriptor;
        RenderTextureDescriptor m_SpecularRTDescriptor;
        RenderTextureDescriptor m_RimRTDescriptor;

        FilterMode m_AmbientFilterMode;
        FilterMode m_SpecularFilterMode;
        FilterMode m_RimFilterMode;

        static Color m_DefaultAmbientColor;
        static Color m_DefaultRimColor;
        static Color m_DefaultSpecularColor;

        const string k_UseSpecularTexture = "USE_SPECULAR_TEXTURE";
        const string k_UseAmbientTexture = "USE_AMBIENT_TEXTURE";
        const string k_UseRimTexture = "USE_RIM_TEXTURE";

        const float k_OverbrightMultiplier = 8.0f;
        const string k_Render2DLightingPassTag = "Render 2D Shape Light Pass";

        static SortingLayer[] m_SortingLayers;

        //==========================================================================================================================
        // private functions
        //==========================================================================================================================
        void CreateRenderTexture(CommandBuffer cmd, string identifier, ref RenderTargetHandle handle, RenderTextureDescriptor descriptor, FilterMode filterMode)
        {
            handle = new RenderTargetHandle();
            handle.Init(identifier);

            if (handle != RenderTargetHandle.CameraTarget)
            {
                var rtDescriptor = descriptor;
                cmd.GetTemporaryRT(handle.id, rtDescriptor, filterMode);
            }
        }

        void CreateRenderTextures(CommandBuffer cmd)
        {
            // Render textures for shape lights
            CreateRenderTexture(cmd, "_SpecularLightingTex", ref m_SpecularLightRTHandle, m_SpecularRTDescriptor, m_SpecularFilterMode);
            CreateRenderTexture(cmd, "_AmbientLightingTex", ref m_AmbientLightRTHandle, m_AmbientRTDescriptor, m_AmbientFilterMode);
            CreateRenderTexture(cmd, "_RimLightingTex", ref m_RimLightRTHandle, m_RimRTDescriptor, m_RimFilterMode);
        }

        void ReleaseRenderTextures(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_SpecularLightRTHandle.id);
            cmd.ReleaseTemporaryRT(m_AmbientLightRTHandle.id);
            cmd.ReleaseTemporaryRT(m_RimLightRTHandle.id);
        }

        void ClearTarget(CommandBuffer cmdBuffer, RenderTexture renderTexture, Color color, string shaderKeyword)
        { 
            cmdBuffer.DisableShaderKeyword(shaderKeyword);
            if (renderTexture != null)
            {
                cmdBuffer.SetRenderTarget(renderTexture);
                cmdBuffer.ClearRenderTarget(false, true, color, 1.0f);
            }
        }

        void RenderLightSet(CommandBuffer cmdBuffer, int layerToRender, RenderTargetHandle renderTargetHandle, Color clearColor, string shaderKeyword, List<Light2D> lights)
        {
            // This should only be called if we have a valid renderTargetHandle
            cmdBuffer.DisableShaderKeyword(shaderKeyword);
            bool renderedFirstLight = false;
            if (lights.Count > 0)
            {
                for (int i = 0; i < lights.Count; i++)
                {
                    Light2D light = lights[i];
                    if (light.IsLitLayer(layerToRender) && light.isActiveAndEnabled && light.m_LightIntensity > 0)
                    {
                        Material shapeLightMaterial = light.GetMaterial();
                        if (shapeLightMaterial != null)
                        {
                            Mesh lightMesh = light.GetMesh();
                            if (lightMesh != null)
                            {
                                if (!renderedFirstLight)
                                {
                                    cmdBuffer.SetRenderTarget(renderTargetHandle.id);
                                    //SetRenderTarget(cmdBuffer, renderTargetHandle.id, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, clearColor, TextureDimension.Tex2D);
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

        void RenderLights(CommandBuffer cmdBuffer, int layerToRender)
        {
            cmdBuffer.SetGlobalFloat("_LightIntensityScale", Light2D.GetIntensityScale());

            cmdBuffer.BeginSample("2D Shape Lights - Specular Lights");
            List<Light2D> specularLights = Light2D.GetSpecularLights();
            RenderLightSet(cmdBuffer, layerToRender, m_SpecularLightRTHandle, m_DefaultSpecularColor, k_UseSpecularTexture, specularLights);
            cmdBuffer.EndSample("2D Shape Lights - Specular Lights");

            cmdBuffer.BeginSample("2D Shape Lights - Ambient Lights");
            List<Light2D> ambientLights = Light2D.GetAmbientLights();
            RenderLightSet(cmdBuffer, layerToRender, m_AmbientLightRTHandle, m_DefaultAmbientColor, k_UseAmbientTexture, ambientLights);
            cmdBuffer.EndSample("2D Shape Lights - Ambient Lights");

            cmdBuffer.BeginSample("2D Shape Lights - Rim Lights");
            List<Light2D> rimLights = Light2D.GetRimLights();
            RenderLightSet(cmdBuffer, layerToRender, m_RimLightRTHandle, m_DefaultRimColor, k_UseRimTexture, rimLights);
            cmdBuffer.EndSample("2D Shape Lights - Rim Lights");
        }

        //==========================================================================================================================
        // public functions
        //==========================================================================================================================
        public void Setup(Color defaultAmbientColor, RenderTextureDescriptor ambientRTDescriptor, RenderTextureDescriptor specularRTDescriptor, RenderTextureDescriptor rimRTDescriptor, FilterMode ambientFilterMode, FilterMode specularFilterMode, FilterMode rimFilterMode)
        {
            RegisterShaderPassName("LightweightForward");


            // This should probably come from the Light2D. Maybe this value should be assigned to the Light2D when the pipeline is created
            m_DefaultAmbientColor = defaultAmbientColor / k_OverbrightMultiplier;
            m_DefaultSpecularColor = Color.black;
            m_DefaultRimColor = Color.black;

            m_AmbientRTDescriptor = ambientRTDescriptor;
            m_SpecularRTDescriptor = specularRTDescriptor;
            m_RimRTDescriptor = rimRTDescriptor;

            m_AmbientFilterMode = ambientFilterMode;
            m_SpecularFilterMode = specularFilterMode;
            m_RimFilterMode = rimFilterMode;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_Render2DLightingPassTag);
            CreateRenderTextures(cmd);

            cmd.SetGlobalColor("_AmbientColor", m_DefaultAmbientColor);

            // Do my light culling here...
            Camera camera = renderingData.cameraData.camera;
            SortingLayer[] sortingLayers = SortingLayer.layers;
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                //bool isLitLayer = true; // We need to check the to see if any of the lights are using this layer
                int layerToRender = sortingLayers[i].id;
                short layerValue = (short)sortingLayers[i].value;

                SortingSettings sortingSettings = new SortingSettings(camera);
                SortingCriteria criteria = sortingSettings.criteria | SortingCriteria.BackToFront;
                sortingSettings.criteria = criteria;

                FilteringSettings filterSettings = new FilteringSettings();
                filterSettings.layerMask = ~0;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = SortingLayerRange.all;
                DrawingSettings drawSettings = new DrawingSettings();
                drawSettings.enableInstancing = true;
                drawSettings.enableDynamicBatching = true;
                drawSettings.sortingSettings = sortingSettings;

                RenderLights(cmd, layerToRender);
                //context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }

            ReleaseRenderTextures(cmd);
            context.ExecuteCommandBuffer(cmd);
        }
    }
}
