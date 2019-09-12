using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    interface IMasterNode
    {
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        void ProcessPreviewMaterial(Material material);
    }
}
