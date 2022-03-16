using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Vector2Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Vector2", // Name
@"
    Out.x = X;
    Out.y = Y;
",
            new ParameterDescriptor("X", TYPE.Float, Usage.In),
            new ParameterDescriptor("Y", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Vector 2" },
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "2, v2, vec2, float2" },
            { "Tooltip", "a user-defined value with 2 channels" },
            { "Parameters.X.Tooltip", "the first component" },
            { "Parameters.Y.Tooltip", "the second component" },
            { "Parameters.Out.Tooltip", "a user-defined value with 2 channels" }
        };
    }
}
