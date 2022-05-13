using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Matrix3Node : IStandardNode
    {
        public static string Name = "Matrix3x3";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = Matrix3x3;",
            new ParameterDescriptor("Matrix3x3", TYPE.Mat3, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Mat3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix 3x3",
            tooltip: "creates a static 3x3 matrix",
            categories: new string[2] { "Input", "Matrix" },
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix3x3"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a 3x3 matrix"
                )
            }
        );
    }
}
