using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class AllNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "All",
            "Out = all(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
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
