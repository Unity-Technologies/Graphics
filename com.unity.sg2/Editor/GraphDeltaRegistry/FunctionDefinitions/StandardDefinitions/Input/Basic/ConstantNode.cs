using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ConstantNode : IStandardNode
    {
        static string Name => "Constant";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "PI",
                    "    Out = 3.1415926f * Multiplier;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Multiplier", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                    }
                ),
                new(
                    "TAU",
                    "    Out = 6.28318530f * Multiplier;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Multiplier", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                    }
                ),
                new(
                    "PHI",
                    "    Out = 1.618034f * Multiplier;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Multiplier", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                    }
                ),
                new(
                    "E",
                    "    Out = 2.718282f * Multiplier;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Multiplier", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                    }
                ),
                new(
                    "SQRT2",
                    "    Out = 1.414214f * Multiplier;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Multiplier", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Returns the selected constant value (pi, tau, phi, e, or sqrt2).",
            category: "Input/Basic",
            synonyms: new string[3] { "PI", "TAU", "PHI" },
            hasPreview: false,
            selectableFunctions: new()
            {
                { "PI", "PI" },
                { "TAU", "TAU" },
                { "PHI", "PHI" },
                { "E", "E" },
                { "SQRT2", "SQRT2" },
            },
            functionSelectorLabel: " ",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the constant value selected with the dropdown"
                )
            }
        );
    }
}
