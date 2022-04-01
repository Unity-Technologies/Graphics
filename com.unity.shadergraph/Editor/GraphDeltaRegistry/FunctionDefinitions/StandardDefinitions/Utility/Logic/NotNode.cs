using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NotNode : IStandardNode
    {
        public static string Name = "Not";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = !In;",
            new ParameterDescriptor("In", TYPE.Bool, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the opposite of the input",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[1] { "!" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the opposite of the input"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "!" },
            { "Tooltip", "returns the opposite of the input " },
            { "Category", "Utility, Logic" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the opposite of the input " },
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
