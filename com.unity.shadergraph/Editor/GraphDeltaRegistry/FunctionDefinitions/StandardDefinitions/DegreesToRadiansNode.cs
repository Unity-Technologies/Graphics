using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class DegreesToRadiansNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "DegreesToRadians",
            "Out = radians(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "degtorad, radians, convert" },
            { "Tooltip", "converts degrees to radians" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the input converted to radians" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Degrees To Radians" }
        };
    }
}
