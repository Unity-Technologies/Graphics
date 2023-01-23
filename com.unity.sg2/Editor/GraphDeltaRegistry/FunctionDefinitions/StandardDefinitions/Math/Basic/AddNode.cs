using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class AddNode : IStandardNode
    {
        public static string Name => "Add";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = A + B;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                new ParameterDescriptor("B", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the sum of input A and input B.",
            category: "Math/Basic",
            synonyms: new string[4] { "Addition", "Sum", "+", "plus" },
            description: "pkg://Documentation~/previews/Add.md",
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
                    tooltip: "A + B"
                )
            }
        );
    }
}
