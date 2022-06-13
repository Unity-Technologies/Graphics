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
                    "ComparisonEqual",
                    "    Out = A == B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "ComparisonNotEqual",
                    "    Out = A != B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "ComparisonLess",
                    "    Out = A < B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "ComparisonLessOrEqual",
                    "    Out = A <= B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "ComparisonGreater",
                    "    Out = A > B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "ComparisonGreaterOrEqual",
                    "    Out = A >= B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Compares the 2 input values based on the selected condition.",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[6] { "equal", "less", "greater", ">", "<", "=" },
            hasPreview: false,
            selectableFunctions: new()
            {
                { "ComparisonEqual", "== Equal" },
                { "ComparisonNotEqual", "!= NotEqual" },
                { "ComparisonLess", "< Less" },
                { "ComparisonLessOrEqual", "<= Less Or Equal" },
                { "ComparisonGreater", "> Greater" },
                { "ComparisonGreaterOrEqual", ">= Greater Or Equal" },
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
