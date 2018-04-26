using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode : INode
    {
        string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}
