using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Internal
{
    public interface IMasterNode
    {
        string renderQueueTag { get; }
        string renderTypeTag { get; }
        ConditionalField[] GetConditionalFields(PassDescriptor pass);
        void ProcessPreviewMaterial(Material material);
    }
}
