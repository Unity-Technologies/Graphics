using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NandNode : IStandardNode
    {
        public static string Name => "Nand";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = !A && !B;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("A", TYPE.Bool, Usage.In),
                new ParameterDescriptor("B", TYPE.Bool, Usage.In),
                new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Checks if input A and input B are both false.",
            category: "Utility/Logic",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            description: "pkg://Documentation~/previews/Nand.md",
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
                    tooltip: "true if A and B are false"
                )
            }
        );
    }
}
