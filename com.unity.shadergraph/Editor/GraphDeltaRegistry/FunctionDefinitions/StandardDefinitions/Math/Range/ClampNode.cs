using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ClampNode : IStandardNode
    {
        public static string Name = "Clamp";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = clamp(In, Min, Max);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Min", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Max", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "prevents the input from going below min or above max",
            categories: new string[2] { "Math", "Range" },
            synonyms: new string[1] { "limit" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Min",
                    tooltip: "minimum value"
                ),
                new ParameterUIDescriptor(
                    name: "Max",
                    tooltip: "maximum value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "inupt value clamped between Min and Max"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "Name.Synonyms", "limit" },
            { "Tooltip", "prevents the input from going below min or above max" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Min.Tooltip", "minimum value" },
            { "Parameters.Max.Tooltip", "maximum value" },
            { "Parameters.Out.Tooltip", "inupt value clamped between Min and Max" }
        };
    }
}
