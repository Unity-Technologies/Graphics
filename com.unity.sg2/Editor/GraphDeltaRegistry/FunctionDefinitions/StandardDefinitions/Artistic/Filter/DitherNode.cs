using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class DitherNode : IStandardNode
    {
        public static string Name = "Dither";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
    uv = ScreenPosition.xy * _ScreenParams.xy;
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("ScreenPosition", TYPE.Vec2, Usage.In),//TODO: ScreenPosition default support
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out),
            new ParameterDescriptor("uv", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("DITHER_THRESHOLDS", TYPE.Mat4, Usage.Local, new float[]
        {
        1.0f / 17.0f,  9.0f / 17.0f,  3.0f / 17.0f, 11.0f / 17.0f,
        13.0f / 17.0f,  5.0f / 17.0f, 15.0f / 17.0f,  7.0f / 17.0f,
        4.0f / 17.0f, 12.0f / 17.0f,  2.0f / 17.0f, 10.0f / 17.0f,
        16.0f / 17.0f,  8.0f / 17.0f, 14.0f / 17.0f,  6.0f / 17.0f
        })
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "converts a grayscale value to a black and white value with a dither pattern",
            categories: new string[2] { "Artistic", "Filter" },
            synonyms: new string[4] { "half tone", "noise", "diffusion", "bayer grid" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "ScreenPosition",
                    tooltip: "coordinates used to apply dither pattern"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "dither patterned based on the input value"
                )
            }
        );
    }
}
