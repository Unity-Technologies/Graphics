using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class OrNode : IStandardNode
    {
        public static string Name = "Or";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = A || B;",
            new ParameterDescriptor("A", TYPE.Bool, Usage.In),
            new ParameterDescriptor("B", TYPE.Bool, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns true if either of the inputs are true",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[1] { "||" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Input A"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Input B"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "true if either A or B are true"
                )
            }
        );
    }
}
