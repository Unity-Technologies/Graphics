using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class CombineNode : IStandardNode
    {
        public static string Name = "Combine";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
    RGBA.r = R;
    RGBA.g = G;
    RGBA.b = B;
    RGBA.a = A;
    RGB.r = R;
    RGB.g = G;
    RGB.b = B;
    RG.r = R;
    RG.g = G;
",
            new ParameterDescriptor("R", TYPE.Float, Usage.In),
            new ParameterDescriptor("G", TYPE.Float, Usage.In),
            new ParameterDescriptor("B", TYPE.Float, Usage.In),
            new ParameterDescriptor("A", TYPE.Float, Usage.In),
            new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
            new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("RG", TYPE.Vec2, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Merges 2 or more input float values into a vector.",
            categories: new string[1] { "Channel" },
            synonyms: new string[1] { "append" },
            parameters: new ParameterUIDescriptor[7] {
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "red channel of output"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "green channel of output"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "blue channel of output"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "alpha channel of output"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 formed by the input values"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "A vector3 formed by the input values"
                ),
                new ParameterUIDescriptor(
                    name: "RG",
                    tooltip: "A vector2 formed by the input values"
                )
            }
        );
    }
}
