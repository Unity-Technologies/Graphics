using System;
using System.Collections.Generic;
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

        void SetupTarget(ref TargetSetupContext context);
        void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks);
        ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks);

        // TODO: Should we have the GUI implementation integrated in this way?
        // TODO: Also I currently use this to rebuild the inspector
        // TODO: How are we going to update the inspector when the data object is changed? (Sai)
        void GetInspectorContent(PropertySheet propertySheet, Action onChange);
    }
}
