using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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

        /// <inheritdoc />
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_PassData.m_IsOpaque = m_IsOpaque;
            m_PassData.m_RenderStateBlock = m_RenderStateBlock;
            m_PassData.m_FilteringSettings = m_FilteringSettings;
            m_PassData.m_ShaderTagIdList = m_ShaderTagIdList;
            m_PassData.m_ProfilingSampler = m_ProfilingSampler;

            CameraSetup(cmd, m_PassData, ref renderingData);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);

            ExecutePass(context, m_PassData, ref renderingData, renderingData.cameraData.IsCameraProjectionMatrixFlipped());
        }

        private static void CameraSetup(CommandBuffer cmd, PassData data, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderer.useDepthPriming && data.m_IsOpaque && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
            {
                data.m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Equal);
                data.m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
            else if (data.m_RenderStateBlock.depthState.compareFunction == CompareFunction.Equal)
            {
                data.m_RenderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                data.m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData data, ref RenderingData renderingData, bool yFlip)
        {
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, data.m_ProfilingSampler))
            {
                // Global render pass data containing various settings.
                // x,y,z are currently unused
                // w is used for knowing whether the object is opaque(1) or alpha blended(0)
                Vector4 drawObjectPassData = new Vector4(0.0f, 0.0f, 0.0f, (data.m_IsOpaque) ? 1.0f : 0.0f);
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
                float alphaToMaskAvailable = ((renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1) && data.m_IsOpaque) ? 1.0f : 0.0f;
                cmd.SetGlobalFloat(ShaderPropertyId.alphaToMaskAvailable, alphaToMaskAvailable);

                // TODO RENDERGRAPH: once RenderLists land we should be able to remove these 2 lines
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var sortFlags = (data.m_IsOpaque) ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
                if (renderingData.cameraData.renderer.useDepthPriming && data.m_IsOpaque && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
                    sortFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;

                var filterSettings = data.m_FilteringSettings;

#if UNITY_EDITOR
                // When rendering the preview camera, we want the layer mask to be forced to Everything
                if (renderingData.cameraData.isPreviewCamera)
                {
                    filterSettings.layerMask = -1;
                }
#endif

                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(data.m_ShaderTagIdList, ref renderingData, sortFlags);

                var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
                if (activeDebugHandler != null)
                {
                    activeDebugHandler.DrawWithDebugRenderState(context, cmd, ref renderingData, ref drawSettings, ref filterSettings, ref data.m_RenderStateBlock,
                        (ScriptableRenderContext ctx, CommandBuffer cmd, ref RenderingData data, ref DrawingSettings ds, ref FilteringSettings fs, ref RenderStateBlock rsb) =>
                        {
                            RenderingUtils.DrawRendererListWithRenderStateBlock(ctx, cmd, data, ds, fs, rsb);
                        });
                }
                else
                {
                    RenderingUtils.DrawRendererListWithRenderStateBlock(context, cmd, renderingData, drawSettings, filterSettings, data.m_RenderStateBlock);

                    // Render objects that did not match any shader pass with error shader
                    RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, SortingCriteria.None, cmd);
                }


            }
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        protected class PassData
        {
            internal TextureHandle m_Albedo;
            internal TextureHandle m_Depth;

            internal RenderingData m_RenderingData;

            internal bool m_IsOpaque;
            internal RenderStateBlock m_RenderStateBlock;
            internal FilteringSettings m_FilteringSettings;
            internal List<ShaderTagId> m_ShaderTagIdList;
            internal ProfilingSampler m_ProfilingSampler;

            internal bool m_ShouldTransparentsReceiveShadows;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle colorTarget, TextureHandle depthTarget, TextureHandle mainShadowsTexture, TextureHandle additionalShadowsTexture, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Objects Pass", out var passData,
                m_ProfilingSampler))
            {
                passData.m_Albedo = builder.UseColorBuffer(colorTarget, 0);
                passData.m_Depth = builder.UseDepthBuffer(depthTarget, DepthAccess.Write);

                if (mainShadowsTexture.IsValid())
                    builder.ReadTexture(mainShadowsTexture);
                if (additionalShadowsTexture.IsValid())
                    builder.ReadTexture(additionalShadowsTexture);

                passData.m_RenderingData = renderingData;

                builder.AllowPassCulling(false);

                InitPassData(ref passData);

                passData.m_ShouldTransparentsReceiveShadows = m_ShouldTransparentsReceiveShadows;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    RenderGraphRenderFunc(data, context, false);
                });

            }
        }

        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        protected void InitPassData(ref PassData passData)
        {
            passData.m_IsOpaque = m_IsOpaque;
            passData.m_RenderStateBlock = m_RenderStateBlock;
            passData.m_FilteringSettings = m_FilteringSettings;
            passData.m_ShaderTagIdList = m_ShaderTagIdList;
            passData.m_ProfilingSampler = m_ProfilingSampler;
        }

        /// <summary>
        /// Shared DrawObject RenderGraph render function
        /// </summary>
        /// <param name="data"></param>
        /// <param name="context"></param>
        /// <param name="isRenderingLayersPass"></param>
        protected static void RenderGraphRenderFunc(PassData data, RenderGraphContext context, bool isRenderingLayersPass)
        {
            ref var renderingData = ref data.m_RenderingData;

            // TODO RENDERGRAPH figure out where to put XR proj flip logic so that it can be auto handled in render graph
#if ENABLE_VR && ENABLE_XR_MODULE
            if (renderingData.cameraData.xr.enabled)
            {
                // SetRenderTarget might alter the internal device state(winding order).
                // Non-stereo buffer is already updated internally when switching render target. We update stereo buffers here to keep the consistency.
                bool renderIntoTexture = data.m_Albedo != renderingData.cameraData.xr.renderTarget;
                renderingData.cameraData.PushBuiltinShaderConstantsXR(renderingData.commandBuffer, renderIntoTexture);
                XRSystemUniversal.MarkShaderProperties(renderingData.commandBuffer, renderingData.cameraData.xrUniversal, renderIntoTexture);
            }
#endif

            // Currently we only need to call this additional pass when the user
            // doesn't want transparent objects to receive shadows
            if (!data.m_IsOpaque && !data.m_ShouldTransparentsReceiveShadows)
                TransparentSettingsPass.ExecutePass(context.cmd, data.m_ShouldTransparentsReceiveShadows);

            bool yFlip = renderingData.cameraData.IsRenderTargetProjectionMatrixFlipped(data.m_Albedo, data.m_Depth);
            CameraSetup(context.cmd, data, ref renderingData);

            if (isRenderingLayersPass)
                CoreUtils.SetKeyword(context.cmd, ShaderKeywordStrings.WriteRenderingLayers, true);

            ExecutePass(context.renderContext, data, ref renderingData, yFlip);
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

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.WriteRenderingLayers, true);

            base.Execute(context, ref renderingData);
        }

        private class RenderingLayersPassData
        {
            internal PassData basePassData;
            internal RenderingLayerUtils.MaskSize maskSize;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle colorTarget, TextureHandle renderingLayersTexture, TextureHandle depthTarget, TextureHandle mainShadowsTexture, TextureHandle additionalShadowsTexture, RenderingLayerUtils.MaskSize maskSize, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<RenderingLayersPassData>("Draw Objects With Rendering Layers Pass", out var passData,
                m_ProfilingSampler))
            {
                passData.basePassData.m_Albedo = builder.UseColorBuffer(colorTarget, 0);
                builder.UseColorBuffer(renderingLayersTexture, 1);
                passData.basePassData.m_Depth = builder.UseDepthBuffer(depthTarget, DepthAccess.Write);

                if (mainShadowsTexture.IsValid())
                    builder.ReadTexture(mainShadowsTexture);
                if (additionalShadowsTexture.IsValid())
                    builder.ReadTexture(additionalShadowsTexture);

                passData.basePassData.m_RenderingData = renderingData;

                builder.AllowPassCulling(false);

                InitPassData(ref passData.basePassData);

                passData.basePassData.m_ShouldTransparentsReceiveShadows = m_ShouldTransparentsReceiveShadows;
                passData.maskSize = maskSize;

                builder.SetRenderFunc((RenderingLayersPassData data, RenderGraphContext context) =>
                {
                    CoreUtils.SetKeyword(context.cmd, ShaderKeywordStrings.WriteRenderingLayers, true);

                    RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);

                    RenderGraphRenderFunc(data.basePassData, context, false);
                });

            }
        }
    }
}
