using RenderGraph;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    public struct DeferredLightingNode : IRenderNode
    {
        public Resource<AttachmentIdentifier> depth { get; private set; }
        public Resource<AttachmentIdentifier> albedo { get; private set; }
        public Resource<AttachmentIdentifier> specRough { get; private set; }
        public Resource<AttachmentIdentifier> normal { get; private set; }
        public Resource<AttachmentIdentifier> lighting { get; private set; }

        public void Setup(ref ResourceBuilder builder)
        {
            depth = builder.ReadAttachment();
            albedo = builder.ReadAttachment();
            specRough = builder.ReadAttachment();
            normal = builder.ReadAttachment();
            lighting = builder.WriteAttachment();
        }

        public void Run(ref ResourceContext r, ScriptableRenderContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
