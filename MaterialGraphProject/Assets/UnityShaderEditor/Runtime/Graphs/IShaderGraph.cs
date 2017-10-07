using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    interface IShaderGraph
    {
        string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}