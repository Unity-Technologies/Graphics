using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class StepNode : IStandardNode
    {
        public static string Name = "Step";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = step(Edge, In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Edge", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns 1 if the input is greater or equal to Edge else returns 0",
            categories: new string[2] { "Math", "Round" },
            synonyms: new string[1] { "quantize" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "an input vale"
                ),
                new ParameterUIDescriptor(
                    name: "Edge",
                    tooltip: "the rounding point"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "1 if the input is greater or equal to Edge,  otherwise 0"
                )
            }
        );
    }
}
