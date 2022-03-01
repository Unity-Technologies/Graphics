using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        internal List<ShaderTagId> shaderTagIds { get; set; }

        private RTHandle depthHandle { get; set; }
        private RTHandle normalHandle { get; set; }
        private FilteringSettings m_FilteringSettings;

        // Constants
        private static readonly List<ShaderTagId> k_DepthNormals = new List<ShaderTagId> { new ShaderTagId("DepthNormals"), new ShaderTagId("DepthNormalsOnly") };

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
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            useNativeRenderPass = false;
            this.shaderTagIds = k_DepthNormals;
        }

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
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
                ConfigureTarget(normalHandle, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            else
                ConfigureTarget(normalHandle, depthHandle);

            ConfigureClear(ClearFlag.All, Color.black);
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData)
        {
            var renderingData = passData.renderingData;
            var cmd = renderingData.commandBuffer;
            var shaderTagIds = passData.shaderTagIds;
            var filteringSettings = passData.filteringSettings;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthNormalPrepass)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            PassData passData = new PassData();
            passData.renderingData = renderingData;
            passData.shaderTagIds = this.shaderTagIds;
            passData.filteringSettings = m_FilteringSettings;
            ExecutePass(context, passData);
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
        }

        public class PassData
        {
            public TextureHandle cameraDepthTexture;
            public TextureHandle cameraNormalsTexture;
            public RenderingData renderingData;
            public List<ShaderTagId> shaderTagIds;
            public FilteringSettings filteringSettings;
        }

        public void Render(out TextureHandle cameraNormalsTexture, out TextureHandle cameraDepthTexture, ref RenderingData renderingData)
        {
            RenderGraph graph = renderingData.renderGraph;
            const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
            const int k_DepthBufferBits = 32;

            using (var builder = graph.AddRenderPass<PassData>("DepthNormals Prepass", out var passData, new ProfilingSampler("DepthNormals Prepass")))
            {
                var depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
                depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
                cameraDepthTexture = UniversalRenderer.CreateRenderGraphTexture(graph, depthDescriptor, "_CameraDepthTexture", true);

                // TODO RENDERGRAPH: Handle Deferred case, see _CameraNormalsTexture logic in UniversalRenderer.cs 
                var normalDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                normalDescriptor.depthBufferBits = 0;
                // Never have MSAA on this depth texture. When doing MSAA depth priming this is the texture that is resolved to and used for post-processing.
                normalDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
                                                    // Find compatible render-target format for storing normals.
                                                    // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
                                                    // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
                                                    // TODO: deferred
     
                normalDescriptor.graphicsFormat = GetGraphicsFormat();
                cameraNormalsTexture = UniversalRenderer.CreateRenderGraphTexture(graph, normalDescriptor, "_CameraNormalsTexture", true);

                passData.cameraNormalsTexture = builder.UseColorBuffer(cameraNormalsTexture, 0);
                passData.cameraDepthTexture = builder.UseDepthBuffer(cameraDepthTexture, DepthAccess.Write);
                passData.renderingData = renderingData;
                passData.shaderTagIds = this.shaderTagIds;
                passData.filteringSettings = m_FilteringSettings;

                //  TODO RENDERGRAPH: culling? force culluing off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data);
                });

                return;
            }
        }
    }
}
