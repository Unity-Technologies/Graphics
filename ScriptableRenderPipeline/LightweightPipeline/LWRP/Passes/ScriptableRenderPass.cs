using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class ScriptableRenderPass
    {
        public ScriptableRenderPass(RenderTextureFormat[] colorAttachments, RenderTextureFormat depthAttachment)
        {
            this.colorAttachments = colorAttachments;
            this.depthAttachment = depthAttachment;
        }

        public abstract void BindSurface(CommandBuffer cmd, RenderTextureDescriptor attachmentDescriptor, int samples);

        public abstract void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData,
            Camera camera, bool stereoRendering);

        public abstract void Dispose(CommandBuffer cmd);

        public RenderTextureFormat[] colorAttachments { get; set; }
        public RenderTextureFormat depthAttachment { get; set; }
        protected List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>();

        public void RegisterShaderPassName(string passName)
        {
            m_ShaderPassNames.Add(new ShaderPassName(passName));
        }
    }
}
