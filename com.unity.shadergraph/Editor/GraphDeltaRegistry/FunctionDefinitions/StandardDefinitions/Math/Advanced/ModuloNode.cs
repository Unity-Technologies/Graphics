using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ModuloNode : IStandardNode
    {
        public static string Name = "Modulo";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = fmod(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the remainder of dividing A by B",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[2] { "fmod", "%" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the numerator"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the denominator"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the remainder of dividing input A by input B"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Advanced" },
            { "Name.Synonyms", "fmod, %" },
            { "Tooltip", "returns the remainder of dividing A by B" },
            { "Parameters.A.Tooltip", "the numerator" },
            { "Parameters.B.Tooltip", "the denominator" },
            { "Parameters.Out.Tooltip", "the remainder of dividing input A by input B" }
        };
    }
}
