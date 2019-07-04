using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.LWRP;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class Render2DLightingPass : ScriptableRenderPass
    {
        static SortingLayer[] s_SortingLayers;
        Renderer2DData m_RendererData;
        static readonly ShaderTagId k_2DRenderingPassName = new ShaderTagId("Lightweight2D");
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_2DRenderingPassName };

        public Render2DLightingPass(Renderer2DData rendererData)
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
            cmd.Clear();

            Profiler.BeginSample("RenderSpritesWithLighting - Create Render Textures");
            RendererLighting.CreateRenderTextures(cmd, camera);
            Profiler.EndSample();

            cmd.SetGlobalFloat("_HDREmulationScale", m_RendererData.hdrEmulationScale);
            cmd.SetGlobalFloat("_InverseHDREmulationScale", 1.0f / m_RendererData.hdrEmulationScale);


#if UNITY_EDITOR
            bool isPreview = false;
            isPreview = EditorSceneManager.IsPreviewSceneObject(camera);

            if (isPreview)
                RendererLighting.SetPreviewShaderGlobals(cmd);
            else
#endif
                RendererLighting.SetShapeLightShaderGlobals(cmd);

            context.ExecuteCommandBuffer(cmd);

            Profiler.BeginSample("RenderSpritesWithLighting - Prepare");
            DrawingSettings combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            DrawingSettings normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

            FilteringSettings filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;
            Profiler.EndSample();

            for (int i = 0; i < s_SortingLayers.Length; i++)
            {
                // The canvas renderer overrides its sorting layer value with short.MaxValue in the editor.
                // When drawing the last sorting layer, include the range from layerValue to short.MaxValue
                // so that UI can be rendered in the scene view.
                short layerValue = (short)s_SortingLayers[i].value;
                var upperBound = (i == s_SortingLayers.Length - 1) ? short.MaxValue : layerValue;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerValue, upperBound);

                int layerToRender = s_SortingLayers[i].id;

                Light2D.LightStats lightStats;
                lightStats = Light2D.GetLightStatsByLayer(layerToRender);

                if (lightStats.totalNormalMapUsage > 0)
                    RendererLighting.RenderNormals(context, renderingData.cullResults, normalsDrawSettings, filterSettings);

                cmd.Clear();
                if (lightStats.totalLights > 0)
                {
#if UNITY_EDITOR
                    cmd.name = "Render Lights - " + SortingLayer.IDToName(layerToRender);
#endif
                    RendererLighting.RenderLights(camera, cmd, layerToRender);
                }
                else
                {
                    RendererLighting.ClearDirtyLighting(cmd);
                }

                CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.white);
                context.ExecuteCommandBuffer(cmd);

                Profiler.BeginSample("RenderSpritesWithLighting - Draw Transparent Renderers");
                context.DrawRenderers(renderingData.cullResults, ref combinedDrawSettings, ref filterSettings);
                Profiler.EndSample();

                if (lightStats.totalVolumetricUsage > 0)
                {
                    
                    cmd.Clear();
#if UNITY_EDITOR
                    cmd.name = "Render Light Volumes" + SortingLayer.IDToName(layerToRender);
#endif
                    RendererLighting.RenderLightVolumes(camera, cmd, layerToRender);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
            }

            cmd.Clear();
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
