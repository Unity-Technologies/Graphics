using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class RenderPipelineMaterial : Object
    {
        // GBuffer management
        public virtual bool IsDefferedMaterial() { return false; }
        public virtual int GetMaterialGBufferCount(HDRenderPipelineAsset asset) { return 0; }
        public virtual void GetMaterialGBufferDescription(HDRenderPipelineAsset asset, out GraphicsFormat[] RTFormat, out GBufferUsage[] gBufferUsage, out bool[] enableWrite)
        {
            RTFormat = null;
            gBufferUsage = null;
            enableWrite = null;
        }

        // Regular interface
        public virtual void Build(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources) {}
        public virtual void Cleanup() {}

        // Following function can be use to initialize GPU resource (once or each frame) and bind them
        public virtual void RenderInit(CommandBuffer cmd) {}
        public virtual void Bind(CommandBuffer cmd) {}
    }
}
