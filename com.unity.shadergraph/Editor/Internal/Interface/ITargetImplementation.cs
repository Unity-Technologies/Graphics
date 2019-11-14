using System;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    interface ITargetImplementation
    {
        Type targetType { get; } 
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }

        bool IsValid(IMasterNode masterNode);
        bool IsPipelineCompatible(RenderPipelineAsset currentPipeline);
        void SetupTarget(ref TargetSetupContext context);
    }
}
