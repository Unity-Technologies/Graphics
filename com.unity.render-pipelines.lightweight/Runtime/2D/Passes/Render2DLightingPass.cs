using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class Render2DLightingPass : ScriptableRenderPass
    {
        private Light2DRTInfo m_AmbientRenderTextureInfo;
        private Light2DRTInfo m_SpecularRenderTextureInfo;
        private Light2DRTInfo m_RimRenderTextureInfo;
        private Light2DRTInfo m_PointLightNormalRenderTextureInfo;
        private Light2DRTInfo m_PointLightColorRenderTextureInfo;

        public RenderTexture m_NormalMapRT;
        public RenderTexture m_PointLightingRT;

        public Color m_DefaultRimColor = Color.clear;
        public Color m_DefaultSpecularColor = Color.clear;

        static CommandBuffer m_CommandBuffer;
        static SortingLayer[] m_SortingLayers;
        static SortingLayerRange m_SortingLayerRange;

        static ShaderTagId m_CombinedShapeLightPassName = new ShaderTagId("CombinedShapeLight");

        public Render2DLightingPass()
        {
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "Lights and Shadows Command Buffer";
            m_SortingLayers = SortingLayer.layers;

            RendererPointLights.Initialize();
        }

        public void Setup(Color ambientColor, Light2DRTInfo ambientRTInfo, Light2DRTInfo specularRTInfo, Light2DRTInfo rimRTInfo, Light2DRTInfo pointLightNormalRTInfo, Light2DRTInfo pointLightColorRTInfo, float lightIntensityScale)
        {
            m_AmbientRenderTextureInfo = ambientRTInfo;
            m_SpecularRenderTextureInfo = specularRTInfo;
            m_RimRenderTextureInfo = rimRTInfo;
            m_PointLightNormalRenderTextureInfo = pointLightNormalRTInfo;
            m_PointLightColorRenderTextureInfo = pointLightColorRTInfo;

            float inverseLightIntensityScale = 1 / lightIntensityScale;

            Shader.SetGlobalFloat("_LightIntensityScale", lightIntensityScale);
            Shader.SetGlobalFloat("_InverseLightIntensityScale", inverseLightIntensityScale);

            RendererShapeLights.Setup(inverseLightIntensityScale * ambientColor, inverseLightIntensityScale * m_DefaultSpecularColor, inverseLightIntensityScale * m_DefaultRimColor);
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                m_SortingLayers = SortingLayer.layers;
#endif
            Camera camera = renderingData.cameraData.camera;


            SortingSettings sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;

            Profiler.BeginSample("RenderSpritesWithLighting - Prepare");
            DrawingSettings drawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;
            bool renderBuffersDirty = true;
            Profiler.EndSample();

            Profiler.BeginSample("RenderSpritesWithLighting - Create Render Textures");
            RendererShapeLights.CreateRenderTextures(m_AmbientRenderTextureInfo, m_SpecularRenderTextureInfo, m_RimRenderTextureInfo);
            RendererPointLights.CreateRenderTextures(m_PointLightNormalRenderTextureInfo, m_PointLightColorRenderTextureInfo);
            Profiler.EndSample();

            m_CommandBuffer.Clear();
            RendererPointLights.SetShaderGlobals(m_CommandBuffer);
            RendererShapeLights.SetShaderGlobals(m_CommandBuffer);
            RendererPointLights.Clear(m_CommandBuffer);
            RendererShapeLights.Clear(m_CommandBuffer);

            context.ExecuteCommandBuffer(m_CommandBuffer);

            bool cleared = false;
            for (int i = 0; i < m_SortingLayers.Length; i++)
            {
                m_CommandBuffer.Clear();
                bool isLitLayer = true;
                int layerToRender = m_SortingLayers[i].id;
                short layerValue = (short)m_SortingLayers[i].value;

                m_SortingLayerRange = new SortingLayerRange(layerValue, layerValue);
                filterSettings.sortingLayerRange = m_SortingLayerRange;

                if (isLitLayer)
                {
                    RendererPointLights.RenderLights(camera, m_CommandBuffer, context, renderingData.cullResults, drawSettings, filterSettings, layerToRender);
                    RendererShapeLights.RenderLights(camera, m_CommandBuffer, layerToRender);
                    renderBuffersDirty = true;
                }
                else if (renderBuffersDirty)
                {
                    RendererPointLights.Clear(m_CommandBuffer);
                    RendererShapeLights.Clear(m_CommandBuffer);
                    renderBuffersDirty = false;
                }

                // This should have an optimization where I can determine if this needs to be called
                m_CommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                // This is only needed if no previous pass has cleared the camera RT yet.
                if (!cleared)
                {
                    m_CommandBuffer.ClearRenderTarget(true, true, renderingData.cameraData.camera.backgroundColor);
                    cleared = true;
                }

                context.ExecuteCommandBuffer(m_CommandBuffer);

                Profiler.BeginSample("RenderSpritesWithLighting - Draw Renderers");
                drawSettings.SetShaderPassName(0, m_CombinedShapeLightPassName);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                Profiler.EndSample();

                if (isLitLayer)
                {
                    m_CommandBuffer.Clear();
                    RendererShapeLights.RenderLightVolumes(camera, m_CommandBuffer, layerToRender);
                    context.ExecuteCommandBuffer(m_CommandBuffer);
                }
            }

            Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
            RendererPointLights.ReleaseRenderTextures();
            RendererShapeLights.ReleaseRenderTextures();
            Profiler.EndSample();
        }
    }
}
