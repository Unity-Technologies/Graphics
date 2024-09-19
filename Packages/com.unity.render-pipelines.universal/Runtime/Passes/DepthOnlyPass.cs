using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
        private RTHandle destination { get; set; }
        private GraphicsFormat depthStencilFormat;
        internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private PassData m_PassData;
        FilteringSettings m_FilteringSettings;

        // Statics
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthOnly");
        private static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");

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
            profilingSampler = new ProfilingSampler("Draw Depth Only");
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
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618

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

            #pragma warning restore CS0618
        }

        private static void ExecutePass(RasterCommandBuffer cmd, RendererList rendererList)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthPrepass)))
            {
                cmd.DrawRendererList(rendererList);
            }
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var param = InitRendererListParams(universalRenderingData, cameraData, lightData);
            RendererList rendererList = context.CreateRendererList(ref param);

            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), rendererList);
        }

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagId, renderingData, cameraData, lightData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;
            return new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, ref TextureHandle cameraDepthTexture, uint batchLayerMask, bool setGlobalDepth)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                var param = InitRendererListParams(renderingData, cameraData, lightData);
                param.filteringSettings.batchLayerMask = batchLayerMask;
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderAttachmentDepth(cameraDepthTexture, AccessFlags.Write);

                if (setGlobalDepth)
                    builder.SetGlobalTextureAfterPass(cameraDepthTexture, s_CameraDepthTextureID);

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.rendererList);
                });
            }
        }
    }
}
