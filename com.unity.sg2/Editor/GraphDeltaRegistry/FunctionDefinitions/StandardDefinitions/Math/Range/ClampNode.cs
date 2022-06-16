using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ClampNode : IStandardNode
    {
        public static string Name => "Clamp";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = clamp(In, Min, Max);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Min", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Max", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Prevents an input value from going below the specified min or above the specified max.",
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
    }
}
