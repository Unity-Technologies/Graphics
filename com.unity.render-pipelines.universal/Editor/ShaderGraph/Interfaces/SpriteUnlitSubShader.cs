using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.Universal
{
    abstract class SpriteUnlitSubShader : JsonObject, ISubShader
    {
        public abstract string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        public abstract bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        public abstract int GetPreviewPassIndex();
    }
}
