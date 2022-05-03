using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class LengthNode : IStandardNode
    {
        public static string Name = "Length";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = length(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the magnitude or length of the input vector.",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[1] { "measure" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input vector"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the length of the input vector"
                )
            }
        );
    }
}
