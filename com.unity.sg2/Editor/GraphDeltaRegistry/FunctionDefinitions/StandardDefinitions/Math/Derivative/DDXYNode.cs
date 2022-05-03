using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DDXYNode : IStandardNode
    {
        public static string Name = "DDXY";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = abs(ddx(In)) + abs(ddy(In));",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the sum of both partial derivatives of the input.",
            categories: new string[2] { "Math", "Derivative" },
            synonyms: new string[2] { "derivative", "slope" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sum of both partial derivatives of the input"
                )
            }
        );
    }
}
