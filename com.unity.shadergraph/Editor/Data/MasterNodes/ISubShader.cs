using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    interface ISubShader : IJsonObject
    {
        string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        int GetPreviewPassIndex();
    }
}
