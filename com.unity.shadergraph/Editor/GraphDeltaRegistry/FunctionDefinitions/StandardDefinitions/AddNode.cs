using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class AddNode : IStandardNode
    {
        public FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Add", // Name
            "Out = A + B;",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Addition, Sum" },
            { "Tooltip", "Addition Function" },
            { "Parameters.In.Tooltip", "Input A" },
            { "Parameters.Exp.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "A + B" }
        };
    }
}
