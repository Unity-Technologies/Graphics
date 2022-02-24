using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class AbsoluteNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,          // Version
            "Absolute",    // Name
            "Out = abs(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "positive" },
            { "Tooltip", "returns the positive version of the input " },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the positive version of the input" },
            { "Category", "Math, Advanced" }

        };
    }
}
