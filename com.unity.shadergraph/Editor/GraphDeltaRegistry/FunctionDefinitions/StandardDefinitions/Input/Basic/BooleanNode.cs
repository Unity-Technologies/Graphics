using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class BooleanNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Boolean", // Name
            "Out = BoolConst;",
            new ParameterDescriptor("BoolConst", TYPE.Bool, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "switch, true, false, on, off" },
            { "Tooltip", "a true/false check box" },
            { "Parameters.Out.Tooltip", "a constant true or false value" }
        };
    }
}
