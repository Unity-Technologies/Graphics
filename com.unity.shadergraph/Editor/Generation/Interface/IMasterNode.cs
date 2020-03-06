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
        void ProcessPreviewMaterial(Material material);
    }
}
