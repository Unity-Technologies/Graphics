using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    public interface IMasterNode
    {
        SurfaceMaterialOptions options { get; }
        IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper);
    }
}