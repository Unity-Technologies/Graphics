using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public struct PassData
    {
        public LightData lightData;
        public CameraData cameraData;
    }

    public abstract class ScriptableRenderPass
    {
        public ScriptableRenderPass(ForwardRenderer renderer, int[] inputs, int[] targets)
        {
            inputHandles = inputs;
            targetHandles = targets;

            attachments = new RenderTargetIdentifier[targets.Length];
            for (int i = 0; i < targets.Length; ++i)
                attachments[i] = renderer.GetSurface(targets[i]);
        }

        public abstract void BindSurface(CommandBuffer cmd, RenderTextureDescriptor attachmentDescriptor, int samples);

        public abstract void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref PassData passData);

        public abstract void Dispose(CommandBuffer cmd);

        public int[] inputHandles { get; private set; }

        public int[] targetHandles { get; private set; }
        public RenderTargetIdentifier[] attachments { get; private set; }

        protected List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>();

        public void RegisterShaderPassName(string passName)
        {
            m_ShaderPassNames.Add(new ShaderPassName(passName));
        }
    }
}
