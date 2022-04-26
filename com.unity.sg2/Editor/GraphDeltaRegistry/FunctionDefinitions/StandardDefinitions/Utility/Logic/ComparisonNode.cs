using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ComparisonNode : IStandardNode
    {
        public static string Name = "Comparison";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Equal",
                    "Out = A == B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                ),
                new(
                    1,
                    "NotEqual",
                    "Out = A != B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                ),
                new(
                    1,
                    "Less",
                    "Out = A < B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                ),
                new(
                    1,
                    "LessOrEqual",
                    "Out = A <= B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                ),
                new(
                    1,
                    "Greater",
                    "Out = A > B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                ),
                new(
                    1,
                    "GreaterOrEqual",
                    "Out = A >= B;",
                    new ParameterDescriptor("A", TYPE.Float, Usage.In),
                    new ParameterDescriptor("B", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "compares the two input values based on the condition selected on the dropdown",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[6] { "equal", "less", "greater", ">", "<", "=" },
            hasPreview: false,
            selectableFunctions: new()
            {
                { "Equal", "Equal" },
                { "NotEqual", "NotEqual" },
                { "Less", "Less" },
                { "LessOrEqual", "Less Or Equal" },
                { "Greater", "Greater" },
                { "GreaterOrEqual", "Greater Or Equal" },
            },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the first input value"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the second input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "true if the two input values meet the comparison criteria in the dropdown"
                )
            }
        );
    }
}
