using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineMaterial : Object
    {
        // GBuffer management
        public virtual int GetMaterialGBufferCount() { return 0; }
        public virtual void GetMaterialGBufferDescription(out RenderTextureFormat[] RTFormat, out RenderTextureReadWrite[] RTReadWrite)
        {
            RTFormat = null;
            RTReadWrite = null;
        }

        // Regular interface
        public virtual void Build(HDRenderPipelineAsset hdAsset) {}
        public virtual void Cleanup() {}

        // Following function can be use to initialize GPU resource (once or each frame) and bind them
        public virtual void RenderInit(CommandBuffer cmd) {}
        public virtual void Bind() {}
    }
}
