using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class DegreesToRadiansNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "DegreesToRadians",
            "Out = radians(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "degtorad, radians, convert" },
            { "Tooltip", "converts degrees to radians" },
            { "Parameters.In.Tooltip", "a value in degrees" },
            { "Parameters.Out.Tooltip", "the input converted to radians" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Degrees To Radians" }
        };
    }
}
