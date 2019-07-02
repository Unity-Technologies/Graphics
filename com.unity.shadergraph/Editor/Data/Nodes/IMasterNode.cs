using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    interface IMasterNode
    {
        string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        ISubShader GetActiveSubShader();
        void ProcessPreviewMaterial(Material material);
    }
}
