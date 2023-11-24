using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draw  objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names UniversalForward or SRPDefaultUnlit.
    /// </summary>
    public class DrawObjectsPass : ScriptableRenderPass
    {
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        string m_ProfilerTag;

        /// <summary>
        /// Profiling sampler
        /// </summary>
        protected ProfilingSampler m_ProfilingSampler;

        bool m_IsOpaque;

        /// <summary>
        /// Used to indicate whether transparent objects should receive shadows or not.
        /// </summary>
        public bool m_ShouldTransparentsReceiveShadows;

        PassData m_PassData;

        static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");

        /// <summary>
        /// Creates a new <c>DrawObjectsPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="shaderTagIds"></param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        /// <seealso cref="ShaderTagId"/>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        /// <seealso cref="StencilState"/>
        public DrawObjectsPass(string profilerTag, ShaderTagId[] shaderTagIds, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawObjectsPass));
            m_PassData = new PassData();
            m_ProfilerTag = profilerTag;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            foreach (ShaderTagId sid in shaderTagIds)
                m_ShaderTagIdList.Add(sid);
            renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_IsOpaque = opaque;
            m_ShouldTransparentsReceiveShadows = false;

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        /// <summary>
        /// Creates a new <c>DrawObjectsPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        /// <seealso cref="StencilState"/>
        public DrawObjectsPass(string profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profilerTag,
            new ShaderTagId[] { new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly") },
            opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        { }

        internal DrawObjectsPass(URPProfileId profileId, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profileId.GetType().Name, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
            m_ProfilingSampler = ProfilingSampler.Get(profileId);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            InitPassData(cameraData, ref m_PassData);
            InitRendererLists(universalRenderingData, cameraData, lightData, ref m_PassData, context, default(RenderGraph), false);

            using (new ProfilingScope(renderingData.commandBuffer, m_ProfilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, m_PassData.rendererList, m_PassData.objectsWithErrorRendererList, m_PassData.cameraData.IsCameraProjectionMatrixFlipped());
            }
        }

        internal static void ExecutePass(RasterCommandBuffer cmd, PassData data, RendererList rendererList, RendererList objectsWithErrorRendererList, bool yFlip)
        {
            // Global render pass data containing various settings.
            // x,y,z are currently unused
            // w is used for knowing whether the object is opaque(1) or alpha blended(0)
            Vector4 drawObjectPassData = new Vector4(0.0f, 0.0f, 0.0f, (data.isOpaque) ? 1.0f : 0.0f);
            cmd.SetGlobalVector(s_DrawObjectPassDataPropID, drawObjectPassData);

            // scaleBias.x = flipSign
            // scaleBias.y = scale
            // scaleBias.z = bias
            // scaleBias.w = unused
            float flipSign = yFlip ? -1.0f : 1.0f;
            Vector4 scaleBias = (flipSign < 0.0f)
                ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBias);

            // Set a value that can be used by shaders to identify when AlphaToMask functionality may be active
            // The material shader alpha clipping logic requires this value in order to function correctly in all cases.
            float alphaToMaskAvailable = ((data.cameraData.cameraTargetDescriptor.msaaSamples > 1) && data.isOpaque) ? 1.0f : 0.0f;
            cmd.SetGlobalFloat(ShaderPropertyId.alphaToMaskAvailable, alphaToMaskAvailable);

            var activeDebugHandler = GetActiveDebugHandler(data.cameraData);
            if (activeDebugHandler != null)
            {
                data.debugRendererLists.DrawWithRendererList(cmd);
            }
            else
            {
                cmd.DrawRendererList(rendererList);
                // Render objects that did not match any shader pass with error shader
                RenderingUtils.DrawRendererListObjectsWithError(cmd, ref objectsWithErrorRendererList);
            }
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        internal class PassData
        {
            internal TextureHandle albedoHdl;
            internal TextureHandle depthHdl;

            internal UniversalCameraData cameraData;
            internal bool isOpaque;
            internal bool shouldTransparentsReceiveShadows;
            internal RendererListHandle rendererListHdl;
            internal RendererListHandle objectsWithErrorRendererListHdl;
            internal DebugRendererLists debugRendererLists;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
            internal RendererList objectsWithErrorRendererList;
        }

        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        internal void InitPassData(UniversalCameraData cameraData, ref PassData passData)
        {
            passData.cameraData = cameraData;
            passData.isOpaque = m_IsOpaque;
            passData.shouldTransparentsReceiveShadows = m_ShouldTransparentsReceiveShadows;
        }

        internal void InitRendererLists(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            ref Camera camera = ref cameraData.camera;
            var sortFlags = (m_IsOpaque) ? cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
            if (cameraData.renderer.useDepthPriming && m_IsOpaque && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
                sortFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;

            var filterSettings = m_FilteringSettings;
#if UNITY_EDITOR
                // When rendering the preview camera, we want the layer mask to be forced to Everything
                if (cameraData.isPreviewCamera)
                {
                    filterSettings.layerMask = -1;
                }
#endif
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortFlags);
            if (cameraData.renderer.useDepthPriming && m_IsOpaque && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
            {
                m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Equal);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
            else if (m_RenderStateBlock.depthState.compareFunction == CompareFunction.Equal)
            {
                m_RenderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }

            var activeDebugHandler = GetActiveDebugHandler(cameraData);
            if (useRenderGraph)
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(renderGraph, ref renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawSettings, filterSettings, m_RenderStateBlock, ref passData.rendererListHdl);
                    RenderingUtils.CreateRendererListObjectsWithError(renderGraph, ref renderingData.cullResults, camera, filterSettings, sortFlags, ref passData.objectsWithErrorRendererListHdl);
                }
            }
            else
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context, ref renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(context, ref renderingData.cullResults, drawSettings, filterSettings, m_RenderStateBlock, ref passData.rendererList);
                    RenderingUtils.CreateRendererListObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, sortFlags, ref passData.objectsWithErrorRendererList);
                }
            }
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle colorTarget, TextureHandle depthTarget, TextureHandle mainShadowsTexture, TextureHandle additionalShadowsTexture)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Objects Pass", out var passData,
                m_ProfilingSampler))
            {
                InitPassData(cameraData, ref passData);

                if (colorTarget.IsValid())
                {
                    passData.albedoHdl = colorTarget;
                    builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                }

                if (depthTarget.IsValid())
                {
                    passData.depthHdl = depthTarget;
                    builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                }

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);
                RenderGraphUtils.UseDBufferIfValid(builder, resourceData);

                InitRendererLists(renderingData, cameraData, lightData, ref passData, default(ScriptableRenderContext), renderGraph, true);
                var activeDebugHandler = GetActiveDebugHandler(cameraData);
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.rendererListHdl);
                    builder.UseRendererList(passData.objectsWithErrorRendererListHdl);
                }

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Currently we only need to call this additional pass when the user
                    // doesn't want transparent objects to receive shadows
                    if (!data.isOpaque && !data.shouldTransparentsReceiveShadows)
                        TransparentSettingsPass.ExecutePass(context.cmd, data.shouldTransparentsReceiveShadows);

                    bool yFlip = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.albedoHdl, data.depthHdl);

                    ExecutePass(context.cmd, data, data.rendererListHdl, data.objectsWithErrorRendererListHdl, yFlip);
                });
            }
        }
    }

    /// <summary>
    /// Extension of DrawObjectPass that also output Rendering Layers Texture as second render target.
    /// </summary>
    internal class DrawObjectsWithRenderingLayersPass : DrawObjectsPass
    {
        RTHandle[] m_ColorTargetIndentifiers;
        RTHandle m_DepthTargetIndentifiers;

        /// <summary>
        /// Creates a new <c>DrawObjectsWithRenderingLayersPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        public DrawObjectsWithRenderingLayersPass(URPProfileId profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference) :
            base(profilerTag, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
            m_ColorTargetIndentifiers = new RTHandle[2];
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="colorAttachment">Color attachment handle.</param>
        /// <param name="renderingLayersTexture">Texture used with rendering layers.</param>
        /// <param name="depthAttachment">Depth attachment handle.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Setup(RTHandle colorAttachment, RTHandle renderingLayersTexture, RTHandle depthAttachment)
        {
            if (colorAttachment == null)
                throw new ArgumentException("Color attachment can not be null", "colorAttachment");
            if (renderingLayersTexture == null)
                throw new ArgumentException("Rendering layers attachment can not be null", "renderingLayersTexture");
            if (depthAttachment == null)
                throw new ArgumentException("Depth attachment can not be null", "depthAttachment");

            m_ColorTargetIndentifiers[0] = colorAttachment;
            m_ColorTargetIndentifiers[1] = renderingLayersTexture;
            m_DepthTargetIndentifiers = depthAttachment;
        }

        /// <inheritdoc/>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_ColorTargetIndentifiers, m_DepthTargetIndentifiers);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;

            // Enable Rendering Layers
            cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

            // Execute
            base.Execute(context, ref renderingData);

            // Clean up
            cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);
        }

        private class RenderingLayersPassData
        {
            internal PassData basePassData;
            internal RenderingLayerUtils.MaskSize maskSize;

            public RenderingLayersPassData()
            {
                basePassData = new PassData();
            }
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle colorTarget, TextureHandle renderingLayersTexture, TextureHandle depthTarget, TextureHandle mainShadowsTexture, TextureHandle additionalShadowsTexture, RenderingLayerUtils.MaskSize maskSize)
        {
            using (var builder = renderGraph.AddRasterRenderPass<RenderingLayersPassData>("Draw Objects With Rendering Layers Pass", out var passData,
                m_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                InitPassData(cameraData, ref passData.basePassData);
                passData.maskSize = maskSize;

                passData.basePassData.albedoHdl = colorTarget;
                builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderingLayersTexture, 1, AccessFlags.Write);
                passData.basePassData.depthHdl = depthTarget;
                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;
                if (renderer != null)
                {
                    TextureHandle ssaoTexture = resourceData.ssaoTexture;
                    if (ssaoTexture.IsValid())
                        builder.UseTexture(ssaoTexture, AccessFlags.Read);

                    RenderGraphUtils.UseDBufferIfValid(builder, resourceData);
                }

                InitRendererLists(renderingData, cameraData, lightData, ref passData.basePassData, default(ScriptableRenderContext), renderGraph, true);
                var activeDebugHandler = GetActiveDebugHandler(cameraData);
                if (activeDebugHandler != null)
                {
                    passData.basePassData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.basePassData.rendererListHdl);
                    builder.UseRendererList(passData.basePassData.objectsWithErrorRendererListHdl);
                }

                builder.AllowPassCulling(false);
                // Required here because of RenderingLayerUtils.SetupProperties
                builder.AllowGlobalStateModification(true);
                builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering);

                builder.SetRenderFunc((RenderingLayersPassData data, RasterGraphContext context) =>
                {
                    // Enable Rendering Layers
                    context.cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

                    RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);

                    // Currently we only need to call this additional pass when the user
                    // doesn't want transparent objects to receive shadows
                    if (!data.basePassData.isOpaque && !data.basePassData.shouldTransparentsReceiveShadows)
                        TransparentSettingsPass.ExecutePass(context.cmd, data.basePassData.shouldTransparentsReceiveShadows);

                    bool yFlip = data.basePassData.cameraData.IsRenderTargetProjectionMatrixFlipped(data.basePassData.albedoHdl, data.basePassData.depthHdl);

                    // Execute
                    ExecutePass(context.cmd, data.basePassData, data.basePassData.rendererListHdl, data.basePassData.objectsWithErrorRendererListHdl, yFlip);

                    // Clean up
                    context.cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);
                });
            }
        }
    }
}
