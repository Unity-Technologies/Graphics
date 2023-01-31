using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SplitNode : IStandardNode
    {
        public static string Name => "Split";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    R = In.r;
    G = In.g;
    B = In.b;
    A = In.a;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec4, Usage.In),
                new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                new ParameterDescriptor("A", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Separates the channels of an input vector.",
            category: "Channel",
            synonyms: new string[1] { "separate" },
            hasPreview: false,
            description: "pkg://Documentation~/previews/Split.md",
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a vector to split"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "red channel of the input"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "green channel of the input"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "blue channel of the input"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "alpha channel of the input"
                )
            }
        );
    }
}
