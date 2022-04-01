using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NegateNode : IStandardNode
    {
        public static string Name = "Negate";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = -1 * In;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "multiplies the input by negative 1",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[3] { "invert", "opposite", "-" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the negated version of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "invert, opposite, -" },
            { "Tooltip", "multiplies the input by negative 1" },
            { "Category", "Math, Advanced" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the negated version of the input" }
        };
    }
}
