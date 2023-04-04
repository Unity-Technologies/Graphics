using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SquareRootNode : IStandardNode
    {
        public static string Name => "SquareRoot";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            "SquareRoot",
            "Out = sqrt(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Square Root",
            tooltip: "Calculates the square root of the input.",
            category: "Math/Basic",
            synonyms: new string[1] { "sqrt" },
            description: "pkg://Documentation~/previews/SquareRoot.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the square root of the input"
                )
            }
        );
    }
}
