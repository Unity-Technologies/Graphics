// This enum definition really belongs to MaterialExtension.cs, however for limitation of the parser that generates HLSL definitions,
// hlsl code is not generated from files that use C# 7+ features, so this needs to be in its own file for now. Will need to move back
// MaterialExtension.cs once the parser is updated to support C# 7.
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    // Enum values are hardcoded for retro-compatibility. Don't change them.
    [GenerateHLSL]
    enum BlendMode
    {
        // Note: value is due to code change, don't change the value
        Alpha = 0,
        Premultiply = 4,
        Additive = 1
    }
}
