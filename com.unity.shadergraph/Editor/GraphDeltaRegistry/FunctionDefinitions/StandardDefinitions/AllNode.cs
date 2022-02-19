using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class AllNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "All",
            "Out = all(In);",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns true if all components of the input In are non-zero" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "true if all input components are non-zero" },
            { "Category", "Utility, Logic" }
        };
    }
}
