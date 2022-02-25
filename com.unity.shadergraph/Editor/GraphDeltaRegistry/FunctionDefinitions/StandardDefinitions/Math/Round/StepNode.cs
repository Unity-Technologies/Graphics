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
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Edge", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Round" },
            { "Name.Synonyms", "quantize" },
            { "Tooltip", "returns 1 if the input is greater or equal to Edge else returns 0" },
            { "Parameters.In.Tooltip", "an input vale" },
            { "Parameters.Edge.Tooltip", "the rounding point" },
            { "Parameters.Out.Tooltip", "1 if the input is greater or equal to Edge,  otherwise 0" }
        };
    }
}
