using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Render2DLightingPass : ScriptableRenderPass
    {
        public RenderTexture m_NormalMapRT;
        public RenderTexture m_PointLightingRT;

        static CommandBuffer m_CommandBuffer;
        static SortingLayer[] m_SortingLayers;
        static SortingLayerRange m_SortingLayerRange;

        static ShaderTagId m_CombinedShapeLightPassName = new ShaderTagId("CombinedShapeLight");

        public Render2DLightingPass()
        {
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "Lights and Shadows Command Buffer";
            m_SortingLayers = SortingLayer.layers;
        }

        public void Setup(float lightIntensityScale, _2DLightOperationDescription[] lightOperations, Camera camera)
        {
            float inverseLightIntensityScale = 1 / lightIntensityScale;

            Shader.SetGlobalFloat("_LightIntensityScale", lightIntensityScale);
            Shader.SetGlobalFloat("_InverseLightIntensityScale", inverseLightIntensityScale);

            RendererLighting.Setup(lightOperations, camera);
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
            Profiler.EndSample();

            Profiler.BeginSample("RenderSpritesWithLighting - Create Render Textures");
            RendererLighting.CreateRenderTextures(context);
            Profiler.EndSample();

            m_CommandBuffer.Clear();
            RendererLighting.SetShapeLightShaderGlobals(m_CommandBuffer);
            context.ExecuteCommandBuffer(m_CommandBuffer);

            bool cleared = false;
            for (int i = 0; i < m_SortingLayers.Length; i++)
            {
                m_CommandBuffer.Clear();
                int layerToRender = m_SortingLayers[i].id;
                short layerValue = (short)m_SortingLayers[i].value;

                m_SortingLayerRange = new SortingLayerRange(layerValue, layerValue);
                filterSettings.sortingLayerRange = m_SortingLayerRange;

                RendererLighting.RenderNormals(context, renderingData.cullResults, drawSettings, filterSettings);
                RendererLighting.RenderLights(camera, m_CommandBuffer, layerToRender);

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

                m_CommandBuffer.Clear();
                RendererLighting.RenderLightVolumes(camera, m_CommandBuffer, layerToRender, Light2D.LightProjectionTypes.Shape);
                RendererLighting.RenderLightVolumes(camera, m_CommandBuffer, layerToRender, Light2D.LightProjectionTypes.Point);
                context.ExecuteCommandBuffer(m_CommandBuffer);
            }

            Profiler.BeginSample("RenderSpritesWithLighting - Release RenderTextures");
            RendererLighting.ReleaseRenderTextures(context);
            Profiler.EndSample();
        }
    }
}
