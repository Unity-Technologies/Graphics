using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    interface ISubShader
    {
        string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
    }
}
