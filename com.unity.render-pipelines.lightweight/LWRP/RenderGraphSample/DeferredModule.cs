using RenderGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    public struct DeferredModule : IRenderModule
    {
        public Camera camera { get; private set; }
        public CullResults cullResults { get; private set; }
        public bool supportsHDR { get; private set; }

        public DeferredModule(Camera camera, CullResults cullResults, bool supportsHDR)
            : this()
        {
            this.camera = camera;
            this.cullResults = cullResults;
            this.supportsHDR = supportsHDR;
        }

        public void Setup(ref GraphBuilder builder)
        {
            var format = supportsHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            
            var gBufferNode = new GBufferNode(camera, cullResults);
            builder.Connect(builder.CreateAttachment(RenderTextureFormat.ARGB32), gBufferNode.albedo);
            builder.Connect(builder.CreateAttachment(RenderTextureFormat.ARGB32), gBufferNode.specRough);
            builder.Connect(builder.CreateAttachment(RenderTextureFormat.ARGB2101010), gBufferNode.normal);
            builder.Connect(builder.CreateAttachment(format), gBufferNode.emission);

            var lightingNode = new DeferredLightingNode();
            builder.Connect(gBufferNode.albedo, lightingNode.albedo);
            builder.Connect(gBufferNode.specRough, lightingNode.specRough);
            builder.Connect(gBufferNode.normal, lightingNode.normal);
            builder.Connect(gBufferNode.emission, lightingNode.lighting);
        }
    }
}
