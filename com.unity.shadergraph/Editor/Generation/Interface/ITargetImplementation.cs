using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface ITargetImplementation
    {
        Type targetType { get; }
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }

        Type dataType { get; }
        TargetImplementationData data { get; set; }

        bool IsPipelineCompatible(RenderPipelineAsset currentPipeline);
        void SetupTarget(ref TargetSetupContext context);

        List<BlockFieldDescriptor> GetSupportedBlocks();
        ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks);

        // TODO: Should we have the GUI implementation integrated in this way?
        // TODO: Also I currently use this to rebuild the inspector
        // TODO: How are we going to update the inspector when the data object is changed? (Sai)
        void GetInspectorContent(PropertySheet propertySheet, Action onChange);
    }
}
