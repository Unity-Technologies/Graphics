using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SubtractNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Subtract", // Name
            "Out = A - B;",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In), //defaults to 1
            new ParameterDescriptor("B", TYPE.Vector, Usage.In), //defaults to 1
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Basic" },
            { "Name.Synonyms", "subtraction, remove, -, minus" },
            { "Tooltip", "removes the value of B from A" },
            { "Parameters.Out.Tooltip", "A minus B" }
        };
    }
}
