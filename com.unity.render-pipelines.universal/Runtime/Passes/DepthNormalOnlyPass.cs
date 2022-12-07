using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Render all objects that have a 'DepthNormals' and/or 'DepthNormalsOnly' pass into the given depth and normal buffers.
    /// </summary>
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        internal List<ShaderTagId> shaderTagIds { get; set; }

        private RTHandle depthHandle { get; set; }
        private RTHandle normalHandle { get; set; }
        private RTHandle renderingLayersHandle { get; set; }
        internal bool enableRenderingLayers { get; set; } = false;
        internal RenderingLayerUtils.MaskSize renderingLayersMaskSize { get; set; }
        private FilteringSettings m_FilteringSettings;
        private PassData m_PassData;
        // Constants
        private static readonly List<ShaderTagId> k_DepthNormals = new List<ShaderTagId> { new ShaderTagId("DepthNormals"), new ShaderTagId("DepthNormalsOnly") };
        private static readonly RTHandle[] k_ColorAttachment1 = new RTHandle[1];
        private static readonly RTHandle[] k_ColorAttachment2 = new RTHandle[2];

        /// <summary>
        /// Creates a new <c>DepthNormalOnlyPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthNormalOnlyPass));
            m_PassData = new PassData();
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            useNativeRenderPass = false;
            this.shaderTagIds = k_DepthNormals;
        }

        /// <summary>
        /// Finds the format to use for the normals texture.
        /// </summary>
        /// <returns>The GraphicsFormat to use with the Normals texture.</returns>
        public static GraphicsFormat GetGraphicsFormat()
        {
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8G8B8A8_SNorm, FormatUsage.Render))
                return GraphicsFormat.R8G8B8A8_SNorm; // Preferred format
            else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render))
                return GraphicsFormat.R16G16B16A16_SFloat; // fallback
            else
                return GraphicsFormat.R32G32B32A32_SFloat; // fallback
        }

        /// <summary>
        /// Configures the pass.
        /// </summary>
        /// <param name="depthHandle">The <c>RTHandle</c> used to render depth to.</param>
        /// <param name="normalHandle">The <c>RTHandle</c> used to render normals.</param>
        /// <seealso cref="RTHandle"/>
        public void Setup(RTHandle depthHandle, RTHandle normalHandle)
        {
            this.depthHandle = depthHandle;
            this.normalHandle = normalHandle;
            enableRenderingLayers = false;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="depthHandle">The <c>RTHandle</c> used to render depth to.</param>
        /// <param name="normalHandle">The <c>RTHandle</c> used to render normals.</param>
        /// <param name="decalLayerHandle">The <c>RTHandle</c> used to render decals.</param>
        public void Setup(RTHandle depthHandle, RTHandle normalHandle, RTHandle decalLayerHandle)
        {
            Setup(depthHandle, normalHandle);
            renderingLayersHandle = decalLayerHandle;
            enableRenderingLayers = true;
        }


        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RTHandle[] colorHandles;
            if (enableRenderingLayers)
            {
                k_ColorAttachment2[0] = normalHandle;
                k_ColorAttachment2[1] = renderingLayersHandle;
                colorHandles = k_ColorAttachment2;
            }
            else
            {
                k_ColorAttachment1[0] = normalHandle;
                colorHandles = k_ColorAttachment1;
            }

            if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
                ConfigureTarget(colorHandles, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            else
                ConfigureTarget(colorHandles, depthHandle);

            ConfigureClear(ClearFlag.All, Color.black);
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            var shaderTagIds = passData.shaderTagIds;
            var filteringSettings = passData.filteringSettings;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthNormalPrepass)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.WriteRenderingLayers, passData.enableRenderingLayers);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagIds, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);
                var rl = context.CreateRendererList(ref param);
                cmd.DrawRendererList(rl);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.shaderTagIds = this.shaderTagIds;
            m_PassData.filteringSettings = m_FilteringSettings;
            m_PassData.enableRenderingLayers = enableRenderingLayers;
            ExecutePass(context, m_PassData, ref renderingData);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            normalHandle = null;
            depthHandle = null;
            renderingLayersHandle = null;
        }

        private class PassData
        {
            internal TextureHandle cameraDepthTexture;
            internal TextureHandle cameraNormalsTexture;
            internal RenderingData renderingData;
            internal List<ShaderTagId> shaderTagIds;
            internal FilteringSettings filteringSettings;
            internal bool enableRenderingLayers;
            internal RenderingLayerUtils.MaskSize maskSize;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle cameraNormalsTexture, TextureHandle cameraDepthTexture, TextureHandle renderingLayersTexture, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("DepthNormals Prepass", out var passData, base.profilingSampler))
            {
                passData.cameraNormalsTexture = builder.UseColorBuffer(cameraNormalsTexture, 0);
                passData.cameraDepthTexture = builder.UseDepthBuffer(cameraDepthTexture, DepthAccess.Write);
                passData.renderingData = renderingData;
                passData.shaderTagIds = shaderTagIds;
                passData.filteringSettings = m_FilteringSettings;
                passData.enableRenderingLayers = enableRenderingLayers;
                if (enableRenderingLayers)
                {
                    builder.UseColorBuffer(renderingLayersTexture, 1);
                    passData.maskSize = renderingLayersMaskSize;
                }

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);
                    ExecutePass(context.renderContext, data, ref data.renderingData);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph, "_CameraNormalsTexture", cameraNormalsTexture, "Set Camera Normals Texture");
            RenderGraphUtils.SetGlobalTexture(renderGraph,"_CameraDepthTexture", cameraDepthTexture, "Set Global CameraDepthTexture");
        }
    }
}
