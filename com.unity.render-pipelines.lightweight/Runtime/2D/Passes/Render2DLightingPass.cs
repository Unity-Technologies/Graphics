using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class Render2DLightingPass : ScriptableRenderPass
    {
        static SortingLayer[] s_SortingLayers;
        _2DRendererData m_RendererData;
        static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("CombinedShapeLight");

        public Render2DLightingPass(_2DRendererData rendererData)
        {
            if (s_SortingLayers == null)
                s_SortingLayers = SortingLayer.layers;

            m_RendererData = rendererData;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                s_SortingLayers = SortingLayer.layers;
#endif
            Camera camera = renderingData.cameraData.camera;
            RendererLighting.Setup(m_RendererData);

            CommandBuffer cmd = CommandBufferPool.Get("Render 2D Lighting");

            Profiler.BeginSample("RenderSpritesWithLighting - Create Render Textures");
            RendererLighting.CreateRenderTextures(cmd, camera);
            Profiler.EndSample();

            cmd.SetGlobalFloat("_LightIntensityScale", m_RendererData.lightIntensityScale);
            cmd.SetGlobalFloat("_InverseLightIntensityScale", 1.0f / m_RendererData.lightIntensityScale);
            RendererLighting.SetShapeLightShaderGlobals(cmd);

            context.ExecuteCommandBuffer(cmd);

            Profiler.BeginSample("RenderSpritesWithLighting - Prepare");
            DrawingSettings drawSettings = CreateDrawingSettings(k_CombinedRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

            FilteringSettings filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;
            Profiler.EndSample();

            bool cleared = false;
            for (int i = 0; i < s_SortingLayers.Length; i++)
            {
                short layerValue = (short)s_SortingLayers[i].value;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerValue, layerValue);

                RendererLighting.RenderNormals(context, renderingData.cullResults, drawSettings, filterSettings);

                cmd.Clear();
                int layerToRender = s_SortingLayers[i].id;
                RendererLighting.RenderLights(camera, cmd, layerToRender);

                // This should have an optimization where I can determine if this needs to be called.
                // And the clear is only needed if no previous pass has cleared the camera RT yet.
                var clearFlag = cleared ? ClearFlag.None : ClearFlag.All;
                var clearColor = renderingData.cameraData.camera.backgroundColor;
                cleared = true;
                SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, clearFlag, clearColor, TextureDimension.Tex2D);
                
                context.ExecuteCommandBuffer(cmd);

                Profiler.BeginSample("RenderSpritesWithLighting - Draw Renderers");
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                Profiler.EndSample();

                cmd.Clear();
                RendererLighting.RenderLightVolumes(camera, cmd, layerToRender);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
            RendererLighting.ReleaseRenderTextures(cmd);
            Profiler.EndSample();

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            filterSettings.sortingLayerRange = SortingLayerRange.all;
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
        }
    }
}
