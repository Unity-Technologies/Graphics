#if HAS_VFX_GRAPH

using UnityEngine.Rendering.Universal;

namespace UnityEditor.VFX.URP
{
    class VFXURPSubOutput : VFXSRPSubOutput
    {
        //URP only support motion vector on opaque geometry (with or without ShaderGraph)
        public override bool supportsMotionVector => owner.isBlendModeOpaque;

        public override string GetRenderPipelineTagStr() => UniversalRenderPipeline.k_ShaderTagName;
    }
}
#endif
