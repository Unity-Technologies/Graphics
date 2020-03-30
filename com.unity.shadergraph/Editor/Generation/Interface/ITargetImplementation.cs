using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface ITargetImplementation
    {
        Type targetType { get; }
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }
        string renderTypeTag { get; }
        string renderQueueTag { get; }

        void SetupTarget(ref TargetSetupContext context);
        void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks);
        ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks);
        void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode);
        void ProcessPreviewMaterial(Material material);
        VisualElement GetSettings(Action onChange);
    }

    [GenerationAPI]
    internal interface ITargetHasMetadata
    {
        string metadataIdentifier { get; }
        ScriptableObject GetMetadataObject();
    }
}
