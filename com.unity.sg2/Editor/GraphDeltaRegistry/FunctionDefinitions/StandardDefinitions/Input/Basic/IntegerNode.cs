using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class IntegerNode : IStandardNode
    {
        public static string Name => "Integer";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = IntConst;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("IntConst", TYPE.Int, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Int, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "a constant, single-channel, whole number value",
            category: "Input/Basic",
            synonyms: new string[1] { "whole number" },
            description: "pkg://Documentation~/previews/Integer.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "IntConst",
                    displayName: string.Empty
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "a single channel value without a decimal"
                )
            }
        );
    }
}
