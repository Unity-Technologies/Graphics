using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface IMasterNode
    {
        string renderQueueTag { get; }
        string renderTypeTag { get; }
        bool virtualTexturingEnabled { get; }
        ConditionalField[] GetConditionalFields(PassDescriptor pass);
        void ProcessPreviewMaterial(Material material);
    }
}
