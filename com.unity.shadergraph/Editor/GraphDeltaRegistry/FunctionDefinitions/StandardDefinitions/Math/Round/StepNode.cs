using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class StepNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Step", // Name
            "Out = step(Edge, In);",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Edge", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "quantize" },
            { "Tooltip", "returns 1 if the input is greater or equal to Edge else returns 0" },
            { "Parameters.In.Tooltip", "In" },
            { "Parameters.Edge.Tooltip", "Edge" },
            { "Parameters.Out.Tooltip", "1 if the input is greater or equal to Edge,  otherwise 0" }
        };
    }
}
