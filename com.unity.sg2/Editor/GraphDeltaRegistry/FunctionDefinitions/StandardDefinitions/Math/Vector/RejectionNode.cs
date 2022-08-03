using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RejectionNode : IStandardNode
    {
        public static string Name => "Rejection";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = A - (B * dot(A, B) / dot(B, B));",
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
            tooltip: "Projects input A onto the plane orthogonal, or perpendicular, to input B.",
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
                    tooltip: "the projection of A onto the plane orthogonal, or perpendicular, to B"
                )
            }
        );
    }
}
