using RenderGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    public struct GBufferNode : IRenderNode
    {
        Camera camera { get; set; }
        CullResults cullResults { get; set; }

        public Resource<AttachmentIdentifier> depth { get; private set; }
        public Resource<AttachmentIdentifier> albedo { get; private set; }
        public Resource<AttachmentIdentifier> specRough { get; private set; }
        public Resource<AttachmentIdentifier> normal { get; private set; }
        public Resource<AttachmentIdentifier> emission { get; private set; }

        public GBufferNode(Camera camera, CullResults cullResults)
            : this()
        {
            this.camera = camera;
            this.cullResults = cullResults;
        }

        public void Setup(ref ResourceBuilder builder)
        {
            depth = builder.ReadWriteAttachment();
            albedo = builder.WriteAttachment();
            specRough = builder.WriteAttachment();
            normal = builder.WriteAttachment();
            emission = builder.WriteAttachment();
        }

        public void Run(ref ResourceContext r, ScriptableRenderContext context)
        {
            var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("LightweightDeferred"))
            {
                sorting = { flags = SortFlags.CommonOpaque },
                rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
                flags = DrawRendererFlags.EnableInstancing,
            };

            var filterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };

            context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
        }
    }
}
