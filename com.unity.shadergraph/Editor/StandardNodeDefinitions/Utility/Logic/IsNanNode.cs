using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class IsNanNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "IsNan",
            "Out = isnan(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns true if the input is not a number (NaN)" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "true if the input is not a number (NaN)" },
            { "Category", "Utility, Logic" },
            { "DisplayName", "Is Nan"}
        };
    }
}
