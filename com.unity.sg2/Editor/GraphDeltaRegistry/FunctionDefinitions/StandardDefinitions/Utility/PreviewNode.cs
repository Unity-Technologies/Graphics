using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PreviewNode : IStandardNode
    {
        public static string Name => "Preview";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = In;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Any, Usage.In),
                new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a preview of the shader at a specific point in the graph.",
            category: "Utility",
            synonyms: new string[1] { "triangle wave" },
            description: "pkg://Documentation~/previews/Preview.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the exact same value as the input"
                )
            }
        );
    }
}
