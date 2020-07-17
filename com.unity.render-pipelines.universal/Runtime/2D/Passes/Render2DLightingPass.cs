using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Render2DLightingPass : ScriptableRenderPass
    {
        static SortingLayer[] s_SortingLayers;
        Renderer2DData m_Renderer2DData;
        static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");
        static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly ShaderTagId k_DepthRenderingPassName = new ShaderTagId("DepthRendering");
        static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");
        static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

        public Render2DLightingPass(Renderer2DData rendererData)
        {
            if (s_SortingLayers == null)
                s_SortingLayers = SortingLayer.layers;

            m_Renderer2DData = rendererData;
        }

        public void GetTransparencySortingMode(Camera camera, ref SortingSettings sortingSettings)
        {
            TransparencySortMode mode = camera.transparencySortMode;

            if (mode == TransparencySortMode.Default)
            {
                mode = m_Renderer2DData.transparencySortMode;
                if (mode == TransparencySortMode.Default)
                    mode = camera.orthographic ? TransparencySortMode.Orthographic : TransparencySortMode.Perspective;
            }

            if (mode == TransparencySortMode.Perspective)
            {
                sortingSettings.distanceMetric = DistanceMetric.Perspective;
            }
            else if (mode == TransparencySortMode.Orthographic)
            {
                sortingSettings.distanceMetric = DistanceMetric.Orthographic;
            }
            else
            {
                sortingSettings.distanceMetric = DistanceMetric.CustomAxis;
                sortingSettings.customAxis = m_Renderer2DData.transparencySortAxis;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            bool isLitView = true;

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
                isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                isLitView = false;

            if (!Application.isPlaying)
                s_SortingLayers = SortingLayer.layers;
#endif
            Camera camera = renderingData.cameraData.camera;

            FilteringSettings filterSettings = new FilteringSettings();
            filterSettings.renderQueueRange = RenderQueueRange.all;
            filterSettings.layerMask = -1;
            filterSettings.renderingLayerMask = 0xFFFFFFFF;
            filterSettings.sortingLayerRange = SortingLayerRange.all;


            //RendererLighting.Setup(renderingData, m_Renderer2DData);

            CommandBuffer cmd = CommandBufferPool.Get("Render 2D Lighting");
            cmd.Clear();

            //RendererLighting.CreateNormalMapRenderTexture(cmd);

            cmd.SetGlobalFloat("_HDREmulationScale", m_Renderer2DData.hdrEmulationScale);
            cmd.SetGlobalFloat("_InverseHDREmulationScale", 1.0f / m_Renderer2DData.hdrEmulationScale);
            cmd.SetGlobalColor("_RendererColor", Color.white);
            cmd.SetGlobalFloat("_CustomDepth", 1.0f);

            cmd.SetGlobalFloat("_UseSceneLighting", 0.0f);

            context.ExecuteCommandBuffer(cmd);

            DrawingSettings combinedDrawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            DrawingSettings normalsDrawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);
            DrawingSettings depthDrawSettings = CreateDrawingSettings(k_DepthRenderingPassName, ref renderingData, SortingCriteria.CommonOpaque);

            // Depth pre-pass.
            cmd.Clear();

            CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None);
            context.ExecuteCommandBuffer(cmd);
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(renderingData.cullResults, ref depthDrawSettings, ref filterSettings);


            // Color Pass
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            filterSettings.sortingLayerRange = SortingLayerRange.all;

            SortingSettings sortSettings = combinedDrawSettings.sortingSettings;
            GetTransparencySortingMode(camera, ref sortSettings);

            context.DrawRenderers(renderingData.cullResults, ref combinedDrawSettings, ref filterSettings);

            filterSettings.sortingLayerRange = SortingLayerRange.all;
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None);
        }
    }
}
