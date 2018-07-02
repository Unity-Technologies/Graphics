using System.Runtime.InteropServices;
using RenderGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    public struct GBufferNode : IRenderNode
    {
        readonly bool m_SupportsHDR;

        GCHandle m_Camera;
        VirtualResource<CullResults> m_CullResults;
        VirtualResource<AttachmentIdentifier> m_Depth;

        public VirtualResource<AttachmentIdentifier> albedo;
        public VirtualResource<AttachmentIdentifier> specRough;
        public VirtualResource<AttachmentIdentifier> normal;
        public VirtualResource<AttachmentIdentifier> emission;

        public GBufferNode(Camera camera, bool supportsHdr)
            : this()
        {
            m_Camera = GCHandle.Alloc(camera, GCHandleType.Pinned);
            m_SupportsHDR = supportsHdr;
        }

        public void Setup(ref ResourceBuilder builder)
        {
            m_CullResults = builder.Read<CullResults>();
            m_Depth = builder.Read<AttachmentIdentifier>();
            albedo = builder.Create<AttachmentIdentifier>();
            specRough = builder.Create<AttachmentIdentifier>();
            normal = builder.Create<AttachmentIdentifier>();
            emission = builder.Create<AttachmentIdentifier>();
        }

        public void Run(ref ResourceManager r, ScriptableRenderContext context)
        {
            var camera = (Camera)m_Camera.Target;
            r.Create(albedo, RenderTextureFormat.ARGB32);
            r.Create(specRough, RenderTextureFormat.ARGB32);
            r.Create(normal, RenderTextureFormat.ARGB2101010);
            r.Create(emission, m_SupportsHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
//            m_Depth = builder.ReadAttachment(RenderTextureFormat.Depth);
//            albedo = builder.CreateAttachment(RenderTextureFormat.ARGB32);
//            specRough = builder.CreateAttachment(RenderTextureFormat.ARGB32);
//            normal = builder.CreateAttachment(RenderTextureFormat.ARGB2101010);
//            emission = builder.CreateAttachment(m_SupportsHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            using (context.BeginSubPass(new AttachmentList { r.Get(albedo), r.Get(specRough), r.Get(normal), r.Get(emission) }))
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

                context.DrawRenderers(r.Get(m_CullResults).visibleRenderers, ref drawSettings, filterSettings);
            }

            m_Camera.Free();
        }
    }
}
