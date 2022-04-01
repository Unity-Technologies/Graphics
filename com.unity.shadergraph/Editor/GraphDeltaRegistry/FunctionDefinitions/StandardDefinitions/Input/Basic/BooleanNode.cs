using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class BooleanNode : IStandardNode
    {
        public static string Name = "Boolean";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = BoolConst;",
            new ParameterDescriptor("BoolConst", TYPE.Bool, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "a true/false check box",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[5] { "switch", "true", "false", "on", "off" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "BoolConst"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a constant true or false value"
                ),
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "switch, true, false, on, off" },
            { "Tooltip", "a true/false check box" },
            { "Parameters.Out.Tooltip", "a constant true or false value" }
        };
    }
}
