using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMasterNode : INode
    {
        string GetShader(
            GenerationMode mode,
            out List<PropertyGenerator.TextureInfo> configuredTextures);

        string GetVariableNameForNode();
    }
}
