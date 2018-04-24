using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode
    {
        string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
        bool IsPipelineCompatible(IRenderPipeline renderPipeline);
    }
}
