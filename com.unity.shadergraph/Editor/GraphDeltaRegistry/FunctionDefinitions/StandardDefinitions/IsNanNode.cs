using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class IsNanNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "IsNan",
            "Out = isnan(In) ? 1 : 0;",
            new ParameterDescriptor("In", TYPE.Bool, Usage.In),
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
