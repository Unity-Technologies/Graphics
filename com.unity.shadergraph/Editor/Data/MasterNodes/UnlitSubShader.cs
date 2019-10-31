using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    abstract class UnlitSubShader : JsonObject, ISubShader
    {
        public abstract string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        public abstract bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        public abstract int GetPreviewPassIndex();
    }
}
