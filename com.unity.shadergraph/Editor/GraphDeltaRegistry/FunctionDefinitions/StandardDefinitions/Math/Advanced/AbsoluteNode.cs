using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class AbsoluteNode : IStandardNode
    {
        public static string Name = "Absolute";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = abs(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the positive version of the input",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[1] { "positive" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the positive version of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "positive" },
            { "Tooltip", "returns the positive version of the input " },
            { "Category", "Math, Advanced" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the positive version of the input" }
        };
    }
}
