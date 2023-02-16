using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ComparisonNode : IStandardNode
    {
        public static string Name => "Comparison";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Equal",
                    "    Out = A == B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "NotEqual",
                    "    Out = A != B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "Less",
                    "    Out = A < B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "LessOrEqual",
                    "    Out = A <= B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "Greater",
                    "    Out = A > B;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Float, Usage.In),
                        new ParameterDescriptor("B", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
                    }
                ),
                new(
                    "GreaterOrEqual",
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
            category: "Utility/Logic",
            synonyms: new string[6] { "equal", "less", "greater", ">", "<", "=" },
            description: "pkg://Documentation~/previews/Comparison.md",
            hasPreview: false,
            selectableFunctions: new()
            {
                { "Equal", "== Equal" },
                { "NotEqual", "!= NotEqual" },
                { "Less", "< Less" },
                { "LessOrEqual", "<= Less Or Equal" },
                { "Greater", "> Greater" },
                { "GreaterOrEqual", ">= Greater Or Equal" },
            },
            hasModes: true,
            functionSelectorLabel: " ",
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
