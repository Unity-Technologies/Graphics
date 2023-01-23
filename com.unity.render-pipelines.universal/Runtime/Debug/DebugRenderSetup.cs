using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    class DebugRenderSetup : IDisposable
    {
        private readonly DebugHandler m_DebugHandler;
        private readonly int m_Index;
        readonly FilteringSettings m_FilteringSettings;
        private RendererList m_RendererList;
        private RendererListHandle m_RendererListHandle;
        private DebugDisplaySettingsMaterial MaterialSettings => m_DebugHandler.DebugDisplaySettings.materialSettings;
        private DebugDisplaySettingsRendering RenderingSettings => m_DebugHandler.DebugDisplaySettings.renderingSettings;
        private DebugDisplaySettingsLighting LightingSettings => m_DebugHandler.DebugDisplaySettings.lightingSettings;

        internal void Begin(RasterCommandBuffer cmd)
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.sceneOverrideMode;

            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Wireframe:
                {
                    // RasterCmd TODO: Manage wireframe mode using RasterCommandBuffer
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        // RasterCmd TODO: Manage wireframe mode using RasterCommandBuffer
                    }
                    break;
                }
            }

            m_DebugHandler.SetupShaderProperties(cmd, m_Index);
        }

        internal void End(RasterCommandBuffer cmd)
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.sceneOverrideMode;

            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Wireframe:
                {
                    // RasterCmd TODO: Manage wireframe mode using RasterCommandBuffer
                    //m_Context.Submit();
                    //GL.wireframe = false;
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        // RasterCmd TODO: Manage wireframe mode using RasterCommandBuffer
                        //m_Context.Submit();
                        //GL.wireframe = false;
                    }
                    break;
                }
            }
        }

        internal DebugRenderSetup(DebugHandler debugHandler,
            int index,
            FilteringSettings filteringSettings)
        {
            m_DebugHandler = debugHandler;
            m_Index = index;
            m_FilteringSettings = filteringSettings;
        }

        internal void CreateRendererList(
            ScriptableRenderContext context,
            ref RenderingData renderingData,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock)
        {
            RenderingUtils.CreateRendererListWithRenderStateBlock(context, renderingData, drawingSettings, filteringSettings, renderStateBlock, ref m_RendererList);
        }

        internal void CreateRendererList(
            RenderGraph renderGraph,
            ref RenderingData renderingData,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock)
        {
            RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, renderingData, drawingSettings, filteringSettings, renderStateBlock, ref m_RendererListHandle);
        }

        internal void DrawWithRendererList(RasterCommandBuffer cmd)
        {
            if(m_RendererList.isValid)
                cmd.DrawRendererList(m_RendererList);
            else if (m_RendererListHandle.IsValid())
                cmd.DrawRendererList(m_RendererListHandle);
        }

        internal DrawingSettings CreateDrawingSettings(DrawingSettings drawingSettings)
        {
            bool usesReplacementMaterial = (MaterialSettings.vertexAttributeDebugMode != DebugVertexAttributeMode.None);

            if (usesReplacementMaterial)
            {
                Material replacementMaterial = m_DebugHandler.ReplacementMaterial;
                DrawingSettings modifiedDrawingSettings = drawingSettings;

                modifiedDrawingSettings.overrideMaterial = replacementMaterial;
                modifiedDrawingSettings.overrideMaterialPassIndex = 0;
                return modifiedDrawingSettings;
            }

            // No overrides, return original
            return drawingSettings;
        }

        internal RenderStateBlock GetRenderStateBlock(RenderStateBlock renderStateBlock)
        {
            DebugSceneOverrideMode sceneOverrideMode = RenderingSettings.sceneOverrideMode;

            // Potentially override parts of the RenderStateBlock
            switch (sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Overdraw:
                {
                    var isOpaque = m_FilteringSettings.renderQueueRange == RenderQueueRange.opaque || m_FilteringSettings.renderQueueRange == RenderQueueRange.all;
                    var isTransparent = m_FilteringSettings.renderQueueRange == RenderQueueRange.transparent || m_FilteringSettings.renderQueueRange == RenderQueueRange.all;
                    var overdrawOpaque =
                        m_DebugHandler.DebugDisplaySettings.renderingSettings.overdrawMode == DebugOverdrawMode.Opaque
                        || m_DebugHandler.DebugDisplaySettings.renderingSettings.overdrawMode == DebugOverdrawMode.All;
                    var overdrawTransparent =
                        m_DebugHandler.DebugDisplaySettings.renderingSettings.overdrawMode == DebugOverdrawMode.Transparent
                        || m_DebugHandler.DebugDisplaySettings.renderingSettings.overdrawMode == DebugOverdrawMode.All;

                    var blendOverdraw = isOpaque && overdrawOpaque || isTransparent && overdrawTransparent;
                    var destination = blendOverdraw ? BlendMode.One : BlendMode.Zero;

                    RenderTargetBlendState additiveBlend = new RenderTargetBlendState(sourceColorBlendMode: BlendMode.One, destinationColorBlendMode: destination);

                    // Additive-blend but leave z-write and culling as they are when we draw normally
                    renderStateBlock.blendState = new BlendState { blendState0 = additiveBlend };
                    renderStateBlock.mask = RenderStateMask.Blend;
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        // Ensure we render the wireframe in front of the solid triangles of the previous pass...
                        renderStateBlock.rasterState = new RasterState(offsetUnits: -1, offsetFactor: -1);
                        renderStateBlock.mask = RenderStateMask.Raster;
                    }
                    break;
                }
            }

            return renderStateBlock;
        }

        public void Dispose()
        {
        }
    }
}
