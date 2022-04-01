using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IsNanNode : IStandardNode
    {
        public static string Name = "IsNan";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = isnan(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Is Nan",
            tooltip: "returns true if the input is not a number (NaN)",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "true if the input is not a number (NaN)"
                )
            }
        );
        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns true if the input is not a number (NaN)" },
            { "Category", "Utility, Logic" },
            { "DisplayName", "Is Nan" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "true if the input is not a number (NaN)" }
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
