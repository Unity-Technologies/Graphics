using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class OrNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Or", // Name
            "Out = A || B;",
            new ParameterDescriptor("A", TYPE.Bool, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Bool, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Utility, Logic" },
            { "Name.Synonyms", "||" },
            { "Tooltip", "returns true if either of the inputs are true" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "true if either A or B are true" }
        };
    }
}
