using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode
    {
        string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}
