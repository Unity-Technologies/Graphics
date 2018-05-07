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
        public LightweightForwardRenderer renderer { get; private set; }
        public int[] colorHandles { get; set; }
        public int depthHandle;

        protected bool m_Disposed;
        protected List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>();

        public ScriptableRenderPass(LightweightForwardRenderer renderer)
        {
            this.renderer = renderer;
            m_Disposed = true;
        }

        public abstract void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples);

        public abstract void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData);

        public abstract void Dispose(CommandBuffer cmd);

        public RenderTargetIdentifier GetSurface(int handle)
        {
            if (renderer == null)
            {
                Debug.LogError("Pass has invalid renderer");
                return new RenderTargetIdentifier();
            }

            return renderer.GetSurface(handle);
        }

        public void RegisterShaderPassName(string passName)
        {
            m_ShaderPassNames.Add(new ShaderPassName(passName));
        }
    }
}
