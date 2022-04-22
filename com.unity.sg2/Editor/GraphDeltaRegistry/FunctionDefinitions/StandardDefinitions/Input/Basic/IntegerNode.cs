using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class IntegerNode : IStandardNode
    {
        public static string Name = "Integer";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = IntConst;",
            new ParameterDescriptor("IntConst", TYPE.Int, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Int, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Contrast",
            tooltip: "a constant, single-channel, whole number value",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[1] { "whole number" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "IntConst"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a single channel value without a decimal"
                )
            }
        );
    }
}
