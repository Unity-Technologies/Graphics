using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    class DebugRenderSetup : IDisposable
    {
        private readonly DebugHandler m_DebugHandler;
        private readonly FilteringSettings m_FilteringSettings;
        private readonly int m_Index;
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
                    cmd.SetWireframe(true);
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        cmd.SetWireframe(true);
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
                    cmd.SetWireframe(false);
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (m_Index == 1)
                    {
                        cmd.SetWireframe(false);
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
            m_FilteringSettings = filteringSettings;
            m_Index = index;
        }

        internal void CreateRendererList(
            ScriptableRenderContext context,
            ref CullingResults cullResults,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock,
            ref RendererList rendererList)
        {
            RenderingUtils.CreateRendererListWithRenderStateBlock(context, ref cullResults, drawingSettings, filteringSettings, renderStateBlock, ref rendererList);
        }

        internal void CreateRendererList(
            RenderGraph renderGraph,
            ref CullingResults cullResults,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock,
            ref RendererListHandle rendererListHdl)
        {
            RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref cullResults, drawingSettings, filteringSettings, renderStateBlock, ref rendererListHdl);
        }

        internal void DrawWithRendererList(RasterCommandBuffer cmd, ref RendererList rendererList)
        {
            if(rendererList.isValid)
                cmd.DrawRendererList(rendererList);
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

                case DebugSceneOverrideMode.Wireframe:
                {
                    // Disable culling to see all lines
                    renderStateBlock.rasterState = new RasterState(
                        cullingMode: CullMode.Off        
                    );

                    renderStateBlock.mask = RenderStateMask.Raster;
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

        internal int GetIndex() { return m_Index; }

        public void Dispose()
        {
        }
    }
}
