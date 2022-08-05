using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DotProductNode : IStandardNode
    {
        public static string Name => "DotProduct";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = dot(A, B);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                new ParameterDescriptor("B", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Dot Product",
            tooltip: "Calculates the dot product of two input vectors.",
            category: "Math/Vector",
            synonyms: new string[0],
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
                    tooltip: "the dot product of A and B"
                )
            }
        );
    }
}
