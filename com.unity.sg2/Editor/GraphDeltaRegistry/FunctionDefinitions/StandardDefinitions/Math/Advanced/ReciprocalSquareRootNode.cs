using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ReciprocalSquareRootNode : IStandardNode
    {
        public static string Name = "ReciprocalSquareRoot";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = rsqrt(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] {1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Reciprocal Square Root",
            tooltip: "divides 1 by the square root of the input",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[2] { "rsqrt", "inversesqrt" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "1 divided by the square root of the input"
                )
            }
        );
    }
}
