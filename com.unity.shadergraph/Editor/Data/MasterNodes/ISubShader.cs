using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    interface ISubShader
    {
        string GetSubshader(AbstractMaterialNode outputNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        bool IsMasterNodeCompatible(IMasterNode masterNode);
    }
}
