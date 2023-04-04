using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NotNode : IStandardNode
    {
        public static string Name => "Not";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = !In;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Bool, Usage.In),
                new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the opposite of the input",
            category: "Utility/Logic",
            synonyms: new string[1] { "!" },
            hasPreview: false,
            description: "pkg://Documentation~/previews/Not.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the opposite of the input"
                )
            }
        );
    }
}
