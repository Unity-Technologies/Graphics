using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI] // TODO: Public
    internal abstract class Target
    {
        public string displayName { get; set; }
        public bool isHidden { get; set; }
        public abstract void Setup(ref TargetSetupContext context);
        public abstract bool IsValid(IMasterNode masterNode);
        public abstract bool IsPipelineCompatible(RenderPipelineAsset currentPipeline);
    }
}
