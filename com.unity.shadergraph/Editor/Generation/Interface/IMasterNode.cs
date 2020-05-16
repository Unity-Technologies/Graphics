using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface IMasterNode
    {
        string renderQueueTag { get; }
        string renderTypeTag { get; }

        // NOTE: This should only consider whether the MASTER NODE can support virtual texturing. (render pipeline support will be checked separately)
        bool supportsVirtualTexturing { get; }

        ConditionalField[] GetConditionalFields(PassDescriptor pass);
        void ProcessPreviewMaterial(Material material);

        // NOTE: Remove when stacks are integrated, this is only here as a stopgap measure as IHasSettings has been removed
        VisualElement CreateSettingsElement();
    }
}
