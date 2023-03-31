using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class Vector4Node : IStandardNode
    {
        public static string Name => "Vector4";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = X;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("X", TYPE.Vec4, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Vector 4",
            tooltip: "Creates a user-defined value with 4 channels.",
            category: "Input/Basic",
            synonyms: new string[4] { "4", "v4", "vec4", "float4" },
            description: "pkg://Documentation~/previews/Vector4.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "X",
                    displayName: string.Empty,
                    tooltip: "a user-defined value with 4 channels"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "a user-defined value with 4 channels"
                )
            }
        );
    }
}
