using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NotNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Not",
            "Out = !In;",
            new ParameterDescriptor("In", TYPE.Bool, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "!" },
            { "Tooltip", "returns the opposite of the input " },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the opposite of the input " },
            { "Category", "Utility, Logic" }
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
