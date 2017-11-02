using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode
    {
        SurfaceMaterialOptions options { get; }
        IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper);
    }
}
