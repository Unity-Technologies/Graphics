using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class IsInfiniteNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "IsInfinite",
            "Out = isinf(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns true if the input In is an infinite value" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "true if the input is an infinite value" },
            { "Category", "Utility, Logic" },
            { "DisplayName", "Is Infinite" }
        };
    }
}
