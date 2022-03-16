using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NotNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Not",
            "Out = !In;",
            new ParameterDescriptor("In", TYPE.Bool, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "!" },
            { "Tooltip", "returns the opposite of the input " },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the opposite of the input " },
            { "Category", "Utility, Logic" }
        };
    }
}
