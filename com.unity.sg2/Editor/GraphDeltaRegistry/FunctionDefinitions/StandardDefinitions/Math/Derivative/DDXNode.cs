using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DDXNode : IStandardNode
    {
        public static string Name = "DDX";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = ddx(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the partial derivative of the input in relation to the screen-space x-coordinate.",
            categories: new string[2] { "Math", "Derivative" },
            synonyms: new string[2] { "derivative", "slope" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the difference between the value of the current pixel and the horizontal neighboring pixel"
                )
            }
        );
    }
}
