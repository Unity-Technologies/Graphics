using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalizeNode : IStandardNode
    {
        public static string Name = "Normalize";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = normalize(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "adjusts the input vector to unit length",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[1] { "Unitize" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a vector to normalize"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "In / Length(In)"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Advanced" },
            { "Name.Synonyms", "Unitize" },
            { "Tooltip", "adjusts the input vector to unit length" },
            { "Parameters.In.Tooltip", "a vector to normalize" },
            { "Parameters.Out.Tooltip", "In / Length(In)" }
        };
    }
}
