using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class AnyNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Any",
            "Out = any(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns true if any of the components of the input In are non-zero" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "true if any input components are non-zero" },
            { "Category", "Utility, Logic" }
        };
    }
}
