using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class StepNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Step", // Name
            "Out = step(Edge, In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Edge", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
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
