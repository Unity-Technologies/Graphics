using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Render all objects that have a 'DepthOnly' pass into the given depth buffer.
    ///
    /// You can use this pass to prime a depth buffer for subsequent rendering.
    /// Use it as a z-prepass, or use it to generate a depth buffer.
    /// </summary>
    public class DepthOnlyPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthOnly");

        private RTHandle destination { get; set; }
        private GraphicsFormat depthStencilFormat;
        internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private PassData m_PassData;
        FilteringSettings m_FilteringSettings;

        /// <summary>
        /// Creates a new <c>DepthOnlyPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        public DepthOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthOnlyPass));
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            useNativeRenderPass = false;
            this.shaderTagId = k_ShaderTagId;
        }

        /// <summary>
        /// Configures the pass.
        /// </summary>
        /// <param name="baseDescriptor">The <c>RenderTextureDescriptor</c> used for the depthStencilFormat.</param>
        /// <param name="depthAttachmentHandle">The <c>RTHandle</c> used to render to.</param>
        /// <seealso cref="RenderTextureDescriptor"/>
        /// <seealso cref="RTHandle"/>
        /// <seealso cref="GraphicsFormat"/>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RTHandle depthAttachmentHandle)
        {
            this.destination = depthAttachmentHandle;
            this.depthStencilFormat = baseDescriptor.depthStencilFormat;
        }

        /// <inheritdoc />
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;

            // When depth priming is in use the camera target should not be overridden so the Camera's MSAA depth attachment is used.
            if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
            {
                ConfigureTarget(renderingData.cameraData.renderer.cameraDepthTargetHandle);
                // Only clear depth here so we don't clear any bound color target. It might be unused by this pass but that doesn't mean we can just clear it. (e.g. in case of overlay cameras + depth priming)
                ConfigureClear(ClearFlag.Depth, Color.black);
            }
            // When not using depth priming the camera target should be set to our non MSAA depth target.
            else
            {
                useNativeRenderPass = true;
                ConfigureTarget(destination);
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, ref RenderingData renderingData)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthPrepass)))
            {
                cmd.DrawRendererList(rendererList);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.renderingData = renderingData;
            var param = InitRendererListParams(ref renderingData);
            RendererList rendererList = context.CreateRendererList(ref param);

            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, rendererList, ref renderingData);
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal RendererListHandle rendererList;
        }

        private RendererListParams InitRendererListParams(ref RenderingData renderingData)
        {
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagId, ref renderingData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;
            return new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        }

        internal void Render(RenderGraph renderGraph, ref TextureHandle cameraDepthTexture, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("DepthOnly Prepass", out var passData, base.profilingSampler))
            {
                passData.renderingData = renderingData;
                var param = InitRendererListParams(ref renderingData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.UseTextureFragmentDepth(cameraDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Write);

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererList, ref data.renderingData);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph,"_CameraDepthTexture", cameraDepthTexture, "Set Global CameraDepthTexture");
        }
    }
}
