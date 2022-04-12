using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Matrix4Node : IStandardNode
    {
        public static string Name = "Matrix4x4";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = Matrix4x4;",
            new ParameterDescriptor("Matrix4x4", TYPE.Mat4, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Mat4, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "make the darks darker and the brights brighter",
            categories: new string[2] { "Input", "Matrix" },
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix4x4"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a 4x4 matrix"
                )
            }
        );
    }
}
