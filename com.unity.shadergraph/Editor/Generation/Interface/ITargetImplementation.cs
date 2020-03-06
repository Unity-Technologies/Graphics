using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface ITargetImplementation
    {
        Type targetType { get; }
        Type dataType { get; }
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }

        bool IsValid(IMasterNode masterNode);
        bool IsPipelineCompatible(RenderPipelineAsset currentPipeline);
        void SetupTarget(ref TargetSetupContext context);

        List<BlockFieldDescriptor> GetSupportedBlocks(TargetImplementationData data);
        ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks, TargetImplementationData data);
    }
}
