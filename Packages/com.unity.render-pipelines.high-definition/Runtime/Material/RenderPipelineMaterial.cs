using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class RenderPipelineMaterial : Object
    {
        // GBuffer management
        public virtual bool IsDefferedMaterial() { return false; }

        // Regular interface
        public virtual void Build(HDRenderPipeline renderPipeline) { }

        // Optionally build resources offline (in editor).
        public virtual void BuildOffline(ref List<RenderTexture> resourceList) { }

        public virtual void Cleanup() { }

        // Following function can be use to initialize GPU resource (once or each frame) and bind them
        public virtual void RenderInit(CommandBuffer cmd) { }
        public virtual void Bind(CommandBuffer cmd) { }
    }
}
