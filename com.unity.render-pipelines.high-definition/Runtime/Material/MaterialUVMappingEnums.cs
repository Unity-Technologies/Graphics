// This enum definitions really belongs to MaterialExtension.cs, however for limitation of the parser that generates HLSL definitions,
// hlsl code is not generated from files that use C# 7+ features, so this needs to be in its own file for now. Will need to move back
// MaterialExtension.cs once the parser is updated to support C# 7.
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal enum UVEmissiveMapping
    {
        UV0,
        UV1,
        UV2,
        UV3,
        Planar,
        Triplanar,
        SameAsBase
    }

    [GenerateHLSL]
    internal enum UVBaseMapping
    {
        UV0,
        UV1,
        UV2,
        UV3,
        Planar,
        Triplanar
    }
}
