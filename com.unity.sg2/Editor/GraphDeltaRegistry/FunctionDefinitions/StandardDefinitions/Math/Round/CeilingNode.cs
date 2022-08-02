using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CeilingNode : IStandardNode
    {
        public static string Name => "Ceiling";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = ceil(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "rounds the input up to the nearest whole number",
            categories: new string[2] { "Math", "Round" },
            synonyms: new string[1] { "up" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the input rounded up to the nearest whole number"
                )
            }
        );
    }
}
