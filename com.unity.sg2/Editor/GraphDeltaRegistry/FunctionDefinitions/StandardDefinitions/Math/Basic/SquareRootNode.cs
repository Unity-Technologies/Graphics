using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SquareRootNode : IStandardNode
    {
        public static string Name = "SquareRoot";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SquareRoot",
            "Out = sqrt(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Square Root",
            tooltip: "returns the square root of the input",
            categories: new string[2] { "Math", "Basic" },
            synonyms: new string[1] { "sqrt" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the square root of the input"
                )
            }
        );
    }
}
