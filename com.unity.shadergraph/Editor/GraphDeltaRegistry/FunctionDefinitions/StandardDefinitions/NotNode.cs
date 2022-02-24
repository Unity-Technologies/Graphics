using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class NotNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Not",
            "Out = !In;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
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
