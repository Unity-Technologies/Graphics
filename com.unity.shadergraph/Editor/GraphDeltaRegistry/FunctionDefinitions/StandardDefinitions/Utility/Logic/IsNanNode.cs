using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class IsNanNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "IsNan",
            "Out = isnan(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
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
