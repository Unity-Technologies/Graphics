using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthNormals");

        internal RenderTextureDescriptor normalDescriptor { get; set; }
        internal RenderTextureDescriptor depthDescriptor { get; set; }
        internal bool allocateDepth { get; set; } = true;
        internal bool allocateNormal { get; set; } = true;
        internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private RenderTargetHandle depthHandle { get; set; }
        private RenderTargetHandle normalHandle { get; set; }
        private FilteringSettings m_FilteringSettings;

        // Constants
        private const int k_DepthBufferBits = 32;

        /// <summary>
        /// Create the DepthNormalOnlyPass
        /// </summary>
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthNormalOnlyPass));
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthHandle, RenderTargetHandle normalHandle)
        {
            // Find compatible render-target format for storing normals.
            // Shader code outputs normals in signed format to be compatible with deferred gbuffer layout.
            // Deferred gbuffer format is signed so that normals can be blended for terrain geometry.
            GraphicsFormat normalsFormat;
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8G8B8A8_SNorm, FormatUsage.Render))
                normalsFormat = GraphicsFormat.R8G8B8A8_SNorm; // Preferred format
            else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render))
                normalsFormat = GraphicsFormat.R16G16B16A16_SFloat; // fallback
            else
                normalsFormat = GraphicsFormat.R32G32B32A32_SFloat; // fallback

            this.depthHandle = depthHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = k_DepthBufferBits;
            baseDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            depthDescriptor = baseDescriptor;

            this.normalHandle = normalHandle;
            baseDescriptor.graphicsFormat = normalsFormat;
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.msaaSamples = 1;
            normalDescriptor = baseDescriptor;

            this.allocateDepth = true;
            this.allocateNormal = true;
            this.shaderTagId = k_ShaderTagId;
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (this.allocateNormal)
                cmd.GetTemporaryRT(normalHandle.id, normalDescriptor, FilterMode.Point);
            if (this.allocateDepth)
                cmd.GetTemporaryRT(depthHandle.id, depthDescriptor, FilterMode.Point);
            ConfigureTarget(
                new RenderTargetIdentifier(normalHandle.Identifier(), 0, CubemapFace.Unknown, -1),
                new RenderTargetIdentifier(depthHandle.Identifier(), 0, CubemapFace.Unknown, -1)
            );
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthNormalPrepass)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(this.shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            if (depthHandle != RenderTargetHandle.CameraTarget)
            {
                if (this.allocateNormal)
                    cmd.ReleaseTemporaryRT(normalHandle.id);
                if (this.allocateDepth)
                    cmd.ReleaseTemporaryRT(depthHandle.id);
                normalHandle = RenderTargetHandle.CameraTarget;
                depthHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
