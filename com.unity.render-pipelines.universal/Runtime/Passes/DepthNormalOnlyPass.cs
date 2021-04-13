using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthNormals");

        internal RenderTextureDescriptor normalDescriptor { get; set; }
        internal RenderTextureDescriptor depthDescriptor { get; set; }
        internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private RTHandle depth { get; set; }
        private RTHandle normal { get; set; }
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
        public void Setup(ScriptableRenderContext context, RenderTextureDescriptor baseDescriptor, ref RTHandle depth, ref RTHandle normal)
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

            this.depth = depth;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = k_DepthBufferBits;
            baseDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            depthDescriptor = baseDescriptor;

            this.normal = normal;
            baseDescriptor.graphicsFormat = normalsFormat;
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.msaaSamples = 1;
            normalDescriptor = baseDescriptor;

            CommandBuffer cmd = CommandBufferPool.Get();

            if (depth == null || depth.rt.depth != depthDescriptor.depthBufferBits)
            {
                depth?.Release();
                depth = RTHandles.Alloc(Vector2.one,
                    depthBufferBits: (DepthBits)depthDescriptor.depthBufferBits,
                    dimension: depthDescriptor.dimension,
                    enableRandomWrite: depthDescriptor.enableRandomWrite,
                    msaaSamples: (MSAASamples)depthDescriptor.msaaSamples,
                    autoGenerateMips: depthDescriptor.autoGenerateMips,
                    bindTextureMS: depthDescriptor.bindMS,
                    memoryless: depthDescriptor.memoryless,
                    colorFormat: GraphicsFormat.DepthAuto,
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    name: "_CameraDepthTexture");
                cmd.SetGlobalTexture(depth.name, depth);
            }

            if (normal == null || normal.rt.graphicsFormat != normalsFormat)
            {
                normal?.Release();
                normal = RTHandles.Alloc(Vector2.one,
                    colorFormat: normalDescriptor.graphicsFormat,
                    dimension: normalDescriptor.dimension,
                    enableRandomWrite: normalDescriptor.enableRandomWrite,
                    useMipMap: normalDescriptor.mipCount > 0,
                    autoGenerateMips: normalDescriptor.autoGenerateMips,
                    msaaSamples: (MSAASamples)normalDescriptor.msaaSamples,
                    bindTextureMS: normalDescriptor.bindMS,
                    memoryless: normalDescriptor.memoryless,
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    name: "_CameraNormalsTexture");
                cmd.SetGlobalTexture(normal.name, normal);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            this.shaderTagId = k_ShaderTagId;
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(normal, depth);
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

            if (depth != null)
            {
                normal = null;
                depth = null;
            }
        }
    }
}
