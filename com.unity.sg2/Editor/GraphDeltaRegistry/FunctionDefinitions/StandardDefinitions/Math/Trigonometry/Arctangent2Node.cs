using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Arctangent2Node : IStandardNode
    {
        public static string Name = "Arctangent2";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = atan2(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the arctangent of input A divided by input B",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "atan2" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the numerator"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the denominator"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the arctangent of A divided by B"
                )
            }
        );
    }
}
