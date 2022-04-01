using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class LerpNode : IStandardNode
    {
        public static string Name = "Lerp";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = lerp(A, B, T);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("T", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "blends between A and B given the value of T",
            categories: new string[2] { "Math", "Interpolation" },
            synonyms: new string[5] { "Mix", "Blend", "Interpolate", "Extrapolate", "Linear Interpolate" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Out will be this value when T is zero"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Out will be this value when T is one"
                ),
                new ParameterUIDescriptor(
                    name: "T",
                    tooltip: "the blend value.  Will return A when this is 0 and B when this is 1"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A * (1-T) + B * T"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Interpolation" },
            { "Name.Synonyms", "Mix, Blend, Interpolate, Extrapolate, Linear Interpolate" },
            { "Tooltip", "blends between A and B given the value of T" },
            { "Parameters.A.Tooltip", "Out will be this value when T is zero" },
            { "Parameters.B.Tooltip", "Out will be this value when T is one" },
            { "Parameters.T.Tooltip", "the blend value.  Will return A when this is 0 and B when this is 1" },
            { "Parameters.Out.Tooltip", "A * (1-T) + B * T" }
        };
    }
}
