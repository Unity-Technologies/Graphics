using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode
    {
        SurfaceMaterialOptions options { get; }
        string GetShader(GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}
