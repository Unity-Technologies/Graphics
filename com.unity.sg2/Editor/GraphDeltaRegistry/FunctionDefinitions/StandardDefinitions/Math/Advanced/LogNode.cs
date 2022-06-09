using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class LogNode : IStandardNode
    {
        static string Name = "Log";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    "LogBaseE",
                    "    Out = log(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    "LogBase2",
                    "    Out = log2(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    "LogBase10",
                    "    Out = log10(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the logarithm of the input.",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[0] {  },
            selectableFunctions: new()
            {
                { "LogBaseE", "BaseE" },
                { "LogBase2", "Base2" },
                { "LogBase10", "Base10" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "logarithm of the input"
                )
            }
        );
    }
}
