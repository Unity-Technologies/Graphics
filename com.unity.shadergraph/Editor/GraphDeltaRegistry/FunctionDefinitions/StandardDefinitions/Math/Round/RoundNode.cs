using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RoundNode : IStandardNode
    {
        public static string Name = "Round";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = round(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "move the input up or down to the nearest whole number",
            categories: new string[2] { "Math", "Round" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the nearest whole number of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "move the input up or down to the nearest whole number" },
            { "Category", "Math, Round" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the nearest whole number of the input" },
        };
    }
}
