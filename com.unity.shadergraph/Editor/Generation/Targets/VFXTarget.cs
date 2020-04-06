using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    sealed class VFXTarget : Target
    {
        public VFXTarget()
        {
            displayName = "Visual Effect";
        }

        public override void Setup(ref TargetSetupContext context)
        {
        }

        public override bool IsValid(IMasterNode masterNode)
        {
            return masterNode is VfxMasterNode;
        }

        public override bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline != null;
        }
    }
}
