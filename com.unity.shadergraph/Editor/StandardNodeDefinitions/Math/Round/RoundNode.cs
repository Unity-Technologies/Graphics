using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class RoundNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Round",
            "Out = round(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "move the input up or down to the nearest whole number" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the nearest whole number of the input" },
            { "Category", "Math, Round" }
        };
    }
}
