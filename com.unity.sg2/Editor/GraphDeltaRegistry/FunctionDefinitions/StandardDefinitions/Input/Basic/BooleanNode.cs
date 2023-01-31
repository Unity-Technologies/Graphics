using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class BooleanNode : IStandardNode
    {
        public static string Name => "Boolean";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = BoolConst;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("BoolConst", TYPE.Bool, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "a true/false check box",
            category: "Input/Basic",
            synonyms: new string[5] { "switch", "true", "false", "on", "off" },
            description: "pkg://Documentation~/previews/Boolean.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "BoolConst",
                    displayName: " "
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a constant true or false value"
                ),
            }
        );
    }
}
